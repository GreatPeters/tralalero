if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
var backup=SessionState.GetString("Harbor.PolishBackup","");if(string.IsNullOrEmpty(backup))throw new Exception("Preserve baseline first");
const string coin="Assets/ShooterSurvival/Resources/VFX/GoldCoin.png";
if(!System.IO.File.Exists(coin)&&!AssetDatabase.CopyAsset("Assets/ShooterSurvival/UI/HarborWorkshop/Coin.png",coin))throw new Exception("Coin art copy failed");
var importer=(TextureImporter)AssetImporter.GetAtPath(coin);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.maxTextureSize=256;importer.SaveAndReimport();
int bodies=0,prefabs=0;
void Tune(GameObject root)
{
    foreach(var collider in root.GetComponentsInChildren<Collider>(true))
    {
        if(!collider.isTrigger || !(collider is CapsuleCollider || collider is SphereCollider || collider is BoxCollider))continue;
        if(collider.GetComponent<IndianOceanAssets.ShooterSurvival.EnemyScript_space>()==null && collider.GetComponent<IndianOceanAssets.ShooterSurvival.EnemyScript>()==null)continue;
        var tuning=collider.GetComponent<EnemyHitboxSize>();if(tuning==null)tuning=collider.gameObject.AddComponent<EnemyHitboxSize>();
        tuning.Configure(collider);EditorUtility.SetDirty(collider);EditorUtility.SetDirty(tuning);bodies++;
        if(PrefabUtility.IsPartOfPrefabInstance(collider)){PrefabUtility.RecordPrefabInstancePropertyModifications(collider);PrefabUtility.RecordPrefabInstancePropertyModifications(tuning);}
    }
}
foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/JH/Model/Prefab","Assets/ShooterSurvival/Prefabs/Entities"}))
{
    string path=AssetDatabase.GUIDToAssetPath(guid);var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);
    if(source.GetComponentInChildren<IndianOceanAssets.ShooterSurvival.EnemyScript_space>(true)==null && source.GetComponentInChildren<IndianOceanAssets.ShooterSurvival.EnemyScript>(true)==null)continue;
    string snapshot=backup+"/prefabs/"+guid+".prefab";System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(snapshot));if(!System.IO.File.Exists(snapshot))System.IO.File.Copy(path,snapshot);
    var instance=PrefabUtility.LoadPrefabContents(path);
    try{Tune(instance);PrefabUtility.SaveAsPrefabAsset(instance,path);prefabs++;}
    finally{PrefabUtility.UnloadPrefabContents(instance);}
}
var results=new System.Collections.Generic.List<string>();
foreach(var name in new[]{"Noryangjin_MapTool_Mode_SR18","HighWay","RestStop"})
{
    var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/"+name+".unity");
    string snapshot=backup+"/"+name+"-before-polish.unity";
    if(!System.IO.File.Exists(snapshot)&&!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,snapshot,true))throw new Exception("Backup failed");
    foreach(var root in scene.GetRootGameObjects())Tune(root);
    HarborGameUIInstaller.PolishOpenScene();
    if(name=="Noryangjin_MapTool_Mode_SR18")
    {
        var props=scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform.Find("Props");
        var ship=props.Find("SR18_Polish_Ship_2");var station=props.Cast<Transform>().Single(t=>t.name.StartsWith("SR18_L_G21_"));
        var position=station.position+station.forward*22;position.y=-.7f;ship.position=position;
        var direction=station.position-position;direction.y=0;ship.rotation=Quaternion.LookRotation(direction);
        var stats=ship.GetComponent<ObstacleStats>();stats.fireDistance=45;
        PrefabUtility.RecordPrefabInstancePropertyModifications(ship);PrefabUtility.RecordPrefabInstancePropertyModifications(stats);
    }
    if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("Save failed: "+name);
    results.Add(name);
}
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(original);
return new{scenes=results,prefabs,bodies};
