using System;
using BepInEx.Logging;
using Bulbul;
using ChillMusicInformationSync.UISync;
using static ChillMusicInformationSync.SMTC.SMTCImport;


namespace ChillMusicInformationSync.SMTC
{

    public sealed class SMTCEventHandle : IDisposable
    {
        private static SMTCEventHandle _instance;
        public static SMTCEventHandle Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SMTCEventHandle();
                }
                return _instance;
            }
        }
        private bool _isInitialized = false;

        // 私有构造函数，防止外部直接实例化
        private SMTCEventHandle() { }
        private ManualLogSource _logger;
        public void Initialize(ManualLogSource logger)
        {
            if (_isInitialized) return;
            _logger = logger;
            try
            {
                // 启动 C++ Worker 线程和 WinRT/COM 初始化
                SMTCImport.Instance.Initialize();

                // 订阅回调事件 (在主线程中完成订阅)
                SMTCImport.Instance.SMTCDataChanged += OnSMTCDataChanged;

                _logger.LogInfo("[SMTC Event Handle] 库初始化成功，已订阅事件。");

                // 首次强制更新一次，以显示当前状态
                UpdateAllData();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[SMTC Event Handle] 初始化失败: {ex.Message}");
            }
        }
        public void Dispose()
        {
            if (!_isInitialized) return;
            SMTCImport.Instance.SMTCDataChanged -= OnSMTCDataChanged;
            SMTCImport.Instance.Dispose();
            _isInitialized = false;
            _logger.LogInfo("[SMTC Event Handle] 已清理资源。");
        }

        // --- 3. 核心：事件回调函数 (Unity 主线程) ---

        private void OnSMTCDataChanged(SMTC_EventType eventType)
        {
            // 确定性：此代码块在 Unity/Mono 的主线程中执行。

            switch (eventType)
            {
                case SMTC_EventType.MediaPropertiesChanged:
                    UpdateMediaProperties();
                    break;

                case SMTC_EventType.PlaybackStatusChanged:
                    UpdatePlaybackStatus();
                    break;

                case SMTC_EventType.TimelineChanged:
                    UpdateTimeline();
                    break;

                case SMTC_EventType.SessionChanged:
                    _logger.LogInfo("[SMTC Event Handle] Session 发生切换，执行全量更新。");
                    UpdateAllData();
                    break;
            }
        }

        // --- 4. 事件对应的私有更新方法 ---

        private void UpdateAllData()
        {
            UpdateMediaProperties();
            UpdatePlaybackStatus();
            UpdateTimeline();
        }

        private void UpdateMediaProperties()
        {
            string title = SMTCImport.Instance.GetTitle();
            string artist = SMTCImport.Instance.GetArtist();

            // 2. 使用 internal set 更新 SMTCStatus
            SMTCStatus.CurrentTitle = title;
            SMTCStatus.CurrentArtist = artist;

            UISync.MusicUISync.changeMusicTrigger(title, artist, MusicChangeKind.Manual);
            _logger.LogInfo($"[SMTC Event Handle] 状态更新: {title} - {artist}");
        }

        private void UpdatePlaybackStatus()
        {
            SMTCStatus.IsPlaying = SMTCImport.Instance.IsPlaying();
            bool isPlaying = SMTCImport.Instance.IsPlaying();
            string title = SMTCImport.Instance.GetTitle();
            string artist = SMTCImport.Instance.GetArtist();
            MusicUISync.SetGameMainState(isPlaying);
            if (isPlaying)
            {
                UISync.MusicUISync.changeMusicTrigger(title, artist, MusicChangeKind.Manual);
                UISync.MusicUISync.SetButtonToPause();
            }
            else
            {
                UISync.MusicUISync.SetButtonToPlay();
            }

            _logger.LogInfo($"[SMTC Event Handle] 状态更新: {(isPlaying ? "播放中" : "暂停")}");
        }
        
        private void UpdateTimeline()
        {
            (TimeSpan Position, TimeSpan Duration) timeline = SMTCImport.Instance.GetTimeline();

            if ( timeline.Duration.TotalSeconds >= 0)
            {
                float normalizedProgress = (float)(timeline.Position.TotalSeconds / timeline.Duration.TotalSeconds);
                SMTCStatus.CurrentProgress = normalizedProgress;
                //_logger.LogInfo($"[SMTC Event Handle] 进度更新: {timeline.Position} / {timeline.Duration} ({normalizedProgress:P2})");
            }
        }
       
    }
}
