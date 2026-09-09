var c=UnityEngine.Object.FindFirstObjectByType<IndianOceanAssets.ShooterSurvival.CanvasScript>();
if(!c.youWinUI.activeInHierarchy)throw new Exception("Clear screen required");
var button=c.youWinUI.transform.Find("Next Level").GetComponentInChildren<UnityEngine.UI.Button>();
if(!button.isActiveAndEnabled)throw new Exception("Replay action hidden");
button.onClick.Invoke();return "Invoked the existing PLAY AGAIN button action";
