using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Reflection;

namespace ChillMusicInformationSync.UISync
{
    public static class ControlButtonHider
    {
        private static MusicUI _musicUI;
        private static bool _isButtonsHidden = false;

        public static void GetMusicUIInstance(MusicUI instance)
        {
            _musicUI = instance;
        }

        public static void HideOriginalButtons()
        {
            if (_musicUI == null || _isButtonsHidden) return;

            // 1. 获取私有引用 (基于你提供的 MusicUI 源码)
            Image loopImg = GetField<Image>("_loopChangeButtonImage");
            Image shuffleImg = GetField<Image>("_shuffleChangeButtonImage");
            // 注意：MusicAudioButton 对应的是 volumeUI 里的内容，或者是独立存在的
            // 我们通过实例的 transform 直接找更稳
            Transform interfaceTrans = _musicUI.transform.Find("Interface");
            if (interfaceTrans == null) return;

            Transform audioBtn = interfaceTrans.Find("MusicAudioButton");

            _isButtonsHidden = true;

            // 2. 执行安全隐藏：缩放 + 透明度
            // 我们不再改 anchoredPosition，因为会跟 LayoutGroup 冲突导致消失
            SafeHide(loopImg?.transform);
            SafeHide(shuffleImg?.transform);
            SafeHide(audioBtn);
        }

        private static void SafeHide(Transform trans)
        {
            if (trans == null) return;

            // 确保有 CanvasGroup
            CanvasGroup cg = trans.gameObject.GetComponent<CanvasGroup>() ?? trans.gameObject.AddComponent<CanvasGroup>();

            // 动画 1: 缩放归零 (Ease.InBack 会有一个先稍微变大再快速缩小的效果，很帅)
            DOTween.To(() => trans.localScale, x => trans.localScale = x, Vector3.zero, 0.4f)
                .SetEase(Ease.InBack);

            // 动画 2: 透明度归零
            DOTween.To(() => cg.alpha, x => cg.alpha = x, 0f, 0.3f);

            // 关键：为了不让这些按钮还占用布局空间，我们可以强制关闭它们的 LayoutElement（如果存在）
            LayoutElement le = trans.gameObject.GetComponent<LayoutElement>() ?? trans.gameObject.AddComponent<LayoutElement>();
            DOTween.To(() => le.preferredWidth, x => le.preferredWidth = x, 0f, 0.4f);
        }

        public static void RestoreButtons()
        {
            if (_musicUI == null || !_isButtonsHidden) return;

            Transform interfaceTrans = _musicUI.transform.Find("Interface");
            if (interfaceTrans == null) return;

            SafeRestore(GetField<Image>("_loopChangeButtonImage")?.transform);
            SafeRestore(GetField<Image>("_shuffleChangeButtonImage")?.transform);
            SafeRestore(interfaceTrans.Find("MusicAudioButton"));

            _isButtonsHidden = false;
        }

        private static void SafeRestore(Transform trans)
        {
            if (trans == null) return;
            CanvasGroup cg = trans.GetComponent<CanvasGroup>();

            // 恢复缩放
            DOTween.To(() => trans.localScale, x => trans.localScale = x, Vector3.one, 0.5f)
                .SetEase(Ease.OutBack);

            // 恢复透明度
            if (cg != null) DOTween.To(() => cg.alpha, x => cg.alpha = x, 1f, 0.3f);

            // 恢复布局宽度 (假设原始宽度是 40)
            LayoutElement le = trans.GetComponent<LayoutElement>();
            if (le != null) DOTween.To(() => le.preferredWidth, x => le.preferredWidth = x, 40f, 0.5f);
        }

        private static T GetField<T>(string fieldName) where T : class
        {
            FieldInfo field = typeof(MusicUI).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            return field?.GetValue(_musicUI) as T;
        }
    }
}