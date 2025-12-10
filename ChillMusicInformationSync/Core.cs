using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using UnityEngine;
using ChillMusicInformationSync.SMTC;
using ChillMusicInformationSync.UISync;



namespace ChillMusicInformationSync
{
    [BepInPlugin("caiw.ChillMusicInformationSync", "ChillMusicInformationSync", "1.0.0")]
    public class ChillMusicInformationSync : BaseUnityPlugin
    {
        internal static ChillMusicInformationSync Instance;
        private static bool _initialized = false;
        private ManualLogSource _logger;

        private static GameObject _runner;

        // 存储MusicUI实例
        private static MusicUI _musicUIInstance;
        private bool _lastSMTCIsPlaying = false;

        private void Awake()
        {
            Instance = this;
            _logger = Logger;
            Logger.LogInfo("Starting ChillMusicInformationSync...");
            try
            {
                _runner = new GameObject("SMTCSyncRunner");
                _runner.hideFlags = HideFlags.HideAndDontSave;
                DontDestroyOnLoad(_runner);
                _runner.SetActive(true);
                //挂载组件
                var behaviour =  _runner.AddComponent<SyncBehaviour>();
                behaviour.InitLogger(Logger);
                MusicUISync.Initialize_logger(Logger);
                SMTCEventHandle.Instance.Initialize(Logger);
                Logger.LogInfo("Runner 创建成功！");
            }
            catch (Exception ex)
            {
                Logger.LogInfo($"Runner 创建失败：{ex}");
            }
            try
            {
                var harmony = new Harmony("ChillMusicInformationSnyc");
                harmony.PatchAll();

            }
            catch (Exception ex)
            {
                Logger.LogInfo($"Harmony失败：{ex}");
            }
            Logger.LogInfo("ChillMusicInformationSync Initialized!");
        }
        private void OnDestroy()
        {
            
        }

        
    }
}




