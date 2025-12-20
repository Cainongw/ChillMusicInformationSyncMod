using UnityEngine;
using UnityEngine.UI;
using ChillMusicInformationSync.SMTC;

namespace ChillMusicInformationSync.UISync
{
    public static class MusicCoverMananger
    {
        private static Vector2 _originalPos = Vector2.zero; // 记录原始位置

        public static void RefreshCoverInCenterIcons()
        {
            byte[] coverData = SMTCImport.Instance.GetCoverImage();
            if (coverData == null || coverData.Length == 0) return;

            GameObject centerIcons = GameObject.Find("CenterIcons");
            if (!centerIcons) return;

            RectTransform parentRect = centerIcons.GetComponent<RectTransform>();

            // 第一次运行，记录初始坐标，方便以后还原
            if (_originalPos == Vector2.zero) _originalPos = parentRect.anchoredPosition;

            Transform coverTransform = centerIcons.transform.Find("Mod_SongCover");
            GameObject coverObj;
            Image img;

            if (coverTransform == null)
            {
                coverObj = new GameObject("Mod_SongCover");
                coverObj.transform.SetParent(centerIcons.transform, false);
                img = coverObj.AddComponent<Image>();
                coverObj.transform.SetAsLastSibling();

                float coverSize = 80f; // 封面宽度
                float spacing = 20f;   // 封面和按钮之间的间距
                coverObj.GetComponent<RectTransform>().sizeDelta = new Vector2(coverSize, coverSize);

                // 移动的距离大约等于：封面宽度 + 间距
                parentRect.anchoredPosition = new Vector2(_originalPos.x - (coverSize + spacing), _originalPos.y);
            }
            else
            {
                coverObj = coverTransform.gameObject;
                img = coverObj.GetComponent<Image>();
                if (img.sprite != null)
                {
                    if (img.sprite.texture != null) Object.Destroy(img.sprite.texture);
                    Object.Destroy(img.sprite);
                }
            }

            // 加载图片
            Texture2D tex = new Texture2D(2, 2);
            if (tex.LoadImage(coverData))
            {
                img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                img.preserveAspect = true;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        }

        // 删除函数：用于隐藏或停止播放时调用
        public static void RemoveCoverFromCenterIcons()
        {
            GameObject centerIcons = GameObject.Find("CenterIcons");
            if (!centerIcons) return;

            Transform coverTransform = centerIcons.transform.Find("Mod_SongCover");
            if (coverTransform != null)
            {
                Object.Destroy(coverTransform.gameObject);

                // --- 核心逻辑：还原位置 ---
                centerIcons.GetComponent<RectTransform>().anchoredPosition = _originalPos;

                LayoutRebuilder.ForceRebuildLayoutImmediate(centerIcons.GetComponent<RectTransform>());
            }
        }
    }
}
