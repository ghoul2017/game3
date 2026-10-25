using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.UI
{
    public static class SlateUI
    {
        public static readonly Color Background = Hex(0x10191f);
        public static readonly Color Panel = Hex(0x19252c);
        public static readonly Color Card = Hex(0x213139);
        public static readonly Color Border = Hex(0x394a50);
        public static readonly Color Ink = Hex(0xe6e2d5);
        public static readonly Color Muted = Hex(0xa5b0ae);
        public static readonly Color Gold = Hex(0xd7b576);
        public static readonly Color Teal = Hex(0x7cc5b2);
        public static readonly Color Red = Hex(0xd9897c);
        public static readonly Color Blue = Hex(0x83b3ce);
        private static Font font;

        public static Color Hex(int value, float alpha = 1f) => new Color(((value >> 16) & 255) / 255f, ((value >> 8) & 255) / 255f, (value & 255) / 255f, alpha);

        public static Font Font
        {
            get
            {
                if (font == null)
                {
                    font = UnityEngine.Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Microsoft YaHei UI", "Noto Sans CJK SC", "Arial" }, 22);
                }
                return font;
            }
        }

        public static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        public static Image Box(Transform parent, string name, float x, float y, float width, float height, Color color)
        {
            Image image = Rect(parent, name, x, y, width, height).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static RectTransform CardBox(Transform parent, string name, float x, float y, float width, float height, Color? color = null)
        {
            Image box = Box(parent, name, x, y, width, height, color ?? Panel);
            Box(box.transform, "上边线", 0, 0, width, 1, Border);
            return box.rectTransform;
        }

        public static Text Label(Transform parent, string text, float x, float y, float width, float height, int size = 20, Color? color = null, TextAnchor anchor = TextAnchor.UpperLeft)
        {
            Text label = Rect(parent, "文字", x, y, width, height).gameObject.AddComponent<Text>();
            label.font = Font;
            label.text = text;
            label.fontSize = size;
            label.color = color ?? Ink;
            label.alignment = anchor;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.supportRichText = false;
            return label;
        }

        public static Button Button(Transform parent, string name, string label, float x, float y, float width, float height, UnityAction action, bool enabled = true, bool primary = false)
        {
            Image box = Box(parent, name, x, y, width, height, primary ? Hex(0x31584f) : Card);
            box.raycastTarget = true;
            Button button = box.gameObject.AddComponent<Button>();
            button.targetGraphic = box;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Hex(0xc8ded7);
            colors.pressedColor = Hex(0x90b4a9);
            colors.selectedColor = Color.white;
            colors.disabledColor = Hex(0x677477, 0.55f);
            button.colors = colors;
            button.interactable = enabled;
            button.onClick.AddListener(() =>
            {
                if (Game.Core.AudioManager.Instance != null)
                {
                    Game.Core.AudioManager.Instance.Click();
                }
            });
            button.onClick.AddListener(action);
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            Label(box.transform, label, 10, 0, width - 20, height, 18, enabled ? (primary ? Teal : Ink) : Muted, TextAnchor.MiddleCenter);
            return button;
        }

        public static void Bar(Transform parent, string name, int current, int maximum, float x, float y, float width, Color color)
        {
            Box(parent, name, x, y, width, 8, Background);
            Box(parent, $"{name}数值", x, y, width * Mathf.Clamp01(current / (float)Math.Max(1, maximum)), 8, color);
        }

        public static RectTransform Scroll(Transform parent, string name, float x, float y, float width, float height, float contentHeight)
        {
            RectTransform root = Rect(parent, name, x, y, width, height);
            ScrollRect scroll = root.gameObject.AddComponent<ScrollRect>();
            Image image = root.gameObject.AddComponent<Image>();
            image.color = Panel;
            root.gameObject.AddComponent<RectMask2D>();
            RectTransform content = Rect(root, "内容", 0, 0, width - 24, Mathf.Max(height, contentHeight));
            scroll.viewport = root;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 35;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            Image track = Box(root, "滚动条", width - 7, 0, 6, height, Background);
            Image handle = Box(track.transform, "滚动块", 0, 0, 6, height, Border);
            handle.rectTransform.anchorMin = Vector2.zero;
            handle.rectTransform.anchorMax = Vector2.one;
            handle.rectTransform.sizeDelta = Vector2.zero;
            handle.raycastTarget = true;
            Scrollbar scrollbar = track.gameObject.AddComponent<Scrollbar>();
            track.raycastTarget = true;
            scrollbar.handleRect = handle.rectTransform;
            scrollbar.targetGraphic = handle;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            return content;
        }

        public static string Bonus(Stats stats)
        {
            string result = "";
            if (stats.hp != 0)
            {
                result += $"生命+{stats.hp}  ";
            }
            if (stats.mp != 0)
            {
                result += $"法力+{stats.mp}  ";
            }
            if (stats.attack != 0)
            {
                result += $"物攻+{stats.attack}  ";
            }
            if (stats.magic != 0)
            {
                result += $"法攻+{stats.magic}  ";
            }
            if (stats.defense != 0)
            {
                result += $"防御+{stats.defense}  ";
            }
            if (stats.speed != 0)
            {
                result += $"速度+{stats.speed}  ";
            }
            return result.Trim();
        }
    }
}
