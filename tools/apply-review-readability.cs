using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using IndianOceanAssets.ShooterSurvival;

public static class ApplyReviewReadability
{
    const string AssetsFolder="Assets/ShooterSurvival/Materials/Generated/ReviewReadability/";
    const string BackupFolder="tmp/review-fixes-20-runs-2026-09-22/before/";
    static Material womanMaterial,warningMaterial;
    public static string Main()
    {
        if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit Mode required");
        string original=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        Directory.CreateDirectory(AssetsFolder);Directory.CreateDirectory(BackupFolder);AssetDatabase.Refresh();
        var font=UnityEngine.Object.FindFirstObjectByType<DefeatPresentation>(FindObjectsInactive.Include).resultText.font;
        BuildWarningPrefab(font);warningMaterial=BuildRoadWarning();
        foreach(string path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/ShooterSurvival/Prefabs/Highway/Enemies"}).Select(AssetDatabase.GUIDToAssetPath).Concat(new[]{"Assets/JH/Model/Prefab/Enemy_Woman.prefab"}))
        {
            Backup(path);var root=PrefabUtility.LoadPrefabContents(path);
            try{if(path.Contains("Enemy_Woman"))Woman(root);else HighwayEnemy(root);PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        int women=0,highway=0;
        foreach(string name in new[]{"Noryangjin_MapTool_Mode_SR18","HighWay","RestStop"})
        {
            string path="Assets/ShooterSurvival/Scenes/Tools/"+name+".unity";Backup(path);var scene=EditorSceneManager.OpenScene(path);var roots=scene.GetRootGameObjects();
            foreach(var enemy in roots.SelectMany(r=>r.GetComponentsInChildren<EnemyScript_space>(true)))
            {
                var animator=enemy.GetComponentInChildren<Animator>(true);if(animator==null)continue;
                if(animator.name.Contains("Baker_in_Apron")){Woman(enemy.gameObject);women++;}
                else if(name=="HighWay"&&animator.name=="Body"){HighwayEnemy(enemy.gameObject);highway++;}
            }
            foreach(var defeat in roots.SelectMany(r=>r.GetComponentsInChildren<DefeatPresentation>(true)))Defeat(defeat);
            var map=roots.First(g=>g.name=="Noryangjin_MapTool");var camera=roots.SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).First(c=>c.CompareTag("MainCamera"));
            var occlusion=camera.GetComponent<NoryangjinCameraOcclusion>();
            var old=new SerializedObject(occlusion).FindProperty("additionalOccluderGroups");
            var existing=Enumerable.Range(0,old.arraySize).Select(i=>old.GetArrayElementAtIndex(i).objectReferenceValue as Transform).Where(t=>t!=null);
            var gantries=map.GetComponentsInChildren<Transform>(true).Where(t=>t.name.Contains("Harbor_lane_signal_gantry")||t.name.Contains("HighwayForkSign")||t.name=="Korean_Direction_Sign");
            var groups=existing.Concat(gantries).Distinct().ToArray();occlusion.ConfigureAdditionalOccluders(groups);Record(occlusion);
            foreach(var group in groups)foreach(var renderer in group.GetComponentsInChildren<Renderer>(true))
            {var flags=GameObjectUtility.GetStaticEditorFlags(renderer.gameObject)&~StaticEditorFlags.OccluderStatic;GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,flags);Record(renderer.gameObject);}
            foreach(var traffic in roots.SelectMany(r=>r.GetComponentsInChildren<HighwayOncomingTraffic>(true)))foreach(var line in traffic.warnings)
            {
                line.sharedMaterial=warningMaterial;line.startWidth=line.endWidth=2.2f;line.textureMode=LineTextureMode.Tile;line.textureScale=new Vector2(.33f,1);Record(line);
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(original);
        return $"Saved three scenes; Woman visuals {women}, highway actors {highway}; threat prefab, defeat recap, scoped gantry references and striped warnings.";
    }
    public static void Woman(GameObject root)
    {
        var animator=root.GetComponentInChildren<Animator>(true);var skin=animator.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if(womanMaterial==null)
        {
            string path=AssetsFolder+"WomanReadable.mat";womanMaterial=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(womanMaterial==null){womanMaterial=new Material(skin.sharedMaterial);womanMaterial.name="WomanReadable";AssetDatabase.CreateAsset(womanMaterial,path);}
            womanMaterial.shader=Shader.Find("FlatKit/Stylized Surface");womanMaterial.SetColor("_ColorDim",new Color(.66f,.70f,.77f));womanMaterial.SetFloat("_SelfShadingSize",.5f);womanMaterial.SetFloat("_ShadowEdgeSize",.12f);womanMaterial.SetFloat("_LightContribution",.15f);womanMaterial.SetFloat("_SpecularEnabled",0);womanMaterial.SetFloat("_OutlineEnabled",0);
            womanMaterial.EnableKeyword("_CELPRIMARYMODE_SINGLE");womanMaterial.EnableKeyword("_TEXTUREBLENDINGMODE_MULTIPLY");womanMaterial.DisableKeyword("DR_SPECULAR_ON");womanMaterial.DisableKeyword("DR_OUTLINE_ON");womanMaterial.SetShaderPassEnabled("Outline",false);EditorUtility.SetDirty(womanMaterial);
        }
        skin.sharedMaterial=womanMaterial;skin.forceMatrixRecalculationPerRender=true;Record(skin);
        var knife=root.GetComponentsInChildren<MeshFilter>(true).First(m=>m.name.Contains("Chef_s_Steel")).transform;
        var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);
        var fingers=hand.GetComponentsInChildren<Transform>().Where(t=>t.name.Contains("RightHandIndex")||t.name.Contains("RightHandThumb")).ToArray();
        var transforms=animator.GetComponentsInChildren<Transform>(true).Select(t=>new{t,p=t.localPosition,q=t.localRotation,s=t.localScale}).ToArray();
        var clip=animator.runtimeAnimatorController.animationClips.First(c=>c.name.Contains("RelaxedCarry"));clip.SampleAnimation(animator.gameObject,clip.length*.35f);
        var previousGrip=root.GetComponent<WomanKnifeGrip>();
        var rotations=previousGrip!=null&&previousGrip.fingerRotations!=null&&previousGrip.fingerRotations.Length==fingers.Length?previousGrip.fingerRotations:fingers.Select(t=>t.localRotation).ToArray();
        var positions=previousGrip!=null&&previousGrip.fingerPositions!=null&&previousGrip.fingerPositions.Length==fingers.Length?previousGrip.fingerPositions:fingers.Select(t=>t.localPosition).ToArray();
        foreach(var state in transforms){state.t.localPosition=state.p;state.t.localRotation=state.q;state.t.localScale=state.s;}
        var grip=root.GetComponent<WomanKnifeGrip>();if(grip==null)grip=root.AddComponent<WomanKnifeGrip>();grip.knife=knife;grip.fingers=fingers;grip.fingerRotations=rotations;grip.fingerPositions=positions;grip.gripInHand=new Vector3(-.027f,.127f,.055f);grip.fingerCurlDegrees=20;grip.ApplyGrip();
        Record(grip);Record(knife);foreach(var finger in fingers)Record(finger);
    }
    static void HighwayEnemy(GameObject root)
    {
        var animator=root.GetComponentInChildren<Animator>(true);if(animator==null)return;
        bool chief=root.name.Contains("TollgateChief");animator.transform.localScale=Vector3.one*(chief?1.54f:1.4f);Record(animator.transform);
        var props=animator.GetComponent<HighwayCharacterProportions>();if(props==null)props=animator.gameObject.AddComponent<HighwayCharacterProportions>();
        var bones=animator.GetComponentsInChildren<Transform>(true);props.leftHand=bones.FirstOrDefault(t=>t.name=="Hand.L");props.rightHand=bones.FirstOrDefault(t=>t.name=="Hand.R");props.head=bones.FirstOrDefault(t=>t.name=="Head");props.handScale=.82f;props.headScale=.94f;
        foreach(var hand in new[]{props.leftHand,props.rightHand})if(hand!=null){hand.localScale=Vector3.one*.82f;Record(hand);}
        if(props.head!=null){props.head.localScale=Vector3.one*.94f;Record(props.head);}Record(props);
        var cap=root.GetComponent<CapsuleCollider>();if(cap!=null){cap.height=Mathf.Max(cap.height,chief?2.7f:2.5f);var center=cap.center;center.y=cap.height*.5f;cap.center=center;Record(cap);}
    }
    static void Defeat(DefeatPresentation defeat)
    {
        var panel=defeat.resultText.transform.parent;var font=defeat.resultText.font;
        var coin=panel.Find("CoinEmblem") as RectTransform;if(coin!=null)Rect(coin,.43f,.79f,.57f,.885f);
        Rect(defeat.resultText.rectTransform,.08f,.39f,.92f,.50f);defeat.resultText.fontSize=34;Record(defeat.resultText);
        defeat.causeText=Text(panel,"DamageCause",font,42,.06f,.64f,.94f,.785f);defeat.causeText.text="마지막 피해  적과 충돌\n-120 HP";
        defeat.adviceText=Text(panel,"DamageAdvice",font,31,.08f,.52f,.92f,.64f);defeat.adviceText.text="접촉 전에 처치하거나 빈 쪽으로 피하세요.";
        defeat.buildText=Text(panel,"RunBuild",font,28,.06f,.345f,.94f,.395f);defeat.buildText.text="이번 판  공격력 68 · 최대 체력 500";
        Rect((RectTransform)defeat.rewardedButton.transform,.07f,.224f,.93f,.318f);Rect(defeat.adStatusText.rectTransform,.08f,.18f,.92f,.222f);
        Rect((RectTransform)defeat.continueButton.transform,.07f,.045f,.93f,.156f);Record(defeat);
    }
    static TMP_Text Text(Transform parent,string name,TMP_FontAsset font,float size,float x,float y,float xx,float yy)
    {
        var target=parent.Find(name);if(target==null){var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);target=go.transform;}
        var text=target.GetComponent<TMP_Text>();text.font=font;text.fontSize=size;text.enableAutoSizing=false;text.alignment=TextAlignmentOptions.Center;text.color=new Color(.09f,.14f,.22f);text.raycastTarget=false;text.textWrappingMode=TextWrappingModes.Normal;Rect(text.rectTransform,x,y,xx,yy);Record(text);return text;
    }
    static void BuildWarningPrefab(TMP_FontAsset font)
    {
        string folder="Assets/ShooterSurvival/Resources/CombatFeedback";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
        var go=new GameObject("ContactWarning");var text=go.AddComponent<TextMeshPro>();
        try
        {
            text.font=font;text.fontSize=3.4f;text.alignment=TextAlignmentOptions.Center;text.enableAutoSizing=false;text.rectTransform.sizeDelta=new Vector2(2.5f,1.2f);text.text="접촉 시\n사망";
            string path=AssetsFolder+"ContactWarningText.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(font.material);AssetDatabase.CreateAsset(material,path);}
            material.SetColor("_OutlineColor",new Color(.05f,.07f,.1f));material.SetFloat("_OutlineWidth",.22f);text.fontSharedMaterial=material;text.color=new Color(1,.3f,.2f);text.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            PrefabUtility.SaveAsPrefabAsset(go,folder+"/ContactWarning.prefab");EditorUtility.SetDirty(material);
        }
        finally{UnityEngine.Object.DestroyImmediate(go);}
    }
    static Material BuildRoadWarning()
    {
        string texturePath=AssetsFolder+"HazardStripes.asset";var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if(texture==null)
        {
            texture=new Texture2D(128,128,TextureFormat.RGBA32,false){name="HazardStripes",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Bilinear};var pixels=new Color[128*128];
            for(int y=0;y<128;y++)for(int x=0;x<128;x++)pixels[y*128+x]=((x+y)/20)%2==0?new Color(1,.45f,.025f,.78f):new Color(.17f,.10f,.035f,.55f);
            texture.SetPixels(pixels);texture.Apply();AssetDatabase.CreateAsset(texture,texturePath);
        }
        string path=AssetsFolder+"HazardStripes.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(mat,path);}
        mat.SetTexture("_BaseMap",texture);mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Surface",1);mat.SetFloat("_SrcBlend",5);mat.SetFloat("_DstBlend",10);mat.SetFloat("_ZWrite",0);mat.SetFloat("_Cull",0);mat.SetOverrideTag("RenderType","Transparent");mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");mat.renderQueue=3000;EditorUtility.SetDirty(mat);return mat;
    }
    static void Rect(RectTransform t,float x,float y,float xx,float yy){t.anchorMin=new Vector2(x,y);t.anchorMax=new Vector2(xx,yy);t.offsetMin=t.offsetMax=Vector2.zero;Record(t);}
    static void Record(UnityEngine.Object obj){EditorUtility.SetDirty(obj);if(PrefabUtility.IsPartOfPrefabInstance(obj))PrefabUtility.RecordPrefabInstancePropertyModifications(obj);}
    static void Backup(string path){string target=BackupFolder+path;Directory.CreateDirectory(Path.GetDirectoryName(target));if(!File.Exists(target))File.Copy(path,target);}
}
