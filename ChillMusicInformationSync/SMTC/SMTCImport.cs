using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace ChillMusicInformationSync.SMTC
{
    public sealed class SMTCImport : IDisposable
    {
        // ============= P/Invoke 导入 =============
        const string DllName = "SMTC-Bridge-Cpp.dll";
        private const CallingConvention NativeCall = CallingConvention.Cdecl;


        public enum SMTC_EventType
        {
            MediaPropertiesChanged = 0, // Title, Artist, Cover 变化
            TimelineChanged = 1,        // Position, Duration 变化
            PlaybackStatusChanged = 2,  // 播放状态变化
            SessionChanged = 3          // Session 切换 (如切换播放器)
        }
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void SMTC_UpdateCallback(SMTC_EventType eventType);

        // DllImport 声明: 生命周期
        [DllImport(DllName, CallingConvention = NativeCall)]
        private static extern void InitSMTC();

        [DllImport(DllName, CallingConvention = NativeCall)]
        private static extern void ShutdownSMTC();

        // DllImport 声明: 回调注册
        [DllImport(DllName, CallingConvention = NativeCall)]
        private static extern void RegisterUpdateCallback(SMTC_UpdateCallback callback);

        // DllImport 声明: Getter
        [DllImport(DllName, CallingConvention = NativeCall)]
        private static extern int SMTC_GetTitle(byte[] buffer, int len);

        [DllImport(DllName, CallingConvention = NativeCall)]
        private static extern int SMTC_GetArtist(byte[] buffer, int len);

        [DllImport(DllName, CallingConvention = NativeCall)]
        private static extern bool SMTC_GetPlaybackStatus();

        [DllImport(DllName, CallingConvention = NativeCall)]
        private static extern void SMTC_GetTimeline(out long position, out long duration);
        [DllImport(DllName, CallingConvention = NativeCall)]
        private static extern void SMTC_SetTimeline(long positionTicks);

        // DllImport 声明: 控制
        [DllImport(DllName, CallingConvention = NativeCall)]
        public static extern void SMTC_Play();

        [DllImport(DllName, CallingConvention = NativeCall)]
        public static extern void SMTC_Pause();

        [DllImport(DllName, CallingConvention = NativeCall)]
        public static extern void SMTC_Next();
        [DllImport(DllName, CallingConvention = NativeCall)]
        public static extern void SMTC_Previous();

        [DllImport(DllName, CallingConvention = NativeCall)]
        public static extern void SMTC_VolumeUp();

        [DllImport(DllName, CallingConvention = NativeCall)]
        public static extern void SMTC_SetVolume(float volume);

        public event Action<SMTC_EventType> SMTCDataChanged;

        // 单例模式
        private static readonly SMTCImport _instance = new SMTCImport();
        public static SMTCImport Instance => _instance;

        // 存储回调委托，防止被垃圾回收 (GC)
        private readonly SMTC_UpdateCallback _callbackDelegate;

        // 捕获主线程的同步上下文，用于安全调度回调
        private SynchronizationContext _mainThreadContext;

        // 缓冲区 (避免每次 Getter 都重新分配内存)
        private readonly byte[] _titleBuffer = new byte[256];
        private readonly byte[] _artistBuffer = new byte[256];


        private SMTCImport()
        {
            // 实例化委托
            _callbackDelegate = new SMTC_UpdateCallback(OnNativeCallback);
        }
        public void Initialize()
        {
            if (_mainThreadContext != null)
            {
                // 已经初始化，避免重复操作
                return;
            }

            // 捕获当前的 SynchronizationContext (如果是 Unity/WPF/WinForms，这会是 UI 线程)
            _mainThreadContext = SynchronizationContext.Current;

            if (_mainThreadContext == null)
            {
                throw new InvalidOperationException("无法获取主线程上下文。请在有 SynchronizationContext 的主线程中调用 Initialize。");
            }

            InitSMTC();
            RegisterUpdateCallback(_callbackDelegate);
        }
        private void OnNativeCallback(SMTC_EventType eventType)
        {
            // 警告：此方法在 C++ 线程中运行！不应执行耗时或 UI 操作。
            // 立即调度回主线程。
            _mainThreadContext.Post(state =>
            {
                // 在主线程中触发 C# 事件
                SMTCDataChanged?.Invoke((SMTC_EventType)state);

            }, eventType);
        }
        public void Dispose()
        {
            if (_mainThreadContext == null) return;

            // 注销回调，防止 DLL 关闭后仍有调用
            RegisterUpdateCallback(null);
            ShutdownSMTC();
            _mainThreadContext = null;
        }


        // --- 3. 公共 Getter 方法（线程安全） ---

        public string GetTitle()
        {
            int len = SMTC_GetTitle(_titleBuffer, _titleBuffer.Length);
            if (len > 0)
            {
                // DLL 返回 UTF-8 编码的 C-style 字符串，需要正确解码
                return Encoding.UTF8.GetString(_titleBuffer, 0, len);
            }
            return string.Empty;
        }

        public string GetArtist()
        {
            int len = SMTC_GetArtist(_artistBuffer, _artistBuffer.Length);
            if (len > 0)
            {
                return Encoding.UTF8.GetString(_artistBuffer, 0, len);
            }
            return string.Empty;
        }

        public bool IsPlaying()
        {
            return SMTC_GetPlaybackStatus();
        }
        public (TimeSpan Position, TimeSpan Duration) GetTimeline()
        {
            SMTC_GetTimeline(out long positionTicks, out long durationTicks);

            // 1 Tick = 100 纳秒 (C++ TimeSpan 默认单位)
            return (TimeSpan.FromTicks(positionTicks), TimeSpan.FromTicks(durationTicks));
        }
        public static void SetTimelinePosition(TimeSpan position)
        {
            // C# TimeSpan.Ticks 的单位与 WinRT (100纳秒) 相同，可以直接转换
            SMTC_SetTimeline(position.Ticks);
        }
    }
}
