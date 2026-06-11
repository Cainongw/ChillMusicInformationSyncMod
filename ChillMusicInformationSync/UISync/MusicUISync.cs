using BepInEx.Logging;
using Bulbul;
using HarmonyLib;
using System;
using System.Reflection;
using ChillMusicInformationSync.SMTC;


namespace ChillMusicInformationSync.UISync
{
    public static class MusicUISync
    {
        private static ManualLogSource _logger;

        private static FacilityMusic _facilityInstance;
        private static MusicUI _musicUIInstance;

        private static readonly FieldInfo MainStateFieldInfo =
            AccessTools.Field(typeof(FacilityMusic), "_mainState");
        private static readonly Type MainStateType = AccessTools.Inner(typeof(FacilityMusic), "MainState");
        private static readonly object MainStatePlaying = MainStateType != null ? Enum.Parse(MainStateType, "Playing") : null;
        private static readonly object MainStatePause = MainStateType != null ? Enum.Parse(MainStateType, "Pause") : null;

        public static void Initialize_logger(ManualLogSource external_logger)
        {
            _logger = external_logger;
        }

        public static void GetInstance(FacilityMusic facilityInstance, MusicUI instance)
        {
            _facilityInstance = facilityInstance;
            _musicUIInstance = instance;

            if (_musicUIInstance == null)
            {
                _logger?.LogError("未能在场景中找到 MusicUI 实例。UI同步功能将无法使用。");
                return;
            }

            _logger?.LogInfo($"成功找到 MusicUI 实例：{_musicUIInstance.name} (GameObject)");
            if (!IsMusicUIActiveInHierarchy()) return;

            if (SMTCStatus.IsControlledByMod)
            {
                SetButtonToPause();
                MusicCoverMananger.RefreshCoverInCenterIcons();
                ControlButtonHider.HideOriginalButtons();
            }
            else
            {
                SetButtonToPlay();
                SetGameMainState(false);
            }
        }

        public static bool isMusicUISynced() { return _musicUIInstance != null; }

        public static bool IsMusicUIActiveInHierarchy()
        {
            return _musicUIInstance != null && _musicUIInstance.gameObject.activeInHierarchy;
        }

        public static void changeMusicTrigger(string musicTitle, string artistName, object changeKind)
        {
            if (!IsMusicUIActiveInHierarchy())
            {
                return;
            }

            try
            {
                MethodInfo onChangeMusic = typeof(MusicUI).GetMethod(
                    "OnChangeMusic",
                    BindingFlags.Public | BindingFlags.Instance
                );

                if (onChangeMusic != null)
                {
                    onChangeMusic.Invoke(
                        _musicUIInstance,
                        new object[] { musicTitle, artistName, changeKind }
                    );

                    _logger?.LogInfo($"UI 更新触发成功: 歌曲='{musicTitle}', 歌手='{artistName}'");
                }
                else
                {
                    _logger?.LogError("未找到 OnChangeMusic 方法，请检查方法签名或名称是否正确。");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"触发 UI 更新时发生错误: {ex.Message}");
            }
        }

        public static void SetButtonToPlay()
        {
            if (!IsMusicUIActiveInHierarchy())
            {
                return;
            }
            try
            {
                _musicUIInstance.OnPauseMusic();
                _logger?.LogInfo("音乐播放按钮状态已设置为 '播放' (当前暂停)。");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"调用 OnPauseMusic 时发生错误: {ex.Message}");
            }
        }

        public static void SetButtonToPause()
        {
            if (!IsMusicUIActiveInHierarchy())
            {
                return;
            }
            try
            {
                _musicUIInstance.OnPlayMusic();
                _logger?.LogInfo("音乐播放按钮状态已设置为 '暂停' (当前播放)。");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"调用 OnPlayMusic 时发生错误: {ex.Message}");
            }
        }
        public static void SetGameMainState(bool isPlaying)
        {
            if (_facilityInstance == null || MainStateFieldInfo == null)
            {
                return;
            }

            object newState = isPlaying ? MainStatePlaying : MainStatePause;

            MainStateFieldInfo.SetValue(_facilityInstance, newState);
        }
        public static void PauseGameMusic()
        {
            if (_facilityInstance == null) return;
            if (!_facilityInstance.IsPaused)
            {
                _facilityInstance.PauseMusic();
            }
        }
    }
}
