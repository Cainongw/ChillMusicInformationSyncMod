using Bulbul;
using ChillMusicInformationSync.SMTC;
using ChillMusicInformationSync.UISync;
using HarmonyLib;


namespace ChillMusicInformationSync.Patches
{
    /*
    [HarmonyPatch(typeof(FacilityMusic), "PauseMusic")]
    public static class FacilityPausePatch
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            if (SMTCStatus.IsControlledByMod)
            {
                SMTCImport.SMTC_Pause();
                return false;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(FacilityMusic), "UnPauseMusic")]
    public static class FacilityUnPausePatch
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            if (SMTCStatus.IsControlledByMod)
            {
                SMTCImport.SMTC_Play();
                return false;
            }
            return true;
        }
    }
    */
    [HarmonyPatch(typeof(FacilityMusic), "OnClickButtonPlayOrPauseMusic")]
    public class PlayPauseButtonPatch
    {
        [HarmonyPrefix]
        static bool Prefix(FacilityMusic __instance)
        {
            if (SMTCStatus.IsControlledByMod)
            {
                if (!__instance.IsPaused)
                {
                    // --- 1. 执行暂停逻辑 ---
                    SMTCImport.SMTC_Pause();
                    MusicUISync.SetButtonToPlay();
                }
                else
                {
                    // --- 2. 执行播放逻辑 ---
                    SMTCImport.SMTC_Play();
                    MusicUISync.SetButtonToPause();
                }

                return false; // 拦截原生逻辑，防止内置音乐被触发
            }
            return true; // 正常模式
        }
    }
    [HarmonyPatch(typeof(FacilityMusic), "IsPaused", MethodType.Getter)]
    public class IsPausedPatch
    {
        static void Postfix(ref bool __result)
        {
            // 如果外部正在控制且正在播放，强制让游戏认为"现在不处于暂停状态"
            // 这样 Prefix 里的 !__instance.IsPaused 才能正确识别到当前正在播放
            if (SMTCStatus.IsControlledByMod && SMTCStatus.IsPlaying)
            {
                __result = false;
            }
        }
    }
    [HarmonyPatch(typeof(FacilityMusic), "OnClickButtonSkip")]
    public static class OnNextMusicPatch
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            if (SMTCStatus.IsControlledByMod)
            {
                SMTCImport.SMTC_Next();
                return false;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(FacilityMusic), "OnClickButtonBack")]
    public static class OnPreviousMusicPatch
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            if (SMTCStatus.IsControlledByMod)
            {
                SMTCImport.SMTC_Previous();
                return false;
            }
            return true;
        }
    }
}
        



