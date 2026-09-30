using UnityEngine;

// Slats disappear into the fixed roller housing, rather than translating a giant plate above the roof.
public sealed class NoryangjinRollerShutterVisual : MonoBehaviour
{
    public Transform[] slats=System.Array.Empty<Transform>();
    public Transform bottomRail;
    public float height=5.8f;
    public float Opening{get;private set;}
    public void SetOpening(float value)
    {
        Opening=Mathf.Clamp(value,0,height);
        float pitch=height/Mathf.Max(1,slats.Length);
        for(int i=0;i<slats.Length;i++)if(slats[i]!=null)
        {
            float y=(i+.5f)*pitch+Opening;
            slats[i].gameObject.SetActive(y<=height);
            slats[i].localPosition=new Vector3(0,y,0);
        }
        if(bottomRail!=null)bottomRail.localPosition=new Vector3(0,Opening+.07f,-.025f);
    }
}
