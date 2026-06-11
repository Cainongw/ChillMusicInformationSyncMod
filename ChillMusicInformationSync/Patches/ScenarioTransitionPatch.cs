using BepInEx.Logging;
using HarmonyLib;
using Bulbul;
using ChillMusicInformationSync.SMTC;

namespace ChillMusicInformationSync.Patches
{
    internal static class SMTCScenarioHelper
    {
        private static ManualLogSource _log;
        private static bool _wasControlledByMod;

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

            _wasControlledByMod = true;
            SMTCStatus.IsSwitchingToBuiltIn = true;
            SMTCStatus.IsControlledByMod = false;
            SMTCStatus.IsPlaying = false;
            SMTCImport.SMTC_Pause();

            _log?.LogInfo("[ScenarioPatch] SMTC已断开");
        }

        internal static void TryReconnectSMTC()
        {
            if (!_wasControlledByMod)
            {
                _log?.LogDebug("[ScenarioPatch] 断开前未被控制，跳过恢复");
                return;
            }
            _wasControlledByMod = false;

            _log?.LogInfo("[ScenarioPatch] 情景结束，恢复SMTC控制权");

            SMTCStatus.IsSwitchingToBuiltIn = false;
            SMTCStatus.IsControlledByMod = true;
            SMTCStatus.IsPlaying = SMTCImport.Instance.IsPlaying();
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
