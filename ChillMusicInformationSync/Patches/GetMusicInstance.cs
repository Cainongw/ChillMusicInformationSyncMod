using Bulbul;
using ChillMusicInformationSync.UISync;
using HarmonyLib;
using System.Reflection;

namespace ChillMusicInformationSync.Patches
{
    [HarmonyPatch(typeof(FacilityMusic), "Setup")]
    public static class MusicUISetupPatch
    {
        // 游戏重构后 _musicUI 字段已移除，现在使用 DI 注入的 IMusicPlayerUI[] 数组
        private static readonly FieldInfo MusicPlayerUIsField =
            AccessTools.Field(typeof(FacilityMusic), "_musicPlayerUIs");

        [HarmonyPostfix]
        public static void Postfix(FacilityMusic __instance)
        {
            // 检查 MusicUISync 是否已经捕获了实例
            if (!MusicUISync.isMusicUISynced())
            {
                MusicUI musicUIInstance = null;

                // 从 IMusicPlayerUI[] 数组中查找 MusicUI 实例
                var playerUIs = MusicPlayerUIsField?.GetValue(__instance) as IMusicPlayerUI[];
                if (playerUIs != null)
                {
                    foreach (var ui in playerUIs)
                    {
                        if (ui is MusicUI found)
                        {
                            musicUIInstance = found;
                            break;
                        }
                    }
                }

                MusicUISync.GetInstance(__instance, musicUIInstance);
                ControlButtonHider.GetMusicUIInstance(musicUIInstance);
            }
        }
    }
}
