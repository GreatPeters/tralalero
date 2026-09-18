var type=typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.GameView");
var view=UnityEditor.EditorWindow.GetWindow(type);
var flags=System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Static;
int width=SessionState.GetInt("Coastal.ViewWidth",1080),height=SessionState.GetInt("Coastal.ViewHeight",2340);
var method=type.GetMethod("SetCustomResolution",flags);
method.Invoke(method.IsStatic?null:view,new object[]{new Vector2(width,height),"Coastal UI "+width+"x"+height});view.Repaint();
return new{width,height};
