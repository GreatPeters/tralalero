if(!UnityEditor.EditorApplication.isPlaying||!IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning)throw new Exception("Start normal run first");
var p=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.PlayerScript>();
var field=p.GetType().GetField("maxHealthWithUpgrades",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
field.SetValue(p,10000000f);p.currentHealth=10000000f;
return new{testOnlyHealth=10000000,normalAttackUnchanged=p.currentDamage,normalSpeedUnchanged=p.ForwardMoveSpeed,reason="Allow continuous full-stage interaction verification; not a balance test. Nothing saved to workbook or upgrades."};
