// SafeSMTC.cpp — 事件驱动安全版本（针对 C# 回调）
#include <string>
#include <vector>
#include <mutex>
#include <atomic>
#include <thread>
#include <iostream>
#include <algorithm>
#include <queue>
#include <condition_variable>
#include <functional>
#include <windows.h>
// ... (保留所有原有的 WinRT 和 Core Audio 头文件) ...
#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Foundation.Collections.h>
#include <winrt/Windows.Media.Control.h>
#include <winrt/Windows.Storage.Streams.h>
#include <mmdeviceapi.h>
#include <endpointvolume.h>
#pragma comment(lib, "Ole32.lib")

using namespace winrt;
using namespace Windows::Media::Control;
using namespace Windows::Storage::Streams;
using namespace Windows::Foundation;

// ================= 全局控制变量 =================
// ... (保留 g_isRunning, g_workerThread, g_dataMutex, g_title, g_artist, ...) ...
static std::atomic<bool> g_isRunning{ false };
static std::thread g_workerThread;
static std::mutex g_dataMutex;
static std::string g_title;
static std::string g_artist;
static std::vector<uint8_t> g_coverBuffer;
static int64_t g_positionTicks = 0;
static int64_t g_durationTicks = 0;
static bool g_isPlaying = false;
static bool g_hasNewCover = false;
static std::atomic<bool> g_isDataDirty{ false }; // 新增：数据是否发生变化标记

// ... (保留 WinRT 管理对象 g_manager, g_currentSession, tokens, 队列等) ...
static GlobalSystemMediaTransportControlsSessionManager g_manager = nullptr;
static GlobalSystemMediaTransportControlsSession g_currentSession = nullptr;
static winrt::event_token g_sessionChangedToken{};
static winrt::event_token g_mediaPropertiesToken{};
static winrt::event_token g_timelinePropertiesToken{};
static winrt::event_token g_playbackInfoToken{};
static std::mutex g_queueMutex;
static std::condition_variable g_queueCv;
static std::queue<std::function<void()>> g_taskQueue;
static bool g_isChangingSession = false;

// ================= C# 回调接口定义 =================
// 定义事件类型：0=媒体属性(Title/Artist/Cover), 1=时间轴(Position/Duration), 2=播放状态(Playing)
enum SMTC_EventType {
    MediaPropertiesChanged = 0,
    TimelineChanged = 1,
    PlaybackStatusChanged = 2,
    SessionChanged = 3 // 内部使用，但可以暴露给 C#
};

// C# 回调函数指针类型：当数据变化时被调用
// 注意：这个回调是在 C++ worker 线程中调用的！C# 侧需要确保线程安全。
using SMTC_UpdateCallback = void(__stdcall*)(SMTC_EventType eventType);
static SMTC_UpdateCallback g_externalCallback = nullptr;

// ================= Core Audio helper =================
// ... (保留 GetEndpointVolume 和 ChangeSystemVolumeBy 函数) ...
static IAudioEndpointVolume* GetEndpointVolume() { /* ... 原有实现 ... */
    HRESULT hr = S_OK; IMMDeviceEnumerator* pEnumerator = nullptr; IMMDevice* pDevice = nullptr; IAudioEndpointVolume* pVolume = nullptr;
    hr = CoCreateInstance(__uuidof(MMDeviceEnumerator), nullptr, CLSCTX_INPROC_SERVER, __uuidof(IMMDeviceEnumerator), (void**)&pEnumerator);
    if (FAILED(hr) || !pEnumerator) { if (pEnumerator) pEnumerator->Release(); return nullptr; }
    hr = pEnumerator->GetDefaultAudioEndpoint(eRender, eConsole, &pDevice);
    pEnumerator->Release();
    if (FAILED(hr) || !pDevice) { if (pDevice) pDevice->Release(); return nullptr; }
    hr = pDevice->Activate(__uuidof(IAudioEndpointVolume), CLSCTX_INPROC_SERVER, nullptr, (void**)&pVolume);
    pDevice->Release();
    if (FAILED(hr) || !pVolume) { if (pVolume) pVolume->Release(); return nullptr; }
    return pVolume;
}
static void ChangeSystemVolumeBy(double delta) { 
    IAudioEndpointVolume* pVolume = GetEndpointVolume(); if (!pVolume) return;
    float current = 0.0f; HRESULT hr = pVolume->GetMasterVolumeLevelScalar(&current);
    if (SUCCEEDED(hr)) {
        double target = static_cast<double>(current) + delta;
        if (target < 0.0) target = 0.0; if (target > 1.0) target = 1.0;
        pVolume->SetMasterVolumeLevelScalar(static_cast<float>(target), nullptr);
    }
    pVolume->Release();
}
static void SetSystemVolumeTo(float targetVolume) {
    IAudioEndpointVolume* pVolume = GetEndpointVolume();
    if (!pVolume) return;

    // 确保音量在 0.0 到 1.0 之间
    if (targetVolume < 0.0f) targetVolume = 0.0f;
    if (targetVolume > 1.0f) targetVolume = 1.0f;

    // SetMasterVolumeLevelScalar 接受 float (0.0~1.0), pGUID 可为 nullptr 表示系统范围改变
    pVolume->SetMasterVolumeLevelScalar(targetVolume, nullptr);

    pVolume->Release();
}

// ================= 辅助函数 =================
static std::string WinRTStringToString(hstring const& hstr) {
    return winrt::to_string(hstr);
}
static void EnqueueTask(std::function<void()> task) { /* ... 原有实现 ... */
    { std::lock_guard<std::mutex> lk(g_queueMutex); g_taskQueue.push(std::move(task)); } g_queueCv.notify_one();
}
static void UnregisterCurrentSessionEvents() { /* ... 原有实现 ... */
    try {
        if (g_currentSession) {
            if (g_mediaPropertiesToken.value) { try { g_currentSession.MediaPropertiesChanged(g_mediaPropertiesToken); } catch (...) {} g_mediaPropertiesToken = {}; }
            if (g_timelinePropertiesToken.value) { try { g_currentSession.TimelinePropertiesChanged(g_timelinePropertiesToken); } catch (...) {} g_timelinePropertiesToken = {}; }
            if (g_playbackInfoToken.value) { try { g_currentSession.PlaybackInfoChanged(g_playbackInfoToken); } catch (...) {} g_playbackInfoToken = {}; }
        }
    }
    catch (...) {}
}

// **新增：调用 C# 回调（安全地在 Worker 线程中）**
static void TriggerCallback(SMTC_EventType eventType) {
    if (g_externalCallback) {
        // 重要：在 C++ worker 线程调用 C# 函数。
        // C# 侧需要考虑是否需要 Marshal 到主线程。
        g_externalCallback(eventType);
    }
}


// ================= Update 函数（在 Worker 线程中执行） =================
fire_and_forget UpdateMediaProperties(GlobalSystemMediaTransportControlsSession session) {
    try {
        auto strongSession = session;
        auto props = co_await strongSession.TryGetMediaPropertiesAsync();

        if (!props) co_return;

        bool changed = false;
        {
            std::lock_guard<std::mutex> lk(g_dataMutex);
            std::string newTitle = WinRTStringToString(props.Title());
            std::string newArtist = WinRTStringToString(props.Artist());

            if (g_title != newTitle || g_artist != newArtist) {
                g_title = std::move(newTitle);
                g_artist = std::move(newArtist);
                changed = true;
            }
        }

        // 处理封面
        auto thumbRef = props.Thumbnail();
        if (thumbRef) {
            auto stream = co_await thumbRef.OpenReadAsync();
            if (stream) {
                // ... (封面读取和写入 g_coverBuffer 的逻辑不变) ...
                DataReader reader(stream);
                uint32_t size = static_cast<uint32_t>(stream.Size());
                co_await reader.LoadAsync(size);

                std::vector<uint8_t> localBuf(size);
                reader.ReadBytes(localBuf);

                {
                    std::lock_guard<std::mutex> lk(g_dataMutex);
                    // 实际项目中，你可能需要比较 localBuf 和 g_coverBuffer 来避免不必要的更新
                    // 这里我们简单地认为获取成功就更新
                    g_coverBuffer = std::move(localBuf);
                    g_hasNewCover = true;
                    changed = true; // 封面变化也算 MediaPropertiesChanged
                }
            }
        }
        else {
            std::lock_guard<std::mutex> lk(g_dataMutex);
            if (!g_coverBuffer.empty()) {
                g_coverBuffer.clear();
                g_hasNewCover = false;
                changed = true;
            }
        }

        if (changed) {
            g_isDataDirty.store(true);
            TriggerCallback(SMTC_EventType::MediaPropertiesChanged);
        }
    }
    catch (...) { /* 忽略异常 */ }
}

static void UpdateTimeline_Internal(GlobalSystemMediaTransportControlsSession session) {
    try {
        auto timeline = session.GetTimelineProperties();
        if (timeline) {
            bool changed = false;
            {
                std::lock_guard<std::mutex> lk(g_dataMutex);
                int64_t newPosition = timeline.Position().count();
                int64_t newDuration = timeline.EndTime().count();

                if (g_positionTicks != newPosition || g_durationTicks != newDuration) {
                    g_positionTicks = newPosition;
                    g_durationTicks = newDuration;
                    changed = true;
                }
            }
            if (changed) {
                g_isDataDirty.store(true);
                TriggerCallback(SMTC_EventType::TimelineChanged);
            }
        }
    }
    catch (...) {}
}

static void UpdatePlaybackInfo_Internal(GlobalSystemMediaTransportControlsSession session) {
    try {
        auto info = session.GetPlaybackInfo();
        if (info) {
            bool changed = false;
            {
                std::lock_guard<std::mutex> lk(g_dataMutex);
                bool newIsPlaying = (info.PlaybackStatus() == GlobalSystemMediaTransportControlsSessionPlaybackStatus::Playing);
                if (g_isPlaying != newIsPlaying) {
                    g_isPlaying = newIsPlaying;
                    changed = true;
                }
            }
            if (changed) {
                g_isDataDirty.store(true);
                TriggerCallback(SMTC_EventType::PlaybackStatusChanged);
            }
        }
    }
    catch (...) {}
}

// ... (SetupSessionEvents_Internal, OnSessionManagerChanged_Internal, WorkerThreadFunc 保持原有逻辑，但 OnSessionManagerChanged_Internal 应在最后触发 SessionChanged 事件) ...
static void SetupSessionEvents_Internal(GlobalSystemMediaTransportControlsSession session) {
    if (!session) return;
    winrt::weak_ref<GlobalSystemMediaTransportControlsSession> weakSession{ session };

    g_mediaPropertiesToken = session.MediaPropertiesChanged([weakSession](auto&& s, auto&&) {
        EnqueueTask([weakSession]() { if (auto strong = weakSession.get()) { UpdateMediaProperties(strong); } });
        });
    g_timelinePropertiesToken = session.TimelinePropertiesChanged([weakSession](auto&& s, auto&&) {
        EnqueueTask([weakSession]() { if (auto strong = weakSession.get()) { UpdateTimeline_Internal(strong); } });
        });
    g_playbackInfoToken = session.PlaybackInfoChanged([weakSession](auto&& s, auto&&) {
        EnqueueTask([weakSession]() { if (auto strong = weakSession.get()) { UpdatePlaybackInfo_Internal(strong); } });
        });

    // 启动一次初始读取
    EnqueueTask([weakSession]() {
        if (auto strong = weakSession.get()) {
            UpdateMediaProperties(strong);
            UpdateTimeline_Internal(strong);
            UpdatePlaybackInfo_Internal(strong);
        }
        });
}


static void OnSessionManagerChanged_Internal() {
    if (!g_manager) return;
    if (g_isChangingSession) return;
    g_isChangingSession = true;

    UnregisterCurrentSessionEvents();
    g_currentSession = nullptr;

    GlobalSystemMediaTransportControlsSession bestSession = nullptr;
    try {
        auto sessions = g_manager.GetSessions();
        for (auto const& s : sessions) {
            try {
                auto info = s.GetPlaybackInfo();
                if (info && info.PlaybackStatus() == GlobalSystemMediaTransportControlsSessionPlaybackStatus::Playing) {
                    bestSession = s;
                    break;
                }
            }
            catch (...) {}
        }
        if (!bestSession) {
            bestSession = g_manager.GetCurrentSession();
        }
    }
    catch (...) {}

    g_currentSession = bestSession;
    if (g_currentSession) {
        SetupSessionEvents_Internal(g_currentSession);
        // 确保首次加载也触发事件（因为 SetupSessionEvents_Internal 会 Enqueue 初始读取）
    }

    g_isChangingSession = false;

    // **新增：Session 切换完成，通知外部**
    g_isDataDirty.store(true);
    TriggerCallback(SMTC_EventType::SessionChanged);
}


static void WorkerThreadFunc() {
    init_apartment();

    // 请求 manager
    try {
        auto op = GlobalSystemMediaTransportControlsSessionManager::RequestAsync();
        while (op.Status() == AsyncStatus::Started && g_isRunning.load()) {
            std::this_thread::sleep_for(std::chrono::milliseconds(10));
        }
        if (!g_isRunning.load()) { uninit_apartment(); return; }

        g_manager = op.GetResults();
        if (!g_manager) { uninit_apartment(); return; }

        // 注册 manager.CurrentSessionChanged 事件
        winrt::weak_ref<GlobalSystemMediaTransportControlsSessionManager> weakManager{ g_manager };
        g_sessionChangedToken = g_manager.CurrentSessionChanged([weakManager](auto&&, auto&&) {
            EnqueueTask([weakManager]() { if (auto mgr = weakManager.get()) { OnSessionManagerChanged_Internal(); } });
            });

        // 首次执行一次
        EnqueueTask([]() { OnSessionManagerChanged_Internal(); });

        // 主循环：等待任务并执行
        while (g_isRunning.load()) {
            std::function<void()> task;
            {
                std::unique_lock<std::mutex> lk(g_queueMutex);
                if (g_taskQueue.empty()) {
                    g_queueCv.wait_for(lk, std::chrono::milliseconds(200));
                }
                if (!g_taskQueue.empty()) {
                    task = std::move(g_taskQueue.front());
                    g_taskQueue.pop();
                }
            }

            if (task) {
                try { task(); }
                catch (...) {}
            }
        }

        // 退出前清理：注销 session 和 manager 事件
        try {
            UnregisterCurrentSessionEvents();
            if (g_sessionChangedToken.value) { try { g_manager.CurrentSessionChanged(g_sessionChangedToken); } catch (...) {} g_sessionChangedToken = {}; }
        }
        catch (...) {}

        g_manager = nullptr;
        g_currentSession = nullptr;
    }
    catch (...) {}

    uninit_apartment();
}


// ================= 导出接口 =================

// **新增：注册 C# 回调函数**
extern "C" __declspec(dllexport) void RegisterUpdateCallback(SMTC_UpdateCallback callback) {
    // 简单地保存函数指针，不需要线程安全锁，因为只有 worker 线程会调用它
    g_externalCallback = callback;
}

// **新增：重置数据变化标志**
extern "C" __declspec(dllexport) void SMTC_ClearDataDirtyFlag() {
    g_isDataDirty.store(false);
}

// **新增：检查是否有数据变化**
extern "C" __declspec(dllexport) bool SMTC_IsDataDirty() {
    return g_isDataDirty.load();
}


extern "C" __declspec(dllexport) void InitSMTC() {
    bool expected = false;
    if (!g_isRunning.compare_exchange_strong(expected, true)) { return; }
    g_workerThread = std::thread([]() { WorkerThreadFunc(); });
}

extern "C" __declspec(dllexport) void ShutdownSMTC() {
    bool expected = true;
    if (!g_isRunning.compare_exchange_strong(expected, false)) { return; }
    g_queueCv.notify_one();
    if (g_workerThread.joinable()) { try { g_workerThread.join(); } catch (...) {} }
    {
        std::lock_guard<std::mutex> lk(g_dataMutex);
        g_title.clear(); g_artist.clear(); g_coverBuffer.clear(); g_positionTicks = 0; g_durationTicks = 0; g_isPlaying = false; g_hasNewCover = false;
    }
    g_externalCallback = nullptr; // 清理回调
}



static void EnqueueControl(std::function<void(GlobalSystemMediaTransportControlsSession)> fn) {
    EnqueueTask([fn]() {
        if (g_currentSession) {
            try { fn(g_currentSession); }
            catch (...) {}
        }
        });
}
extern "C" __declspec(dllexport) void SMTC_PlayPause() { EnqueueControl([](auto session) { session.TryTogglePlayPauseAsync(); }); }
extern "C" __declspec(dllexport) void SMTC_Play() { EnqueueControl([](auto session) { session.TryPlayAsync(); }); }
extern "C" __declspec(dllexport) void SMTC_Pause() { EnqueueControl([](auto session) { session.TryPauseAsync(); }); }
extern "C" __declspec(dllexport) void SMTC_Next() { EnqueueControl([](auto session) { session.TrySkipNextAsync(); }); }
extern "C" __declspec(dllexport) void SMTC_Previous() { EnqueueControl([](auto session) { session.TrySkipPreviousAsync(); }); }
extern "C" __declspec(dllexport) void SMTC_VolumeUp() { EnqueueTask([]() { try { ChangeSystemVolumeBy(0.05); } catch (...) {} }); }
extern "C" __declspec(dllexport) void SMTC_VolumeDown() { EnqueueTask([]() { try { ChangeSystemVolumeBy(-0.05); } catch (...) {} }); }
extern "C" __declspec(dllexport) void SMTC_SetVolume(float volume) {EnqueueTask([volume]() {try { SetSystemVolumeTo(volume); }catch (...) {} });
}


extern "C" __declspec(dllexport) int SMTC_GetTitle(char* buffer, int len) {
    if (!buffer || len <= 0) return 0;
    std::lock_guard<std::mutex> lk(g_dataMutex);
    if (g_title.empty()) return 0;
    int copyLen = std::min<int>(len - 1, (int)g_title.size());
    memcpy(buffer, g_title.data(), copyLen);
    buffer[copyLen] = '\0';
    return copyLen;
}
extern "C" __declspec(dllexport) int SMTC_GetArtist(char* buffer, int len) { /* ... 原有实现 ... */
    if (!buffer || len <= 0) return 0; std::lock_guard<std::mutex> lk(g_dataMutex); if (g_artist.empty()) return 0;
    int copyLen = std::min<int>(len - 1, (int)g_artist.size()); memcpy(buffer, g_artist.data(), copyLen); buffer[copyLen] = '\0'; return copyLen;
}
extern "C" __declspec(dllexport) bool SMTC_GetPlaybackStatus() {
    std::lock_guard<std::mutex> lk(g_dataMutex);
    return g_isPlaying;
}
extern "C" __declspec(dllexport) void SMTC_GetTimeline(long long* position, long long* duration) {
    if (!position || !duration) return;
    std::lock_guard<std::mutex> lk(g_dataMutex);
    *position = g_positionTicks;
    *duration = g_durationTicks;
}
extern "C" __declspec(dllexport) int SMTC_GetCoverImage(uint8_t* buffer, int len) {
    if (!buffer || len <= 0) return 0;
    std::lock_guard<std::mutex> lk(g_dataMutex);
    if (g_coverBuffer.empty()) return 0;
    int copyLen = std::min<int>(len, (int)g_coverBuffer.size());
    memcpy(buffer, g_coverBuffer.data(), copyLen);
    return copyLen;
}
extern "C" __declspec(dllexport) void SMTC_SetTimeline(long long positionTicks) {
    EnqueueControl([positionTicks](auto session) {
        session.TryChangePlaybackPositionAsync(positionTicks);
        });
}