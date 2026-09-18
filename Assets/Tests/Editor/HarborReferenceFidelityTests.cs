using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class HarborReferenceFidelityTests
{
    [TestCase("Noryangjin_MapTool_Mode_SR18")]
    [TestCase("HighWay")]
    [TestCase("RestStop")]
    public void LobbyHasDisplayTypographyAndSeparatedGesture(string name)
    {
        string path="Assets/ShooterSurvival/Scenes/Tools/"+name+".unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try{
            var canvas=scene.GetRootGameObjects().Single(g=>g.name=="Canvas").transform;
            var hint=canvas.Find("UI/Main/Center/StartHint");var title=hint.Find("Title").GetComponent<TMP_Text>();
            Assert.That(AssetDatabase.GetAssetPath(title.font),Is.EqualTo(HarborGameUIInstaller.RoundedFontPath));
            Assert.That(title.font.atlasPadding,Is.GreaterThanOrEqualTo(20));Assert.That(title.fontSize,Is.GreaterThanOrEqualTo(80));
            var material=title.fontSharedMaterial;Assert.That(material.name,Is.EqualTo("Coastal_RoundedPrompt"));
            Assert.That(material.IsKeywordEnabled("OUTLINE_ON"),Is.True);Assert.That(material.GetFloat("_OutlineWidth"),Is.GreaterThan(.20f));
            Assert.That(material.GetFloat("_FaceDilate"),Is.GreaterThanOrEqualTo(material.GetFloat("_OutlineWidth")),"Preserve white glyph interiors as the SDF stroke widens");
            Assert.That(material.GetTexture("_MainTex")==title.font.atlasTextures[0],Is.True);
            var hand=hint.Find("Finger").GetComponent<UnityEngine.UI.Image>();Assert.That(hand.sprite.name,Is.EqualTo("GestureHand"));
            foreach(string direction in new[]{"Left","Right"}){var arrow=hint.Find(direction).GetComponent<RectTransform>();Assert.That(arrow.gameObject.activeSelf,Is.True);Assert.That(arrow.anchorMin.y,Is.GreaterThan(hand.rectTransform.anchorMax.y));}
            Assert.That(hint.GetComponent<HarborSwipeHint>().amplitude,Is.GreaterThanOrEqualTo(50));
            foreach(var label in canvas.Find("UI/Main/Bottom").GetComponentsInChildren<TMP_Text>())Assert.That(label.fontSize,Is.GreaterThanOrEqualTo(60));
            if(name=="Noryangjin_MapTool_Mode_SR18"){
                var map=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform;var merchant=map.GetComponentInChildren<HarborMerchantGreeting>(true);
                Assert.That(merchant.transform.localScale.x,Is.EqualTo(1.8f).Within(.01));Assert.That(merchant.transform.position.x,Is.GreaterThan(50));
                var entrance=map.Find("Roads/HarborOpeningPier_03");Assert.That(entrance.GetComponent<MeshFilter>().sharedMesh,Is.SameAs(entrance.GetComponent<MeshCollider>().sharedMesh));
                Physics.SyncTransforms();var floors=map.Find("Roads").GetComponentsInChildren<MeshCollider>(true);
                foreach(float x in new[]{49f,52.7f,55f,58f,61f})Assert.That(floors.Any(c=>c.Raycast(new Ray(new Vector3(x,2,-115.11f),Vector3.down),out var hit,3)),Is.True,"Main pier support at "+x);
                Assert.That(floors.Any(c=>c.Raycast(new Ray(merchant.transform.position+Vector3.up*2,Vector3.down),out var hit,3)&&Mathf.Abs(hit.point.y-merchant.transform.position.y)<.2f),Is.True,"Merchant stool rests on connected deck");
            }
        }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
    }
}
