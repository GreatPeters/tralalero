using System.Linq;
using System.Collections.Generic;
using UnityEngine;
public static class InspectSeagullRoadWidth
{
    public static string Main()
    {
        var map=GameObject.Find("Noryangjin_MapTool").transform;
        var bird=map.GetComponentsInChildren<ObstacleStats>(true).First(s=>s.obstaclePattern==ObstaclePattern.Seagull);
        var roads=map.Find("Roads").GetComponentsInChildren<MeshCollider>(true);
        var rows=new List<string>();
        foreach(float z in new[]{-15f,-8f,0f,8f,15f,22f})
        {
            var samples=new List<float>();
            for(float x=-4;x<=4;x+=.25f)
            {
                var ray=new Ray(bird.transform.position+bird.transform.forward*z+bird.transform.right*x+Vector3.up*3,Vector3.down);
                if(roads.Any(c=>c.Raycast(ray,out var hit,6)))samples.Add(x);
            }
            rows.Add($"z={z}: "+(samples.Count>0?$"{samples.Min()} .. {samples.Max()}":"none"));
        }
        foreach(float lane in new[]{-1.5f,0f,1.5f})
        {
            int misses=0,outside=0;
            for(float z=-9;z<=9;z+=.1f)foreach(float side in new[]{-.7f,0f,.7f})
            {
                bool OnRoad(float dz)
                {
                    var ray=new Ray(bird.transform.position+bird.transform.forward*(z+dz)+bird.transform.right*(lane+side)+Vector3.up*3,Vector3.down);
                    return roads.Any(c=>c.Raycast(ray,out var hit,6));
                }
                if(!OnRoad(0)){misses++;if(!OnRoad(-.15f)&&!OnRoad(.15f))outside++;}
            }
            rows.Add($"lane={lane}, point-ray misses={misses}, misses beyond 0.15m mesh seams={outside}");
        }
        return string.Join("\n",rows);
    }
}
