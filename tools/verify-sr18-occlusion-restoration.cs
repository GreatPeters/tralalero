var p=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.PlayerScript>();var c=Camera.main;var effect=c.GetComponent<IndianOceanAssets.ShooterSurvival.NoryangjinCameraOcclusion>();
effect.enabled=false;if(effect.HiddenCount!=0)throw new Exception("Disable did not restore hidden renderers");
var offset=Quaternion.Inverse(p.transform.rotation)*(c.transform.position-p.transform.position);var rotation=Quaternion.Inverse(p.transform.rotation)*c.transform.rotation;
p.transform.SetPositionAndRotation(new Vector3(0,12.16f,-77.75f),Quaternion.Euler(0,90,0));if(!c.transform.IsChildOf(p.transform))c.transform.SetPositionAndRotation(p.transform.position+p.transform.rotation*offset,p.transform.rotation*rotation);
effect.enabled=true;effect.RefreshVisibility();var roads=p.GetComponent<IndianOceanAssets.ShooterSurvival.NoryangjinRoadHeightFollower>().RoadRoot;
bool floorVisible=false;foreach(var road in roads.GetComponentsInChildren<MeshCollider>(true))if(road.Raycast(new Ray(p.transform.position+Vector3.up,Vector3.down),out var hit,2)&&hit.normal.y>.7f){floorVisible=!road.GetComponent<Renderer>().forceRenderingOff;break;}
if(!floorVisible)throw new Exception("Upper road floor was hidden");
return new{passed=true,restoreOnDisable=true,upperFloorVisible=floorVisible,roadColliders=roads.GetComponentsInChildren<MeshCollider>(true).Count(r=>r.enabled)};
