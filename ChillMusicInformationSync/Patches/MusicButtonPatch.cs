using Bulbul;
using ChillMusicInformationSync.SMTC;
using ChillMusicInformationSync.UISync;
using HarmonyLib;
using System.Collections;
using UnityEngine;


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
    [HarmonyPatch(typeof(FacilityMusic), "UnPauseMusic")]
    public static class FacilityUnPausePatch
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            if (SMTCStatus.IsControlledByMod && SMTCStatus.IsPlaying)
            {
                MusicUISync.SetButtonToPause();
                ControlButtonHider.HideOriginalButtons();
                return false;
            }
            return true;
        }
    }
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
                    SMTCStatus.PlayAttemptId++;
                    SMTCImport.SMTC_Pause();
                    MusicUISync.SetButtonToPlay();
                }
                else
                {
                    // --- 2. 执行播放逻辑 ---
                    int attemptId = ++SMTCStatus.PlayAttemptId;
                    SMTCImport.SMTC_Play();
                    MusicUISync.SetButtonToPause();
                    if (ChillMusicInformationSync.RunnerBehaviour != null)
                    {
                        ChillMusicInformationSync.RunnerBehaviour.StartCoroutine(FallbackToBuiltInIfNoSession(attemptId));
                    }
                }

                return false; // 拦截原生逻辑，防止内置音乐被触发
            }
            return true; // 正常模式
        }
        private static IEnumerator FallbackToBuiltInIfNoSession(int attemptId)
        {
            yield return new WaitForSecondsRealtime(0.45f);
            if (attemptId == SMTCStatus.PlayAttemptId && SMTCStatus.IsControlledByMod && !SMTCStatus.IsPlaying)
            {
                MusicUISync.RestoreBuiltInMusic(true);
            }
        }
    }
    [HarmonyPatch(typeof(FacilityMusic), "IsPaused", MethodType.Getter)]
    public class IsPausedPatch
    {
        static void Postfix(ref bool __result)
        {
            // SMTC 接管时，以 SMTC 的真实播放状态为准，避免与内置 _mainState 脱节
            if (SMTCStatus.IsControlledByMod)
            {
                __result = !SMTCStatus.IsPlaying;
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
        



