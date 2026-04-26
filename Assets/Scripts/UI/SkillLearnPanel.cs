using System.Collections.Generic;
using Game.Core;
using Logger = Game.Core.Logger;
using Game.Config;
using Game.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    [System.Obsolete("暂未废弃，等待WindowBase版本完成")]
    public class SkillLearnPanel : UIPanel
    {
        // 标签页定义
        private static readonly SkillTab[] Tabs = new[]
        {
            SkillTab.All, SkillTab.Attack, SkillTab.Heal,
            SkillTab.Buff, SkillTab.Control, SkillTab.Passive, SkillTab.Learned
        };
        private static readonly string[] TabNames = new[]
        {
            "全部", "攻击", "治疗", "增益", "控制", "被动", "已学"
        };

        // 当前选中标签页
        private int currentTab;

        // 对象池大小
        private const int SkillPoolSize = 48;
        private const int SkillSelectPoolSize = 16;
        private const int TabCount = 7;
        private const int EquipSlotCount = GameConstants.MAX_SKILL_SLOTS;

        // 缓存引用
        private Text goldText;
        private List<UIBuilder.PoolRow> skillRows;

        // 标签页按钮池
        private Button[] tabBtns;
        private Text[] tabBtnLabels;

        // 装备槽引用（4个固定槽位）
        private Button[] slotBtns = new Button[EquipSlotCount];
        private Text[] slotNameLabels = new Text[EquipSlotCount];
        private Text[] slotMpLabels = new Text[EquipSlotCount];

        // 技能选择弹窗
        private GameObject selectOverlay;
        private Text selectTitle;
        private List<UIBuilder.PoolRow> selectRows;
        private Button selectCloseBtn;
        private int selectingSlotIndex = -1; // 当前正在选择装备的槽位索引

        /// <summary>仅供 PrefabGenerator 调用</summary>
        public static void CreateVisuals(Transform parent)
        {
            UIBuilder.CreateFullScreenBg(parent);
            UIBuilder.CreateTitleBar(parent, "技能学习");

            // 金币栏
            var goldBar = UIBuilder.CreatePanel(parent, 0, 40, "GoldBar");
            var goldRt = goldBar.GetComponent<RectTransform>();
            goldRt.anchorMin = new Vector2(0, 1);
            goldRt.anchorMax = new Vector2(1, 1);
            goldRt.pivot = new Vector2(0.5f, 1);
            goldRt.anchoredPosition = new Vector2(0, -70);
            goldRt.sizeDelta = new Vector2(0, 40);
            goldBar.GetComponent<Image>().color = UIBuilder.InfoBarBg;
            var gold = UIBuilder.CreateText(goldBar.transform, "", 18, UIBuilder.Gold, TextAnchor.MiddleCenter, name: "GoldText");
            UIBuilder.SetStretch(gold.gameObject);

            // 标签页栏（金币栏下方）
            var tabBar = UIBuilder.CreatePanel(parent, 0, 36, "TabBar");
            var tabRt = tabBar.GetComponent<RectTransform>();
            tabRt.anchorMin = new Vector2(0, 1);
            tabRt.anchorMax = new Vector2(1, 1);
            tabRt.pivot = new Vector2(0.5f, 1);
            tabRt.anchoredPosition = new Vector2(0, -110);
            tabRt.sizeDelta = new Vector2(0, 36);
            tabBar.GetComponent<Image>().color = UIBuilder.InfoBarBg;

            var tabInner = tabBar.transform;
            // 7个标签页按钮，均匀排列
            for (int i = 0; i < TabCount; i++)
            {
                var (btn, label) = UIBuilder.CreateButtonWithRef(tabInner, TabNames[i], 110, 30, null, 14, $"Tab_{i}");
                btn.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0.5f);
                btn.GetComponent<RectTransform>().anchorMax = new Vector2(0, 0.5f);
                btn.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
                btn.GetComponent<RectTransform>().anchoredPosition = new Vector2(80 + i * 120, 0);
            }

            // 返回按钮（底部锚定）
            var btnBack = UIBuilder.CreateButton(parent, "返回", 180, 42, null, 18, "BtnBack");
            var btnBackRt = btnBack.GetComponent<RectTransform>();
            btnBackRt.anchorMin = new Vector2(0.5f, 0);
            btnBackRt.anchorMax = new Vector2(0.5f, 0);
            btnBackRt.pivot = new Vector2(0.5f, 0);
            btnBackRt.anchoredPosition = new Vector2(0, 30);

            // ===== 装备槽区（4个独立槽位） =====
            CreateEquipSlotVisuals(parent);

            // 技能列表（拉伸填充 TabBar 和 EquippedSkills 之间）
            var listPanel = UIBuilder.CreateBorderedPanel(parent, 900, 420, "SkillList");
            var listRt = listPanel.GetComponent<RectTransform>();
            listRt.anchorMin = new Vector2(0, 0);
            listRt.anchorMax = new Vector2(1, 1);
            listRt.pivot = new Vector2(0.5f, 0.5f);
            listRt.offsetMin = new Vector2(10, 162);
            listRt.offsetMax = new Vector2(-10, -150);

            var inner = listPanel.transform.Find("Inner");
            UIBuilder.CreatePoolRows(inner, SkillPoolSize, 42, 170, 800);

            // ===== 技能选择弹窗（默认隐藏） =====
            CreateSkillSelectOverlay(parent);
        }

        /// <summary>创建4个装备槽位视觉结构</summary>
        private static void CreateEquipSlotVisuals(Transform parent)
        {
            var eqPanel = UIBuilder.CreateBorderedPanel(parent, 900, 70, "EquippedSkills");
            var eqPanelRt = eqPanel.GetComponent<RectTransform>();
            eqPanelRt.anchorMin = new Vector2(0, 0);
            eqPanelRt.anchorMax = new Vector2(1, 0);
            eqPanelRt.pivot = new Vector2(0.5f, 0);
            eqPanelRt.anchoredPosition = new Vector2(0, 82);
            eqPanelRt.sizeDelta = new Vector2(-20, 70);

            var eqInner = eqPanel.transform.Find("Inner");

            var eqLbl = UIBuilder.CreateText(eqInner, "装备槽:", 15, UIBuilder.Gold, TextAnchor.MiddleLeft, name: "EqLabel");
            UIBuilder.SetAnchored(eqLbl.gameObject, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(15, 0), new Vector2(80, 28));

            // 4个固定装备槽按钮
            float slotW = 170;
            float gap = 15;
            float startX = -((EquipSlotCount * slotW + (EquipSlotCount - 1) * gap) / 2f) + slotW / 2f + 95;

            for (int i = 0; i < EquipSlotCount; i++)
            {
                var slotOuter = UIBuilder.CreateBorderedPanel(eqInner, slotW, 52, $"Slot_{i}");
                UIBuilder.SetAnchored(slotOuter,
                    new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                    new Vector2(startX + i * (slotW + gap), 0), new Vector2(slotW, 52));

                var slotInner = slotOuter.transform.Find("Inner");

                // 技能名称
                var nameLabel = UIBuilder.CreateText(slotInner, "", 15, UIBuilder.TextWhite,
                    TextAnchor.MiddleCenter, false, "SkillName");
                UIBuilder.SetAnchored(nameLabel.gameObject,
                    new Vector2(0, 0.35f), new Vector2(1, 1),
                    Vector2.zero, Vector2.zero);

                // MP消耗标签
                var mpLabel = UIBuilder.CreateText(slotInner, "", 12, UIBuilder.MpColor,
                    TextAnchor.MiddleCenter, false, "SkillMp");
                UIBuilder.SetAnchored(mpLabel.gameObject,
                    new Vector2(0, 0), new Vector2(1, 0.4f),
                    Vector2.zero, Vector2.zero);

                // 添加Button组件使槽位可点击
                slotOuter.AddComponent<Button>();
            }
        }

        /// <summary>创建技能选择弹窗覆盖层</summary>
        private static void CreateSkillSelectOverlay(Transform parent)
        {
            var overlay = new GameObject("SkillSelectOverlay");
            overlay.transform.SetParent(parent, false);
            UIBuilder.SetStretch(overlay);
            var oImg = overlay.AddComponent<Image>();
            oImg.color = new Color(UIBuilder.DarkBg.r, UIBuilder.DarkBg.g, UIBuilder.DarkBg.b, 0.92f);
            overlay.AddComponent<CanvasGroup>();
            overlay.SetActive(false);

            // 弹窗面板
            var panel = UIBuilder.CreateBorderedPanel(overlay.transform, 600, 420, "SelectPanel");
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta = new Vector2(600, 420);

            var panelInner = panel.transform.Find("Inner");

            // 标题
            var title = UIBuilder.CreateText(panelInner, "选择技能装备", 22, UIBuilder.Gold,
                TextAnchor.MiddleCenter, true, "SelectTitle");
            UIBuilder.SetAnchored(title.gameObject,
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -30), new Vector2(0, 36));

            // 关闭按钮
            var closeBtn = UIBuilder.CreateButton(panelInner, "关闭", 120, 36, null, 16, "CloseBtn");
            UIBuilder.SetAnchored(closeBtn.gameObject,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 15), new Vector2(120, 36));

            // 技能列表
            var listPanel = UIBuilder.CreateBorderedPanel(panelInner, 550, 300, "SelectList");
            var listRt = listPanel.GetComponent<RectTransform>();
            listRt.anchorMin = new Vector2(0.5f, 0.5f);
            listRt.anchorMax = new Vector2(0.5f, 0.5f);
            listRt.pivot = new Vector2(0.5f, 0.5f);
            listRt.anchoredPosition = new Vector2(0, 10);
            listRt.sizeDelta = new Vector2(550, 300);

            var listInner = listPanel.transform.Find("Inner");
            UIBuilder.CreatePoolRows(listInner, SkillSelectPoolSize, 38, 130, 520);
        }

        protected override void BindReferences()
        {
            goldText = FindComponent<Text>("GoldBar/GoldText");

            // 标签页按钮
            tabBtns = new Button[TabCount];
            tabBtnLabels = new Text[TabCount];
            for (int i = 0; i < TabCount; i++)
            {
                tabBtns[i] = FindComponent<Button>($"TabBar/Tab_{i}");
                tabBtnLabels[i] = FindComponent<Text>($"TabBar/Tab_{i}/Label");
            }

            // 从预制体中重建技能行对象池引用
            var inner = FindChild("SkillList/Inner");
            skillRows = new List<UIBuilder.PoolRow>();
            for (int i = 0; i < inner.childCount && skillRows.Count < SkillPoolSize; i++)
            {
                var child = inner.GetChild(i);
                if (child.name == "PoolRow")
                {
                    skillRows.Add(new UIBuilder.PoolRow
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

            // 装备槽引用
            for (int i = 0; i < EquipSlotCount; i++)
            {
                slotBtns[i] = FindComponent<Button>($"EquippedSkills/Inner/Slot_{i}");
                slotNameLabels[i] = FindComponent<Text>($"EquippedSkills/Inner/Slot_{i}/Inner/SkillName");
                slotMpLabels[i] = FindComponent<Text>($"EquippedSkills/Inner/Slot_{i}/Inner/SkillMp");
            }

            // 技能选择弹窗
            selectOverlay = transform.Find("SkillSelectOverlay").gameObject;
            selectTitle = FindComponent<Text>("SkillSelectOverlay/SelectPanel/Inner/SelectTitle");
            selectCloseBtn = FindComponent<Button>("SkillSelectOverlay/SelectPanel/Inner/CloseBtn");

            var selectListInner = FindChild("SkillSelectOverlay/SelectPanel/Inner/SelectList/Inner");
            selectRows = new List<UIBuilder.PoolRow>();
            for (int i = 0; i < selectListInner.childCount && selectRows.Count < SkillSelectPoolSize; i++)
            {
                var child = selectListInner.GetChild(i);
                if (child.name == "PoolRow")
                {
                    selectRows.Add(new UIBuilder.PoolRow
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

            // 标签页按钮事件
            for (int i = 0; i < TabCount; i++)
            {
                int capturedIdx = i;
                tabBtns[i]?.onClick.AddListener(() =>
                {
                    currentTab = capturedIdx;
                    RefreshUI();
                });
            }

            // 装备槽按钮事件
            for (int i = 0; i < EquipSlotCount; i++)
            {
                int capturedSlot = i;
                slotBtns[i]?.onClick.AddListener(() =>
                {
                    PlayUIClick();
                    OnClickEquipSlot(capturedSlot);
                });
            }

            // 选择弹窗关闭按钮
            selectCloseBtn?.onClick.AddListener(() =>
            {
                PlayUIClick();
                CloseSelectOverlay();
            });
        }

        public override void OnShow()
        {
            currentTab = 0;
            RefreshUI();
        }

        /// <summary>
        /// 零销毁刷新：只修改属性
        /// </summary>
        private void RefreshUI()
        {
            var player = GameManager.Instance.Player;

            // 金币（显示已学技能数量）
            if (goldText != null)
            {
                goldText.text = $"金币: {player.gold}    已学技能: {player.learnedSkills.Count}/{player.maxSkillSlots}";
            }

            // 标签页高亮
            for (int i = 0; i < TabCount; i++)
            {
                if (tabBtns[i] == null) continue;
                bool isActive = (i == currentTab);
                tabBtns[i].image.color = isActive
                    ? new Color(UIBuilder.Gold.r, UIBuilder.Gold.g, UIBuilder.Gold.b, 0.25f)
                    : UIBuilder.ButtonNormal;
                if (tabBtnLabels[i] != null)
                    tabBtnLabels[i].color = isActive ? UIBuilder.Gold : UIBuilder.TextDim;
            }

            // 根据当前标签页获取技能列表
            var tabType = Tabs[currentTab];
            var filteredSkills = ConfigManager.Instance.GetSkillsByTab(tabType, player.learnedSkills);

            // 技能列表
            int shown = 0;
            for (int si = 0; si < filteredSkills.Count && shown < SkillPoolSize; si++)
            {
                var skill = filteredSkills[si];
                bool learned = player.learnedSkills.Contains(skill.id);
                bool canLearn = !learned
                    && (skill.prereqSkillId == 0 || player.learnedSkills.Contains(skill.prereqSkillId))
                    && player.gold >= skill.learnCost
                    && player.level >= skill.level;

                var row = skillRows[shown];
                if (row.obj == null) { shown++; continue; }
                row.obj.SetActive(true);
                if (row.rt != null) row.rt.anchoredPosition = new Vector2(0, 170 - shown * 46);
                if (row.bg != null) row.bg.color = learned
                    ? new Color(UIBuilder.Green.r * 0.4f, UIBuilder.Green.g * 0.4f, UIBuilder.Green.b * 0.3f, 0.5f)
                    : (shown % 2 == 0 ? UIBuilder.RowEven : UIBuilder.RowOdd);

                // 分类标签
                string categoryStr = skill.GetCategoryName();
                string statStr = skill.power > 0 ? $"威力:{skill.power}%" : "";
                string mpStr = skill.mpCost > 0 ? $"MP:{skill.mpCost}" : "";
                string info = $"[{categoryStr}] {skill.name}  {statStr} {mpStr}  {skill.desc}";
                if (skill.prereqSkillId > 0)
                {
                    var prereq = ConfigManager.Instance.GetSkill(skill.prereqSkillId);
                    if (prereq != null) info += $"  (前置:{prereq.name})";
                }
                if (row.nameText != null)
                {
                    row.nameText.text = info;
                    row.nameText.color = learned ? UIBuilder.Green : UIBuilder.TextWhite;
                }

                // 学习按钮 / 已学标签
                if (row.actionBtn != null)
                {
                    row.actionBtn.gameObject.SetActive(true);
                    row.actionBtn.onClick.RemoveAllListeners();
                    if (learned)
                    {
                        row.actionBtn.interactable = false;
                        row.actionBtn.image.color = new Color(UIBuilder.Green.r * 0.4f, UIBuilder.Green.g * 0.4f, UIBuilder.Green.b * 0.3f, 0.5f);
                    }
                    else
                    {
                        row.actionBtn.interactable = canLearn;
                        row.actionBtn.image.color = canLearn
                            ? UIBuilder.ButtonNormal
                            : new Color(UIBuilder.ButtonDisable.r, UIBuilder.ButtonDisable.g, UIBuilder.ButtonDisable.b, 0.6f);
                        int capturedId = skill.id;
                        int capturedCost = skill.learnCost;
                        row.actionBtn.onClick.AddListener(() => LearnSkill(capturedId, capturedCost));
                    }
                }
                if (row.actionBtnLabel != null)
                {
                    if (learned)
                    {
                        row.actionBtnLabel.text = "已学";
                        row.actionBtnLabel.color = UIBuilder.Green;
                    }
                    else
                    {
                        row.actionBtnLabel.text = $"{skill.learnCost}金";
                        row.actionBtnLabel.color = canLearn ? UIBuilder.Gold : UIBuilder.TextDim;
                    }
                }
                if (row.secondBtn != null) row.secondBtn.gameObject.SetActive(false);

                shown++;
            }

            // 隐藏未使用的技能行
            for (int i = shown; i < SkillPoolSize; i++)
            {
                if (i < skillRows.Count && skillRows[i].obj != null)
                    skillRows[i].obj.SetActive(false);
            }

            // 装备槽位刷新
            RefreshEquipSlots(player);
        }

        /// <summary>刷新技能槽位显示（learnedSkills即可用技能）</summary>
        private void RefreshEquipSlots(PlayerData player)
        {
            for (int i = 0; i < EquipSlotCount; i++)
            {
                if (slotBtns[i] == null) continue;

                bool hasSkill = i < player.learnedSkills.Count;
                if (hasSkill)
                {
                    var skillId = player.learnedSkills[i];
                    var skill = ConfigManager.Instance.GetSkill(skillId);
                    if (skill != null)
                    {
                        if (slotNameLabels[i] != null)
                        {
                            slotNameLabels[i].text = skill.name;
                            slotNameLabels[i].color = UIBuilder.TextWhite;
                        }
                        if (slotMpLabels[i] != null)
                        {
                            slotMpLabels[i].text = skill.mpCost > 0 ? $"MP {skill.mpCost}" : "";
                            slotMpLabels[i].color = UIBuilder.MpColor;
                        }
                    }
                    else
                    {
                        if (slotNameLabels[i] != null)
                        {
                            slotNameLabels[i].text = "???";
                            slotNameLabels[i].color = UIBuilder.TextDim;
                        }
                        if (slotMpLabels[i] != null) slotMpLabels[i].text = "";
                    }
                    slotBtns[i].image.color = new Color(UIBuilder.ButtonNormal.r, UIBuilder.ButtonNormal.g, UIBuilder.ButtonNormal.b, 1f);
                }
                else
                {
                    if (slotNameLabels[i] != null)
                    {
                        slotNameLabels[i].text = $"+ 空槽位";
                        slotNameLabels[i].color = UIBuilder.TextDim;
                    }
                    if (slotMpLabels[i] != null) slotMpLabels[i].text = "";
                    slotBtns[i].image.color = new Color(UIBuilder.ButtonDisable.r, UIBuilder.ButtonDisable.g, UIBuilder.ButtonDisable.g, 0.5f);
                }
            }
        }

        /// <summary>点击技能槽位处理：已学技能点击=遗忘</summary>
        private void OnClickEquipSlot(int slotIndex)
        {
            var player = GameManager.Instance.Player;
            bool hasSkill = slotIndex < player.learnedSkills.Count;

            if (hasSkill)
            {
                // 已学技能：遗忘
                int skillId = player.learnedSkills[slotIndex];
                var skill = ConfigManager.Instance.GetSkill(skillId);
                string skillName = skill != null ? skill.name : "技能";
                player.ForgetSkill(skillId);
                Logger.LogI("技能", $"遗忘技能 {skillName}");
                RefreshUI();
            }
            // 空槽位不响应（技能通过列表学习）
        }

        private void CloseSelectOverlay()
        {
            if (selectOverlay != null)
                selectOverlay.SetActive(false);
        }

        private void LearnSkill(int skillId, int cost)
        {
            Logger.LogI("技能", $"学习技能 id={skillId} 花费={cost}");
            var player = GameManager.Instance.Player;
            if (player.gold < cost || player.learnedSkills.Contains(skillId)) return;
            player.gold -= cost;
            if (!player.LearnSkill(skillId))
            {
                // 槽满：退还金币
                player.gold += cost;
                Logger.LogI("技能", "技能槽已满，无法学习");
            }
            RefreshUI();
        }

        /// <summary>供外部调用的刷新入口（如神殿学技能后回调）</summary>
        public void RefreshFromOutside()
        {
            RefreshUI();
        }
    }
}
