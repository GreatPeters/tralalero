#if UNITY_EDITOR
using System;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class Sr18OpeningRhythmTests
{
    [Test]
    public void Opening_OffersReachableWeavesAndReadableRewardApproaches()
    {
        const string path = "Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity";
        var scene = SceneManager.GetSceneByPath(path);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try
        {
            var map = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform;
            Transform G(int index) => map.Find("Props").Cast<Transform>().Single(t => t.name.StartsWith($"SR18_L_G{index:00}_T"));
            foreach (int index in new[] { 1, 4, 7 })
            {
                var group = G(index);
                var center = group.position - group.right * (index % 2 == 0 ? .8f : -.8f);
                var parts = group.GetComponentsInChildren<ObstacleStats>().OrderBy(p => p.name).ToArray();
                Assert.That(parts.Length, Is.EqualTo(3));
                var sides = parts.Select(p => Vector3.Dot(p.transform.position - center, group.right)).ToArray();
                Assert.That(sides.Min(), Is.LessThan(-1f));
                Assert.That(sides.Max(), Is.GreaterThan(1f), "The group must require a lane change");
                for (int i = 0; i < parts.Length; i++)
                {
                    if (i > 0)
                        Assert.That(Vector3.Dot(parts[i].transform.position - parts[i - 1].transform.position, group.forward),
                            Is.GreaterThanOrEqualTo(11.98f), "Allow1.5 seconds between centers at baseline speed8");
                    var box = parts[i].GetComponent<BoxCollider>();
                    Vector3 lane = parts[i].transform.position + group.right * (-Mathf.Sign(sides[i]) * 2f - sides[i]);
                    Vector3 local = box.transform.InverseTransformPoint(lane) - box.center;
                    Vector3 ext = box.size * .5f;
                    Vector3 closest = box.transform.TransformPoint(box.center + new Vector3(
                        Mathf.Clamp(local.x, -ext.x, ext.x), Mathf.Clamp(local.y, -ext.y, ext.y), Mathf.Clamp(local.z, -ext.z, ext.z)));
                    Assert.That(Vector3.Distance(lane, closest), Is.GreaterThan(.8f), "Player radius.69 plus margin");
                }
            }
            foreach (int index in new[] { 3, 6 })
            {
                var hole = G(index);
                var pair = map.Find("Bonuses").GetComponentsInChildren<BonusWallChoicePair>().Single(p => p.name.StartsWith($"SR18_L_B{index:00}_T"));
                Vector3 center = (pair.Left.transform.position + pair.Right.transform.position) * .5f;
                Vector3 delta = hole.position - center;
                Assert.That(Vector3.Dot(delta, hole.forward), Is.EqualTo(25).Within(.03), "About three seconds of approach keep the hole visible above the label and the altar off the plank seam");
                Assert.That(Mathf.Abs(Vector3.Dot(delta, hole.right)), Is.EqualTo(1.9f).Within(.03));
                Assert.That(pair.IsConfigured, Is.True);
                Assert.That(Vector3.Dot(pair.Right.transform.position - pair.Left.transform.position, hole.right), Is.EqualTo(3.8f).Within(.03));
            }
            var bucket4 = G(4);
            var ambush = map.Find("Enemies").GetComponentsInChildren<EnemyEventController>().Single(e => e.name.StartsWith("SR18_L_E05_T"));
            var gate = map.Find("Props").GetComponentsInChildren<EnemyEventActivationSpot>().Single(s => s.Targets.Contains(ambush));
            Assert.That(Vector3.Dot(gate.transform.position - bucket4.Find("Bucket_1").position, bucket4.forward), Is.InRange(0f, 8f));
            Assert.That(Vector3.Dot(bucket4.Find("Bucket_3").position - gate.transform.position, bucket4.forward), Is.InRange(16f, 24f));
            var fill = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Image>(true)).Single(i => i.name == "HealthFill");
            Assert.That(fill.type, Is.EqualTo(Image.Type.Filled));
            Assert.That(fill.sprite.border, Is.EqualTo(Vector4.zero), "A solid fill must not stretch the old nine-slice end caps");
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }
}
#endif
