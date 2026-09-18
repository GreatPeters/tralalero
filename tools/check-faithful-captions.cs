var canvas=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.CanvasScript>();var story=canvas.GetComponentInChildren<OpeningStoryUI>(true);string path="map-concepts/harbor-faithful-art-2026-09-17/captions-"+Screen.width+"x"+Screen.height+".txt";
System.Collections.IEnumerator Check(){story.Open();float until=Time.realtimeSinceStartup+12;while(!story.IsMoviePlaying&&Time.realtimeSinceStartup<until)yield return null;
 for(int i=0;i<4;i++){
  until=Time.realtimeSinceStartup+8;while(story.IsSeeking&&Time.realtimeSinceStartup<until)yield return null;yield return new WaitForSecondsRealtime(.15f);Canvas.ForceUpdateCanvases();story.captionText.ForceMeshUpdate();
  if(story.captionText.isTextOverflowing)throw new Exception("Caption overflow at scene "+i);
  System.IO.File.AppendAllText(path,"PASS page="+story.CurrentPage+" font="+story.captionText.fontSize+" lines="+story.captionText.textInfo.lineCount+" fit="+story.movieDisplay.GetComponent<UnityEngine.UI.AspectRatioFitter>().aspectMode+"\n");if(i<3)story.Next();
 }
 story.Skip();System.IO.File.AppendAllText(path,"COMPLETE\n");
}
System.IO.File.WriteAllText(path,"");canvas.StartCoroutine(Check());return path;
