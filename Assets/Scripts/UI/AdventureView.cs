using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Game.UI.SlateUI;

namespace Game.UI
{
    public sealed class AdventureView : MonoBehaviour
    {
        public AdventureSession Session
        {
            get; private set;
        }
        private RectTransform canvas;
        private RectTransform page;
        private bool title = true;
        private string modal = "";
        private string error = "";
        private string heroName = "旅人";
        private int profession;
        private int selectedRoom = -1;
        private int shownFloor;
        private Action confirmed;
        private string confirmation;
        private bool announcingCompletion;
        private Texture2D titleArt;
        private WorldBoard board;
        private int selectedSkill;
        private int guildProfession = -1;
        private Text hintLabel;
        private RectTransform detailRoot;
        private RectTransform roomTip;
        private float bagOffset;
        private int previewRoom = -1;
        private int previousBattleRoom = -1;
        private int previousBattleFloor;
        private int previousRound;
        private int previousEnemyHp;
        private int previousHeroHp;

        public void Initialize(AdventureSession session)
        {
            Session = session;
            titleArt = Resources.Load<Texture2D>("Art/TitleGate");
            if (titleArt == null)
            {
                throw new InvalidOperationException("[界面] 缺少标题插画 Art/TitleGate");
            }
            Session.Changed += OnChanged;
            GameObject root = new GameObject("冒险界面", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            Canvas component = root.GetComponent<Canvas>();
            component.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            canvas = root.GetComponent<RectTransform>();
            RectTransform backdrop = Box(canvas, "全屏底色", 0, 0, 1600, 900, Background).rectTransform;
            backdrop.anchorMin = Vector2.zero;
            backdrop.anchorMax = Vector2.one;
            backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
            if (EventSystem.current == null)
            {
                new GameObject("输入事件", typeof(EventSystem), typeof(StandaloneInputModule));
            }
            // 地图和背包共享点击容差，避免轻微手部移动变成拖动。
            EventSystem.current.pixelDragThreshold = 16;
            Render();
        }

        private void OnDestroy()
        {
            if (Session != null)
            {
                Session.Changed -= OnChanged;
            }
        }

        private void OnChanged()
        {
            string feedback = "";
            if (Session.Data != null && !title)
            {
                BattleData battle = Session.Data.battle;
                if (battle != null)
                {
                    if (battle.room == previousBattleRoom && previousBattleFloor == Session.Data.currentFloor && battle.round != previousRound)
                    {
                        int damage = Math.Max(0, previousEnemyHp - battle.enemy.hp);
                        int loss = Math.Max(0, previousHeroHp - battle.hero.hp);
                        feedback = damage > 0 ? $"造成 {damage} 伤害" : "";
                        if (loss > 0)
                        {
                            feedback = $"{feedback}    受到 {loss} 伤害".Trim();
                        }
                    }
                    previousBattleRoom = battle.room;
                    previousBattleFloor = Session.Data.currentFloor;
                    previousRound = battle.round;
                    previousEnemyHp = battle.enemy.hp;
                    previousHeroHp = battle.hero.hp;
                }
                else if (previousBattleRoom >= 0)
                {
                    previousBattleRoom = -1;
                }
                if (Session.Data.currentFloor != shownFloor)
                {
                    selectedRoom = -1;
                    previewRoom = -1;
                    shownFloor = Session.Data.currentFloor;
                }
                if (Session.Data.completed && !announcingCompletion)
                {
                    announcingCompletion = true;
                    modal = "通关";
                }
            }
            Render();
            if (feedback != "" && modal == "")
            {
                Text popup = Label(page, feedback, 430, 320, 700, 64, 23, Gold, TextAnchor.MiddleCenter);
                Shadow shadow = popup.gameObject.AddComponent<Shadow>();
                shadow.effectColor = Background;
                shadow.effectDistance = new Vector2(2, -2);
                popup.gameObject.AddComponent<CombatFeedback>();
            }
        }

        private void Safe(Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                error = exception.Message;
                Debug.LogError($"[界面] {exception}");
                Render();
            }
        }

        private void Open(string name)
        {
            modal = name;
            Render();
        }

        private void Render()
        {
            HideRoomTip();
            if (page != null)
            {
                ScrollRect bagScroll = page.GetComponentsInChildren<ScrollRect>().FirstOrDefault(r => r.name == "背包列表");
                if (bagScroll != null)
                {
                    bagOffset = bagScroll.content.anchoredPosition.y;
                }
            }
            if (board != null)
            {
                board.transform.SetParent(canvas, false);
            }
            if (page != null)
            {
                page.gameObject.SetActive(false);
                Destroy(page.gameObject);
            }
            page = Rect(canvas, "当前页面", 0, 0, 1600, 900);
            page.anchorMin = page.anchorMax = new Vector2(0.5f, 0.5f);
            page.pivot = new Vector2(0.5f, 0.5f);
            page.anchoredPosition = Vector2.zero;
            Box(page, "背景", 0, 0, 1600, 900, Background);
            if (title)
            {
                if (board != null)
                {
                    Destroy(board.gameObject);
                    board = null;
                }
                DrawTitle();
            }
            else
            {
                DrawGame();
            }
            if (modal != "")
            {
                DrawModal();
            }
            if (confirmed != null)
            {
                DrawConfirmation();
            }
            if (error != "")
            {
                RectTransform box = Overlay("无法完成操作", 760, 320, false);
                Label(box, error, 32, 95, 696, 130, 20, Red);
                Button(box, "错误确认", "知道了", 520, 250, 200, 44, () => { error = ""; Render(); });
            }
        }

        private void DrawTitle()
        {
            RawImage art = Rect(page, "地牢入口插画", 0, 0, 1600, 900).gameObject.AddComponent<RawImage>();
            art.texture = titleArt;
            art.raycastTarget = false;
            Box(page, "标题侧线", 112, 170, 3, 420, Gold);
            Label(page, "方块与\n地下城", 145, 227, 700, 216, 84, Ink);
            Button(page, "继续冒险", "继续冒险    →", 151, 556, 380, 62, () => Safe(() =>
            {
                Session.Continue();
                title = false;
                shownFloor = Session.Data.currentFloor;
                selectedRoom = -1;
                previewRoom = -1;
                announcingCompletion = Session.Data.completed;
                modal = "";
                Render();
            }), Session.Store.Exists, true);
            Button(page, "新的旅程", "新的旅程", 151, 632, 380, 58, () => Open("创建"));
            Button(page, "标题设置", "设置", 151, 711, 180, 48, () => Open("设置"));
            Button(page, "退出游戏", "退出游戏", 347, 711, 184, 48, Application.Quit);
        }

        private void DrawGame()
        {
            AdventureData data = Session.Data;
            bool town = data.inTown;
            Label(page, "方块与地下城", 24, 22, 230, 38, 23, Gold);
            Label(page, town ? "余烬镇" : $"{Session.Config.Floor(data.currentFloor).region}   /   第 {data.currentFloor:00} 层", 260, 24, 790, 36, 23);


            Button(page, "设置入口", "设置", 1338, 27, 100, 45, () => Open("设置"));
            Button(page, "标题入口", "标题", 1452, 27, 110, 45, () =>
            {
                Session.RetrySave();
                if (Session.SaveError == "")
                {
                    title = true;
                    modal = "";
                    Render();
                }
            });
            Box(page, "分隔", 24, 76, 1552, 1, Border);
            DrawHero();
            if (town)
            {
                DrawTown();
            }
            else
            {
                DrawDungeon();
            }
            DrawJournal();
            DrawBag();
            DrawRelicBar();
            hintLabel = Label(page, Session.SaveError == "" ? Session.Notice : Session.SaveError, 260, 865, Session.SaveError == "" ? 1300 : 1060, 27, 15, Session.SaveError == "" ? Muted : Red);
            if (Session.SaveError != "")
            {
                Button(page, "重试保存", "重试保存", 1360, 846, 200, 40, Session.RetrySave);
            }
        }

        private void BindTip(GameObject target, string text)
        {
            HoverInfo info = target.AddComponent<HoverInfo>();
            info.host = page;
            info.text = text;
        }

        private void HideRoomTip()
        {
            if (roomTip != null)
            {
                roomTip.gameObject.SetActive(false);
                Destroy(roomTip.gameObject);
                roomTip = null;
            }
        }

        private void DrawHero()
        {
            HeroData hero = Session.Data.hero;
            Stats stats = hero.GetStats(Session.Config);
            ProfessionDef professionDef = Session.Config.Profession(hero.profession);
            RectTransform box = CardBox(page, "角色状态", 24, 100, 212, 296);
            Button portrait = Button(box, "角色区块", professionDef.symbol, 14, 14, 56, 62, TargetSelf, true, selectedSkill == -1 || selectedSkill > 0 && IsSelfSkill(Session.Config.Skill(selectedSkill)));
            BindTip(portrait.gameObject, $"{hero.name} · {professionDef.name}\n经验 {hero.experience}/{hero.ExperienceNeeded}");
            Label(box, hero.name, 82, 16, 122, 28, 21);
            Label(box, $"{professionDef.name} · {hero.level}级", 82, 49, 122, 24, 15, Muted);
            Label(box, $"生命  {hero.hp}/{stats.hp}", 14, 96, 184, 25, 18, Red);
            Bar(box, "生命条", hero.hp, stats.hp, 14, 128, 184, Red);
            Label(box, $"法力  {hero.mp}/{stats.mp}", 14, 150, 184, 25, 18, Blue);
            Bar(box, "法力条", hero.mp, stats.mp, 14, 182, 184, Blue);
            Image attributes = Box(box, "属性提示", 14, 208, 184, 44, Color.clear);
            attributes.raycastTarget = true;
            Label(attributes.transform, $"物攻 {stats.attack}    法攻 {stats.magic}", 0, 0, 184, 25, 16);
            BindTip(attributes.gameObject, $"物攻 {stats.attack} · 法攻 {stats.magic}\n防御 {stats.defense} · 速度 {stats.speed}\n物攻用于破墙，法攻用于破除屏障。\n生命上限 {stats.hp} · 法力上限 {stats.mp}");
            Label(box, $"金币  {hero.gold}", 14, 258, 184, 26, 19, Gold);
        }

        private void EnsureWorld(bool town)
        {
            if (board == null || board.IsTown != town || (!town && board.Floor != Session.Data.Current))
            {
                if (board != null)
                {
                    Destroy(board.gameObject);
                }
                RectTransform root = Rect(page, "世界场景", 260, 100, 1040, 660);
                board = root.gameObject.AddComponent<WorldBoard>();
                if (town)
                {
                    board.Town(destination =>
                    {
                        if (destination == "旅馆")
                        {
                            Session.Rest();
                        }
                        else if (destination == "出发")
                        {
                            Session.EnterFloor(1);
                        }
                        else
                        {
                            Open(destination);
                        }
                    });
                }
                else
                {
                    board.Dungeon(Session, SelectRoom, PreviewRoom, () => SelectSkill(0));
                }
            }
            board.transform.SetParent(page, false);
            RectTransform rect = board.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(260, -100);
            rect.sizeDelta = new Vector2(1040, 660);
            if (!town)
            {
                board.Refresh(Session, selectedSkill);
            }
        }

        private void DrawTown()
        {
            EnsureWorld(true);
            DrawMapControls();
            RectTransform box = CardBox(page, "城镇目标", 1324, 100, 252, 180);
            Label(box, "余烬镇", 16, 16, 220, 30, 22, Gold);
            Label(box, $"最深抵达  {Session.Data.unlockedFloor}层", 16, 58, 220, 26, 18, Muted);
            Label(box, Session.Data.completed ? "地下城已通关" : "目标：击败第十层首领", 16, 112, 220, 45, 17, Muted);
        }

        private void DrawMapControls()
        {
            Button all = Button(page, "查看全图", "全图", 1028, 26, 76, 38, () => board.Viewport.ShowAll());
            BindTip(all.gameObject, "滚轮：以鼠标位置缩放\n左键拖动地图（中键也可）\n全图：查看整个楼层");
            string[] arrows = { "←", "↑", "↓", "→" };
            Vector2[] directions = { Vector2.right, Vector2.down, Vector2.up, Vector2.left };
            for (int i = 0; i < 4; i++)
            {
                Vector2 direction = directions[i];
                Button move = Button(page, $"移动地图{i}", "", 1210 + i * 29, 27, 27, 38, () => board.Viewport.Pan(direction * 160));
                Label(move.transform, arrows[i], 0, 0, 27, 38, 20, Ink, TextAnchor.MiddleCenter);
            }
            if (!Session.Data.inTown)
            {
                Button(page, "返回城镇", "返城", 1114, 26, 86, 38, () => Session.ReturnToTown(), Session.Data.battle == null);
            }
        }

        private void DrawDungeon()
        {
            EnsureWorld(false);
            DrawMapControls();
            DrawRoomDetail();
            DrawActions();
        }

        private static bool IsSelfSkill(SkillDef skill) => skill.kind == SkillKind.Heal || skill.kind == SkillKind.Shield;

        private void Hint(string text)
        {
            if (hintLabel != null)
            {
                hintLabel.text = text;
            }
        }

        private void SelectSkill(int id)
        {
            selectedSkill = id;
            Render();
        }

        private bool ReadySkill()
        {
            if (selectedSkill > 0 && Session.Data.hero.mp < Session.SkillCost(Session.Config.Skill(selectedSkill)))
            {
                Hint("法力不足。");
                return false;
            }
            return true;
        }

        private void TargetSelf()
        {
            if (Session.Data.battle == null)
            {
                Hint("当前不在战斗中。");
                return;
            }
            if (selectedSkill != -1 && (selectedSkill <= 0 || !IsSelfSkill(Session.Config.Skill(selectedSkill))))
            {
                Hint("请选择自身技能或防御。");
                return;
            }
            if (ReadySkill())
            {
                Session.Act(selectedSkill);
            }
        }

        private void PreviewRoom(RoomData room)
        {
            HideRoomTip();
            if (room == null || modal != "")
            {
                return;
            }
            string text;
            if (room.IsObstacle)
            {
                string attribute = room.kind == RoomKind.Wall ? "物攻" : "法攻";
                Stats stats = Session.Data.hero.GetStats(Session.Config);
                int current = room.kind == RoomKind.Wall ? stats.attack : stats.magic;
                text = room.cleared ? $"{WorldBoard.Name(room.kind)} · 已破坏" : $"{WorldBoard.Name(room.kind)}\n需要{attribute} {room.breakPower} · 当前 {current}";
            }
            else if (!room.cleared && (room.kind == RoomKind.Monster || room.kind == RoomKind.Elite || room.kind == RoomKind.Boss))
            {
                MonsterDef monster = Session.Config.Monster(room.monster);
                Stats stats = Session.EnemyStats(room);
                text = $"{monster.name}\n{monster.description}\n生命 {stats.hp} · 防御 {stats.defense}\n物攻 {stats.attack} · 法攻 {stats.magic} · 速度 {stats.speed}";
            }
            else
            {
                string description = RoomDescription(room);
                text = description == "" ? WorldBoard.Name(room.kind) : $"{WorldBoard.Name(room.kind)}\n{description}";
            }
            roomTip = HoverInfo.Show(page, text, Input.mousePosition);
        }

        private void SelectRoom(RoomData room)
        {
            selectedRoom = room.id;
            bool hostile = room.kind == RoomKind.Monster || room.kind == RoomKind.Elite || room.kind == RoomKind.Boss;
            if (hostile && !room.cleared)
            {
                if (selectedSkill == -1 || (selectedSkill > 0 && IsSelfSkill(Session.Config.Skill(selectedSkill))))
                {
                    Hint("该技能只能对自身使用。");
                    return;
                }
                if (!ReadySkill())
                {
                    return;
                }
                if (Session.Data.battle == null)
                {
                    Session.Explore(room.id);
                }
                if (Session.Data.battle != null && Session.Data.battle.room == room.id)
                {
                    Session.Act(selectedSkill);
                }
                return;
            }
            if (Session.Data.battle != null)
            {
                return;
            }
            if ((room.kind == RoomKind.Merchant || room.kind == RoomKind.Shrine) && !room.cleared)
            {
                modal = "奖励";
                Session.Explore(room.id);
            }
            else if (!room.cleared || room.IsObstacle)
            {
                Session.Explore(room.id);
            }
        }

        private void DrawRoomDetail()
        {
            BattleData battle = Session.Data.battle;
            detailRoot = CardBox(page, "房间详情", 1324, 100, 252, 180);
            if (battle == null)
            {
                int seen = Session.Data.Current.rooms.Count(r => r.revealed);
                Label(detailRoot, $"第 {Session.Data.currentFloor} 层", 16, 16, 220, 30, 23, Gold);
                Label(detailRoot, $"已发现 {seen}/{Session.Data.Current.rooms.Count}", 16, 58, 220, 26, 17, Muted);
                return;
            }
            MonsterDef monster = Session.Config.Monster(battle.monster);
            Label(detailRoot, monster.name, 16, 12, 220, 30, 21, Red);
            Label(detailRoot, $"{battle.enemy.hp}/{battle.stats.hp}    护盾 {battle.enemy.shield}", 16, 48, 220, 25, 16);
            Bar(detailRoot, "敌人生命", battle.enemy.hp, battle.stats.hp, 16, 79, 220, Red);
            Image intent = Box(detailRoot, "敌人意图", 16, 95, 220, 28, Color.clear);
            intent.raycastTarget = true;
            Label(intent.transform, Session.EnemyIntent, 0, 0, 220, 28, 18, Gold);
            BindTip(intent.gameObject, $"第{battle.round}轮\n物攻 {battle.stats.attack} · 法攻 {battle.stats.magic}\n防御 {battle.stats.defense} · 速度 {battle.stats.speed}\n{Effects(battle.enemy)}");
            Button(detailRoot, "选择防御", "防御", 16, 135, 104, 32, () => SelectSkill(-1), true, selectedSkill == -1);
            Button(detailRoot, "撤退", "撤退", 132, 135, 104, 32, () => Session.Escape(), !monster.boss);
        }

        private void DrawActions()
        {
            RectTransform bar = CardBox(page, "技能选择栏", 260, 772, 1040, 82);
            List<SkillDef> skills = Session.Data.hero.skills.Select(Session.Config.Skill).Where(x => x.kind != SkillKind.Passive).ToList();
            Button(bar, "普通攻击", "普通攻击", 8, 4, 120, 34, () => SelectSkill(0), true, selectedSkill == 0);
            for (int index = 0; index < skills.Count; index++)
            {
                SkillDef skill = skills[index];
                int slot = index + 1;
                string label = $"{skill.name}{(Session.Data.hero.SkillRank(skill.id) > 1 ? $"·{Session.Data.hero.SkillRank(skill.id)}" : "")} {Session.SkillCost(skill)}";
                Button button = Button(bar, $"选择{skill.id}", label, 8 + slot % 8 * 128, 4 + slot / 8 * 38, 120, 34, () => SelectSkill(skill.id), true, selectedSkill == skill.id);
                button.GetComponentInChildren<Text>().fontSize = 16;
                WorldPointer pointer = button.gameObject.AddComponent<WorldPointer>();
                BindTip(button.gameObject, $"{skill.name} · 技能等级{Session.Data.hero.SkillRank(skill.id)} · {Session.SkillCost(skill)}法力\n{skill.description}\n{(IsSelfSkill(skill) ? "目标：角色区块" : "目标：敌人区块")}");
                pointer.right = () => SelectSkill(0);
            }
        }

        private void DrawJournal()
        {
            RectTransform box = CardBox(page, "冒险日志", 24, 418, 212, 340);
            Label(box, "近况", 14, 14, 184, 28, 18, Gold);
            List<string> logs = Session.Data.journal;
            List<Text> rows = new List<Text>();
            float usedHeight = 0;
            for (int index = logs.Count - 1; index >= 0; index--)
            {
                string entry = logs[index];
                string brief = entry.Contains("。") ? entry.Substring(0, entry.IndexOf("。") + 1) : entry;
                Text row = Label(box, brief, 14, 46, 184, 60, 15, Muted);
                float height = Mathf.Clamp(Mathf.Ceil(row.preferredHeight) + 2, 20, 60);
                if (usedHeight + height > 280)
                {
                    row.gameObject.SetActive(false);
                    Destroy(row.gameObject);
                    break;
                }
                row.name = "近况条目";
                row.rectTransform.sizeDelta = new Vector2(184, height);
                row.raycastTarget = true;
                BindTip(row.gameObject, entry);
                rows.Add(row);
                usedHeight += height + 6;
            }
            float y = 46;
            for (int index = rows.Count - 1; index >= 0; index--)
            {
                Text row = rows[index];
                row.rectTransform.anchoredPosition = new Vector2(14, -y);
                y += row.rectTransform.rect.height + 6;
            }
        }

        private void Confirm(string message, Action action)
        {
            confirmation = message;
            confirmed = action;
            Render();
        }

        private void DrawConfirmation()
        {
            RectTransform box = Overlay("请确认", 700, 290, false);
            Label(box, confirmation, 30, 90, 640, 100, 21, Ink);
            Button(box, "取消确认", "取消", 230, 217, 190, 44, () => { confirmed = null; Render(); });
            Button(box, "确定", "确定", 450, 217, 210, 44, () =>
            {
                Action action = confirmed;
                confirmed = null;
                Safe(action);
            }, true, true);
        }

        private RectTransform Overlay(string name, float width, float height, bool closable = true)
        {
            Image shade = Box(page, "遮罩", 0, 0, 1600, 900, Hex(0x050b10, 0.88f));
            shade.raycastTarget = true;
            RectTransform box = CardBox(shade.transform, name, (1600 - width) / 2, (900 - height) / 2, width, height);
            box.GetComponent<Image>().raycastTarget = true;
            Label(box, name, 30, 24, width - 130, 44, 29, Gold);
            if (closable)
            {
                Button(box, "关闭弹窗", "关闭", width - 105, 24, 75, 40, () => { modal = ""; Render(); });
            }
            Box(box, "标题分隔", 30, 77, width - 60, 1, Border);
            return box;
        }

        private void DrawModal()
        {
            switch (modal)
            {
                case "创建":
                    DrawCreate();
                    break;
                case "商店":
                    DrawShop();
                    break;
                case "奖励":
                    DrawRewardChoice();
                    break;
                case "公会":
                    DrawGuild();
                    break;
                case "设置":
                    DrawSettings();
                    break;
                case "通关":
                    DrawCompletion();
                    break;
            }
        }

        private void DrawCreate()
        {
            RectTransform box = Overlay("选择你的旅人", 1170, 610);
            for (int index = 0; index < Session.Config.Professions.Count; index++)
            {
                ProfessionDef choice = Session.Config.Professions[index];
                RectTransform card = CardBox(box, choice.name, 30 + index * 226, 159, 210, 297, choice.id == profession ? Hex(0x2b443e) : Card);
                Label(card, choice.symbol, 20, 19, 170, 70, 48, choice.id == profession ? Teal : Gold, TextAnchor.MiddleCenter);
                Label(card, choice.name, 15, 100, 180, 35, 26, Ink, TextAnchor.MiddleCenter);
                Label(card, choice.title, 15, 140, 180, 24, 14, Gold, TextAnchor.MiddleCenter);
                Label(card, choice.description, 20, 177, 170, 65, 16, Muted);
                Button(card, $"职业{choice.id}", choice.id == profession ? "已选择" : "选择", 20, 245, 170, 37, () => { profession = choice.id; Render(); }, true, choice.id == profession);
            }
            Label(box, "旅人姓名", 32, 504, 120, 38, 20, Muted);
            Image inputBox = Box(box, "姓名输入", 166, 493, 360, 52, Background);
            inputBox.raycastTarget = true;
            InputField input = inputBox.gameObject.AddComponent<InputField>();
            Text text = Label(inputBox.transform, "", 14, 8, 330, 36, 23);
            input.textComponent = text;
            input.targetGraphic = inputBox;
            input.characterLimit = 12;
            input.text = heroName;
            input.onValueChanged.AddListener(value => heroName = value);
            Button(box, "开始旅程", "走进余烬镇    →", 771, 493, 365, 56, () =>
            {
                Action start = () =>
                {
                    title = false;
                    modal = "";
                    selectedRoom = -1;
                    previewRoom = -1;
                    selectedSkill = 0;
                    announcingCompletion = false;
                    Session.Create(profession, heroName, unchecked((int)DateTime.UtcNow.Ticks));
                };
                if (Session.Store.Exists)
                {
                    Confirm("开始新的旅程会替换当前的新版本冒险存档。确定重新开始？", start);
                }
                else
                {
                    Safe(start);
                }
            }, true, true);
        }

        private void DrawShop()
        {
            RectTransform box = Overlay("旅人商店", 1030, 710);
            Label(box, $"金币  {Session.Data.hero.gold}", 30, 97, 970, 35, 20, Gold);
            List<ItemDef> items = Session.Config.Items.OrderBy(x => x.kind == ItemKind.Potion ? 0 : 1).ThenBy(x => x.level).ThenBy(x => x.kind).ToList();
            RectTransform content = Scroll(box, "商品列表", 30, 153, 970, 522, items.Count * 100);
            for (int index = 0; index < items.Count; index++)
            {
                ItemDef item = items[index];
                RectTransform row = CardBox(content, $"商品{item.id}", 0, index * 100, 940, 88, Card);
                Label(row, $"{item.name}   ·   等级{item.level}", 18, 12, 640, 30, 21);
                Label(row, item.kind == ItemKind.Potion ? item.description : Bonus(item.bonus), 18, 49, 650, 30, 17, Muted);
                Button(row, $"购买{item.id}", $"{item.price} 金币  购买", 715, 20, 205, 47, () => Session.Buy(item.id), Session.Data.battle == null && Session.Data.hero.level >= item.level && Session.Data.hero.gold >= item.price);
            }
        }

        private void DrawBag()
        {
            RectTransform box = CardBox(page, "主界面背包", 1324, 300, 252, 554);
            HeroData hero = Session.Data.hero;
            Label(box, "背包", 14, 12, 220, 28, 21, Gold);
            int[] equipment = { hero.weapon, hero.armor, hero.accessory };
            for (int slot = 0; slot < 3; slot++)
            {
                int id = equipment[slot];
                Image cell = Box(box, $"装备槽{slot}", 14 + slot * 76, 52, 68, 58, Card);
                cell.raycastTarget = true;
                Label(cell.transform, new[] { "武器", "防具", "饰品" }[slot], 2, 4, 64, 22, 14, Muted, TextAnchor.MiddleCenter);
                Label(cell.transform, id == 0 ? "—" : Session.Config.Item(id).name, 2, 29, 64, 26, 13, Ink, TextAnchor.MiddleCenter);
                BindTip(cell.gameObject, id == 0 ? "尚未装备" : $"{Session.Config.Item(id).name}\n{Bonus(Session.Config.Item(id).bonus)}");
            }
            List<ItemStack> items = hero.bag.OrderBy(x => Session.Config.Item(x.id).kind).ThenBy(x => x.id).ToList();
            RectTransform content = Scroll(box, "背包列表", 10, 126, 232, 413, Mathf.Ceil(items.Count / 3f) * 88);
            content.anchoredPosition = new Vector2(0, Mathf.Clamp(bagOffset, 0, Mathf.Max(0, content.rect.height - 413)));
            for (int index = 0; index < items.Count; index++)
            {
                ItemStack stack = items[index];
                ItemDef item = Session.Config.Item(stack.id);
                bool potion = item.kind == ItemKind.Potion;
                Button cell = Button(content, $"使用{item.id}", "", index % 3 * 70, index / 3 * 88, 65, 80, () => { if (potion) { Session.UsePotion(item.id); } else { Session.Equip(item.id); } }, potion || Session.Data.battle == null && hero.level >= item.level);
                Label(cell.transform, item.name, 2, 6, 61, 42, 14, potion ? (item.heal > 0 ? Red : Blue) : Gold, TextAnchor.MiddleCenter);
                Label(cell.transform, $"×{stack.count}", 3, 51, 59, 23, 14, Muted, TextAnchor.MiddleCenter);
                BindTip(cell.gameObject, $"{item.name} · 等级{item.level}\n{(potion ? item.description : Bonus(item.bonus))}\n左键：{(potion ? "使用" : "装备（战斗外）")}\n{(Session.Data.inTown ? $"右键：出售一个，获得{item.price / 2}金币" : "回城后可以出售")}");
                WorldPointer pointer = cell.gameObject.AddComponent<WorldPointer>();
                pointer.right = () => { if (Session.Data.inTown) { Session.Sell(item.id); } };
            }
        }

        private void DrawGuild()
        {
            RectTransform box = Overlay("冒险者公会", 1030, 730);
            HeroData hero = Session.Data.hero;
            Label(box, $"等级 {hero.level}    ·    金币 {hero.gold}", 30, 97, 970, 35, 20, Gold);
            Button(box, "技能全部", "全部", 30, 145, 145, 42, () => { guildProfession = -1; Render(); }, true, guildProfession == -1);
            foreach (ProfessionDef definition in Session.Config.Professions)
            {
                int id = definition.id;
                Button(box, $"技能分类{id}", definition.name, 190 + id * 160, 145, 145, 42, () => { guildProfession = id; Render(); }, true, guildProfession == id);
            }
            List<SkillDef> skills = Session.Config.Skills.Where(x => x.id < 900 && (guildProfession < 0 || x.profession == guildProfession)).ToList();
            RectTransform content = Scroll(box, "技能列表", 30, 210, 970, 480, skills.Count * 116);
            for (int index = 0; index < skills.Count; index++)
            {
                SkillDef skill = skills[index];
                bool known = hero.skills.Contains(skill.id);
                RectTransform row = CardBox(content, $"技能{skill.id}", 0, index * 116, 940, 104, Card);
                Label(row, $"{skill.name}  ·  {Session.Config.Profession(skill.profession).name}  ·  {(skill.kind == SkillKind.Passive ? "被动" : $"{Session.SkillCost(skill)}法力")}  ·  等级{skill.level}", 18, 12, 696, 30, 20, known ? Teal : Ink);
                Label(row, skill.description, 18, 52, 696, 45, 17, Muted);
                Button(row, $"学习{skill.id}", known ? "已掌握" : skill.price == 0 ? "免费研习" : $"{skill.price}金币 学习", 731, 28, 190, 48, () => Session.Learn(skill.id), !known && Session.Data.inTown && hero.level >= skill.level && hero.gold >= skill.price && (skill.prerequisite == 0 || hero.skills.Contains(skill.prerequisite)), !known);
            }
        }

        private void DrawRelicBar()
        {
            RectTransform box = CardBox(page, "宝物增益栏", 24, 774, 212, 80);
            RectTransform content = Scroll(box, "宝物图标", 6, 6, 200, 68, Math.Max(68, ((Session.Data.hero.relics.Count + 4) / 5) * 38));
            for (int i = 0; i < Session.Data.hero.relics.Count; i++)
            {
                RelicDef relic = Session.Config.Relic(Session.Data.hero.relics[i]);
                Button icon = Button(content, $"宝物{relic.id}", "", i % 5 * 39, i / 5 * 38, 35, 34, () => { });
                RectTransform symbol = Rect(icon.transform, "宝物符号", 5, 4, 25, 25);
                DungeonGlyph glyph = symbol.gameObject.AddComponent<DungeonGlyph>();
                RoomKind[] symbols = { RoomKind.Shrine, RoomKind.Spring, RoomKind.Chest, RoomKind.Barrier };
                glyph.kind = symbols[i % symbols.Length];
                glyph.color = i % 2 == 0 ? Gold : Teal;
                glyph.raycastTarget = false;
                BindTip(icon.gameObject, $"{relic.name}\n永久增益\n{relic.description}");
            }
        }

        private void DrawRewardChoice()
        {
            RoomData room = Session.Data.Current.Room(selectedRoom);
            RectTransform box = Overlay(room.kind == RoomKind.Merchant ? "旅途商人" : "神殿", 1000, 500);
            if (room.kind == RoomKind.Merchant)
            {
                Label(box, $"金币 {Session.Data.hero.gold}", 30, 95, 930, 32, 20, Muted);
            }
            for (int i = 0; i < room.offers.Count; i++)
            {
                int index = i;
                RewardData offer = room.offers[i];
                Button card = Button(box, $"奖励{i}", "", 30 + i * 315, 150, 300, 250, () =>
                {
                    if (Session.ChooseReward(room.id, index))
                    {
                        modal = "";
                        Render();
                    }
                }, Session.Data.hero.gold >= offer.price);
                string details = Session.RewardText(offer);
                string brief = string.Join("\n", details.Split('\n').Take(3));
                Label(card.transform, brief, 18, 20, 264, 150, 19, Ink);
                BindTip(card.gameObject, details);
                Label(card.transform, offer.price == 0 ? "领取" : $"{offer.price} 金币", 18, 207, 264, 30, 22, Gold);
            }
            if (room.kind == RoomKind.Merchant)
            {
                Button(box, "放弃交易", "放弃交易", 330, 430, 340, 42, () => { modal = ""; Session.PassMerchant(room.id); });
            }
        }

        private void DrawSettings()
        {
            RectTransform box = Overlay("设置", 730, 535);
            SettingsManager settings = SettingsManager.Instance;
            VolumeSlider(box, "音乐音量", 117, settings.MusicVolume, value => { settings.MusicVolume = value; settings.ApplyAudio(); settings.Save(); });
            VolumeSlider(box, "音效音量", 217, settings.SfxVolume, value => { settings.SfxVolume = value; settings.ApplyAudio(); settings.Save(); });
            Label(box, "显示模式", 30, 323, 160, 33, 21, Muted);
            Button(box, "窗口1280", "1280 × 720", 30, 381, 210, 54, () => settings.SetDisplay(1280, 720, false));
            Button(box, "窗口1600", "1600 × 900", 260, 381, 210, 54, () => settings.SetDisplay(1600, 900, false));
            Button(box, "全屏模式", "全屏", 490, 381, 210, 54, () => settings.SetDisplay(Screen.currentResolution.width, Screen.currentResolution.height, true));
        }

        private static void VolumeSlider(Transform parent, string name, float y, float value, UnityEngine.Events.UnityAction<float> change)
        {
            Label(parent, name, 30, y, 180, 36, 21);
            Image track = Box(parent, name, 230, y + 6, 460, 24, Background);
            track.raycastTarget = true;
            Slider slider = track.gameObject.AddComponent<Slider>();
            Image fill = Box(track.transform, "音量填充", 0, 0, 460, 24, Teal);
            Image handle = Box(track.transform, "音量滑块", 0, -6, 18, 36, Gold);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            handle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            handle.rectTransform.anchoredPosition = Vector2.zero;
            handle.raycastTarget = true;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = value;
            slider.onValueChanged.AddListener(change);
        }

        private void DrawCompletion()
        {
            RectTransform box = Overlay("晨光重返余烬镇", 870, 480);
            Label(box, "你打通了地下城。", 35, 109, 800, 57, 37, Teal, TextAnchor.MiddleCenter);
            Label(box, $"{Session.Data.hero.name} · {Session.Config.Profession(Session.Data.hero.profession).name}\n等级 {Session.Data.hero.level}   /   宝物 {Session.Data.hero.relics.Count} 件\n\n你带回的不只是战利品，还有属于城镇的明天。", 45, 195, 780, 155, 23, Ink, TextAnchor.MiddleCenter);
            Button(box, "通关返回", "返回城镇", 220, 389, 430, 54, () => { modal = ""; Session.ReturnToTown(); }, true, true);
        }

        private string RoomDescription(RoomData room)
        {
            if (room.cleared && room.kind != RoomKind.Stairs && room.kind != RoomKind.Merchant)
            {
                return "已清理";
            }
            switch (room.kind)
            {
                case RoomKind.Boss:
                    MonsterDef monster = Session.Config.Monster(room.monster);
                    return $"{monster.name}\n{monster.description}\n\n不可撤退";
                case RoomKind.Merchant:
                    return "";
                case RoomKind.Stairs:
                    return Session.Data.Current.BossCleared ? "前往下一层" : "击败本层首领后开启";
                case RoomKind.Spring:
                    return "恢复50%最大生命与法力\n返城后恢复可用";
                case RoomKind.Shrine:
                    return "";
                default:
                    return "";
            }
        }

        private static string Effects(FighterData fighter)
        {
            string result = "";
            if (fighter.poisonTurns > 0)
            {
                result += $"中毒 {fighter.poisonTurns}轮  ";
            }
            if (fighter.bleedTurns > 0)
            {
                result += $"流血 {fighter.bleedTurns}轮  ";
            }
            if (fighter.stun > 0)
            {
                result += "受控  ";
            }
            return result == "" ? "无异常状态" : result;
        }


    }

    public sealed class CombatFeedback : MonoBehaviour
    {
        private float elapsed;
        private Text label;
        private RectTransform rect;

        private void Awake()
        {
            label = GetComponent<Text>();
            rect = GetComponent<RectTransform>();
        }

        private void Update()
        {
            elapsed += Time.unscaledDeltaTime;
            rect.anchoredPosition += Vector2.up * (18 * Time.unscaledDeltaTime);
            Color color = label.color;
            color.a = 1 - Mathf.Clamp01((elapsed - 0.65f) / 0.45f);
            label.color = color;
            if (elapsed > 1.1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
