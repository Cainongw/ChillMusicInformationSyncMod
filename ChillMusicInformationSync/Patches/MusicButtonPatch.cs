using Bulbul;
using ChillMusicInformationSync.SMTC;
using HarmonyLib;


namespace ChillMusicInformationSync.Patches
{
    [HarmonyPatch(typeof(FacilityMusic), "PauseMusic")]
    public static class FacilityPausePatch
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            if (SMTCStatus.IsPlaying)
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
            if (SMTCStatus.IsPlaying)
            {
                SMTCImport.SMTC_Play();
                return false;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(FacilityMusic), "OnClickButtonSkip")]
    public static class OnNextMusicPatch
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            if (SMTCStatus.IsPlaying)
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
            if (SMTCStatus.IsPlaying)
            {
                SMTCImport.SMTC_Previous();
                return false;
            }
            return true;
        }
    }
}
        



