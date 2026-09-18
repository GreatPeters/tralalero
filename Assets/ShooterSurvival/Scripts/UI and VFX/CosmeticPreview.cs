using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public sealed class CosmeticPreview : MonoBehaviour
{
    public CosmeticVisualCatalog catalog;
    public RawImage display;
    public Texture2D platformTexture;
    public bool referencePresentation;
    private GameObject stage;
    private Transform model;
    private Camera previewCamera;
    private RenderTexture texture;
    private Material groundMaterial;
    private readonly List<Material> platformMaterials=new();
    private string currentSkin, currentShoes, currentHat;
    private float yaw;
    private int renderAfterFrame=-1;
    private readonly List<Mesh> poseMeshes=new();
    private readonly List<GameObject> poseObjects=new();
    private static readonly Vector3 Focus=new(0,1.05f,0),CameraPosition=new(-3.7f,2.65f,6.3f);
    public RenderTexture Texture => texture;

    public void Show(string skin, string shoes, string hat)
    {
        if (catalog == null || catalog.previewModel == null) return;
        EnsureStage();
        if(currentSkin!=skin || currentShoes!=shoes || currentHat!=hat)
        {
            ClearPose();CosmeticAppearance.Apply(model, catalog, skin, shoes, hat);BakePreviewPose();
            currentSkin=skin;currentShoes=shoes;currentHat=hat;
            RequestRender();
        }
        if (display != null) display.texture = texture;
    }
    private void BakePreviewPose()
    {
        // The off-screen manual camera probe showed a different GPU-skinned pose.
        // The cause was not isolated. Bake once per selection to render the verified
        // static pose consistently while the preview camera orbits.
        foreach(var source in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if(!source.gameObject.activeInHierarchy)continue;
            var mesh=new Mesh{name="Equipment preview pose"};source.BakeMesh(mesh,true);poseMeshes.Add(mesh);
            var view=new GameObject("__PreviewPose");view.layer=31;view.transform.SetParent(source.transform,false);poseObjects.Add(view);
            view.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=view.AddComponent<MeshRenderer>();renderer.sharedMaterials=source.sharedMaterials;
            source.enabled=false;
        }
    }
    private void ClearPose()
    {
        foreach(var view in poseObjects){if(view!=null)view.SetActive(false);Remove(view);}poseObjects.Clear();
        foreach(var mesh in poseMeshes)Remove(mesh);poseMeshes.Clear();
    }
    private void LateUpdate()
    {
        if(renderAfterFrame<0 || Time.frameCount<=renderAfterFrame || previewCamera==null)return;
        renderAfterFrame=-1;previewCamera.enabled=false;
    }
    private void RequestRender()
    {
        if (previewCamera == null) return;
        if (!Application.isPlaying) { previewCamera.Render(); return; }
        // Let URP schedule and resolve a normal camera render on Android.
        // Keep two actual frames for the freshly opened UI/layout, then stop idle rendering.
        previewCamera.enabled = true;
        renderAfterFrame = Time.frameCount + 1;
    }
    public void Rotate(float degrees)
    {
        if(stage==null)return;
        yaw+=degrees;
        previewCamera.transform.localPosition=Focus+Quaternion.Euler(0,yaw,0)*(CameraPosition-Focus)*(referencePresentation?.88f:1f);
        previewCamera.transform.LookAt(stage.transform.position+Focus);
        RequestRender();
    }
    public void ResetView(){yaw=0;Rotate(0);}
    private void EnsureStage()
    {
        if (stage != null) return;
        stage = new GameObject("__CosmeticPreview") { hideFlags = HideFlags.HideAndDontSave };
        stage.transform.position = new Vector3(10000, 10000, 10000);
        var instance = Instantiate(catalog.previewModel, stage.transform, false);
        model = instance.transform; model.localScale = Vector3.one * catalog.previewScale;
        model.localRotation = Quaternion.identity;
        foreach (var t in instance.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;
        foreach (var r in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { r.enabled = true; r.updateWhenOffscreen = true; }
        SetNeutralPose(instance);
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Remove(ground.GetComponent<Collider>());
        ground.transform.SetParent(stage.transform, false); ground.layer = 31;
        ground.transform.localScale = new Vector3(3.6f, .02f, 3.9f);
        groundMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        var theme = GameUITheme.Current;
        groundMaterial.SetColor("_BaseColor", platformTexture != null ? Color.white : theme != null ? Color.Lerp(theme.card, theme.accent, .32f) : new Color(.72f, .88f, .83f));
        if (platformTexture != null) groundMaterial.SetTexture("_BaseMap", platformTexture);
        ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;
        if(referencePresentation)
        {
            // Keep the wood top and the model's feet at their original height.
            PlatformPart("Blue enamel plinth",new Vector3(0,-.20f,0),new Vector3(3.72f,.19f,4.02f),new Color(.015f,.10f,.34f));
            PlatformPart("Upper chrome rim",new Vector3(0,-.025f,0),new Vector3(3.75f,.025f,4.05f),new Color(.20f,.65f,.95f));
            PlatformPart("Lower chrome rim",new Vector3(0,-.39f,0),new Vector3(3.75f,.022f,4.05f),new Color(.12f,.40f,.72f));
            var boltMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));boltMaterial.SetColor("_BaseColor",new Color(.66f,.77f,.88f));
            boltMaterial.SetFloat("_Metallic",.6f);boltMaterial.SetFloat("_Smoothness",.65f);platformMaterials.Add(boltMaterial);
            for(int i=0;i<16;i++){
                float angle=i*Mathf.PI*2/16;
                foreach(float y in new[]{-.11f,-.29f}){
                    var bolt=GameObject.CreatePrimitive(PrimitiveType.Sphere);bolt.name="Plinth rivet";Remove(bolt.GetComponent<Collider>());
                    bolt.layer=31;bolt.transform.SetParent(stage.transform,false);bolt.transform.localPosition=new Vector3(Mathf.Sin(angle)*1.863f,y,Mathf.Cos(angle)*2.013f);
                    bolt.transform.localScale=Vector3.one*.045f;bolt.GetComponent<Renderer>().sharedMaterial=boltMaterial;
                }
            }
        }
        var cameraObject = new GameObject("Preview Camera"); cameraObject.transform.SetParent(stage.transform, false);
        cameraObject.transform.localPosition = Focus+(CameraPosition-Focus)*(referencePresentation?.88f:1f);
        cameraObject.transform.LookAt(stage.transform.position + Focus);
        previewCamera = cameraObject.AddComponent<Camera>(); previewCamera.enabled = false;
        previewCamera.depth = -100; previewCamera.allowMSAA = false;
        previewCamera.cullingMask = 1 << 31; previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = Color.clear; previewCamera.fieldOfView = 32;previewCamera.aspect=4f/3f;
        previewCamera.nearClipPlane = .1f; previewCamera.farClipPlane = 20;
        previewCamera.allowHDR = false;
        texture = new RenderTexture(1024, 768, 24, RenderTextureFormat.ARGB32) { name = "Cosmetic preview", antiAliasing=1 };
        texture.Create(); previewCamera.targetTexture = texture;
        var lightObject = new GameObject("Preview Fill"); lightObject.transform.SetParent(stage.transform, false);
        lightObject.transform.localPosition = new Vector3(-2, 4, 3);
        var light = lightObject.AddComponent<Light>(); light.type = LightType.Point; light.range = 12;
        light.intensity = 3; light.cullingMask = 1 << 31; light.shadows = LightShadows.None;
    }
    private void PlatformPart(string name,Vector3 position,Vector3 scale,Color color)
    {
        var part=GameObject.CreatePrimitive(PrimitiveType.Cylinder);part.name=name;
        Remove(part.GetComponent<Collider>());part.layer=31;part.transform.SetParent(stage.transform,false);
        part.transform.localPosition=position;part.transform.localScale=scale;
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetColor("_BaseColor",color);
        material.SetFloat("_Metallic",.35f);material.SetFloat("_Smoothness",.60f);
        platformMaterials.Add(material);part.GetComponent<Renderer>().sharedMaterial=material;
    }
    private void SetNeutralPose(GameObject instance)
    {
        var renderer=instance.GetComponentInChildren<SkinnedMeshRenderer>();if(renderer==null)return;
        var bind=renderer.sharedMesh.bindposes;var bones=renderer.bones;if(bind.Length!=bones.Length)return;
        float scale=(renderer.transform.worldToLocalMatrix*bones[0].localToWorldMatrix*bind[0]).lossyScale.x;
        model.localScale*=scale;
        var poses=new Matrix4x4[bind.Length];for(int i=0;i<bind.Length;i++)poses[i]=renderer.transform.localToWorldMatrix*bind[i].inverse;
        for(int i=0;i<bones.Length;i++)
        {
            var bone=bones[i];var pose=poses[i];bone.SetPositionAndRotation(pose.GetColumn(3),pose.rotation);
            var parent=bone.parent.lossyScale;var size=pose.lossyScale;bone.localScale=new Vector3(size.x/parent.x,size.y/parent.y,size.z/parent.z);
        }
    }
    private void OnDisable() => Dispose();
    private void OnDestroy() => Dispose();
    public void Dispose()
    {
        ClearPose();
        if(stage!=null)stage.SetActive(false);
        if (display != null) display.texture = null;
        if (texture != null) texture.Release();
        Remove(stage); Remove(texture); Remove(groundMaterial);
        foreach(var material in platformMaterials)Remove(material);platformMaterials.Clear();
        stage = null; model=null; previewCamera=null; texture = null; groundMaterial = null;
        currentSkin=currentShoes=currentHat=null; yaw=0;renderAfterFrame=-1;
    }
    private static void Remove(Object value)
    {
        if (value == null) return;
        if (Application.isPlaying) Destroy(value); else DestroyImmediate(value);
    }
}
