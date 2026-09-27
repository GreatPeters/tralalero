#if UNITY_EDITOR
using System.IO;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;

public sealed class EncounterPlacementTablesTests
{
    [Test]
    public void Workbook_ContainsThreeBalancedChaptersAndTheirRoles()
    {
        using var stream = GameDataWorkbook.OpenRead("Data.xlsx");
        var all = EncounterPlacementTables.Read(stream);
        Assert.That(all.Count, Is.EqualTo(300));
        Assert.That(all.GroupBy(r => r.scene).Select(g => g.Count()), Is.EquivalentTo(new[] { 100, 100, 100 }));
        var rows=all.Where(r=>r.scene=="Noryangjin_MapTool_Mode_SR18").ToArray();
        Assert.That(rows.Length, Is.EqualTo(100));
        Assert.That(rows.Count(r => r.kind == "적 배치"), Is.EqualTo(50));
        Assert.That(rows.Count(r => r.kind == "보너스 배치"), Is.EqualTo(25));
        Assert.That(rows.Count(r => r.kind == "기믹 배치"), Is.EqualTo(25));
        Assert.That(rows.Count(r => r.kind == "적 배치" && r.mode == EnemyEventMode.PatrolBetweenStartAndTarget), Is.EqualTo(10));
        Assert.That(rows.Count(r => r.kind == "적 배치" && r.mode == EnemyEventMode.AmbushMoveThenShoot), Is.EqualTo(4));
        Assert.That(rows.Single(r => r.id.Contains("T161")).kind, Is.EqualTo("보너스 배치"));
        var throwers=rows.Where(r=>r.kind=="적 배치"&&r.id.Contains("FatMan")).ToArray();
        Assert.That(throwers.Length,Is.EqualTo(6));
        Assert.That(throwers.All(r=>r.mode==EnemyEventMode.Shoot&&r.moveSpeed==0&&r.moveDistance==0&&r.throwDelay==.62f),Is.True);
        Assert.That(rows.Where(r => r.kind == "적 배치" && r.mode == EnemyEventMode.AmbushMoveThenShoot).All(r => r.throwDelay == .45f), Is.True);
        Assert.That(rows.Single(r=>r.id=="SR18_L_E25_T293_Enemy_Woman_Right").enabled,Is.False);
        var earlyBoss=rows.Single(r=>r.id=="SR18_L_E06_T065_Enemy_YllowMan_Net_Right");
        Assert.That(earlyBoss.tier,Is.EqualTo(EnemyTier.Boss));
        Assert.That(earlyBoss.health,Is.GreaterThan(rows.Single(r=>r.id=="SR18_L_E06_T065_Enemy_YllowMan_Net").health*2));
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
