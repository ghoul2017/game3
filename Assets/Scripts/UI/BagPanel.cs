using System.Collections.Generic;
using Game.Core;
using Game.Config;
using Game.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class BagPanel : UIPanel
    {
        public override void OnInit()
        {
            UIBuilder.CreateFullScreenBg(transform);
            UIBuilder.CreateTitleBar(transform, "背  包");

            // 物品列表
            var listPanel = UIBuilder.CreateBorderedPanel(transform, 900, 450, "ItemList");
            listPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 50);

            // 底部信息
            var infoBar = UIBuilder.CreatePanel(transform, 900, 50, "InfoBar");
            infoBar.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -210);

            UIBuilder.CreateButton(transform, "返回", 200, 45, () =>
            {
                UIManager.Instance.Back();
            }, 22, "BtnBack").GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -280);
        }

        public override void OnShow()
        {
            RefreshUI();
        }

        private void RefreshUI()
        {
            var player = GameManager.Instance.Player;

            // 统计物品
            var itemCounts = new Dictionary<int, int>();
            foreach (var itemId in player.bagItems)
            {
                if (!itemCounts.ContainsKey(itemId)) itemCounts[itemId] = 0;
                itemCounts[itemId]++;
            }

            // 物品列表
            var inner = transform.Find("ItemList/Inner");
            if (inner != null)
            {
                for (int i = inner.childCount - 1; i >= 0; i--)
                    Object.DestroyImmediate(inner.GetChild(i).gameObject);

                int idx = 0;
                foreach (var kv in itemCounts)
                {
                    var item = ConfigManager.Instance.GetItem(kv.Key);
                    if (item == null) continue;

                    float y = 190 - idx * 55;
                    var row = new GameObject($"Item_{kv.Key}");
                    row.transform.SetParent(inner, false);
                    var rowRt = row.AddComponent<RectTransform>();
                    rowRt.sizeDelta = new Vector2(800, 45);
                    rowRt.anchoredPosition = new Vector2(0, y);

                    var rowImg = row.AddComponent<Image>();
                    rowImg.color = idx % 2 == 0 ? new Color(0.1f, 0.1f, 0.16f, 0.5f) : new Color(0.08f, 0.08f, 0.12f, 0.5f);

                    string statStr = "";
                    if (item.atk > 0) statStr += $"攻+{item.atk} ";
                    if (item.def > 0) statStr += $"防+{item.def} ";
                    if (item.hp > 0) statStr += $"HP+{item.hp} ";
                    if (item.restoreHp > 0) statStr += $"恢复HP{item.restoreHp} ";
                    if (item.restoreMp > 0) statStr += $"恢复MP{item.restoreMp} ";

                    string typeName = item.type switch
                    {
                        ItemType.Weapon => "武器",
                        ItemType.Armor => "防具",
                        ItemType.Accessory => "饰品",
                        ItemType.Consumable => "消耗品",
                        _ => ""
                    };

                    var nameText = UIBuilder.CreateText(row.transform, $"[{typeName}] {item.name} x{kv.Value}  {statStr}", 16, UIBuilder.TextWhite, TextAnchor.MiddleLeft);
                    UIBuilder.SetAnchored(nameText.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(-150, 0), new Vector2(500, 40));

                    int capturedId = kv.Key;
                    // 装备按钮（武器/防具/饰品）
                    if (item.type == ItemType.Weapon || item.type == ItemType.Armor || item.type == ItemType.Accessory)
                    {
                        bool isEquipped = (item.type == ItemType.Weapon && player.weaponId == capturedId)
                            || (item.type == ItemType.Armor && player.armorId == capturedId)
                            || (item.type == ItemType.Accessory && player.accessoryId == capturedId);

                        if (isEquipped)
                        {
                            UIBuilder.CreateText(row.transform, "已装备", 15, UIBuilder.Green, TextAnchor.MiddleCenter)
                                .GetComponent<RectTransform>().anchoredPosition = new Vector2(280, 0);
                        }
                        else
                        {
                            UIBuilder.CreateButton(row.transform, "装备", 80, 30,
                                () => EquipItem(capturedId), 14, $"Equip_{capturedId}")
                                .GetComponent<RectTransform>().anchoredPosition = new Vector2(280, 0);
                        }
                    }

                    // 使用按钮（消耗品）
                    if (item.type == ItemType.Consumable)
                    {
                        UIBuilder.CreateButton(row.transform, "使用", 80, 30,
                            () => UseItem(capturedId), 14, $"Use_{capturedId}")
                            .GetComponent<RectTransform>().anchoredPosition = new Vector2(280, 0);
                    }

                    // 出售
                    int sellPrice = item.price / 2;
                    UIBuilder.CreateButton(row.transform, $"卖{sellPrice}金", 80, 30,
                        () => SellItem(capturedId, sellPrice), 14, $"Sell_{capturedId}")
                        .GetComponent<RectTransform>().anchoredPosition = new Vector2(370, 0);

                    idx++;
                }

                if (itemCounts.Count == 0)
                {
                    var emptyText = UIBuilder.CreateText(inner, "背包是空的", 20, UIBuilder.TextDim, TextAnchor.MiddleCenter);
                    UIBuilder.SetStretch(emptyText.gameObject);
                }
            }

            // 底部信息
            var infoBar = transform.Find("InfoBar");
            if (infoBar != null)
            {
                for (int i = infoBar.childCount - 1; i >= 0; i--)
                    Object.DestroyImmediate(infoBar.GetChild(i).gameObject);
                string info = $"容量: {player.bagItems.Count}/{player.maxBagSize}    金币: {player.gold}";
                var t = UIBuilder.CreateText(infoBar, info, 16, UIBuilder.Gold, TextAnchor.MiddleCenter);
                UIBuilder.SetStretch(t.gameObject);
            }
        }

        private void EquipItem(int itemId)
        {
            Debug.Log($"[背包] 装备物品 id={itemId}");
            var player = GameManager.Instance.Player;
            var item = ConfigManager.Instance.GetItem(itemId);
            if (item == null) return;

            // 卸下旧装备
            switch (item.type)
            {
                case ItemType.Weapon:
                    if (player.weaponId != 0) player.bagItems.Add(player.weaponId);
                    player.weaponId = itemId;
                    break;
                case ItemType.Armor:
                    if (player.armorId != 0) player.bagItems.Add(player.armorId);
                    player.armorId = itemId;
                    break;
                case ItemType.Accessory:
                    if (player.accessoryId != 0) player.bagItems.Add(player.accessoryId);
                    player.accessoryId = itemId;
                    break;
            }

            player.bagItems.Remove(itemId);
            player.RecalcStats();
            RefreshUI();
        }

        private void UseItem(int itemId)
        {
            Debug.Log($"[背包] 使用物品 id={itemId}");
            var player = GameManager.Instance.Player;
            var item = ConfigManager.Instance.GetItem(itemId);
            if (item == null) return;

            if (item.restoreHp > 0)
                player.hp = Mathf.Min(player.hp + item.restoreHp, player.maxHp);
            if (item.restoreMp > 0)
                player.mp = Mathf.Min(player.mp + item.restoreMp, player.maxMp);

            player.bagItems.Remove(itemId);
            RefreshUI();
        }

        private void SellItem(int itemId, int price)
        {
            Debug.Log($"[背包] 出售物品 id={itemId} 价格={price}");
            var player = GameManager.Instance.Player;
            player.bagItems.Remove(itemId);
            player.gold += price;
            RefreshUI();
        }
    }
}
