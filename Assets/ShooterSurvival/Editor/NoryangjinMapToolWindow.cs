#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public enum NoryangjinMapToolDirection
{
    North = 0,
    East = 1,
    South = 2,
    West = 3
}

public enum NoryangjinMapToolRoadTurn
{
    Straight = 0,
    Right90 = 1,
    Left90 = -1
}

public enum NoryangjinMapToolPaletteCategory
{
    All = 0,
    Road = 1,
    Building = 2,
    Prop = 3,
    Decoration = 4,
    Background = 5
}

public enum NoryangjinMapToolPaletteClickAction
{
    SelectInMapTool = 0,
    SelectPrefabAsset = 1,
    RenameDisplayName = 2,
    EditFootprint = 3
}

public enum NoryangjinMapToolJoystickCenterAction
{
    PlaceSelectedIcon = 0
}

public enum NoryangjinMapToolSceneGridCellState
{
    Empty = 0,
    Occupied = 1,
    LastPlaced = 2
}

public enum NoryangjinMapToolPlacementLayer
{
    Road = 0,
    Object = 1,
    SeagullPerch = 2,
    Background = 3
}

public readonly struct NoryangjinMapToolSceneViewPreset
{
    public NoryangjinMapToolSceneViewPreset(Quaternion rotation, float size, bool orthographic)
    {
        Rotation = rotation;
        Size = size;
        Orthographic = orthographic;
    }

    public Quaternion Rotation { get; }
    public float Size { get; }
    public bool Orthographic { get; }
}

public readonly struct NoryangjinMapToolOccupiedCell : IEquatable<NoryangjinMapToolOccupiedCell>
{
    public NoryangjinMapToolOccupiedCell(Vector2Int cell, NoryangjinMapToolPlacementLayer layer)
    {
        Cell = cell;
        Layer = layer;
    }

    public Vector2Int Cell { get; }
    public NoryangjinMapToolPlacementLayer Layer { get; }

    public bool Equals(NoryangjinMapToolOccupiedCell other)
    {
        return Cell == other.Cell && Layer == other.Layer;
    }

    public override bool Equals(object obj)
    {
        return obj is NoryangjinMapToolOccupiedCell other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Cell, Layer);
    }
}

public sealed class NoryangjinMapToolPaletteRenameWindow : EditorWindow
{
    private NoryangjinMapToolWindow owner;
    private string prefabPath;
    private string displayName;

    public static void Open(NoryangjinMapToolWindow owner, string prefabPath, string currentName)
    {
        NoryangjinMapToolPaletteRenameWindow window = CreateInstance<NoryangjinMapToolPaletteRenameWindow>();
        window.owner = owner;
        window.prefabPath = prefabPath;
        window.displayName = currentName;
        window.titleContent = new GUIContent("이름 변경");
        window.minSize = new Vector2(240f, 88f);
        window.maxSize = new Vector2(240f, 88f);
        window.ShowUtility();
        window.Focus();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("표시 이름", EditorStyles.boldLabel);
        displayName = EditorGUILayout.TextField(displayName);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("취소", GUILayout.Width(72f)))
                Close();

            if (GUILayout.Button("저장", GUILayout.Width(72f)))
            {
                owner.SetPaletteDisplayName(prefabPath, NoryangjinMapToolWindow.NormalizePaletteDisplayName(displayName));
                Close();
            }
        }
    }
}

public sealed class NoryangjinMapToolPaletteFootprintWindow : EditorWindow
{
    private NoryangjinMapToolWindow owner;
    private string prefabPath;
    private string displayName;
    private int width;
    private int depth;

    public static void Open(NoryangjinMapToolWindow owner, string prefabPath, string displayName, Vector2Int currentFootprint)
    {
        currentFootprint = NoryangjinMapToolWindow.NormalizeManualFootprint(currentFootprint);

        NoryangjinMapToolPaletteFootprintWindow window = CreateInstance<NoryangjinMapToolPaletteFootprintWindow>();
        window.owner = owner;
        window.prefabPath = prefabPath;
        window.displayName = displayName;
        window.width = currentFootprint.x;
        window.depth = currentFootprint.y;
        window.titleContent = new GUIContent("칸수 수정");
        window.minSize = new Vector2(240f, 112f);
        window.maxSize = new Vector2(240f, 112f);
        window.ShowUtility();
        window.Focus();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField(displayName, EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        width = EditorGUILayout.IntField("X 칸", width);
        depth = EditorGUILayout.IntField("Z 칸", depth);
        if (EditorGUI.EndChangeCheck())
        {
            Vector2Int normalized = NoryangjinMapToolWindow.NormalizeManualFootprint(new Vector2Int(width, depth));
            width = normalized.x;
            depth = normalized.y;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("취소", GUILayout.Width(72f)))
                Close();

            if (GUILayout.Button("저장", GUILayout.Width(72f)))
            {
                owner.SetPaletteManualFootprint(prefabPath, new Vector2Int(width, depth));
                Close();
            }
        }
    }
}

public static class NoryangjinMapToolGridUtility
{
    private const float FallbackCellSize = 1f;

    public static Vector3 GridToWorld(Vector3 origin, int gridX, int gridZ, float cellSize, float height)
    {
        cellSize = NormalizeCellSize(cellSize);

        return new Vector3(
            origin.x + gridX * cellSize,
            height,
            origin.z + gridZ * cellSize);
    }

    public static Vector3 SnapToGrid(Vector3 position, Vector3 origin, float cellSize, float height)
    {
        cellSize = NormalizeCellSize(cellSize);

        int gridX = Mathf.RoundToInt((position.x - origin.x) / cellSize);
        int gridZ = Mathf.RoundToInt((position.z - origin.z) / cellSize);
        return GridToWorld(origin, gridX, gridZ, cellSize, height);
    }

    public static float DirectionToYaw(NoryangjinMapToolDirection direction)
    {
        return direction switch
        {
            NoryangjinMapToolDirection.East => 90f,
            NoryangjinMapToolDirection.South => 180f,
            NoryangjinMapToolDirection.West => 270f,
            _ => 0f
        };
    }

    public static Vector2Int DirectionToStep(NoryangjinMapToolDirection direction)
    {
        return direction switch
        {
            NoryangjinMapToolDirection.East => new Vector2Int(1, 0),
            NoryangjinMapToolDirection.South => new Vector2Int(0, -1),
            NoryangjinMapToolDirection.West => new Vector2Int(-1, 0),
            _ => new Vector2Int(0, 1)
        };
    }

    public static NoryangjinMapToolDirection DirectionAfterRoadTurn(
        NoryangjinMapToolDirection direction,
        NoryangjinMapToolRoadTurn turn)
    {
        const int directionCount = 4;
        int directionIndex = ((int)direction + (int)turn + directionCount) % directionCount;
        return (NoryangjinMapToolDirection)directionIndex;
    }

    public static string BuildInstanceName(string category, string variant, int gridX, int gridZ)
    {
        return $"{category}_{variant}_X{gridX:+00;-00;+00}_Z{gridZ:+00;-00;+00}";
    }

    public static float NormalizeCellSize(float cellSize)
    {
        return cellSize > 0f ? cellSize : FallbackCellSize;
    }
}

public sealed class NoryangjinMapToolWindow : EditorWindow
{
    internal const string KoreanWindowTitle = "노량진 맵툴";
    internal const string MapToolScenePath = "Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode.unity";

    private const string PaletteDefaultsPath = "Assets/ShooterSurvival/Editor/NoryangjinMapToolPaletteDefaults.asset";
    private const string WorkFloorMaterialPath = "Assets/ShooterSurvival/Materials/Generated/MapTool_Work_Floor.mat";
    private const string WorkGridMaterialPath = "Assets/ShooterSurvival/Materials/Generated/MapTool_Work_Grid.mat";
    private const string WorkSubGridMaterialPath = "Assets/ShooterSurvival/Materials/Generated/MapTool_Work_SubGrid.mat";
    private const string OriginMarkerMaterialPath = "Assets/ShooterSurvival/Materials/Generated/MapTool_Origin_Marker.mat";
    private const string PrefabRoot = "Assets/ShooterSurvival/Prefabs/MeshyAI/Stage01_Noryangjin";
    private const string JhPrefabRoot = "Assets/JH/Prefab";
    internal const string JhWaterPrefabPath = JhPrefabRoot + "/water.prefab";
    internal const string SeagullPerchPrefabPath = PrefabRoot + "/008_STAGE01_NRY_PROPS_008_Seagull_perch_post/008_STAGE01_NRY_PROPS_008_Seagull_perch_post.prefab";
    internal const string DockMetalCleatPrefabPath = PrefabRoot + "/026_STAGE01_NRY_PROPS_014_Dock_metal_cleat/026_STAGE01_NRY_PROPS_014_Dock_metal_cleat.prefab";
    private const string RootName = "Noryangjin_MapTool";
    private const string RoadParentName = "Roads";
    private const string PropParentName = "Props";
    private const string WaterParentName = "Water";
    private const string WaterBackdropInstanceName = "Background_Water";
    private const string WorkFloorName = "MapTool_Work_Floor";
    private const string WorkGridName = "MapTool_Work_Grid";
    private const string OriginPostName = "MapTool_Origin_Post";
    internal static readonly string[] MapToolWorkObjectNames =
    {
        WorkFloorName,
        OriginPostName,
        WorkGridName
    };
    internal const string PlacementPreviewName = "MapTool_Placement_Preview";

    internal const string PositionMoveSectionTitle = "설치 조정";
    internal const string ObjectSectionTitle = "오브젝트";
    internal const string PlacementAngleSectionTitle = "설치 각도";
    internal const string PlacementAnglePaletteHint = "선택 프리팹의 다음 배치 각도";
    internal const string PlacementAnglePlacedObjectHint = "선택 오브젝트에 바로 적용";
    internal const string PlacementAngleNoSelectionHint = "프리팹을 고르면 배치 전 각도를 조정할 수 있습니다.";
    internal const string MapToolEnabledLabel = "ON";
    internal const string MapToolDisabledLabel = "OFF";
    internal const string MapToolDisabledHelp = "노량진 맵툴 비적용 상태입니다. ON으로 바꾸면 그리드, 프리뷰, 설치가 다시 적용됩니다.";
    internal const string RefreshMapToolButtonLabel = "리프레시";
    internal const string DeleteAllPlacedObjectsButtonLabel = "모두 삭제";
    internal const string DeleteAllPlacedObjectsNoTargetsMessage = "삭제할 배치 오브젝트가 없습니다.";
    internal const string EmptyPaletteItemPrefabPath = "__EMPTY_CELL__";
    internal const string EmptyPaletteItemLabel = "빈 칸";
    internal const int EmptyPaletteItemSortOrder = -1;
    internal const string SelectPaletteItemPrefabPath = "__SELECT_CELL__";
    internal const string SelectPaletteItemLabel = "선택";
    internal const string SelectPaletteItemIconText = "✓";
    internal const int SelectPaletteItemSortOrder = 0;
    internal const string ClearSelectionPaletteItemPrefabPath = "__CLEAR_SELECTION__";
    internal const string ClearSelectionPaletteItemLabel = "해제";
    internal const string ClearSelectionPaletteItemIconText = "X";
    internal const int ClearSelectionPaletteItemSortOrder = 1;
    internal const int RoadPaletteItemSortOrder = 2;
    internal const int BuiltinBackgroundPaletteItemSortOrder = 3;
    internal const NoryangjinMapToolJoystickCenterAction JoystickCenterAction = NoryangjinMapToolJoystickCenterAction.PlaceSelectedIcon;
    internal const float DefaultCellSize = 1.125f;
    private const float PreviousDefaultCellSize = 2.25f;
    private const float LegacyDefaultCellSize = 4.5f;
    internal const float WorkGridLineY = 0.04f;
    internal const float WorkGridLineWidth = 0.05f;
    internal const float WorkGridLineVerticalThickness = 0.004f;
    internal const float WorkSubGridLineWidth = 0.018f;
    internal const bool DrawTopViewWorkGridOverlay = true;
    internal const float WorkGridOverlayLineWidthPixels = 2.5f;
    internal const float WorkSubGridOverlayLineWidthPixels = 1f;
    private const float TopSceneViewForwardDotThreshold = 0.98f;
    internal const int WorkGridExtent = 100;
    internal const int WorkGridSubdivisionsPerCell = 5;
    internal const bool DefaultShowWorkSubGrid = false;
    internal static readonly bool DrawPlacementValidityFillAsGuiOverlay = true;
    internal const float PlacementPreviewAlpha = 0.5f;
    internal const float DefaultSceneViewSize = 60f;
    internal const float TopSceneViewSize = 120f;
    internal const float RotationQuickStepDegrees = 45f;
    internal const int CursorCellIconSize = 128;
    internal const int CursorCellSummaryWidth = 136;
    internal const int CursorCellLabelTopGap = 0;
    internal const int CursorCellLabelOffsetX = -6;
    internal const int CursorCellLabelOffsetY = -6;
    internal const int PositionMoveControlsMinHeight = 174;
    internal const bool DefaultShowSceneGrid = false;
    internal const bool ShowSceneDirectionArrow = false;
    internal const bool ShowJoystickPad = false;
    internal static readonly Vector3 RotationQuickAxis = Vector3.up;
    internal const string JoystickUpLabel = "^";
    internal const string JoystickLeftLabel = "<";
    internal const string JoystickCenterLabel = "OK";
    internal const string JoystickRightLabel = ">";
    internal const string JoystickDownLabel = "v";
    internal const int PaletteTileSize = 82;
    internal const int PaletteSidePadding = 10;
    internal const int PaletteTopPadding = 6;
    internal const int PaletteTileGap = 10;
    internal const int PaletteRowGap = 10;

    internal static readonly string[] PrimaryTabNames = Array.Empty<string>();
    internal static readonly string[] RotationQuickButtonLabels = { "-45도", "+45도" };
    internal const float HeightQuickStep = 0.1f;
    internal static readonly string[] HeightQuickButtonLabels = { "-0.1", "+0.1" };
    internal static readonly float[] PositionOffsetQuickSteps = { -0.01f, -0.1f, 0.1f, 0.01f };
    internal static readonly string[] PositionOffsetQuickButtonLabels = { "-0.01", "-0.1", "+0.1", "+0.01" };
    internal const string SlopeHighLabel = "위";
    internal const string SlopeLowLabel = "아래";
    internal const string SelectedObjectIndividualSaveButtonLabel = "개별 저장";
    internal const string SelectedObjectPrefabWideSaveButtonLabel = "프리팹 전체 적용";
    internal const string SelectedObjectIndividualSaveHint = "개별 저장: 위 값은 현재 선택 오브젝트에만 바로 적용됩니다.";
    internal const string SelectedObjectScaleSectionLabel = "스케일";
    internal static readonly string[] SelectedObjectScaleAxisLabels = { "X", "Z", "Y" };
    internal const string SelectedObjectMoveJoystickSectionLabel = "한 칸 이동";
    internal const string SelectedObjectAbsoluteRotationSectionLabel = "Y 회전";
    internal const string SelectedObjectMoveJoystickCenterLabel = "스냅";
    private static readonly NoryangjinMapToolPaletteCategory[] PaletteCategories =
    {
        NoryangjinMapToolPaletteCategory.All,
        NoryangjinMapToolPaletteCategory.Road,
        NoryangjinMapToolPaletteCategory.Building,
        NoryangjinMapToolPaletteCategory.Prop,
        NoryangjinMapToolPaletteCategory.Decoration,
        NoryangjinMapToolPaletteCategory.Background
    };

    private static readonly RoadPiece[] RoadPieces =
    {
        new RoadPiece(
            "Basic",
            "기본길",
            "Assets/polyperfect/Poly Universal Pack/Prefabs/Fantasy/Docks Fantasy/Pier_Long_Fantasy.prefab",
            NoryangjinMapToolRoadTurn.Straight),
        new RoadPiece(
            "LeftTurn",
            "좌회전길",
            "Assets/polyperfect/Poly Universal Pack/Prefabs/Fantasy/Docks Fantasy/Pier_Long_Fantasy_LeftTurn.prefab",
            NoryangjinMapToolRoadTurn.Left90),
        new RoadPiece(
            "RightTurn",
            "우회전길",
            "Assets/polyperfect/Poly Universal Pack/Prefabs/Fantasy/Docks Fantasy/Pier_Long_Fantasy_RightTurn.prefab",
            NoryangjinMapToolRoadTurn.Right90),
        new RoadPiece(
            "Bridge",
            "다리",
            "Assets/polyperfect/Poly Universal Pack/Prefabs/Fantasy/Bridges_Fantasy/Bridge_Rope_Small_Fantasy.prefab",
            NoryangjinMapToolRoadTurn.Straight),
        new RoadPiece(
            "Uphill",
            "오르막길",
            "Assets/polyperfect/Poly Universal Pack/Prefabs/Fantasy/Docks Fantasy/Pier_Rope_Stairs_Fantasy.prefab",
            NoryangjinMapToolRoadTurn.Straight,
            new RoadCompanion("Assets/polyperfect/Poly Universal Pack/Prefabs/Fantasy/Docks Fantasy/Pier_Pillars_Fantasy.prefab")),
        new RoadPiece(
            "Downhill",
            "내리막길",
            "Assets/polyperfect/Poly Universal Pack/Prefabs/Fantasy/Docks Fantasy/Pier_Rope_Stairs_Fantasy_Downhill.prefab",
            NoryangjinMapToolRoadTurn.Straight,
            new RoadCompanion(
                "Assets/polyperfect/Poly Universal Pack/Prefabs/Fantasy/Docks Fantasy/Pier_Pillars_Fantasy.prefab",
                new Vector3(0f, 1.075f, -2.233f)))
    };

    private static readonly string[] PalettePrefabRoots =
    {
        PrefabRoot,
        JhPrefabRoot
    };

    private static readonly Dictionary<string, string> KoreanPaletteLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Blue fish crate"] = "파란상자",
        ["Styrofoam fish box"] = "스티로박스",
        ["Ice fish tank"] = "얼음수조",
        ["Seafood push cart"] = "수산물수레",
        ["Wet floor safety cone"] = "주의콘",
        ["Buoy with rope post"] = "밧줄부표",
        ["Fish market lamp sign"] = "시장간판",
        ["Seagull perch post"] = "갈매기횃대",
        ["Dropped fish pickup"] = "생선줍기",
        ["Puffer enemy"] = "복어",
        ["Dock railing module"] = "부두난간",
        ["Rope post barrier"] = "밧줄기둥",
        ["Concrete seawall curb"] = "방파제턱",
        ["Fish market storefront facade"] = "수산상점",
        ["Sashimi restaurant stall front"] = "횟집가판",
        ["Seafood display stall module"] = "해산물가판",
        ["Ocean water plane backdrop"] = "바다배경",
        ["water"] = "물",
        ["Harbor fishing boat"] = "항구어선",
        ["Distant hillside village module"] = "언덕마을",
        ["Fish box stack"] = "상자더미",
        ["Aquarium tank row"] = "수족관줄",
        ["Ice box stack"] = "얼음박스",
        ["Standalone dock piling"] = "부두말뚝",
        ["Life ring buoy"] = "구명부표",
        ["Boat tire fender"] = "타이어완충",
        ["Dock metal cleat"] = "금속계선주",
        ["Red market hanging lamp"] = "빨간등",
        ["Crab mascot sign"] = "게간판",
        ["Fish mascot sign"] = "생선간판",
        ["Crab aquarium tank"] = "게 수족관",
        ["Octopus aquarium tank"] = "문어 수족관",
        ["Orange fish crate variant"] = "주황상자",
        ["White fish crate variant"] = "흰상자",
        ["Fishing net pile"] = "그물더미",
        ["Anchor prop"] = "닻소품",
        ["Harbor utility pole"] = "항구전신주",
        ["Floating sea buoy"] = "바다부표",
        ["Floating wooden plank"] = "나무판자",
        ["Ice chunk floor scatter"] = "얼음조각",
        ["Fish scrap floor scatter"] = "생선잔해",
        ["Flying seagull silhouette"] = "갈매기실루엣",
        ["Fishing boat detail kit"] = "어선소품",
        ["Market awning color variant set"] = "시장차양",
        ["Fish-market wooden X barricade"] = "나무바리케",
        ["Harbor lane signal gantry"] = "항구신호대",
        ["Pier Long Fantasy"] = "기본길",
        ["Pier Long Fantasy LeftTurn"] = "좌회전길",
        ["Pier Long Fantasy RightTurn"] = "우회전길",
        ["Bridge Rope Small Fantasy"] = "다리",
        ["Pier Rope Stairs Fantasy"] = "오르막길",
        ["Pier Rope Stairs Fantasy Downhill"] = "내리막길",
        ["Pier Pillars Fantasy"] = "오르막기둥"
    };

    [SerializeField] private Vector3 origin = Vector3.zero;
    [SerializeField] private float cellSize = DefaultCellSize;
    [SerializeField] private float placementHeight;
    [SerializeField] private int gridX;
    [SerializeField] private int gridZ;
    [SerializeField] private NoryangjinMapToolDirection direction;
    [SerializeField] private Vector3 propScale = Vector3.one;
    [SerializeField] private GameObject propPrefab;
    [SerializeField] private bool mapToolEnabled = true;
    [SerializeField] private bool advanceAfterRoad = true;
    [SerializeField] private bool showSceneGrid = DefaultShowSceneGrid;
    [SerializeField] private bool showWorkSubGrid = DefaultShowWorkSubGrid;
    [SerializeField] private bool showGridLabels = true;
    [SerializeField] private bool showCursor = true;
    [SerializeField] private int gridHalfExtent = 10;
    [SerializeField] private int selectedTab;
    [SerializeField] private bool showAdvancedSettings;
    [SerializeField] private bool showCursorControls;
    [SerializeField] private bool showSelectedItemSettings;
    [SerializeField] private bool isTopSceneView;
    [SerializeField] private NoryangjinMapToolPaletteCategory selectedPaletteCategory = NoryangjinMapToolPaletteCategory.Building;
    [SerializeField] private string selectedPalettePrefabPath;

    private Vector2 scroll;
    private Vector2 paletteScroll;
    private List<PaletteItem> paletteItems;
    private NoryangjinMapToolPaletteDefaults paletteDefaults;
    private GameObject placementPreviewInstance;
    private string placementPreviewPrefabPath;
    private List<Material> placementPreviewMaterials = new();
    private bool coarsePlacementSnapActive;
    private Vector2Int coarsePlacementSnapAnchor;
    private int lastPlacedObjectInstanceId;

    [MenuItem("Tools/MeshyAI/노량진 맵툴", false, 2305)]
    public static void Open()
    {
        NoryangjinMapToolWindow window = GetWindow<NoryangjinMapToolWindow>();
        window.titleContent = new GUIContent(KoreanWindowTitle);
        window.showSceneGrid = DefaultShowSceneGrid;
        window.showWorkSubGrid = DefaultShowWorkSubGrid;
        window.isTopSceneView = false;
        window.minSize = new Vector2(430f, 560f);
        window.Show();
    }

    [MenuItem("Tools/MeshyAI/노량진 맵툴 열기 또는 생성", false, 2306)]
    public static void OpenOrCreateMapToolScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MapToolScenePath) != null || File.Exists(MapToolScenePath))
        {
            EditorSceneManager.OpenScene(MapToolScenePath, OpenSceneMode.Single);
            if (SetupMapToolSceneDefaults())
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
        }
        else
        {
            string sceneDirectory = Path.GetDirectoryName(MapToolScenePath);
            if (!string.IsNullOrEmpty(sceneDirectory))
                Directory.CreateDirectory(sceneDirectory);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            SetupMapToolSceneDefaults();
            EditorSceneManager.SaveScene(scene, MapToolScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Open();
        Selection.activeGameObject = EnsureRoot();
        FrameMapToolSceneView();
    }

    private void OnEnable()
    {
        titleContent = new GUIContent(KoreanWindowTitle);
        cellSize = MigrateCellSizeDefault(cellSize);
        showSceneGrid = DefaultShowSceneGrid;
        ApplyMapToolWorkObjectsActiveState(mapToolEnabled);
        SceneView.duringSceneGui -= DrawSceneGrid;
        SceneView.duringSceneGui += DrawSceneGrid;
        Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        Undo.undoRedoPerformed += OnUndoRedoPerformed;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DrawSceneGrid;
        Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        DestroyPlacementPreview();
        DestroyPlacementPreviewMaterials();
    }

    private void OnGUI()
    {
        if (HandleUndoCommand(Event.current))
            return;

        DrawHeader();
        scroll = EditorGUILayout.BeginScrollView(scroll);
        if (!mapToolEnabled)
            EditorGUILayout.HelpBox(MapToolDisabledHelp, MessageType.Info);

        using (new EditorGUI.DisabledScope(!mapToolEnabled))
        {
            DrawPaletteControls();
        }

        EditorGUILayout.EndScrollView();
    }

    private bool HandleUndoCommand(Event currentEvent)
    {
        if (!IsUndoCommand(currentEvent))
            return false;

        if (currentEvent.type == EventType.KeyDown)
            Undo.PerformUndo();

        RefreshAfterUndoRedo();
        currentEvent.Use();
        return true;
    }

    private void OnUndoRedoPerformed()
    {
        RefreshAfterUndoRedo();
    }

    private void RefreshAfterUndoRedo()
    {
        ClearTransientMapToolVisualStateAfterUndo(ref coarsePlacementSnapActive, ref lastPlacedObjectInstanceId);
        DestroyPlacementPreview();
        RestoreMapToolSceneViewRenderState(isTopSceneView, FindSceneObjectByNameIncludingInactive(RootName));
        SceneView.RepaintAll();
        Repaint();
    }

    internal static void ClearTransientMapToolVisualStateAfterUndo(
        ref bool coarseSnapActive,
        ref int lastPlacedInstanceId)
    {
        coarseSnapActive = false;
        lastPlacedInstanceId = 0;
    }

    private int BeginMapToolUndoGroup(string undoName)
    {
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(undoName);
        Undo.RecordObject(this, undoName);
        return undoGroup;
    }

    private void DrawHeader()
    {
        bool isToolScene = IsMapToolScenePath(SceneManager.GetActiveScene().path);
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            bool enabled = GUILayout.Toggle(
                mapToolEnabled,
                mapToolEnabled ? MapToolEnabledLabel : MapToolDisabledLabel,
                EditorStyles.toolbarButton,
                GUILayout.Width(44f));
            if (enabled != mapToolEnabled)
                SetMapToolEnabled(enabled);

            GUILayout.Label(FormatCursorStatus(isToolScene, gridX, gridZ, direction, cellSize), EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("씬", EditorStyles.toolbarButton, GUILayout.Width(36f)))
            {
                OpenOrCreateMapToolScene();
                ApplyMapToolWorkObjectsActiveState(mapToolEnabled);
            }

            if (GUILayout.Button(RefreshMapToolButtonLabel, EditorStyles.toolbarButton, GUILayout.Width(64f)))
                RefreshMapToolVisibility();

            if (GUILayout.Button(isTopSceneView ? "원래뷰" : "탑뷰", EditorStyles.toolbarButton, GUILayout.Width(56f)))
                ToggleMapToolSceneView();

            if (GUILayout.Button("루트", EditorStyles.toolbarButton, GUILayout.Width(42f)))
                Selection.activeGameObject = EnsureRoot();
        }
    }

    private void SetMapToolEnabled(bool enabled)
    {
        mapToolEnabled = enabled;
        ApplyMapToolWorkObjectsActiveState(mapToolEnabled);
        if (!mapToolEnabled)
        {
            coarsePlacementSnapActive = false;
            DestroyPlacementPreview();
        }

        SceneView.RepaintAll();
        Repaint();
    }

    private void RefreshMapToolVisibility()
    {
        mapToolEnabled = true;
        coarsePlacementSnapActive = false;
        lastPlacedObjectInstanceId = 0;
        Selection.activeObject = null;
        DestroyPlacementPreview();
        DestroyPlacementPreviewMaterials();

        GameObject root = FindOrCreateRootIncludingInactive();
        bool changed = RestoreMapToolVisibleObjects(root.transform, recordUndo: false);
        changed |= SetupMapToolSceneDefaults();
        changed |= ApplyMapToolWorkObjectsActiveState(true, recordUndo: false);
        changed |= DestroyTransientPlacementPreviewObjects(recordUndo: false);
        RestoreMapToolSceneViewRenderState(isTopSceneView, root);

        if (changed && root.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(root.scene);

        AssetDatabase.Refresh();
        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
        Repaint();
    }

    private static bool ApplyMapToolWorkObjectsActiveState(bool active)
    {
        return ApplyMapToolWorkObjectsActiveState(active, recordUndo: true);
    }

    private static bool ApplyMapToolWorkObjectsActiveState(bool active, bool recordUndo)
    {
        GameObject root = GameObject.Find(RootName);
        if (root == null)
            return false;

        bool changed = false;
        string undoName = active ? "Enable Noryangjin Map Tool Work Objects" : "Disable Noryangjin Map Tool Work Objects";
        foreach (string objectName in MapToolWorkObjectNames)
        {
            Transform child = root.transform.Find(objectName);
            if (child == null || child.gameObject.activeSelf == active)
                continue;

            if (recordUndo)
                Undo.RecordObject(child.gameObject, undoName);
            child.gameObject.SetActive(active);
            EditorUtility.SetDirty(child.gameObject);
            changed = true;
        }

        if (changed)
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        return changed;
    }

    internal static bool RestoreMapToolVisibleObjects(Transform root)
    {
        return RestoreMapToolVisibleObjects(root, recordUndo: true);
    }

    internal static bool RestoreMapToolVisibleObjects(Transform root, bool recordUndo)
    {
        if (root == null)
            return false;

        bool changed = false;
        const string undoName = "Refresh Noryangjin Map Tool Visibility";
        changed |= SetGameObjectActive(root.gameObject, true, undoName, recordUndo);

        changed |= RestoreMapToolChildHierarchy(root, RoadParentName, undoName, recordUndo, enableRenderers: true);
        changed |= RestoreMapToolChildHierarchy(root, PropParentName, undoName, recordUndo, enableRenderers: true);
        changed |= RestoreMapToolChildHierarchy(root, WaterParentName, undoName, recordUndo, enableRenderers: true);

        foreach (string objectName in MapToolWorkObjectNames)
            changed |= RestoreMapToolChildHierarchy(root, objectName, undoName, recordUndo, enableRenderers: false);

        return changed;
    }

    private static void RestoreMapToolSceneViewRenderState(bool topView, GameObject root)
    {
        if (root != null)
            SceneVisibilityManager.instance.Show(root, true);
        else
            SceneVisibilityManager.instance.ShowAll();

        foreach (SceneView sceneView in SceneView.sceneViews)
        {
            if (sceneView == null)
                continue;

            sceneView.cameraMode = SceneView.GetBuiltinCameraMode(DrawCameraMode.Textured);
            sceneView.in2DMode = false;
        }

        ApplyMapToolSceneViewPreset(BuildSceneViewPreset(topView));
    }

    private static bool RestoreMapToolChildHierarchy(
        Transform root,
        string childName,
        string undoName,
        bool recordUndo,
        bool enableRenderers)
    {
        Transform child = root != null ? root.Find(childName) : null;
        if (child == null)
            return false;

        bool changed = false;
        foreach (Transform descendant in child.GetComponentsInChildren<Transform>(true))
        {
            changed |= SetGameObjectActive(descendant.gameObject, true, undoName, recordUndo);
            if (enableRenderers)
                changed |= SetRendererEnabled(descendant.gameObject, true);
        }

        return changed;
    }

    private static bool SetGameObjectActive(GameObject target, bool active, string undoName, bool recordUndo)
    {
        if (target == null || target.activeSelf == active)
            return false;

        if (recordUndo)
            Undo.RecordObject(target, undoName);
        target.SetActive(active);
        EditorUtility.SetDirty(target);
        return true;
    }

    private void DrawPaletteControls()
    {
        DrawPositionMoveSection();
        EditorGUILayout.Space(6f);
        DrawObjectSection();
    }

    private void DrawPositionMoveSection()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(PositionMoveSectionTitle, EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label($"X {gridX} / Z {gridZ}", EditorStyles.miniBoldLabel);
                GUILayout.FlexibleSpace();
            }

            using (new EditorGUILayout.HorizontalScope(GUILayout.MinHeight(PositionMoveControlsMinHeight)))
            {
                GUILayout.Space(8f);
                DrawCursorCellObjectSummary();
                GUILayout.Space(18f);
                DrawPlacementAngleControl();
                GUILayout.FlexibleSpace();
            }

            GUILayout.Space(6f);
        }
    }

    private void DrawCursorCellObjectSummary()
    {
        GameObject target = FindPlacedObjectAtCursor();
        string prefabPath = GetPrefabAssetPathForPlacedObject(target);
        string label = BuildCursorCellObjectLabel(prefabPath);
        Texture2D preview = GetCursorCellObjectPreview(prefabPath);
        GUIStyle centeredNameStyle = new GUIStyle(EditorStyles.miniBoldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            clipping = TextClipping.Clip
        };

        using (new EditorGUILayout.VerticalScope(GUILayout.Width(CursorCellSummaryWidth)))
        {
            Rect iconSlotRect = GUILayoutUtility.GetRect(
                CursorCellSummaryWidth,
                CursorCellIconSize,
                GUILayout.Width(CursorCellSummaryWidth),
                GUILayout.Height(CursorCellIconSize));
            Rect iconRect = new Rect(
                iconSlotRect.x + (iconSlotRect.width - CursorCellIconSize) * 0.5f,
                iconSlotRect.y,
                CursorCellIconSize,
                CursorCellIconSize);
            GUI.Box(iconRect, GUIContent.none);
            if (preview != null)
                GUI.DrawTexture(new Rect(iconRect.x + 4f, iconRect.y + 4f, CursorCellIconSize - 8f, CursorCellIconSize - 8f), preview, ScaleMode.ScaleToFit);
            else
                GUI.Label(iconRect, label, EditorStyles.centeredGreyMiniLabel);

            GUILayout.Space(CursorCellLabelTopGap);
            Rect labelRect = GUILayoutUtility.GetRect(
                new GUIContent(label),
                centeredNameStyle,
                GUILayout.Width(CursorCellSummaryWidth),
                GUILayout.Height(18f));
            labelRect.x += CursorCellLabelOffsetX;
            labelRect.y += CursorCellLabelOffsetY;
            GUI.Label(labelRect, label, centeredNameStyle);

            GameObject selectedTarget = GetRotationTarget();
            if (selectedTarget != null)
                DrawSelectedObjectMoveJoystick(selectedTarget);
        }
    }

    private void DrawObjectSection()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(ObjectSectionTitle, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                Color oldBackgroundColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.38f, 0.32f, 1f);
                if (GUILayout.Button(DeleteAllPlacedObjectsButtonLabel, GUILayout.Width(82f), GUILayout.Height(22f)))
                    DeleteAllPlacedObjects();
                GUI.backgroundColor = oldBackgroundColor;
            }

            DrawPaletteCategoryToolbar();
            DrawPaletteGrid();
        }
    }

    private void DrawJoystickPad()
    {
        const float buttonSize = 34f;

        using (new EditorGUILayout.VerticalScope(GUILayout.Width(buttonSize * 3f)))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                GUILayout.Space(buttonSize);
                if (GUILayout.Button(JoystickUpLabel, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
                    MoveCursorBy(0, 1);
                GUILayout.Space(buttonSize);
                GUILayout.FlexibleSpace();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(JoystickLeftLabel, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
                    MoveCursorBy(-1, 0);
                if (GUILayout.Button(JoystickCenterLabel, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
                    PlaceSelectedPaletteItem();
                if (GUILayout.Button(JoystickRightLabel, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
                    MoveCursorBy(1, 0);
                GUILayout.FlexibleSpace();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                GUILayout.Space(buttonSize);
                if (GUILayout.Button(JoystickDownLabel, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
                    MoveCursorBy(0, -1);
                GUILayout.Space(buttonSize);
                GUILayout.FlexibleSpace();
            }
        }
    }

    private void DrawPlacementAngleControl()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(220f)))
        {
            GUILayout.Label(PlacementAngleSectionTitle, EditorStyles.miniBoldLabel);

            GameObject target = GetRotationTarget();
            if (target == null)
            {
                DrawSelectedPalettePlacementAngleControl();
                return;
            }

            DrawSelectedObjectPlacementAngleControl(target);

            GUILayout.Space(4f);
            DrawSelectedObjectScaleFields(target);

            GUILayout.Space(4f);
            DrawSelectedObjectPositionOffsetFields(target);

            GUILayout.Space(4f);
            GUILayout.Label($"Y {target.transform.position.y:0.0}", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(HeightQuickButtonLabels[0], GUILayout.Height(22f)))
                    MoveSelectedObjectHeight(target, -HeightQuickStep);
                if (GUILayout.Button(HeightQuickButtonLabels[1], GUILayout.Height(22f)))
                    MoveSelectedObjectHeight(target, HeightQuickStep);
            }
        }
    }

    private void DrawSelectedPalettePlacementAngleControl()
    {
        PaletteItem? selectedItem = FindSelectedPaletteItem();
        if (!selectedItem.HasValue || !ShouldShowPlacementPreview(selectedItem.Value.PrefabPath))
        {
            using (new EditorGUI.DisabledScope(true))
            {
                DrawPlacementYawField(0f);
                DrawPlacementYawQuickButtons(null);
            }

            GUILayout.Label(PlacementAngleNoSelectionHint, EditorStyles.wordWrappedMiniLabel);
            return;
        }

        PaletteItem item = selectedItem.Value;
        NoryangjinMapToolPalettePlacementEntry entry = GetPaletteDefaults().GetOrCreateEntry(item.PrefabPath);

        GUILayout.Label(PlacementAnglePaletteHint, EditorStyles.wordWrappedMiniLabel);
        EditorGUI.BeginChangeCheck();
        float yawOffset = DrawPlacementYawField(entry.yawOffset);
        if (EditorGUI.EndChangeCheck())
            ApplySelectedPaletteYawOffset(item, entry, yawOffset);

        DrawPlacementYawQuickButtons(deltaY =>
        {
            ApplySelectedPaletteYawOffset(item, entry, MovePlacementYawOffsetByStep(entry.yawOffset, deltaY));
        });
    }

    private void DrawSelectedObjectPlacementAngleControl(GameObject target)
    {
        if (!TryGetPlacedObjectPrefabBaseRotation(target, out Quaternion prefabBaseRotation))
        {
            GUILayout.Label(PlacementAnglePlacedObjectHint, EditorStyles.wordWrappedMiniLabel);
            Vector3 currentEuler = NormalizeEulerForInspector(target.transform.eulerAngles);
            EditorGUI.BeginChangeCheck();
            Vector3 nextEuler = DrawRotationFields(currentEuler);
            if (EditorGUI.EndChangeCheck())
                ApplyCursorObjectRotation(target, nextEuler);

            DrawRotationQuickButtons(target);
            return;
        }

        GUILayout.Label("선택 오브젝트 / 다음 배치 기본 Y", EditorStyles.wordWrappedMiniLabel);
        float yawOffset = CalculatePaletteYawOffsetFromPlacedRotation(target.transform.rotation, prefabBaseRotation);
        EditorGUI.BeginChangeCheck();
        float nextYawOffset = DrawPlacementYawField(yawOffset);
        if (EditorGUI.EndChangeCheck())
            ApplySelectedObjectPaletteYaw(target, prefabBaseRotation, nextYawOffset);

        DrawPlacementYawQuickButtons(deltaY =>
        {
            ApplySelectedObjectPaletteYaw(target, prefabBaseRotation, MovePlacementYawOffsetByStep(yawOffset, deltaY));
        });
    }

    private static float DrawPlacementYawField(float yawOffset)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("Y", GUILayout.Width(14f));
            return EditorGUILayout.FloatField(yawOffset, GUILayout.Width(72f));
        }
    }

    private static void DrawPlacementYawQuickButtons(Action<float> applyDelta)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(applyDelta == null))
            {
                if (GUILayout.Button(RotationQuickButtonLabels[0], GUILayout.Height(22f)))
                    applyDelta?.Invoke(-RotationQuickStepDegrees);
                if (GUILayout.Button(RotationQuickButtonLabels[1], GUILayout.Height(22f)))
                    applyDelta?.Invoke(RotationQuickStepDegrees);
            }
        }
    }

    private void DrawRotationQuickButtons(GameObject target)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(RotationQuickButtonLabels[0], GUILayout.Height(22f)) && target != null)
                RotateSelectedObjectY(target, -RotationQuickStepDegrees);
            if (GUILayout.Button(RotationQuickButtonLabels[1], GUILayout.Height(22f)) && target != null)
                RotateSelectedObjectY(target, RotationQuickStepDegrees);
        }
    }

    private void DrawSelectedObjectScaleFields(GameObject target)
    {
        if (target == null)
            return;

        GUILayout.Label(SelectedObjectScaleSectionLabel, EditorStyles.miniBoldLabel);
        Vector3 scale = target.transform.localScale;
        EditorGUI.BeginChangeCheck();
        float displayedX = scale.x;
        float displayedZ = scale.z;
        float displayedY = scale.y;
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(SelectedObjectScaleAxisLabels[0], GUILayout.Width(12f));
            displayedX = EditorGUILayout.DelayedFloatField(displayedX, GUILayout.Width(50f));
            GUILayout.Label(SelectedObjectScaleAxisLabels[1], GUILayout.Width(12f));
            displayedZ = EditorGUILayout.DelayedFloatField(displayedZ, GUILayout.Width(50f));
            GUILayout.Label(SelectedObjectScaleAxisLabels[2], GUILayout.Width(12f));
            displayedY = EditorGUILayout.DelayedFloatField(displayedY, GUILayout.Width(50f));
        }

        if (EditorGUI.EndChangeCheck())
            ApplySelectedObjectScale(target, BuildSelectedObjectScaleFromDisplayedFields(displayedX, displayedZ, displayedY));
    }

    private void DrawSelectedObjectPositionOffsetFields(GameObject target)
    {
        if (target == null || !TryGetMapToolPlacedObjectGridPosition(target.name, out Vector2Int anchor))
            return;

        float placementGridCellSize = BuildPlacementSnapCellSize(cellSize, false);
        Vector3 offset = CalculatePlacedObjectPositionOffset(
            target.transform.position,
            origin,
            anchor,
            placementGridCellSize,
            placementHeight);
        GUILayout.Label("X/Z/Y 이동 오프셋", EditorStyles.miniBoldLabel);
        EditorGUI.BeginChangeCheck();
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("X", GUILayout.Width(12f));
            offset.x = EditorGUILayout.FloatField(offset.x, GUILayout.Width(58f));
            GUILayout.Label("Z", GUILayout.Width(12f));
            offset.z = EditorGUILayout.FloatField(offset.z, GUILayout.Width(58f));
            GUILayout.Label("Y", GUILayout.Width(12f));
            offset.y = EditorGUILayout.FloatField(offset.y, GUILayout.Width(58f));
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("X", GUILayout.Width(12f));
            for (int i = 0; i < PositionOffsetQuickSteps.Length; i++)
            {
                if (GUILayout.Button(PositionOffsetQuickButtonLabels[i], GUILayout.Height(22f)))
                    offset.x = MovePositionOffsetByStep(offset.x, PositionOffsetQuickSteps[i]);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("Z", GUILayout.Width(12f));
            for (int i = 0; i < PositionOffsetQuickSteps.Length; i++)
            {
                if (GUILayout.Button(PositionOffsetQuickButtonLabels[i], GUILayout.Height(22f)))
                    offset.z = MovePositionOffsetByStep(offset.z, PositionOffsetQuickSteps[i]);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("Y", GUILayout.Width(12f));
            for (int i = 0; i < PositionOffsetQuickSteps.Length; i++)
            {
                if (GUILayout.Button(PositionOffsetQuickButtonLabels[i], GUILayout.Height(22f)))
                    offset.y = MovePositionOffsetByStep(offset.y, PositionOffsetQuickSteps[i]);
            }
        }

        if (EditorGUI.EndChangeCheck())
            ApplySelectedObjectPositionOffset(target, anchor, offset);

        GUILayout.Label(SelectedObjectIndividualSaveHint, EditorStyles.wordWrappedMiniLabel);
        if (GUILayout.Button(SelectedObjectIndividualSaveButtonLabel, GUILayout.Height(22f)))
            SaveSelectedObjectIndividualPlacement(target);

        using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(GetPrefabAssetPathForPlacedObject(target))))
        {
            GUIContent prefabWideSaveContent = new GUIContent(
                SelectedObjectPrefabWideSaveButtonLabel,
                "현재 선택 오브젝트의 위치, 높이, 회전, 크기를 같은 프리팹의 배치 기본값으로 저장합니다.");
            if (GUILayout.Button(prefabWideSaveContent, GUILayout.Height(22f)))
                SaveSelectedObjectAsPrefabPlacementDefault(target, anchor);
        }
    }

    private void DrawSelectedObjectMoveJoystick(GameObject target)
    {
        const float buttonWidth = 42f;
        const float buttonHeight = 23f;

        GUILayout.Space(5f);
        GUILayout.Label(SelectedObjectMoveJoystickSectionLabel, EditorStyles.miniBoldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            GUILayout.Space(buttonWidth);
            if (GUILayout.Button("위", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                MoveSelectedObjectByGridStep(target, 0, 1);
            GUILayout.Space(buttonWidth);
            GUILayout.FlexibleSpace();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("왼쪽", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                MoveSelectedObjectByGridStep(target, -1, 0);
            if (GUILayout.Button(SelectedObjectMoveJoystickCenterLabel, GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                SnapSelectionToGrid();
            if (GUILayout.Button("오른쪽", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                MoveSelectedObjectByGridStep(target, 1, 0);
            GUILayout.FlexibleSpace();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            GUILayout.Space(buttonWidth);
            if (GUILayout.Button("아래", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                MoveSelectedObjectByGridStep(target, 0, -1);
            GUILayout.Space(buttonWidth);
            GUILayout.FlexibleSpace();
        }

        GUILayout.Space(5f);
        GUILayout.Label(SelectedObjectAbsoluteRotationSectionLabel, EditorStyles.miniBoldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("-180", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                RotateSelectedObjectY(target, -180f);
            if (GUILayout.Button("+180", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                RotateSelectedObjectY(target, 180f);
            GUILayout.FlexibleSpace();
        }
    }

    private static Vector3 DrawRotationFields(Vector3 euler)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            euler.x = EditorGUILayout.FloatField(euler.x, GUILayout.Width(52f));
            euler.y = EditorGUILayout.FloatField(euler.y, GUILayout.Width(52f));
            euler.z = EditorGUILayout.FloatField(euler.z, GUILayout.Width(52f));
        }

        return euler;
    }

    private void DrawCompactPlacementBar(Vector3 cursorWorld)
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            GUILayout.Label($"X {gridX}", EditorStyles.boldLabel, GUILayout.Width(44f));
            GUILayout.Label($"Z {gridZ}", EditorStyles.boldLabel, GUILayout.Width(44f));
            direction = (NoryangjinMapToolDirection)EditorGUILayout.EnumPopup(direction, GUILayout.Width(88f));
            GUILayout.Label($"Y {cursorWorld.y:0.##}", EditorStyles.miniLabel, GUILayout.Width(54f));
            GUILayout.FlexibleSpace();

            showCursorControls = GUILayout.Toggle(showCursorControls, "커서", EditorStyles.miniButton, GUILayout.Width(48f));
            showSelectedItemSettings = GUILayout.Toggle(showSelectedItemSettings, "설정", EditorStyles.miniButton, GUILayout.Width(48f));
        }
    }

    private void DrawSelectedPaletteSettings()
    {
        PaletteItem? selectedItem = FindSelectedPaletteItem();
        if (!selectedItem.HasValue)
        {
            EditorGUILayout.HelpBox("아이콘을 선택하면 개별 크기, 회전, 높이를 설정할 수 있습니다.", MessageType.Info);
            return;
        }

        NoryangjinMapToolPalettePlacementEntry entry = GetPaletteDefaults().GetOrCreateEntry(selectedItem.Value.PrefabPath);

        EditorGUILayout.Space(8f);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("선택 아이콘 설정", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(selectedItem.Value.Label, EditorStyles.miniLabel);

            EditorGUI.BeginChangeCheck();
            entry.scale = EditorGUILayout.Vector3Field("크기 보정", entry.scale);
            Vector3 placementOffset = new(entry.positionOffset.x, entry.heightOffset, entry.positionOffset.y);
            EditorGUILayout.LabelField("X/Z/Y 오프셋");
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("X", GUILayout.Width(12f));
                placementOffset.x = EditorGUILayout.FloatField(placementOffset.x, GUILayout.Width(58f));
                GUILayout.Label("Z", GUILayout.Width(12f));
                placementOffset.z = EditorGUILayout.FloatField(placementOffset.z, GUILayout.Width(58f));
                GUILayout.Label("Y", GUILayout.Width(12f));
                placementOffset.y = EditorGUILayout.FloatField(placementOffset.y, GUILayout.Width(58f));
            }

            entry.positionOffset = new Vector2(placementOffset.x, placementOffset.z);
            entry.heightOffset = placementOffset.y;
            entry.yawOffset = EditorGUILayout.FloatField("개별 보정 Y", entry.yawOffset);

            if (EditorGUI.EndChangeCheck())
                SavePaletteDefaults();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("현재 선택값 저장"))
                    SavePaletteDefaults();

                if (GUILayout.Button("기본값으로 되돌리기"))
                {
                    GetPaletteDefaults().ResetEntry(selectedItem.Value.PrefabPath);
                    SavePaletteDefaults();
                }
            }
        }
    }

    private void DrawCompactSelectedItemBar()
    {
        PaletteItem? selectedItem = FindSelectedPaletteItem();
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            string label = selectedItem.HasValue ? selectedItem.Value.Label : "아이콘을 선택하세요";
            GUILayout.Label(label, EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(!selectedItem.HasValue))
            {
                if (GUILayout.Button("배치", GUILayout.Width(92f), GUILayout.Height(28f)))
                    PlaceSelectedPaletteItem();
            }
        }
    }

    private void DrawPaletteCategoryToolbar()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            foreach (NoryangjinMapToolPaletteCategory category in PaletteCategories)
            {
                bool selected = selectedPaletteCategory == category;
                GUIStyle style = selected ? EditorStyles.miniButtonMid : EditorStyles.miniButton;
                if (GUILayout.Button(PaletteCategoryToKorean(category), style, GUILayout.Height(26f)))
                    selectedPaletteCategory = category;
            }
        }
    }

    private void DrawPaletteGrid()
    {
        List<PaletteItem> items = GetPaletteItems();
        int visibleCount = 0;
        int columns = CalculatePaletteColumnCount(position.width - 28f - PaletteSidePadding * 2f);

        float availableHeight = Mathf.Max(120f, position.height - 42f);
        paletteScroll = EditorGUILayout.BeginScrollView(paletteScroll, GUILayout.Height(availableHeight));
        GUILayout.Space(PaletteTopPadding);

        for (int i = 0; i < items.Count; i++)
        {
            PaletteItem item = items[i];
            if (!IsPaletteItemVisible(item))
                continue;

            if (visibleCount % columns == 0)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(PaletteSidePadding);
            }
            else
            {
                GUILayout.Space(PaletteTileGap);
            }

            DrawPaletteTile(item, PaletteTileSize);
            visibleCount++;

            if (visibleCount % columns == 0)
            {
                GUILayout.Space(PaletteSidePadding);
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(PaletteRowGap);
            }
        }

        if (visibleCount % columns != 0)
        {
            GUILayout.FlexibleSpace();
            GUILayout.Space(PaletteSidePadding);
            EditorGUILayout.EndHorizontal();
        }

        if (visibleCount == 0)
            EditorGUILayout.HelpBox("이 카테고리에 표시할 프리팹이 없습니다.", MessageType.Info);

        EditorGUILayout.EndScrollView();
    }

    private void DrawPaletteTile(PaletteItem item, int tileSize)
    {
        bool selected = string.Equals(selectedPalettePrefabPath, item.PrefabPath, StringComparison.Ordinal);
        GUIStyle tileStyle = selected ? "flow node 0 on" : "flow node 0";
        Texture2D preview = item.Prefab == null ? null : AssetPreview.GetAssetPreview(item.Prefab) ?? AssetPreview.GetMiniThumbnail(item.Prefab) as Texture2D;

        using (new EditorGUILayout.VerticalScope(tileStyle, GUILayout.Width(tileSize), GUILayout.Height(tileSize + 28)))
        {
            Rect imageRect = GUILayoutUtility.GetRect(tileSize - 8, tileSize - 8, GUILayout.Width(tileSize - 8), GUILayout.Height(tileSize - 8));
            GUIContent content = new GUIContent(preview, item.Label);
            Event currentEvent = Event.current;
            bool hasFootprintBadge = TryGetPaletteFootprintBadge(item, imageRect, out string footprintLabel, out Rect footprintBadgeRect);

            if (currentEvent.type == EventType.MouseDown && imageRect.Contains(currentEvent.mousePosition))
            {
                if (hasFootprintBadge &&
                    footprintBadgeRect.Contains(currentEvent.mousePosition) &&
                    ShouldEditPaletteFootprintBadge(currentEvent.clickCount, currentEvent.button))
                {
                    selectedPalettePrefabPath = item.PrefabPath;
                    OpenPaletteFootprintWindow(item);
                    currentEvent.Use();
                    return;
                }

                if (IsClearSelectionPaletteItemPath(item.PrefabPath))
                {
                    ClearMapToolSelection();
                    currentEvent.Use();
                    return;
                }

                selectedPalettePrefabPath = item.PrefabPath;

                if (GetPaletteTileClickAction(currentEvent.clickCount, currentEvent.button) == NoryangjinMapToolPaletteClickAction.SelectPrefabAsset)
                    SelectPalettePrefabAsset(item);

                currentEvent.Use();
            }

            GUI.Button(imageRect, content);
            DrawSpecialPaletteIcon(item, imageRect);
            DrawPaletteFootprintBadge(footprintLabel, footprintBadgeRect, hasFootprintBadge);

            Rect labelRect = GUILayoutUtility.GetRect(new GUIContent(item.Label), EditorStyles.miniLabel, GUILayout.Width(tileSize - 8));
            GUI.Label(labelRect, item.Label, EditorStyles.miniLabel);

            if (currentEvent.type == EventType.MouseDown && labelRect.Contains(currentEvent.mousePosition) &&
                GetPaletteLabelClickAction(currentEvent.clickCount, currentEvent.button) == NoryangjinMapToolPaletteClickAction.RenameDisplayName)
            {
                selectedPalettePrefabPath = item.PrefabPath;
                OpenPaletteRenameWindow(item);
                currentEvent.Use();
            }
        }
    }

    private void OpenPaletteRenameWindow(PaletteItem item)
    {
        NoryangjinMapToolPaletteRenameWindow.Open(this, item.PrefabPath, item.Label);
    }

    private void OpenPaletteFootprintWindow(PaletteItem item)
    {
        Vector2Int currentFootprint = GetPaletteItemFootprint(item, NoryangjinMapToolGridUtility.NormalizeCellSize(cellSize));
        NoryangjinMapToolPaletteFootprintWindow.Open(this, item.PrefabPath, item.Label, currentFootprint);
    }

    private void SelectPalettePrefabAsset(PaletteItem item)
    {
        if (string.IsNullOrEmpty(item.PrefabPath))
            return;

        OpenProjectAsset(item.PrefabPath);
        Repaint();
    }

    internal void SetPaletteDisplayName(string prefabPath, string displayName)
    {
        GetPaletteDefaults().SetCustomLabel(prefabPath, NormalizePaletteDisplayName(displayName));
        SavePaletteDefaults();
        paletteItems = null;
        Repaint();
    }

    internal void SetPaletteManualFootprint(string prefabPath, Vector2Int footprint)
    {
        NoryangjinMapToolPalettePlacementEntry entry = GetPaletteDefaults().GetOrCreateEntry(prefabPath);
        entry.useManualFootprint = true;
        entry.manualFootprint = NormalizeManualFootprint(footprint);
        SavePaletteDefaults();
        Repaint();
        SceneView.RepaintAll();
    }

    private bool IsPaletteItemVisible(PaletteItem item)
    {
        if (string.Equals(item.PrefabPath, EmptyPaletteItemPrefabPath, StringComparison.Ordinal))
            return true;
        if (IsSelectPaletteItemPath(item.PrefabPath) || IsClearSelectionPaletteItemPath(item.PrefabPath))
            return true;

        return selectedPaletteCategory == NoryangjinMapToolPaletteCategory.All || item.Category == selectedPaletteCategory;
    }

    private PaletteItem? FindSelectedPaletteItem()
    {
        if (selectedPalettePrefabPath == null)
            return null;

        foreach (PaletteItem item in GetPaletteItems())
        {
            if (string.Equals(item.PrefabPath, selectedPalettePrefabPath, StringComparison.Ordinal))
                return item;
        }

        return null;
    }

    private void DrawCursorPad()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label($"X {gridX} / Z {gridZ}", EditorStyles.boldLabel, GUILayout.Width(96f));
            GUILayout.FlexibleSpace();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("위", GUILayout.Width(76f), GUILayout.Height(30f)))
                MoveCursorBy(0, 1);
            GUILayout.FlexibleSpace();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("왼쪽", GUILayout.Width(76f), GUILayout.Height(30f)))
                MoveCursorBy(-1, 0);
            if (GUILayout.Button("원점", GUILayout.Width(76f), GUILayout.Height(30f)))
                ResetCursor();
            if (GUILayout.Button("오른쪽", GUILayout.Width(76f), GUILayout.Height(30f)))
                MoveCursorBy(1, 0);
            GUILayout.FlexibleSpace();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("아래", GUILayout.Width(76f), GUILayout.Height(30f)))
                MoveCursorBy(0, -1);
            GUILayout.FlexibleSpace();
        }
    }

    private void DrawCompactCursorControls()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("왼쪽", GUILayout.Height(26f)))
                    MoveCursorBy(-1, 0);
                if (GUILayout.Button("위", GUILayout.Height(26f)))
                    MoveCursorBy(0, 1);
                if (GUILayout.Button("아래", GUILayout.Height(26f)))
                    MoveCursorBy(0, -1);
                if (GUILayout.Button("오른쪽", GUILayout.Height(26f)))
                    MoveCursorBy(1, 0);
                if (GUILayout.Button("원점", GUILayout.Width(54f), GUILayout.Height(26f)))
                    ResetCursor();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("선택 위치로 커서 이동"))
                    MoveCursorToSelection();

                if (GUILayout.Button("선택 항목 그리드 스냅"))
                    SnapSelectionToGrid();
            }
        }
    }

    private void DrawBasicControls()
    {
        EditorGUILayout.LabelField("1. 작업 기준", EditorStyles.boldLabel);
        origin = EditorGUILayout.Vector3Field("원점", origin);
        cellSize = Mathf.Max(0.01f, EditorGUILayout.FloatField("셀 크기", cellSize));
        placementHeight = EditorGUILayout.FloatField("배치 높이", placementHeight);

        using (new EditorGUILayout.HorizontalScope())
        {
            gridX = EditorGUILayout.IntField("그리드 X", gridX);
            gridZ = EditorGUILayout.IntField("그리드 Z", gridZ);
        }

        direction = (NoryangjinMapToolDirection)EditorGUILayout.EnumPopup("진행 방향", direction);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("선택 위치로 커서 이동"))
                MoveCursorToSelection();

            if (GUILayout.Button("커서 원점으로"))
                ResetCursor();
        }

        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField("2. 씬 그리드 표시", EditorStyles.boldLabel);
        showSceneGrid = EditorGUILayout.Toggle("그리드 표시", showSceneGrid);
        showWorkSubGrid = GUILayout.Toggle(showWorkSubGrid, "서브 그리드 표시", "Button", GUILayout.Height(26f));
        showCursor = EditorGUILayout.Toggle("커서 표시", showCursor);
        showGridLabels = EditorGUILayout.Toggle("좌표 라벨 표시", showGridLabels);
        gridHalfExtent = Mathf.Clamp(EditorGUILayout.IntField("표시 범위", gridHalfExtent), 2, 80);

        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField("3. 선택 오브젝트", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("선택 항목 그리드 스냅"))
                SnapSelectionToGrid();

            if (GUILayout.Button("씬 뷰 다시 그리기"))
                SceneView.RepaintAll();
        }
    }

    private void DrawRoadControls()
    {
        EditorGUILayout.LabelField("도로 배치", EditorStyles.boldLabel);
        advanceAfterRoad = EditorGUILayout.Toggle("배치 후 커서 이동", advanceAfterRoad);

        string[] missingRoadPrefabPaths = FindMissingRoadPrefabPaths();
        if (missingRoadPrefabPaths.Length > 0)
            EditorGUILayout.HelpBox("도로 프리팹을 찾을 수 없습니다:\n" + string.Join("\n", missingRoadPrefabPaths), MessageType.Warning);

        EditorGUILayout.Space(4f);
        foreach (RoadPiece roadPiece in RoadPieces)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(roadPiece.KoreanLabel, GUILayout.Height(34f)))
                    PlaceRoad(roadPiece);

                if (GUILayout.Button("찾기", GUILayout.Width(64f), GUILayout.Height(34f)))
                    PingAsset(roadPiece.PrefabPath);
            }
        }

        EditorGUILayout.HelpBox("도로를 누르면 현재 커서 위치에 배치합니다.", MessageType.None);
    }

    private void DrawPropControls()
    {
        EditorGUILayout.LabelField("소품 배치", EditorStyles.boldLabel);
        propScale = EditorGUILayout.Vector3Field("소품 스케일", propScale);
        propPrefab = (GameObject)EditorGUILayout.ObjectField("소품 프리팹", propPrefab, typeof(GameObject), false);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("선택 프리팹 사용"))
                UseSelectedPrefabAsProp();

            if (GUILayout.Button("프리팹 폴더 열기"))
                EditorUtility.RevealInFinder(Path.GetFullPath(PrefabRoot));
        }

        using (new EditorGUI.DisabledScope(propPrefab == null))
        {
            if (GUILayout.Button("현재 커서에 소품 배치", GUILayout.Height(34f)))
                PlaceProp();
        }

        if (propPrefab == null)
            EditorGUILayout.HelpBox("소품 프리팹을 지정하거나 Project 창에서 프리팹을 선택한 뒤 선택 프리팹 사용을 누르세요.", MessageType.Info);
    }

    private void DrawValidationControls()
    {
        EditorGUILayout.LabelField("맵툴 검증", EditorStyles.boldLabel);

        bool isToolScene = IsMapToolScenePath(SceneManager.GetActiveScene().path);
        DrawCheckRow("맵툴 씬", isToolScene, isToolScene ? "현재 맵툴 씬입니다." : "일반 게임/작업 씬입니다.");
        DrawCheckRow("맵툴 루트", GameObject.Find(RootName) != null, GameObject.Find(RootName) != null ? "루트가 있습니다." : "루트가 없습니다. 버튼으로 생성할 수 있습니다.");
        DrawCheckRow("셀 크기", cellSize > 0f, cellSize > 0f ? $"현재 셀 크기 {cellSize:0.###}" : "셀 크기는 0보다 커야 합니다.");

        string[] missingRoadPrefabPaths = FindMissingRoadPrefabPaths();
        DrawCheckRow("도로 프리팹", missingRoadPrefabPaths.Length == 0, missingRoadPrefabPaths.Length == 0 ? "도로 프리팹 3개를 찾았습니다." : $"{missingRoadPrefabPaths.Length}개 누락");

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("맵툴 루트 생성/선택", GUILayout.Height(30f)))
                Selection.activeGameObject = EnsureRoot();

            if (GUILayout.Button("도로 프리팹 폴더 열기", GUILayout.Height(30f)))
                EditorUtility.RevealInFinder(Path.GetFullPath(PrefabRoot));
        }

        showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "고급 정보", true);
        if (showAdvancedSettings)
        {
            EditorGUILayout.SelectableLabel($"맵툴 씬: {MapToolScenePath}", EditorStyles.textField, GUILayout.Height(18f));
            EditorGUILayout.SelectableLabel($"프리팹 루트: {PrefabRoot}", EditorStyles.textField, GUILayout.Height(18f));

            foreach (string missingPath in missingRoadPrefabPaths)
                EditorGUILayout.SelectableLabel(missingPath, EditorStyles.textField, GUILayout.Height(18f));
        }
    }

    private static void DrawCheckRow(string label, bool ok, string detail)
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            GUILayout.Label(ok ? "OK" : "확인", GUILayout.Width(42f));
            GUILayout.Label(label, EditorStyles.boldLabel, GUILayout.Width(96f));
            GUILayout.Label(detail, EditorStyles.wordWrappedLabel);
        }
    }

    private void DrawSceneGrid(SceneView sceneView)
    {
        if (!ShouldApplyMapTool(mapToolEnabled))
        {
            DestroyPlacementPreview();
            return;
        }

        if (HandleUndoCommand(Event.current))
            return;

        float normalizedCellSize = NoryangjinMapToolGridUtility.NormalizeCellSize(cellSize);
        HandleScenePalettePlacement(sceneView, normalizedCellSize);

        Color oldColor = Handles.color;
        CompareFunction oldZTest = Handles.zTest;
        DrawStableTopViewWorkGridOverlay(normalizedCellSize, sceneView, showWorkSubGrid);
        DrawLastPlacedObjectFootprint(normalizedCellSize);
        DrawSelectedPaletteFootprintPreview(normalizedCellSize);
        DrawPlacedObjectHeightLabels();

        if (!showSceneGrid)
        {
            Handles.color = oldColor;
            Handles.zTest = oldZTest;
            return;
        }

        int extent = Mathf.Clamp(gridHalfExtent, 2, 80);
        int minX = gridX - extent;
        int maxX = gridX + extent;
        int minZ = gridZ - extent;
        int maxZ = gridZ + extent;

        Handles.color = new Color(0.2f, 0.8f, 1f, 0.32f);

        for (int x = minX; x <= maxX; x++)
        {
            Vector3 start = NoryangjinMapToolGridUtility.GridToWorld(origin, x, minZ, normalizedCellSize, placementHeight);
            Vector3 end = NoryangjinMapToolGridUtility.GridToWorld(origin, x, maxZ, normalizedCellSize, placementHeight);
            Handles.DrawLine(start, end);
        }

        for (int z = minZ; z <= maxZ; z++)
        {
            Vector3 start = NoryangjinMapToolGridUtility.GridToWorld(origin, minX, z, normalizedCellSize, placementHeight);
            Vector3 end = NoryangjinMapToolGridUtility.GridToWorld(origin, maxX, z, normalizedCellSize, placementHeight);
            Handles.DrawLine(start, end);
        }

        DrawOriginMarker(normalizedCellSize);

        if (showCursor)
            DrawCursorTile(normalizedCellSize);

        Handles.color = oldColor;
        Handles.zTest = oldZTest;
    }

    private void DrawStableTopViewWorkGridOverlay(float normalizedCellSize, SceneView sceneView, bool drawSubGrid)
    {
        bool sceneViewOrthographic = sceneView != null && sceneView.orthographic;
        Quaternion sceneViewRotation = sceneView != null ? sceneView.rotation : Quaternion.identity;
        if (!ShouldDrawStableTopViewWorkGridOverlay(DrawTopViewWorkGridOverlay, isTopSceneView, sceneViewOrthographic, sceneViewRotation))
            return;

        float min = BuildWorkGridBoundaryOffset(-WorkGridExtent, normalizedCellSize);
        float max = BuildWorkGridBoundaryOffset(WorkGridExtent + 1, normalizedCellSize);
        Handles.zTest = CompareFunction.Always;
        Handles.color = new Color(0.08f, 0.75f, 0.9f, 0.92f);

        for (int i = -WorkGridExtent; i <= WorkGridExtent + 1; i++)
        {
            float offset = BuildWorkGridBoundaryOffset(i, normalizedCellSize);
            float y = BuildSceneGridOverlayHeight(placementHeight);
            Handles.DrawAAPolyLine(
                WorkGridOverlayLineWidthPixels,
                new Vector3(offset, y, min),
                new Vector3(offset, y, max));
            Handles.DrawAAPolyLine(
                WorkGridOverlayLineWidthPixels,
                new Vector3(min, y, offset),
                new Vector3(max, y, offset));
        }

        if (!drawSubGrid)
            return;

        Handles.color = new Color(0.18f, 0.58f, 0.68f, 0.68f);
        for (int cell = -WorkGridExtent; cell <= WorkGridExtent; cell++)
        {
            for (int subdivision = 1; subdivision < WorkGridSubdivisionsPerCell; subdivision++)
            {
                float offset = BuildWorkGridSubdivisionOffset(cell, subdivision, normalizedCellSize);
                float y = BuildSceneGridOverlayHeight(placementHeight);
                Handles.DrawAAPolyLine(
                    WorkSubGridOverlayLineWidthPixels,
                    new Vector3(offset, y, min),
                    new Vector3(offset, y, max));
                Handles.DrawAAPolyLine(
                    WorkSubGridOverlayLineWidthPixels,
                    new Vector3(min, y, offset),
                    new Vector3(max, y, offset));
            }
        }
    }

    private void DrawCurrentSceneGridCellFill(float normalizedCellSize)
    {
        float placementGridCellSize = BuildPlacementSnapCellSize(normalizedCellSize, false);
        Vector2Int cursor = new Vector2Int(gridX, gridZ);
        HashSet<Vector2Int> occupiedCells = CollectOccupiedGridCells(placementGridCellSize);
        NoryangjinMapToolSceneGridCellState state = GetSceneGridCellState(cursor, occupiedCells);
        DrawSceneGridCellFill(gridX, gridZ, placementGridCellSize, state);
    }

    private void DrawSelectedPaletteFootprintPreview(float normalizedCellSize)
    {
        PaletteItem? selectedItem = FindSelectedPaletteItem();
        if (!selectedItem.HasValue || !ShouldDrawPlacementValidityFill(selectedItem.Value.PrefabPath))
            return;

        float placementGridCellSize = BuildPlacementSnapCellSize(normalizedCellSize, false);
        List<Vector2Int> previewCells = GetPaletteItemFootprintCells(selectedItem.Value, placementGridCellSize);
        HashSet<NoryangjinMapToolOccupiedCell> occupiedCells = CollectLayeredOccupiedGridCells(placementGridCellSize);
        NoryangjinMapToolSceneGridCellState previewState = ResolveFootprintPreviewState(
            previewCells,
            GetPaletteItemLayer(selectedItem.Value),
            occupiedCells);

        foreach (Vector2Int cell in previewCells)
            DrawSceneGridCellFill(cell.x, cell.y, placementGridCellSize, previewState);

        DrawSlopeHeightLabels(selectedItem.Value.PrefabPath, previewCells, placementGridCellSize);
    }

    private void DrawSlopeHeightLabels(string prefabPath, IReadOnlyList<Vector2Int> previewCells, float placementGridCellSize)
    {
        if (!TryBuildSlopeHeightLabelCells(prefabPath, previewCells, direction, out Vector2Int highCell, out Vector2Int lowCell))
            return;

        DrawSlopeHeightLabel(highCell, placementGridCellSize, SlopeHighLabel);
        DrawSlopeHeightLabel(lowCell, placementGridCellSize, SlopeLowLabel);
    }

    private void DrawSlopeHeightLabel(Vector2Int cell, float placementGridCellSize, string label)
    {
        Vector3 worldPosition = NoryangjinMapToolGridUtility.GridToWorld(
            origin,
            cell.x,
            cell.y,
            placementGridCellSize,
            BuildSceneGridOverlayHeight(placementHeight)) + Vector3.up * 0.16f;
        GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 20,
            normal = { textColor = Color.white }
        };

        Handles.Label(worldPosition, label, labelStyle);
    }

    private void DrawPlacedObjectHeightLabels()
    {
        GameObject root = GameObject.Find(RootName);
        if (root == null)
            return;

        GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
            normal = { textColor = Color.cyan }
        };

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child == root.transform ||
                !child.gameObject.activeInHierarchy ||
                !ShouldDrawPlacedObjectHeightLabel(child.gameObject.name, mapToolEnabled))
            {
                continue;
            }

            Vector3 labelPosition = BuildPlacedObjectHeightLabelPosition(child.gameObject);
            Handles.Label(labelPosition, FormatPlacedObjectHeightLabel(child.position.y), labelStyle);
        }
    }

    private static Vector3 BuildPlacedObjectHeightLabelPosition(GameObject target)
    {
        Bounds bounds = CalculateRendererBounds(target);
        Vector3 position = bounds.size == Vector3.zero ? target.transform.position : bounds.center;
        float verticalOffset = Mathf.Max(0.35f, bounds.extents.y + 0.2f);
        return position + Vector3.up * verticalOffset;
    }

    private void DrawLastPlacedObjectFootprint(float normalizedCellSize)
    {
        GameObject lastPlacedObject = ResolveLastPlacedObject();
        if (lastPlacedObject == null ||
            !TryGetMapToolPlacedObjectGridPosition(lastPlacedObject.name, out Vector2Int anchor))
            return;

        float placementGridCellSize = BuildPlacementSnapCellSize(normalizedCellSize, false);
        foreach (Vector2Int cell in GetPlacedObjectDisplayedFootprintCells(lastPlacedObject, anchor, placementGridCellSize))
            DrawSceneGridCellFill(cell.x, cell.y, placementGridCellSize, NoryangjinMapToolSceneGridCellState.LastPlaced);
    }

    private GameObject ResolveLastPlacedObject()
    {
        if (lastPlacedObjectInstanceId == 0)
            return null;

        GameObject lastPlacedObject = EditorUtility.InstanceIDToObject(lastPlacedObjectInstanceId) as GameObject;
        if (lastPlacedObject != null)
            return lastPlacedObject;

        lastPlacedObjectInstanceId = 0;
        return null;
    }

    private static void DrawSpecialPaletteIcon(PaletteItem item, Rect imageRect)
    {
        string iconText = GetSpecialPaletteIconText(item.PrefabPath);
        if (string.IsNullOrEmpty(iconText))
            return;

        GUIStyle iconStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 28,
            normal = { textColor = Color.white }
        };
        GUI.Label(imageRect, iconText, iconStyle);
    }

    private bool TryGetPaletteFootprintBadge(PaletteItem item, Rect imageRect, out string footprintLabel, out Rect badgeRect)
    {
        footprintLabel = string.Empty;
        badgeRect = Rect.zero;
        if (item.Prefab == null || IsEmptyPaletteItemPath(item.PrefabPath))
            return false;

        footprintLabel = BuildFootprintLabel(GetPaletteItemFootprint(
            item,
            NoryangjinMapToolGridUtility.NormalizeCellSize(cellSize)));
        GUIStyle badgeStyle = new GUIStyle(EditorStyles.miniBoldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        Vector2 badgeSize = badgeStyle.CalcSize(new GUIContent(footprintLabel));
        badgeSize.x = Mathf.Max(28f, badgeSize.x + 8f);
        badgeSize.y = 18f;
        badgeRect = new Rect(
            imageRect.xMax - badgeSize.x - 2f,
            imageRect.y + 2f,
            badgeSize.x,
            badgeSize.y);

        return true;
    }

    private static void DrawPaletteFootprintBadge(string footprintLabel, Rect badgeRect, bool hasFootprintBadge)
    {
        if (!hasFootprintBadge)
            return;

        GUIStyle badgeStyle = new GUIStyle(EditorStyles.miniBoldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        EditorGUI.DrawRect(badgeRect, new Color(0f, 0f, 0f, 0.68f));
        GUI.Label(badgeRect, footprintLabel, badgeStyle);
    }

    private void HandleScenePalettePlacement(SceneView sceneView, float normalizedCellSize)
    {
        PaletteItem? selectedItem = FindSelectedPaletteItem();
        if (!selectedItem.HasValue)
        {
            DestroyPlacementPreview();
            return;
        }

        Event currentEvent = Event.current;
        if (currentEvent == null)
            return;

        if (currentEvent.type != EventType.MouseMove &&
            currentEvent.type != EventType.MouseDrag &&
            currentEvent.type != EventType.MouseDown)
            return;

        bool coarseSnap = ShouldUseCoarsePlacementSnap(currentEvent);
        float placementGridCellSize = BuildPlacementSnapCellSize(normalizedCellSize, false);
        if (!TryGetSceneMouseGridCell(currentEvent.mousePosition, normalizedCellSize, out Vector2Int fineHoverCell))
        {
            SetPlacementPreviewVisible(false);
            return;
        }

        Vector2Int hoverCell = fineHoverCell;
        if (coarseSnap)
        {
            if (!coarsePlacementSnapActive)
            {
                coarsePlacementSnapActive = true;
                coarsePlacementSnapAnchor = new Vector2Int(gridX, gridZ);
            }

            hoverCell = SnapPlacementGridCellToCoarseStep(fineHoverCell, coarsePlacementSnapAnchor);
        }
        else
        {
            coarsePlacementSnapActive = false;
        }

        if (ShouldTrackSceneMouseForPlacementPreview(currentEvent.type))
        {
            gridX = hoverCell.x;
            gridZ = hoverCell.y;
            UpdatePlacementPreview(selectedItem.Value);
        }

        if (currentEvent.type == EventType.MouseMove || currentEvent.type == EventType.MouseDrag)
        {
            sceneView.Repaint();
            Repaint();
        }

        if (currentEvent.type != EventType.MouseDown || currentEvent.button != 0)
            return;

        gridX = hoverCell.x;
        gridZ = hoverCell.y;
        UpdatePlacementPreview(selectedItem.Value);

        if (IsSelectPaletteItemPath(selectedItem.Value.PrefabPath))
        {
            Selection.activeGameObject = FindPlacedObjectOverlappingCursor(placementGridCellSize);
            Repaint();
        }
        else if (IsClearSelectionPaletteItemPath(selectedItem.Value.PrefabPath))
        {
            ClearMapToolSelection();
        }
        else if (CanPlaceSelectedPaletteItemAtCursor(placementGridCellSize))
        {
            PlaceSelectedPaletteItem();
            UpdatePlacementPreview(selectedItem.Value);
        }

        currentEvent.Use();
    }

    internal static bool ShouldTrackSceneMouseForPlacementPreview(EventType eventType)
    {
        return eventType == EventType.MouseMove ||
               eventType == EventType.MouseDrag;
    }

    internal static bool ShouldUseCoarsePlacementSnap(Event currentEvent)
    {
        return currentEvent != null &&
               (currentEvent.shift ||
                (currentEvent.modifiers & EventModifiers.Shift) == EventModifiers.Shift);
    }

    internal static bool IsUndoCommand(Event currentEvent)
    {
        if (currentEvent == null)
            return false;

        if (currentEvent.type == EventType.ExecuteCommand &&
            string.Equals(currentEvent.commandName, "UndoRedoPerformed", StringComparison.Ordinal))
        {
            return true;
        }

        if (currentEvent.type != EventType.KeyDown || currentEvent.keyCode != KeyCode.Z)
            return false;

        bool hasUndoModifier = currentEvent.control ||
                               currentEvent.command ||
                               (currentEvent.modifiers & EventModifiers.Control) == EventModifiers.Control ||
                               (currentEvent.modifiers & EventModifiers.Command) == EventModifiers.Command;
        bool hasRedoModifier = currentEvent.shift ||
                               currentEvent.alt ||
                               (currentEvent.modifiers & EventModifiers.Shift) == EventModifiers.Shift ||
                               (currentEvent.modifiers & EventModifiers.Alt) == EventModifiers.Alt;
        return hasUndoModifier && !hasRedoModifier;
    }

    private void UpdatePlacementPreview(PaletteItem selectedItem)
    {
        if (selectedItem.Prefab == null || !ShouldShowPlacementPreview(selectedItem.PrefabPath))
        {
            DestroyPlacementPreview();
            return;
        }

        GameObject preview = EnsurePlacementPreview(selectedItem);
        if (preview == null)
            return;

        NoryangjinMapToolPalettePlacementEntry placement = GetPaletteDefaults().GetOrCreateEntry(selectedItem.PrefabPath);
        preview.transform.position = BuildPalettePlacementPosition(
            origin,
            gridX,
            gridZ,
            BuildPlacementSnapCellSize(cellSize, false),
            placementHeight,
            placement.heightOffset,
            placement.positionOffset);
        preview.transform.rotation = BuildPalettePlacementRotation(selectedItem.Prefab.transform.rotation, placement.yawOffset);
        preview.transform.localScale = BuildPalettePlacementScale(selectedItem.Prefab.transform.localScale, placement.scale);
        preview.SetActive(true);
    }

    private GameObject EnsurePlacementPreview(PaletteItem selectedItem)
    {
        if (placementPreviewInstance != null &&
            string.Equals(placementPreviewPrefabPath, selectedItem.PrefabPath, StringComparison.Ordinal))
            return placementPreviewInstance;

        DestroyPlacementPreview();

        placementPreviewInstance = PrefabUtility.InstantiatePrefab(selectedItem.Prefab) as GameObject;
        if (placementPreviewInstance == null)
            placementPreviewInstance = Instantiate(selectedItem.Prefab);

        placementPreviewInstance.name = PlacementPreviewName;
        placementPreviewPrefabPath = selectedItem.PrefabPath;
        PreparePlacementPreviewObject(placementPreviewInstance);
        ApplyPlacementPreviewTransparency(placementPreviewInstance);
        return placementPreviewInstance;
    }

    private static void PreparePlacementPreviewObject(GameObject preview)
    {
        if (preview == null)
            return;

        foreach (Transform child in preview.GetComponentsInChildren<Transform>(true))
            child.gameObject.hideFlags = HideFlags.HideAndDontSave;

        foreach (Collider collider in preview.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;

        foreach (MonoBehaviour behaviour in preview.GetComponentsInChildren<MonoBehaviour>(true))
            behaviour.enabled = false;
    }

    private void ApplyPlacementPreviewTransparency(GameObject preview)
    {
        DestroyPlacementPreviewMaterials();
        foreach (Renderer renderer in preview.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
                continue;

            for (int i = 0; i < materials.Length; i++)
                materials[i] = CreatePlacementPreviewMaterial(materials[i]);
            renderer.sharedMaterials = materials;
        }
    }

    private Material CreatePlacementPreviewMaterial(Material source)
    {
        Material material = source != null
            ? new Material(source)
            : new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));

        material.hideFlags = HideFlags.HideAndDontSave;
        material.name = source != null
            ? $"{source.name}_MapToolPreview50"
            : "MapTool_Placement_Preview_Transparent";
        ApplyPlacementPreviewMaterialTransparency(material);

        placementPreviewMaterials ??= new List<Material>();
        placementPreviewMaterials.Add(material);
        return material;
    }

    private static void ApplyPlacementPreviewMaterialTransparency(Material material)
    {
        if (material == null)
            return;

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", BuildPlacementPreviewTransparentColor(material.GetColor("_BaseColor")));
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", BuildPlacementPreviewTransparentColor(material.GetColor("_Color")));

        material.SetOverrideTag("RenderType", "Transparent");
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Mode"))
            material.SetFloat("_Mode", 3f);
        if (material.HasProperty("_AlphaClip"))
            material.SetFloat("_AlphaClip", 0f);
        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);

        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private void SetPlacementPreviewVisible(bool visible)
    {
        if (placementPreviewInstance != null)
            placementPreviewInstance.SetActive(visible);
    }

    private void DestroyPlacementPreview()
    {
        if (placementPreviewInstance != null)
            DestroyImmediate(placementPreviewInstance);

        placementPreviewInstance = null;
        placementPreviewPrefabPath = null;
    }

    private void DestroyPlacementPreviewMaterials()
    {
        if (placementPreviewMaterials != null)
        {
            foreach (Material material in placementPreviewMaterials)
            {
                if (material != null)
                    DestroyImmediate(material);
            }

            placementPreviewMaterials.Clear();
        }
    }

    private bool TryGetSceneMouseGridCell(Vector2 mousePosition, float normalizedCellSize, out Vector2Int cell)
    {
        cell = default;
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
        var placementPlane = new Plane(Vector3.up, new Vector3(0f, placementHeight, 0f));
        if (!placementPlane.Raycast(ray, out float distance))
            return false;

        Vector3 world = ray.GetPoint(distance);
        cell = BuildPlacementGridCell(world, origin, normalizedCellSize, false);
        return true;
    }

    private HashSet<Vector2Int> CollectOccupiedGridCells(float normalizedCellSize)
    {
        var occupiedCells = new HashSet<Vector2Int>();
        foreach (NoryangjinMapToolOccupiedCell occupiedCell in CollectLayeredOccupiedGridCells(normalizedCellSize))
            occupiedCells.Add(occupiedCell.Cell);

        return occupiedCells;
    }

    private HashSet<NoryangjinMapToolOccupiedCell> CollectLayeredOccupiedGridCells(float normalizedCellSize)
    {
        var occupiedCells = new HashSet<NoryangjinMapToolOccupiedCell>();
        GameObject root = GameObject.Find(RootName);
        if (root == null)
            return occupiedCells;

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child == root.transform)
                continue;

            if (TryGetMapToolPlacedObjectGridPosition(child.gameObject.name, out Vector2Int cell))
            {
                NoryangjinMapToolPlacementLayer layer = GetPlacedObjectLayer(child.gameObject);
                foreach (Vector2Int occupiedCell in GetPlacedObjectDisplayedFootprintCells(child.gameObject, cell, normalizedCellSize))
                    occupiedCells.Add(new NoryangjinMapToolOccupiedCell(occupiedCell, layer));
            }
        }

        return occupiedCells;
    }

    private void DrawSceneGridCellFill(int x, int z, float normalizedCellSize, NoryangjinMapToolSceneGridCellState state)
    {
        CompareFunction oldZTest = Handles.zTest;
        Vector3 center = NoryangjinMapToolGridUtility.GridToWorld(
            origin,
            x,
            z,
            normalizedCellSize,
            BuildSceneGridOverlayHeight(placementHeight));
        float half = BuildSceneGridCellFillHalfExtent(normalizedCellSize);
        var corners = new[]
        {
            center + new Vector3(-half, 0f, -half),
            center + new Vector3(-half, 0f, half),
            center + new Vector3(half, 0f, half),
            center + new Vector3(half, 0f, -half)
        };
        Color fill = GetPlacementValidityFillColor(state);

        if (DrawPlacementValidityFillAsGuiOverlay)
        {
            DrawSceneGridCellGuiFill(corners, fill);
        }
        else
        {
            Handles.zTest = CompareFunction.Always;
            Handles.DrawSolidRectangleWithOutline(corners, fill, Color.clear);
            Handles.zTest = oldZTest;
        }
    }

    private static void DrawSceneGridCellGuiFill(Vector3[] worldCorners, Color fill)
    {
        if (worldCorners == null || worldCorners.Length < 4 || Event.current == null || Event.current.type != EventType.Repaint)
            return;

        Vector3[] guiCorners = new Vector3[worldCorners.Length];
        for (int i = 0; i < worldCorners.Length; i++)
        {
            Vector2 point = HandleUtility.WorldToGUIPoint(worldCorners[i]);
            guiCorners[i] = new Vector3(point.x, point.y, 0f);
        }

        Color oldColor = Handles.color;
        Handles.BeginGUI();
        Handles.color = fill;
        Handles.DrawAAConvexPolygon(guiCorners);
        Handles.color = oldColor;
        Handles.EndGUI();
    }

    private void DrawOriginMarker(float normalizedCellSize)
    {
        Vector3 originPosition = NoryangjinMapToolGridUtility.GridToWorld(origin, 0, 0, normalizedCellSize, placementHeight);
        Handles.color = new Color(1f, 0.8f, 0.1f, 0.9f);
        Handles.DrawWireDisc(originPosition, Vector3.up, normalizedCellSize * 0.24f);
        Handles.Label(originPosition + Vector3.up * 0.25f, "원점");
    }

    private void DrawCursorTile(float normalizedCellSize)
    {
        float placementGridCellSize = BuildPlacementSnapCellSize(normalizedCellSize, false);
        Vector3 center = NoryangjinMapToolGridUtility.GridToWorld(
            origin,
            gridX,
            gridZ,
            placementGridCellSize,
            BuildSceneGridOverlayHeight(placementHeight));
        float half = BuildSceneGridCellFillHalfExtent(placementGridCellSize);
        var corners = new[]
        {
            center + new Vector3(-half, 0f, -half),
            center + new Vector3(-half, 0f, half),
            center + new Vector3(half, 0f, half),
            center + new Vector3(half, 0f, -half)
        };

        Handles.DrawSolidRectangleWithOutline(corners, Color.clear, new Color(0f, 1f, 0.2f, 0.6f));
        Handles.Label(center + Vector3.up * 0.35f, $"커서 X {gridX} / Z {gridZ}\n{DirectionToKorean(direction)}");
    }

    private void DrawNextStepPreview(float normalizedCellSize)
    {
        Vector2Int step = NoryangjinMapToolGridUtility.DirectionToStep(direction);
        float placementGridCellSize = BuildPlacementSnapCellSize(normalizedCellSize, false);
        Vector3 start = NoryangjinMapToolGridUtility.GridToWorld(origin, gridX, gridZ, placementGridCellSize, placementHeight) + Vector3.up * 0.08f;
        Vector3 end = NoryangjinMapToolGridUtility.GridToWorld(origin, gridX + step.x, gridZ + step.y, placementGridCellSize, placementHeight) + Vector3.up * 0.08f;

        Handles.color = new Color(0.1f, 1f, 0.35f, 0.9f);
        Handles.DrawAAPolyLine(4f, start, end);
        Handles.ConeHandleCap(0, end, Quaternion.LookRotation((end - start).normalized, Vector3.up), placementGridCellSize * 0.18f, EventType.Repaint);
    }

    private void DrawGridLabels(float normalizedCellSize, int minX, int maxX, int minZ, int maxZ)
    {
        int stride = Mathf.Max(1, Mathf.CeilToInt((maxX - minX) / 8f));
        Handles.color = new Color(0.85f, 0.95f, 1f, 0.85f);

        for (int x = minX; x <= maxX; x += stride)
        {
            for (int z = minZ; z <= maxZ; z += stride)
            {
                Vector3 point = NoryangjinMapToolGridUtility.GridToWorld(origin, x, z, normalizedCellSize, placementHeight);
                Handles.Label(point + Vector3.up * 0.12f, $"{x},{z}");
            }
        }
    }

    private void PlaceRoad(RoadPiece roadPiece)
    {
        NoryangjinMapToolPalettePlacementEntry placement = GetRoadPlacementDefault(GetPaletteDefaults(), roadPiece.PrefabPath);
        PlaceRoad(roadPiece, placement);
    }

    internal static NoryangjinMapToolPalettePlacementEntry GetRoadPlacementDefault(
        NoryangjinMapToolPaletteDefaults defaults,
        string prefabPath)
    {
        return defaults != null
            ? defaults.GetOrCreateEntry(prefabPath)
            : NoryangjinMapToolPalettePlacementEntry.CreateDefault(prefabPath);
    }

    private void PlaceRoad(RoadPiece roadPiece, NoryangjinMapToolPalettePlacementEntry placement, bool allowCursorAutoAdvance = true)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(roadPiece.PrefabPath);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog(KoreanWindowTitle, $"도로 프리팹을 찾을 수 없습니다:\n{roadPiece.PrefabPath}", "확인");
            return;
        }

        int undoGroup = BeginMapToolUndoGroup($"Place {roadPiece.KoreanLabel}");
        try
        {
            GameObject instance = PlacePrefab(
                prefab,
                EnsureChild(EnsureRoot().transform, RoadParentName),
                NoryangjinMapToolGridUtility.BuildInstanceName("Road", roadPiece.Label, gridX, gridZ),
                placement);
            PlaceRoadCompanions(instance.transform, roadPiece);

            Selection.activeGameObject = instance;
            RegisterLastPlacedObject(instance);

            if (ShouldAdvanceRoadCursorAfterPlacement(advanceAfterRoad, allowCursorAutoAdvance))
            {
                direction = NoryangjinMapToolGridUtility.DirectionAfterRoadTurn(direction, roadPiece.Turn);
                Vector2Int step = NoryangjinMapToolGridUtility.DirectionToStep(direction);
                gridX += step.x * WorkGridSubdivisionsPerCell;
                gridZ += step.y * WorkGridSubdivisionsPerCell;
            }
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }

        SceneView.RepaintAll();
    }

    private void PlaceRoadCompanions(Transform roadRoot, RoadPiece roadPiece)
    {
        foreach (RoadCompanion roadCompanion in roadPiece.Companions)
        {
            string companionPrefabPath = roadCompanion.PrefabPath;
            GameObject companionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(companionPrefabPath);
            if (companionPrefab == null)
            {
                Debug.LogWarning($"[{KoreanWindowTitle}] Companion road prefab not found: {companionPrefabPath}");
                continue;
            }

            GameObject companion = PrefabUtility.InstantiatePrefab(companionPrefab, roadRoot) as GameObject;
            if (companion == null)
                companion = Instantiate(companionPrefab, roadRoot);

            companion.name = companionPrefab.name;
            companion.transform.localPosition = roadCompanion.LocalPosition;
            companion.transform.localRotation = Quaternion.Euler(roadCompanion.LocalEulerAngles);
            companion.transform.localScale = roadCompanion.LocalScale;
            Undo.RegisterCreatedObjectUndo(companion, $"Place {roadPiece.KoreanLabel} companion");
            EditorUtility.SetDirty(companion);
        }
    }

    private void PlaceProp()
    {
        NoryangjinMapToolPalettePlacementEntry placement = NoryangjinMapToolPalettePlacementEntry.CreateDefault(propPrefab.name);
        placement.scale = propScale;

        int undoGroup = BeginMapToolUndoGroup($"Place {propPrefab.name}");
        try
        {
            GameObject instance = PlacePrefab(
                propPrefab,
                EnsureChild(EnsureRoot().transform, PropParentName),
                NoryangjinMapToolGridUtility.BuildInstanceName("Prop", propPrefab.name, gridX, gridZ),
                placement);

            Selection.activeGameObject = instance;
            RegisterLastPlacedObject(instance);
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }
    }

    private void PlaceSelectedPaletteItem()
    {
        if (selectedPalettePrefabPath == null)
            return;

        if (IsEmptyPaletteItemPath(selectedPalettePrefabPath))
        {
            DeletePlacedObjectsOverlappingCursor();
            return;
        }

        if (IsSelectPaletteItemPath(selectedPalettePrefabPath))
        {
            Selection.activeGameObject = FindPlacedObjectOverlappingCursor(BuildPlacementSnapCellSize(cellSize, false));
            Repaint();
            return;
        }

        if (IsClearSelectionPaletteItemPath(selectedPalettePrefabPath))
        {
            ClearMapToolSelection();
            return;
        }

        foreach (PaletteItem item in GetPaletteItems())
        {
            if (!string.Equals(item.PrefabPath, selectedPalettePrefabPath, StringComparison.Ordinal))
                continue;

            if (!CanPlacePaletteItemAtCursor(item))
                return;

            PlacePaletteItem(item);
            return;
        }
    }

    private void PlacePaletteItem(PaletteItem item)
    {
        if (item.Prefab == null)
            return;

        NoryangjinMapToolPalettePlacementEntry placement = GetPaletteDefaults().GetOrCreateEntry(item.PrefabPath);

        if (TryPlaceKnownRoadPiece(item.PrefabPath, placement, allowCursorAutoAdvance: false))
            return;

        if (IsLowCostWaterBackgroundPath(item.PrefabPath))
        {
            PlaceWaterBackground(item, placement);
            return;
        }

        string parentName = item.Category == NoryangjinMapToolPaletteCategory.Road ? RoadParentName : PropParentName;
        string instanceKind = item.Category == NoryangjinMapToolPaletteCategory.Road ? "Road" : "Prop";

        int undoGroup = BeginMapToolUndoGroup($"Place {item.Label}");
        try
        {
            GameObject instance = PlacePrefab(
                item.Prefab,
                EnsureChild(EnsureRoot().transform, parentName),
                NoryangjinMapToolGridUtility.BuildInstanceName(instanceKind, item.Prefab.name, gridX, gridZ),
                placement);

            Selection.activeGameObject = instance;
            RegisterLastPlacedObject(instance);
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }

        SceneView.RepaintAll();
    }

    private void PlaceWaterBackground(PaletteItem item, NoryangjinMapToolPalettePlacementEntry placement)
    {
        Transform waterParent = EnsureChild(EnsureRoot().transform, WaterParentName);
        GameObject instance = waterParent.Find(WaterBackdropInstanceName)?.gameObject;

        int undoGroup = BeginMapToolUndoGroup($"Place {item.Label}");
        try
        {
            if (instance == null)
            {
                instance = PrefabUtility.InstantiatePrefab(item.Prefab, waterParent) as GameObject;
                if (instance == null)
                    instance = Instantiate(item.Prefab, waterParent);

                Undo.RegisterCreatedObjectUndo(instance, $"Place {item.Label}");
            }
            else
            {
                Undo.RecordObject(instance.transform, $"Move {item.Label}");
            }

            instance.name = WaterBackdropInstanceName;
            instance.transform.position = BuildPalettePlacementPosition(
                origin,
                gridX,
                gridZ,
                BuildPlacementSnapCellSize(cellSize, false),
                placementHeight,
                placement.heightOffset,
                placement.positionOffset);
            instance.transform.rotation = BuildPalettePlacementRotation(item.Prefab.transform.rotation, placement.yawOffset);
            instance.transform.localScale = BuildPalettePlacementScale(item.Prefab.transform.localScale, placement.scale);
            OptimizeMapToolWaterInstance(instance);

            Selection.activeGameObject = instance;
            RegisterLastPlacedObject(instance);
            EditorUtility.SetDirty(instance);
            if (instance.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(instance.scene);
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }

        SceneView.RepaintAll();
    }

    private void DeletePlacedObjectsOverlappingCursor()
    {
        GameObject root = GameObject.Find(RootName);
        if (root == null)
            return;

        var deleteTargets = new List<GameObject>();
        Vector2Int cursor = new Vector2Int(gridX, gridZ);
        float placementGridCellSize = BuildPlacementSnapCellSize(cellSize, false);
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child == root.transform)
                continue;

            if (!IsMapToolPlacedObjectName(child.gameObject.name))
                continue;

            if (TryGetMapToolPlacedObjectGridPosition(child.gameObject.name, out Vector2Int anchor) &&
                GetPlacedObjectDisplayedFootprintCells(child.gameObject, anchor, placementGridCellSize).Contains(cursor))
                deleteTargets.Add(child.gameObject);
        }

        if (deleteTargets.Count == 0)
            return;

        GameObject deleteTarget = SelectSingleCursorDeleteTarget(deleteTargets, cursor);
        if (deleteTarget == null)
            return;

        int undoGroup = BeginMapToolUndoGroup("Delete Map Tool Objects");
        try
        {
            if (deleteTarget.GetInstanceID() == lastPlacedObjectInstanceId)
                lastPlacedObjectInstanceId = 0;
            Undo.DestroyObjectImmediate(deleteTarget);
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }

        SceneView.RepaintAll();
    }

    private bool CanPlaceSelectedPaletteItemAtCursor(float normalizedCellSize)
    {
        PaletteItem? selectedItem = FindSelectedPaletteItem();
        if (!selectedItem.HasValue)
            return false;

        if (IsEmptyPaletteItemPath(selectedItem.Value.PrefabPath))
            return true;

        if (!IsGridManagedPaletteItemPath(selectedItem.Value.PrefabPath))
            return true;

        return CanPlaceFootprintCells(
            GetPaletteItemFootprintCells(selectedItem.Value, normalizedCellSize),
            GetPaletteItemLayer(selectedItem.Value),
            CollectLayeredOccupiedGridCells(normalizedCellSize));
    }

    private bool CanPlacePaletteItemAtCursor(PaletteItem item)
    {
        if (!IsGridManagedPaletteItemPath(item.PrefabPath))
            return true;

        float normalizedCellSize = BuildPlacementSnapCellSize(cellSize, false);
        return CanPlaceFootprintCells(
            GetPaletteItemFootprintCells(item, normalizedCellSize),
            GetPaletteItemLayer(item),
            CollectLayeredOccupiedGridCells(normalizedCellSize));
    }

    private void DeleteAllPlacedObjects()
    {
        GameObject root = GameObject.Find(RootName);
        if (root == null)
            return;

        List<GameObject> deleteTargets = CollectDeleteAllPlacedObjectTargets(root.transform);

        if (deleteTargets.Count == 0)
        {
            EditorUtility.DisplayDialog(KoreanWindowTitle, DeleteAllPlacedObjectsNoTargetsMessage, "확인");
            return;
        }

        if (!EditorUtility.DisplayDialog(KoreanWindowTitle, $"{deleteTargets.Count}개 배치 오브젝트를 모두 삭제할까요?", DeleteAllPlacedObjectsButtonLabel, "취소"))
            return;

        int undoGroup = BeginMapToolUndoGroup("Delete All Map Tool Objects");
        try
        {
            foreach (GameObject target in deleteTargets)
            {
                if (target.GetInstanceID() == lastPlacedObjectInstanceId)
                    lastPlacedObjectInstanceId = 0;

                Undo.DestroyObjectImmediate(target);
            }
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }

        SceneView.RepaintAll();
        Repaint();
    }

    private static List<GameObject> CollectDeleteAllPlacedObjectTargets(Transform root)
    {
        var deleteTargets = new List<GameObject>();
        var seen = new HashSet<GameObject>();

        AddDirectChildrenToDeleteTargets(root.Find(RoadParentName), deleteTargets, seen);
        AddDirectChildrenToDeleteTargets(root.Find(PropParentName), deleteTargets, seen);
        AddDirectChildrenToDeleteTargets(root.Find(WaterParentName), deleteTargets, seen);

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child == root || HasPlacementContainerAncestor(child, root))
                continue;

            string parentName = child.parent != null ? child.parent.name : string.Empty;
            if (ShouldDeleteAllPlacedObjectsTarget(child.gameObject.name, parentName) && seen.Add(child.gameObject))
                deleteTargets.Add(child.gameObject);
        }

        return deleteTargets;
    }

    private static void AddDirectChildrenToDeleteTargets(
        Transform parent,
        List<GameObject> deleteTargets,
        HashSet<GameObject> seen)
    {
        if (parent == null)
            return;

        foreach (Transform child in parent)
        {
            if (seen.Add(child.gameObject))
                deleteTargets.Add(child.gameObject);
        }
    }

    private static bool HasPlacementContainerAncestor(Transform child, Transform root)
    {
        Transform current = child.parent;
        while (current != null && current != root)
        {
            if (IsMapToolPlacementContainerName(current.name))
                return true;

            current = current.parent;
        }

        return false;
    }

    private GameObject FindPlacedObjectAtCursor()
    {
        GameObject root = GameObject.Find(RootName);
        if (root == null)
            return null;

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child == root.transform)
                continue;

            if (IsMapToolPlacedObjectName(child.gameObject.name, gridX, gridZ))
                return child.gameObject;
        }

        return null;
    }

    private GameObject FindPlacedObjectOverlappingCursor(float normalizedCellSize)
    {
        GameObject root = GameObject.Find(RootName);
        if (root == null)
            return null;

        Vector2Int cursor = new Vector2Int(gridX, gridZ);
        var candidates = new List<GameObject>();
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child == root.transform || !IsMapToolPlacedObjectName(child.gameObject.name))
                continue;

            if (TryGetMapToolPlacedObjectGridPosition(child.gameObject.name, out Vector2Int anchor) &&
                GetPlacedObjectDisplayedFootprintCells(child.gameObject, anchor, normalizedCellSize).Contains(cursor))
                candidates.Add(child.gameObject);
        }

        candidates.Sort((left, right) => GetSelectionPriority(GetPlacedObjectLayer(left)).CompareTo(GetSelectionPriority(GetPlacedObjectLayer(right))));
        return candidates.Count > 0 ? candidates[0] : null;
    }

    internal static GameObject SelectSingleCursorDeleteTarget(List<GameObject> candidates, Vector2Int cursor)
    {
        if (candidates == null || candidates.Count == 0)
            return null;

        var anchoredCandidates = new List<GameObject>();
        foreach (GameObject candidate in candidates)
        {
            if (IsMapToolPlacedObjectName(candidate != null ? candidate.name : null, cursor.x, cursor.y))
                anchoredCandidates.Add(candidate);
        }

        if (anchoredCandidates.Count == 0)
        {
            candidates.Sort((left, right) =>
            {
                return GetSelectionPriority(GetPlacedObjectLayer(left)).CompareTo(GetSelectionPriority(GetPlacedObjectLayer(right)));
            });

            return candidates[0];
        }

        anchoredCandidates.Sort((left, right) =>
        {
            return GetSelectionPriority(GetPlacedObjectLayer(left)).CompareTo(GetSelectionPriority(GetPlacedObjectLayer(right)));
        });

        return anchoredCandidates[0];
    }

    private List<Vector2Int> GetPlacedObjectDisplayedFootprintCells(GameObject target, Vector2Int anchor, float normalizedCellSize)
    {
        if (target == null)
            return BuildDisplayedFootprintCells(anchor, Vector2Int.one);

        string prefabPath = GetPrefabAssetPathForPlacedObject(target);
        if (string.IsNullOrEmpty(prefabPath))
            return BuildBoundsFootprintCells(CalculateRendererBounds(target), origin, normalizedCellSize);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
            return BuildBoundsFootprintCells(CalculateRendererBounds(target), origin, normalizedCellSize);

        NoryangjinMapToolPalettePlacementEntry placement = GetPaletteDefaults().GetOrCreateEntry(prefabPath);
        if (!placement.useManualFootprint)
            return BuildBoundsFootprintCells(CalculateRendererBounds(target), origin, normalizedCellSize);

        Vector3 boundsSize = CalculateRendererBounds(prefab).size;
        boundsSize.x *= Mathf.Abs(placement.scale.x);
        boundsSize.z *= Mathf.Abs(placement.scale.z);
        Vector2Int footprint = ResolvePaletteFootprint(CalculateFootprintSize(boundsSize, normalizedCellSize), placement);
        return BuildDisplayedFootprintCells(anchor, ScaleManualFootprintForPlacementGrid(footprint));
    }

    private GameObject GetRotationTarget()
    {
        GameObject selected = ResolveSelectedPlacedObject(Selection.activeGameObject);
        if (selected != null)
            return selected;

        return FindPlacedObjectAtCursor();
    }

    internal static GameObject ResolveSelectedPlacedObject(GameObject selected)
    {
        Transform current = selected != null ? selected.transform : null;
        while (current != null)
        {
            if (IsMapToolPlacedObjectName(current.gameObject.name) || IsMapToolPlacementRoot(current))
                return current.gameObject;

            current = current.parent;
        }

        return null;
    }

    private static bool IsMapToolPlacementRoot(Transform transform)
    {
        return transform != null &&
               transform.parent != null &&
               IsMapToolPlacementContainerName(transform.parent.name);
    }

    private void ClearMapToolSelection()
    {
        Selection.activeObject = null;
        selectedPalettePrefabPath = null;
        SceneView.RepaintAll();
        Repaint();
    }

    private static string GetPrefabAssetPathForPlacedObject(GameObject target)
    {
        if (target == null)
            return null;

        return PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(target);
    }

    private static bool TryGetPlacedObjectPrefabBaseRotation(GameObject target, out Quaternion prefabBaseRotation)
    {
        prefabBaseRotation = Quaternion.identity;
        string prefabPath = GetPrefabAssetPathForPlacedObject(target);
        if (string.IsNullOrEmpty(prefabPath))
            return false;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
            return false;

        prefabBaseRotation = prefab.transform.rotation;
        return true;
    }

    private static Texture2D GetCursorCellObjectPreview(string prefabPath)
    {
        if (string.IsNullOrEmpty(prefabPath))
            return null;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        return prefab == null
            ? null
            : AssetPreview.GetAssetPreview(prefab) ?? AssetPreview.GetMiniThumbnail(prefab) as Texture2D;
    }

    private void RotateSelectedObjectY(GameObject target, float deltaY)
    {
        if (target == null)
            return;

        Vector3 euler = NormalizeEulerForInspector(target.transform.eulerAngles);
        ApplyCursorObjectRotation(target, MoveObjectRotationYByStep(euler, deltaY));
    }

    private void ApplySelectedObjectPaletteYaw(GameObject target, Quaternion prefabBaseRotation, float yawOffset)
    {
        if (!ApplySelectedObjectRotationToTarget(target, BuildPalettePlacementRotation(prefabBaseRotation, yawOffset)))
            return;

        SceneView.RepaintAll();
        Repaint();
    }

    private void ApplySelectedPaletteYawOffset(
        PaletteItem selectedItem,
        NoryangjinMapToolPalettePlacementEntry entry,
        float yawOffset)
    {
        entry.yawOffset = yawOffset;
        SavePaletteDefaults();
        UpdatePlacementPreview(selectedItem);
        SceneView.RepaintAll();
        Repaint();
    }

    private void MoveSelectedObjectHeight(GameObject target, float deltaY)
    {
        if (target == null)
            return;

        Undo.RecordObject(target.transform, "Move Map Tool Object Height");
        Vector3 position = target.transform.position;
        position.y = MoveHeightByStep(position.y, deltaY);
        target.transform.position = position;
        EditorUtility.SetDirty(target);
        SceneView.RepaintAll();
        Repaint();
    }

    private void ApplySelectedObjectScale(GameObject target, Vector3 scale)
    {
        if (!ApplySelectedObjectScaleToTarget(target, scale, writePrefabAssetRoot: false))
            return;

        SceneView.RepaintAll();
        Repaint();
    }

    internal static bool ApplySelectedObjectScaleToTarget(GameObject target, Vector3 scale, bool writePrefabAssetRoot)
    {
        if (target == null)
            return false;

        Undo.RecordObject(target.transform, "Scale Map Tool Object");
        target.transform.localScale = scale;
        PrefabUtility.RecordPrefabInstancePropertyModifications(target.transform);
        EditorUtility.SetDirty(target);
        EditorUtility.SetDirty(target.transform);
        if (target.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(target.scene);

        string prefabPath = GetPrefabAssetPathForPlacedObject(target);
        if (writePrefabAssetRoot && !string.IsNullOrEmpty(prefabPath) &&
            !ApplyPrefabInstanceRootScaleOverride(target, prefabPath, scale) &&
            !ApplyPrefabAssetRootScale(prefabPath, scale))
        {
            return false;
        }

        return true;
    }

    private void ApplySelectedObjectPositionOffset(GameObject target, Vector2Int anchor, Vector2 offset)
    {
        ApplySelectedObjectPositionOffset(target, anchor, new Vector3(offset.x, 0f, offset.y));
    }

    private void ApplySelectedObjectPositionOffset(GameObject target, Vector2Int anchor, Vector3 offset)
    {
        if (target == null)
            return;

        Undo.RecordObject(target.transform, "Move Map Tool Object Offset");
        target.transform.position = BuildPlacedObjectPositionWithOffset(
            origin,
            anchor,
            BuildPlacementSnapCellSize(cellSize, false),
            placementHeight,
            offset);
        EditorUtility.SetDirty(target);
        SceneView.RepaintAll();
        Repaint();
    }

    private void MoveSelectedObjectByGridStep(GameObject target, int offsetX, int offsetZ)
    {
        if (target == null)
            return;

        Undo.RecordObject(target.transform, "Move Map Tool Object By Grid Step");
        target.transform.position = MoveObjectPositionByGridStep(
            target.transform.position,
            offsetX,
            offsetZ,
            BuildPlacementSnapCellSize(cellSize, false));
        PrefabUtility.RecordPrefabInstancePropertyModifications(target.transform);
        EditorUtility.SetDirty(target);
        EditorUtility.SetDirty(target.transform);
        if (target.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(target.scene);
        SceneView.RepaintAll();
        Repaint();
    }

    private void SaveSelectedObjectIndividualPlacement(GameObject target)
    {
        if (target == null)
            return;

        EditorUtility.SetDirty(target);
        EditorUtility.SetDirty(target.transform);
        if (target.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(target.scene);
            if (!string.IsNullOrEmpty(target.scene.path))
                EditorSceneManager.SaveScene(target.scene);
        }

        ShowNotification(new GUIContent("개별 저장됨"));
        SceneView.RepaintAll();
        Repaint();
    }

    private void SaveSelectedObjectAsPrefabPlacementDefault(GameObject target, Vector2Int anchor)
    {
        if (target == null)
            return;

        string prefabPath = GetPrefabAssetPathForPlacedObject(target);
        if (string.IsNullOrEmpty(prefabPath))
            return;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog(KoreanWindowTitle, $"프리팹을 찾을 수 없습니다:\n{prefabPath}", "확인");
            return;
        }

        NoryangjinMapToolPalettePlacementEntry entry = GetPaletteDefaults().GetOrCreateEntry(prefabPath);
        CopyPlacedObjectTransformToPaletteEntry(
            entry,
            target.transform.position,
            target.transform.rotation,
            target.transform.localScale,
            prefab.transform.rotation,
            prefab.transform.localScale,
            origin,
            anchor,
            BuildPlacementSnapCellSize(cellSize, false),
            placementHeight);
        SavePaletteDefaults();
        selectedPalettePrefabPath = prefabPath;
        PaletteItem? selectedItem = FindSelectedPaletteItem();
        if (selectedItem.HasValue)
            UpdatePlacementPreview(selectedItem.Value);
        ShowNotification(new GUIContent($"프리팹 기본값 저장됨: Y {entry.yawOffset:0.#}"));
        SceneView.RepaintAll();
        Repaint();
    }

    private void ApplyCursorObjectRotation(GameObject target, Vector3 euler)
    {
        if (!ApplySelectedObjectRotationToTarget(target, Quaternion.Euler(euler)))
            return;

        SceneView.RepaintAll();
        Repaint();
    }

    internal static bool ApplySelectedObjectRotationToTarget(GameObject target, Vector3 euler)
    {
        return ApplySelectedObjectRotationToTarget(target, Quaternion.Euler(euler));
    }

    internal static bool ApplySelectedObjectRotationToTarget(GameObject target, Quaternion rotation)
    {
        if (target == null)
            return false;

        Undo.RecordObject(target.transform, "Rotate Map Tool Object");
        target.transform.rotation = rotation;
        PrefabUtility.RecordPrefabInstancePropertyModifications(target.transform);
        EditorUtility.SetDirty(target);
        EditorUtility.SetDirty(target.transform);
        if (target.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(target.scene);

        return true;
    }

    private bool TryPlaceKnownRoadPiece(string prefabPath, NoryangjinMapToolPalettePlacementEntry placement, bool allowCursorAutoAdvance)
    {
        foreach (RoadPiece roadPiece in RoadPieces)
        {
            if (!string.Equals(roadPiece.PrefabPath, prefabPath, StringComparison.Ordinal))
                continue;

            PlaceRoad(roadPiece, placement, allowCursorAutoAdvance);
            return true;
        }

        return false;
    }

    private GameObject PlacePrefab(
        GameObject prefab,
        Transform parent,
        string instanceName,
        NoryangjinMapToolPalettePlacementEntry placement)
    {
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
        if (instance == null)
            instance = Instantiate(prefab, parent);

        instance.name = instanceName;
        instance.transform.position = BuildPalettePlacementPosition(
            origin,
            gridX,
            gridZ,
            BuildPlacementSnapCellSize(cellSize, false),
            placementHeight,
            placement.heightOffset,
            placement.positionOffset);
        instance.transform.rotation = BuildPalettePlacementRotation(prefab.transform.rotation, placement.yawOffset);
        instance.transform.localScale = BuildPalettePlacementScale(prefab.transform.localScale, placement.scale);
        Undo.RegisterCreatedObjectUndo(instance, $"Place {instanceName}");
        EditorUtility.SetDirty(instance);
        return instance;
    }

    private void RegisterLastPlacedObject(GameObject instance)
    {
        lastPlacedObjectInstanceId = instance != null ? instance.GetInstanceID() : 0;
        SceneView.RepaintAll();
        Repaint();
    }

    private static void OptimizeMapToolWaterInstance(GameObject instance)
    {
        if (instance == null)
            return;

        foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
        {
            Undo.RecordObject(collider, "Disable water collision");
            collider.enabled = false;
            EditorUtility.SetDirty(collider);
        }

        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
        {
            Undo.RecordObject(renderer, "Optimize water renderer");
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.allowOcclusionWhenDynamic = false;
            EditorUtility.SetDirty(renderer);
        }
    }

    private Vector2Int GetPaletteItemFootprint(PaletteItem item, float normalizedCellSize)
    {
        if (item.Prefab == null)
            return Vector2Int.one;

        NoryangjinMapToolPalettePlacementEntry placement = GetPaletteDefaults().GetOrCreateEntry(item.PrefabPath);
        Vector3 boundsSize = CalculateRendererBounds(item.Prefab).size;
        boundsSize.x *= Mathf.Abs(placement.scale.x);
        boundsSize.z *= Mathf.Abs(placement.scale.z);
        return ResolvePaletteFootprint(CalculateFootprintSize(boundsSize, normalizedCellSize), placement);
    }

    private List<Vector2Int> GetPaletteItemFootprintCells(PaletteItem item, float normalizedCellSize)
    {
        if (item.Prefab == null)
            return BuildAnchoredFootprintCells(new Vector2Int(gridX, gridZ), Vector2Int.one);

        NoryangjinMapToolPalettePlacementEntry placement = GetPaletteDefaults().GetOrCreateEntry(item.PrefabPath);
        if (!placement.useManualFootprint)
            return BuildBoundsFootprintCells(GetPaletteItemPlacementBounds(item, placement), origin, normalizedCellSize);

        return BuildDisplayedFootprintCells(
            new Vector2Int(gridX, gridZ),
            ScaleManualFootprintForPlacementGrid(GetPaletteItemFootprint(item, NoryangjinMapToolGridUtility.NormalizeCellSize(cellSize))));
    }

    private Bounds GetPaletteItemPlacementBounds(PaletteItem item, NoryangjinMapToolPalettePlacementEntry placement)
    {
        if (placementPreviewInstance != null &&
            string.Equals(placementPreviewPrefabPath, item.PrefabPath, StringComparison.Ordinal))
            return CalculateRendererBounds(placementPreviewInstance);

        GameObject preview = PrefabUtility.InstantiatePrefab(item.Prefab) as GameObject;
        if (preview == null)
            preview = Instantiate(item.Prefab);

        try
        {
            preview.hideFlags = HideFlags.HideAndDontSave;
            preview.transform.position = BuildPalettePlacementPosition(
                origin,
                gridX,
                gridZ,
                BuildPlacementSnapCellSize(cellSize, false),
                placementHeight,
                placement.heightOffset,
                placement.positionOffset);
            preview.transform.rotation = BuildPalettePlacementRotation(item.Prefab.transform.rotation, placement.yawOffset);
            preview.transform.localScale = BuildPalettePlacementScale(item.Prefab.transform.localScale, placement.scale);
            return CalculateRendererBounds(preview);
        }
        finally
        {
            DestroyImmediate(preview);
        }
    }

    private static NoryangjinMapToolPlacementLayer GetPaletteItemLayer(PaletteItem item)
    {
        return GetPaletteItemLayer(item.PrefabPath, item.Category);
    }

    internal static NoryangjinMapToolPlacementLayer GetPaletteItemLayer(
        string prefabPath,
        NoryangjinMapToolPaletteCategory category)
    {
        if (IsSeagullPerchPrefabPath(prefabPath))
            return NoryangjinMapToolPlacementLayer.SeagullPerch;

        if (IsBackgroundOverlayObjectPrefabPath(prefabPath))
            return NoryangjinMapToolPlacementLayer.Object;

        if (category == NoryangjinMapToolPaletteCategory.Background)
            return NoryangjinMapToolPlacementLayer.Background;

        return category == NoryangjinMapToolPaletteCategory.Road
            ? NoryangjinMapToolPlacementLayer.Road
            : NoryangjinMapToolPlacementLayer.Object;
    }

    private static NoryangjinMapToolPlacementLayer GetPlacedObjectLayer(GameObject target)
    {
        string prefabPath = GetPrefabAssetPathForPlacedObject(target);
        if (IsSeagullPerchPrefabPath(prefabPath))
            return NoryangjinMapToolPlacementLayer.SeagullPerch;

        if (IsBackgroundOverlayObjectPrefabPath(prefabPath))
            return NoryangjinMapToolPlacementLayer.Object;

        if (!string.IsNullOrEmpty(prefabPath) &&
            CategorizePrefabPath(prefabPath) == NoryangjinMapToolPaletteCategory.Background)
        {
            return NoryangjinMapToolPlacementLayer.Background;
        }

        Transform current = target == null ? null : target.transform;
        while (current != null)
        {
            if (string.Equals(current.name, RoadParentName, StringComparison.Ordinal))
                return NoryangjinMapToolPlacementLayer.Road;

            current = current.parent;
        }

        return NoryangjinMapToolPlacementLayer.Object;
    }

    private static Bounds CalculateRendererBounds(GameObject target)
    {
        if (target == null)
            return new Bounds(Vector3.zero, Vector3.zero);

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(target.transform.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }

    private void SnapSelectionToGrid()
    {
        foreach (GameObject selected in Selection.gameObjects)
        {
            Undo.RecordObject(selected.transform, "Snap Selection To Grid");
            selected.transform.position = NoryangjinMapToolGridUtility.SnapToGrid(
                selected.transform.position,
                origin,
                BuildPlacementSnapCellSize(cellSize, false),
                placementHeight);
            EditorUtility.SetDirty(selected);
        }

        SceneView.RepaintAll();
    }

    private void MoveCursorToSelection()
    {
        if (Selection.activeTransform == null)
            return;

        Vector3 position = Selection.activeTransform.position;
        Vector2Int cursor = BuildPlacementGridCell(position, origin, cellSize, false);
        gridX = cursor.x;
        gridZ = cursor.y;
        SceneView.RepaintAll();
        Repaint();
    }

    private void MoveCursorBy(int offsetX, int offsetZ)
    {
        Vector2Int moved = MoveGridCoordinate(new Vector2Int(gridX, gridZ), offsetX, offsetZ);
        gridX = moved.x;
        gridZ = moved.y;
        direction = DirectionFromMoveOffset(offsetX, offsetZ);
        SceneView.RepaintAll();
        Repaint();
    }

    private void ResetCursor()
    {
        gridX = 0;
        gridZ = 0;
        SceneView.RepaintAll();
        Repaint();
    }

    private void UseSelectedPrefabAsProp()
    {
        GameObject selected = Selection.activeObject as GameObject;
        if (selected == null)
            return;

        string path = AssetDatabase.GetAssetPath(selected);
        if (string.IsNullOrEmpty(path) || !IsSelectablePalettePrefabPath(path))
        {
            EditorUtility.DisplayDialog(
                KoreanWindowTitle,
                $"다음 폴더 아래의 프리팹을 선택하세요:\n{string.Join("\n", PalettePrefabRoots)}",
                "확인");
            return;
        }

        propPrefab = selected;
    }

    private static bool SetupMapToolSceneDefaults()
    {
        bool changed = false;
        GameObject root = EnsureRoot();
        changed |= EnsureChildExists(root.transform, RoadParentName);
        changed |= EnsureChildExists(root.transform, PropParentName);
        changed |= EnsureChildExists(root.transform, WaterParentName);
        changed |= EnsureWorkFloor(root.transform);
        changed |= EnsureWorkGrid(root.transform);
        changed |= EnsureOriginPost(root.transform);

        Camera camera = Camera.main;
        if (camera != null)
        {
            changed |= SetObjectName(camera.gameObject, "MapTool_Camera");
            changed |= SetTransform(camera.transform, new Vector3(22f, 24f, -22f), Quaternion.Euler(55f, -45f, 0f), Vector3.one);
            changed |= SetCameraProjection(camera, true, 24f);
        }

        Light light = FindFirstObjectByType<Light>();
        if (light != null)
        {
            changed |= SetObjectName(light.gameObject, "MapTool_DirectionalLight");
            changed |= SetTransform(light.transform, light.transform.position, Quaternion.Euler(50f, -30f, 0f), Vector3.one);
            if (!Mathf.Approximately(light.intensity, 1.2f))
            {
                light.intensity = 1.2f;
                changed = true;
            }
        }

        if (changed)
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        return changed;
    }

    private static GameObject EnsureRoot()
    {
        GameObject root = GameObject.Find(RootName);
        if (root != null)
            return root;

        root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Create Noryangjin Map Tool Root");
        return root;
    }

    private static GameObject FindOrCreateRootIncludingInactive()
    {
        GameObject root = FindSceneObjectByNameIncludingInactive(RootName);
        if (root != null)
            return root;

        return EnsureRoot();
    }

    private static GameObject FindSceneObjectByNameIncludingInactive(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
            return null;

        Scene activeScene = SceneManager.GetActiveScene();
        foreach (GameObject candidate in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (candidate == null ||
                EditorUtility.IsPersistent(candidate) ||
                !candidate.scene.IsValid() ||
                candidate.scene != activeScene ||
                !string.Equals(candidate.name, objectName, StringComparison.Ordinal))
            {
                continue;
            }

            return candidate;
        }

        return null;
    }

    private static bool DestroyTransientPlacementPreviewObjects()
    {
        return DestroyTransientPlacementPreviewObjects(recordUndo: true);
    }

    private static bool DestroyTransientPlacementPreviewObjects(bool recordUndo)
    {
        var targets = new List<GameObject>();
        foreach (GameObject candidate in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (candidate == null ||
                EditorUtility.IsPersistent(candidate) ||
                !string.Equals(candidate.name, PlacementPreviewName, StringComparison.Ordinal))
            {
                continue;
            }

            targets.Add(candidate);
        }

        foreach (GameObject target in targets)
        {
            if (recordUndo)
                Undo.DestroyObjectImmediate(target);
            else
                DestroyImmediate(target);
        }

        return targets.Count > 0;
    }

    private static Transform EnsureChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
            return child;

        var childObject = new GameObject(childName);
        childObject.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(childObject, $"Create {childName}");
        return childObject.transform;
    }

    private static bool EnsureChildExists(Transform parent, string childName)
    {
        bool exists = parent.Find(childName) != null;
        EnsureChild(parent, childName);
        return !exists;
    }

    private static bool EnsureWorkFloor(Transform parent)
    {
        GameObject floor = FindOrCreatePrimitiveChild(parent, WorkFloorName, PrimitiveType.Cube, out bool changed);
        float floorSize = BuildWorkGridFloorSize(DefaultCellSize);
        changed |= SetTransform(floor.transform, new Vector3(0f, -0.05f, 0f), Quaternion.identity, new Vector3(floorSize, 0.04f, floorSize));
        changed |= RemoveCollider(floor);
        changed |= SetRendererMaterial(floor, EnsureMapToolMaterial(WorkFloorMaterialPath, new Color(0.88f, 0.9f, 0.88f, 1f)));
        changed |= SetRendererEnabled(floor, false);
        return changed;
    }

    private static bool EnsureOriginPost(Transform parent)
    {
        GameObject marker = FindOrCreatePrimitiveChild(parent, OriginPostName, PrimitiveType.Cylinder, out bool changed);
        changed |= SetTransform(marker.transform, new Vector3(0f, 0.16f, 0f), Quaternion.identity, new Vector3(0.12f, 0.16f, 0.12f));
        changed |= RemoveCollider(marker);
        changed |= SetRendererMaterial(marker, EnsureMapToolMaterial(OriginMarkerMaterialPath, new Color(1f, 0.78f, 0.12f, 1f)));
        return changed;
    }

    private static bool EnsureWorkGrid(Transform parent)
    {
        bool changed = EnsureChildExists(parent, WorkGridName);
        Transform grid = parent.Find(WorkGridName);
        Material material = EnsureMapToolMaterial(WorkGridMaterialPath, new Color(0.02f, 0.34f, 0.42f, 1f));
        float span = BuildWorkGridSpan(DefaultCellSize);

        for (int i = -WorkGridExtent; i <= WorkGridExtent + 1; i++)
        {
            float offset = BuildWorkGridBoundaryOffset(i, DefaultCellSize);
            GameObject xLine = FindOrCreatePrimitiveChild(grid, $"MapTool_Work_Grid_X_{FormatGridIndex(i)}", PrimitiveType.Cube, out bool xCreated);
            changed |= xCreated;
            changed |= SetTransform(
                xLine.transform,
                new Vector3(offset, WorkGridLineY, 0f),
                Quaternion.identity,
                new Vector3(WorkGridLineWidth, WorkGridLineVerticalThickness, span));
            changed |= RemoveCollider(xLine);
            changed |= SetRendererMaterial(xLine, material);
            changed |= SetRendererEnabled(xLine, false);

            GameObject zLine = FindOrCreatePrimitiveChild(grid, $"MapTool_Work_Grid_Z_{FormatGridIndex(i)}", PrimitiveType.Cube, out bool zCreated);
            changed |= zCreated;
            changed |= SetTransform(
                zLine.transform,
                new Vector3(0f, WorkGridLineY, offset),
                Quaternion.identity,
                new Vector3(span, WorkGridLineVerticalThickness, WorkGridLineWidth));
            changed |= RemoveCollider(zLine);
            changed |= SetRendererMaterial(zLine, material);
            changed |= SetRendererEnabled(zLine, false);
        }

        changed |= DestroyChildrenWithPrefix(grid, "MapTool_Work_SubGrid_");

        return changed;
    }

    private static bool DestroyChildrenWithPrefix(Transform parent, string prefix)
    {
        var targets = new List<GameObject>();
        foreach (Transform child in parent)
        {
            if (child.name.StartsWith(prefix, StringComparison.Ordinal))
                targets.Add(child.gameObject);
        }

        foreach (GameObject target in targets)
            Undo.DestroyObjectImmediate(target);

        return targets.Count > 0;
    }

    private static string FormatGridIndex(int index)
    {
        return index < 0 ? $"N{Mathf.Abs(index):00}" : $"P{index:00}";
    }

    private static GameObject FindOrCreatePrimitiveChild(Transform parent, string childName, PrimitiveType primitiveType, out bool changed)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            changed = false;
            return child.gameObject;
        }

        GameObject childObject = GameObject.CreatePrimitive(primitiveType);
        childObject.name = childName;
        childObject.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(childObject, $"Create {childName}");
        changed = true;
        return childObject;
    }

    private static Material EnsureMapToolMaterial(string assetPath, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                        Shader.Find("Unlit/Color") ??
                        Shader.Find("Universal Render Pipeline/Lit") ??
                        Shader.Find("Standard");
        Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (material == null)
        {
            string directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, assetPath);
        }
        else if (shader != null && material.shader != shader)
        {
            material.shader = shader;
        }

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        else if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);

        EditorUtility.SetDirty(material);
        return material;
    }

    private static bool SetObjectName(GameObject target, string expectedName)
    {
        if (target.name == expectedName)
            return false;

        target.name = expectedName;
        return true;
    }

    private static bool SetTransform(Transform target, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        bool changed = false;
        if (target.localPosition != position)
        {
            target.localPosition = position;
            changed = true;
        }

        if (target.localRotation != rotation)
        {
            target.localRotation = rotation;
            changed = true;
        }

        if (target.localScale != scale)
        {
            target.localScale = scale;
            changed = true;
        }

        return changed;
    }

    private static bool SetCameraProjection(Camera camera, bool orthographic, float orthographicSize)
    {
        bool changed = false;
        if (camera.orthographic != orthographic)
        {
            camera.orthographic = orthographic;
            changed = true;
        }

        if (!Mathf.Approximately(camera.orthographicSize, orthographicSize))
        {
            camera.orthographicSize = orthographicSize;
            changed = true;
        }

        return changed;
    }

    private static bool RemoveCollider(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();
        if (collider == null)
            return false;

        UnityEngine.Object.DestroyImmediate(collider);
        return true;
    }

    private static bool SetRendererMaterial(GameObject target, Material material)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer == null || renderer.sharedMaterial == material)
            return false;

        renderer.sharedMaterial = material;
        return true;
    }

    private static bool SetRendererEnabled(GameObject target, bool enabled)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer == null || renderer.enabled == enabled)
            return false;

        renderer.enabled = enabled;
        return true;
    }

    private static void FrameMapToolSceneView()
    {
        ApplyMapToolSceneViewPreset(BuildSceneViewPreset(false));
    }

    private void ToggleMapToolSceneView()
    {
        isTopSceneView = !isTopSceneView;
        ApplyMapToolSceneViewPreset(BuildSceneViewPreset(isTopSceneView));
        Repaint();
    }

    private static void ApplyMapToolSceneViewPreset(NoryangjinMapToolSceneViewPreset preset)
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null && SceneView.sceneViews.Count > 0)
            sceneView = SceneView.sceneViews[0] as SceneView;

        if (sceneView == null)
            return;

        sceneView.LookAt(Vector3.zero, preset.Rotation, preset.Size, preset.Orthographic);
        sceneView.Repaint();
    }

    internal static NoryangjinMapToolSceneViewPreset BuildSceneViewPreset(bool topView)
    {
        return topView
            ? new NoryangjinMapToolSceneViewPreset(Quaternion.Euler(90f, 0f, 0f), TopSceneViewSize, true)
            : new NoryangjinMapToolSceneViewPreset(Quaternion.Euler(55f, -45f, 0f), DefaultSceneViewSize, true);
    }

    private static void PingAsset(string assetPath)
    {
        UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
        if (asset == null)
            return;

        EditorGUIUtility.PingObject(asset);
        Selection.activeObject = asset;
    }

    private NoryangjinMapToolPaletteDefaults GetPaletteDefaults()
    {
        if (paletteDefaults != null)
            return paletteDefaults;

        paletteDefaults = AssetDatabase.LoadAssetAtPath<NoryangjinMapToolPaletteDefaults>(PaletteDefaultsPath);
        if (paletteDefaults != null)
            return paletteDefaults;

        paletteDefaults = CreateInstance<NoryangjinMapToolPaletteDefaults>();
        string directory = Path.GetDirectoryName(PaletteDefaultsPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        AssetDatabase.CreateAsset(paletteDefaults, PaletteDefaultsPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return paletteDefaults;
    }

    private void SavePaletteDefaults()
    {
        EditorUtility.SetDirty(GetPaletteDefaults());
        AssetDatabase.SaveAssets();
    }

    internal static bool IsMapToolScenePath(string scenePath)
    {
        return string.Equals(scenePath, MapToolScenePath, StringComparison.OrdinalIgnoreCase);
    }

    internal static string FormatCursorStatus(
        bool isMapToolScene,
        int cursorX,
        int cursorZ,
        NoryangjinMapToolDirection cursorDirection,
        float currentCellSize)
    {
        string sceneLabel = isMapToolScene ? "맵툴 씬" : "일반 씬";
        return $"{sceneLabel} | 커서 X {cursorX} / Z {cursorZ} | 방향 {DirectionToKorean(cursorDirection)} | 셀 {NoryangjinMapToolGridUtility.NormalizeCellSize(currentCellSize):0.###}";
    }

    internal static Vector2Int MoveGridCoordinate(Vector2Int currentCoordinate, int offsetX, int offsetZ)
    {
        return new Vector2Int(currentCoordinate.x + offsetX, currentCoordinate.y + offsetZ);
    }

    internal static Vector3 BuildPalettePlacementPosition(
        Vector3 currentOrigin,
        int cursorX,
        int cursorZ,
        float currentCellSize,
        float currentPlacementHeight,
        float itemHeightOffset)
    {
        return BuildPalettePlacementPosition(
            currentOrigin,
            cursorX,
            cursorZ,
            currentCellSize,
            currentPlacementHeight,
            itemHeightOffset,
            Vector2.zero);
    }

    internal static Vector3 BuildPalettePlacementPosition(
        Vector3 currentOrigin,
        int cursorX,
        int cursorZ,
        float currentCellSize,
        float currentPlacementHeight,
        float itemHeightOffset,
        Vector2 itemPositionOffset)
    {
        return BuildPalettePlacementPosition(
            currentOrigin,
            cursorX,
            cursorZ,
            currentCellSize,
            currentPlacementHeight,
            itemHeightOffset,
            new Vector3(itemPositionOffset.x, 0f, itemPositionOffset.y));
    }

    internal static Vector3 BuildPalettePlacementPosition(
        Vector3 currentOrigin,
        int cursorX,
        int cursorZ,
        float currentCellSize,
        float currentPlacementHeight,
        float itemHeightOffset,
        Vector3 itemPositionOffset)
    {
        Vector3 position = NoryangjinMapToolGridUtility.GridToWorld(
            currentOrigin,
            cursorX,
            cursorZ,
            currentCellSize,
            currentPlacementHeight + itemHeightOffset + itemPositionOffset.y);
        position.x += itemPositionOffset.x;
        position.z += itemPositionOffset.z;
        return position;
    }

    internal static Vector2 CalculatePlacedObjectPositionOffset(
        Vector3 objectPosition,
        Vector3 currentOrigin,
        Vector2Int anchor,
        float currentCellSize)
    {
        Vector3 anchorPosition = NoryangjinMapToolGridUtility.GridToWorld(
            currentOrigin,
            anchor.x,
            anchor.y,
            currentCellSize,
            objectPosition.y);
        return new Vector2(objectPosition.x - anchorPosition.x, objectPosition.z - anchorPosition.z);
    }

    internal static Vector3 CalculatePlacedObjectPositionOffset(
        Vector3 objectPosition,
        Vector3 currentOrigin,
        Vector2Int anchor,
        float currentCellSize,
        float currentPlacementHeight)
    {
        Vector3 anchorPosition = NoryangjinMapToolGridUtility.GridToWorld(
            currentOrigin,
            anchor.x,
            anchor.y,
            currentCellSize,
            currentPlacementHeight);
        return new Vector3(
            objectPosition.x - anchorPosition.x,
            objectPosition.y - currentPlacementHeight,
            objectPosition.z - anchorPosition.z);
    }

    internal static Vector3 BuildPlacedObjectPositionWithOffset(
        Vector3 currentOrigin,
        Vector2Int anchor,
        float currentCellSize,
        float currentHeight,
        Vector2 offset)
    {
        return BuildPlacedObjectPositionWithOffset(
            currentOrigin,
            anchor,
            currentCellSize,
            currentHeight,
            new Vector3(offset.x, 0f, offset.y));
    }

    internal static Vector3 BuildPlacedObjectPositionWithOffset(
        Vector3 currentOrigin,
        Vector2Int anchor,
        float currentCellSize,
        float currentHeight,
        Vector3 offset)
    {
        Vector3 anchorPosition = NoryangjinMapToolGridUtility.GridToWorld(
            currentOrigin,
            anchor.x,
            anchor.y,
            currentCellSize,
            currentHeight);
        anchorPosition.x += offset.x;
        anchorPosition.y += offset.y;
        anchorPosition.z += offset.z;
        return anchorPosition;
    }

    internal static void CopyPlacedObjectTransformToPaletteEntry(
        NoryangjinMapToolPalettePlacementEntry entry,
        Vector3 objectPosition,
        Quaternion objectRotation,
        Vector3 objectScale,
        Quaternion prefabBaseRotation,
        Vector3 prefabBaseScale,
        Vector3 currentOrigin,
        Vector2Int anchor,
        float currentCellSize,
        float currentPlacementHeight)
    {
        if (entry == null)
            return;

        float normalizedCellSize = NoryangjinMapToolGridUtility.NormalizeCellSize(currentCellSize);
        Vector3 positionOffset = CalculatePlacedObjectPositionOffset(
            objectPosition,
            currentOrigin,
            anchor,
            normalizedCellSize,
            currentPlacementHeight);
        entry.positionOffset = new Vector2(positionOffset.x, positionOffset.z);
        entry.heightOffset = positionOffset.y;
        entry.yawOffset = CalculatePaletteYawOffsetFromPlacedRotation(objectRotation, prefabBaseRotation);
        entry.scale = CalculatePaletteScaleMultiplier(objectScale, prefabBaseScale);
    }

    internal static float CalculatePaletteYawOffsetFromPlacedRotation(
        Quaternion objectRotation,
        Quaternion prefabBaseRotation)
    {
        Quaternion yawRotation = objectRotation * Quaternion.Inverse(prefabBaseRotation);
        return NormalizeEulerAngleForInspector(yawRotation.eulerAngles.y);
    }

    internal static Vector3 CalculatePaletteScaleMultiplier(Vector3 objectScale, Vector3 prefabBaseScale)
    {
        return new Vector3(
            DivideScaleComponent(objectScale.x, prefabBaseScale.x),
            DivideScaleComponent(objectScale.y, prefabBaseScale.y),
            DivideScaleComponent(objectScale.z, prefabBaseScale.z));
    }

    private static float DivideScaleComponent(float objectScale, float prefabBaseScale)
    {
        return Mathf.Approximately(prefabBaseScale, 0f) ? 1f : objectScale / prefabBaseScale;
    }

    internal static Quaternion BuildPalettePlacementRotation(
        Quaternion prefabBaseRotation,
        float itemYawOffset)
    {
        Quaternion cursorYaw = Quaternion.Euler(0f, itemYawOffset, 0f);
        return cursorYaw * prefabBaseRotation;
    }

    internal static Vector3 NormalizeEulerForInspector(Vector3 euler)
    {
        return new Vector3(
            NormalizeEulerAngleForInspector(euler.x),
            NormalizeEulerAngleForInspector(euler.y),
            NormalizeEulerAngleForInspector(euler.z));
    }

    private static float NormalizeEulerAngleForInspector(float angle)
    {
        angle %= 360f;
        if (angle > 180f)
            angle -= 360f;
        if (angle < -180f)
            angle += 360f;
        return angle;
    }

    internal static Vector3 BuildPalettePlacementScale(Vector3 prefabBaseScale, Vector3 itemScaleMultiplier)
    {
        return Vector3.Scale(prefabBaseScale, itemScaleMultiplier);
    }

    internal static Vector2Int CalculateFootprintSize(Vector3 worldBoundsSize, float currentCellSize)
    {
        float normalizedCellSize = NoryangjinMapToolGridUtility.NormalizeCellSize(currentCellSize);
        int width = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(worldBoundsSize.x) / normalizedCellSize));
        int depth = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(worldBoundsSize.z) / normalizedCellSize));
        return new Vector2Int(width, depth);
    }

    internal static Vector2Int NormalizeManualFootprint(Vector2Int footprint)
    {
        return new Vector2Int(Mathf.Max(1, footprint.x), Mathf.Max(1, footprint.y));
    }

    internal static Vector2Int ResolvePaletteFootprint(Vector2Int automaticFootprint, NoryangjinMapToolPalettePlacementEntry placement)
    {
        if (placement != null && placement.useManualFootprint)
            return NormalizeManualFootprint(placement.manualFootprint);

        return NormalizeManualFootprint(automaticFootprint);
    }

    internal static List<Vector2Int> BuildAnchoredFootprintCells(Vector2Int anchor, Vector2Int footprint)
    {
        int width = Mathf.Max(1, footprint.x);
        int depth = Mathf.Max(1, footprint.y);
        var cells = new List<Vector2Int>(width * depth);

        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
                cells.Add(new Vector2Int(anchor.x + x, anchor.y + z));
        }

        return cells;
    }

    internal static List<Vector2Int> BuildDisplayedFootprintCells(Vector2Int anchor, Vector2Int displayedFootprint)
    {
        return BuildAnchoredFootprintCells(anchor, NormalizeManualFootprint(displayedFootprint));
    }

    internal static Vector2Int ScaleManualFootprintForPlacementGrid(Vector2Int footprint)
    {
        Vector2Int normalized = NormalizeManualFootprint(footprint);
        return new Vector2Int(
            normalized.x * WorkGridSubdivisionsPerCell,
            normalized.y * WorkGridSubdivisionsPerCell);
    }

    internal static bool CanPlaceFootprint(Vector2Int anchor, Vector2Int footprint, ISet<Vector2Int> occupiedCells)
    {
        foreach (Vector2Int cell in BuildAnchoredFootprintCells(anchor, footprint))
        {
            if (occupiedCells != null && occupiedCells.Contains(cell))
                return false;
        }

        return true;
    }

    internal static bool CanPlaceFootprintCells(IEnumerable<Vector2Int> footprintCells, ISet<Vector2Int> occupiedCells)
    {
        foreach (Vector2Int cell in footprintCells)
        {
            if (occupiedCells != null && occupiedCells.Contains(cell))
                return false;
        }

        return true;
    }

    internal static bool CanPlaceFootprintCells(
        IEnumerable<Vector2Int> footprintCells,
        NoryangjinMapToolPlacementLayer placementLayer,
        ISet<NoryangjinMapToolOccupiedCell> occupiedCells)
    {
        foreach (Vector2Int cell in footprintCells)
        {
            if (occupiedCells != null && occupiedCells.Contains(new NoryangjinMapToolOccupiedCell(cell, placementLayer)))
                return false;
        }

        return true;
    }

    internal static NoryangjinMapToolSceneGridCellState ResolveFootprintPreviewState(
        IEnumerable<Vector2Int> footprintCells,
        NoryangjinMapToolPlacementLayer placementLayer,
        ISet<NoryangjinMapToolOccupiedCell> occupiedCells)
    {
        return CanPlaceFootprintCells(footprintCells, placementLayer, occupiedCells)
            ? NoryangjinMapToolSceneGridCellState.Empty
            : NoryangjinMapToolSceneGridCellState.Occupied;
    }

    internal static int GetSelectionPriority(NoryangjinMapToolPlacementLayer placementLayer)
    {
        return placementLayer == NoryangjinMapToolPlacementLayer.Object ? 0 : 1;
    }

    internal static float MoveHeightByStep(float currentHeight, float step)
    {
        return currentHeight + step;
    }

    internal static float MovePositionOffsetByStep(float currentOffset, float step)
    {
        return currentOffset + step;
    }

    internal static Vector3 MoveObjectPositionByGridStep(
        Vector3 currentPosition,
        int offsetX,
        int offsetZ,
        float snapCellSize)
    {
        float normalizedSnapCellSize = NoryangjinMapToolGridUtility.NormalizeCellSize(snapCellSize);
        return new Vector3(
            currentPosition.x + offsetX * normalizedSnapCellSize,
            currentPosition.y,
            currentPosition.z + offsetZ * normalizedSnapCellSize);
    }

    internal static Vector3 MoveObjectRotationYByStep(Vector3 currentEuler, float deltaY)
    {
        return new Vector3(currentEuler.x, currentEuler.y + deltaY, currentEuler.z);
    }

    internal static Vector3 BuildSelectedObjectScaleFromFields(Vector3 scale)
    {
        return scale;
    }

    internal static Vector3 BuildSelectedObjectScaleFromDisplayedFields(float x, float z, float y)
    {
        return new Vector3(x, y, z);
    }

    internal static bool ApplyPrefabAssetRootScale(string prefabPath, Vector3 scale)
    {
        if (string.IsNullOrEmpty(prefabPath) || AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            return false;

        GameObject prefabContents = null;
        try
        {
            prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabContents == null)
                return false;

            if (prefabContents.transform.localScale != scale)
            {
                prefabContents.transform.localScale = scale;
                PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
                AssetDatabase.SaveAssets();
            }

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"Failed to apply prefab asset root scale '{prefabPath}': {exception.Message}");
            return false;
        }
        finally
        {
            if (prefabContents != null)
                PrefabUtility.UnloadPrefabContents(prefabContents);
        }
    }

    internal static bool ApplyPrefabInstanceRootScaleOverride(GameObject target, string prefabPath, Vector3 scale)
    {
        if (target == null || string.IsNullOrEmpty(prefabPath) || !PrefabUtility.IsPartOfPrefabInstance(target))
            return false;

        GameObject prefabInstanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(target);
        if (prefabInstanceRoot == null)
            return false;

        Transform rootTransform = prefabInstanceRoot.transform;
        rootTransform.localScale = scale;

        var serializedTransform = new SerializedObject(rootTransform);
        SerializedProperty localScaleProperty = serializedTransform.FindProperty("m_LocalScale");
        if (localScaleProperty == null)
            return false;

        localScaleProperty.vector3Value = scale;
        serializedTransform.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(rootTransform);
        PrefabUtility.ApplyPropertyOverride(localScaleProperty, prefabPath, InteractionMode.AutomatedAction);
        AssetDatabase.SaveAssets();
        return true;
    }

    internal static float MovePlacementYawOffsetByStep(float currentYawOffset, float step)
    {
        return currentYawOffset + step;
    }

    internal static bool CanPlaceEmptyPaletteItem(Vector2Int anchor, ISet<Vector2Int> occupiedCells)
    {
        return true;
    }

    internal static List<Vector2Int> BuildBoundsFootprintCells(Bounds bounds, Vector3 currentOrigin, float currentCellSize)
    {
        float normalizedCellSize = NoryangjinMapToolGridUtility.NormalizeCellSize(currentCellSize);
        int minX = Mathf.CeilToInt((bounds.min.x - currentOrigin.x) / normalizedCellSize);
        int maxX = Mathf.FloorToInt((bounds.max.x - currentOrigin.x) / normalizedCellSize);
        int minZ = Mathf.CeilToInt((bounds.min.z - currentOrigin.z) / normalizedCellSize);
        int maxZ = Mathf.FloorToInt((bounds.max.z - currentOrigin.z) / normalizedCellSize);

        var cells = new List<Vector2Int>();
        for (int z = minZ; z <= maxZ; z++)
        {
            for (int x = minX; x <= maxX; x++)
                cells.Add(new Vector2Int(x, z));
        }

        if (cells.Count > 0)
            return cells;

        int fallbackX = Mathf.RoundToInt((bounds.center.x - currentOrigin.x) / normalizedCellSize);
        int fallbackZ = Mathf.RoundToInt((bounds.center.z - currentOrigin.z) / normalizedCellSize);
        return new List<Vector2Int> { new(fallbackX, fallbackZ) };
    }

    internal static string BuildFootprintLabel(Vector2Int footprint)
    {
        return $"{Mathf.Max(1, footprint.x)}x{Mathf.Max(1, footprint.y)}";
    }

    internal static bool ShouldAdvanceRoadCursorAfterPlacement(bool userEnabledAdvance, bool placementAllowsAdvance)
    {
        return userEnabledAdvance && placementAllowsAdvance;
    }

    internal static float MigrateCellSizeDefault(float currentCellSize)
    {
        return Mathf.Approximately(currentCellSize, LegacyDefaultCellSize) ||
               Mathf.Approximately(currentCellSize, PreviousDefaultCellSize)
            ? DefaultCellSize
            : currentCellSize;
    }

    internal static string FormatSimplePlacementHint(Vector3 cursorWorldPosition, NoryangjinMapToolDirection cursorDirection)
    {
        return $"월드 좌표 ({cursorWorldPosition.x:0.##}, {cursorWorldPosition.y:0.##}, {cursorWorldPosition.z:0.##}) / 방향 {DirectionToKorean(cursorDirection)}. 커서를 맞춘 뒤 아이콘을 누르세요.";
    }

    internal static NoryangjinMapToolPaletteCategory CategorizePrefabPath(string prefabPath)
    {
        string normalizedPath = prefabPath.Replace('\\', '/');
        if (string.Equals(normalizedPath, JhWaterPrefabPath, StringComparison.OrdinalIgnoreCase))
            return NoryangjinMapToolPaletteCategory.Background;

        if (normalizedPath.Contains("_ROAD_", StringComparison.OrdinalIgnoreCase))
            return NoryangjinMapToolPaletteCategory.Road;

        if (normalizedPath.Contains("_BLD_", StringComparison.OrdinalIgnoreCase))
            return NoryangjinMapToolPaletteCategory.Building;

        if (normalizedPath.Contains("_BG_", StringComparison.OrdinalIgnoreCase))
            return NoryangjinMapToolPaletteCategory.Background;

        if (normalizedPath.Contains("_DCR_", StringComparison.OrdinalIgnoreCase) ||
            normalizedPath.Contains("_BND_", StringComparison.OrdinalIgnoreCase))
            return NoryangjinMapToolPaletteCategory.Decoration;

        return NoryangjinMapToolPaletteCategory.Prop;
    }

    internal static string BuildPaletteLabel(string prefabPath)
    {
        string fileName = Path.GetFileNameWithoutExtension(prefabPath);
        string[] parts = fileName.Split('_');
        int labelStart = parts.Length > 5 ? 5 : 0;
        var labelParts = new List<string>();

        for (int i = labelStart; i < parts.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(parts[i]))
                labelParts.Add(parts[i]);
        }

        string englishLabel = labelParts.Count > 0 ? string.Join(" ", labelParts) : fileName.Replace('_', ' ');
        return KoreanPaletteLabels.TryGetValue(englishLabel, out string koreanLabel) ? koreanLabel : englishLabel;
    }

    internal static bool IsSelectablePalettePrefabPath(string prefabPath)
    {
        string normalizedPath = prefabPath.Replace('\\', '/');
        foreach (string palettePrefabRoot in PalettePrefabRoots)
        {
            if (IsAssetUnderPath(normalizedPath, palettePrefabRoot))
                return true;
        }

        return false;
    }

    private static bool IsPalettePrefabPathAllowed(string prefabPath)
    {
        string normalizedPath = prefabPath.Replace('\\', '/');
        if (!IsSelectablePalettePrefabPath(normalizedPath))
            return false;

        if (string.Equals(normalizedPath, DockMetalCleatPrefabPath, StringComparison.OrdinalIgnoreCase))
            return false;

        return CategorizePrefabPath(normalizedPath) != NoryangjinMapToolPaletteCategory.Road;
    }

    private static bool IsAssetUnderPath(string assetPath, string rootPath)
    {
        string normalizedAssetPath = assetPath.Replace('\\', '/');
        string normalizedRootPath = rootPath.Replace('\\', '/');
        return normalizedAssetPath.StartsWith(normalizedRootPath + "/", StringComparison.OrdinalIgnoreCase);
    }

    internal static string PaletteCategoryToKorean(NoryangjinMapToolPaletteCategory category)
    {
        return category switch
        {
            NoryangjinMapToolPaletteCategory.Road => "도로",
            NoryangjinMapToolPaletteCategory.Building => "건물",
            NoryangjinMapToolPaletteCategory.Prop => "소품",
            NoryangjinMapToolPaletteCategory.Decoration => "장식",
            NoryangjinMapToolPaletteCategory.Background => "배경",
            _ => "전체"
        };
    }

    internal static int CalculatePaletteColumnCount(float availableWidth)
    {
        float tileStride = PaletteTileSize + PaletteTileGap;
        return Mathf.Max(1, Mathf.FloorToInt((availableWidth + PaletteTileGap) / tileStride));
    }

    internal static NoryangjinMapToolPaletteClickAction GetPaletteTileClickAction(int clickCount, int mouseButton)
    {
        return mouseButton == 0 && clickCount >= 2
            ? NoryangjinMapToolPaletteClickAction.SelectPrefabAsset
            : NoryangjinMapToolPaletteClickAction.SelectInMapTool;
    }

    internal static NoryangjinMapToolPaletteClickAction GetPaletteLabelClickAction(int clickCount, int mouseButton)
    {
        return mouseButton == 0 && clickCount >= 2
            ? NoryangjinMapToolPaletteClickAction.RenameDisplayName
            : NoryangjinMapToolPaletteClickAction.SelectInMapTool;
    }

    internal static bool ShouldEditPaletteFootprintBadge(int clickCount, int mouseButton)
    {
        return mouseButton == 0 && clickCount >= 2;
    }

    internal static string NormalizePaletteDisplayName(string displayName)
    {
        string normalized = (displayName ?? string.Empty).Trim();
        return normalized.Length > 8 ? normalized[..8] : normalized;
    }

    internal static bool LooksLikeBrokenKoreanText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        return text.Contains('?') || text.Contains('�');
    }

    internal static bool IsEmptyPaletteItemPath(string prefabPath)
    {
        return string.Equals(prefabPath, EmptyPaletteItemPrefabPath, StringComparison.Ordinal);
    }

    internal static bool IsSelectPaletteItemPath(string prefabPath)
    {
        return string.Equals(prefabPath, SelectPaletteItemPrefabPath, StringComparison.Ordinal);
    }

    internal static bool IsClearSelectionPaletteItemPath(string prefabPath)
    {
        return string.Equals(prefabPath, ClearSelectionPaletteItemPrefabPath, StringComparison.Ordinal);
    }

    internal static bool IsLowCostWaterBackgroundPath(string prefabPath)
    {
        return string.Equals((prefabPath ?? string.Empty).Replace('\\', '/'), JhWaterPrefabPath, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsSeagullPerchPrefabPath(string prefabPath)
    {
        return string.Equals((prefabPath ?? string.Empty).Replace('\\', '/'), SeagullPerchPrefabPath, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsBackgroundOverlayObjectPrefabPath(string prefabPath)
    {
        string normalizedPath = (prefabPath ?? string.Empty).Replace('\\', '/');
        return normalizedPath.Contains("Floating_sea_buoy", StringComparison.OrdinalIgnoreCase) ||
               normalizedPath.Contains("Floating_wooden_plank", StringComparison.OrdinalIgnoreCase) ||
               normalizedPath.Contains("Harbor_fishing_boat", StringComparison.OrdinalIgnoreCase) ||
               normalizedPath.Contains("Fishing_boat_detail_kit", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsGridManagedPaletteItemPath(string prefabPath)
    {
        return !IsLowCostWaterBackgroundPath(prefabPath);
    }

    internal static bool ShouldShowPlacementPreview(string prefabPath)
    {
        return !string.IsNullOrEmpty(prefabPath) &&
               IsGridManagedPaletteItemPath(prefabPath) &&
               !IsEmptyPaletteItemPath(prefabPath) &&
               !IsSelectPaletteItemPath(prefabPath) &&
               !IsClearSelectionPaletteItemPath(prefabPath);
    }

    internal static Color GetPlacementPreviewTint(bool canPlace)
    {
        return canPlace
            ? new Color(0.1f, 1f, 0.25f, 0.68f)
            : new Color(1f, 0.08f, 0.06f, 0.68f);
    }

    internal static Color GetPlacementValidityFillColor(NoryangjinMapToolSceneGridCellState state)
    {
        return state switch
        {
            NoryangjinMapToolSceneGridCellState.Occupied => new Color(1f, 0.04f, 0.02f, 0.5f),
            NoryangjinMapToolSceneGridCellState.LastPlaced => new Color(0.08f, 0.35f, 1f, 0.34f),
            _ => new Color(0.35f, 1f, 0.18f, 0.25f)
        };
    }

    internal static bool ShouldDrawPlacementValidityFill(string prefabPath)
    {
        return ShouldShowPlacementPreview(prefabPath);
    }

    internal static Color BuildPlacementPreviewTransparentColor(Color sourceColor)
    {
        return new Color(sourceColor.r, sourceColor.g, sourceColor.b, PlacementPreviewAlpha);
    }

    internal static string GetSpecialPaletteIconText(string prefabPath)
    {
        if (IsSelectPaletteItemPath(prefabPath))
            return SelectPaletteItemIconText;
        if (IsClearSelectionPaletteItemPath(prefabPath))
            return ClearSelectionPaletteItemIconText;

        return string.Empty;
    }

    internal static NoryangjinMapToolSceneGridCellState GetSceneGridCellState(
        Vector2Int cell,
        ISet<Vector2Int> occupiedCells)
    {
        if (occupiedCells != null && occupiedCells.Contains(cell))
            return NoryangjinMapToolSceneGridCellState.Occupied;

        return NoryangjinMapToolSceneGridCellState.Empty;
    }

    internal static bool ShouldDrawSceneGridCellFill(Vector2Int cell, Vector2Int cursor)
    {
        return cell == cursor;
    }

    internal static bool ShouldApplyMapTool(bool mapToolEnabled)
    {
        return mapToolEnabled;
    }

    internal static string FormatPlacedObjectHeightLabel(float y)
    {
        return $"Y {y:0.00}";
    }

    internal static bool ShouldDrawPlacedObjectHeightLabel(string objectName, bool mapToolEnabled)
    {
        return mapToolEnabled && IsMapToolPlacedObjectName(objectName);
    }

    internal static bool TryBuildSlopeHeightLabelCells(
        string prefabPath,
        IReadOnlyList<Vector2Int> footprintCells,
        NoryangjinMapToolDirection placementDirection,
        out Vector2Int highCell,
        out Vector2Int lowCell)
    {
        highCell = default;
        lowCell = default;
        if (footprintCells == null || footprintCells.Count == 0)
            return false;

        bool uphill = IsKnownRoadPieceLabel(prefabPath, "Uphill");
        bool downhill = IsKnownRoadPieceLabel(prefabPath, "Downhill");
        if (!uphill && !downhill)
            return false;

        Vector2Int startCell;
        Vector2Int endCell;
        ResolveFootprintStartAndEndCells(footprintCells, placementDirection, out startCell, out endCell);
        if (uphill)
        {
            highCell = startCell;
            lowCell = endCell;
            return true;
        }

        lowCell = startCell;
        highCell = endCell;
        return true;
    }

    private static bool IsKnownRoadPieceLabel(string prefabPath, string label)
    {
        foreach (RoadPiece roadPiece in RoadPieces)
        {
            if (string.Equals(roadPiece.Label, label, StringComparison.Ordinal) &&
                string.Equals(roadPiece.PrefabPath, prefabPath, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static void ResolveFootprintStartAndEndCells(
        IReadOnlyList<Vector2Int> footprintCells,
        NoryangjinMapToolDirection placementDirection,
        out Vector2Int startCell,
        out Vector2Int endCell)
    {
        Vector2Int step = NoryangjinMapToolGridUtility.DirectionToStep(placementDirection);
        startCell = footprintCells[0];
        endCell = footprintCells[0];
        int startProjection = ProjectCellOnDirection(startCell, step);
        int endProjection = startProjection;

        for (int i = 1; i < footprintCells.Count; i++)
        {
            Vector2Int cell = footprintCells[i];
            int projection = ProjectCellOnDirection(cell, step);
            if (projection < startProjection)
            {
                startProjection = projection;
                startCell = cell;
            }

            if (projection > endProjection)
            {
                endProjection = projection;
                endCell = cell;
            }
        }
    }

    private static int ProjectCellOnDirection(Vector2Int cell, Vector2Int directionStep)
    {
        return cell.x * directionStep.x + cell.y * directionStep.y;
    }

    internal static bool ShouldDrawStableTopViewWorkGridOverlay(
        bool drawEnabled,
        bool topViewToggle,
        bool sceneViewOrthographic,
        Quaternion sceneViewRotation)
    {
        if (!drawEnabled)
            return false;
        if (topViewToggle)
            return true;

        return sceneViewOrthographic || IsTopOrthographicSceneView(sceneViewOrthographic, sceneViewRotation);
    }

    internal static bool IsTopOrthographicSceneView(bool sceneViewOrthographic, Quaternion sceneViewRotation)
    {
        if (!sceneViewOrthographic)
            return false;

        Vector3 forward = sceneViewRotation * Vector3.forward;
        return Vector3.Dot(forward.normalized, Vector3.down) >= TopSceneViewForwardDotThreshold;
    }

    internal static float BuildWorkGridBoundaryOffset(int boundaryIndex, float currentCellSize)
    {
        float normalizedCellSize = NoryangjinMapToolGridUtility.NormalizeCellSize(currentCellSize);
        return (boundaryIndex - 0.5f) * normalizedCellSize;
    }

    internal static float BuildWorkGridSubcellSize(float currentCellSize)
    {
        float normalizedCellSize = NoryangjinMapToolGridUtility.NormalizeCellSize(currentCellSize);
        return normalizedCellSize / WorkGridSubdivisionsPerCell;
    }

    internal static float BuildPlacementSnapCellSize(float currentCellSize, bool coarseSnap)
    {
        float normalizedCellSize = NoryangjinMapToolGridUtility.NormalizeCellSize(currentCellSize);
        return coarseSnap ? normalizedCellSize : BuildWorkGridSubcellSize(normalizedCellSize);
    }

    internal static Vector2Int BuildPlacementGridCell(
        Vector3 worldPosition,
        Vector3 currentOrigin,
        float currentCellSize,
        bool coarseSnap)
    {
        float normalizedCellSize = NoryangjinMapToolGridUtility.NormalizeCellSize(currentCellSize);
        float snapCellSize = BuildPlacementSnapCellSize(normalizedCellSize, coarseSnap);
        int x = Mathf.RoundToInt((worldPosition.x - currentOrigin.x) / snapCellSize);
        int z = Mathf.RoundToInt((worldPosition.z - currentOrigin.z) / snapCellSize);

        if (coarseSnap)
        {
            x *= WorkGridSubdivisionsPerCell;
            z *= WorkGridSubdivisionsPerCell;
        }

        return new Vector2Int(x, z);
    }

    internal static Vector2Int SnapPlacementGridCellToCoarseStep(Vector2Int fineCell, Vector2Int anchorCell)
    {
        return new Vector2Int(
            anchorCell.x + Mathf.RoundToInt((fineCell.x - anchorCell.x) / (float)WorkGridSubdivisionsPerCell) * WorkGridSubdivisionsPerCell,
            anchorCell.y + Mathf.RoundToInt((fineCell.y - anchorCell.y) / (float)WorkGridSubdivisionsPerCell) * WorkGridSubdivisionsPerCell);
    }

    internal static float BuildWorkGridSubdivisionOffset(int cellIndex, int subdivisionIndex, float currentCellSize)
    {
        float normalizedCellSize = NoryangjinMapToolGridUtility.NormalizeCellSize(currentCellSize);
        float clampedSubdivision = Mathf.Clamp(subdivisionIndex, 1, WorkGridSubdivisionsPerCell - 1);
        return BuildWorkGridBoundaryOffset(cellIndex, normalizedCellSize) +
               BuildWorkGridSubcellSize(normalizedCellSize) * clampedSubdivision;
    }

    internal static float BuildWorkGridSpan(float currentCellSize)
    {
        float normalizedCellSize = NoryangjinMapToolGridUtility.NormalizeCellSize(currentCellSize);
        return normalizedCellSize * (WorkGridExtent * 2f + 1f);
    }

    internal static float BuildWorkGridFloorSize(float currentCellSize)
    {
        return BuildWorkGridSpan(currentCellSize) + NoryangjinMapToolGridUtility.NormalizeCellSize(currentCellSize);
    }

    internal static float BuildSceneGridCellFillHalfExtent(float currentCellSize)
    {
        return NoryangjinMapToolGridUtility.NormalizeCellSize(currentCellSize) * 0.5f;
    }

    internal static float BuildSceneGridOverlayHeight(float currentPlacementHeight)
    {
        return currentPlacementHeight + WorkGridLineY;
    }

    internal static bool TryGetMapToolPlacedObjectGridPosition(string objectName, out Vector2Int cell)
    {
        cell = default;
        if (string.IsNullOrEmpty(objectName))
            return false;

        int zMarkerIndex = objectName.LastIndexOf("_Z", StringComparison.Ordinal);
        if (zMarkerIndex < 0)
            return false;

        int xMarkerIndex = objectName.LastIndexOf("_X", zMarkerIndex, StringComparison.Ordinal);
        if (xMarkerIndex < 0)
            return false;

        string xText = objectName.Substring(xMarkerIndex + 2, zMarkerIndex - xMarkerIndex - 2);
        string zText = objectName[(zMarkerIndex + 2)..];
        if (!int.TryParse(xText, out int x) || !int.TryParse(zText, out int z))
            return false;

        cell = new Vector2Int(x, z);
        return true;
    }

    internal static NoryangjinMapToolDirection DirectionFromMoveOffset(int offsetX, int offsetZ)
    {
        if (offsetX > 0)
            return NoryangjinMapToolDirection.East;
        if (offsetX < 0)
            return NoryangjinMapToolDirection.West;
        if (offsetZ < 0)
            return NoryangjinMapToolDirection.South;

        return NoryangjinMapToolDirection.North;
    }

    internal static bool IsMapToolPlacedObjectName(string objectName, int targetX, int targetZ)
    {
        string coordinateSuffix = $"_X{targetX:+00;-00;+00}_Z{targetZ:+00;-00;+00}";
        return objectName.EndsWith(coordinateSuffix, StringComparison.Ordinal);
    }

    internal static bool IsMapToolPlacedObjectName(string objectName)
    {
        return TryGetMapToolPlacedObjectGridPosition(objectName, out _);
    }

    internal static bool ShouldDeleteAllPlacedObjectsTarget(string objectName, string parentName)
    {
        return IsMapToolPlacementContainerName(parentName) || IsMapToolPlacedObjectName(objectName);
    }

    private static bool IsMapToolPlacementContainerName(string objectName)
    {
        return string.Equals(objectName, RoadParentName, StringComparison.Ordinal) ||
               string.Equals(objectName, PropParentName, StringComparison.Ordinal) ||
               string.Equals(objectName, WaterParentName, StringComparison.Ordinal);
    }

    internal static string BuildCursorCellObjectLabel(string prefabPath)
    {
        return string.IsNullOrEmpty(prefabPath) ? EmptyPaletteItemLabel : BuildPaletteLabel(prefabPath);
    }

    internal static bool SelectProjectAsset(string assetPath)
    {
        UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
        if (asset == null)
            return false;

        EditorUtility.FocusProjectWindow();
        Selection.objects = new[] { asset };
        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
        return true;
    }

    internal static bool OpenProjectAsset(string assetPath)
    {
        UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
        if (asset == null)
            return false;

        SelectProjectAsset(assetPath);
        bool opened = AssetDatabase.OpenAsset(asset);
        SelectProjectAsset(assetPath);
        return opened;
    }

    internal static string DirectionToKorean(NoryangjinMapToolDirection mapDirection)
    {
        return mapDirection switch
        {
            NoryangjinMapToolDirection.East => "동쪽",
            NoryangjinMapToolDirection.South => "남쪽",
            NoryangjinMapToolDirection.West => "서쪽",
            _ => "북쪽"
        };
    }

    internal static string[] FindMissingRoadPrefabPaths()
    {
        var missingPaths = new List<string>();
        foreach (RoadPiece roadPiece in RoadPieces)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(roadPiece.PrefabPath);
            if (prefab == null)
                missingPaths.Add(roadPiece.PrefabPath);

            foreach (RoadCompanion roadCompanion in roadPiece.Companions)
            {
                string companionPrefabPath = roadCompanion.PrefabPath;
                GameObject companionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(companionPrefabPath);
                if (companionPrefab == null)
                    missingPaths.Add(companionPrefabPath);
            }
        }

        return missingPaths.ToArray();
    }

    private List<PaletteItem> GetPaletteItems()
    {
        if (paletteItems != null)
            return paletteItems;

        paletteItems = new List<PaletteItem>();
        paletteItems.Add(new PaletteItem(
            EmptyPaletteItemLabel,
            EmptyPaletteItemPrefabPath,
            null,
            NoryangjinMapToolPaletteCategory.All,
            EmptyPaletteItemSortOrder));
        paletteItems.Add(new PaletteItem(
            SelectPaletteItemLabel,
            SelectPaletteItemPrefabPath,
            null,
            NoryangjinMapToolPaletteCategory.All,
            SelectPaletteItemSortOrder));
        paletteItems.Add(new PaletteItem(
            ClearSelectionPaletteItemLabel,
            ClearSelectionPaletteItemPrefabPath,
            null,
            NoryangjinMapToolPaletteCategory.All,
            ClearSelectionPaletteItemSortOrder));
        AddRoadPaletteItems(paletteItems);
        AddBuiltinBackgroundPaletteItems(paletteItems);

        var prefabGuids = new HashSet<string>(StringComparer.Ordinal);
        foreach (string palettePrefabRoot in PalettePrefabRoots)
        {
            if (!AssetDatabase.IsValidFolder(palettePrefabRoot))
                continue;

            foreach (string prefabGuid in AssetDatabase.FindAssets("t:Prefab", new[] { palettePrefabRoot }))
                prefabGuids.Add(prefabGuid);
        }

        foreach (string prefabGuid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
            if (string.IsNullOrEmpty(prefabPath))
                continue;

            string normalizedPath = prefabPath.Replace('\\', '/');
            if (HasPaletteItem(paletteItems, normalizedPath))
                continue;

            if (normalizedPath.Contains("/_old/", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!IsPalettePrefabPathAllowed(normalizedPath))
                continue;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                continue;

            paletteItems.Add(new PaletteItem(
                BuildPaletteDisplayLabel(prefabPath),
                prefabPath,
                prefab,
                CategorizePrefabPath(prefabPath),
                10));
        }

        paletteItems.Sort((left, right) =>
        {
            int sortOrderCompare = left.SortOrder.CompareTo(right.SortOrder);
            if (sortOrderCompare != 0)
                return sortOrderCompare;

            int categoryCompare = left.Category.CompareTo(right.Category);
            return categoryCompare != 0 ? categoryCompare : string.Compare(left.Label, right.Label, StringComparison.Ordinal);
        });

        return paletteItems;
    }

    private void AddRoadPaletteItems(List<PaletteItem> items)
    {
        foreach (RoadPiece roadPiece in RoadPieces)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(roadPiece.PrefabPath);
            if (prefab == null)
                continue;

            items.Add(new PaletteItem(
                BuildPaletteDisplayLabel(roadPiece.PrefabPath),
                roadPiece.PrefabPath,
                prefab,
                NoryangjinMapToolPaletteCategory.Road,
                RoadPaletteItemSortOrder));
        }
    }

    private void AddBuiltinBackgroundPaletteItems(List<PaletteItem> items)
    {
        GameObject waterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(JhWaterPrefabPath);
        if (waterPrefab == null)
            return;

        items.Add(new PaletteItem(
            BuildPaletteDisplayLabel(JhWaterPrefabPath),
            JhWaterPrefabPath,
            waterPrefab,
            NoryangjinMapToolPaletteCategory.Background,
            BuiltinBackgroundPaletteItemSortOrder));
    }

    private static bool HasPaletteItem(List<PaletteItem> items, string prefabPath)
    {
        foreach (PaletteItem item in items)
        {
            if (string.Equals(item.PrefabPath, prefabPath, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private string BuildPaletteDisplayLabel(string prefabPath)
    {
        string customLabel = NormalizePaletteDisplayName(GetPaletteDefaults().GetCustomLabel(prefabPath));
        return ResolvePaletteDisplayLabel(prefabPath, customLabel);
    }

    internal static string ResolvePaletteDisplayLabel(string prefabPath, string customLabel)
    {
        string normalizedCustomLabel = NormalizePaletteDisplayName(customLabel);
        return string.IsNullOrEmpty(normalizedCustomLabel) || LooksLikeBrokenKoreanText(normalizedCustomLabel)
            ? BuildPaletteLabel(prefabPath)
            : normalizedCustomLabel;
    }

    private readonly struct RoadPiece
    {
        public RoadPiece(
            string label,
            string koreanLabel,
            string prefabPath,
            NoryangjinMapToolRoadTurn turn,
            params RoadCompanion[] companions)
        {
            Label = label;
            KoreanLabel = koreanLabel;
            PrefabPath = prefabPath;
            Turn = turn;
            Companions = companions ?? Array.Empty<RoadCompanion>();
        }

        public string Label { get; }
        public string KoreanLabel { get; }
        public string PrefabPath { get; }
        public NoryangjinMapToolRoadTurn Turn { get; }
        public RoadCompanion[] Companions { get; }
        public Vector3[] CompanionLocalPositions => Array.ConvertAll(Companions, companion => companion.LocalPosition);
        public string[] CompanionPrefabPaths => Array.ConvertAll(Companions, companion => companion.PrefabPath);
    }

    private readonly struct RoadCompanion
    {
        public RoadCompanion(string prefabPath)
            : this(prefabPath, Vector3.zero, Vector3.zero, Vector3.one)
        {
        }

        public RoadCompanion(string prefabPath, Vector3 localPosition)
            : this(prefabPath, localPosition, Vector3.zero, Vector3.one)
        {
        }

        public RoadCompanion(string prefabPath, Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale)
        {
            PrefabPath = prefabPath;
            LocalPosition = localPosition;
            LocalEulerAngles = localEulerAngles;
            LocalScale = localScale;
        }

        public string PrefabPath { get; }
        public Vector3 LocalPosition { get; }
        public Vector3 LocalEulerAngles { get; }
        public Vector3 LocalScale { get; }
    }

    private readonly struct PaletteItem
    {
        public PaletteItem(
            string label,
            string prefabPath,
            GameObject prefab,
            NoryangjinMapToolPaletteCategory category,
            int sortOrder)
        {
            Label = label;
            PrefabPath = prefabPath;
            Prefab = prefab;
            Category = category;
            SortOrder = sortOrder;
        }

        public string Label { get; }
        public string PrefabPath { get; }
        public GameObject Prefab { get; }
        public NoryangjinMapToolPaletteCategory Category { get; }
        public int SortOrder { get; }
    }
}
#endif
