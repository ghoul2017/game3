using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// UI 配色常量 — 深色主题：黑底(#0d0d1a) + 暗金强调(#c8a24c)
    /// </summary>
    public static class UIColors
    {
        // 背景
        public static readonly Color DarkBg       = new Color(0.051f, 0.051f, 0.102f, 0.97f);   // 极深黑蓝 #0d0d1a
        public static readonly Color PanelBg      = new Color(0.102f, 0.102f, 0.180f, 0.96f);   // 面板底色 #1a1a2e
        public static readonly Color CardBg       = new Color(0.133f, 0.133f, 0.267f, 0.95f);   // 卡片背景 #222244

        // 文字
        public static readonly Color Gold         = new Color(0.784f, 0.635f, 0.298f);           // 暗金标题 #c8a24c
        public static readonly Color GoldDim      = new Color(0.627f, 0.510f, 0.239f);           // 暗金(暗)
        public static readonly Color TextWhite    = new Color(0.878f, 0.847f, 0.753f);           // 正文色 #e0d8c0
        public static readonly Color TextDim      = new Color(0.588f, 0.529f, 0.475f);           // 次要文字 #958678 (对比度>=4.5:1)

        // 按钮
        public static readonly Color ButtonNormal = new Color(0.176f, 0.176f, 0.333f, 0.95f);   // 按钮正常 #2d2d55
        public static readonly Color ButtonHover  = new Color(0.239f, 0.239f, 0.416f, 0.95f);   // 按钮悬停 #3d3d6a
        public static readonly Color ButtonPress  = new Color(0.102f, 0.102f, 0.200f, 0.95f);   // 按下更深
        public static readonly Color ButtonGold   = new Color(0.235f, 0.180f, 0.078f, 0.95f);   // 金色按钮(重要操作)
        public static readonly Color ButtonDisable = new Color(0.102f, 0.102f, 0.200f, 0.95f);  // 禁用按钮 #1a1a33

        // 边框
        public static readonly Color BorderColor  = new Color(0.392f, 0.318f, 0.149f, 0.70f);   // 暗金边框
        public static readonly Color BorderDim    = new Color(0.235f, 0.191f, 0.089f, 0.55f);   // 暗淡边框

        // 功能色（高饱和度，适配深色背景）
        public static readonly Color Red          = new Color(0.906f, 0.298f, 0.235f);           // 危险红 #e74c3c
        public static readonly Color Green        = new Color(0.345f, 0.839f, 0.553f);           // 治愈绿 #58d68d
        public static readonly Color Blue         = new Color(0.365f, 0.678f, 0.886f);           // 法力蓝 #5dade2
        public static readonly Color Purple       = new Color(0.686f, 0.478f, 0.773f);           // 魔法紫 #af7ac5
        public static readonly Color Orange       = new Color(0.941f, 0.753f, 0.251f);           // 金色图标 #f0c040

        // 状态条
        public static readonly Color HpColor      = new Color(0.906f, 0.298f, 0.235f);           // 生命红 (同Red)
        public static readonly Color MpColor      = new Color(0.365f, 0.678f, 0.886f);           // 法力蓝 (同Blue)
        public static readonly Color ExpColor     = new Color(0.941f, 0.753f, 0.251f);           // 经验金 (同Orange)

        // 房间颜色
        public static readonly Color RoomCurrent  = new Color(0.365f, 0.678f, 0.886f, 0.90f);   // 当前房间(蓝)
        public static readonly Color RoomCleared  = new Color(0.133f, 0.133f, 0.267f, 0.50f);   // 已清房间
        public static readonly Color RoomMonster  = new Color(0.906f, 0.298f, 0.235f, 0.85f);   // 怪物(红)
        public static readonly Color RoomElite    = new Color(0.686f, 0.478f, 0.773f, 0.85f);   // 精英(紫)
        public static readonly Color RoomBoss     = new Color(0.950f, 0.150f, 0.150f, 0.92f);   // BOSS(亮红)
        public static readonly Color RoomTreasure = new Color(0.941f, 0.753f, 0.251f, 0.85f);   // 宝箱(金)
        public static readonly Color RoomSpring   = new Color(0.345f, 0.839f, 0.553f, 0.85f);   // 泉水(绿)
        public static readonly Color RoomMerchant = new Color(0.750f, 0.550f, 0.150f, 0.85f);   // 商人(橙)
        public static readonly Color RoomExit     = new Color(0.365f, 0.678f, 0.886f, 0.90f);   // 出口(蓝)

        // 交替行色
        public static readonly Color RowEven      = new Color(0.133f, 0.133f, 0.267f, 0.40f);   // CardBg 变体
        public static readonly Color RowOdd       = new Color(0.102f, 0.102f, 0.180f, 0.35f);   // PanelBg 变体

        // 信息栏背景
        public static readonly Color InfoBarBg    = new Color(0.051f, 0.051f, 0.102f, 0.92f);   // DarkBg 同色
        // 标题栏背景
        public static readonly Color TitleBarBg   = new Color(0.067f, 0.055f, 0.125f, 0.95f);   // 深色标题栏
        // 滑块底色
        public static readonly Color SliderBg     = new Color(0.067f, 0.067f, 0.125f, 0.85f);   // 滑块暗色底色
        // 视口背景(ScrollRect Viewport)
        public static readonly Color ViewportBg   = new Color(0.102f, 0.102f, 0.180f, 0.96f);   // 同 PanelBg
    }
}
