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
        // BepInEx Log Source
        private static ManualLogSource _logger;

        // 存储找到的 FacilityMusic , MusicUI 实例
        private static FacilityMusic _facilityInstance;
        private static MusicUI _musicUIInstance;

        //  查找并存储 _mainState 字段的引用 (FieldInfo)
        private static readonly FieldInfo MainStateFieldInfo =
            AccessTools.Field(typeof(FacilityMusic), "_mainState");
        //  查找 MainState 枚举类型
        private static readonly Type MainStateType = AccessTools.Inner(typeof(FacilityMusic), "MainState");
        // 缓存 Playing 和 Pause 的枚举值对象
        // Enum.Parse 在运行时查找 "Playing" 和 "Pause" 的实际值
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
            }

            _logger?.LogInfo($"成功找到 MusicUI 实例：{_musicUIInstance.name} (GameObject)");
            if (SMTCStatus.IsControlledByMod)
            {
                SetButtonToPause();
                MusicCoverMananger.RefreshCoverInCenterIcons();
                ControlButtonHider.HideOriginalButtons();
                //SetGameMainState(true);
            }
            else
            {
                SetButtonToPlay();
                SetGameMainState(false);
            }
        }

        public static bool isMusicUISynced() { return _musicUIInstance != null; }


        public static void changeMusicTrigger(string musicTitle, string artistName, object changeKind)
        {
            if (_musicUIInstance == null)
            {
                _logger?.LogWarning("MusicUI 实例未找到。无法触发 UI 更新。");
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

        // ----------------------------------------------------
        // 按钮控制函数
        // ----------------------------------------------------

        public static void SetButtonToPlay()
        {
            if (_musicUIInstance == null)
            {
                _logger?.LogWarning("MusicUI 实例未找到。无法更改按钮状态。");
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

        /// <summary>
        /// 将 MusicUI 上的播放/暂停按钮设置为“暂停”图标（即当前为播放状态）。
        /// 对应 MusicUI.OnPlayMusic() (Token: 0x060004E3)
        /// </summary>
        public static void SetButtonToPause()
        {
            if (_musicUIInstance == null)
            {
                _logger?.LogWarning("MusicUI 实例未找到。无法更改按钮状态。");
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

            // 核心步骤：使用反射引用来设置私有字段的值
            MainStateFieldInfo.SetValue(_facilityInstance, newState);
        }
        public static void PauseGameMusic()
        {
            if (!_facilityInstance.IsPaused)
            {
                _facilityInstance.PauseMusic();
            }
        }
    }
}
