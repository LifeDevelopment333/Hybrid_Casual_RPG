using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메뉴: RPG > UI > Blackiron 레이아웃 프리팹 만들기
/// 기존 BlackironUI.prefab은 건드리지 않고 BlackironUI_Layout.prefab을 새로 만든다.
///
/// 구조
/// BlackironUI_Layout (Canvas, 1080x1920, Match Width)
/// └ SafeArea
///   ├ HUD            : 상단 상태/지역, 우측 획득·자동전투, 조이스틱 영역, EXP 바
///   ├ Screens        : Equipment / Bag / Stats (하단 탭 위 영역, 기본 꺼짐)
///   ├ BottomNav      : 탭 4개 (하나만 존재)
///   └ PopupLayer     : 팝업용 빈 레이어
/// </summary>
public static class BlackironLayoutBuilder
{
    private const string ArtRoot = "Assets/RPG/ART/UI/Blackiron-v1/";
    private const string FontPath = ArtRoot + "Fonts/Blackiron Korean SDF.asset";
    private const string SavePath = "Assets/RPG/Prefab/UI/Blackiron/BlackironUI_Layout.prefab";

    // 기준 치수
    private const float Margin = 24f;
    private const float NavHeight = 190f;
    private const float HeaderHeight = 150f;

    private static readonly Color TextMain = new Color32(0xEE, 0xE3, 0xC8, 0xFF);
    private static readonly Color TextSub = new Color32(0xB5, 0xA8, 0x8C, 0xFF);
    private static readonly Color TextGold = new Color32(0xE6, 0xB8, 0x5C, 0xFF);
    private static readonly Color Placeholder = new Color32(0x3A, 0x34, 0x2C, 0xFF);

    private static TMP_FontAsset font;
    private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    [MenuItem("RPG/UI/Blackiron 레이아웃 프리팹 만들기")]
    public static void Build()
    {
        spriteCache.Clear();
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null) Debug.LogWarning($"[LayoutBuilder] 폰트를 찾을 수 없음: {FontPath}");

        if (AssetDatabase.LoadAssetAtPath<GameObject>(SavePath) != null &&
            EditorUtility.DisplayDialog("덮어쓰기", $"{SavePath}\n이미 있어요. 덮어쓸까요?", "덮어쓰기", "취소") == false)
            return;

        GameObject root = BuildRoot();
        try
        {
            PrefabUtility.SaveAsPrefabAsset(root, SavePath);
            Debug.Log($"[LayoutBuilder] 생성 완료: {SavePath}");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(SavePath));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    // ───────────────────────────── Root ─────────────────────────────

    private static GameObject BuildRoot()
    {
        var root = new GameObject("BlackironUI_Layout", typeof(RectTransform));
        root.layer = LayerMask.NameToLayer("UI");

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20; // 씬의 조이스틱 캔버스(10)보다 위

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f; // 세로 게임: 폭 기준 → 20:9 폰에서도 좌우 안 잘림

        root.AddComponent<GraphicRaycaster>();

        RectTransform safe = Stretch(Create("SafeArea", root.transform));
        // SafeArea 스크립트가 있으면 붙인다 (없어도 빌드는 됨)
        System.Type safeAreaType = System.Type.GetType("SafeArea, Assembly-CSharp");
        if (safeAreaType != null) safe.gameObject.AddComponent(safeAreaType);

        BuildHUD(safe);

        RectTransform screens = Stretch(Create("Screens", safe), 0, 0, 0, NavHeight);
        BuildEquipmentScreen(screens).gameObject.SetActive(false);
        BuildBagScreen(screens).gameObject.SetActive(false);
        BuildStatsScreen(screens).gameObject.SetActive(false);

        BuildBottomNav(safe);

        Stretch(Create("PopupLayer", safe));

        return root;
    }

    // ───────────────────────────── HUD ─────────────────────────────

    private static void BuildHUD(RectTransform parent)
    {
        RectTransform hud = Stretch(Create("HUD", parent));

        // 좌상단: 플레이어 상태
        RectTransform status = Anchored(Create("PlayerStatus", hud), new Vector2(0, 1), new Vector2(Margin, -Margin), new Vector2(510, 190));
        Img(status, "panels/panel-section");

        RectTransform portrait = Anchored(Create("PortraitFrame", status), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(158, 158));
        Img(portrait, "panels/frame-portrait");
        RectTransform face = Center(Create("Portrait", portrait), Vector2.zero, new Vector2(110, 110));
        Img(face, null).color = Placeholder;

        Text(Anchored(Create("Level", status), new Vector2(0, 1), new Vector2(192, -12), new Vector2(300, 60)),
            "Lv.1", 38, TextGold, TextAlignmentOptions.Left);
        Bar(Anchored(Create("HealthBar", status), new Vector2(0, 1), new Vector2(192, -76), new Vector2(296, 42)),
            "controls/bar-hp-fill", "100 / 100");
        Text(Anchored(Create("Experience", status), new Vector2(0, 1), new Vector2(192, -132), new Vector2(296, 36)),
            "EXP 0 / 100", 26, TextSub, TextAlignmentOptions.Left);

        // 우상단: 지역
        RectTransform location = Anchored(Create("Location", hud), new Vector2(1, 1), new Vector2(-Margin, -Margin), new Vector2(380, 120));
        Img(location, "panels/panel-section");
        Text(Anchored(Create("Area", location), new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(340, 46)),
            "버려진 요새", 34, TextMain, TextAlignmentOptions.Center);
        Text(Anchored(Create("SubArea", location), new Vector2(0.5f, 1), new Vector2(0, -64), new Vector2(340, 36)),
            "외곽", 26, TextSub, TextAlignmentOptions.Center);

        // 우측: 획득 알림 (최근 2줄)
        RectTransform loot = Anchored(Create("LootFeed", hud), new Vector2(1, 0), new Vector2(-Margin, 560), new Vector2(330, 150));
        Img(loot, "panels/panel-section");
        var lootLayout = loot.gameObject.AddComponent<VerticalLayoutGroup>();
        lootLayout.padding = new RectOffset(20, 20, 18, 18);
        lootLayout.spacing = 8;
        lootLayout.childControlWidth = lootLayout.childControlHeight = true;
        lootLayout.childForceExpandWidth = true;
        lootLayout.childForceExpandHeight = true;
        LootEntry(loot, "LootEntry_Gold", "icons/icon-gold", "골드 +0");
        LootEntry(loot, "LootEntry_Item", "icons/icon-bag", "아이템 +0");

        // 우하단: 자동 전투
        RectTransform auto = Anchored(Create("AutoCombatButton", hud), new Vector2(1, 0), new Vector2(-Margin, 410), new Vector2(300, 110));
        Btn(auto);
        Img(Anchored(Create("Icon", auto), new Vector2(0, 0.5f), new Vector2(22, 0), new Vector2(64, 64)), "icons/icon-auto-combat");
        Text(Anchored(Create("Label", auto), new Vector2(0, 0.5f), new Vector2(96, 0), new Vector2(190, 70)),
            "자동 전투", 34, TextMain, TextAlignmentOptions.Left);

        // 좌하단: 조이스틱 자리 표시 (실제 조이스틱은 씬의 PlayerControls 캔버스)
        Anchored(Create("JoystickArea", hud), new Vector2(0, 0), new Vector2(40, 300), new Vector2(300, 300));

        // 하단: EXP 바 (탭 바 바로 위)
        RectTransform expBar = Create("ExperienceBar", hud);
        expBar.anchorMin = new Vector2(0, 0);
        expBar.anchorMax = new Vector2(1, 0);
        expBar.pivot = new Vector2(0.5f, 0);
        expBar.offsetMin = new Vector2(Margin, NavHeight + 12);
        expBar.offsetMax = new Vector2(-Margin, NavHeight + 12 + 40);
        Bar(expBar, "controls/bar-exp-fill", "EXP 0.0%");
    }

    private static void LootEntry(RectTransform parent, string name, string icon, string label)
    {
        RectTransform row = Create(name, parent);
        Img(Anchored(Create("Icon", row), new Vector2(0, 0.5f), Vector2.zero, new Vector2(48, 48)), icon);
        Text(Stretch(Create("Text", row), 64, 0, 0, 0), label, 28, TextMain, TextAlignmentOptions.Left);
    }

    // ───────────────────────────── Screens ─────────────────────────────

    private static RectTransform ScreenBase(RectTransform parent, string name, string title)
    {
        RectTransform screen = Stretch(Create(name, parent));
        Img(screen, "panels/panel-main").raycastTarget = true; // 뒤의 HUD 터치 막기

        RectTransform plaque = Anchored(Create("TitlePlaque", screen), new Vector2(0.5f, 1), new Vector2(0, -Margin), new Vector2(600, 100));
        Img(plaque, "panels/plaque-title");
        Text(Stretch(Create("Title", plaque), 30, 30, 10, 10), title, 46, TextGold, TextAlignmentOptions.Center);

        RectTransform close = Anchored(Create("CloseButton", screen), new Vector2(1, 1), new Vector2(-Margin, -Margin - 6), new Vector2(88, 88));
        Btn(close);
        Img(Center(Create("Icon", close), Vector2.zero, new Vector2(48, 48)), "icons/icon-close");

        return screen;
    }

    private static RectTransform BuildEquipmentScreen(RectTransform parent)
    {
        RectTransform screen = ScreenBase(parent, "EquipmentScreen", "장비");

        // 헤더 ~ 스탯 요약 사이를 꽉 채움 (긴 폰에서 늘어남)
        RectTransform panel = Stretch(Create("EquipmentPanel", screen), Margin, Margin, HeaderHeight, Margin + 200 + 16 + 150 + 16);
        Img(panel, "panels/panel-section");

        RectTransform left = SlotColumn(panel, "LeftSlots", 0f, 30f);
        RectTransform right = SlotColumn(panel, "RightSlots", 1f, -30f);
        EquipSlot(left, "Slot_Weapon", "무기");
        EquipSlot(left, "Slot_Armor", "갑옷");
        EquipSlot(left, "Slot_Gloves", "장갑");
        EquipSlot(left, "Slot_Legs", "다리");
        EquipSlot(right, "Slot_Head", "머리");
        EquipSlot(right, "Slot_Necklace", "목걸이");
        EquipSlot(right, "Slot_Earring", "귀걸이");
        EquipSlot(right, "Slot_Ring", "반지");

        RectTransform preview = Create("CharacterPreview", panel);
        preview.anchorMin = new Vector2(0.5f, 0);
        preview.anchorMax = new Vector2(0.5f, 1);
        preview.pivot = new Vector2(0.5f, 0.5f);
        preview.sizeDelta = new Vector2(500, -60); // 위아래 30 여백
        Img(preview, null).color = Placeholder;
        Text(Center(Create("Label", preview), Vector2.zero, new Vector2(460, 60)), "캐릭터 미리보기", 30, TextSub, TextAlignmentOptions.Center);

        // 아이템 상세 바로 위
        RectTransform summary = Create("StatSummary", screen);
        summary.anchorMin = new Vector2(0, 0);
        summary.anchorMax = new Vector2(1, 0);
        summary.pivot = new Vector2(0.5f, 0);
        summary.offsetMin = new Vector2(Margin, Margin + 200 + 16);
        summary.offsetMax = new Vector2(-Margin, Margin + 200 + 16 + 150);
        Img(summary, "panels/panel-stat-row");
        Text(Stretch(Create("Text", summary), 40, 40, 20, 20),
            "공격력  0        방어력  0\n최대 HP  0        치명타  0%", 30, TextMain, TextAlignmentOptions.Center);

        ItemDetail(screen, "장착");
        return screen;
    }

    private static RectTransform SlotColumn(RectTransform panel, string name, float side, float x)
    {
        RectTransform col = Anchored(Create(name, panel), new Vector2(side, 0.5f), new Vector2(x, 0), new Vector2(170, 760));
        var layout = col.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 26;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = layout.childControlHeight = false;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        return col;
    }

    private static void EquipSlot(RectTransform parent, string name, string label)
    {
        RectTransform slot = Slot(parent, name, 164);
        // 빈 슬롯일 때 보이는 부위 이름 (아이템 들어오면 끄기)
        var slotName = Text(Center(Create("SlotName", slot), Vector2.zero, new Vector2(140, 50)),
            label, 28, TextSub, TextAlignmentOptions.Center);
        slotName.color = new Color(TextSub.r, TextSub.g, TextSub.b, 0.6f);
    }

    private static RectTransform BuildBagScreen(RectTransform parent)
    {
        RectTransform screen = ScreenBase(parent, "BagScreen", "가방");

        Text(Anchored(Create("Capacity", screen), new Vector2(1, 1), new Vector2(-Margin - 10, -HeaderHeight + 6), new Vector2(300, 44)),
            "0 / 40", 28, TextSub, TextAlignmentOptions.Right);

        // 스크롤 가능한 슬롯 그리드
        RectTransform scroll = Stretch(Create("ItemScroll", screen), Margin, Margin, HeaderHeight + 50, 250);
        Img(scroll, "panels/panel-section").raycastTarget = true;
        var scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        RectTransform viewport = Stretch(Create("Viewport", scroll), 12, 12, 12, 12);
        viewport.gameObject.AddComponent<RectMask2D>();

        RectTransform content = Create("Content", viewport);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.offsetMin = content.offsetMax = Vector2.zero;

        var grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(180, 180);
        grid.spacing = new Vector2(16, 16);
        grid.padding = new RectOffset(12, 12, 12, 12);
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewport;
        scrollRect.content = content;

        for (int i = 1; i <= 40; i++) Slot(content, $"BagSlot_{i:00}", 180);

        ItemDetail(screen, "사용");
        return screen;
    }

    private static RectTransform BuildStatsScreen(RectTransform parent)
    {
        RectTransform screen = ScreenBase(parent, "StatsScreen", "능력치");

        RectTransform profile = TopBlock(Create("Profile", screen), HeaderHeight, 240);
        Img(profile, "panels/panel-section");
        RectTransform frame = Anchored(Create("PortraitFrame", profile), new Vector2(0, 0.5f), new Vector2(30, 0), new Vector2(200, 200));
        Img(frame, "panels/frame-portrait");
        Img(Center(Create("Portrait", frame), Vector2.zero, new Vector2(130, 130)), null).color = Placeholder;
        Text(Anchored(Create("Level", profile), new Vector2(0, 1), new Vector2(260, -30), new Vector2(680, 70)),
            "Lv.1", 52, TextGold, TextAlignmentOptions.Left);
        Text(Anchored(Create("ExpLabel", profile), new Vector2(0, 1), new Vector2(260, -104), new Vector2(680, 44)),
            "EXP 0 / 100", 28, TextSub, TextAlignmentOptions.Left);
        Bar(Anchored(Create("ExperienceBar", profile), new Vector2(0, 1), new Vector2(260, -160), new Vector2(680, 34)),
            "controls/bar-exp-fill", null);

        RectTransform points = TopBlock(Create("AvailablePoints", screen), HeaderHeight + 256, 100);
        Img(points, "panels/panel-stat-row");
        Text(Stretch(Create("Points", points), 30, 30, 10, 10), "남은 포인트   0", 38, TextGold, TextAlignmentOptions.Center);

        RectTransform list = TopBlock(Create("StatList", screen), HeaderHeight + 372, 780);
        var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 12;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        StatRow(list, "StatRow_Health", "icons/icon-health", "최대 HP", "100");
        StatRow(list, "StatRow_Attack", "icons/icon-attack", "공격력", "10");
        StatRow(list, "StatRow_Defense", "icons/icon-defense", "방어력", "5");
        StatRow(list, "StatRow_MoveSpeed", "icons/icon-movement", "이동속도", "4.0");
        StatRow(list, "StatRow_AttackSpeed", "icons/icon-attack-speed", "공격속도", "1.0");
        StatRow(list, "StatRow_Critical", "icons/icon-critical", "치명타 확률", "5%");

        RectTransform reset = Anchored(Create("ResetButton", screen), new Vector2(0.5f, 0), new Vector2(0, Margin), new Vector2(380, 110));
        Btn(reset);
        Img(Anchored(Create("Icon", reset), new Vector2(0, 0.5f), new Vector2(40, 0), new Vector2(56, 56)), "icons/icon-reset");
        Text(Anchored(Create("Label", reset), new Vector2(0, 0.5f), new Vector2(110, 0), new Vector2(240, 70)),
            "초기화", 36, TextMain, TextAlignmentOptions.Left);

        return screen;
    }

    private static void StatRow(RectTransform parent, string name, string icon, string label, string value)
    {
        RectTransform row = Create(name, parent);
        Img(row, "panels/panel-stat-row");
        var le = row.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = le.minHeight = 120;

        Img(Anchored(Create("Icon", row), new Vector2(0, 0.5f), new Vector2(34, 0), new Vector2(64, 64)), icon);
        Text(Anchored(Create("Name", row), new Vector2(0, 0.5f), new Vector2(124, 0), new Vector2(420, 70)),
            label, 34, TextMain, TextAlignmentOptions.Left);
        Text(Anchored(Create("Value", row), new Vector2(1, 0.5f), new Vector2(-150, 0), new Vector2(220, 70)),
            value, 36, TextGold, TextAlignmentOptions.Right);

        RectTransform plus = Anchored(Create("AddPointButton", row), new Vector2(1, 0.5f), new Vector2(-26, 0), new Vector2(92, 88));
        Btn(plus);
        Img(Center(Create("Icon", plus), Vector2.zero, new Vector2(44, 44)), "icons/icon-plus");
    }

    private static void ItemDetail(RectTransform screen, string buttonLabel)
    {
        RectTransform detail = Create("ItemDetail", screen);
        detail.anchorMin = new Vector2(0, 0);
        detail.anchorMax = new Vector2(1, 0);
        detail.pivot = new Vector2(0.5f, 0);
        detail.offsetMin = new Vector2(Margin, Margin);
        detail.offsetMax = new Vector2(-Margin, Margin + 200);
        Img(detail, "panels/panel-item-detail");

        RectTransform iconSlot = Anchored(Create("IconSlot", detail), new Vector2(0, 0.5f), new Vector2(30, 0), new Vector2(150, 150));
        Img(iconSlot, "controls/slot-common");
        Img(Center(Create("Icon", iconSlot), Vector2.zero, new Vector2(130, 130)), null).enabled = false;

        Text(Anchored(Create("ItemName", detail), new Vector2(0, 0.5f), new Vector2(204, 36), new Vector2(500, 56)),
            "아이템을 선택하세요", 36, TextMain, TextAlignmentOptions.Left);
        Text(Anchored(Create("ItemStat", detail), new Vector2(0, 0.5f), new Vector2(204, -36), new Vector2(500, 48)),
            "", 28, TextSub, TextAlignmentOptions.Left);

        RectTransform action = Anchored(Create("ActionButton", detail), new Vector2(1, 0.5f), new Vector2(-30, 0), new Vector2(240, 110));
        Btn(action).interactable = false;
        Text(Stretch(Create("Label", action), 10, 10, 10, 10), buttonLabel, 38, TextMain, TextAlignmentOptions.Center);
    }

    // ───────────────────────────── Bottom Nav ─────────────────────────────

    private static void BuildBottomNav(RectTransform parent)
    {
        RectTransform nav = Create("BottomNav", parent);
        nav.anchorMin = new Vector2(0, 0);
        nav.anchorMax = new Vector2(1, 0);
        nav.pivot = new Vector2(0.5f, 0);
        nav.offsetMin = Vector2.zero;
        nav.offsetMax = new Vector2(0, NavHeight);
        Img(nav, "panels/panel-main").raycastTarget = true;

        var layout = nav.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 14, 14);
        layout.spacing = 10;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = true;

        Tab(nav, "Tab_Battle", "icons/icon-battle", "전투", true);
        Tab(nav, "Tab_Equipment", "icons/icon-equipment", "장비", false);
        Tab(nav, "Tab_Bag", "icons/icon-bag", "가방", false);
        Tab(nav, "Tab_Stats", "icons/icon-stats", "능력치", false);
    }

    private static void Tab(RectTransform parent, string name, string icon, string label, bool selected)
    {
        RectTransform tab = Create(name, parent);
        Image bg = Img(tab, selected ? "controls/tab-selected" : "controls/tab-normal");
        bg.raycastTarget = true;
        var button = tab.gameObject.AddComponent<Button>();
        button.targetGraphic = bg;

        Img(Anchored(Create("Icon", tab), new Vector2(0.5f, 0.5f), new Vector2(0, 22), new Vector2(76, 76)), icon);
        Text(Anchored(Create("Label", tab), new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(220, 42)),
            label, 30, selected ? TextGold : TextSub, TextAlignmentOptions.Center);
    }

    // ───────────────────────────── 공용 부품 ─────────────────────────────

    private static RectTransform Slot(RectTransform parent, string name, float size)
    {
        RectTransform slot = Create(name, parent);
        slot.sizeDelta = new Vector2(size, size);
        Btn(slot, "controls/slot-common", "controls/slot-selected");

        Image icon = Img(Stretch(Create("Icon", slot), 12, 12, 12, 12), null);
        icon.preserveAspect = true;
        icon.enabled = false; // 아이템 있을 때만 켬
        return slot;
    }

    private static void Bar(RectTransform bar, string fillSprite, string label)
    {
        Img(bar, "controls/bar-track");

        Image fill = Img(Stretch(Create("Fill", bar), 6, 6, 6, 6), fillSprite);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0.66f; // 미리보기용

        if (label != null)
            Text(Stretch(Create("Value", bar)), label, 24, TextMain, TextAlignmentOptions.Center);
    }

    private static Button Btn(RectTransform rt, string normal = "controls/button-normal", string pressed = "controls/button-pressed")
    {
        Image bg = Img(rt, normal);
        bg.raycastTarget = true;

        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = bg;
        button.transition = Selectable.Transition.SpriteSwap;
        button.spriteState = new SpriteState
        {
            highlightedSprite = normal == "controls/button-normal" ? LoadSprite("controls/button-highlighted") : null,
            pressedSprite = LoadSprite(pressed),
            selectedSprite = null,
            disabledSprite = normal == "controls/button-normal" ? LoadSprite("controls/button-disabled") : null,
        };
        return button;
    }

    private static Image Img(RectTransform rt, string sprite)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.raycastTarget = false;

        if (sprite != null)
        {
            img.sprite = LoadSprite(sprite);
            if (img.sprite != null && img.sprite.border != Vector4.zero)
                img.type = Image.Type.Sliced;
        }
        return img;
    }

    private static TextMeshProUGUI Text(RectTransform rt, string text, float size, Color color, TextAlignmentOptions align)
    {
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow; // 영역보다 커도 숨기지 않음
        return tmp;
    }

    private static Sprite LoadSprite(string key)
    {
        if (spriteCache.TryGetValue(key, out Sprite cached)) return cached;

        string path = ArtRoot + key + ".png";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) Debug.LogWarning($"[LayoutBuilder] 스프라이트 없음: {path}");

        spriteCache[key] = sprite;
        return sprite;
    }

    // ───────────────────────────── RectTransform 헬퍼 ─────────────────────────────

    private static RectTransform Create(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    /// <summary> 부모 전체로 늘림 (여백: 좌, 우, 위, 아래) </summary>
    private static RectTransform Stretch(RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
        return rt;
    }

    /// <summary> 한 점에 고정 (anchor = pivot) </summary>
    private static RectTransform Anchored(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    private static RectTransform Center(RectTransform rt, Vector2 pos, Vector2 size)
    {
        return Anchored(rt, new Vector2(0.5f, 0.5f), pos, size);
    }

    /// <summary> 위에서 top 만큼 내려온 곳에 좌우 꽉 찬 블록 </summary>
    private static RectTransform TopBlock(RectTransform rt, float top, float height)
    {
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.offsetMin = new Vector2(Margin, -top - height);
        rt.offsetMax = new Vector2(-Margin, -top);
        return rt;
    }
}
