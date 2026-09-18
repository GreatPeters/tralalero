var type = typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.GameView");
var view = UnityEditor.EditorWindow.GetWindow(type);
var method = type.GetMethod("SetCustomResolution", System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Static);
method.Invoke(method.IsStatic ? null : view, new object[]{new UnityEngine.Vector2(576,1024),"Road Pattern QA 576x1024"});
return RoadPatternPlaytest.Begin("accepted-reststop-late-chapter", "RestStop", 0, 75, false, 37, 46);
