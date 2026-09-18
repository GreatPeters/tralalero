if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play Mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var canvas=scene.GetRootGameObjects().Single(g=>g.name=="Canvas").GetComponent<IndianOceanAssets.ShooterSurvival.CanvasScript>();var story=canvas.GetComponentInChildren<OpeningStoryUI>(true);
string folder="tmp/image-previews/ui-text-alignment-2026-09-17/"+scene.name+"/"+Screen.width+"x"+Screen.height;System.IO.Directory.CreateDirectory(folder);string report=folder+"/checks.txt";System.IO.File.WriteAllText(report,"");
void Check(bool value,string label){System.IO.File.AppendAllText(report,(value?"PASS ":"FAIL ")+label+"\n");if(!value)throw new Exception(label);}
Vector2 GlyphCenter(TMPro.TMP_Text text){text.ForceMeshUpdate();var glyphs=text.textInfo.characterInfo.Take(text.textInfo.characterCount).Where(c=>c.isVisible).ToArray();return new Vector2((glyphs.Min(c=>c.bottomLeft.x)+glyphs.Max(c=>c.topRight.x))*.5f,(glyphs.Min(c=>c.bottomLeft.y)+glyphs.Max(c=>c.topRight.y))*.5f);}
System.Collections.IEnumerator Ready(){float end=Time.realtimeSinceStartup+12;while((story.IsSeeking||!story.IsMoviePlaying||story.video.frame<0)&&Time.realtimeSinceStartup<end)yield return null;Check(!story.IsSeeking&&story.IsMoviePlaying,"Movie seek complete");}
System.Collections.IEnumerator Shot(string name){yield return new WaitForSecondsRealtime(.25f);Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot(folder+"/"+name+".png");yield return new WaitForSecondsRealtime(.35f);}
System.Collections.IEnumerator Capture(){
 story.Open();yield return Ready();
 for(int i=0;i<4;i++){
  yield return new WaitForSecondsRealtime(.12f);Canvas.ForceUpdateCanvases();
  foreach(var text in new[]{story.chapterText,story.titleText,story.captionText,story.nextText}.Concat(story.sceneIndicators.SelectMany(x=>x.GetComponentsInChildren<TMPro.TMP_Text>()))){
   var error=Vector2.Distance(GlyphCenter(text),text.rectTransform.rect.center);Check(error<.35f,"Glyph center "+text.name+" page="+i+" error="+error.ToString("F3"));
  }
  Check(!story.captionText.isTextOverflowing,"Caption fits page "+i);
  if(i==2){
   var group=story.previousButton.transform.Find("AlignedContent").GetComponent<RectTransform>();var label=group.GetComponentInChildren<TMPro.TMP_Text>();var icon=group.Find("ReplayIcon").GetComponent<RectTransform>();
   Check(label.rectTransform.rect.width>100&&group.rect.width>200,"Previous content resolved its layout");
   var parent=story.previousButton.transform;var labelCenter=parent.InverseTransformPoint(label.transform.TransformPoint(GlyphCenter(label)));var iconCenter=parent.InverseTransformPoint(icon.TransformPoint(icon.rect.center));
   Check(Mathf.Abs(labelCenter.y-iconCenter.y)<.5f,"Previous icon and text share the same visual center line");Check(group.anchoredPosition.sqrMagnitude<.01f,"Previous content group is centered in button");yield return Shot("01-story-scene-3");
  }
  if(i<3){story.Next();yield return Ready();}
 }
 story.Previous();yield return Ready();Check(story.CurrentPage==2,"Previous navigation preserved");story.Skip();yield return Shot("02-lobby");
 var shop=canvas.GetComponentInChildren<CosmeticShopUI>(true);shop.Open();yield return new WaitForSecondsRealtime(.15f);shop.Close();yield return Shot("03-lobby-return");
 System.IO.File.AppendAllText(report,"COMPLETE\n");
}
canvas.StartCoroutine(Capture());return folder;
