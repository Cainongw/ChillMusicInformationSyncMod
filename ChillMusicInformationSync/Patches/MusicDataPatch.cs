using HarmonyLib;
using System;
using Bulbul;
using ChillMusicInformationSync.SMTC;

namespace ChillMusicInformationSync.Patch
{

    [HarmonyPatch(typeof(MusicUI), "OnChangeMusic", new Type[] { typeof(string), typeof(string), typeof(MusicChangeKind) })]
    public static class DynamicSMTCUISyncPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(MusicUI __instance, ref string musicTitle, ref string artistName)
        {
            if (!__instance.isActiveAndEnabled || !__instance.gameObject.activeInHierarchy)
                return true;

            if (SMTCStatus.IsControlledByMod)
            {
                musicTitle = SMTCStatus.CurrentTitle;
                artistName = SMTCStatus.CurrentArtist;
            }
            return true;
        }
    }
}

