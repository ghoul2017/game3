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
        private const int PoolSize = 20;
        private List<UIBuilder.PoolRow> itemRows;

        /// <summary>仅供 PrefabGenerator 调用，运行时不执行</summary>
        public static void CreateVisuals(Transform parent)
        {
            UIBuilder.CreateFullScreenBg(parent);
            UIBuilder.CreateTitleBar(parent, "背       包");

            var listPanel = UIBuilder.CreateBorderedPanel(parent, 900, 460, "ItemList");
            var listRt = listPanel.GetComponent<RectTransform>();
            listRt.anchorMin = new Vector2(0, 1);
            listRt.anchorMax = new Vector2(1, 1);
            listRt.pivot = new Vector2(0.5f, 1);
            listRt.anchoredPosition = new Vector2(0, -80);
            listRt.sizeDelta = new Vector2(0, 460);

            var inner = listPanel.transform.Find("Inner");
            var empty = UIBuilder.CreateText(inner, "背包是空的", 18, UIBuilder.TextDim, TextAnchor.MiddleCenter, name: "EmptyText");
            UIBuilder.SetStretch(empty.gameObject);
            empty.gameObject.SetActive(false);

            UIBuilder.CreatePoolRows(inner, PoolSize, 42, 195, 800);

            var infoBar = UIBuilder.CreatePanel(parent, 900, 42, "InfoBar");
            var infoBarRt = infoBar.GetComponent<RectTransform>();
            infoBarRt.anchorMin = new Vector2(0, 1);
            infoBarRt.anchorMax = new Vector2(1, 1);
            infoBarRt.pivot = new Vector2(0.5f, 1);
            infoBarRt.anchoredPosition = new Vector2(0, -555);
            infoBarRt.sizeDelta = new Vector2(0, 42);

            var info = UIBuilder.CreateText(infoBar.transform, "", 15, UIBuilder.Gold, TextAnchor.MiddleCenter, name: "InfoText");
            UIBuilder.SetStretch(info.gameObject);

            UIBuilder.CreateButton(parent, "返回", 180, 42, null, 18, "BtnBack")
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -615);
        }

        protected override void BindReferences()
        {
            infoText = FindComponent<Text>("InfoBar/InfoText");
            emptyText = FindComponent<Text>("ItemList/Inner/EmptyText");

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
            FindComponent<Button>("BtnBack").onClick.AddListener(() => UIManager.Instance.Back());
        }

        public override void OnShow()
        {
            RefreshUI();
        }

        private void RefreshUI()
        {
            var player = GameManager.Instance.Player;
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
                row.bg.color = idx % 2 == 0 ? UIBuilder.RowEven : UIBuilder.RowOdd;

                string statStr = "";
                if (item.atk > 0) statStr += $"攻+{item.atk} ";
                if (item.def > 0) statStr += $"防+{item.def} ";
                if (item.hp > 0) statStr += $"HP+{item.hp} ";
                if (item.restoreHp > 0) statStr += $"恢复HP{item.restoreHp} ";
                if (item.restoreMp > 0) statStr += $"恢复MP{item.restoreMp} ";

                string typeName = item.type switch
                {
                    ItemType.Weapon => "武器", ItemType.Armor => "防具",
                    ItemType.Accessory => "饰品", ItemType.Consumable => "消耗品", _ => ""
                };
                row.nameText.text = $"[{typeName}] {item.name} x{kv.Value}  {statStr}";

                int capturedId = kv.Key;
                bool isEquipType = item.type == ItemType.Weapon || item.type == ItemType.Armor || item.type == ItemType.Accessory;

                if (isEquipType)
                {
                    bool isEquipped = (item.type == ItemType.Weapon && player.weaponId == capturedId)
                        || (item.type == ItemType.Armor && player.armorId == capturedId)
                        || (item.type == ItemType.Accessory && player.accessoryId == capturedId);

                    row.actionBtn.gameObject.SetActive(true);
                    row.actionBtn.interactable = !isEquipped;
                    row.actionBtnLabel.text = isEquipped ? "已装备" : "装备";
                    row.actionBtnLabel.color = isEquipped ? UIBuilder.Green : UIBuilder.Gold;
                    row.actionBtn.image.color = isEquipped
                        ? new Color(0.08f, 0.2f, 0.1f, 0.5f)
                        : UIBuilder.ButtonNormal;
                    if (!isEquipped)
                    {
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

            for (int i = idx; i < PoolSize; i++)
                itemRows[i].obj.SetActive(false);

            emptyText.gameObject.SetActive(idx == 0);
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
                    player.weaponId = itemId; break;
                case ItemType.Armor:
                    if (player.armorId != 0) player.bagItems.Add(player.armorId);
                    player.armorId = itemId; break;
                case ItemType.Accessory:
                    if (player.accessoryId != 0) player.bagItems.Add(player.accessoryId);
                    player.accessoryId = itemId; break;
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
            if (item.restoreHp > 0) player.hp = Mathf.Min(player.hp + item.restoreHp, player.maxHp);
            if (item.restoreMp > 0) player.mp = Mathf.Min(player.mp + item.restoreMp, player.maxMp);
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
