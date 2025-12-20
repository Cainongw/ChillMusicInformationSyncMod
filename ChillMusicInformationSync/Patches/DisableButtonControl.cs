using Bulbul;
using ChillMusicInformationSync.SMTC;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChillMusicInformationSync.Patches
{
    // 按钮已经被隐藏，这里先注释掉保留着（
    /*
    // 拦截循环播放按钮的点击
    [HarmonyPatch(typeof(FacilityMusic), "OnClickButtonChangeLoop")]
    public static class DisableLoopButtonPatch
    {
        public static bool Prefix()
        {
            if (SMTCStatus.IsPlaying)
            {
                // 如果 SMTC 正在播放，阻止原方法执行
                return false;
            }
            return true;
        }
    }

    // 拦截随机播放按钮的点击
    [HarmonyPatch(typeof(FacilityMusic), "OnClickButtonShuffleChange")]
    public static class DisableShuffleButtonPatch
    {
        public static bool Prefix()
        {
            if (SMTCStatus.IsPlaying)
            {
                // 如果 SMTC 正在播放，阻止原方法执行
                return false;
            }
            return true;
        }
    */
}
