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

    /// <summary>
    /// Nguồn máu của một enemy, đọc được từ HAI loại component máu đang có trong dự án:
    ///  1. ICharacterRuntime (CharacterRuntime / EnemyHealth kế thừa nó / BossHealth): Health, TotalHealth.
    ///  2. Component có trường currentHealth và maxHealth (bản EnemyHealth kiểu FSM): đọc bằng reflection.
    /// Nhờ vậy thanh máu chạy dù enemy dùng bản EnemyHealth nào.
    /// </summary>
    internal sealed class HealthSource
    {
        private const System.Reflection.BindingFlags Flags =
            System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.NonPublic;

        private readonly ICharacterRuntime _runtime;
        private readonly Component _component;
        private readonly System.Reflection.FieldInfo _current;
        private readonly System.Reflection.FieldInfo _max;

        public string Description { get; }

        private HealthSource(ICharacterRuntime runtime)
        {
            _runtime = runtime;
            Description = "ICharacterRuntime";
        }

        private HealthSource(Component component, System.Reflection.FieldInfo current, System.Reflection.FieldInfo max)
        {
            _component = component;
            _current = current;
            _max = max;
            Description = component.GetType().Name + ".currentHealth/maxHealth";
        }

        public static HealthSource Find(GameObject owner)
        {
            ICharacterRuntime runtime = owner.GetComponent<ICharacterRuntime>();
            if (runtime != null) return new HealthSource(runtime);

            foreach (MonoBehaviour behaviour in owner.GetComponents<MonoBehaviour>())
            {
                if (behaviour == null) continue;

                System.Type type = behaviour.GetType();
                System.Reflection.FieldInfo current = type.GetField("currentHealth", Flags);
                System.Reflection.FieldInfo max = type.GetField("maxHealth", Flags);

                if (current != null && max != null && IsNumber(current.FieldType) && IsNumber(max.FieldType))
                    return new HealthSource(behaviour, current, max);
            }

            return null;
        }

        public float Current => _runtime != null
            ? _runtime.Health
            : System.Convert.ToSingle(_current.GetValue(_component));

        public float Max => _runtime != null
            ? _runtime.TotalHealth
            : System.Convert.ToSingle(_max.GetValue(_component));

        public bool Alive => Current > 0f;

        public float Ratio
        {
            get
            {
                float max = Max;
                return max > 0f ? Mathf.Clamp01(Current / max) : 0f;
            }
        }

        private static bool IsNumber(System.Type type)
            => type == typeof(float) || type == typeof(int) || type == typeof(double);
    }
}
