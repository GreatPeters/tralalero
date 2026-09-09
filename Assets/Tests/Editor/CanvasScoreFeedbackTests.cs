#if UNITY_EDITOR
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using Object=UnityEngine.Object;

public sealed class CanvasScoreFeedbackTests
{
    [Test] public void ScoreWithoutAnimatorController_UpdatesTextWithoutWarning()
    {
        var canvasObject=new GameObject("Canvas test");var playerObject=new GameObject("Player test");var labelObject=new GameObject("Label",typeof(RectTransform));
        bool warned=false;
        Application.LogCallback observe=(message,stack,type)=>{if(type==LogType.Warning&&message.Contains("AnimatorController"))warned=true;};
        Application.logMessageReceived+=observe;
        try
        {
            var canvas=canvasObject.AddComponent<CanvasScript>();var player=playerObject.AddComponent<PlayerScript>();player.playerScore=1;
            var label=labelObject.AddComponent<TextMeshProUGUI>();var animator=labelObject.AddComponent<Animator>();
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(CanvasScript).GetField("playerScript",flags).SetValue(canvas,player);
            typeof(CanvasScript).GetField("playerScoreText",flags).SetValue(canvas,label);
            typeof(CanvasScript).GetField("scorePopAnimator",flags).SetValue(canvas,animator);
            typeof(CanvasScript).GetMethod("UpdateScore",flags).Invoke(canvas,null);
            Assert.That(label.text,Is.EqualTo("1"));Assert.That(warned,Is.False);
        }
        finally{Application.logMessageReceived-=observe;Object.DestroyImmediate(canvasObject);Object.DestroyImmediate(playerObject);Object.DestroyImmediate(labelObject);}
    }
}
#endif
