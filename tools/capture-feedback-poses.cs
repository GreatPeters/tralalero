if(!EditorApplication.isPlaying)throw new InvalidOperationException("Pose capture requires disposable Play Mode");
        string folder="tmp/image-previews/feedback-2026-09-19/"+SessionState.GetString("Feedback.Phase","before");
        System.IO.Directory.CreateDirectory(folder);
        var cameraObject=new GameObject("Feedback review camera");
        var camera=cameraObject.AddComponent<Camera>();camera.CopyFrom(Camera.main);camera.enabled=false;
        System.Collections.IEnumerator Capture() { try {
            var merchant=UnityEngine.Object.FindFirstObjectByType<HarborMerchantGreeting>();
            var actor=UnityEngine.Object.FindObjectsByType<IndianOceanAssets.ShooterSurvival.EnemyEventController>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(x=>x.name=="SR18_L_E01_T005_Enemy_OldMan_Right");
            foreach(var animator in new[]{merchant.animator,actor.GetComponentInChildren<Animator>(true)}) {
                bool shop=animator==merchant.animator;
                var clip=shop?animator.runtimeAnimatorController.animationClips.First(c=>c.name.Contains("StandGreet")):animator.runtimeAnimatorController.animationClips.First(c=>c.name.Contains("CalmIdle"));
                foreach(string state in shop?new[]{"StandGreet"}:new[]{"idle","walk","attack_loop"}) foreach(float time in shop?new[]{0f,1.2f,2.2f}:new[]{0f,.2f,.6f}) {


                        animator.speed=1;animator.Play(state,0,shop?time/clip.length:time);animator.Update(.001f);animator.speed=0;yield return new WaitForEndOfFrame();
                        Transform root=shop?merchant.transform:actor.transform;
                        var center=root.position+Vector3.up*(shop?2.6f:1.1f);
                        camera.transform.position=center+(shop?root.forward:animator.transform.forward)*(shop?10:6)+(shop?root.right:animator.transform.right)*(shop?2:1.8f)+Vector3.up*(shop?1.2f:.65f);
                        camera.transform.LookAt(center);camera.fieldOfView=38;
                        Shot(camera,folder+"/"+(shop?"merchant":"shovel")+"-"+state+"-"+time.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+".png");

                }
            }
        } finally {UnityEngine.Object.DestroyImmediate(cameraObject);} }

    void Shot(Camera camera,string path) {
        if(System.IO.File.Exists(path))throw new System.IO.IOException("Preserve evidence: "+path);
        var rt=new RenderTexture(1000,1000,24);var tex=new Texture2D(1000,1000,TextureFormat.RGB24,false);var prev=RenderTexture.active;
        try{rt.Create();camera.targetTexture=rt;camera.aspect=1;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1000,1000),0,0);tex.Apply();System.IO.File.WriteAllBytes(path,tex.EncodeToPNG());}
        finally{camera.targetTexture=null;RenderTexture.active=prev;UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
    }

UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.CanvasScript>().StartCoroutine(Capture());return folder;






