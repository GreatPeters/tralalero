#if UNITY_EDITOR
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public sealed class NoryangjinLateRampEncounterTests
{
    [Test]
    public void FinalDescentLeavesTimeToShootBeforeEnemyContact()
    {
        const string path="Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity";
        var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try
        {
            var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;
            var enemy=map.Find("Enemies").GetComponentsInChildren<EnemyEventController>(true).Single(e=>e.name.StartsWith("SR18_L_E23_"));
            var level=map.GetComponentsInChildren<NoryangjinTurnSpot>(true).Single(t=>t.name=="SR18_Slope_08_Level");
            Assert.That(level.transform.position.x-enemy.transform.position.x,Is.GreaterThanOrEqualTo(20f));
            var spot=map.Find("Props").GetComponentsInChildren<EnemyEventActivationSpot>(true).Single(s=>s.Targets.Contains(enemy));
            Assert.That(spot.transform.position.x,Is.LessThan(level.transform.position.x));
            Assert.That(spot.transform.position.x-enemy.transform.position.x,Is.GreaterThanOrEqualTo(18f));
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
    }
}
#endif
