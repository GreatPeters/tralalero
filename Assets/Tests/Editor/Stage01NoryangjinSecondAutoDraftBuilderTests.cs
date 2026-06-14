using System.Globalization;
using System.IO;
using NUnit.Framework;
using UnityEngine;

public class Stage01NoryangjinSecondAutoDraftBuilderTests
{
    private const string ScenePath = "Assets/ShooterSurvival/Scenes/Generated/Stage01_2_Noryangjin_AutoDraft.unity";
    private const string PreviewPath = "output/stage01_2_noryangjin_autodraft_preview.png";
    private const string GeneratedMaterialPath = "Assets/ShooterSurvival/Materials/Generated";
    private const string UrpLitShaderGuid = "933532a4fcc9baf4fa0491de14d08ed7";
    private static string sceneYaml;

    [OneTimeSetUp]
    public void BuildSceneOnce()
    {
        if (NeedsGeneratedSceneRefresh())
            Stage01NoryangjinSecondAutoDraftBuilder.BuildScene();

        sceneYaml = File.ReadAllText(ScenePath);
    }

    private static bool NeedsGeneratedSceneRefresh()
    {
        if (!File.Exists(ScenePath) || !File.Exists(PreviewPath))
            return true;

        string yaml = File.ReadAllText(ScenePath);
        return !yaml.Contains("017_stage01_ocean_water_backdrop_center")
            || !yaml.Contains("027_left_upper_hanging_lamp_depth_04")
            || !yaml.Contains("Stage01_2_Distant_Noryangjin_Skyline_Tower_05");
    }

    [Test]
    public void BuildScene_UsesContinuousConceptPierWithRoadSurfaceSkin()
    {
        Assert.That(sceneYaml, Does.Contain("m_Name: Stage01_2_Noryangjin_WetMarketPier_ConceptDraft"));
        Assert.That(sceneYaml, Does.Contain("m_Name: Stage01_2_Continuous_Concept_Pier"));
        Assert.That(sceneYaml, Does.Contain("value: 046_road_surface_skin_near_00"));
        Assert.That(sceneYaml, Does.Contain("value: 047_road_surface_skin_curve_04"));
        Assert.That(sceneYaml, Does.Contain("value: 048_road_surface_skin_wide_foreground"));
        float roadSkinScaleX = ReadFirstScaleOverrideAfterName(sceneYaml, "046_road_surface_skin_near_00", "x");
        Assert.That(ReadFirstScaleOverrideAfterName(sceneYaml, "046_road_surface_skin_near_00", "y"), Is.GreaterThan(roadSkinScaleX * 1.8f));
        Assert.That(ReadFirstScaleOverrideAfterName(sceneYaml, "046_road_surface_skin_near_00", "z"), Is.LessThan(roadSkinScaleX * 0.35f));
        Assert.That(sceneYaml, Does.Contain("m_Name: Stage01_2_Pier_Plank_Row_00_Lane_00"));
        Assert.That(sceneYaml, Does.Contain("m_Name: Stage01_2_Pier_Plank_Row_11_Lane_06"));
        Assert.That(sceneYaml, Does.Contain("m_Name: Stage01_2_Pier_Foreground_Extension_Lane_00"));
        Assert.That(sceneYaml, Does.Contain("m_Name: Stage01_2_Pier_Foreground_Extension_Lane_06"));
        Assert.That(sceneYaml, Does.Contain("m_Name: Stage01_2_Pier_Cross_Seam_00"));
        Assert.That(sceneYaml, Does.Contain("m_Name: Stage01_2_Pier_Left_Edge_Beam_00"));
        Assert.That(sceneYaml, Does.Contain("m_Name: Stage01_2_Pier_Right_Edge_Beam_00"));
        Assert.That(sceneYaml, Does.Contain("m_Name: Stage01_2_Road_Playable_Collider"));
        Assert.That(sceneYaml, Does.Not.Contain("stage01_2_s_curve_pier_mid"));
        Assert.That(sceneYaml, Does.Not.Contain("stage01_2_straight_pier_intro"));
        Assert.That(sceneYaml, Does.Not.Contain("stage01_2_straight_pier_exit"));
        Assert.That(sceneYaml, Does.Not.Contain("046_stage01_2_wet_straight_pier_road_module_12"));
        Assert.That(sceneYaml, Does.Not.Contain("m_Name: Stage01_2_Continuous_Wet_Runner_Surface"));
        Assert.That(sceneYaml, Does.Not.Contain("m_Name: Stage01_2_Center_Play_Lane_Gloss"));
        Assert.That(sceneYaml, Does.Not.Contain("m_Name: Stage01_2_Wet_Road_Plate_00"));
        Assert.That(sceneYaml, Does.Not.Contain("m_Name: Stage01_2_Wet_Road_Bolt_Row_00_L"));
        Assert.That(ReadFirstPositionXAfterName(sceneYaml, "Stage01_2_Pier_Plank_Row_04_Lane_03"), Is.GreaterThan(0.5f));
        Assert.That(ReadFirstPositionXAfterName(sceneYaml, "Stage01_2_Pier_Plank_Row_07_Lane_03"), Is.LessThan(-0.5f));
        Assert.That(ReadFirstPositionZAfterName(sceneYaml, "Stage01_2_Pier_Foreground_Extension_Lane_03"), Is.LessThan(-2.0f));
    }

    [Test]
    public void BuildScene_ComposesFishMarketAndHarborFromStage01NoryangjinPrefabs()
    {
        Assert.That(sceneYaml, Does.Contain("m_Name: Stage01_2_Stage01_Noryangjin_Source_Prefab_Set_Dressing"));
        Assert.That(sceneYaml, Does.Contain("m_Name: Harbor_Water_Left_Flat_Plane"));
        Assert.That(sceneYaml, Does.Contain("m_Name: Harbor_Water_Right_Flat_Plane"));
        Assert.That(sceneYaml, Does.Contain("m_Name: Harbor_Water_Back_Flat_Plane"));
        Assert.That(sceneYaml, Does.Contain("017_stage01_ocean_water_backdrop_center"));
        Assert.That(sceneYaml, Does.Contain("018_offset_fishing_boat_left_background"));
        Assert.That(sceneYaml, Does.Contain("018_offset_fishing_boat_right_background"));
        Assert.That(sceneYaml, Does.Contain("018_center_left_mast_boat_background"));
        Assert.That(sceneYaml, Does.Contain("019_distant_hillside_village_backdrop"));
        Assert.That(sceneYaml, Does.Contain("m_Name: Stage01_2_Distant_Noryangjin_Skyline_Tower_00"));
        Assert.That(sceneYaml, Does.Contain("m_Name: Stage01_2_Distant_Noryangjin_Skyline_Tower_05"));
        Assert.That(sceneYaml, Does.Contain("014_left_market_facade_near"));
        Assert.That(sceneYaml, Does.Contain("015_right_sashimi_restaurant_near"));
        Assert.That(sceneYaml, Does.Contain("016_left_seafood_display_mid"));
        Assert.That(sceneYaml, Does.Contain("043_right_market_awning_mid"));
        Assert.That(sceneYaml, Does.Contain("030_left_crab_aquarium_foreground"));
        Assert.That(sceneYaml, Does.Contain("031_right_octopus_aquarium_foreground"));
        Assert.That(sceneYaml, Does.Contain("027_left_warm_market_lamp_near"));
        Assert.That(sceneYaml, Does.Contain("036_left_foreground_utility_pole_frame"));
        Assert.That(sceneYaml, Does.Contain("036_right_foreground_utility_pole_frame"));
        Assert.That(sceneYaml, Does.Contain("027_left_upper_hanging_lamp_frame"));
        Assert.That(sceneYaml, Does.Contain("029_right_upper_fish_sign_frame"));
        Assert.That(sceneYaml, Does.Contain("036_left_mid_utility_pole_frame"));
        Assert.That(sceneYaml, Does.Contain("027_left_upper_hanging_lamp_depth_04"));
        Assert.That(sceneYaml, Does.Contain("027_right_upper_hanging_lamp_depth_06"));
        Assert.That(sceneYaml, Does.Contain("028_left_crab_mascot_market_sign"));
        Assert.That(sceneYaml, Does.Contain("029_right_fish_mascot_market_sign"));
        Assert.That(sceneYaml, Does.Not.Contain("m_Name: Stage01_2_Left_Blue_Awning_Canopy_00"));
        Assert.That(sceneYaml, Does.Not.Contain("m_Name: Stage01_2_Right_Striped_Awning_Canopy_00"));
        Assert.That(sceneYaml, Does.Not.Contain("m_Name: Stage01_2_Left_Market_Crop_Wall_00"));
        Assert.That(sceneYaml, Does.Not.Contain("m_Name: Stage01_2_Right_Market_Crop_Wall_00"));
        Assert.That(sceneYaml, Does.Not.Contain("m_Name: Stage01_2_String_Light_Line_00"));
        Assert.That(sceneYaml, Does.Not.Contain("m_Name: Stage01_2_Distant_Harbor_City_Backdrop"));
        Assert.That(ReadFirstPositionXAfterName(sceneYaml, "Harbor_Water_Left_Flat_Plane"), Is.EqualTo(-6.15f).Within(0.2f));
        Assert.That(ReadFirstPositionXAfterName(sceneYaml, "Harbor_Water_Right_Flat_Plane"), Is.EqualTo(6.15f).Within(0.2f));
        Assert.That(ReadFirstScaleXAfterName(sceneYaml, "Harbor_Water_Left_Flat_Plane"), Is.GreaterThan(1.2f));
        Assert.That(ReadFirstScaleXAfterName(sceneYaml, "Harbor_Water_Right_Flat_Plane"), Is.GreaterThan(1.2f));
        Assert.That(ReadFirstPositionXAfterName(sceneYaml, "019_distant_hillside_village_backdrop"), Is.LessThan(-9.5f));
        Assert.That(ReadFirstPositionYAfterName(sceneYaml, "027_left_warm_market_lamp_near"), Is.GreaterThan(1.2f));
        Assert.That(ReadFirstPositionYAfterName(sceneYaml, "041_left_flying_gull_open_harbor"), Is.GreaterThan(6f));
        Assert.That(ReadFirstScaleXOverrideAfterName(sceneYaml, "036_left_foreground_utility_pole_frame"), Is.GreaterThan(95f));
        Assert.That(ReadFirstPositionXAfterName(sceneYaml, "014_left_market_facade_near"), Is.EqualTo(-3.05f).Within(0.2f));
        Assert.That(ReadFirstPositionXAfterName(sceneYaml, "015_right_sashimi_restaurant_near"), Is.EqualTo(3.05f).Within(0.2f));
        Assert.That(ReadFirstScaleXOverrideAfterName(sceneYaml, "014_left_market_facade_near"), Is.GreaterThan(110f));
        Assert.That(ReadFirstScaleXOverrideAfterName(sceneYaml, "014_left_market_facade_near"), Is.LessThan(260f));
        Assert.That(ReadFirstScaleXOverrideAfterName(sceneYaml, "015_right_sashimi_restaurant_near"), Is.GreaterThan(150f));
        Assert.That(ReadFirstScaleXOverrideAfterName(sceneYaml, "015_right_sashimi_restaurant_near"), Is.LessThan(290f));
        Assert.That(ReadFirstScaleXOverrideAfterName(sceneYaml, "043_left_market_awning_foreground"), Is.GreaterThan(75f));
        Assert.That(ReadFirstScaleXOverrideAfterName(sceneYaml, "043_right_market_awning_mid"), Is.GreaterThan(55f));
        Assert.That(CountOccurrences(sceneYaml, "_market_"), Is.GreaterThanOrEqualTo(8));
    }

    [Test]
    public void BuildScene_UsesStage01PrefabPropsForForegroundDensity()
    {
        Assert.That(sceneYaml, Does.Contain("m_Name: Stage01_2_Stage01_Noryangjin_Source_Prefab_Set_Dressing"));
        Assert.That(sceneYaml, Does.Contain("035_left_foreground_anchor_prop"));
        Assert.That(sceneYaml, Does.Contain("034_right_foreground_fishing_net_pile"));
        Assert.That(sceneYaml, Does.Contain("039_left_ice_scatter_wet_market"));
        Assert.That(sceneYaml, Does.Contain("040_right_fish_scrap_scatter_wet_market"));
        Assert.That(sceneYaml, Does.Not.Contain("m_Name: Stage01_2_No_Magenta_Fallback_Materials_Applied"));
        Assert.That(sceneYaml, Does.Not.Contain("m_Name: Stage01_2_Left_Foreground_Anchor_Silhouette"));
        Assert.That(sceneYaml, Does.Not.Contain("m_Name: Stage01_2_Right_Foreground_Net_Pile_Silhouette"));
        Assert.That(sceneYaml, Does.Not.Contain("m_Name: Stage01_2_Left_Ice_Shards_Foreground_00"));
        Assert.That(sceneYaml, Does.Not.Contain("m_Name: Stage01_2_Right_Ice_Shards_Foreground_00"));
        Assert.That(sceneYaml, Does.Not.Contain("m_Name: Stage01_2_Harbor_Water_Grid_Hide_Left"));
        Assert.That(sceneYaml, Does.Not.Contain("m_Name: Stage01_2_Harbor_Water_Grid_Hide_Right"));
    }

    [Test]
    public void BuildScene_WritesStage01SecondGeneratedMaterialsAsUrpLit()
    {
        string[] materialPaths = Directory.GetFiles(GeneratedMaterialPath, "Stage01_2*.mat");
        Assert.That(materialPaths, Is.Not.Empty);

        foreach (string materialPath in materialPaths)
        {
            string materialYaml = File.ReadAllText(materialPath);
            Assert.That(materialYaml, Does.Contain(UrpLitShaderGuid), Path.GetFileName(materialPath));
            Assert.That(materialYaml, Does.Not.Contain("m_Shader: {fileID: 46, guid: 0000000000000000f000000000000000"), Path.GetFileName(materialPath));
        }

        Assert.That(sceneYaml, Does.Not.Contain("Stage01_2_Market_Blue_Paint"));
        Assert.That(sceneYaml, Does.Not.Contain("Stage01_2_Market_Awning_White"));
    }

    [Test]
    public void BuildScene_KeepsCenterLanePlayableWhileScalingForegroundPropsCloser()
    {
        Assert.That(sceneYaml, Does.Contain("m_Name: Stage01_2_Center_Gold_Coin_Line_00"));
        Assert.That(sceneYaml, Does.Contain("m_Name: Stage01_2_Center_Gold_Coin_Line_10"));
        Assert.That(sceneYaml, Does.Not.Contain("009_center_gold_pickup_line_00"));
        Assert.That(sceneYaml, Does.Contain("045_overhead_harbor_lane_signal"));
        Assert.That(sceneYaml, Does.Contain("044_right_edge_barricade_warning"));
        Assert.That(sceneYaml, Does.Contain("001_left_foreground_blue_crate_blocker"));
        Assert.That(sceneYaml, Does.Contain("002_right_foreground_styrofoam_box_blocker"));
        Assert.That(ReadFirstPositionXAfterName(sceneYaml, "Player_Blue_Shark_Preview"), Is.EqualTo(0f).Within(0.01f));
        Assert.That(ReadFirstPositionZAfterName(sceneYaml, "Player_Blue_Shark_Preview"), Is.GreaterThan(-3.6f));
        Assert.That(ReadFirstPositionXAfterName(sceneYaml, "Stage01_2_Center_Gold_Coin_Line_05"), Is.GreaterThan(0.45f));
        Assert.That(ReadFirstPositionXAfterName(sceneYaml, "001_left_foreground_blue_crate_blocker"), Is.EqualTo(-2.75f).Within(0.12f));
        Assert.That(ReadFirstPositionXAfterName(sceneYaml, "002_right_foreground_styrofoam_box_blocker"), Is.EqualTo(2.75f).Within(0.12f));
        Assert.That(ReadFirstScaleXOverrideAfterName(sceneYaml, "001_left_foreground_blue_crate_blocker"), Is.GreaterThan(0.85f));
    }

    [Test]
    public void BuildScene_UsesTighterRunnerPreviewCamera()
    {
        Assert.That(sceneYaml, Does.Contain("m_Name: Camera - Stage01_2 Runner 9x16 Preview"));
        Assert.That(sceneYaml, Does.Contain("field of view: 50"));
        Assert.That(ReadFirstPositionYAfterName(sceneYaml, "Camera - Stage01_2 Runner 9x16 Preview"), Is.LessThan(3.4f));
        Assert.That(ReadFirstPositionZAfterName(sceneYaml, "Camera - Stage01_2 Runner 9x16 Preview"), Is.LessThan(-8.0f));
    }

    [Test]
    public void BuildScene_WritesCameraPreviewForVisualReview()
    {
        Assert.That(File.Exists(PreviewPath), Is.True, "Expected the builder to write a 9:16 preview capture.");
        FileInfo fileInfo = new FileInfo(PreviewPath);
        Assert.That(fileInfo.Length, Is.GreaterThan(10000));
    }

    [Test]
    public void BuildScene_PreviewKeepsForegroundOnPierInsteadOfOpenWater()
    {
        Color32 leftBottom = ReadPreviewPixel(96, 56);
        Color32 rightBottom = ReadPreviewPixel(624, 56);

        Assert.That(LooksLikeOpenWater(leftBottom), Is.False, $"left bottom pixel was {leftBottom}");
        Assert.That(LooksLikeOpenWater(rightBottom), Is.False, $"right bottom pixel was {rightBottom}");
    }

    [Test]
    public void BuildScene_PreviewAvoidsBlankSkyDominatingUpperMidFrame()
    {
        Color32[] upperMidSamples =
        {
            ReadPreviewPixel(300, 940),
            ReadPreviewPixel(330, 940),
            ReadPreviewPixel(360, 940),
            ReadPreviewPixel(390, 940),
            ReadPreviewPixel(420, 940),
            ReadPreviewPixel(330, 900),
            ReadPreviewPixel(360, 900),
            ReadPreviewPixel(390, 900)
        };

        int blankSkySamples = 0;
        foreach (Color32 sample in upperMidSamples)
        {
            if (LooksLikeBlankSky(sample))
                blankSkySamples++;
        }

        Assert.That(blankSkySamples, Is.LessThan(upperMidSamples.Length / 2), $"upper mid samples had {blankSkySamples} blank sky-like pixels");
    }

    private static float ReadFirstPositionXAfterName(string yaml, string objectName)
    {
        return ReadFirstPositionComponentAfterName(yaml, objectName, "x");
    }

    private static float ReadFirstPositionYAfterName(string yaml, string objectName)
    {
        return ReadFirstPositionComponentAfterName(yaml, objectName, "y");
    }

    private static float ReadFirstPositionZAfterName(string yaml, string objectName)
    {
        return ReadFirstPositionComponentAfterName(yaml, objectName, "z");
    }

    private static float ReadFirstPositionComponentAfterName(string yaml, string objectName, string component)
    {
        int nameIndex = FindExactLineIndex(yaml, "value: " + objectName);
        bool prefabOverrideName = nameIndex >= 0;
        if (nameIndex < 0)
            nameIndex = FindExactLineIndex(yaml, "m_Name: " + objectName);

        Assert.That(nameIndex, Is.GreaterThanOrEqualTo(0), "Expected scene object was not written.");

        int positionIndex = -1;
        if (prefabOverrideName)
        {
            int prefabBlockStart = yaml.LastIndexOf("PrefabInstance:", nameIndex, System.StringComparison.Ordinal);
            int prefabBlockEnd = yaml.IndexOf("\n--- !u!", nameIndex, System.StringComparison.Ordinal);
            if (prefabBlockEnd < 0)
                prefabBlockEnd = yaml.Length;

            if (prefabBlockStart >= 0)
                positionIndex = yaml.IndexOf("propertyPath: m_LocalPosition." + component, prefabBlockStart, prefabBlockEnd - prefabBlockStart, System.StringComparison.Ordinal);
        }

        if (positionIndex < 0)
            positionIndex = yaml.IndexOf("m_LocalPosition: {x:", nameIndex, System.StringComparison.Ordinal);

        Assert.That(positionIndex, Is.GreaterThanOrEqualTo(0), "Expected scene object position was not written.");

        if (yaml.IndexOf("m_LocalPosition: {x:", nameIndex, System.StringComparison.Ordinal) == positionIndex)
        {
            int valueStart = yaml.IndexOf(component + ":", positionIndex, System.StringComparison.Ordinal);
            Assert.That(valueStart, Is.GreaterThanOrEqualTo(0), "Expected scene object position component was not written.");

            valueStart += (component + ":").Length;
            int valueEnd = component == "z"
                ? yaml.IndexOf('}', valueStart)
                : yaml.IndexOf(',', valueStart);
            return float.Parse(yaml.Substring(valueStart, valueEnd - valueStart).Trim(), CultureInfo.InvariantCulture);
        }

        int valueIndex = yaml.IndexOf("value:", positionIndex, System.StringComparison.Ordinal);
        int valueStartOverride = valueIndex + "value:".Length;
        int valueEndOverride = yaml.IndexOf('\n', valueStartOverride);
        string value = yaml.Substring(valueStartOverride, valueEndOverride - valueStartOverride).Trim();

        return float.Parse(value, CultureInfo.InvariantCulture);
    }

    private static float ReadFirstScaleXAfterName(string yaml, string objectName)
    {
        return ReadFirstPrimitiveScaleAfterName(yaml, objectName, "x:");
    }

    private static float ReadFirstScaleZAfterName(string yaml, string objectName)
    {
        return ReadFirstPrimitiveScaleAfterName(yaml, objectName, "z:");
    }

    private static float ReadFirstPrimitiveScaleAfterName(string yaml, string objectName, string componentMarker)
    {
        int nameIndex = FindExactLineIndex(yaml, "m_Name: " + objectName);
        Assert.That(nameIndex, Is.GreaterThanOrEqualTo(0), "Expected scene object was not written.");

        int scaleIndex = yaml.IndexOf("m_LocalScale: {x:", nameIndex, System.StringComparison.Ordinal);
        Assert.That(scaleIndex, Is.GreaterThanOrEqualTo(0), "Expected primitive scale was not written.");

        int valueStart = yaml.IndexOf(componentMarker, scaleIndex, System.StringComparison.Ordinal);
        Assert.That(valueStart, Is.GreaterThanOrEqualTo(0), "Expected scale component was not written.");

        valueStart += componentMarker.Length;
        int valueEnd = yaml.IndexOf(componentMarker == "z:" ? "}" : ",", valueStart, System.StringComparison.Ordinal);
        return float.Parse(yaml.Substring(valueStart, valueEnd - valueStart).Trim(), CultureInfo.InvariantCulture);
    }

    private static float ReadFirstScaleXOverrideAfterName(string yaml, string objectName)
    {
        return ReadFirstScaleOverrideAfterName(yaml, objectName, "x");
    }

    private static float ReadFirstScaleOverrideAfterName(string yaml, string objectName, string component)
    {
        int nameIndex = FindExactLineIndex(yaml, "value: " + objectName);
        Assert.That(nameIndex, Is.GreaterThanOrEqualTo(0), "Expected prefab object was not written.");

        int prefabBlockStart = yaml.LastIndexOf("PrefabInstance:", nameIndex, System.StringComparison.Ordinal);
        Assert.That(prefabBlockStart, Is.GreaterThanOrEqualTo(0), "Expected prefab block was not written.");

        int prefabBlockEnd = yaml.IndexOf("\n--- !u!", nameIndex, System.StringComparison.Ordinal);
        if (prefabBlockEnd < 0)
            prefabBlockEnd = yaml.Length;

        int scaleIndex = yaml.IndexOf("propertyPath: m_LocalScale." + component, prefabBlockStart, prefabBlockEnd - prefabBlockStart, System.StringComparison.Ordinal);
        Assert.That(scaleIndex, Is.GreaterThanOrEqualTo(0), "Expected prefab scale override was not written in the prefab block.");

        int valueIndex = yaml.IndexOf("value:", scaleIndex, System.StringComparison.Ordinal);
        int valueStart = valueIndex + "value:".Length;
        int valueEnd = yaml.IndexOf('\n', valueStart);
        return float.Parse(yaml.Substring(valueStart, valueEnd - valueStart).Trim(), CultureInfo.InvariantCulture);
    }

    private static Color32 ReadPreviewPixel(int x, int y)
    {
        byte[] bytes = File.ReadAllBytes(PreviewPath);
        var texture = new Texture2D(2, 2);
        ImageConversion.LoadImage(texture, bytes);
        Color32 pixel = texture.GetPixel(x, y);
        Object.DestroyImmediate(texture);
        return pixel;
    }

    private static bool LooksLikeOpenWater(Color32 color)
    {
        return color.b > 145 && color.g > 90 && color.r < 80;
    }

    private static bool LooksLikeBlankSky(Color32 color)
    {
        return color.b > 165 && color.g > 130 && color.r > 90;
    }

    private static int FindExactLineIndex(string yaml, string line)
    {
        int searchIndex = 0;
        while (searchIndex < yaml.Length)
        {
            int index = yaml.IndexOf(line, searchIndex, System.StringComparison.Ordinal);
            if (index < 0)
                return -1;

            int lineStart = index == 0 ? 0 : yaml.LastIndexOf('\n', index - 1) + 1;
            int lineEnd = yaml.IndexOf('\n', index);
            if (lineEnd < 0)
                lineEnd = yaml.Length;

            string currentLine = yaml.Substring(lineStart, lineEnd - lineStart).Trim();
            if (currentLine == line)
                return index;

            searchIndex = index + line.Length;
        }

        return -1;
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        int count = 0;
        int index = 0;
        while ((index = haystack.IndexOf(needle, index, System.StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }
}
