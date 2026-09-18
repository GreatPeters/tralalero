using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEngine;

public sealed class OilSteeringEffectTests
{
    [Test] public void OverlappingOil_RefreshesWithoutStackingOrChangingRoute()
    {
        var go=new GameObject("Oil effect player");
        try
        {
            var player=go.AddComponent<PlayerScript>();player.moveSensitivity_Devision=110;
            go.transform.SetPositionAndRotation(new Vector3(3,2,9),Quaternion.Euler(0,90,0));
            var effect=go.AddComponent<OilSteeringEffect>();effect.Apply(player,2);effect.Apply(player,3);
            Assert.That(player.moveSensitivity_Devision,Is.EqualTo(200).Within(.01f));
            Assert.That(go.transform.position,Is.EqualTo(new Vector3(3,2,9)));
            Assert.That(Quaternion.Angle(go.transform.rotation,Quaternion.Euler(0,90,0)),Is.LessThan(.01f));
            effect.Clear();Assert.That(player.moveSensitivity_Devision,Is.EqualTo(110));
        }
        finally{Object.DestroyImmediate(go);}
    }
}
