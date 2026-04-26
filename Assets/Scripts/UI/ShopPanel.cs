using System.Collections.Generic;
using Game.Core;
using Logger = Game.Core.Logger;
using Game.Config;
using Game.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    [System.Obsolete("已废弃，商店由ShopWindow(WindowBase)替代")]
    public class ShopPanel : UIPanel
    {
        // 缓存引用
        private Text goldText;
        private Button btnPrev;
        private Button btnNext;
        private const int PageSize = 8;
        private const int PoolSize = 8;
        private List<UIBuilder.PoolRow> itemRows;

        // 所有商品缓存（OnInit 时构建一次）
        private List<ItemConfig> allItems;
        private int scrollOffset;

        /// <summary>仅供 PrefabGenerator 调用</summary>
        public static void CreateVisuals(Transform parent)
        {
            UIBuilder.CreateFullScreenBg(parent);
            UIBuilder.CreateTitleBar(parent, "商       店");

            // 玩家金币栏
            var goldBar = UIBuilder.CreatePanel(parent, 900, 40, "GoldBar");
            var goldRt = goldBar.GetComponent<RectTransform>();
            goldRt.anchorMin = new Vector2(0, 1);
            goldRt.anchorMax = new Vector2(1, 1);
            goldRt.pivot = new Vector2(0.5f, 1);
            goldRt.anchoredPosition = new Vector2(0, -70);
            goldRt.sizeDelta = new Vector2(0, 40);
            goldBar.GetComponent<Image>().color = UIBuilder.InfoBarBg;
            var gold = UIBuilder.CreateText(goldBar.transform, "", 18, UIBuilder.Gold, TextAnchor.MiddleCenter, name: "GoldText");
            UIBuilder.SetStretch(gold.gameObject);

            // 返回按钮（底部锚定）
            var btnBack = UIBuilder.CreateButton(parent, "返回", 180, 45, null, 20, "BtnBack");
            var btnBackRt = btnBack.GetComponent<RectTransform>();
            btnBackRt.anchorMin = new Vector2(0.5f, 0);
            btnBackRt.anchorMax = new Vector2(0.5f, 0);
            btnBackRt.pivot = new Vector2(0.5f, 0);
            btnBackRt.anchoredPosition = new Vector2(0, 30);

            // 翻页按钮（底部锚定，BtnBack 上方 10px）
            var prevBtn = UIBuilder.CreateButton(parent, "上一页", 130, 38, null, 16, "BtnPrev");
            var prevRt = prevBtn.GetComponent<RectTransform>();
            prevRt.anchorMin = new Vector2(0.5f, 0);
            prevRt.anchorMax = new Vector2(0.5f, 0);
            prevRt.pivot = new Vector2(0.5f, 0);
            prevRt.anchoredPosition = new Vector2(-100, 85);

            var nextBtn = UIBuilder.CreateButton(parent, "下一页", 130, 38, null, 16, "BtnNext");
            var nextRt = nextBtn.GetComponent<RectTransform>();
            nextRt.anchorMin = new Vector2(0.5f, 0);
            nextRt.anchorMax = new Vector2(0.5f, 0);
            nextRt.pivot = new Vector2(0.5f, 0);
            nextRt.anchoredPosition = new Vector2(100, 85);

            // 商品列表（拉伸填充 GoldBar 和 BtnPrev 之间）
            var listPanel = UIBuilder.CreateBorderedPanel(parent, 900, 480, "ItemList");
            var listRt = listPanel.GetComponent<RectTransform>();
            listRt.anchorMin = new Vector2(0, 0);
            listRt.anchorMax = new Vector2(1, 1);
            listRt.pivot = new Vector2(0.5f, 0.5f);
            listRt.offsetMin = new Vector2(10, 133);
            listRt.offsetMax = new Vector2(-10, -120);

            var inner = listPanel.transform.Find("Inner");
            UIBuilder.CreatePoolRows(inner, PoolSize, 50, 210, 800);
        }

        protected override void BindReferences()
        {
            goldText = FindComponent<Text>("GoldBar/GoldText");
            btnPrev = FindComponent<Button>("BtnPrev");
            btnNext = FindComponent<Button>("BtnNext");

            // 从预制体中重建对象池引用
            var inner = FindChild("ItemList/Inner");
            itemRows = new List<UIBuilder.PoolRow>();
            for (int i = 0; i < inner.childCount && itemRows.Count < PoolSize; i++)
            {
                var child = inner.GetChild(i);
                if (child.name == "PoolRow")
                {
                    itemRows.Add(new UIBuilder.PoolRow
                    {
                        obj = child.gameObject,
                        rt = child as RectTransform,
                        bg = child.GetComponent<Image>(),
                        nameText = child.Find("NameText")?.GetComponent<Text>(),
                        actionBtn = child.Find("ActionBtn")?.GetComponent<Button>(),
                        actionBtnLabel = child.Find("ActionBtn/Label")?.GetComponent<Text>(),
                        secondBtn = child.Find("SecondBtn")?.GetComponent<Button>(),
                        secondBtnLabel = child.Find("SecondBtn/Label")?.GetComponent<Text>()
                    });
                }
            }
        }

        protected override void BindEvents()
        {
            FindComponent<Button>("BtnBack").onClick.AddListener(() =>
            {
                scrollOffset = 0;
                UIManager.Instance.Back();
            });
            btnPrev.onClick.AddListener(() => { scrollOffset = Mathf.Max(0, scrollOffset - PageSize); RefreshUI(); });
            btnNext.onClick.AddListener(() => { scrollOffset += PageSize; RefreshUI(); });
        }

        public override void OnShow()
        {
            BuildItemList();
            scrollOffset = 0;
            RefreshUI();
        }

        private void BuildItemList()
        {
            allItems = new List<ItemConfig>();
            allItems.AddRange(ConfigManager.Instance.GetItemsByType(ItemType.Weapon));
            allItems.AddRange(ConfigManager.Instance.GetItemsByType(ItemType.Armor));
            allItems.AddRange(ConfigManager.Instance.GetItemsByType(ItemType.Accessory));
            allItems.AddRange(ConfigManager.Instance.GetItemsByType(ItemType.Consumable));
        }

        private void RefreshUI()
        {
            var player = GameManager.Instance.Player;

            // 金币
            goldText.text = $"金币: {player.gold}";

            // 商品行
            int shown = 0;
            for (int i = scrollOffset; i < allItems.Count && shown < PoolSize; i++, shown++)
            {
                var item = allItems[i];
                var row = itemRows[shown];
                row.obj.SetActive(true);
                row.rt.anchoredPosition = new Vector2(0, 210 - shown * 60);
                row.bg.color = shown % 2 == 0 ? UIBuilder.RowEven : UIBuilder.RowOdd;

                // 物品名+属性
                string statStr = "";
                if (item.atk > 0) statStr += $" 攻+{item.atk}";
                if (item.def > 0) statStr += $" 防+{item.def}";
                if (item.hp > 0) statStr += $" HP+{item.hp}";
                if (item.mp > 0) statStr += $" MP+{item.mp}";
                if (item.speed > 0) statStr += $" 速+{item.speed}";
                if (item.crit > 0) statStr += $" 暴+{item.crit}";
                if (item.dodge > 0) statStr += $" 闪+{item.dodge}";
                if (item.equipEffectType != EquipEffectType.None) statStr += $" [{GetEquipEffectName(item.equipEffectType, item.equipEffectValue)}]";
                if (item.restoreHp > 0) statStr += $" 恢复HP {item.restoreHp}";
                if (item.restoreMp > 0) statStr += $" 恢复MP {item.restoreMp}";

                string reqStr = item.reqLevel > 0 ? $"  [需Lv.{item.reqLevel}]" : "";
                row.nameText.text = $"{item.name}  {statStr}{reqStr}";

                // 购买按钮
                bool levelOk = item.reqLevel <= 0 || player.level >= item.reqLevel;
                bool canBuy = levelOk && player.gold >= item.price && player.bagItems.Count < player.maxBagSize;

                row.actionBtn.gameObject.SetActive(true);
                row.actionBtn.interactable = canBuy;
                row.actionBtnLabel.text = $"{item.price}金";
                row.actionBtnLabel.color = canBuy ? UIBuilder.Gold : UIBuilder.TextDim;
                row.actionBtn.image.color = UIBuilder.ButtonNormal;
                row.actionBtn.onClick.RemoveAllListeners();
                int capturedId = item.id;
                int capturedPrice = item.price;
                row.actionBtn.onClick.AddListener(() => BuyItem(capturedId, capturedPrice));

                // 隐藏第二按钮（商店不需要）
                row.secondBtn.gameObject.SetActive(false);
            }

            // 隐藏未使用的行
            for (int i = shown; i < PoolSize; i++)
                itemRows[i].obj.SetActive(false);

            // 翻页状态
            btnPrev.interactable = scrollOffset > 0;
            btnNext.interactable = scrollOffset + PoolSize < allItems.Count;
        }

        private void BuyItem(int itemId, int price)
        {
            Logger.LogI("商店", $"购买物品 id={itemId} 价格={price}");
            var player = GameManager.Instance.Player;
            var item = ConfigManager.Instance.GetItem(itemId);
            if (item != null && player.level < item.reqLevel) return; // 等级限制
            if (player.gold < price || player.bagItems.Count >= player.maxBagSize) return;

            player.gold -= price;
            player.bagItems.Add(itemId);
            RefreshUI();
        }

        /// <summary>获取装备特殊效果的显示名称</summary>
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
