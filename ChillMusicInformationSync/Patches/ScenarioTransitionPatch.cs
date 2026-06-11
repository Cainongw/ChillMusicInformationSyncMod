using BepInEx.Logging;
using HarmonyLib;
using Bulbul;
using ChillMusicInformationSync.SMTC;
using ChillMusicInformationSync.UISync;

namespace ChillMusicInformationSync.Patches
{
    internal static class SMTCScenarioHelper
    {
        private static ManualLogSource _log;

        internal static void InitLogger(ManualLogSource log)
        {
            _log = log;
        }

        internal static void DisconnectSMTC()
        {
            if (!SMTCStatus.IsControlledByMod)
            {
                _log?.LogDebug("[ScenarioPatch] IsControlledByMod=false, 跳过断开");
                return;
            }

            _log?.LogInfo("[ScenarioPatch] 情景开始，断开SMTC...");

            SMTCStatus.IsSwitchingToBuiltIn = true;
            SMTCStatus.IsControlledByMod = false;
            SMTCStatus.IsPlaying = false;
            SMTCImport.SMTC_Pause();
            MusicCoverMananger.RemoveCoverFromCenterIcons();
            ControlButtonHider.RestoreButtons();

            _log?.LogInfo("[ScenarioPatch] SMTC已断开");
        }

        internal static void TryReconnectSMTC()
        {
            bool isPlaying = SMTCImport.Instance.IsPlaying();
            _log?.LogDebug($"[ScenarioPatch] 情景结束，SMTC是否在播放={isPlaying}");

            if (isPlaying)
            {
                SMTCStatus.IsSwitchingToBuiltIn = false;
                SMTCStatus.IsControlledByMod = true;
                SMTCStatus.IsPlaying = true;

                _log?.LogInfo("[ScenarioPatch] SMTC仍在播放，已恢复控制权");
            }
        }
    }

    [HarmonyPatch(typeof(RoomGameManager), "ReadyPlayStory")]
    public static class ScenarioReadyPatch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            SMTCScenarioHelper.DisconnectSMTC();
        }
    }

    [HarmonyPatch(typeof(UIShowManagerForPC), "AdjustUIForPlayScenario")]
    public static class ScenarioUIDisablePatch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            SMTCScenarioHelper.DisconnectSMTC();
        }
    }

    [HarmonyPatch(typeof(UIShowManagerForPC), "AdjustUIForEndScenario")]
    public static class ScenarioUIEnablePatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            SMTCScenarioHelper.TryReconnectSMTC();
        }
    }
}
