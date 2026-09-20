using UnityEngine;
using UnityEngine.SceneManagement;

namespace IndianOceanAssets.ShooterSurvival
{
    /// <summary>Detached, reward-free unfold/transfer/impact feedback shared by all bonuses.</summary>
    public sealed class BonusTalismanPickup : MonoBehaviour
    {
        public const float Duration = 1.02f;
        private BonusTalismanVisual visual;
        private PlayerScript player;
        private Transform body;
        private Vector3 bodyLocalPoint, sourcePosition, playerStart, iconLocalStart, iconScale;
        private float surfaceOffset, elapsed, claimStamp;
        private Camera viewCamera;
        private SpriteRenderer pulse, ring;
        private SpriteRenderer[] motes;
        private LineRenderer trail, trailHalo;
        public float Elapsed => elapsed;
        public Vector3 TargetPosition
        {
            get
            {
                if(body==null)return Vector3.zero;
                Vector3 point=body.TransformPoint(bodyLocalPoint);
                return viewCamera!=null?point+(viewCamera.transform.position-point).normalized*surfaceOffset:point;
            }
        }

        public static BonusTalismanPickup Spawn(BonusTalismanVisual source, PlayerScript target)
        {
            var copy=Instantiate(source,source.transform.position,source.transform.rotation);
            copy.name="BonusTalismanPickup";copy.transform.localScale=Vector3.one;copy.IsPickup=true;
            SceneManager.MoveGameObjectToScene(copy.gameObject,target.gameObject.scene);
            var effect=copy.gameObject.AddComponent<BonusTalismanPickup>();
            effect.visual=copy;effect.player=target;effect.claimStamp=target.lastWallTouchTime;
            effect.viewCamera=Camera.main;effect.sourcePosition=copy.transform.position;effect.playerStart=target.transform.position;
            effect.iconLocalStart=copy.icon.transform.localPosition;effect.iconScale=copy.icon.transform.localScale;
            effect.body=target.transform.Find("Original")??target.transform;
            SkinnedMeshRenderer main=null;float largest=0;
            foreach(var renderer in effect.body.GetComponentsInChildren<SkinnedMeshRenderer>())
                if(renderer.bounds.size.sqrMagnitude>largest){largest=renderer.bounds.size.sqrMagnitude;main=renderer;}
            Vector3 center=main!=null?main.bounds.center+Vector3.up*main.bounds.extents.y*.35f:target.transform.position+Vector3.up*1.6f;
            effect.surfaceOffset=main!=null?main.bounds.extents.magnitude+.05f:.5f;
            effect.bodyLocalPoint=effect.body.InverseTransformPoint(center);
            var glow=Resources.Load<Sprite>("BonusTalisman/SoftGlow");
            var material=Resources.Load<Material>("BonusTalisman/Polished/GoldGlow");
            effect.pulse=effect.Sprite("TorsoPulse",glow,material);
            effect.ring=effect.Sprite("ImpactRing",Resources.Load<Sprite>("BonusTalisman/Polished/ImpactRing"),material);
            effect.motes=new SpriteRenderer[8];
            for(int i=0;i<effect.motes.Length;i++)effect.motes[i]=effect.Sprite("Glint"+i,glow,material);
            effect.trail=effect.Line("AbsorptionTrail","TrailCore",.13f);
            effect.trailHalo=effect.Line("TrailHalo","TrailHalo",.38f);
            effect.Sample(0);
            return effect;
        }
        SpriteRenderer Sprite(string name,Sprite sprite,Material material)
        {
            var go=new GameObject(name);go.transform.SetParent(transform,false);
            var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sharedMaterial=material;renderer.color=Color.clear;renderer.sortingOrder=5;return renderer;
        }
        LineRenderer Line(string name,string material,float width)
        {
            var go=new GameObject(name);go.transform.SetParent(transform,false);var line=go.AddComponent<LineRenderer>();
            line.sharedMaterial=Resources.Load<Material>("BonusTalisman/Polished/"+material);line.useWorldSpace=true;line.positionCount=20;line.numCapVertices=4;line.widthMultiplier=width;
            line.widthCurve=new AnimationCurve(new Keyframe(0,.08f),new Keyframe(.6f,1),new Keyframe(1,.45f));line.enabled=false;return line;
        }
        private void Update()
        {
            if(player==null||!player.gameObject.activeInHierarchy||!TimeManager.isGameRunning||player.currentHealth<=0||player.lastWallTouchTime<claimStamp){Destroy(gameObject);return;}
            elapsed+=Time.deltaTime;Sample(elapsed);if(elapsed>=Duration)Destroy(gameObject);
        }
        public void Sample(float time)
        {
            if(visual==null||body==null)return;
            // Keep the unfolding prop within the moving gameplay view.
            Vector3 side=viewCamera!=null?viewCamera.transform.right:Vector3.right;
            Vector3 up=viewCamera!=null?viewCamera.transform.up:Vector3.up;
            float reveal=Mathf.SmoothStep(0,1,Mathf.Clamp01(time/.18f));
            transform.position=sourcePosition+(player.transform.position-playerStart)*.85f+(side*-1.5f+up*.7f)*reveal;
            if(viewCamera!=null)transform.rotation=viewCamera.transform.rotation*Quaternion.Euler(0,-7,0);
            float open=Mathf.SmoothStep(0,1,Mathf.Clamp01(time/.24f));visual.SetOpen(open);
            float transfer=Mathf.SmoothStep(0,1,Mathf.Clamp01((time-.22f)/.49f));
            Vector3 start=transform.TransformPoint(iconLocalStart),end=TargetPosition;
            Vector3 control=(start+end)*.5f+side*1.3f+up*.55f;
            visual.icon.transform.position=Curve(start,control,end,transfer);
            float shrink=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((time-.70f)/.17f));
            visual.icon.transform.localScale=iconScale*Mathf.Lerp(1.15f,.6f,transfer)*shrink;
            visual.icon.color=new Color(1,1,1,shrink);
            if(viewCamera!=null)visual.icon.transform.rotation=viewCamera.transform.rotation;
            float paperSize=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((time-.36f)/.27f));
            visual.paper.localScale=Vector3.one*paperSize;
            if(visual.readyHalo!=null)visual.readyHalo.color=new Color(1,.8f,.35f,paperSize*(.25f+.5f*Mathf.Sin(Mathf.Clamp01(time/.45f)*Mathf.PI)));
            float trailAlpha=1-Mathf.Clamp01((time-.68f)/.15f);
            SetTrail(trail,start,control,end,transfer,trailAlpha,time);
            SetTrail(trailHalo,start,control,end,transfer,trailAlpha*.36f,time);
            float hit=Mathf.Clamp01((time-.66f)/.34f),impact=Mathf.Sin(hit*Mathf.PI);
            Pose(pulse,end,Vector3.one*Mathf.Lerp(.8f,2.45f,hit),new Color(1,.83f,.5f,impact*.8f));
            Pose(ring,end,Vector3.one*Mathf.Lerp(.45f,2.2f,hit),new Color(1,.75f,.3f,impact*.75f));
            for(int i=0;i<motes.Length;i++)
            {
                float angle=i*Mathf.PI*2/motes.Length;Vector3 offset=(side*Mathf.Cos(angle)+up*Mathf.Sin(angle))*Mathf.Lerp(.15f,1.05f,hit);
                Pose(motes[i],end+offset,Vector3.one*(.10f+Mathf.Sin(i*2.1f)*.025f),new Color(1,.9f,.55f,impact));
            }
            visual.labelRoot.SetActive(time>.55f&&time<Duration);
            visual.labelRoot.transform.position=end+up*1.15f;
            if(viewCamera!=null)visual.labelRoot.transform.rotation=viewCamera.transform.rotation;
            visual.labelRoot.transform.localScale=Vector3.one*(.72f*(1-Mathf.Clamp01((time-.89f)/.13f)));
        }
        static void SetTrail(LineRenderer line,Vector3 start,Vector3 control,Vector3 end,float transfer,float alpha,float time)
        {
            line.enabled=time>.22f&&time<.83f;
            line.startColor=new Color(1,1,1,0);line.endColor=new Color(1,1,1,alpha);
            for(int i=0;i<20;i++)line.SetPosition(i,Curve(start,control,end,Mathf.Lerp(Mathf.Max(0,transfer-.7f),transfer,i/19f)));
        }
        void Pose(SpriteRenderer renderer,Vector3 position,Vector3 scale,Color color)
        {renderer.transform.position=position;renderer.transform.localScale=scale;renderer.color=color;if(viewCamera!=null)renderer.transform.rotation=viewCamera.transform.rotation;}
        static Vector3 Curve(Vector3 a,Vector3 b,Vector3 c,float t)=>(1-t)*(1-t)*a+2*(1-t)*t*b+t*t*c;
    }
}
