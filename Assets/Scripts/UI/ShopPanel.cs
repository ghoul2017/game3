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
        private int scrollOffset;

        public override void OnInit()
        {
            UIBuilder.CreateFullScreenBg(transform);
            UIBuilder.CreateTitleBar(transform, "商  店");

            // 玩家金币
            var goldBar = UIBuilder.CreatePanel(transform, 0, 40, "GoldBar");
            var goldRt = goldBar.GetComponent<RectTransform>();
            goldRt.anchorMin = new Vector2(0, 1);
            goldRt.anchorMax = new Vector2(1, 1);
            goldRt.pivot = new Vector2(0.5f, 1);
            goldRt.anchoredPosition = new Vector2(0, -70);
            goldRt.sizeDelta = new Vector2(0, 40);
            goldBar.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.10f, 0.9f);

            // 商品列表
            var listPanel = UIBuilder.CreateBorderedPanel(transform, 900, 500, "ItemList");
            listPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 10);

            // 底部按钮
            UIBuilder.CreateButton(transform, "返回", 200, 45, () =>
            {
                scrollOffset = 0;
                UIManager.Instance.Back();
            }, 22, "BtnBack").GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -300);

            // 翻页
            UIBuilder.CreateButton(transform, "上一页", 120, 35, () => { scrollOffset = Mathf.Max(0, scrollOffset - 8); RefreshUI(); }, 18, "BtnPrev")
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(-150, -260);
            UIBuilder.CreateButton(transform, "下一页", 120, 35, () => { scrollOffset += 8; RefreshUI(); }, 18, "BtnNext")
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(150, -260);
        }

        public override void OnShow()
        {
            scrollOffset = 0;
            RefreshUI();
        }

        private void RefreshUI()
        {
            var player = GameManager.Instance.Player;

            // 金币
            var goldBar = transform.Find("GoldBar");
            if (goldBar != null)
            {
                for (int i = goldBar.childCount - 1; i >= 0; i--)
                    Object.Destroy(goldBar.GetChild(i).gameObject);
                var goldText = UIBuilder.CreateText(goldBar, $"金币: {player.gold}", 20, UIBuilder.Gold, TextAnchor.MiddleCenter);
                UIBuilder.SetStretch(goldText.gameObject);
            }

            // 商品列表
            var inner = transform.Find("ItemList/Inner");
            if (inner == null) return;
            for (int i = inner.childCount - 1; i >= 0; i--)
                Object.Destroy(inner.GetChild(i).gameObject);

            var allItems = new List<ItemConfig>();
            // 武器
            allItems.AddRange(ConfigManager.Instance.GetItemsByType(ItemType.Weapon));
            allItems.AddRange(ConfigManager.Instance.GetItemsByType(ItemType.Armor));
            allItems.AddRange(ConfigManager.Instance.GetItemsByType(ItemType.Accessory));
            allItems.AddRange(ConfigManager.Instance.GetItemsByType(ItemType.Consumable));

            int shown = 0;
            for (int i = scrollOffset; i < allItems.Count && shown < 8; i++, shown++)
            {
                var item = allItems[i];
                float y = 210 - shown * 60;

                // 物品行
                var row = new GameObject($"Item_{i}");
                row.transform.SetParent(inner, false);
                var rowRt = row.AddComponent<RectTransform>();
                rowRt.sizeDelta = new Vector2(800, 50);
                rowRt.anchoredPosition = new Vector2(0, y);

                var rowImg = row.AddComponent<Image>();
                rowImg.color = shown % 2 == 0 ? new Color(0.1f, 0.1f, 0.16f, 0.5f) : new Color(0.08f, 0.08f, 0.12f, 0.5f);

                // 物品名
                string statStr = "";
                if (item.atk > 0) statStr += $" 攻+{item.atk}";
                if (item.def > 0) statStr += $" 防+{item.def}";
                if (item.hp > 0) statStr += $" HP+{item.hp}";
                if (item.mp > 0) statStr += $" MP+{item.mp}";
                if (item.restoreHp > 0) statStr += $" 恢复HP {item.restoreHp}";
                if (item.restoreMp > 0) statStr += $" 恢复MP {item.restoreMp}";

                var nameText = UIBuilder.CreateText(row, $"{item.name}  {statStr}", 17, UIBuilder.TextWhite, TextAnchor.MiddleLeft);
                UIBuilder.SetAnchored(nameText.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(-350, 0), new Vector2(500, 40));

                // 价格+购买按钮
                int capturedId = item.id;
                int capturedPrice = item.price;
                bool canBuy = player.gold >= capturedPrice && player.bagItems.Count < player.maxBagSize;

                var buyBtn = UIBuilder.CreateButton(row, $"{item.price}金", 90, 35,
                    () => BuyItem(capturedId, capturedPrice), 16, $"Buy_{i}");
                buyBtn.interactable = canBuy;
                buyBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(300, 0);
            }
        }

        private void BuyItem(int itemId, int price)
        {
            var player = GameManager.Instance.Player;
            if (player.gold < price || player.bagItems.Count >= player.maxBagSize) return;

            player.gold -= price;
            player.bagItems.Add(itemId);
            RefreshUI();
        }
    }
}
