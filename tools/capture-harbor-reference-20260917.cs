if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play Mode required");
OpeningStoryUI.Instance?.Skip();var canvas=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.CanvasScript>();
string scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;string folder="tmp/image-previews/harbor-reference-fidelity-2026-09-17/"+SessionState.GetString("HarborReference.Phase","candidate-1")+"/"+scene;System.IO.Directory.CreateDirectory(folder);
var hint=canvas.transform.Find("UI/Main/Center/StartHint");var finger=hint.Find("Finger").GetComponent<RectTransform>();
System.Collections.IEnumerator Capture(){
 hint.gameObject.SetActive(false);hint.gameObject.SetActive(true);
 yield return new WaitForSecondsRealtime(.4f);var a=finger.anchoredPosition;ScreenCapture.CaptureScreenshot(folder+"/01-lobby.png");
 yield return new WaitForSecondsRealtime(.4f);var b=finger.anchoredPosition;ScreenCapture.CaptureScreenshot(folder+"/02-hand-motion.png");
 System.IO.File.WriteAllText(folder+"/checks.txt","Finger moves independently: "+(Vector2.Distance(a,b)>5)+" A="+a+" B="+b+"\n");
 if(scene=="Noryangjin_MapTool_Mode_SR18"){
  string frames="tmp/image-previews/harbor-reference-fidelity-2026-09-17/hand-motion-frames";System.IO.Directory.CreateDirectory(frames);
  float min=float.PositiveInfinity,max=float.NegativeInfinity;var left=hint.Find("Left").localPosition;var right=hint.Find("Right").localPosition;
  for(int i=0;i<32;i++){min=Mathf.Min(min,finger.anchoredPosition.x);max=Mathf.Max(max,finger.anchoredPosition.x);ScreenCapture.CaptureScreenshot(frames+"/"+i.ToString("D3")+".png");yield return new WaitForSecondsRealtime(.10f);}
  System.IO.File.AppendAllText(folder+"/checks.txt","Hand cycle range="+(max-min)+"; arrows stationary="+(left==hint.Find("Left").localPosition&&right==hint.Find("Right").localPosition)+"\n");
  hint.gameObject.SetActive(false);bool centered=finger.anchoredPosition.sqrMagnitude<.01f;hint.gameObject.SetActive(true);System.IO.File.AppendAllText(folder+"/checks.txt","Reopen centered="+centered+"\n");
 }
 canvas.PlayerPressedStartButton();canvas.GetComponent<CoastalTutorialUI>().Close();var player=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.PlayerScript>();player.canShoot=false;
 yield return new WaitForSecondsRealtime(1.25f);IndianOceanAssets.ShooterSurvival.TimeManager.timeFactor=0;ScreenCapture.CaptureScreenshot(folder+"/03-merchant-greeting.png");
 yield return new WaitForSecondsRealtime(3);ScreenCapture.CaptureScreenshot(folder+"/04-merchant-seated.png");
 var merchant=UnityEngine.Object.FindFirstObjectByType<HarborMerchantGreeting>();if(merchant!=null)System.IO.File.AppendAllText(folder+"/checks.txt","Merchant greeted: "+merchant.HasGreeted+"\n");
 System.IO.File.AppendAllText(folder+"/checks.txt","COMPLETE\n");
}
canvas.StartCoroutine(Capture());return folder;
