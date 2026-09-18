var type = typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.GameView");
var view = UnityEditor.EditorWindow.GetWindow(type);
var method = type.GetMethod("SetCustomResolution", System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Static);
method.Invoke(method.IsStatic ? null : view, new object[]{new UnityEngine.Vector2(576,1024),"Chapter QA 576x1024"});
view.Repaint();
return new { width=576,height=1024,index=type.GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(view) };
