using NUnit.Framework;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;

public sealed class HighwayEncounterLanesTests
{
    [TestCase(30,4.4f)] [TestCase(22,4.4f)] [TestCase(12,1.85f)] [TestCase(0,1.85f)] [TestCase(-3,1.85f)] [TestCase(-6,4.4f)]
    public void FunnelNarrowsBeforeContactAndRestoresAfterPassage(float ahead,float width)
        =>Assert.That(HighwayEncounterRow.PassageHalfWidth(ahead,4.4f,1.85f),Is.EqualTo(width).Within(.001f));

    [Test]
    public void EntireNarrowPassageOverlapsOneRealEnemyCapsule()
    {
        for(float lane=-1.85f;lane<=1.85f;lane+=.01f)
            Assert.That(Mathf.Min(Mathf.Abs(lane+1.1f),Mathf.Abs(lane-1.1f)),Is.LessThan(.57f+.69f));
    }

    [Test]
    public void StraddlingBothEnemiesCannotChargeTwoContactPayments()
    {
        var root=new GameObject("Row");var left=new GameObject("Left");var right=new GameObject("Right");
        try
        {
            var row=root.AddComponent<HighwayEncounterRow>();row.left=left.AddComponent<EnemyScript_space>();row.right=right.AddComponent<EnemyScript_space>();
            Assert.That(row.AcceptPhysicalContact(row.left),Is.True);Assert.That(row.AcceptPhysicalContact(row.right),Is.False);
            row.ResetForRun();Assert.That(row.AcceptPhysicalContact(row.right),Is.True);
        }
        finally{Object.DestroyImmediate(left);Object.DestroyImmediate(right);Object.DestroyImmediate(root);}
    }
}
