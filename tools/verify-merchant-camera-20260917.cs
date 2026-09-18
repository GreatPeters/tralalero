using System;using System.Linq;using System.IO;using UnityEngine;using UnityEditor;using Object=UnityEngine.Object;
public static class VerifyMerchantCamera20260917{public static object Main(){
 var merchant=Object.FindFirstObjectByType<HarborMerchantGreeting>();var roads=GameObject.Find("Noryangjin_MapTool").transform.Find("Roads");
 var head=merchant.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Head");var delta=head.position-Camera.main.transform.position;
 var blocks=Physics.RaycastAll(Camera.main.transform.position,delta.normalized,delta.magnitude-.2f).Where(h=>h.collider.transform.IsChildOf(roads)).ToArray();
 var skin=merchant.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=new Mesh();skin.BakeMesh(mesh);float minimum=mesh.vertices.Min(v=>skin.transform.TransformPoint(v).y);Object.Destroy(mesh);
 string report="Pier blockers to head="+blocks.Length+"\nHead viewport="+Camera.main.WorldToViewportPoint(head.position)+"\nBaked body minimum Y="+minimum+"\nStation="+merchant.transform.position+"\nSeated="+merchant.animator.GetCurrentAnimatorStateInfo(0).IsName("SeatedIdle");
 File.WriteAllText("map-concepts/harbor-reference-fidelity-2026-09-17/merchant-camera.txt",report);if(blocks.Length>0)throw new Exception(report);return report;
}}
