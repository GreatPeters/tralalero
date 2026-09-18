if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Edit Mode required");
const string path="Assets/JH/UI/Opening/Animated/Curse_Opening_Animated.mp4";UnityEditor.AssetDatabase.ImportAsset(path,UnityEditor.ImportAssetOptions.ForceUpdate|UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var clip=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>(path);if(clip==null||clip.frameCount!=484||clip.width!=576||clip.height!=1024)throw new System.InvalidOperationException("Opening import mismatch");
var opening=UnityEngine.Object.FindFirstObjectByType<OpeningStoryUI>(UnityEngine.FindObjectsInactive.Include);if(opening.movie!=clip)throw new System.InvalidOperationException("Scene references a different opening");
return new{clip.frameCount,clip.length,clip.width,clip.height};
