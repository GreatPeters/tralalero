using UnityEngine;

// Local-space water threads and moving drops; collision remains owned by NoryangjinHoseEvent.
public sealed class NoryangjinHoseSprayVisual : MonoBehaviour
{
    public LineRenderer[] threads=System.Array.Empty<LineRenderer>();
    public Transform[] droplets=System.Array.Empty<Transform>();
    public Vector3 start=new Vector3(0,.95f,0),end=new Vector3(2.7f,.16f,0);
    private Vector3 Point(float t,float phase)
    {
        var point=Vector3.Lerp(start,end,t);
        point.y+=Mathf.Sin(t*Mathf.PI)*.12f;
        point.z+=Mathf.Sin(t*25-Time.time*20+phase)*.025f*Mathf.Sin(t*Mathf.PI);
        return point;
    }
    private void OnEnable()=>Update();
    private void Update()
    {
        for(int i=0;i<threads.Length;i++)if(threads[i]!=null)
            for(int j=0;j<threads[i].positionCount;j++)threads[i].SetPosition(j,Point(j/(float)(threads[i].positionCount-1),i*2)+Vector3.up*(i*.028f));
        for(int i=0;i<droplets.Length;i++)if(droplets[i]!=null)
            droplets[i].localPosition=Point(Mathf.Repeat(Time.time*2.8f+i/(float)droplets.Length,1),i)+new Vector3(0,0,Mathf.Sin(i*7)*.075f);
    }
}
