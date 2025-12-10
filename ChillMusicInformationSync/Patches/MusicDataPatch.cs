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
        public static bool Prefix(ref string musicTitle, ref string artistName)
        {
            if (SMTCStatus.IsPlaying)
            {
                musicTitle = SMTCStatus.CurrentTitle;
                artistName = SMTCStatus.CurrentArtist;
                return true;
            }
            else
            {
                // 返回 true，允许原始方法继续执行。
                return true;
            }
        }
    }
}

