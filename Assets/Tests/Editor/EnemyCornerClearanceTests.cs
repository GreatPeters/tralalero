#if UNITY_EDITOR
using System.IO;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEngine;

public sealed class EnemyCornerClearanceTests
{
    [TestCase(0)] [TestCase(90)] [TestCase(180)] [TestCase(270)]
    public void TriggerAndMovingBody_RespectTheOutgoingRoute(int yaw)
    {
        var go = new GameObject("Clearance Test");
        try
        {
            var clearance = go.AddComponent<EnemyCornerClearance>();
            var direction = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            var corner = new Vector3(10, 12, 30); clearance.Configure(corner, direction);
            var safeGate = clearance.ConstrainTrigger(corner + direction * 4);
            Assert.That(clearance.DistanceAfterCorner(safeGate), Is.EqualTo(20).Within(.001f));
            Assert.That(safeGate.y, Is.EqualTo(12));
            Assert.DoesNotThrow(() => clearance.ValidateMovement(corner + direction * 28, corner + direction * 36));
            Assert.Throws<InvalidDataException>(() => clearance.ValidateMovement(corner + direction * 35, corner + direction * 27));
            var far = corner + direction * 50;
            Assert.That(Vector3.Distance(clearance.ConstrainTrigger(far), far), Is.LessThan(.001f));
        }
        finally { Object.DestroyImmediate(go); }
    }
}
#endif
