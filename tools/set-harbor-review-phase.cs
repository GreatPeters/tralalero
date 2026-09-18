using UnityEditor;public static class SetHarborReviewPhase{public static object Main(){SessionState.SetString("HarborRefinement.Phase","release");EditorApplication.isPaused=false;return true;}}
