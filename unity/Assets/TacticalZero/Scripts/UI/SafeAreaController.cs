using UnityEngine;

namespace TacticalZero
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaController : MonoBehaviour
    {
        private RectTransform rect;
        private Rect prior;
        private int width, height;
        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            if (prior != Screen.safeArea || width != Screen.width || height != Screen.height)
                Apply();
        }

        private void Apply()
        {
            prior = Screen.safeArea;
            width = Screen.width;
            height = Screen.height;
            if (width <= 0 || height <= 0)
                return;
            rect.anchorMin = new Vector2(prior.xMin / width, prior.yMin / height);
            rect.anchorMax = new Vector2(prior.xMax / width, prior.yMax / height);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
