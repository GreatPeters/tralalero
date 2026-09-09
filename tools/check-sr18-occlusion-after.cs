var effect=Camera.main.GetComponent<IndianOceanAssets.ShooterSurvival.NoryangjinCameraOcclusion>();effect.enabled=true;effect.RefreshVisibility();
var p=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.PlayerScript>();
return new{folder=UnityEditor.SessionState.GetString("SR18.OcclusionCheck.Folder",""),hidden=effect.HiddenCount,allRoadCollidersStillEnabled=p.GetComponent<IndianOceanAssets.ShooterSurvival.NoryangjinRoadHeightFollower>().RoadRoot.GetComponentsInChildren<MeshCollider>(true).All(c=>c.enabled)};
