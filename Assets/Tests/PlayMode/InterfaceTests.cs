using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game;
using Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class InterfaceTests
{
    private GameObject root;
    private AdventureSession session;
    private string directory;
    private string captures;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        directory = Path.Combine(Application.temporaryCachePath, $"game3-ui-{Guid.NewGuid():N}");
        captures = Path.GetFullPath("Logs/VisualQA");
        Directory.CreateDirectory(captures);
        session = new AdventureSession(new GameConfig(), new SaveStore(Path.Combine(directory, "save.json")));
        root = new GameObject("界面回归");
        root.AddComponent<AdventureView>().Initialize(session);
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        UnityEngine.Object.Destroy(root);
        if (EventSystem.current != null)
        {
            UnityEngine.Object.Destroy(EventSystem.current.gameObject);
        }
        yield return null;
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }
    }

    [UnityTest]
    public IEnumerator MenuTownDungeonBattleAndModalInputsWork()
    {
        yield return Capture("01-标题");
        Click("新的旅程");
        yield return null;
        Click("职业4");
        yield return null;
        root.GetComponentInChildren<InputField>().text = "微光";
        yield return Capture("02-创建");
        Click("开始旅程");
        yield return null;
        Assert.That(session.Data.inTown, Is.True);
        Assert.That(session.Data.hero.profession, Is.EqualTo(4));
        Assert.That(session.Data.hero.name, Is.EqualTo("微光"));
        yield return Capture("03-城镇");

        Click("商店区块");
        yield return null;
        yield return Capture("04-商店");
        int before = session.Data.hero.gold;
        Click("购买2001");
        yield return null;
        Assert.That(session.Data.hero.gold, Is.EqualTo(before - session.Config.Item(2001).price));
        AssertBlocked("地牢区块");
        Click("关闭弹窗");
        yield return null;
        Click("公会区块");
        yield return Capture("05-公会");
        Click("学习101");
        yield return null;
        Assert.That(session.Data.hero.skills, Does.Contain(101));
        Click("技能分类2");
        yield return null;
        Click("学习301");
        yield return null;
        Assert.That(session.Data.hero.skills, Does.Contain(301));
        Click("关闭弹窗");
        yield return null;
        Click("地牢区块");
        yield return null;
        Assert.That(session.Data.currentFloor, Is.EqualTo(1));
        Assert.That(session.Data.inTown, Is.False);
        yield return Capture("06-地牢");

        yield return ExploreUntilBattle();
        yield return Capture("07-战斗");
        int mana = session.Data.hero.mp;
        int round = session.Data.battle.round;
        int roomId = session.Data.battle.room;
        Button targetBefore = Find($"房间{roomId}");
        Click("选择301");
        yield return null;
        Assert.That(session.Data.hero.mp, Is.EqualTo(mana), "选择技能不应推进战斗");
        Click($"房间{roomId}");
        yield return null;
        Assert.That(session.Data.hero.mp, Is.EqualTo(mana), "自身技能点敌人不能扣法力");
        Assert.That(session.Data.battle.round, Is.EqualTo(round));
        Click("角色区块");
        yield return null;
        Assert.That(session.Data.hero.mp, Is.EqualTo(mana - session.SkillCost(session.Config.Skill(301))));
        Assert.That(Find($"房间{roomId}"), Is.SameAs(targetBefore), "回合更新必须保留地图目标节点");
        Click("选择501");
        yield return null;
        mana = session.Data.hero.mp;
        Click($"房间{roomId}");
        yield return null;
        Assert.That(session.Data.hero.mp, Is.LessThan(mana));
        Click("普通攻击");
        yield return null;
        // 初始火矢可能直接击败低生命敌人，此时继续正常探索再检查战斗读档。
        yield return ExploreUntilBattle();
        // 随机地图中的升级可能回满生命，先通过正常战斗产生恢复需求。
        for (int attempt = 0; attempt < 30 && session.Data.hero.hp == session.Data.hero.GetStats(session.Config).hp; attempt++)
        {
            yield return ExploreUntilBattle();
            Click("普通攻击");
            yield return null;
            Click($"房间{session.Data.battle.room}");
            yield return null;
        }
        Assert.That(session.Data.hero.hp, Is.LessThan(session.Data.hero.GetStats(session.Config).hp));
        int potionCount = session.Data.hero.Count(2001);
        MapViewport retainedMap = root.GetComponentInChildren<MapViewport>();
        float retainedZoom = retainedMap.Zoom;
        Vector2 retainedPosition = retainedMap.Content.anchoredPosition;
        Click("使用2001");
        yield return null;
        Assert.That(session.Data.hero.Count(2001), Is.EqualTo(potionCount - 1));
        Assert.That(retainedMap.Zoom, Is.EqualTo(retainedZoom));
        Assert.That(retainedMap.Content.anchoredPosition, Is.EqualTo(retainedPosition));
        Assert.That(root.GetComponentsInChildren<RectTransform>().Any(r => r.name == "主界面背包"), Is.True);
        yield return Capture("08-主界面背包");
        Click("设置入口");
        yield return Capture("09-设置");
        AssertBlocked("使用2001");
        AssertBlocked("普通攻击");
        Click("关闭弹窗");
        yield return null;
        string savedAdventure = JsonUtility.ToJson(session.Data);
        Click("标题入口");
        yield return null;
        Assert.That(session.Store.Exists, Is.True);
        Click("继续冒险");
        yield return null;
        Assert.That(session.Data.battle, Is.Not.Null);
        Assert.That(JsonUtility.ToJson(session.Data), Is.EqualTo(savedAdventure), "继续冒险必须完整恢复退出前的状态，包括升级后的法力");
        MapViewport resumedMap = root.GetComponentInChildren<MapViewport>();
        Assert.That(RectTransformUtility.RectangleContainsScreenPoint(resumedMap.GetComponent<RectTransform>(), Pointer(Find($"房间{session.Data.battle.room}")).position), Is.True, "恢复战斗应定位当前敌人");
        yield return Capture("10-恢复战斗");
        yield return Capture("11-小窗口", 1280, 720);
        yield return Capture("12-较高窗口", 1600, 1000);
    }

    [UnityTest]
    public IEnumerator MerchantOpensFromRoomAndLargeSkillSetFits()
    {
        session.Create(4, "旅商测试", 318);
        session.Data.hero.gold = 6000;
        session.Data.hero.AddExperience(10000, session.Config);
        foreach (SkillDef skill in session.Config.Skills.Where(x => x.id < 900).OrderBy(x => x.level))
        {
            if (!session.Data.hero.skills.Contains(skill.id))
            {
                Assert.That(session.Learn(skill.id), Is.True);
            }
        }
        session.EnterFloor(1);
        RoomData merchant = session.Data.Current.rooms.First(x => x.kind == RoomKind.Merchant);
        merchant.revealed = true;
        session.RetrySave();
        yield return null;
        Click("继续冒险");
        yield return null;
        yield return Capture("13-跨职业技能栏");
        Click("选择504");
        yield return null;
        Assert.That(session.Data.battle, Is.Null);
        int gold = session.Data.hero.gold;
        int count = session.Data.hero.Count(2001);
        Click($"房间{merchant.id}");
        yield return null;
        RoomData offerRoom = session.Data.Current.Room(merchant.id);
        Assert.That(offerRoom.cleared, Is.False);
        Assert.That(offerRoom.offers.Count, Is.EqualTo(3));
        Assert.That(Find("奖励0").gameObject.activeInHierarchy, Is.True);
        AssertBlocked("普通攻击");
        yield return Capture("14-旅商交互");
        int price = offerRoom.offers[0].price;
        Click("奖励0");
        yield return null;
        Assert.That(session.Data.hero.gold, Is.EqualTo(gold - price));
        Assert.That(offerRoom.cleared, Is.True);
        foreach (Button skill in root.GetComponentsInChildren<Button>().Where(x => x.name.StartsWith("选择") && x.name != "选择防御"))
        {
            RectTransform rect = skill.GetComponent<RectTransform>();
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Assert.That(corners.All(x => x.x >= 0 && x.y >= 0), Is.True, "已学技能必须在画面内可选");
        }
    }


    [UnityTest]
    public IEnumerator MapZoomDragHoverAndInventoryRemainIndependent()
    {
        session.Create(4, "视图测试", 318);
        session.EnterFloor(1);
        yield return null;
        Click("继续冒险");
        yield return null;
        MapViewport map = root.GetComponentInChildren<MapViewport>();
        RoomData entrance = session.Data.Current.rooms.Single(r => r.kind == RoomKind.Entrance);
        RectTransform entranceRect = Find($"房间{entrance.id}").GetComponent<RectTransform>();
        Vector3 entranceCenter = map.transform.InverseTransformPoint(entranceRect.TransformPoint(entranceRect.rect.center));
        Assert.That(entranceCenter.x, Is.EqualTo(520).Within(1));
        Assert.That(entranceCenter.y, Is.EqualTo(-330).Within(1));
        foreach (DungeonGlyph glyph in root.GetComponentsInChildren<DungeonGlyph>())
        {
            Assert.That(glyph.rectTransform.rect.width, Is.GreaterThan(0), "小区块图标尺寸必须为正数");
            Assert.That(glyph.rectTransform.rect.height, Is.GreaterThan(0));
        }
        yield return Capture("19-中央入口");
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, map.transform.TransformPoint(new Vector3(520, -330, 0)));
        PointerEventData wheel = new PointerEventData(EventSystem.current) { position = screen, scrollDelta = new Vector2(0, -2) };
        float initial = map.Zoom;
        ExecuteEvents.Execute(map.gameObject, wheel, ExecuteEvents.scrollHandler);
        Assert.That(map.Zoom, Is.LessThan(initial));
        wheel.scrollDelta = new Vector2(0, 3);
        ExecuteEvents.Execute(map.gameObject, wheel, ExecuteEvents.scrollHandler);
        Assert.That(map.Zoom, Is.GreaterThan(initial));
        Vector2 before = map.Content.anchoredPosition;
        PointerEventData drag = new PointerEventData(EventSystem.current) { position = screen, delta = new Vector2(-100, -60), button = PointerEventData.InputButton.Left };
        ExecuteEvents.Execute(map.gameObject, drag, ExecuteEvents.beginDragHandler);
        ExecuteEvents.Execute(map.gameObject, drag, ExecuteEvents.dragHandler);
        Assert.That(map.Content.anchoredPosition, Is.Not.EqualTo(before));
        Assert.That(drag.eligibleForClick, Is.False, "拖动不能触发区块点击");
        Vector2 beforeArrow = map.Content.anchoredPosition;
        Click("移动地图3");
        yield return null;
        Assert.That(map.Content.anchoredPosition, Is.Not.EqualTo(beforeArrow));
        Button potion = Find("使用2001");
        ExecuteEvents.Execute(potion.gameObject, Pointer(potion), ExecuteEvents.pointerEnterHandler);
        yield return null;
        Assert.That(root.GetComponentsInChildren<RectTransform>().Any(r => r.name == "悬停信息"), Is.True);
        yield return Capture("15-物品悬停");
        ExecuteEvents.Execute(potion.gameObject, Pointer(potion), ExecuteEvents.pointerExitHandler);
        yield return null;
        Assert.That(root.GetComponentsInChildren<RectTransform>().Any(r => r.name == "悬停信息"), Is.False);
        Click("查看全图");
        yield return Capture("16-大地图全貌");
        foreach (RoomData room in session.Data.Current.rooms)
        {
            room.revealed = true;
        }
        session.RetrySave();
        yield return null;
        yield return Capture("20-区块尺寸分布展示", clearHover: true);
        RoomData shrine = session.Data.Current.rooms.First(r => r.kind == RoomKind.Shrine);
        Click($"房间{shrine.id}");
        yield return null;
        yield return Capture("21-神殿三选一");
        int relicIndex = shrine.offers.FindIndex(r => r.kind == 1);
        Assert.That(relicIndex, Is.GreaterThanOrEqualTo(0));
        int relicId = shrine.offers[relicIndex].id;
        Click($"奖励{relicIndex}");
        yield return null;
        Button relicButton = Find($"宝物{relicId}");
        ExecuteEvents.Execute(relicButton.gameObject, Pointer(relicButton), ExecuteEvents.pointerEnterHandler);
        yield return null;
        yield return Capture("22-宝物增益悬停");
        ExecuteEvents.Execute(relicButton.gameObject, Pointer(relicButton), ExecuteEvents.pointerExitHandler);
        yield return null;
        RoomData obstacle = session.Data.Current.rooms.First(r => r.IsObstacle && Math.Min(r.width, r.height) == 1);
        session.RetrySave();
        // 独立场景用于画面与输入覆盖，不计入正常通关证据。
        root.GetComponentInChildren<WorldBoard>().Refresh(session, 0);
        wheel.scrollDelta = new Vector2(0, 5);
        ExecuteEvents.Execute(map.gameObject, wheel, ExecuteEvents.scrollHandler);
        map.Focus(Find($"房间{obstacle.id}").GetComponent<RectTransform>());
        yield return Capture("17-墙与屏障");
        Click($"房间{obstacle.id}");
        yield return null;
        Assert.That(obstacle.cleared, Is.False);
        session.Data.hero.AddExperience(100000, session.Config);
        Click($"房间{obstacle.id}");
        yield return null;
        Assert.That(obstacle.cleared, Is.True);
        yield return Capture("18-破坏障碍");
        session.ReturnToTown();
        yield return null;
        int gold = session.Data.hero.gold;
        int count = session.Data.hero.Count(2001);
        Button sell = Find("使用2001");
        PointerEventData right = Pointer(sell);
        right.button = PointerEventData.InputButton.Right;
        ExecuteEvents.Execute(sell.gameObject, right, ExecuteEvents.pointerClickHandler);
        yield return null;
        Assert.That(session.Data.hero.Count(2001), Is.EqualTo(count - 1));
        Assert.That(session.Data.hero.gold, Is.EqualTo(gold + session.Config.Item(2001).price / 2));
        ItemDef weapon = session.Config.Items.First(i => i.kind == ItemKind.Weapon && i.level == 1);
        Assert.That(session.Buy(weapon.id), Is.True);
        yield return null;
        Click($"使用{weapon.id}");
        yield return null;
        Assert.That(session.Data.hero.weapon, Is.EqualTo(weapon.id));
    }

    [UnityTest]
    public IEnumerator PointerPressMovementAndReleaseRespectClicksAndDrags()
    {
        session.Create(4, "输入测试", 318);
        yield return null;
        Click("继续冒险");
        yield return null;
        StandaloneInputModule module = EventSystem.current.GetComponent<StandaloneInputModule>();
        module.enabled = false;
        PointerSequenceInput input = EventSystem.current.gameObject.AddComponent<PointerSequenceInput>();
        module.inputOverride = input;
        yield return Gesture(module, input, "商店区块", new Vector2(12, 0));
        Assert.That(root.GetComponentsInChildren<Button>().Any(b => b.name == "购买2001"), Is.True, "轻微手部移动后松开，应一次打开商店");
        yield return Gesture(module, input, "关闭弹窗", Vector2.zero);
        yield return null;
        yield return Gesture(module, input, "商店区块", new Vector2(40, 0));
        Assert.That(root.GetComponentsInChildren<Button>().Any(b => b.name == "购买2001"), Is.False, "拖动不能打开商店");
        yield return Gesture(module, input, "地牢区块", new Vector2(12, 0));
        Assert.That(session.Data.inTown, Is.False);
        yield return null;
        RoomData merchant = session.Data.Current.rooms.First(r => r.kind == RoomKind.Merchant);
        merchant.revealed = true;
        session.RetrySave();
        yield return null;
        yield return Gesture(module, input, $"房间{merchant.id}", new Vector2(12, 0));
        Assert.That(root.GetComponentsInChildren<Button>().Any(b => b.name == "奖励0"), Is.True, "商人房间轻微移动后仍能打开");
        yield return Gesture(module, input, "放弃交易", Vector2.zero);
        yield return null;
        RoomData enemy = session.Data.Current.rooms.First(r => r.kind == RoomKind.Monster);
        enemy.revealed = true;
        session.Explore(enemy.id);
        // 持续战斗夹具只验证输入，不用于数值与平衡结论。
        session.Data.battle.enemy.hp = 1000;
        session.Data.battle.hero.hp = 1000;
        session.RetrySave();
        yield return null;
        MapViewport map = root.GetComponentInChildren<MapViewport>();
        map.Focus(Find($"房间{enemy.id}").GetComponent<RectTransform>());
        Vector2 beforeDrag = map.Content.anchoredPosition;
        int round = session.Data.battle.round;
        yield return Gesture(module, input, $"房间{enemy.id}", new Vector2(40, 0));
        Assert.That(map.Content.anchoredPosition, Is.Not.EqualTo(beforeDrag));
        Assert.That(session.Data.battle.round, Is.EqualTo(round), "拖动敌人房间不能出手");
        for (int i = 0; i < 4; i++)
        {
            yield return Gesture(module, input, $"房间{enemy.id}", new Vector2(12, 0));
            Assert.That(session.Data.battle.round, Is.EqualTo(++round), "每次完整点击只推进一轮");
            yield return null;
        }
        SkillDef skill = session.Config.Skill(session.Data.hero.skills.First());
        yield return Gesture(module, input, $"选择{skill.id}", Vector2.zero);
        int mana = session.Data.hero.mp;
        yield return Gesture(module, input, $"房间{enemy.id}", new Vector2(12, 0));
        Assert.That(session.Data.hero.mp, Is.EqualTo(mana - session.SkillCost(skill)));
        Assert.That(session.Data.battle.round, Is.EqualTo(++round), "技能选中后点击一次目标即施放");
        session.Data.battle.hero.hp = 1;
        session.Data.hero.hp = 1;
        session.RetrySave();
        yield return null;
        int potions = session.Data.hero.Count(2001);
        yield return Gesture(module, input, "使用2001", new Vector2(0, 12));
        Assert.That(session.Data.hero.Count(2001), Is.EqualTo(potions - 1), "背包内轻微移动不能吞掉药水点击");
        Assert.That(session.Data.battle.round, Is.EqualTo(round + 1));
        session.Data.battle.enemy.hp = 1;
        yield return Gesture(module, input, "普通攻击", Vector2.zero);
        yield return Gesture(module, input, $"房间{enemy.id}", Vector2.zero);
        Assert.That(enemy.cleared, Is.True);
        Assert.That(session.Data.battle, Is.Null);
        yield return Capture("23-紧凑近况与战斗结束", clearHover: true);
        module.inputOverride = null;
        module.enabled = true;
    }

    private IEnumerator Gesture(StandaloneInputModule module, PointerSequenceInput input, string name, Vector2 movement)
    {
        Button button = Find(name);
        MapViewport map = button.GetComponentInParent<MapViewport>();
        if (map != null)
        {
            map.Focus(button.GetComponent<RectTransform>());
        }
        Canvas.ForceUpdateCanvases();
        Vector2 position = Pointer(button).position;
        input.position = position;
        input.phase = 0;
        module.Process();
        yield return null;
        input.phase = 1;
        module.Process();
        yield return null;
        input.phase = 2;
        input.position += movement;
        module.Process();
        yield return null;
        input.phase = 3;
        module.Process();
        yield return null;
        input.phase = 0;
        module.Process();
        yield return null;
    }

    private Button Find(string name)
    {
        return root.GetComponentsInChildren<Button>().Single(x => x.gameObject.name == name);
    }

    private IEnumerator ExploreUntilBattle()
    {
        // 通过房间按钮推进，不改写地图的揭示状态。
        for (int attempts = 0; attempts < 30 && session.Data.battle == null; attempts++)
        {
            RoomData next = session.Data.Current.rooms.First(x => x.revealed && !x.cleared && x.kind != RoomKind.Stairs && x.kind != RoomKind.Boss && !x.IsObstacle);
            Click($"房间{next.id}");
            yield return null;
            if (next.kind == RoomKind.Merchant || next.kind == RoomKind.Shrine)
            {
                Click(next.kind == RoomKind.Merchant ? "放弃交易" : "奖励0");
                yield return null;
            }
        }
        Assert.That(session.Data.battle, Is.Not.Null);
    }

    private void AssertBlocked(string name)
    {
        Button target = Find(name);
        PointerEventData pointer = Pointer(target);
        List<RaycastResult> hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        Assert.That(hits.Count, Is.GreaterThan(0));
        Assert.That(hits[0].gameObject, Is.Not.EqualTo(target.gameObject), "弹窗必须遮挡底层点击");
    }

    private static PointerEventData Pointer(Button button)
    {
        RectTransform rect = button.GetComponent<RectTransform>();
        Vector3 center = rect.TransformPoint(rect.rect.center);
        return new PointerEventData(EventSystem.current)
        {
            position = RectTransformUtility.WorldToScreenPoint(null, center),
            button = PointerEventData.InputButton.Left
        };
    }

    private void Click(string name)
    {
        Canvas.ForceUpdateCanvases();
        Button target = Find(name);
        Assert.That(target.interactable, Is.True, $"{name}按钮应可用");
        MapViewport map = target.GetComponentInParent<MapViewport>();
        if (map != null)
        {
            map.Focus(target.GetComponent<RectTransform>());
            Canvas.ForceUpdateCanvases();
        }
        ScrollRect scroll = target.GetComponentInParent<ScrollRect>();
        if (scroll != null)
        {
            Vector3 local = scroll.content.InverseTransformPoint(target.GetComponent<RectTransform>().TransformPoint(target.GetComponent<RectTransform>().rect.center));
            float offset = Mathf.Clamp(-local.y - scroll.viewport.rect.height / 2, 0, Mathf.Max(0, scroll.content.rect.height - scroll.viewport.rect.height));
            scroll.content.anchoredPosition = new Vector2(0, offset);
            Canvas.ForceUpdateCanvases();
        }
        PointerEventData pointer = Pointer(target);
        List<RaycastResult> hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        Assert.That(hits.Count, Is.GreaterThan(0), $"{name}没有命中点击目标");
        Assert.That(hits[0].gameObject, Is.EqualTo(target.gameObject), $"{name}被其他对象遮挡");
        ExecuteEvents.Execute(target.gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }

    private IEnumerator Capture(string name, int width = 1600, int height = 900, bool clearHover = false)
    {
        GameObject cameraObject = new GameObject("界面截图相机");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = SlateUI.Background;
        RenderTexture target = new RenderTexture(width, height, 24);
        camera.targetTexture = target;
        Canvas canvas = root.GetComponentInChildren<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1;
        yield return null;
        if (clearHover)
        {
            foreach (WorldPointer pointer in root.GetComponentsInChildren<WorldPointer>())
            {
                ExecuteEvents.Execute(pointer.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerExitHandler);
            }
            yield return null;
        }
        Canvas.ForceUpdateCanvases();
        camera.Render();
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = target;
        Texture2D texture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        texture.Apply();
        File.WriteAllBytes(Path.Combine(captures, $"{name}.png"), texture.EncodeToPNG());
        RenderTexture.active = previous;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = null;
        camera.targetTexture = null;
        target.Release();
        UnityEngine.Object.Destroy(target);
        UnityEngine.Object.Destroy(texture);
        UnityEngine.Object.Destroy(cameraObject);
        yield return null;
    }
}

public sealed class PointerSequenceInput : BaseInput
{
    public Vector2 position;
    public int phase;
    public override bool mousePresent => true;
    public override Vector2 mousePosition => position;
    public override Vector2 mouseScrollDelta => Vector2.zero;
    public override int touchCount => 0;
    public override bool GetMouseButtonDown(int button) => button == 0 && phase == 1;
    public override bool GetMouseButton(int button) => button == 0 && (phase == 1 || phase == 2);
    public override bool GetMouseButtonUp(int button) => button == 0 && phase == 3;
    public override float GetAxisRaw(string axisName) => 0;
    public override bool GetButtonDown(string buttonName) => false;
}
