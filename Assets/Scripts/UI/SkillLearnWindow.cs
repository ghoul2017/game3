using System.Collections.Generic;
using Game.Config;
using Game.Core;
using Game.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 技能学习窗口 — 继承 WindowBase(750x520)
    /// 7标签页 + 技能列表(对象池20行) + 学习按钮，移除装备槽逻辑。
    /// </summary>
    public class SkillLearnWindow : WindowBase
    {
        // ============ 常量 ============
        private const int POOL_SIZE = 20;
        private const int TabCount = 7;

        // ============ 标签页定义 ============
        private static readonly SkillTab[] Tabs = new[]
        {
            SkillTab.All, SkillTab.Attack, SkillTab.Heal,
            SkillTab.Buff, SkillTab.Control, SkillTab.Passive, SkillTab.Learned
        };
        private static readonly string[] TabNames = new[]
        {
            "全部", "攻击", "治疗", "增益", "控制", "被动", "已学"
        };

        // ============ 组件引用 ============
        private Text infoText;
        private Button[] tabBtns = new Button[TabCount];
        private Text[] tabBtnLabels = new Text[TabCount];
        private List<UIBuilder.PoolRow> skillRows;

        // ============ 数据 ============
        private int currentTab;

        // ============ WindowBase 虚方法 ============

        protected override string GetWindowTitle() => "技能学习";

        protected override float GetWindowWidth() => 750f;
        protected override float GetWindowHeight() => 520f;

        protected override void BuildContent(Transform contentParent)
        {
            // 顶部信息栏：已学 X/6 + 金币
            var infoBar = UIFactory.CreateUI(contentParent, "InfoBar");
            var infoBarImg = infoBar.AddComponent<Image>();
            infoBarImg.color = UIColors.InfoBarBg;
            var infoBarRt = infoBar.GetComponent<RectTransform>();
            infoBarRt.anchorMin = new Vector2(0, 1);
            infoBarRt.anchorMax = new Vector2(1, 1);
            infoBarRt.pivot = new Vector2(0.5f, 1);
            infoBarRt.anchoredPosition = Vector2.zero;
            infoBarRt.sizeDelta = new Vector2(0, 32);

            infoText = UIFactory.CreateText(infoBar.transform, "", 16, UIColors.Gold,
                TextAnchor.MiddleCenter, true, "InfoText");
            UIBuilder.SetStretch(infoText.gameObject);

            // 标签页栏（信息栏下方）
            var tabBar = UIFactory.CreateUI(contentParent, "TabBar");
            var tabBarImg = tabBar.AddComponent<Image>();
            tabBarImg.color = UIColors.InfoBarBg;
            var tabBarRt = tabBar.GetComponent<RectTransform>();
            tabBarRt.anchorMin = new Vector2(0, 1);
            tabBarRt.anchorMax = new Vector2(1, 1);
            tabBarRt.pivot = new Vector2(0.5f, 1);
            tabBarRt.anchoredPosition = new Vector2(0, -34);
            tabBarRt.sizeDelta = new Vector2(0, 32);

            for (int i = 0; i < TabCount; i++)
            {
                var (btn, label) = UIFactory.CreateButtonWithRef(tabBar.transform, TabNames[i],
                    90, 28, null, 14, $"Tab_{i}");
                var btnRt = btn.GetComponent<RectTransform>();
                btnRt.anchorMin = new Vector2(0, 0.5f);
                btnRt.anchorMax = new Vector2(0, 0.5f);
                btnRt.pivot = new Vector2(0.5f, 0.5f);
                // 7个标签均匀分布
                btnRt.anchoredPosition = new Vector2(60 + i * 100, 0);
                tabBtns[i] = btn;
                tabBtnLabels[i] = label;
            }

            // 技能列表区域（标签页下方填满剩余空间）
            var listPanel = UIFactory.CreateBorderedPanel(contentParent, 740, 400, "SkillList");
            var listRt = listPanel.GetComponent<RectTransform>();
            listRt.anchorMin = Vector2.zero;
            listRt.anchorMax = Vector2.one;
            listRt.offsetMin = new Vector2(4, 4);
            listRt.offsetMax = new Vector2(-4, -68);

            var inner = listPanel.transform.Find("Inner");
            skillRows = UIBuilder.CreatePoolRows(inner, POOL_SIZE, 38, 180, 700);
        }

        protected override void OnWindowOpen()
        {
            currentTab = 0;
            RefreshUI();
        }

        protected override void BindEvents()
        {
            base.BindEvents(); // 关闭按钮

            // 标签页按钮事件
            for (int i = 0; i < TabCount; i++)
            {
                int capturedIdx = i;
                tabBtns[i]?.onClick.AddListener(() =>
                {
                    PlayUIClick();
                    currentTab = capturedIdx;
                    RefreshUI();
                });
            }
        }

        // ============ 刷新UI ============

        protected override void RefreshUI()
        {
            base.RefreshUI();

            var player = GameManager.Instance.Player;

            // 顶部信息栏
            if (infoText != null)
            {
                infoText.text = $"已学技能: {player.learnedSkills.Count}/{player.maxSkillSlots}    金币: {player.gold}";
            }

            // 标签页高亮
            for (int i = 0; i < TabCount; i++)
            {
                if (tabBtns[i] == null) continue;
                bool isActive = (i == currentTab);
                tabBtns[i].image.color = isActive
                    ? new Color(UIColors.Gold.r, UIColors.Gold.g, UIColors.Gold.b, 0.25f)
                    : UIColors.ButtonNormal;
                if (tabBtnLabels[i] != null)
                    tabBtnLabels[i].color = isActive ? UIColors.Gold : UIColors.TextDim;
            }

            // 获取当前标签页的技能列表
            var tabType = Tabs[currentTab];
            var filteredSkills = ConfigManager.Instance.GetSkillsByTab(tabType, player.learnedSkills);

            // 技能列表
            int shown = 0;
            for (int si = 0; si < filteredSkills.Count && shown < POOL_SIZE; si++)
            {
                var skill = filteredSkills[si];
                bool learned = player.learnedSkills.Contains(skill.id);
                bool slotsFull = player.learnedSkills.Count >= player.maxSkillSlots;
                bool canLearn = !learned
                    && (skill.prereqSkillId == 0 || player.learnedSkills.Contains(skill.prereqSkillId))
                    && player.gold >= skill.learnCost
                    && player.level >= skill.level
                    && !slotsFull;

                var row = skillRows[shown];
                row.obj.SetActive(true);
                if (row.rt != null)
                    row.rt.anchoredPosition = new Vector2(0, 180 - shown * 42);
                if (row.bg != null)
                    row.bg.color = learned
                        ? new Color(UIColors.Green.r * 0.4f, UIColors.Green.g * 0.4f, UIColors.Green.b * 0.3f, 0.5f)
                        : (shown % 2 == 0 ? UIColors.RowEven : UIColors.RowOdd);

                // 技能信息：[类型标] 名称 威力% MP消耗 描述
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
                    row.nameText.color = learned ? UIColors.Green : UIColors.TextWhite;
                }

                // 学习按钮 / 已学标记
                if (row.actionBtn != null)
                {
                    row.actionBtn.gameObject.SetActive(true);
                    row.actionBtn.onClick.RemoveAllListeners();
                    if (learned)
                    {
                        row.actionBtn.interactable = false;
                        row.actionBtn.image.color = new Color(UIColors.Green.r * 0.4f,
                            UIColors.Green.g * 0.4f, UIColors.Green.b * 0.3f, 0.5f);
                    }
                    else if (slotsFull)
                    {
                        // 槽满：可点击但弹出提示
                        row.actionBtn.interactable = true;
                        row.actionBtn.image.color = UIColors.ButtonDisable;
                        row.actionBtn.onClick.AddListener(() =>
                        {
                            PlayUIClick();
                            ShowSlotFullTip();
                        });
                    }
                    else
                    {
                        row.actionBtn.interactable = canLearn;
                        row.actionBtn.image.color = canLearn
                            ? UIColors.ButtonNormal
                            : new Color(UIColors.ButtonDisable.r, UIColors.ButtonDisable.g,
                                UIColors.ButtonDisable.b, 0.6f);
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
                        row.actionBtnLabel.color = UIColors.Green;
                    }
                    else if (slotsFull)
                    {
                        row.actionBtnLabel.text = "槽满";
                        row.actionBtnLabel.color = UIColors.Red;
                    }
                    else
                    {
                        row.actionBtnLabel.text = $"{skill.learnCost}金";
                        row.actionBtnLabel.color = canLearn ? UIColors.Gold : UIColors.TextDim;
                    }
                }

                // 遗忘按钮（仅已学技能显示）
                if (row.secondBtn != null)
                {
                    if (learned)
                    {
                        row.secondBtn.gameObject.SetActive(true);
                        row.secondBtn.interactable = true;
                        row.secondBtn.image.color = UIColors.ButtonNormal;
                        if (row.secondBtnLabel != null)
                        {
                            row.secondBtnLabel.text = "遗忘";
                            row.secondBtnLabel.color = UIColors.Red;
                        }
                        int capturedForgetId = skill.id;
                        row.secondBtn.onClick.RemoveAllListeners();
                        row.secondBtn.onClick.AddListener(() => ForgetSkill(capturedForgetId));
                    }
                    else
                    {
                        row.secondBtn.gameObject.SetActive(false);
                    }
                }

                shown++;
            }

            // 隐藏未使用的技能行
            for (int i = shown; i < skillRows.Count; i++)
            {
                if (skillRows[i].obj != null)
                    skillRows[i].obj.SetActive(false);
            }
        }

        // ============ 学习技能 ============

        private void LearnSkill(int skillId, int cost)
        {
            PlayUIClick();
            var player = GameManager.Instance.Player;
            if (player.gold < cost || player.learnedSkills.Contains(skillId)) return;

            // 槽满检查
            if (player.learnedSkills.Count >= player.maxSkillSlots)
            {
                ShowSlotFullTip();
                return;
            }

            // 等级检查
            var skill = ConfigManager.Instance.GetSkill(skillId);
            if (skill != null && player.level < skill.level) return;

            // 前置技能检查
            if (skill != null && skill.prereqSkillId > 0
                && !player.learnedSkills.Contains(skill.prereqSkillId)) return;

            player.gold -= cost;
            if (!player.LearnSkill(skillId))
            {
                // 学习失败（槽满）：退还金币
                player.gold += cost;
                ShowSlotFullTip();
                return;
            }
            Logger.LogI("技能", $"学习技能 id={skillId} 花费={cost}");
            RefreshUI();
        }

        // ============ 遗忘技能 ============

        private void ForgetSkill(int skillId)
        {
            PlayUIClick();
            var player = GameManager.Instance.Player;
            var skill = ConfigManager.Instance.GetSkill(skillId);
            string skillName = skill != null ? skill.name : "技能";
            player.ForgetSkill(skillId);
            Logger.LogI("技能", $"遗忘技能 {skillName}");
            RefreshUI();
        }

        // ============ 槽满提示 ============

        private void ShowSlotFullTip()
        {
            if (TipManager.Instance != null)
            {
                TipManager.Instance.ShowAutoTip(
                    $"技能槽已满({GameConstants.MAX_SKILL_SLOTS}/{GameConstants.MAX_SKILL_SLOTS})，请先遗忘再学习",
                    new Vector2(0, 350f), 3.0f);
            }
        }
    }
}
