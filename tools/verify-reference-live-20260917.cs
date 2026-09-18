if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play Mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var canvas=scene.GetRootGameObjects().Single(g=>g.name=="Canvas").GetComponent<IndianOceanAssets.ShooterSurvival.CanvasScript>();
var hint=canvas.transform.Find("UI/Main/Center/StartHint");var finger=hint.Find("Finger").GetComponent<RectTransform>();var left=hint.Find("Left").GetComponent<RectTransform>();var right=hint.Find("Right").GetComponent<RectTransform>();
string path="map-concepts/harbor-reference-fidelity-2026-09-17/live-"+scene.name+".txt";System.IO.File.WriteAllText(path,"");
void Check(bool ok,string message){System.IO.File.AppendAllText(path,(ok?"PASS ":"FAIL ")+message+"\n");if(!ok)throw new Exception(message);}
System.Collections.IEnumerator Verify(){
 OpeningStoryUI.Instance?.Skip();if(IndianOceanAssets.ShooterSurvival.TimeManager.isGameRunning)throw new Exception("Start this check from a fresh lobby");yield return new WaitForSecondsRealtime(.1f);
 var start=finger.anchoredPosition;var l=left.anchoredPosition;var r=right.anchoredPosition;
 yield return new WaitForSecondsRealtime(.35f);Check(Vector2.Distance(start,finger.anchoredPosition)>5,"Hand moves in idle lobby");Check(l==left.anchoredPosition&&r==right.anchoredPosition,"Upper arrows remain stationary");
 hint.gameObject.SetActive(false);Check(finger.anchoredPosition.sqrMagnitude<.01f,"Disable restores centered hand");hint.gameObject.SetActive(true);Check(finger.anchoredPosition.sqrMagnitude<.01f,"Reopen begins from center without drift");
 if(scene.name=="Noryangjin_MapTool_Mode_SR18"){
  var merchant=UnityEngine.Object.FindFirstObjectByType<HarborMerchantGreeting>();var head=merchant.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Head");var to=head.position-Camera.main.transform.position;
  var blocks=Physics.RaycastAll(Camera.main.transform.position,to.normalized,to.magnitude-.2f).Where(h=>h.collider.transform.IsChildOf(scene.GetRootGameObjects().Single(g=>g.name=="Noryangjin_MapTool").transform.Find("Roads"))).ToArray();
  Check(blocks.Length==0,"No pier post between gameplay camera and merchant head");Check(Camera.main.WorldToViewportPoint(head.position).x>.04f,"Merchant head inside portrait frame");
  var skin=merchant.GetComponentInChildren<SkinnedMeshRenderer>();var baked=new Mesh();skin.BakeMesh(baked);float minY=baked.vertices.Min(v=>skin.transform.TransformPoint(v).y);UnityEngine.Object.Destroy(baked);System.IO.File.AppendAllText(path,"Merchant mesh minimum Y="+minY+"; seated state="+merchant.animator.GetCurrentAnimatorStateInfo(0).shortNameHash+"\n");
  Check(merchant.animator.GetCurrentAnimatorStateInfo(0).IsName("SeatedIdle"),"Merchant is seated before departure");
  string frames="tmp/image-previews/harbor-reference-fidelity-2026-09-17/hand-motion-frames";System.IO.Directory.CreateDirectory(frames);
  for(int i=0;i<32;i++){ScreenCapture.CaptureScreenshot(frames+"/"+i.ToString("D3")+".png");yield return new WaitForSecondsRealtime(.10f);}
 }
 System.IO.File.AppendAllText(path,"COMPLETE\n");
}
canvas.StartCoroutine(Verify());return path;
