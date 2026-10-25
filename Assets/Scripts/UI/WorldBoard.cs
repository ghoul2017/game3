using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Game.UI.SlateUI;

namespace Game.UI
{
    public sealed class WorldBoard : MonoBehaviour
    {
        private sealed class RoomVisual
        {
            public RoomData room;
            public Image floor;
            public Image fog;
            public Image glow;
            public Button button;
            public Text name;
            public Text health;
            public DungeonGlyph glyph;
        }

        public FloorData Floor
        {
            get; private set;
        }
        public bool IsTown
        {
            get; private set;
        }
        private readonly List<RoomVisual> visuals = new List<RoomVisual>();
        private Action<RoomData> click;
        private Action<RoomData> hover;
        private Action cancel;
        private int hovered = -1;
        private float unit;
        private float left;
        private float top;
        private const float Width = 1040;
        private const float Height = 660;
        private RectTransform space;
        public MapViewport Viewport
        {
            get; private set;
        }

        private void CreateSpace(float width, float height)
        {
            space = Rect(transform, "地图内容", 0, 0, width, height);
            Viewport = gameObject.AddComponent<MapViewport>();
            Viewport.Initialize(space);
        }

        public void Town(Action<string> action)
        {
            IsTown = true;
            CreateSpace(1040, 660);
            Box(space, "镇外夜色", 0, 0, 1040, 660, Hex(0x080e13));
            Box(space, "镇心广场", 320, 200, 400, 300, Hex(0x1c2b30));
            Box(space, "炉台", 464, 308, 112, 112, Hex(0x695139));
            Label(space, "◆", 488, 326, 64, 58, 44, Gold, TextAnchor.MiddleCenter);
            Label(space, "余 烬 广 场", 386, 450, 270, 30, 22, Muted, TextAnchor.MiddleCenter);
            TownBuilding("旅馆区块", "炉火旅馆", "休息 · 恢复", 90, 90, 238, 206, () => action("旅馆"));
            TownBuilding("商店区块", "旅人集市", "装备 · 补给", 704, 238, 246, 222, () => action("商店"));
            TownBuilding("公会区块", "冒险者公会", "研习 · 技能", 122, 420, 270, 166, () => action("公会"));
            TownBuilding("地牢区块", "旧城门", "进入第一层", 528, 50, 278, 164, () => action("出发"));
            for (int i = 0; i < 7; i++)
            {
                WallMass(430 + i * 58, 554, 46, 42);
            }
            for (int i = 0; i < 4; i++)
            {
                Box(space, "路灯", 370 + i * 88, 252, 8, 16, Gold);
            }
            Viewport.ShowAll();
        }

        private void WallMass(float x, float y, float width, float height)
        {
            Box(space, "不可进入的墙体", x + 2, y + 2, width - 4, height - 4, Hex(0x101b23));
            Box(space, "墙体顶边", x + 2, y + 2, width - 4, 2, Hex(0x33434d));
        }

        private void TownBuilding(string id, string name, string hint, float x, float y, float width, float height, UnityEngine.Events.UnityAction action)
        {
            Box(space, name + "边界", x + 1, y + 1, width - 2, height - 2, Hex(0x617174));
            Button button = Button(space, id, "", x + 3, y + 3, width - 6, height - 6, action);
            button.GetComponent<Image>().color = Hex(0x233137);
            Box(button.transform, "屋檐", 6, 6, width - 18, 12, Hex(0x695a43));
            Box(button.transform, "门廊", width / 2 - 20, height - 37, 40, 26, Hex(0x0c171d));
            for (int i = 0; i < 3; i++)
            {
                Box(button.transform, "灯窗", 20 + i * 28, 30, 14, 18, Hex(0xc59b59));
            }
            Label(button.transform, name, 4, height / 2 - 34, width - 14, 30, 23, Gold, TextAnchor.MiddleCenter);
            Label(button.transform, hint, 4, height / 2 + 8, width - 14, 24, 16, Teal, TextAnchor.MiddleCenter);
            WorldPointer pointer = button.gameObject.AddComponent<WorldPointer>();
            pointer.enter = () => button.GetComponent<Image>().color = Hex(0x35494b);
            pointer.exit = () => button.GetComponent<Image>().color = Hex(0x233137);
        }

        public void Dungeon(AdventureSession session, Action<RoomData> onClick, Action<RoomData> onHover, Action onCancel)
        {
            Floor = session.Data.Current;
            click = onClick;
            hover = onHover;
            cancel = onCancel;
            int width = session.Config.Floor(Floor.number).width;
            int height = session.Config.Floor(Floor.number).height;
            unit = 48;
            left = 0;
            top = 0;
            CreateSpace(width * unit, height * unit);
            foreach (RoomData room in Floor.rooms)
            {
                BuildRoom(room);
            }
            RoomVisual focus = session.Data.battle == null ? visuals.First(v => v.room.kind == RoomKind.Entrance) : visuals.First(v => v.room.id == session.Data.battle.room);
            Viewport.Focus(focus.button.GetComponent<RectTransform>());
            Refresh(session, 0);
        }

        private void BuildRoom(RoomData room)
        {
            float width = room.width * unit;
            float height = room.height * unit;
            float x = left + room.x * unit;
            float y = top + room.y * unit;
            RectTransform root = Rect(space, $"房间空间{room.id}", x, y, width, height);
            Box(root, "区块边界", 1, 1, width - 2, height - 2, Hex(0x465862));
            Image floor = Box(root, "区块地面", 3, 3, width - 6, height - 6, Hex(0x22343b));
            Image fog = Box(root, "未探索空间", 3, 3, width - 6, height - 6, Hex(0x050b10));
            Image glow = Box(root, "目标描边", 5, 5, width - 10, height - 10, Hex(0x71c6b4, 0.12f));
            Button button = Button(root, $"房间{room.id}", "", 0, 0, width, height, () => click(room));
            button.GetComponent<Image>().color = Color.clear;
            Text name = Label(root, "", 6, 7, width - 12, 20, 12, Ink, TextAnchor.MiddleCenter);
            Text health = Label(root, "", 4, height - 25, width - 8, 20, 12, Muted, TextAnchor.MiddleCenter);
            bool compact = room.width == 1 || room.height == 1;
            name.gameObject.SetActive(!compact);
            health.gameObject.SetActive(!compact);
            float glyphSize = Mathf.Min(36, Mathf.Min(width - 12, height - 12));
            RectTransform glyphRect = Rect(root, "房间物件", width / 2 - glyphSize / 2, height / 2 - glyphSize / 2, glyphSize, glyphSize);
            DungeonGlyph glyph = glyphRect.gameObject.AddComponent<DungeonGlyph>();
            glyph.kind = room.kind;
            glyph.raycastTarget = false;
            WorldPointer pointer = button.gameObject.AddComponent<WorldPointer>();
            pointer.enter = () => { hovered = room.id; glow.gameObject.SetActive(true); hover(room); };
            pointer.exit = () => { hovered = -1; glow.gameObject.SetActive(false); hover(null); };
            pointer.right = () => cancel();
            visuals.Add(new RoomVisual { room = room, floor = floor, fog = fog, glow = glow, button = button, name = name, health = health, glyph = glyph });
        }

        public void Refresh(AdventureSession session, int selectedSkill)
        {
            foreach (RoomVisual visual in visuals)
            {
                RoomData room = visual.room;
                bool active = session.Data.battle != null && session.Data.battle.room == room.id;
                visual.fog.gameObject.SetActive(!room.revealed);
                bool hostile = !room.cleared && (room.kind == RoomKind.Monster || room.kind == RoomKind.Elite || room.kind == RoomKind.Boss);
                visual.floor.color = room.IsObstacle && !room.cleared ? Hex(room.kind == RoomKind.Wall ? 0x494940 : 0x343451) : hostile ? Hex(0x413239) : room.cleared ? Hex(0x1e3037) : Hex(0x293b44);
                visual.button.interactable = room.revealed && (session.Data.battle == null || active);
                visual.button.GetComponent<Image>().raycastTarget = room.revealed;
                visual.glow.gameObject.SetActive(room.revealed && (active || hovered == room.id));
                visual.glow.color = active ? Hex(selectedSkill > 0 ? 0x7dc9d0 : 0xec8e7a, 0.16f) : Hex(0x71c6b4, 0.13f);
                visual.name.text = !room.revealed || room.cleared ? "" : room.IsObstacle ? Name(room.kind) : hostile ? session.Config.Monster(room.monster).name : "";
                visual.glyph.enabled = room.revealed;
                visual.glyph.color = room.cleared ? Hex(0x34464b) : hostile ? Hex(0xe2b0a3) : room.kind == RoomKind.Spring ? Blue : Gold;
                visual.health.text = "";
                if (hostile && room.revealed)
                {
                    int maximum = active ? session.Data.battle.stats.hp : session.EnemyStats(room).hp;
                    visual.health.text = $"{(active ? session.Data.battle.enemy.hp : maximum)}/{maximum}";
                }
                else if (room.revealed)
                {
                    visual.health.text = room.IsObstacle && !room.cleared ? room.breakPower.ToString() : "";
                }
            }
        }

        public static string Name(RoomKind kind) => new[] { "入口", "敌人", "精英", "宝箱", "泉水", "神殿", "旅途商人", "向下楼梯", "首领", "旧区块", "墙块", "屏障" }[(int)kind];

    }

    public sealed class WorldPointer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public Action enter;
        public Action exit;
        public Action right;
        public void OnPointerEnter(PointerEventData eventData)
        {
            enter?.Invoke();
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            exit?.Invoke();
        }
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                right?.Invoke();
            }
        }
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DungeonGlyph : MaskableGraphic
    {
        public RoomKind kind;

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            switch (kind)
            {
                case RoomKind.Wall:
                    for (int row = 0; row < 3; row++)
                    {
                        Quad(mesh, color, 3, 3 + row * 12, 32, 9);
                        Quad(mesh, SlateUI.Hex(0x1c2528), row % 2 == 0 ? 12 : 24, 3 + row * 12, 2, 9);
                    }
                    break;
                case RoomKind.Barrier:
                    Polygon(mesh, color, new Vector2(19, 38), new Vector2(3, 19), new Vector2(19, 0), new Vector2(35, 19));
                    Polygon(mesh, SlateUI.Hex(0x1b2038), new Vector2(19, 30), new Vector2(10, 19), new Vector2(19, 8), new Vector2(28, 19));
                    break;
                case RoomKind.Monster:
                case RoomKind.Elite:
                case RoomKind.Boss:
                    Polygon(mesh, color, new Vector2(6, 27), new Vector2(12, 35), new Vector2(27, 35), new Vector2(33, 27), new Vector2(30, 13), new Vector2(26, 9), new Vector2(26, 3), new Vector2(13, 3), new Vector2(13, 9), new Vector2(9, 13));
                    Quad(mesh, Hex(0x152027), 11, 18, 7, 7);
                    Quad(mesh, Hex(0x152027), 22, 18, 7, 7);
                    if (kind == RoomKind.Boss || kind == RoomKind.Elite)
                    {
                        Polygon(mesh, color, new Vector2(5, 28), new Vector2(0, 38), new Vector2(14, 32));
                        Polygon(mesh, color, new Vector2(26, 32), new Vector2(38, 38), new Vector2(33, 27));
                    }
                    break;
                case RoomKind.Chest:
                    Quad(mesh, color, 4, 5, 30, 20);
                    Quad(mesh, color, 6, 28, 26, 6);
                    Quad(mesh, Hex(0x253239), 16, 14, 6, 9);
                    break;
                case RoomKind.Spring:
                    Polygon(mesh, color, new Vector2(19, 37), new Vector2(7, 16), new Vector2(9, 7), new Vector2(19, 2), new Vector2(29, 7), new Vector2(31, 16));
                    break;
                case RoomKind.Shrine:
                    Polygon(mesh, color, new Vector2(19, 38), new Vector2(8, 20), new Vector2(19, 8), new Vector2(30, 20));
                    Quad(mesh, color, 5, 1, 28, 4);
                    break;
                case RoomKind.Merchant:
                    Polygon(mesh, color, new Vector2(2, 27), new Vector2(10, 36), new Vector2(28, 36), new Vector2(36, 27));
                    Quad(mesh, color, 5, 5, 4, 20);
                    Quad(mesh, color, 29, 5, 4, 20);
                    Quad(mesh, color, 5, 4, 28, 6);
                    break;
                case RoomKind.Stairs:
                    for (int i = 0; i < 5; i++)
                    {
                        Quad(mesh, color, 3 + i * 3, 31 - i * 7, 31 - i * 3, 4);
                    }
                    break;
                default:
                    Quad(mesh, color, 7, 3, 5, 26);
                    Quad(mesh, color, 26, 3, 5, 26);
                    Polygon(mesh, color, new Vector2(7, 27), new Vector2(19, 37), new Vector2(31, 27));
                    break;
            }
        }

        private void Quad(VertexHelper mesh, Color tint, float x, float y, float width, float height)
        {
            Polygon(mesh, tint, new Vector2(x, y), new Vector2(x + width, y), new Vector2(x + width, y + height), new Vector2(x, y + height));
        }

        private void Polygon(VertexHelper mesh, Color tint, params Vector2[] points)
        {
            int index = mesh.currentVertCount;
            foreach (Vector2 point in points)
            {
                Vector2 position = rectTransform.rect.min + new Vector2(point.x / 38 * rectTransform.rect.width, point.y / 38 * rectTransform.rect.height);
                mesh.AddVert(position, tint, Vector2.zero);
            }
            for (int i = 1; i < points.Length - 1; i++)
            {
                mesh.AddTriangle(index, index + i, index + i + 1);
            }
        }
    }
}
