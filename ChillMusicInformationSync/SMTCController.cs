using BepInEx;
using BepInEx.Logging;
using System;
using System.Threading.Tasks;
using UnityEngine;
using Windows.Media.Control;
using WindowsMediaController;
using static WindowsMediaController.MediaManager;

namespace ChillMusicInformationSync
{
    public class SMTCController
    {
        public MediaManager mediaManager;
        // 接受主函数传进来的Logger 不继承BasePlugin
        private readonly ManualLogSource _logger;
        public SMTCController(ManualLogSource loggerInstance)
        {
            _logger = loggerInstance;
        }
        private MusicData currentMusicData = new MusicData();

        public async Task InitAsync()
        {
            mediaManager = new MediaManager();
            _logger.LogInfo("DEBUG: InitAsync step 1 - Subscriptions complete.");
            mediaManager.OnAnySessionOpened += OnSessionOpened;
            mediaManager.OnAnySessionClosed += OnSessionClosed;
            mediaManager.OnFocusedSessionChanged += OnFocusedSessionChanged;
            mediaManager.OnAnyMediaPropertyChanged += OnMediaPropertyChanged;
            mediaManager.OnAnyPlaybackStateChanged += OnPlaybackStateChanged;
            mediaManager.OnAnyTimelinePropertyChanged += OnTimelineChanged;

            await mediaManager.StartAsync();
            _logger.LogInfo("DEBUG: InitAsync step 2 - StartAsync finished.");
        }


        // ------------------ 事件回调 ------------------
        private void OnSessionOpened(MediaManager.MediaSession session)
        {
            _logger.LogInfo($"Session opened: {session.Id}");
        }

        private void OnSessionClosed(MediaManager.MediaSession session)
        {
            _logger.LogInfo($"Session closed: {session.Id}");
        }

        private void OnFocusedSessionChanged(MediaManager.MediaSession session)
        {
            _logger.LogInfo($"Focused session changed: {session.Id}");
        }

        private void OnMediaPropertyChanged(MediaManager.MediaSession sender, GlobalSystemMediaTransportControlsSessionMediaProperties args)
        {
            _logger.LogInfo($"Now playing: {args.Title} - {args.Artist}");
            currentMusicData.Title = args.Title;
            currentMusicData.Artist = args.Artist;
        }

        private void OnPlaybackStateChanged(MediaManager.MediaSession sender, GlobalSystemMediaTransportControlsSessionPlaybackInfo args)
        {
            _logger.LogInfo($"Playback state: {args.PlaybackStatus}");
        }

        private void OnTimelineChanged(MediaSession mediaSession, GlobalSystemMediaTransportControlsSessionTimelineProperties timelineProperties)
        {
            _logger.LogInfo($"Timeline updated: Position={timelineProperties.Position}");
            //currentMusicData.Timeline = timelineProperties.Position;
        }

        // ------------------ 控制方法 ------------------
        public void Play() => mediaManager.GetFocusedSession()?.ControlSession.TryPlayAsync();
        public void Pause() => mediaManager.GetFocusedSession()?.ControlSession.TryPauseAsync();
        public void Next() => mediaManager.GetFocusedSession()?.ControlSession.TrySkipNextAsync();
        public void Previous() => mediaManager.GetFocusedSession()?.ControlSession.TrySkipPreviousAsync();

        public async Task<string> GetTitleAsync()
        {
            var session = mediaManager.GetFocusedSession();
            if (session != null)
            {
                var props = await session.ControlSession.TryGetMediaPropertiesAsync();
                return props?.Title ?? "Unknown";
            }
            return "No session";
        }
    }
}