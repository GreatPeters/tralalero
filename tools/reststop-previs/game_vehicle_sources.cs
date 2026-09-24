using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class RestStopVehicleSources
{
    public static object Main()
    {
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name.StartsWith("Parked car ")).Take(5).Select(t=>new{
            name=t.name,
            meshes=t.GetComponentsInChildren<MeshFilter>(true).Select(m=>new{file=AssetDatabase.GetAssetPath(m.sharedMesh),name=m.sharedMesh.name,localScale=m.transform.localScale.ToString(),rotation=m.transform.localEulerAngles.ToString()}).ToArray(),
            materials=t.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Select(m=>new{name=m.name,path=AssetDatabase.GetAssetPath(m),texture=m.mainTexture!=null?AssetDatabase.GetAssetPath(m.mainTexture):null}).ToArray()
        }).ToArray();
    }
}
