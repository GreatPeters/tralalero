if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Edit Mode required");
if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new System.InvalidOperationException("Save or inspect current scene edits first");
return UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/HighWay.unity").name;
