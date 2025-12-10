using Bulbul;
using ChillMusicInformationSync.UISync;
using HarmonyLib;
using System.Reflection;

namespace ChillMusicInformationSync.Patches
{
    [HarmonyPatch(typeof(FacilityMusic), "Setup")]
    public static class MusicUISetupPatch
    {
        private static readonly FieldInfo MusicUIField = AccessTools.Field(typeof(FacilityMusic), "_musicUI");
        [HarmonyPostfix]
        public static void Postfix(FacilityMusic __instance)
        {
            // 检查 MusicUISync 是否已经捕获了实例
            if (!MusicUISync.isMusicUISynced()) 
            {
                // 确保只执行一次初始同步逻辑
                MusicUI musicUIInstance = MusicUIField?.GetValue(__instance) as MusicUI;
                MusicUISync.GetInstance(__instance , musicUIInstance);
            }
        }
    }
}
