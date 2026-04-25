using System.Collections.Generic;
using Game.Config;
using Game.Core;
using Game.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 商店/商人窗口 — 双模式：
    /// Town 城镇全商店(翻页浏览所有物品) + Merchant 地牢商人(3件随机物品8折选1件)
    /// 继承 WindowBase，使用 Window 层 Canvas 显示。
    /// </summary>
    public class ShopWindow : WindowBase
    {
        // ============ 模式定义 ============
        public enum ShopMode
        {
            Town,       // 城镇全商店：所有物品翻页
            Merchant    // 地牢商人：3件随机物品8折
        }

        // ============ 常量 ============
        private const int PAGE_SIZE = 8;        // 城镇模式每页物品数
        private const int POOL_SIZE = 8;        // 对象池行数
        private const float TOWN_WIDTH = 750f;
        private const float TOWN_HEIGHT = 520f;
        private const float MERCHANT_WIDTH = 500f;
        private const float MERCHANT_HEIGHT = 380f;
        private const float MERCHANT_DISCOUNT = 0.8f; // 8折

        // ============ 组件引用 ============
        private Text goldText;
        private Button btnPrev;
        private Button btnNext;
        private Text pageText;
        private List<UIBuilder.PoolRow> itemRows;

        // ============ 数据 ============
        private ShopMode mode;
        private List<ItemConfig> displayItems;  // 当前显示的物品列表
        private int scrollOffset;               // 城镇模式翻页偏移

        // 商人模式数据
        private List<ItemConfig> merchantItems; // 商人提供的3件物品
        private bool merchantBought;            // 商人模式是否已购买
        private bool isClosing;                 // 防止重复关闭

        // 静态参数传递（ShowWindow前设置，BuildContent时消费）
        private static ShopMode pendingMode;
        private static List<ItemConfig> pendingMerchantItems;

        // ============ 静态入口 ============

        /// <summary>打开城镇商店窗口</summary>
        public static void OpenTownShop()
        {
            // 在 ShowWindow 前设置模式，确保 BuildContent 可读取
            pendingMode = ShopMode.Town;
            pendingMerchantItems = null;
            UIManager.Instance.ShowWindow<ShopWindow>();
        }

        /// <summary>打开地牢商人窗口（3件物品8折）</summary>
        public static void OpenMerchantShop(List<ItemConfig> items)
        {
            pendingMode = ShopMode.Merchant;
            pendingMerchantItems = items;
            UIManager.Instance.ShowWindow<ShopWindow>();
        }

        // ============ WindowBase 虚方法 ============

        protected override string GetWindowTitle()
        {
            return mode == ShopMode.Merchant ? "流浪商人" : "商       店";
        }

        protected override float GetWindowWidth()
        {
            return mode == ShopMode.Merchant ? MERCHANT_WIDTH : TOWN_WIDTH;
        }

        protected override float GetWindowHeight()
        {
            return mode == ShopMode.Merchant ? MERCHANT_HEIGHT : TOWN_HEIGHT;
        }

        // BindReferences 在 OnInit 中调用，先消费静态参数
        protected override void BindReferences()
        {
            mode = pendingMode;
            merchantItems = pendingMerchantItems;
            merchantBought = false;
            isClosing = false;
            pendingMerchantItems = null;

            base.BindReferences(); // 触发 BuildWindowStructure -> BuildContent
        }

        protected override void BuildContent(Transform contentParent)
        {
            float contentW = mode == ShopMode.Merchant ? MERCHANT_WIDTH - 4 : TOWN_WIDTH - 4;
            float rowW = contentW - 20;  // 减去左右边距

            // 金币栏（顶部）
            var goldBar = UIFactory.CreateUI(contentParent, "GoldBar");
            var goldBarImg = goldBar.AddComponent<Image>();
            goldBarImg.color = UIColors.InfoBarBg;
            var goldBarRt = goldBar.GetComponent<RectTransform>();
            goldBarRt.anchorMin = new Vector2(0, 1);
            goldBarRt.anchorMax = new Vector2(1, 1);
            goldBarRt.pivot = new Vector2(0.5f, 1);
            goldBarRt.anchoredPosition = Vector2.zero;
            goldBarRt.sizeDelta = new Vector2(0, 36);

            goldText = UIFactory.CreateText(goldBar.transform, "", 18, UIColors.Gold,
                TextAnchor.MiddleCenter, true, "GoldText");
            UIBuilder.SetStretch(goldText.gameObject);

            if (mode == ShopMode.Town)
            {
                BuildTownContent(contentParent, rowW);
            }
            else
            {
                BuildMerchantContent(contentParent, rowW);
            }
        }

        /// <summary>构建城镇模式内容：物品列表 + 翻页按钮</summary>
        private void BuildTownContent(Transform parent, float rowW)
        {
            // 翻页按钮区域（底部）
            var navBar = UIFactory.CreateUI(parent, "NavBar");
            var navBarRt = navBar.GetComponent<RectTransform>();
            navBarRt.anchorMin = new Vector2(0, 0);
            navBarRt.anchorMax = new Vector2(1, 0);
            navBarRt.pivot = new Vector2(0.5f, 0);
            navBarRt.anchoredPosition = Vector2.zero;
            navBarRt.sizeDelta = new Vector2(0, 42);

            var (prevBtn, prevLabel) = UIFactory.CreateButtonWithRef(navBar.transform, "上一页", 110, 34, null, 16, "BtnPrev");
            prevBtn.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0.5f);
            prevBtn.GetComponent<RectTransform>().anchorMax = new Vector2(0, 0.5f);
            prevBtn.GetComponent<RectTransform>().pivot = new Vector2(0, 0.5f);
            prevBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(20, 0);
            btnPrev = prevBtn;

            pageText = UIFactory.CreateText(navBar.transform, "", 16, UIColors.TextDim,
                TextAnchor.MiddleCenter, false, "PageText");
            var pageRt = pageText.GetComponent<RectTransform>();
            pageRt.anchorMin = new Vector2(0, 0);
            pageRt.anchorMax = new Vector2(1, 1);
            pageRt.offsetMin = Vector2.zero;
            pageRt.offsetMax = Vector2.zero;

            var (nextBtn, nextLabel) = UIFactory.CreateButtonWithRef(navBar.transform, "下一页", 110, 34, null, 16, "BtnNext");
            nextBtn.GetComponent<RectTransform>().anchorMin = new Vector2(1, 0.5f);
            nextBtn.GetComponent<RectTransform>().anchorMax = new Vector2(1, 0.5f);
            nextBtn.GetComponent<RectTransform>().pivot = new Vector2(1, 0.5f);
            nextBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-20, 0);
            btnNext = nextBtn;

            // 物品列表区域（金币栏和导航栏之间）
            var listPanel = UIFactory.CreateBorderedPanel(parent, TOWN_WIDTH, 400, "ItemList");
            var listRt = listPanel.GetComponent<RectTransform>();
            listRt.anchorMin = Vector2.zero;
            listRt.anchorMax = Vector2.one;
            listRt.offsetMin = new Vector2(6, 48);
            listRt.offsetMax = new Vector2(-6, -40);

            var inner = listPanel.transform.Find("Inner");
            itemRows = UIBuilder.CreatePoolRows(inner, POOL_SIZE, 44, 195, rowW - 6);
        }

        /// <summary>构建商人模式内容：3件物品列表 + 提示</summary>
        private void BuildMerchantContent(Transform parent, float rowW)
        {
            // 商人提示文字
            var tipText = UIFactory.CreateText(parent, "商品8折优惠，仅限选购1件", 16, UIColors.Orange,
                TextAnchor.MiddleCenter, false, "TipText");
            var tipRt = tipText.GetComponent<RectTransform>();
            tipRt.anchorMin = new Vector2(0, 1);
            tipRt.anchorMax = new Vector2(1, 1);
            tipRt.pivot = new Vector2(0.5f, 1);
            tipRt.anchoredPosition = new Vector2(0, -40);
            tipRt.sizeDelta = new Vector2(0, 24);

            // 物品列表（只显示3行）
            var listPanel = UIFactory.CreateBorderedPanel(parent, MERCHANT_WIDTH, 240, "ItemList");
            var listRt = listPanel.GetComponent<RectTransform>();
            listRt.anchorMin = new Vector2(0.5f, 0.5f);
            listRt.anchorMax = new Vector2(0.5f, 0.5f);
            listRt.pivot = new Vector2(0.5f, 0.5f);
            listRt.anchoredPosition = new Vector2(0, 10);

            var inner = listPanel.transform.Find("Inner");
            itemRows = UIBuilder.CreatePoolRows(inner, 3, 52, 100, rowW - 6);

            // 翻页按钮不需要
            btnPrev = null;
            btnNext = null;
        }

        protected override void OnWindowOpen()
        {
            if (mode == ShopMode.Town)
            {
                BuildItemList();
                scrollOffset = 0;
            }
            else
            {
                displayItems = merchantItems ?? new List<ItemConfig>();
            }
            RefreshUI();
        }

        protected override void OnWindowClose()
        {
            // 清理引用
            displayItems = null;
            merchantItems = null;
        }

        // ============ 数据构建 ============

        /// <summary>城镇模式：构建全物品列表</summary>
        private void BuildItemList()
        {
            displayItems = new List<ItemConfig>();
            displayItems.AddRange(ConfigManager.Instance.GetItemsByType(ItemType.Weapon));
            displayItems.AddRange(ConfigManager.Instance.GetItemsByType(ItemType.Armor));
            displayItems.AddRange(ConfigManager.Instance.GetItemsByType(ItemType.Accessory));
            displayItems.AddRange(ConfigManager.Instance.GetItemsByType(ItemType.Consumable));
        }

        // ============ 刷新UI ============

        protected override void RefreshUI()
        {
            base.RefreshUI();

            if (displayItems == null || displayItems.Count == 0)
            {
                RefreshEmptyState();
                return;
            }

            var player = GameManager.Instance.Player;

            // 金币
            if (goldText != null)
                goldText.text = $"金币: {player.gold}";

            // 城镇模式翻页
            if (mode == ShopMode.Town)
            {
                RefreshTownItems(player);
            }
            else
            {
                RefreshMerchantItems(player);
            }
        }

        /// <summary>空状态显示</summary>
        private void RefreshEmptyState()
        {
            var player = GameManager.Instance.Player;
            if (goldText != null && player != null)
                goldText.text = $"金币: {player.gold}";

            if (itemRows != null)
            {
                foreach (var row in itemRows)
                    row.obj.SetActive(false);
            }

            if (btnPrev != null) btnPrev.interactable = false;
            if (btnNext != null) btnNext.interactable = false;
            if (pageText != null) pageText.text = "暂无商品";
        }

        /// <summary>城镇模式：刷新物品行</summary>
        private void RefreshTownItems(PlayerData player)
        {
            int maxPool = itemRows.Count;
            int shown = 0;

            for (int i = scrollOffset; i < displayItems.Count && shown < maxPool; i++, shown++)
            {
                var item = displayItems[i];
                var row = itemRows[shown];
                FillItemRow(row, item, item.price, player, shown);
            }

            // 隐藏未使用的行
            for (int i = shown; i < maxPool; i++)
                itemRows[i].obj.SetActive(false);

            // 翻页状态
            if (btnPrev != null)
                btnPrev.interactable = scrollOffset > 0;
            if (btnNext != null)
                btnNext.interactable = scrollOffset + maxPool < displayItems.Count;

            // 页码
            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)displayItems.Count / maxPool));
            int currentPage = scrollOffset / maxPool + 1;
            if (pageText != null)
                pageText.text = $"{currentPage}/{totalPages}";
        }

        /// <summary>商人模式：刷新3件物品</summary>
        private void RefreshMerchantItems(PlayerData player)
        {
            int shown = 0;
            for (int i = 0; i < displayItems.Count && i < itemRows.Count; i++, shown++)
            {
                var item = displayItems[i];
                var row = itemRows[i];
                int discountPrice = Mathf.RoundToInt(item.price * MERCHANT_DISCOUNT);

                // 已购买时全部禁用
                if (merchantBought)
                {
                    FillItemRowDisabled(row, item, discountPrice);
                }
                else
                {
                    FillItemRow(row, item, discountPrice, player, i);
                }
            }

            // 隐藏未使用的行
            for (int i = shown; i < itemRows.Count; i++)
                itemRows[i].obj.SetActive(false);
        }

        /// <summary>填充一行物品数据</summary>
        private void FillItemRow(UIBuilder.PoolRow row, ItemConfig item, int price, PlayerData player, int index)
        {
            row.obj.SetActive(true);
            row.rt.anchoredPosition = new Vector2(0, row.rt.anchoredPosition.y);
            row.bg.color = index % 2 == 0 ? UIColors.RowEven : UIColors.RowOdd;

            // 物品名+属性
            row.nameText.text = FormatItemDisplay(item);

            // 购买按钮
            bool levelOk = item.reqLevel <= 0 || player.level >= item.reqLevel;
            bool canBuy = levelOk && player.gold >= price && player.bagItems.Count < player.maxBagSize;

            row.actionBtn.gameObject.SetActive(true);
            row.actionBtn.interactable = canBuy;
            row.actionBtnLabel.text = $"{price}金";
            row.actionBtnLabel.color = canBuy ? UIColors.Gold : UIColors.TextDim;
            row.actionBtn.image.color = UIColors.ButtonNormal;
            row.actionBtn.onClick.RemoveAllListeners();
            int capturedId = item.id;
            int capturedPrice = price;
            row.actionBtn.onClick.AddListener(() => BuyItem(capturedId, capturedPrice));

            // 隐藏第二按钮
            row.secondBtn.gameObject.SetActive(false);
        }

        /// <summary>填充已禁用的物品行（商人已购买后）</summary>
        private void FillItemRowDisabled(UIBuilder.PoolRow row, ItemConfig item, int price)
        {
            row.obj.SetActive(true);
            row.bg.color = UIColors.RowOdd;

            row.nameText.text = FormatItemDisplay(item);

            row.actionBtn.gameObject.SetActive(true);
            row.actionBtn.interactable = false;
            row.actionBtnLabel.text = "已售";
            row.actionBtnLabel.color = UIColors.TextDim;

            row.secondBtn.gameObject.SetActive(false);
        }

        // ============ 购买逻辑 ============

        private void BuyItem(int itemId, int price)
        {
            PlayUIClick();

            var player = GameManager.Instance.Player;
            var item = ConfigManager.Instance.GetItem(itemId);
            if (item == null) return;

            // 等级检查
            if (item.reqLevel > 0 && player.level < item.reqLevel) return;
            // 金币检查
            if (player.gold < price) return;
            // 背包空间检查
            if (player.bagItems.Count >= player.maxBagSize) return;

            // 扣金币→加背包
            player.gold -= price;
            player.bagItems.Add(itemId);

            // 商人模式：购买后标记已买，关闭窗口
            if (mode == ShopMode.Merchant)
            {
                merchantBought = true;
                RefreshUI();
                // 延迟关闭，让玩家看到购买结果
                StartCoroutine(DelayedClose(0.5f));
                return;
            }

            // 城镇模式：刷新显示
            RefreshUI();
        }

        /// <summary>延迟关闭窗口协程</summary>
        private System.Collections.IEnumerator DelayedClose(float delay)
        {
            yield return new UnityEngine.WaitForSeconds(delay);
            if (!isClosing)
            {
                isClosing = true;
                UIManager.Instance?.CloseWindow();
            }
        }

        // ============ 事件绑定 ============

        protected override void BindEvents()
        {
            base.BindEvents(); // 关闭按钮

            if (btnPrev != null)
                btnPrev.onClick.AddListener(() =>
                {
                    PlayUIClick();
                    scrollOffset = Mathf.Max(0, scrollOffset - PAGE_SIZE);
                    RefreshUI();
                });

            if (btnNext != null)
                btnNext.onClick.AddListener(() =>
                {
                    PlayUIClick();
                    scrollOffset += PAGE_SIZE;
                    RefreshUI();
                });
        }

        // ============ 格式化工具 ============

        /// <summary>格式化物品显示文本：名称+属性+等级要求</summary>
        private static string FormatItemDisplay(ItemConfig item)
        {
            string statStr = "";
            if (item.atk > 0) statStr += $" 攻+{item.atk}";
            if (item.def > 0) statStr += $" 防+{item.def}";
            if (item.hp > 0) statStr += $" HP+{item.hp}";
            if (item.mp > 0) statStr += $" MP+{item.mp}";
            if (item.speed > 0) statStr += $" 速+{item.speed}";
            if (item.crit > 0) statStr += $" 暴+{item.crit}";
            if (item.dodge > 0) statStr += $" 闪+{item.dodge}";
            if (item.equipEffectType != EquipEffectType.None)
                statStr += $" [{GetEquipEffectName(item.equipEffectType, item.equipEffectValue)}]";
            if (item.restoreHp > 0) statStr += $" 恢复HP {item.restoreHp}";
            if (item.restoreMp > 0) statStr += $" 恢复MP {item.restoreMp}";

            string reqStr = item.reqLevel > 0 ? $"  [需Lv.{item.reqLevel}]" : "";
            return $"{item.name}  {statStr}{reqStr}";
        }

        /// <summary>获取装备特殊效果显示名称</summary>
        private static string GetEquipEffectName(EquipEffectType type, int value)
        {
            return type switch
            {
                EquipEffectType.CritChance => $"暴击+{value}%",
                EquipEffectType.Dodge => $"闪避+{value}%",
                EquipEffectType.Lifesteal => $"吸血{value}%",
                EquipEffectType.Thorns => $"反伤{value}%",
                EquipEffectType.SkillCostReduce => $"技能耗-{value}%",
                EquipEffectType.BossDamageBonus => $"Boss伤+{value}%",
                EquipEffectType.DamageVariance => $"波动{value}%",
                EquipEffectType.AutoRevive => $"复活({value}%HP)",
                _ => ""
            };
        }
    }
}
