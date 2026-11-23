using Bulbul;
using ChillMusicInformationSync.SMTC;
using ChillMusicInformationSync.UISync;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;


namespace ChillMusicInformationSync.Patches
{
    [HarmonyPatch(typeof(FacilityMusic), "OnClickButtonPlayListPlayMusicButton")]
    public class FacilityMusicPatch
    {
        // 使用 Postfix 在点击后执行
        [HarmonyPrefix]
        static bool Prefix(FacilityMusic __instance)
        {
            MusicUISync.SwitchToBuiltInState();
            return true;
        }
    }
    internal class PlaylistButtonRestoreHook : MonoBehaviour
    {
        private FacilityMusic _facilityMusic;
        private GameAudioInfo _audioInfo;
        private bool _listenerAdded;

        internal void Configure(FacilityMusic facilityMusic, GameAudioInfo audioInfo, Button playButton)
        {
            _facilityMusic = facilityMusic;
            _audioInfo = audioInfo;
            if (!_listenerAdded && playButton != null)
            {
                _listenerAdded = true;
                playButton.onClick.AddListener(OnBeforePlayListClick);
            }
        }

        private void OnBeforePlayListClick()
        {
            if (!SMTCStatus.IsControlledByMod || _facilityMusic == null)
            {
                return;
            }

            if (_facilityMusic.PlayingMusic == _audioInfo)
            {
                MusicUISync.SwitchToBuiltInState();
                if (_audioInfo != null)
                {
                    MusicUISync.changeMusicTrigger(_audioInfo.Title, _audioInfo.Credit, MusicChangeKind.Manual);
                }
                if (!_facilityMusic.IsPaused)
                {
                    MusicUISync.SetGameMainState(false);
                }
            }
        }
    }

    [HarmonyPatch(typeof(MusicPlayListButtons), "Setup")]
    public static class MusicPlayListButtonsSetupPatch
    {
        private static readonly FieldInfo PlayMusicButtonField =
            AccessTools.Field(typeof(MusicPlayListButtons), "_playMusicbutton");

        [HarmonyPrefix]
        public static void Prefix(MusicPlayListButtons __instance, GameAudioInfo audioInfo, FacilityMusic facilityMusic)
        {
            var playButton = PlayMusicButtonField?.GetValue(__instance) as Button;
            var hook = __instance.GetComponent<PlaylistButtonRestoreHook>();
            if (hook == null)
            {
                hook = __instance.gameObject.AddComponent<PlaylistButtonRestoreHook>();
            }
            hook.Configure(facilityMusic, audioInfo, playButton);
        }
    }
}
    
