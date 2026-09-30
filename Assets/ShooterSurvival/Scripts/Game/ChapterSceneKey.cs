using UnityEngine.SceneManagement;

// Safe-copy scenes (e.g. the Noryangjin revamp copy of SR18) read the same Data.xlsx rows and
// chapter settings as the scene they were copied from.
public static class ChapterSceneKey
{
    public const string Sr18 = "Noryangjin_MapTool_Mode_SR18";
    public const string Sr18Revamp = "Noryangjin_MapTool_Mode_SR18_Revamp";

    public static string Resolve(string sceneName) => sceneName == Sr18Revamp ? Sr18 : sceneName;
    public static string Resolve(Scene scene) => Resolve(scene.name);
}
