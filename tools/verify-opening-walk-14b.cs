if(!UnityEditor.EditorApplication.isPlaying||UnityEditor.EditorApplication.isPaused)throw new System.InvalidOperationException("Unpaused Play Mode required");
var opening=UnityEngine.Object.FindFirstObjectByType<OpeningStoryUI>(UnityEngine.FindObjectsInactive.Include);opening.Open();
const string folder="tmp/image-previews/opening-walk-14b-2026-09-12";System.IO.Directory.CreateDirectory(folder);string capture=folder+"/installed-in-game.png";
int phase=0;double deadline=UnityEditor.EditorApplication.timeSinceStartup+35;UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>{
 try{
  if(UnityEditor.EditorApplication.timeSinceStartup>deadline)throw new System.TimeoutException("Opening playback verification");
  if(phase==0){if(!opening.IsMoviePlaying)return;opening.Next();opening.Next();opening.Next();phase++;return;}
  if(phase==1){if(!opening.IsMoviePlaying||opening.video.time<16||opening.video.frame<384||opening.CurrentPage!=3)return;UnityEngine.ScreenCapture.CaptureScreenshot(capture);phase++;return;}
  if(!System.IO.File.Exists(capture))return;
  var report="{\"frame\":"+opening.video.frame+",\"page\":"+opening.CurrentPage+",\"seconds\":"+opening.video.time.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"playing\":true,\"clipFrames\":"+opening.movie.frameCount+"}";
  System.IO.File.WriteAllText("map-concepts/opening-walk-14b-2026-09-12/installed/unity-playback.json",report);opening.Skip();UnityEditor.EditorApplication.update-=tick;
 }catch(System.Exception error){UnityEditor.EditorApplication.update-=tick;opening.Skip();System.IO.File.WriteAllText("map-concepts/opening-walk-14b-2026-09-12/installed/playback-error.txt",error.ToString());}
};UnityEditor.EditorApplication.update+=tick;return "Checking final14B shot in game";
