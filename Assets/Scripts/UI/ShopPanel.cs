using System.Collections.Generic;
using Game.Core;
using Game.Config;
using Game.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
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

        public override void OnInit()
        {
            UIBuilder.CreateFullScreenBg(transform);
            UIBuilder.CreateTitleBar(transform, "商  店");

            // 玩家金币栏
            var goldBar = UIBuilder.CreatePanel(transform, 900, 40, "GoldBar");
            var goldRt = goldBar.GetComponent<RectTransform>();
            goldRt.anchorMin = new Vector2(0, 1);
            goldRt.anchorMax = new Vector2(1, 1);
            goldRt.pivot = new Vector2(0.5f, 1);
            goldRt.anchoredPosition = new Vector2(0, -70);
            goldRt.sizeDelta = new Vector2(0, 40);
            goldBar.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.10f, 0.9f);
            goldText = UIBuilder.CreateText(goldBar.transform, "", 20, UIBuilder.Gold, TextAnchor.MiddleCenter);
            UIBuilder.SetStretch(goldText.gameObject);

            // 商品列表
            var listPanel = UIBuilder.CreateBorderedPanel(transform, 900, 500, "ItemList");
            listPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 10);
            var inner = listPanel.transform.Find("Inner");

            // 对象池商品行（预创建 PageSize 行）
            itemRows = UIBuilder.CreatePoolRows(inner, PoolSize, 50, 210, 800);

            // 底部按钮
            UIBuilder.CreateButton(transform, "返回", 200, 45, () =>
            {
                scrollOffset = 0;
                UIManager.Instance.Back();
            }, 22, "BtnBack").GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -300);

            // 翻页按钮（缓存引用以便控制 interactable）
            btnPrev = UIBuilder.CreateButton(transform, "上一页", 120, 35,
                () => { scrollOffset = Mathf.Max(0, scrollOffset - PageSize); RefreshUI(); }, 18, "BtnPrev");
            btnPrev.GetComponent<RectTransform>().anchoredPosition = new Vector2(-150, -260);

            btnNext = UIBuilder.CreateButton(transform, "下一页", 120, 35,
                () => { scrollOffset += PageSize; RefreshUI(); }, 18, "BtnNext");
            btnNext.GetComponent<RectTransform>().anchoredPosition = new Vector2(150, -260);

            // 缓存所有商品（OnInit 时构建一次，避免每次 RefreshUI 重复查询）
            BuildItemList();
        }

        public override void OnShow()
        {
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
                row.bg.color = shown % 2 == 0
                    ? new Color(0.1f, 0.1f, 0.16f, 0.5f)
                    : new Color(0.08f, 0.08f, 0.12f, 0.5f);

                // 物品名+属性
                string statStr = "";
                if (item.atk > 0) statStr += $" 攻+{item.atk}";
                if (item.def > 0) statStr += $" 防+{item.def}";
                if (item.hp > 0) statStr += $" HP+{item.hp}";
                if (item.mp > 0) statStr += $" MP+{item.mp}";
                if (item.restoreHp > 0) statStr += $" 恢复HP {item.restoreHp}";
                if (item.restoreMp > 0) statStr += $" 恢复MP {item.restoreMp}";

                row.nameText.text = $"{item.name}  {statStr}";

                // 购买按钮
                bool canBuy = player.gold >= item.price && player.bagItems.Count < player.maxBagSize;

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
            Debug.Log($"[商店] 购买物品 id={itemId} 价格={price}");
            var player = GameManager.Instance.Player;
            if (player.gold < price || player.bagItems.Count >= player.maxBagSize) return;

            player.gold -= price;
            player.bagItems.Add(itemId);
            RefreshUI();
        }
    }
}
