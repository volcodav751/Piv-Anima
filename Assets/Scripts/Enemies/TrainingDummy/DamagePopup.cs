using TMPro;
using UnityEngine;

namespace Game.Combat
{
    public sealed class DamagePopup : MonoBehaviour
    {
        private TextMeshPro label;
        private float lifetime;
        private float elapsed;
        private Color initialColor;

        public static void Show(int amount, Vector3 position, Color color,
            float duration, float fontSize)
        {
            var popupObject = new GameObject("DamagePopup");
            popupObject.transform.position = position;

            var text = popupObject.AddComponent<TextMeshPro>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = amount.ToString();
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.rectTransform.sizeDelta = new Vector2(4f, 1f);
            text.GetComponent<MeshRenderer>().sortingLayerName = "Effects";
            text.GetComponent<MeshRenderer>().sortingOrder = 100;

            var popup = popupObject.AddComponent<DamagePopup>();
            popup.label = text;
            popup.initialColor = color;
            popup.lifetime = Mathf.Max(0.1f, duration);
            Destroy(popupObject, popup.lifetime);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            transform.position += Vector3.up * (0.8f * Time.deltaTime);
            Color color = initialColor;
            color.a *= 1f - Mathf.Clamp01(elapsed / lifetime);
            label.color = color;
        }
    }
}
