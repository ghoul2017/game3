using System.Collections.Generic;
using Game.Core;
using Game.Config;
using Game.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    [System.Obsolete("已废弃，背包已集成到GameHUD")]
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

            // 返回按钮（底部锚定）
            var btnBack = UIBuilder.CreateButton(parent, "返回", 180, 42, null, 18, "BtnBack");
            var btnBackRt = btnBack.GetComponent<RectTransform>();
            btnBackRt.anchorMin = new Vector2(0.5f, 0);
            btnBackRt.anchorMax = new Vector2(0.5f, 0);
            btnBackRt.pivot = new Vector2(0.5f, 0);
            btnBackRt.anchoredPosition = new Vector2(0, 30);

            // 信息栏（底部锚定，BtnBack 上方 10px）
            var infoBar = UIBuilder.CreatePanel(parent, 900, 42, "InfoBar");
            var infoBarRt = infoBar.GetComponent<RectTransform>();
            infoBarRt.anchorMin = new Vector2(0, 0);
            infoBarRt.anchorMax = new Vector2(1, 0);
            infoBarRt.pivot = new Vector2(0.5f, 0);
            infoBarRt.anchoredPosition = new Vector2(0, 82);
            infoBarRt.sizeDelta = new Vector2(-20, 42);

            var info = UIBuilder.CreateText(infoBar.transform, "", 15, UIBuilder.Gold, TextAnchor.MiddleCenter, name: "InfoText");
            UIBuilder.SetStretch(info.gameObject);

            // 物品列表（拉伸填充 TitleBar 和 InfoBar 之间）
            var listPanel = UIBuilder.CreateBorderedPanel(parent, 900, 460, "ItemList");
            var listRt = listPanel.GetComponent<RectTransform>();
            listRt.anchorMin = new Vector2(0, 0);
            listRt.anchorMax = new Vector2(1, 1);
            listRt.pivot = new Vector2(0.5f, 0.5f);
            listRt.offsetMin = new Vector2(10, 134);
            listRt.offsetMax = new Vector2(-10, -70);

            var inner = listPanel.transform.Find("Inner");
            var empty = UIBuilder.CreateText(inner, "背包是空的", 18, UIBuilder.TextDim, TextAnchor.MiddleCenter, name: "EmptyText");
            UIBuilder.SetStretch(empty.gameObject);
            empty.gameObject.SetActive(false);

            UIBuilder.CreatePoolRows(inner, PoolSize, 42, 195, 800);
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
                if (row.obj == null) { idx++; continue; }
                row.obj.SetActive(true);
                if (row.rt != null) row.rt.anchoredPosition = new Vector2(0, 190 - idx * 49);
                if (row.bg != null) row.bg.color = idx % 2 == 0 ? UIBuilder.RowEven : UIBuilder.RowOdd;

                string statStr = "";
                if (item.atk > 0) statStr += $"攻+{item.atk} ";
                if (item.def > 0) statStr += $"防+{item.def} ";
                if (item.hp > 0) statStr += $"HP+{item.hp} ";
                if (item.mp > 0) statStr += $"MP+{item.mp} ";
                if (item.speed > 0) statStr += $"速+{item.speed} ";
                if (item.crit > 0) statStr += $"暴+{item.crit} ";
                if (item.dodge > 0) statStr += $"闪+{item.dodge} ";
                if (item.equipEffectType != EquipEffectType.None) statStr += $"[{GetEquipEffectName(item.equipEffectType, item.equipEffectValue)}] ";
                if (item.restoreHp > 0) statStr += $"恢复HP{item.restoreHp} ";
                if (item.restoreMp > 0) statStr += $"恢复MP{item.restoreMp} ";
                if (item.consumableEffect != ConsumableEffectType.None) statStr += $"[{GetConsumableEffectName(item.consumableEffect, item.consumableEffectValue)}] ";

                string typeName = item.type switch
                {
                    ItemType.Weapon => "武器", ItemType.Armor => "防具",
                    ItemType.Accessory => "饰品", ItemType.Consumable => "消耗品", _ => ""
                };
                string reqStr = item.reqLevel > 0 ? $"  [需Lv.{item.reqLevel}]" : "";
                if (row.nameText != null) row.nameText.text = $"[{typeName}] {item.name} x{kv.Value}  {statStr}{reqStr}";

                int capturedId = kv.Key;
                bool isEquipType = item.type == ItemType.Weapon || item.type == ItemType.Armor || item.type == ItemType.Accessory;

                if (isEquipType)
                {
                    bool isEquipped = (item.type == ItemType.Weapon && player.weaponId == capturedId)
                        || (item.type == ItemType.Armor && player.armorId == capturedId)
                        || (item.type == ItemType.Accessory && player.accessoryId == capturedId);
                    bool levelOk = player.level >= item.reqLevel;

                    if (row.actionBtn != null)
                    {
                        row.actionBtn.gameObject.SetActive(true);
                        row.actionBtn.interactable = !isEquipped && levelOk;
                        row.actionBtn.image.color = isEquipped
                            ? new Color(UIBuilder.Green.r * 0.35f, UIBuilder.Green.g * 0.35f, UIBuilder.Green.b * 0.35f, 0.5f)
                            : UIBuilder.ButtonNormal;
                        if (!isEquipped)
                        {
                            row.actionBtn.onClick.RemoveAllListeners();
                            int id = capturedId;
                            row.actionBtn.onClick.AddListener(() => EquipItem(id));
                        }
                    }
                    if (row.actionBtnLabel != null)
                    {
                        row.actionBtnLabel.text = isEquipped ? "已装备" : (levelOk ? "装备" : $"需Lv.{item.reqLevel}");
                        row.actionBtnLabel.color = isEquipped ? UIBuilder.Green : (levelOk ? UIBuilder.Gold : UIBuilder.TextDim);
                    }
                }
                else if (item.type == ItemType.Consumable)
                {
                    if (row.actionBtn != null)
                    {
                        row.actionBtn.gameObject.SetActive(true);
                        row.actionBtn.interactable = true;
                        row.actionBtn.image.color = UIBuilder.ButtonNormal;
                        row.actionBtn.onClick.RemoveAllListeners();
                        int id = capturedId;
                        row.actionBtn.onClick.AddListener(() => UseItem(id));
                    }
                    if (row.actionBtnLabel != null)
                    {
                        row.actionBtnLabel.text = "使用";
                        row.actionBtnLabel.color = UIBuilder.Gold;
                    }
                }
                else
                {
                    if (row.actionBtn != null) row.actionBtn.gameObject.SetActive(false);
                }

                int sellPrice = item.sellPrice > 0 ? item.sellPrice : item.price / 2;
                if (row.secondBtn != null)
                {
                    row.secondBtn.gameObject.SetActive(true);
                    row.secondBtn.interactable = true;
                    row.secondBtn.image.color = UIBuilder.ButtonNormal;
                    row.secondBtn.onClick.RemoveAllListeners();
                    int sId = capturedId;
                    int sPrice = sellPrice;
                    row.secondBtn.onClick.AddListener(() => SellItem(sId, sPrice));
                }
                if (row.secondBtnLabel != null)
                {
                    row.secondBtnLabel.text = $"卖{sellPrice}金";
                    row.secondBtnLabel.color = UIBuilder.Gold;
                }

                idx++;
            }

            for (int i = idx; i < PoolSize; i++)
            {
                if (itemRows[i].obj != null) itemRows[i].obj.SetActive(false);
            }

            if (emptyText != null) emptyText.gameObject.SetActive(idx == 0);
            if (infoText != null) infoText.text = $"容量: {player.bagItems.Count}/{player.maxBagSize}    金币: {player.gold}";
        }

        private void EquipItem(int itemId)
        {
            Logger.LogI("背包", $"装备物品 id={itemId}");
            var player = GameManager.Instance.Player;
            var item = ConfigManager.Instance.GetItem(itemId);
            if (item == null) return;

            // 等级限制检查
            if (player.level < item.reqLevel)
            {
                Logger.LogI("背包", $"等级不足，需要 {item.reqLevel} 级");
                return;
            }

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
            Logger.LogI("背包", $"使用物品 id={itemId}");
            var player = GameManager.Instance.Player;
            var item = ConfigManager.Instance.GetItem(itemId);
            if (item == null) return;

            // 回城卷轴：仅地牢中可用
            if (item.consumableEffect == ConsumableEffectType.Teleport)
            {
                if (GameManager.Instance.State != GameState.InDungeon)
                {
                    Logger.LogI("背包", "回城卷轴仅在地牢中可用");
                    if (infoText != null) infoText.text = "回城卷轴仅在地牢中可用";
                    return;
                }
                player.bagItems.Remove(itemId);
                // 执行回城逻辑
                GameManager.Instance.Dungeon.OnReturnTown();
                GameManager.Instance.SetState(GameState.InTown);
                GameManager.Instance.SaveGame();
                UIManager.Instance.ReplacePanel<TownPanel>();
                return;
            }

            // 炸弹：仅战斗中可用
            if (item.consumableEffect == ConsumableEffectType.Bomb)
            {
                if (infoText != null) infoText.text = "炸弹仅战斗中可用";
                return;
            }

            // 解毒药：仅战斗中可用（中毒状态在战斗中）
            if (item.consumableEffect == ConsumableEffectType.CurePoison)
            {
                if (infoText != null) infoText.text = "解毒药仅战斗中可用";
                return;
            }

            if (item.restoreHp > 0) player.hp = Mathf.Min(player.hp + item.restoreHp, player.maxHp);
            if (item.restoreMp > 0) player.mp = Mathf.Min(player.mp + item.restoreMp, player.maxMp);

            // 消耗品特殊效果（战斗外）
            switch (item.consumableEffect)
            {
                case ConsumableEffectType.AtkBuff:
                    player.atkBuffBattles += item.consumableEffectValue;
                    Logger.LogI("背包", $"兴奋剂：ATK+20%，持续 {player.atkBuffBattles} 场战斗");
                    break;
                case ConsumableEffectType.Shield:
                    player.hasShieldScroll = true;
                    Logger.LogI("背包", "护盾卷轴：下次受伤减伤50%");
                    break;
                case ConsumableEffectType.ExpBoost:
                    player.hasExpBoost = true;
                    Logger.LogI("背包", "经验书：下一场战斗经验+50%");
                    break;
            }

            player.bagItems.Remove(itemId);
            RefreshUI();
        }

        private void SellItem(int itemId, int price)
        {
            Logger.LogI("背包", $"出售物品 id={itemId} 价格={price}");
            var player = GameManager.Instance.Player;
            player.bagItems.Remove(itemId);
            player.gold += price;
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

        /// <summary>获取消耗品特殊效果的显示名称</summary>
        private static string GetConsumableEffectName(ConsumableEffectType type, int value)
        {
            return type switch
            {
                ConsumableEffectType.CurePoison => "解毒",
                ConsumableEffectType.AtkBuff => $"ATK+20%持续{value}战",
                ConsumableEffectType.Shield => "减伤50%一次",
                ConsumableEffectType.Teleport => "回城",
                ConsumableEffectType.ExpBoost => "经验+50%一场",
                ConsumableEffectType.Bomb => $"炸弹{value}伤",
                _ => ""
            };
        }
    }
}
