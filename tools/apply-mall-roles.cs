using System;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using IndianOceanAssets.ShooterSurvival;using TMPro;using Object=UnityEngine.Object;
public static class ApplyMallRoles {
 const string Assets="Assets/ShooterSurvival/Models/Chapters/Chapters45/DetailedMall-20261003T092859667";
 static Chapter45Director d;static Transform root;static Material warning,ivory,dark,metal,wood;static List<Chapter45Hazard> hazards;
 static GameObject Primitive(Transform p,string name,PrimitiveType type,Vector3 pos,Vector3 scale,Material mat,bool trigger=false){var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;g.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;var c=g.GetComponent<Collider>();if(trigger)c.isTrigger=true;else Object.DestroyImmediate(c);return g;}
 static Transform Group(Transform p,string name){var g=new GameObject(name);g.transform.SetParent(p,false);return g.transform;}
 static Material Load(string name)=>AssetDatabase.LoadAssetAtPath<Material>(Assets+"/"+name+".mat");
 static Chapter45RoleAction Bind(Chapter45Encounter e,EnemyScript_space actor,Chapter45RoleAction.Role role,int index){
  var behavior=actor.gameObject.AddComponent<Chapter45RoleAction>();behavior.director=d;behavior.encounter=e;behavior.role=role;behavior.phaseOffset=index*.7f;behavior.actionDamage=role==Chapter45RoleAction.Role.Basketball?4:role==Chapter45RoleAction.Role.Manager?3:2;behavior.cooldown=4.2f;behavior.moveSpeed=role==Chapter45RoleAction.Role.StudentGrab?2.6f:2.1f;
  // Controller methods still provide death and activation animation; movement is
  // owned by this component, avoiding two simultaneous position writers.
  actor.GetComponent<EnemyEventController>().enabled=false;
  var marker=Primitive(root,actor.name+" warning footprint",PrimitiveType.Cylinder,Vector3.zero,new Vector3(role==Chapter45RoleAction.Role.Basketball?3:2.5f,.012f,role==Chapter45RoleAction.Role.Basketball?3:2.5f),warning);marker.SetActive(false);behavior.warningMarker=marker.transform;
  behavior.warningText=role switch{Chapter45RoleAction.Role.StudentGrab=>"와 머야~!! · 손을 뻗기 전에 옆으로",Chapter45RoleAction.Role.Basketball=>"돌파 준비 · 표시된 방향을 피하세요",Chapter45RoleAction.Role.Golfer=>"골프채 스윙 · 팔 바깥으로 피하세요",Chapter45RoleAction.Role.PlateThrower=>"접시 투척 · 옆으로 이동하세요",Chapter45RoleAction.Role.PopcornThrower=>"팝콘 세 갈래 · 틈으로 피하세요",Chapter45RoleAction.Role.Cleaner=>"대걸레를 휘두릅니다",_=>"다가오는 사람을 피하세요"};
  if(role==Chapter45RoleAction.Role.PlateThrower||role==Chapter45RoleAction.Role.PopcornThrower){
   var template=Primitive(actor.transform,"Inactive authored projectile template",role==Chapter45RoleAction.Role.PlateThrower?PrimitiveType.Cylinder:PrimitiveType.Sphere,new Vector3(.5f,1.15f,.25f),role==Chapter45RoleAction.Role.PlateThrower?new Vector3(.5f,.06f,.5f):Vector3.one*.3f,ivory,true);template.AddComponent<SimpleProjectile>();template.SetActive(false);behavior.projectileTemplate=template;
   var held=Primitive(actor.transform,"Visible held "+role,role==Chapter45RoleAction.Role.PlateThrower?PrimitiveType.Cylinder:PrimitiveType.Cube,new Vector3(.5f,1.15f,.25f),role==Chapter45RoleAction.Role.PlateThrower?new Vector3(.5f,.035f,.5f):new Vector3(.4f,.5f,.4f),ivory);behavior.heldProp=held.transform;
  }
  if(role==Chapter45RoleAction.Role.Basketball)behavior.heldProp=Primitive(actor.transform,"Basketball action prop temporary authored",PrimitiveType.Sphere,new Vector3(.55f,1,.35f),Vector3.one*.42f,wood).transform;
  if(role==Chapter45RoleAction.Role.Golfer||role==Chapter45RoleAction.Role.Cleaner){var prop=Group(actor.transform,role==Chapter45RoleAction.Role.Golfer?"Golf club action prop":"Mop action prop");prop.localPosition=new Vector3(.4f,1,.3f);Primitive(prop,"Metal handle",PrimitiveType.Cylinder,new Vector3(0,-.2f,.5f),new Vector3(.04f,.75f,.04f),metal).transform.localRotation=Quaternion.Euler(65,0,0);Primitive(prop,"Visible striking head",PrimitiveType.Cube,new Vector3(0,-.7f,1.05f),role==Chapter45RoleAction.Role.Golfer?new Vector3(.3f,.15f,.16f):new Vector3(.8f,.2f,.35f),role==Chapter45RoleAction.Role.Golfer?metal:ivory);behavior.heldProp=prop;}
  return behavior;
 }
 static Chapter45RoleAction.Role RoleFor(int floor,int group,int actor){switch(floor){
  case 1:return group==2?Chapter45RoleAction.Role.Guard:Chapter45RoleAction.Role.Tourist;
  case 2:return group==1?Chapter45RoleAction.Role.LinkedCouple:Chapter45RoleAction.Role.ShoppingBags;
  case 3:return group==2?Chapter45RoleAction.Role.Basketball:group==1?Chapter45RoleAction.Role.StudentGrab:actor==1?Chapter45RoleAction.Role.Golfer:Chapter45RoleAction.Role.Guard;
  case 4:return group==1?Chapter45RoleAction.Role.PlateThrower:group==2?Chapter45RoleAction.Role.Guard:Chapter45RoleAction.Role.Tourist;
  case 8:return group==0?Chapter45RoleAction.Role.CalledQueue:group==1?(actor==2?Chapter45RoleAction.Role.PlateThrower:Chapter45RoleAction.Role.ShoppingBags):group==2?(actor==0?Chapter45RoleAction.Role.Basketball:Chapter45RoleAction.Role.StudentGrab):(actor==0?Chapter45RoleAction.Role.PlateThrower:Chapter45RoleAction.Role.CalledQueue);
  case 5:return group==0?Chapter45RoleAction.Role.TicketCrowd:group==1?Chapter45RoleAction.Role.LinkedCouple:actor==0?Chapter45RoleAction.Role.PopcornThrower:Chapter45RoleAction.Role.Cleaner;
  case 6:return Chapter45RoleAction.Role.TicketCrowd;
  default:return Chapter45RoleAction.Role.Manager;
 }}
 static Chapter45Hazard Hazard(string name,int floor,float distance,Vector3 size,Vector3 from,Vector3 to,string text,int branch=-1){
  var g=Group(root,name);d.route.Sample(distance,out var pos,out var forward);g.position=pos;g.rotation=Quaternion.LookRotation(forward);
  var h=g.gameObject.AddComponent<Chapter45Hazard>();h.floor=floor;h.distance=distance;h.warningDistance=18;h.warningSeconds=1.5f;h.operationSeconds=1.25f;h.damageFraction=.08f;h.safeLane=-3.2f;h.safeHalfWidth=1.5f;h.choiceIndex=branch<0?-1:0;h.requiredChoice=branch;h.warning=text;h.sweepFrom=from;h.sweepTo=to;
  var body=Primitive(g,name+" moving body",PrimitiveType.Cube,new Vector3(1.8f,1,0),size,dark,true);body.GetComponent<Collider>().isTrigger=true;h.body=body.transform;
  if(name.Contains("TV")||name.Contains("television")){Primitive(body.transform,"TV visible screen",PrimitiveType.Cube,new Vector3(0,0,-.51f),new Vector3(.9f,.85f,.03f),ivory);}
  else {Primitive(body.transform,"Cart upper tray",PrimitiveType.Cube,new Vector3(0,.6f,0),new Vector3(1.1f,.12f,1.1f),metal);for(int i=0;i<4;i++)Primitive(body.transform,"Cart visible wheel",PrimitiveType.Cylinder,new Vector3(i%2==0?-.45f:.45f,-.65f,i<2?-.4f:.4f),new Vector3(.24f,.09f,.24f),dark).transform.localRotation=Quaternion.Euler(0,0,90);}
  var mark=Primitive(g,name+" shadow warning",PrimitiveType.Cylinder,new Vector3(1.8f,.035f,0),new Vector3(size.x+1,.015f,size.z+1),warning);h.footprint=mark;hazards.Add(h);return h;
 }
 public static object Main(){
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Idle required");EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/ShoeTower.unity");d=Object.FindFirstObjectByType<Chapter45Director>();root=d.transform.Find("DetailedMall_20261003");if(root.GetComponentsInChildren<Chapter45RoleAction>(true).Length>0)throw new Exception("Roles already bound; inspect, do not duplicate");
  warning=Load("Warning amber");ivory=Load("Porcelain");dark=Load("Ink graphite");metal=Load("Satin champagne metal");wood=Load("Warm oak");hazards=d.hazards.ToList();int count=0;
  foreach(var floor in d.encounters.GroupBy(e=>e.floor)) {int group=0;foreach(var e in floor.OrderBy(e=>e.activationDistance)) {Chapter45RoleAction lead=null;for(int i=0;i<e.actors.Length;i++){var role=RoleFor(e.floor,group,i);var b=Bind(e,e.actors[i],role,i);count++;if(role==Chapter45RoleAction.Role.LinkedCouple){if(lead==null)lead=b;else{b.linkedLeader=lead;b.linkedOffset=b.transform.position-lead.transform.position;}}}group++;}}
  var tv=d.encounters.Where(e=>e.floor==4).OrderBy(e=>e.activationDistance).Last();var tvHazard=Hazard("4F falling television",4,tv.stopDistance+4,new Vector3(2.5f,1.5f,.7f),new Vector3(0,9,0),Vector3.zero,"TV 낙하 · 그림자를 피하세요");tvHazard.warningDistance=22;tvHazard.lingerSeconds=1.2f;
  var b1=d.encounters.Where(e=>e.floor==8).OrderBy(e=>e.activationDistance).ToArray();Hazard("B1 warned serving cart",8,b1[2].stopDistance+2,new Vector3(1.4f,1.1f,1.5f),new Vector3(5,0,0),new Vector3(-.7f,0,0),"배식 카트 통과 · 왼쪽 통로는 열려 있습니다",0);
  d.hazards=hazards.ToArray();EditorUtility.SetDirty(d);EditorSceneManager.MarkSceneDirty(d.gameObject.scene);EditorSceneManager.SaveScene(d.gameObject.scene);AssetDatabase.SaveAssets();
  return new{roleActors=count,hazards=d.hazards.Select(h=>h.name).ToArray(),roles=root.GetComponentsInChildren<Chapter45RoleAction>(true).GroupBy(b=>b.role).Select(g=>new{role=g.Key.ToString(),count=g.Count()}).ToArray(),limitation="Behavior installation; role-specific meshes/hand poses and generated props are still separate art gates. Native action props are explicitly provisional."};
 }
}
