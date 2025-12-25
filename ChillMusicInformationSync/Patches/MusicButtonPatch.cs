using Bulbul;
using ChillMusicInformationSync.SMTC;
using HarmonyLib;
using System.Reflection;


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
        // 缓存反射字段以保证性能
        private static FieldInfo _uiField = typeof(FacilityMusic).GetField("_musicUI", BindingFlags.NonPublic | BindingFlags.Instance);

        [HarmonyPrefix]
        static bool Prefix(FacilityMusic __instance)
        {
            if (SMTCStatus.IsControlledByMod)
            {
                // 获取 UI 实例对象 (类型为 FacilityMusicUI)
                object musicUI = _uiField.GetValue(__instance);

                // 判断当前 UI 的视觉状态
                if (!__instance.IsPaused)
                {
                    // --- 1. 执行暂停逻辑 ---
                    SMTCImport.SMTC_Pause();

                    // 强制 UI 切换到“播放图标”样式
                    // 反射调用 _musicUI.OnPauseMusic()
                    musicUI.GetType().GetMethod("OnPauseMusic").Invoke(musicUI, null);
                }
                else
                {
                    // --- 2. 执行播放逻辑 ---
                    SMTCImport.SMTC_Play();

                    // 强制 UI 切换到“暂停图标”样式
                    // 反射调用 _musicUI.OnPlayMusic()
                    musicUI.GetType().GetMethod("OnPlayMusic").Invoke(musicUI, null);
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
            // 如果外部正在控制且正在播放，强制让游戏认为“现在不处于暂停状态”
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
        



