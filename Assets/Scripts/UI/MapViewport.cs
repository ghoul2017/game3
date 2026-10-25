using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class MapViewport : MonoBehaviour, IScrollHandler, IBeginDragHandler, IDragHandler
    {
        public RectTransform Content
        {
            get; private set;
        }
        public float Zoom => Content.localScale.x;
        private RectTransform viewport;

        public void Initialize(RectTransform content)
        {
            viewport = GetComponent<RectTransform>();
            Content = content;
            gameObject.AddComponent<RectMask2D>();
            Image surface = gameObject.AddComponent<Image>();
            surface.color = SlateUI.Hex(0x080e13);
            surface.raycastTarget = true;
        }

        public void ShowAll()
        {
            float scale = Mathf.Min(viewport.rect.width / Content.rect.width, viewport.rect.height / Content.rect.height);
            Content.localScale = Vector3.one * scale;
            Content.anchoredPosition = new Vector2((viewport.rect.width - Content.rect.width * scale) / 2, -(viewport.rect.height - Content.rect.height * scale) / 2);
        }

        public void Focus(RectTransform target)
        {
            Vector2 center = Content.InverseTransformPoint(target.TransformPoint(target.rect.center));
            Content.anchoredPosition = new Vector2(viewport.rect.width / 2, -viewport.rect.height / 2) - center * Zoom;
            Clamp();
        }

        public void OnScroll(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, eventData.position, eventData.pressEventCamera, out Vector2 point);
            float previous = Zoom;
            float fit = Mathf.Min(viewport.rect.width / Content.rect.width, viewport.rect.height / Content.rect.height);
            float next = Mathf.Clamp(previous * Mathf.Pow(1.15f, eventData.scrollDelta.y), Mathf.Min(fit, 0.5f), 1.8f);
            Content.anchoredPosition = point - (point - Content.anchoredPosition) * (next / previous);
            Content.localScale = Vector3.one * next;
            Clamp();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Middle && eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, eventData.position, eventData.pressEventCamera, out Vector2 current);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, eventData.position - eventData.delta, eventData.pressEventCamera, out Vector2 previous);
            eventData.eligibleForClick = false;
            Content.anchoredPosition += current - previous;
            Clamp();
        }

        public void Pan(Vector2 delta)
        {
            Content.anchoredPosition += delta;
            Clamp();
        }

        private void Clamp()
        {
            Vector2 size = Content.rect.size * Zoom;
            Vector2 position = Content.anchoredPosition;
            position.x = size.x <= viewport.rect.width ? (viewport.rect.width - size.x) / 2 : Mathf.Clamp(position.x, viewport.rect.width - size.x, 0);
            position.y = size.y <= viewport.rect.height ? -(viewport.rect.height - size.y) / 2 : Mathf.Clamp(position.y, 0, size.y - viewport.rect.height);
            Content.anchoredPosition = position;
        }
    }

    public sealed class HoverInfo : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public RectTransform host;
        public string text;
        private RectTransform tip;

        public static RectTransform Show(RectTransform host, string text, Vector2 screenPosition)
        {
            Canvas canvas = host.GetComponentInParent<Canvas>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(host, screenPosition, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out Vector2 local);
            RectTransform box = SlateUI.Box(host, "悬停信息", 0, 0, 310, 120, SlateUI.Hex(0x26383f)).rectTransform;
            Text label = SlateUI.Label(box, text, 14, 12, 282, 500, 17);
            float height = Mathf.Clamp(label.preferredHeight + 24, 65, 300);
            label.rectTransform.sizeDelta = new Vector2(282, height - 24);
            box.sizeDelta = new Vector2(310, height);
            float x = local.x - host.rect.xMin + 18;
            float y = host.rect.yMax - local.y + 18;
            box.anchoredPosition = new Vector2(Mathf.Clamp(x, 8, host.rect.width - 318), -Mathf.Clamp(y, 8, host.rect.height - height - 8));
            return box;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Clear();
            tip = Show(host, text, eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData) => Clear();
        private void OnDisable() => Clear();
        private void Clear()
        {
            if (tip != null)
            {
                tip.gameObject.SetActive(false);
                Destroy(tip.gameObject);
            }
        }
    }
}
