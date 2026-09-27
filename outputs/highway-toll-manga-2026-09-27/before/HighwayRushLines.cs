using UnityEngine;
using UnityEngine.UI;

// Screen-edge streaks keep the road centre and HUD clear; no fullscreen white overlay.
public sealed class HighwayRushLines : MaskableGraphic
{
    private void Update(){if(isActiveAndEnabled)SetVerticesDirty();}
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();var rect=rectTransform.rect;var focus=new Vector2(0,rect.height*.16f);
        int count=HighwayChapter2Data.Count("speedLineCount");
        for(int i=0;i<count;i++)
        {
            float seed=Mathf.Repeat(i*.618034f,1);float phase=Mathf.Repeat(Time.time*1.7f+seed,1);
            float side=i%2==0?-1:1;
            var edge=new Vector2(side*rect.width*.62f,Mathf.Lerp(-rect.height*.65f,rect.height*.28f,seed));
            var direction=(edge-focus).normalized;var normal=new Vector2(-direction.y,direction.x);
            var end=Vector2.LerpUnclamped(focus,edge,.68f+phase*.52f);var start=end-direction*(100+160*seed);
            if(Mathf.Abs(start.x)<rect.width*.28f)start=Vector2.Lerp(start,end,.6f);
            float width=2+seed*3;var tint=new Color(.78f,.94f,1,(1-phase)*.5f);int at=mesh.currentVertCount;
            mesh.AddVert(start-normal*width,tint,Vector2.zero);mesh.AddVert(start+normal*width,tint,Vector2.zero);
            mesh.AddVert(end,new Color(1,1,1,0),Vector2.one);mesh.AddTriangle(at,at+1,at+2);
        }
    }
}
