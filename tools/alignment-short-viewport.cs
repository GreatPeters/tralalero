var type=typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.GameView");var view=UnityEditor.EditorWindow.GetWindow(type);var flags=System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Static;
if(SessionState.GetInt("Alignment.OriginalWidth",0)==0){SessionState.SetInt("Alignment.OriginalWidth",Screen.width);SessionState.SetInt("Alignment.OriginalHeight",Screen.height);}
var method=type.GetMethod("SetCustomResolution",flags);method.Invoke(method.IsStatic?null:view,new object[]{new Vector2(1080,1920),"Alignment short phone"});view.Repaint();SessionState.SetString("FaithfulUI.Phase","short-phone");return true;

