var opening = OpeningStoryUI.Instance;
var font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/JH/Font/쩡야공유/GmarketSansTTFBold SDF2.asset");
foreach (var text in opening.GetComponentsInChildren<TMPro.TMP_Text>(true)) { text.font = font; text.enableAutoSizing = false; text.color = new UnityEngine.Color(.12f,.065f,.025f,1); }
opening.titleText.fontSize = 43; opening.captionText.fontSize = 31; opening.chapterText.fontSize = 27; opening.nextText.fontSize = 35;
UnityEngine.Canvas.ForceUpdateCanvases();
return ChapterVisualProbe.Capture("tmp/qa-proof/opening-workshop-font.png");
