using BepInEx.Logging;
using System;
using System.Reflection;
using UnityEngine;
using ChillMusicInformationSync.SMTC;

namespace ChillMusicInformationSync
{
    public class SyncBehaviour : MonoBehaviour
    {
        private static MusicUI _musicUIInstance;
        private static ManualLogSource _logger;
        private static MethodInfo _onChangeMusicMethod;
        private static bool _lastSMTCIsPlaying = false;

        private float _timer = 0f;

        public void InitLogger(BepInEx.Logging.ManualLogSource logger)
        {
            _logger = logger;
        }

        private void Start()
        {
            _logger?.LogInfo("SMTCSyncBehaviour started.");
        }
        void OnApplicationQuit()
        {
            SMTCEventHandle.Instance.Dispose();
        }
        void OnDestroy()
        {
            _logger?.LogInfo("SMTCSyncBehaviour OnDestroy called — shutting down SMTC...");
            try
            {
                SMTCEventHandle.Instance.Dispose();
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error during SMTC Shutdown: {ex}");
            }
        }
    }
}


