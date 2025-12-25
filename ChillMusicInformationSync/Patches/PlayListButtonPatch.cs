using Bulbul;
using ChillMusicInformationSync.SMTC;
using ChillMusicInformationSync.UISync;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace ChillMusicInformationSync.Patches
{
    [HarmonyPatch(typeof(FacilityMusic), "OnClickButtonPlayListPlayMusicButton")]
    public class FacilityMusicPatch
    {
        private static FieldInfo _uiField = typeof(FacilityMusic).GetField("_musicUI", BindingFlags.NonPublic | BindingFlags.Instance);
        // 使用 Postfix 在点击后执行
        [HarmonyPrefix]
        static bool Prefix(FacilityMusic __instance)
        {
            SMTCStatus.IsSwitchingToBuiltIn = true;
            SMTCStatus.IsControlledByMod = false;
            SMTCImport.SMTC_Pause();
            MusicCoverMananger.RemoveCoverFromCenterIcons();
            ControlButtonHider.RestoreButtons();
            return true;
        }
    }
}
    
