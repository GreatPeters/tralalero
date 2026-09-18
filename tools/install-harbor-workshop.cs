using UnityEditor;using UnityEditor.SceneManagement;
public static class InstallHarborWorkshop{public static object Main(){
 EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/Noryangjin_MapTool_Mode_SR18.unity");
 var workshop=HarborWorkshopAssetImporter.InstallWorkshop();var merchant=HarborWorkshopAssetImporter.InstallMerchant();return new{workshop,merchant};
}}
