#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using IndianOceanAssets.ShooterSurvival;

public sealed class HarborFeedbackPresentationTests
{
    [TestCase("BonusGlow",true)]
    [TestCase("BonusPickupBurst",false)]
    public void RewardsUseBoundedGoldStarsWithoutSmoke(string name,bool looping)
    {
        var effect=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ShooterSurvival/Resources/HarborRefinement/"+name+".prefab");
        var particles=effect.GetComponentsInChildren<ParticleSystem>(true);
        Assert.That(particles.Length,Is.EqualTo(2));
        Assert.That(effect.GetComponentsInChildren<MonoBehaviour>(true),Is.Empty,"No inherited camera shake/effect drivers");
        foreach(var particle in particles){
            Assert.That(particle.main.loop,Is.EqualTo(looping));Assert.That(particle.main.maxParticles,Is.LessThanOrEqualTo(32));
            var color=particle.main.startColor.colorMin;Assert.That(color.r,Is.GreaterThan(color.g));Assert.That(color.g,Is.GreaterThan(color.b));
            Assert.That(particle.GetComponent<ParticleSystemRenderer>().sharedMaterial.name.ToLowerInvariant(),Does.Not.Contain("smoke"));
        }
        Assert.That(particles[0].GetComponent<ParticleSystemRenderer>().sharedMaterial.name,Does.Contain("star"));
    }

    [TestCase("Noryangjin_MapTool_Mode_SR18")]
    [TestCase("HighWay")]
    [TestCase("RestStop")]
    public void SettingsKeepRealBindingsAndReadableReferenceControls(string name)
    {
        string path="Assets/ShooterSurvival/Scenes/Tools/"+name+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try{
            var canvas=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CanvasScript>(true)).Single();
            var settings=canvas.settingsMenuUI.GetComponent<HarborSettingsPanel>();var panel=settings.transform.Find("Panel");
            Assert.That(panel.GetComponent<AspectRatioFitter>().aspectMode,Is.EqualTo(AspectRatioFitter.AspectMode.WidthControlsHeight));
            Assert.That(panel.Find("Title").GetComponent<TMP_Text>().fontSize,Is.GreaterThanOrEqualTo(70));
            Assert.That(panel.Find("Sound/Label").GetComponent<TMP_Text>().fontSize,Is.GreaterThanOrEqualTo(50));
            Assert.That(settings.volume,Is.SameAs(canvas.volumeSlider));Assert.That(settings.sensitivity,Is.SameAs(canvas.sensitivitySlider));
            Assert.That(settings.volume.handleRect.sizeDelta.x,Is.GreaterThanOrEqualTo(70));
            Assert.That(settings.soundKnob.GetComponent<Image>().sprite.name,Is.EqualTo("SettingsKnob"));
            Assert.That(panel.Find("Close").GetComponent<Button>().onClick.GetPersistentMethodName(0),Is.EqualTo("Close"));
            Assert.That(panel.Find("RunActions/Retry").GetComponent<Button>().onClick.GetPersistentMethodName(0),Is.EqualTo("LoadGame"));
        }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
    }
}
#endif
