using ChillMusicInformationSync.SMTC;
using HarmonyLib;


namespace ChillMusicInformationSync.Patches
{
    [HarmonyPatch(typeof(MusicService), "GetCurrentMusicProgress")]
    public static class MusicServiceProgressPatch
    {
        public static bool Prefix(ref float __result)
        {
            if (SMTCStatus.IsPlaying)
            {
                if (SMTCStatus.CurrentProgress >= 0f)
                {
                    __result = SMTCStatus.CurrentProgress;
                    return false;
                }
                else
                {
                    return true;

                }
            }
            return true;
        }
    }
}

