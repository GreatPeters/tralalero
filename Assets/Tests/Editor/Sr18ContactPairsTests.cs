#if UNITY_EDITOR
using System;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class Sr18ContactPairsTests
{
    [Test]
    public void NormalPairs_BlockEveryPlayerLaneAndOpenAfterOneSideDies()
    {
        WithScene((scene, map) =>
        {
            var player = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PlayerScript>(true)).Single();
            var playerCollider = player.GetComponent<Collider>();
            Assert.That(playerCollider, Is.Not.Null);
            foreach (int index in new[] { 2, 6 })
            {
                var members = map.Find("Enemies").GetComponentsInChildren<EnemyEventController>()
                    .Where(e => e.name.StartsWith($"SR18_L_E{index:00}_T")).OrderBy(e => e.name).ToArray();
                Assert.That(members.Length, Is.EqualTo(2));
                Assert.That(members.All(e => e.EventMode == EnemyEventMode.AttackLoop), Is.True);
                Vector3 center = (members[0].transform.position + members[1].transform.position) * .5f;
                Vector3 right = members[0].transform.right;
                Quaternion rotation = members[0].transform.rotation;
                bool Hits(EnemyEventController enemy, float lane) => Physics.ComputePenetration(
                    playerCollider, center + right * lane, rotation, enemy.GetComponent<CapsuleCollider>(),
                    enemy.transform.position, enemy.transform.rotation, out _, out _);
                for (int i = 0; i <= 80; i++)
                {
                    float lane = -2f + i * .05f;
                    Assert.That(Hits(members[0], lane) || Hits(members[1], lane), Is.True, $"E{index} has a bypass at lane{lane}");
                }
                Assert.That(Hits(members[1], -2), Is.False, "Defeating the left member opens the left lane");
                Assert.That(Hits(members[0], 2), Is.False, "Defeating the right member opens the right lane");
                var rows = EncounterPlacementTables.Rows.Where(r => r.kind == "적 배치" && r.id.StartsWith($"SR18_L_E{index:00}_T")).ToArray();
                Assert.That(rows.Length, Is.EqualTo(2));
                Assert.That(rows.All(r => r.tier == EnemyTier.Normal), Is.True);
                Assert.That(rows[0].damage, Is.EqualTo(rows[1].damage));
                Assert.That(rows[0].health, Is.EqualTo(rows[1].health));
            }
        });
    }

    [Test]
    public void FatManFootprints_AreCenteredOnTheirRoadAndLampsRemainHazards()
    {
        WithScene((scene, map) =>
        {
            var fats = map.Find("Enemies").GetComponentsInChildren<EnemyEventController>().Where(e => e.name.Contains("FatMan")).ToArray();
            Assert.That(fats.Length, Is.EqualTo(3));
            foreach (var enemy in fats)
            {
                Assert.That(enemy.AmbushEntrySide, Is.EqualTo(0));
                Assert.That(Mathf.Abs(Vector3.Dot(enemy.transform.position - enemy.TargetPoint.position, enemy.transform.right)), Is.LessThan(.01));
                Assert.That(enemy.transform.position.y, Is.EqualTo(enemy.TargetPoint.position.y).Within(.01));
                foreach (var renderer in enemy.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    Vector3 side = enemy.transform.right;
                    float halfWidth = Vector3.Dot(renderer.bounds.extents, new Vector3(Mathf.Abs(side.x), Mathf.Abs(side.y), Mathf.Abs(side.z)));
                    float offset = Mathf.Abs(Vector3.Dot(renderer.bounds.center - enemy.transform.position, side));
                    Assert.That(halfWidth + offset, Is.LessThan(3.15f), enemy.name + " extends beyond the bridge side");
                }
            }
            var lamps = map.Find("Props").Cast<Transform>().Where(t => t.name.StartsWith("SR18_L_G"))
                .SelectMany(t => t.GetComponentsInChildren<ObstacleStats>()).Where(o => o.enabled && o.obstaclePattern == ObstaclePattern.Light).ToArray();
            Assert.That(lamps.Length, Is.EqualTo(8));
            Assert.That(lamps.All(l => l.canBeShotDown), Is.True, "Every authored pole can now be toppled by shooting");
            Assert.That(lamps.All(l => l.value == 50 && l.GetComponents<Collider>().All(c => c.enabled)), Is.True);
        });
    }

    private static void WithScene(Action<Scene, Transform> test)
    {
        const string path = "Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity";
        var scene = SceneManager.GetSceneByPath(path); bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try { test(scene, scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform); }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }
}
#endif
