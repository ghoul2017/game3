using System.Collections.Generic;
using Game.Config;
using Game.Core;
using Game.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 三选一窗口 — 双模式：
    /// Shrine 神殿：3个随机未学技能免费选1学，全已学兜底恢复50%HP
    /// Merchant 商人：3件随机物品8折选1买
    /// 继承 WindowBase(500x380)
    /// </summary>
    public class ThreeChooseOneWindow : WindowBase
    {
        // ============ 模式定义 ============
        public enum ChooseMode
        {
            Shrine,   // 神殿：3个技能免费选1
            Merchant  // 商人：3件物品8折选1
        }

        // ============ 常量 ============
        private const float CARD_W = 200f;
        private const float CARD_H = 80f;
        private const float CARD_GAP = 16f;
        private const float MERCHANT_DISCOUNT = 0.8f;
        private const int MAX_CHOICES = 3;

        // ============ 组件引用 ============
        private Text descText;
        private GameObject[] cardObjs = new GameObject[MAX_CHOICES];
        private Text[] cardNameTexts = new Text[MAX_CHOICES];
        private Text[] cardDescTexts = new Text[MAX_CHOICES];
        private Button[] cardBtns = new Button[MAX_CHOICES];
        private Button btnLeave;

        // ============ 数据 ============
        private ChooseMode mode;
        private List<SkillConfig> shrineSkills;   // 神殿模式：3个候选技能
        private List<ItemConfig> merchantItems;    // 商人模式：3个候选物品
        private bool hasChosen;                    // 是否已选择

        // 神殿全已学兜底：恢复50%HP
        private bool isFallbackHeal;

        // 静态参数传递（ShowWindow前设置，BuildContent时消费）
        private static ChooseMode pendingMode;
        private static List<SkillConfig> pendingShrineSkills;
        private static List<ItemConfig> pendingMerchantItems;
        private static bool pendingFallbackHeal;

        // ============ 静态入口 ============

        /// <summary>打开神殿三选一窗口</summary>
        /// <param name="skills">3个候选技能（已排除已学）</param>
        /// <param name="fallbackHeal">全已学兜底：是否为恢复HP模式</param>
        public static void OpenShrine(List<SkillConfig> skills, bool fallbackHeal = false)
        {
            pendingMode = ChooseMode.Shrine;
            pendingShrineSkills = skills;
            pendingMerchantItems = null;
            pendingFallbackHeal = fallbackHeal;
            UIManager.Instance.ShowWindow<ThreeChooseOneWindow>();
        }

        /// <summary>打开商人三选一窗口</summary>
        /// <param name="items">3件候选物品</param>
        public static void OpenMerchant(List<ItemConfig> items)
        {
            pendingMode = ChooseMode.Merchant;
            pendingShrineSkills = null;
            pendingMerchantItems = items;
            pendingFallbackHeal = false;
            UIManager.Instance.ShowWindow<ThreeChooseOneWindow>();
        }

        // ============ WindowBase 虚方法 ============

        protected override string GetWindowTitle()
        {
            return mode == ChooseMode.Shrine ? "技能神殿" : "流浪商人";
        }

        protected override float GetWindowWidth() => 500f;
        protected override float GetWindowHeight() => 380f;

        // BindReferences 在 OnInit 中调用，先消费静态参数
        protected override void BindReferences()
        {
            mode = pendingMode;
            shrineSkills = pendingShrineSkills ?? new List<SkillConfig>();
            merchantItems = pendingMerchantItems ?? new List<ItemConfig>();
            isFallbackHeal = pendingFallbackHeal;
            hasChosen = false;
            pendingShrineSkills = null;
            pendingMerchantItems = null;

            base.BindReferences(); // 触发 BuildWindowStructure -> BuildContent
        }

        protected override void BuildContent(Transform contentParent)
        {
            // 描述文字（顶部）
            descText = UIFactory.CreateText(contentParent, "", 16, UIColors.TextWhite,
                TextAnchor.MiddleCenter, false, "DescText");
            var descRt = descText.GetComponent<RectTransform>();
            descRt.anchorMin = new Vector2(0, 1);
            descRt.anchorMax = new Vector2(1, 1);
            descRt.pivot = new Vector2(0.5f, 1);
            descRt.anchoredPosition = new Vector2(0, -8);
            descRt.sizeDelta = new Vector2(0, 40);

            // 3张卡片（水平居中排列）
            float totalW = MAX_CHOICES * CARD_W + (MAX_CHOICES - 1) * CARD_GAP;
            float startX = -totalW / 2f + CARD_W / 2f;

            for (int i = 0; i < MAX_CHOICES; i++)
            {
                BuildCard(contentParent, i, startX + i * (CARD_W + CARD_GAP));
            }

            // 离开按钮（底部）
            var (leaveBtn, leaveLabel) = UIFactory.CreateButtonWithRef(contentParent, "离开", 120, 36, null, 18, "BtnLeave");
            var leaveRt = leaveBtn.GetComponent<RectTransform>();
            leaveRt.anchorMin = new Vector2(0.5f, 0);
            leaveRt.anchorMax = new Vector2(0.5f, 0);
            leaveRt.pivot = new Vector2(0.5f, 0);
            leaveRt.anchoredPosition = new Vector2(0, 12);
            btnLeave = leaveBtn;
        }

        /// <summary>构建单张卡片</summary>
        private void BuildCard(Transform parent, int index, float centerX)
        {
            var cardObj = UIFactory.CreateUI(parent, $"Card_{index}");
            var cardImg = cardObj.AddComponent<Image>();
            cardImg.color = UIColors.CardBg;
            var cardRt = cardObj.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(CARD_W, CARD_H);
            cardRt.anchoredPosition = new Vector2(centerX, 10);

            // 卡片边框
            var border = UIFactory.CreateUI(cardObj.transform, "Border");
            var borderImg = border.AddComponent<Image>();
            borderImg.color = UIColors.BorderColor;
            var borderRt = border.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = new Vector2(-1, -1);
            borderRt.offsetMax = new Vector2(1, 1);
            // 边框在背景后面
            border.transform.SetAsFirstSibling();

            // 卡片按钮（覆盖整张卡片）
            var btn = cardObj.AddComponent<Button>();
            var btnColors = btn.colors;
            btnColors.normalColor = UIColors.ButtonNormal;
            btnColors.highlightedColor = UIColors.ButtonHover;
            btnColors.pressedColor = UIColors.ButtonPress;
            btnColors.fadeDuration = 0.1f;
            btn.colors = btnColors;

            // 卡片名称
            var nameText = UIFactory.CreateText(cardObj.transform, "", 16, UIColors.Gold,
                TextAnchor.MiddleCenter, true, "CardName");
            var nameRt = nameText.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0, 0.5f);
            nameRt.anchorMax = new Vector2(1, 1);
            nameRt.offsetMin = new Vector2(6, 0);
            nameRt.offsetMax = new Vector2(-6, -4);

            // 卡片描述
            var descTextCard = UIFactory.CreateText(cardObj.transform, "", 13, UIColors.TextDim,
                TextAnchor.MiddleCenter, false, "CardDesc");
            var descCardRt = descTextCard.GetComponent<RectTransform>();
            descCardRt.anchorMin = new Vector2(0, 0);
            descCardRt.anchorMax = new Vector2(1, 0.5f);
            descCardRt.offsetMin = new Vector2(6, 4);
            descCardRt.offsetMax = new Vector2(-6, 0);

            cardObjs[index] = cardObj;
            cardNameTexts[index] = nameText;
            cardDescTexts[index] = descTextCard;
            cardBtns[index] = btn;
        }

        protected override void OnWindowOpen()
        {
            RefreshUI();
        }

        protected override void OnWindowClose()
        {
            shrineSkills = null;
            merchantItems = null;
        }

        // ============ 事件绑定 ============

        protected override void BindEvents()
        {
            base.BindEvents(); // 关闭按钮

            if (btnLeave != null)
            {
                btnLeave.onClick.AddListener(() =>
                {
                    PlayUIClick();
                    UIManager.Instance.CloseWindow();
                });
            }
        }

        // ============ 刷新UI ============

        protected override void RefreshUI()
        {
            base.RefreshUI();

            if (mode == ChooseMode.Shrine)
            {
                RefreshShrine();
            }
            else
            {
                RefreshMerchant();
            }

            // 离开按钮
            if (btnLeave != null)
            {
                btnLeave.interactable = !hasChosen;
            }
        }

        /// <summary>刷新神殿模式</summary>
        private void RefreshShrine()
        {
            var player = GameManager.Instance.Player;

            if (isFallbackHeal)
            {
                // 兜底恢复HP模式：显示恢复选项
                if (descText != null)
                    descText.text = "你已领悟所有技能，神殿赐予你恢复之力";

                // 只显示一张居中的恢复卡片
                float totalW = CARD_W;
                float startX = 0f;
                for (int i = 0; i < MAX_CHOICES; i++)
                {
                    if (i == 0)
                    {
                        cardObjs[i].SetActive(true);
                        var rt = cardObjs[i].GetComponent<RectTransform>();
                        rt.anchoredPosition = new Vector2(startX, 10);
                        cardNameTexts[i].text = $"恢复 {player.maxHp / 2} HP";
                        cardNameTexts[i].color = UIColors.Green;
                        cardDescTexts[i].text = "接受神殿的赐福";
                        cardBtns[i].interactable = !hasChosen;
                        cardBtns[i].onClick.RemoveAllListeners();
                        cardBtns[i].onClick.AddListener(() =>
                        {
                            PlayUIClick();
                            AcceptFallbackHeal(player);
                        });
                    }
                    else
                    {
                        cardObjs[i].SetActive(false);
                    }
                }
                return;
            }

            // 正常三选一技能
            if (descText != null)
                descText.text = "选择一项技能免费学习";

            float totalW = Mathf.Min(shrineSkills.Count, MAX_CHOICES) * CARD_W
                + (Mathf.Min(shrineSkills.Count, MAX_CHOICES) - 1) * CARD_GAP;
            float startX = -totalW / 2f + CARD_W / 2f;

            for (int i = 0; i < MAX_CHOICES; i++)
            {
                if (i < shrineSkills.Count)
                {
                    cardObjs[i].SetActive(true);
                    var rt = cardObjs[i].GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(startX + i * (CARD_W + CARD_GAP), 10);

                    var skill = shrineSkills[i];
                    string categoryStr = skill.GetCategoryName();
                    cardNameTexts[i].text = $"[{categoryStr}] {skill.name}";
                    cardNameTexts[i].color = UIColors.Gold;
                    cardDescTexts[i].text = skill.desc;

                    cardBtns[i].interactable = !hasChosen;
                    cardBtns[i].onClick.RemoveAllListeners();
                    int capturedId = skill.id;
                    string capturedName = skill.name;
                    cardBtns[i].onClick.AddListener(() =>
                    {
                        PlayUIClick();
                        ChooseShrineSkill(capturedId, capturedName, player);
                    });
                }
                else
                {
                    cardObjs[i].SetActive(false);
                }
            }
        }

        /// <summary>刷新商人模式</summary>
        private void RefreshMerchant()
        {
            var player = GameManager.Instance.Player;

            if (descText != null)
                descText.text = hasChosen ? "已选购，商人离去了" : "商品8折优惠，选购1件";

            float totalW = Mathf.Min(merchantItems.Count, MAX_CHOICES) * CARD_W
                + (Mathf.Min(merchantItems.Count, MAX_CHOICES) - 1) * CARD_GAP;
            float startX = -totalW / 2f + CARD_W / 2f;

            for (int i = 0; i < MAX_CHOICES; i++)
            {
                if (i < merchantItems.Count)
                {
                    cardObjs[i].SetActive(true);
                    var rt = cardObjs[i].GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(startX + i * (CARD_W + CARD_GAP), 10);

                    var item = merchantItems[i];
                    int discountPrice = Mathf.RoundToInt(item.price * MERCHANT_DISCOUNT);

                    cardNameTexts[i].text = item.name;
                    cardNameTexts[i].color = UIColors.Gold;
                    string descStr = FormatItemShort(item);
                    cardDescTexts[i].text = $"{descStr}\n{discountPrice}金";

                    bool canBuy = !hasChosen && player.gold >= discountPrice
                        && player.bagItems.Count < player.maxBagSize;

                    cardBtns[i].interactable = canBuy;
                    cardBtns[i].onClick.RemoveAllListeners();

                    if (hasChosen)
                    {
                        // 已选：全部禁用
                        cardObjs[i].GetComponent<Image>().color = UIColors.RowOdd;
                    }
                    else
                    {
                        cardObjs[i].GetComponent<Image>().color = UIColors.CardBg;
                        int capturedId = item.id;
                        int capturedPrice = discountPrice;
                        string capturedName = item.name;
                        cardBtns[i].onClick.AddListener(() =>
                        {
                            PlayUIClick();
                            ChooseMerchantItem(capturedId, capturedPrice, capturedName, player);
                        });
                    }
                }
                else
                {
                    cardObjs[i].SetActive(false);
                }
            }
        }

        // ============ 选择逻辑 ============

        /// <summary>神殿：选择技能免费学习</summary>
        private void ChooseShrineSkill(int skillId, string skillName, PlayerData player)
        {
            if (hasChosen) return;

            // 槽满检查（在设置 hasChosen 之前）
            if (player.learnedSkills.Count >= player.maxSkillSlots)
            {
                ShowTip($"技能槽已满({player.maxSkillSlots}/{player.maxSkillSlots})，无法学习");
                return;
            }

            hasChosen = true;

            if (player.LearnSkill(skillId))
            {
                Debug.Log($"[神殿] 免费习得技能 {skillName}(id={skillId})");
                ShowTip($"学会了 {skillName}！");
            }
            else
            {
                ShowTip("学习失败");
            }

            // 延迟关闭
            StartCoroutine(DelayedClose(0.6f));
        }

        /// <summary>神殿兜底：接受恢复50%HP</summary>
        private void AcceptFallbackHeal(PlayerData player)
        {
            if (hasChosen) return;
            hasChosen = true;

            int hpHeal = player.maxHp / 2;
            player.hp = Mathf.Min(player.hp + hpHeal, player.maxHp);
            Debug.Log($"[神殿] 兜底恢复HP {hpHeal}");
            ShowTip($"恢复了 {hpHeal} HP！");

            StartCoroutine(DelayedClose(0.6f));
        }

        /// <summary>商人：选择物品购买</summary>
        private void ChooseMerchantItem(int itemId, int price, string itemName, PlayerData player)
        {
            if (hasChosen) return;

            // 金币检查
            if (player.gold < price)
            {
                ShowTip("金币不足");
                return;
            }
            // 背包空间检查
            if (player.bagItems.Count >= player.maxBagSize)
            {
                ShowTip("背包已满");
                return;
            }

            hasChosen = true;
            player.gold -= price;
            player.bagItems.Add(itemId);
            Debug.Log($"[商人] 购买 {itemName}(id={itemId}) 花费={price}");
            ShowTip($"购买了 {itemName}！");

            RefreshUI();
            StartCoroutine(DelayedClose(0.6f));
        }

        // ============ 工具方法 ============

        /// <summary>显示AutoTip提示</summary>
        private void ShowTip(string content)
        {
            if (TipManager.Instance != null)
            {
                TipManager.Instance.ShowAutoTip(content, new Vector2(0, 350f), 2.5f);
            }
        }

        /// <summary>延迟关闭窗口协程</summary>
        private System.Collections.IEnumerator DelayedClose(float delay)
        {
            yield return new WaitForSeconds(delay);
            UIManager.Instance?.CloseWindow();
        }

        /// <summary>格式化物品简要属性</summary>
        private static string FormatItemShort(ItemConfig item)
        {
            string stat = "";
            if (item.atk > 0) stat += $"攻+{item.atk} ";
            if (item.def > 0) stat += $"防+{item.def} ";
            if (item.hp > 0) stat += $"HP+{item.hp} ";
            if (item.mp > 0) stat += $"MP+{item.mp} ";
            if (item.speed > 0) stat += $"速+{item.speed} ";
            if (item.crit > 0) stat += $"暴+{item.crit} ";
            if (item.dodge > 0) stat += $"闪+{item.dodge} ";
            if (item.restoreHp > 0) stat += $"恢复HP {item.restoreHp} ";
            if (item.restoreMp > 0) stat += $"恢复MP {item.restoreMp} ";
            return string.IsNullOrEmpty(stat) ? item.desc : stat.TrimEnd();
        }
    }
}
