if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Edit Mode required");
bool ready=GameDataWorkbookEditor.EnsureRuntimeArchiveCurrent(false);var status=GameDataWorkbookEditor.GetRuntimeArchiveStatus(out string detail);
return new{ready,status=status.ToString(),detail};
