using UnityEngine;
using UnityEngine.UI;

namespace Mhieu.Enemy
{
    /// <summary>Hàm dựng UI bằng code cho các thanh máu (không cần prefab, không cần sprite).</summary>
    internal static class HealthBarUtil
    {
        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        public static Image CreateImage(string name, Transform parent, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            Stretch(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Đặt độ đầy của một Image nằm trong khung cha: neo trái, rộng theo tỉ lệ 0..1.</summary>
        public static void SetFill(Image image, float ratio)
        {
            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static Text CreateText(string name, Transform parent, int size, TextAnchor alignment, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            Stretch(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        public static float Ratio(ICharacterRuntime runtime)
        {
            int total = runtime.TotalHealth;
            return total > 0 ? Mathf.Clamp01((float)runtime.Health / total) : 0f;
        }
    }
}
