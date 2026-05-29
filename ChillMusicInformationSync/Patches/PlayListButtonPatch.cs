using Bulbul;
using ChillMusicInformationSync.SMTC;
using ChillMusicInformationSync.UISync;
using HarmonyLib;


namespace ChillMusicInformationSync.Patches
{
    [HarmonyPatch(typeof(FacilityMusic), "OnClickButtonPlayListPlayMusicButton")]
    public class FacilityMusicPatch
    {
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
    
