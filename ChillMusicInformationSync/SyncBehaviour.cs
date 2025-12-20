using BepInEx.Logging;
using System;
using System.Reflection;
using UnityEngine;
using ChillMusicInformationSync.SMTC;

namespace ChillMusicInformationSync
{
    public class SyncBehaviour : MonoBehaviour
    {
        private static ManualLogSource _logger;

        public void InitLogger(BepInEx.Logging.ManualLogSource logger)
        {
            _logger = logger;
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


