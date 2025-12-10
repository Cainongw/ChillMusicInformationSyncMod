using ChillMusicInformationSync.SMTC;
using HarmonyLib;
using System;


[HarmonyPatch(typeof(MusicService), "SetMusicProgress", new Type[] { typeof(float) })]
public static class SetMusicProgressPatch
{
    public static bool Prefix(float progress)
    {
        if (SMTCStatus.IsPlaying)
        {
            try
            {
                (TimeSpan currentPosition, TimeSpan duration) = SMTCImport.Instance.GetTimeline();

                if (duration.TotalSeconds > 0)
                {
                    // 使用 TotalSeconds 计算更直观，避免精度问题（TimeSpan.Ticks是100纳秒）
                    double newTotalSeconds = duration.TotalSeconds * progress;

                    // 将新的总秒数转换回 TimeSpan
                    TimeSpan newPosition = TimeSpan.FromSeconds(newTotalSeconds);

                    SMTCImport.SetTimelinePosition(newPosition);

                }
                return false;
            }
            catch (Exception ex)
            {
                return true;
            }
        }
        return true;
    }
}