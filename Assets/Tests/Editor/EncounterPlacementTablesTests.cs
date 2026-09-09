#if UNITY_EDITOR
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;

public sealed class EncounterPlacementTablesTests
{
    [Test]
    public void Workbook_ContainsTheLatest76PlacementsAndTheirRoles()
    {
        using var stream = GameDataWorkbook.OpenRead("Data.xlsx");
        var rows = EncounterPlacementTables.Read(stream);
        Assert.That(rows.Count, Is.EqualTo(76));
        Assert.That(rows.Count(r => r.kind == "적 배치"), Is.EqualTo(27));
        Assert.That(rows.Count(r => r.kind == "보너스 배치"), Is.EqualTo(25));
        Assert.That(rows.Count(r => r.kind == "기믹 배치"), Is.EqualTo(24));
        Assert.That(rows.Count(r => r.kind == "적 배치" && r.mode == EnemyEventMode.PatrolBetweenStartAndTarget), Is.EqualTo(5));
        Assert.That(rows.Count(r => r.kind == "적 배치" && r.mode == EnemyEventMode.AmbushMoveThenShoot), Is.EqualTo(5));
        Assert.That(rows.Single(r => r.id.Contains("T161")).kind, Is.EqualTo("보너스 배치"));
        Assert.That(rows.Where(r => r.kind == "적 배치" && r.mode == EnemyEventMode.AmbushMoveThenShoot).All(r => r.activationLead == 44 && r.throwDelay == .8f), Is.True);
    }

    [Test]
    public void Schema_ValidatesTheWholeProtectedWorkbook()
        => GameDataWorkbookSchema.Validate(File.ReadAllBytes(GameDataWorkbook.GetEditorSourceAbsolutePath()));

    [Test]
    public void DuplicateIds_AreRejectedBeforeAnySceneMutation()
    {
        var row = new EncounterPlacementRow { scene = "SR18", id = "E01", kind = "적 배치" };
        Assert.Throws<InvalidDataException>(() => EncounterPlacementTables.ValidateRows(new[] { row, row }));
    }

    [TestCase(-1f)]
    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    public void InvalidSpeed_IsRejected(float speed)
    {
        var row = new EncounterPlacementRow { scene = "SR18", id = "E01", kind = "적 배치", moveSpeed = speed };
        Assert.Throws<InvalidDataException>(() => EncounterPlacementTables.ValidateRows(new[] { row }));
    }

    [Test]
    public void Movement_RequiresPositiveSpeedAndDistance()
    {
        var row = new EncounterPlacementRow { scene = "SR18", id = "E01", kind = "적 배치", mode = EnemyEventMode.AmbushMoveThenShoot, throwSpeed = 14 };
        Assert.Throws<InvalidDataException>(() => EncounterPlacementTables.ValidateRows(new[] { row }));
    }

    [Test]
    public void UnknownMode_IsNotSilentlyConvertedToAttack()
        => Assert.Throws<InvalidDataException>(() => EncounterPlacementTables.ParseMode("왕복 오타"));
}
#endif
