using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; 
using ChillMusicInformationSync.SMTC;

namespace ChillMusicInformationSync.UISync
{
    public static class MusicCoverMananger
    {
        private static Vector2 _originalPos = Vector2.zero;
        private static GameObject _currentCover;
        private static bool? _chillPatcherDetected;
        private static Sprite _injectedSprite;

        private const float AnimDuration = 0.6f;
        private const float CoverSize = 80f;
        private const float Spacing = 20f;

        private static bool IsChillPatcherPresent()
        {
            if (_chillPatcherDetected == null)
                _chillPatcherDetected = Type.GetType(
                    "ChillPatcher.Patches.UIFramework.MusicUI_AlbumArt_Patch, ChillPatcher",
                    false) != null;
            return _chillPatcherDetected.Value;
        }

        private static void InjectCoverToButton(byte[] data)
        {
            var btn = GameObject.Find("IconMusicPlaylist_Button");
            if (btn == null) return;

            var deactive = btn.transform.Find("IconDeactivemage")?.GetComponent<Image>();
            var active = btn.transform.Find("IconActiveImage")?.GetComponent<Image>();
            if (deactive == null || active == null) return;

            if (_injectedSprite != null)
            {
                UnityEngine.Object.Destroy(_injectedSprite.texture);
                UnityEngine.Object.Destroy(_injectedSprite);
                _injectedSprite = null;
            }

            var tex = new Texture2D(2, 2);
            if (!ImageConversion.LoadImage(tex, data)) return;
            _injectedSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));

            deactive.sprite = _injectedSprite;
            active.sprite = _injectedSprite;
        }

        public static void RefreshCoverInCenterIcons()
        {
            byte[] coverData = SMTCImport.Instance.GetCoverImage();
            if (coverData == null || coverData.Length == 0) return;

            // ChillPatcher 共存模式：直接注入封面到按钮图标
            if (IsChillPatcherPresent())
            {
                InjectCoverToButton(coverData);
                return;
            }

            GameObject centerIcons = GameObject.Find("CenterIcons");
            GameObject playlistBtn = GameObject.Find("IconMusicPlaylist_Button");
            if (!centerIcons || !centerIcons.activeInHierarchy || !playlistBtn) return;

            RectTransform centerRect = centerIcons.GetComponent<RectTransform>();
            if (_originalPos == Vector2.zero) _originalPos = centerRect.anchoredPosition;

            if (_currentCover == null)
            {
                // 1. 创建封面 GameObject
                _currentCover = new GameObject("Mod_SongCover");
                _currentCover.transform.SetParent(centerIcons.transform, false);
                _currentCover.transform.SetAsLastSibling();

                Image img = _currentCover.AddComponent<Image>();
                CanvasGroup cg = _currentCover.AddComponent<CanvasGroup>();
                RectTransform coverRect = _currentCover.GetComponent<RectTransform>();

                coverRect.sizeDelta = new Vector2(CoverSize, CoverSize);
                cg.alpha = 0;
                coverRect.localScale = Vector3.zero;

                // 2. 计算目标位置
                Vector2 targetCenterPos = new Vector2(_originalPos.x - (CoverSize + Spacing), _originalPos.y);

                // --- 使用 DOTween.To 替代扩展方法，解决报错问题 ---

                // 推动 CenterIcons 往左移 (等同于 DOAnchorPos)
                DOTween.To(() => centerRect.anchoredPosition, x => centerRect.anchoredPosition = x, targetCenterPos, AnimDuration)
                    .SetEase(Ease.OutQuint);

                // 封面淡入 (等同于 DOFade)
                DOTween.To(() => cg.alpha, x => cg.alpha = x, 1f, AnimDuration);

                // 封面弹出缩放 (等同于 DOScale)
                DOTween.To(() => coverRect.localScale, x => coverRect.localScale = x, Vector3.one, AnimDuration)
                    .SetEase(Ease.OutBack);
            }

            UpdateImageSprite(_currentCover.GetComponent<Image>(), coverData);
        }

        public static void RemoveCoverFromCenterIcons()
        {
            // ChillPatcher 共存模式：清理注入的封面 Sprite
            if (IsChillPatcherPresent())
            {
                if (_injectedSprite != null)
                {
                    UnityEngine.Object.Destroy(_injectedSprite.texture);
                    UnityEngine.Object.Destroy(_injectedSprite);
                    _injectedSprite = null;
                }
                return;
            }

            GameObject centerIcons = GameObject.Find("CenterIcons");
            if (!centerIcons || _currentCover == null) return;

            RectTransform centerRect = centerIcons.GetComponent<RectTransform>();
            CanvasGroup cg = _currentCover.GetComponent<CanvasGroup>();
            RectTransform coverRect = _currentCover.GetComponent<RectTransform>();

            // 消失动画
            DOTween.To(() => coverRect.localScale, x => coverRect.localScale = x, Vector3.zero, 0.3f).SetEase(Ease.InBack);
            DOTween.To(() => cg.alpha, x => cg.alpha = x, 0f, 0.3f).OnComplete(() => {
                UnityEngine.Object.Destroy(_currentCover);
                _currentCover = null;
            });

            // 面板弹回
            DOTween.To(() => centerRect.anchoredPosition, x => centerRect.anchoredPosition = x, _originalPos, 0.5f).SetEase(Ease.OutQuad);
        }

        private static void UpdateImageSprite(Image img, byte[] data)
        {
            if (img.sprite != null)
            {
                UnityEngine.Object.Destroy(img.sprite.texture);
                UnityEngine.Object.Destroy(img.sprite);
            }
            Texture2D tex = new Texture2D(2, 2);
            if (tex.LoadImage(data))
            {
                img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                img.preserveAspect = true;
            }
        }
    }
}