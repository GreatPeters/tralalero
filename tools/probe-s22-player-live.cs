using System;
using System.Linq;
using UnityEngine;
using IndianOceanAssets.ShooterSurvival;
public static class S22PlayerLive
{
 public static object Main(){var root=GameObject.Find("Noryangjin_Player/Original");var a=root.GetComponentInChildren<Animator>();var r=root.GetComponentInChildren<SkinnedMeshRenderer>();return new{Time.frameCount,TimeManager.isGameRunning,a.speed,state=a.GetCurrentAnimatorStateInfo(0).normalizedTime,idle=a.GetCurrentAnimatorStateInfo(0).IsName("Idle"),clips=a.GetCurrentAnimatorClipInfo(0).Select(c=>c.clip.name).ToArray(),mesh=r.sharedMesh.name,bones=r.bones.Length,proxy=r.bones.Last().position.ToString("F4"),rear=r.bones[9].position.ToString("F4")};}
}
