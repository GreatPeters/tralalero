using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;
using Object=UnityEngine.Object;
public static class S22OcclusionState
{
 static string Path(Transform t)=>t.parent==null?t.name:Path(t.parent)+"/"+t.name;
 public static object Main(){var component=Object.FindFirstObjectByType<NoryangjinCameraOcclusion>();var atmosphere=Object.FindFirstObjectByType<NoryangjinMarketAtmosphere>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;var faded=(IDictionary)component.GetType().GetField("faded",flags).GetValue(component);return new{groups=((Transform[])component.GetType().GetField("clearViewGroups",flags).GetValue(component)).Where(t=>t!=null).Select(t=>Path(t)).ToArray(),offset=atmosphere.GetType().GetField("cameraOffset",flags).GetValue(atmosphere).ToString(),rotation=((Quaternion)atmosphere.GetType().GetField("cameraRotation",flags).GetValue(atmosphere)).eulerAngles.ToString(),faded=faded.Keys.Cast<Renderer>().Where(r=>r!=null).Select(r=>new{path=Path(r.transform),bounds=r.bounds.ToString(),opacity=component.SceneryOpacity(r)}).ToArray()};}
}
