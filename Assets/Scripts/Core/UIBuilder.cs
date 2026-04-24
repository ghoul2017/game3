using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Core
{
    /// <summary>
    /// UI 控件静态工厂（门面） — 内部委托给 UIColors / UIFactory / UIPool
    /// 保持向后兼容，所有现有调用者无需修改。
    /// </summary>
    public static class UIBuilder
    {
        // ============ 配色转发 ============

        // 背景
        public static Color DarkBg       => UIColors.DarkBg;
        public static Color PanelBg      => UIColors.PanelBg;
        public static Color CardBg       => UIColors.CardBg;

        // 文字
        public static Color Gold         => UIColors.Gold;
        public static Color GoldDim      => UIColors.GoldDim;
        public static Color TextWhite    => UIColors.TextWhite;
        public static Color TextDim      => UIColors.TextDim;

        // 按钮
        public static Color ButtonNormal => UIColors.ButtonNormal;
        public static Color ButtonHover  => UIColors.ButtonHover;
        public static Color ButtonPress  => UIColors.ButtonPress;
        public static Color ButtonGold   => UIColors.ButtonGold;
        public static Color ButtonDisable => UIColors.ButtonDisable;

        // 边框
        public static Color BorderColor  => UIColors.BorderColor;
        public static Color BorderDim    => UIColors.BorderDim;

        // 功能色
        public static Color Red          => UIColors.Red;
        public static Color Green        => UIColors.Green;
        public static Color Blue         => UIColors.Blue;
        public static Color Purple       => UIColors.Purple;
        public static Color Orange       => UIColors.Orange;

        // 状态条
        public static Color HpColor      => UIColors.HpColor;
        public static Color MpColor      => UIColors.MpColor;
        public static Color ExpColor     => UIColors.ExpColor;

        // 房间颜色
        public static Color RoomCurrent  => UIColors.RoomCurrent;
        public static Color RoomCleared  => UIColors.RoomCleared;
        public static Color RoomMonster  => UIColors.RoomMonster;
        public static Color RoomElite    => UIColors.RoomElite;
        public static Color RoomBoss     => UIColors.RoomBoss;
        public static Color RoomTreasure => UIColors.RoomTreasure;
        public static Color RoomSpring   => UIColors.RoomSpring;
        public static Color RoomMerchant => UIColors.RoomMerchant;
        public static Color RoomExit     => UIColors.RoomExit;

        // 交替行色
        public static Color RowEven      => UIColors.RowEven;
        public static Color RowOdd       => UIColors.RowOdd;

        // 信息栏/标题栏/滑块/视口
        public static Color InfoBarBg    => UIColors.InfoBarBg;
        public static Color TitleBarBg   => UIColors.TitleBarBg;
        public static Color SliderBg     => UIColors.SliderBg;
        public static Color ViewportBg   => UIColors.ViewportBg;

        // ============ 控件工厂转发 ============

        public static GameObject CreateFullScreenBg(Transform parent, string name = "Bg")
            => UIFactory.CreateFullScreenBg(parent, name);

        public static GameObject CreatePanel(Transform parent, float w, float h, string name = "Panel")
            => UIFactory.CreatePanel(parent, w, h, name);

        public static GameObject CreateBorderedPanel(Transform parent, float w, float h, string name = "BorderedPanel")
            => UIFactory.CreateBorderedPanel(parent, w, h, name);

        public static Text CreateText(Transform parent, string content, int fontSize = 24, Color? color = null,
            TextAnchor alignment = TextAnchor.MiddleLeft, bool bold = false, string name = "Text")
            => UIFactory.CreateText(parent, content, fontSize, color, alignment, bold, name);

        public static Text CreateShadowText(Transform parent, string content, int fontSize = 24,
            Color? color = null, Color? shadowColor = null,
            TextAnchor alignment = TextAnchor.MiddleLeft, bool bold = false, string name = "ShadowText")
            => UIFactory.CreateShadowText(parent, content, fontSize, color, shadowColor, alignment, bold, name);

        public static Button CreateButton(Transform parent, string label, float w, float h,
            System.Action onClick = null, int fontSize = 22, string name = "Btn")
            => UIFactory.CreateButton(parent, label, w, h, onClick, fontSize, name);

        public static Button CreateGoldButton(Transform parent, string label, float w, float h,
            System.Action onClick = null, int fontSize = 24, string name = "GoldBtn")
            => UIFactory.CreateGoldButton(parent, label, w, h, onClick, fontSize, name);

        public static Slider CreateSlider(Transform parent, float w, float h, Color fillColor, string name = "Slider")
            => UIFactory.CreateSlider(parent, w, h, fillColor, name);

        public static GameObject CreateTitleBar(Transform parent, string title, string name = "TitleBar")
            => UIFactory.CreateTitleBar(parent, title, name);

        public static GameObject CreateDivider(Transform parent, string name = "Divider")
            => UIFactory.CreateDivider(parent, name);

        public static Slider CreateInteractiveSlider(Transform parent, float w, float h, Color fillColor, string name = "InteractiveSlider")
            => UIFactory.CreateInteractiveSlider(parent, w, h, fillColor, name);

        public static (Button btn, Text label) CreateButtonWithRef(Transform parent, string label, float w, float h,
            System.Action onClick = null, int fontSize = 22, string name = "Btn")
            => UIFactory.CreateButtonWithRef(parent, label, w, h, onClick, fontSize, name);

        // ============ 对象池转发 ============

        /// <summary>
        /// 对象池行结构体（转发到 UIPool.PoolRow）
        /// </summary>
        public struct PoolRow
        {
            public GameObject obj;
            public RectTransform rt;
            public Image bg;
            public Text nameText;
            public Button actionBtn;
            public Text actionBtnLabel;
            public Button secondBtn;
            public Text secondBtnLabel;
        }

        public static List<PoolRow> CreatePoolRows(Transform parent, int count, float rowHeight, float startY, float rowWidth)
        {
            var srcRows = UIPool.CreatePoolRows(parent, count, rowHeight, startY, rowWidth);
            var rows = new List<PoolRow>(srcRows.Count);
            for (int i = 0; i < srcRows.Count; i++)
            {
                var s = srcRows[i];
                rows.Add(new PoolRow
                {
                    obj = s.obj, rt = s.rt, bg = s.bg,
                    nameText = s.nameText,
                    actionBtn = s.actionBtn, actionBtnLabel = s.actionBtnLabel,
                    secondBtn = s.secondBtn, secondBtnLabel = s.secondBtnLabel
                });
            }
            return rows;
        }

        // ============ 布局辅助（保留在此） ============

        public static void SetStretch(GameObject obj)
        {
            var rt = obj.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void SetAnchored(GameObject obj, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var rt = obj.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
        }
    }
}
