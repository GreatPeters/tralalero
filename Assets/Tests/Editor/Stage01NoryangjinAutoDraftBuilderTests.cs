using System.Globalization;
using System.IO;
using NUnit.Framework;

public class Stage01NoryangjinAutoDraftBuilderTests
{
    private const string ScenePath = "Assets/ShooterSurvival/Scenes/Generated/Stage01_Noryangjin_AutoDraft.unity";

    [Test]
    public void BuildScene_UsesContinuousDeckInsteadOfRoadPrefabRows()
    {
        Stage01NoryangjinAutoDraftBuilder.BuildScene();

        string sceneYaml = File.ReadAllText(ScenePath);

        Assert.That(sceneYaml, Does.Contain("m_Name: Stage01_1_Continuous_Pier_Deck"));
        Assert.That(sceneYaml, Does.Contain("m_Name: Deck_Plank_Row_00_Lane_00"));
        Assert.That(sceneYaml, Does.Contain("m_Name: Deck_Plank_Row_10_Lane_06"));
        Assert.That(sceneYaml, Does.Contain("m_Name: Deck_Cross_Seam_00"));
        Assert.That(sceneYaml, Does.Not.Contain("046_stage01_1_wet_pier_floor"));
    }

    [Test]
    public void BuildScene_RestoresMarketBuildingsAroundPier()
    {
        Stage01NoryangjinAutoDraftBuilder.BuildScene();

        string sceneYaml = File.ReadAllText(ScenePath);

        Assert.That(sceneYaml, Does.Contain("014_left_market_facade_near"));
        Assert.That(sceneYaml, Does.Contain("015_right_sashimi_restaurant_near"));
        Assert.That(sceneYaml, Does.Contain("016_left_seafood_display_mid"));
        Assert.That(sceneYaml, Does.Contain("043_right_market_awning_mid"));
        Assert.That(sceneYaml, Does.Contain("021_left_aquarium_row_far"));
        Assert.That(ReadFirstPositionXAfterName(sceneYaml, "014_left_market_facade_near"), Is.GreaterThan(-6.7f));
        Assert.That(ReadFirstPositionXAfterName(sceneYaml, "015_right_sashimi_restaurant_near"), Is.LessThan(6.7f));
    }

    [Test]
    public void BuildScene_DoesNotWriteVisibleSectionLabels()
    {
        Stage01NoryangjinAutoDraftBuilder.BuildScene();

        string sceneYaml = File.ReadAllText(ScenePath);

        Assert.That(sceneYaml, Does.Not.Contain("m_Text: Stage01_1 Straight Pier"));
    }

    [Test]
    public void BuildScene_KeepsPlayerAndPickupLineOnCenterLane()
    {
        Stage01NoryangjinAutoDraftBuilder.BuildScene();

        string sceneYaml = File.ReadAllText(ScenePath);

        Assert.That(ReadFirstPositionXAfterName(sceneYaml, "Player_Blue_Shark_Preview"), Is.EqualTo(0f).Within(0.01f));
        Assert.That(sceneYaml, Does.Contain("009_center_pickup_line_00"));
        Assert.That(sceneYaml, Does.Contain("009_center_pickup_line_08"));
    }

    [Test]
    public void BuildScene_UsesFlatWaterPlanesBesidePier()
    {
        Stage01NoryangjinAutoDraftBuilder.BuildScene();

        string sceneYaml = File.ReadAllText(ScenePath);

        Assert.That(sceneYaml, Does.Contain("m_Name: Harbor_Water_Left_Flat_Plane"));
        Assert.That(sceneYaml, Does.Contain("m_Name: Harbor_Water_Right_Flat_Plane"));
        Assert.That(sceneYaml, Does.Not.Contain("017_left_harbor_water_plane"));
    }

    private static float ReadFirstRotationXAfterName(string yaml, string objectName)
    {
        int nameIndex = yaml.IndexOf("value: " + objectName, System.StringComparison.Ordinal);
        Assert.That(nameIndex, Is.GreaterThanOrEqualTo(0), "Expected scene object was not written.");

        int rotationIndex = yaml.IndexOf("propertyPath: m_LocalRotation.x", nameIndex, System.StringComparison.Ordinal);
        Assert.That(rotationIndex, Is.GreaterThanOrEqualTo(0), "Expected scene object rotation was not written.");

        int valueIndex = yaml.IndexOf("value:", rotationIndex, System.StringComparison.Ordinal);
        Assert.That(valueIndex, Is.GreaterThanOrEqualTo(0), "Expected scene object rotation value was not written.");

        int valueStart = valueIndex + "value:".Length;
        int valueEnd = yaml.IndexOf('\n', valueStart);
        string value = yaml.Substring(valueStart, valueEnd - valueStart).Trim();

        return float.Parse(value, CultureInfo.InvariantCulture);
    }

    private static float ReadFirstPositionXAfterName(string yaml, string objectName)
    {
        int nameIndex = yaml.IndexOf("value: " + objectName, System.StringComparison.Ordinal);
        Assert.That(nameIndex, Is.GreaterThanOrEqualTo(0), "Expected scene object was not written.");

        int prefabBlockStart = yaml.LastIndexOf("PrefabInstance:", nameIndex, System.StringComparison.Ordinal);
        int positionIndex = -1;
        if (prefabBlockStart >= 0)
            positionIndex = yaml.IndexOf("propertyPath: m_LocalPosition.x", prefabBlockStart, nameIndex - prefabBlockStart, System.StringComparison.Ordinal);

        if (positionIndex < 0)
            positionIndex = yaml.IndexOf("propertyPath: m_LocalPosition.x", nameIndex, System.StringComparison.Ordinal);
        if (positionIndex < 0)
            positionIndex = yaml.IndexOf("m_LocalPosition: {x:", nameIndex, System.StringComparison.Ordinal);

        Assert.That(positionIndex, Is.GreaterThanOrEqualTo(0), "Expected scene object position was not written.");

        if (yaml.IndexOf("m_LocalPosition: {x:", nameIndex, System.StringComparison.Ordinal) == positionIndex)
        {
            int valueStart = positionIndex + "m_LocalPosition: {x:".Length;
            int valueEnd = yaml.IndexOf(',', valueStart);
            return float.Parse(yaml.Substring(valueStart, valueEnd - valueStart).Trim(), CultureInfo.InvariantCulture);
        }

        int valueIndex = yaml.IndexOf("value:", positionIndex, System.StringComparison.Ordinal);
        int valueStartOverride = valueIndex + "value:".Length;
        int valueEndOverride = yaml.IndexOf('\n', valueStartOverride);
        string value = yaml.Substring(valueStartOverride, valueEndOverride - valueStartOverride).Trim();

        return float.Parse(value, CultureInfo.InvariantCulture);
    }
}
