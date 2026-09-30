using UnityEngine;

// Only presentation-only bay groups are registered here; game triggers and road colliders stay live.
[DefaultExecutionOrder(500)]
public sealed class NoryangjinInteriorDetailVisibility : MonoBehaviour
{
    public Transform[] bays = System.Array.Empty<Transform>();
    [Tooltip("Raised market props outside the bays (filled by NoryangjinCrossingLiftBuilder).")]
    public Transform[] liftedDetails = System.Array.Empty<Transform>();
    public Light[] localLights = System.Array.Empty<Light>();
    public Transform[] hangingDisplays = System.Array.Empty<Transform>();
    public Renderer[] oldRoadVisuals = System.Array.Empty<Renderer>();
    public Renderer[] coldRoadVisuals = System.Array.Empty<Renderer>();
    private bool[] roadWasEnabled;
    private bool[] coldWasEnabled;
    public float viewDistance = 80;
    public Transform fogCurtain;
    private Transform player;
    private float nextCheck;
    private bool originalFog, captured, applied;
    private Color originalColor;
    private FogMode originalMode;
    private float originalStart,originalEnd;
    public static bool IsInside(Vector3 local,Vector3 heading,bool outside,float length=378)
        => Mathf.Abs(local.x)<7&&Mathf.Abs(local.y)<3&&local.z>0&&local.z<length&&heading.z>.7f&&!outside;
    private void Start()
    {
        player=FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.PlayerScript>()?.transform;
        originalFog=RenderSettings.fog;originalColor=RenderSettings.fogColor;originalMode=RenderSettings.fogMode;
        originalStart=RenderSettings.fogStartDistance;originalEnd=RenderSettings.fogEndDistance;captured=true;
        roadWasEnabled=new bool[oldRoadVisuals.Length];
        for(int i=0;i<oldRoadVisuals.Length;i++)if(oldRoadVisuals[i]!=null)roadWasEnabled[i]=oldRoadVisuals[i].enabled;
        coldWasEnabled=new bool[coldRoadVisuals.Length];
        for(int i=0;i<coldRoadVisuals.Length;i++)if(coldRoadVisuals[i]!=null)coldWasEnabled[i]=coldRoadVisuals[i].enabled;
    }
    private void Update()
    {
        if (player == null || Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + .2f;
        var lift = NoryangjinRevampDirector.Active != null ? NoryangjinRevampDirector.Active.Branch : null;
        var cam = Camera.main;
        foreach (var bay in bays) if (bay != null)
        {
            bool visible = (bay.position - player.position).sqrMagnitude < viewDistance * viewDistance && !CameraInsideRaised(bay, lift, cam, 14);
            if (bay.gameObject.activeSelf != visible) bay.gameObject.SetActive(visible);
        }
        foreach (var detail in liftedDetails) if (detail != null)
        {
            bool visible = !CameraInsideRaised(detail, lift, cam, 8);
            if (detail.gameObject.activeSelf != visible) detail.gameObject.SetActive(visible);
        }
        foreach (var light in localLights) if (light != null)
            light.enabled = (light.transform.position - player.position).sqrMagnitude < 24 * 24;
        var branch=NoryangjinRevampDirector.Active!=null?NoryangjinRevampDirector.Active.Branch:null;
        // The market floor climbs over S3; judge the storey relative to the raised floor.
        var local=transform.InverseTransformPoint(player.position-Vector3.up*(branch!=null?branch.HeightAt(player.position):0));
        bool inside=IsInside(local,transform.InverseTransformDirection(player.forward),branch!=null&&branch.Outside&&branch.Driving);
        bool cold=IsInside(new Vector3(player.position.x+67,player.position.y,player.position.z+344),player.forward,false,130);
        for(int i=0;i<oldRoadVisuals.Length;i++)if(oldRoadVisuals[i]!=null)oldRoadVisuals[i].enabled=roadWasEnabled[i]&&!inside;
        for(int i=0;i<coldRoadVisuals.Length;i++)if(coldRoadVisuals[i]!=null)coldRoadVisuals[i].enabled=coldWasEnabled[i]&&!cold;
        foreach(var display in hangingDisplays)if(display!=null)
            display.gameObject.SetActive((display.position-player.position).sqrMagnitude<viewDistance*viewDistance&&(!inside||display.localPosition.z>local.z+8));
        if(inside)
        {
            applied=true;RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;
            RenderSettings.fogColor=new Color(.7f,.75f,.76f);RenderSettings.fogStartDistance=60;RenderSettings.fogEndDistance=110;
        }
        else if(applied)RestoreFog();
        if(fogCurtain!=null)
        {
            fogCurtain.gameObject.SetActive(inside&&local.z<302);
            fogCurtain.localPosition=new Vector3(0,4,local.z+72);
        }
    }
    // The market floor is raised over the S3 pier: while the shark runs underneath, the normal high
    // camera passes through the raised market, so parts the camera is inside are hidden.
    private bool CameraInsideRaised(Transform part, NoryangjinMarketBranch lift, Camera cam, float halfLength)
    {
        if (lift == null || cam == null) return false;
        float h = lift.HeightAt(part.position);
        if (h <= 3 || player.position.y > h - 3) return false;
        // Camera within the market's width, and the part in the stretch of market around the camera.
        float cameraLateral = Vector3.Dot(cam.transform.position - transform.position, transform.right);
        return Mathf.Abs(cameraLateral) < 11 && Mathf.Abs(Vector3.Dot(cam.transform.position - part.position, transform.forward)) < halfLength;
    }

    private void OnDisable()
    {
        foreach (var detail in liftedDetails) if (detail != null) detail.gameObject.SetActive(true);
        RestoreFog();
        foreach (var bay in bays) if (bay != null) bay.gameObject.SetActive(true);
        foreach (var light in localLights) if (light != null) light.enabled = true;
        foreach(var display in hangingDisplays)if(display!=null)display.gameObject.SetActive(true);
        if(roadWasEnabled!=null)for(int i=0;i<oldRoadVisuals.Length;i++)if(oldRoadVisuals[i]!=null)oldRoadVisuals[i].enabled=roadWasEnabled[i];
        if(coldWasEnabled!=null)for(int i=0;i<coldRoadVisuals.Length;i++)if(coldRoadVisuals[i]!=null)coldRoadVisuals[i].enabled=coldWasEnabled[i];
    }
    private void RestoreFog()
    {
        if(!captured)return;
        RenderSettings.fog=originalFog;RenderSettings.fogColor=originalColor;RenderSettings.fogMode=originalMode;
        RenderSettings.fogStartDistance=originalStart;RenderSettings.fogEndDistance=originalEnd;applied=false;
    }
}
