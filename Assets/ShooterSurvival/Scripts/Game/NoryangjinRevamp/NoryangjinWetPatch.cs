using UnityEngine;
using IndianOceanAssets.ShooterSurvival;

// Persistent wet footprint: visual wetness outlives the spray, and standing in it refreshes the slip.
public sealed class NoryangjinWetPatch : MonoBehaviour
{
    public Renderer surface;
    public ParticleSystem splash;
    public Vector2 halfSize=new Vector2(1.4f,1);
    public float drySeconds=10,sidewaysSpeed=.9f;
    [Tooltip("Stepping on a wet patch spins the shark for this long while draining max HP per second.")]
    public float spinSeconds=1.1f,spinDrainShare=.06f;
    public static readonly Color FilmColor=new Color(.012f,.014f,.02f,.84f);
    public float Wetness{get;private set;}
    private bool spraying;
    private float nextSlip;
    private MaterialPropertyBlock block;
    public void SetSpraying(bool value)
    {
        spraying=value;
        if(value)Wetness=1;
        if(splash!=null)
        {if(value&&!splash.isPlaying)splash.Play();else if(!value&&splash.isPlaying)splash.Stop(true,ParticleSystemStopBehavior.StopEmitting);}
    }
    public void ResetPatch()
    {
        spraying=false;Wetness=0;nextSlip=0;
        if(splash!=null)splash.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        if(surface!=null)surface.enabled=false;
    }
    public static bool Contains(Vector3 local,Vector2 half)
        => Mathf.Abs(local.x)<=half.x&&Mathf.Abs(local.z)<=half.y&&Mathf.Abs(local.y)<2;
    public void AdvanceDrying(float seconds)
        => Wetness=spraying?1:Mathf.Max(0,Wetness-Mathf.Max(0,seconds)/Mathf.Max(.1f,drySeconds));
    private void Update()
    {
        AdvanceDrying(Time.deltaTime);
        if(surface!=null)
        {
            surface.enabled=Wetness>.01f;block??=new MaterialPropertyBlock();
            block.SetColor("_BaseColor",new Color(FilmColor.r,FilmColor.g,FilmColor.b,FilmColor.a*Mathf.Sqrt(Wetness)));block.SetFloat("_Spraying",spraying?1:0);surface.SetPropertyBlock(block);
        }
        var d=NoryangjinRevampDirector.Active;
        if(Wetness<.1f||d==null||!d.Running||d.Player==null||Time.time<nextSlip)return;
        var p=d.Player;if(!Contains(transform.InverseTransformPoint(p.transform.position),halfSize))return;
        nextSlip=Time.time+.25f;
        if(d.Spinning)return;
        // A fresh step spins the shark and drains HP; during the post-spin grace it only slides.
        if(d.StartSpin(spinSeconds,spinDrainShare,PlayerDamageCause.WetFloor,"젖은 바닥! 빙글빙글~"))return;
        var slip=p.GetComponent<NoryangjinWetSteering>()??p.gameObject.AddComponent<NoryangjinWetSteering>();
        bool began=slip.Remaining<=0;slip.Apply(p,1.7f,sidewaysSpeed);
        if(began)d.Timeline.Add($"wet floor slip at {d.Elapsed:F1}");
    }
}
