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
        // 缓存引用
        private Text infoText;
        private Text emptyText;
        private const int PoolSize = 20; // maxBagSize 同步
        private List<UIBuilder.PoolRow> itemRows;

        public override void OnInit()
        {
            UIBuilder.CreateFullScreenBg(transform);
            UIBuilder.CreateTitleBar(transform, "背  包");

            // 物品列表容器
            var listPanel = UIBuilder.CreateBorderedPanel(transform, 900, 450, "ItemList");
            listPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 50);

            var inner = listPanel.transform.Find("Inner");

            // 空背包提示（预创建，默认隐藏）
            emptyText = UIBuilder.CreateText(inner, "背包是空的", 20, UIBuilder.TextDim, TextAnchor.MiddleCenter);
            UIBuilder.SetStretch(emptyText.gameObject);
            emptyText.gameObject.SetActive(false);

            // 物品行对象池（预创建 PoolSize 行）
            itemRows = UIBuilder.CreatePoolRows(inner, PoolSize, 45, 190, 800);

            // 底部信息栏（预创建文本）
            var infoBar = UIBuilder.CreatePanel(transform, 900, 50, "InfoBar");
            infoBar.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -210);
            infoText = UIBuilder.CreateText(infoBar.transform, "", 16, UIBuilder.Gold, TextAnchor.MiddleCenter);
            UIBuilder.SetStretch(infoText.gameObject);

            // 返回按钮
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

            int idx = 0;
            foreach (var kv in itemCounts)
            {
                if (idx >= PoolSize) break;

                var item = ConfigManager.Instance.GetItem(kv.Key);
                if (item == null) continue;

                var row = itemRows[idx];
                row.obj.SetActive(true);
                row.rt.anchoredPosition = new Vector2(0, 190 - idx * 49);
                row.bg.color = idx % 2 == 0
                    ? new Color(0.1f, 0.1f, 0.16f, 0.5f)
                    : new Color(0.08f, 0.08f, 0.12f, 0.5f);

                // 物品名+属性
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

                row.nameText.text = $"[{typeName}] {item.name} x{kv.Value}  {statStr}";

                // 操作按钮（装备/使用/已装备）
                int capturedId = kv.Key;
                bool isEquipType = item.type == ItemType.Weapon || item.type == ItemType.Armor || item.type == ItemType.Accessory;

                if (isEquipType)
                {
                    bool isEquipped = (item.type == ItemType.Weapon && player.weaponId == capturedId)
                        || (item.type == ItemType.Armor && player.armorId == capturedId)
                        || (item.type == ItemType.Accessory && player.accessoryId == capturedId);

                    if (isEquipped)
                    {
                        // 显示"已装备"文字：把按钮当标签用
                        row.actionBtn.gameObject.SetActive(true);
                        row.actionBtn.interactable = false;
                        row.actionBtnLabel.text = "已装备";
                        row.actionBtnLabel.color = UIBuilder.Green;
                        row.actionBtn.image.color = new Color(0.08f, 0.2f, 0.1f, 0.5f);
                    }
                    else
                    {
                        row.actionBtn.gameObject.SetActive(true);
                        row.actionBtn.interactable = true;
                        row.actionBtnLabel.text = "装备";
                        row.actionBtnLabel.color = UIBuilder.Gold;
                        row.actionBtn.image.color = UIBuilder.ButtonNormal;
                        row.actionBtn.onClick.RemoveAllListeners();
                        int id = capturedId;
                        row.actionBtn.onClick.AddListener(() => EquipItem(id));
                    }
                }
                else if (item.type == ItemType.Consumable)
                {
                    row.actionBtn.gameObject.SetActive(true);
                    row.actionBtn.interactable = true;
                    row.actionBtnLabel.text = "使用";
                    row.actionBtnLabel.color = UIBuilder.Gold;
                    row.actionBtn.image.color = UIBuilder.ButtonNormal;
                    row.actionBtn.onClick.RemoveAllListeners();
                    int id = capturedId;
                    row.actionBtn.onClick.AddListener(() => UseItem(id));
                }
                else
                {
                    row.actionBtn.gameObject.SetActive(false);
                }

                // 出售按钮
                int sellPrice = item.price / 2;
                row.secondBtn.gameObject.SetActive(true);
                row.secondBtn.interactable = true;
                row.secondBtnLabel.text = $"卖{sellPrice}金";
                row.secondBtnLabel.color = UIBuilder.Gold;
                row.secondBtn.image.color = UIBuilder.ButtonNormal;
                row.secondBtn.onClick.RemoveAllListeners();
                int sId = capturedId;
                int sPrice = sellPrice;
                row.secondBtn.onClick.AddListener(() => SellItem(sId, sPrice));

                idx++;
            }

            // 隐藏未使用的行
            for (int i = idx; i < PoolSize; i++)
                itemRows[i].obj.SetActive(false);

            // 空背包提示
            emptyText.gameObject.SetActive(idx == 0);

            // 底部信息
            infoText.text = $"容量: {player.bagItems.Count}/{player.maxBagSize}    金币: {player.gold}";
        }

        private void EquipItem(int itemId)
        {
            Debug.Log($"[背包] 装备物品 id={itemId}");
            var player = GameManager.Instance.Player;
            var item = ConfigManager.Instance.GetItem(itemId);
            if (item == null) return;

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
