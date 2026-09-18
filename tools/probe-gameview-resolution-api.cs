var assembly = typeof(UnityEditor.EditorWindow).Assembly;
return new[] { "UnityEditor.GameViewSizes", "UnityEditor.GameViewSize", "UnityEditor.GameViewSizeGroup", "UnityEditor.GameView" }.Select(name => {
    var type = assembly.GetType(name);
    return new { name, constructors = type.GetConstructors(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Select(c=>c.ToString()).ToArray(),
        members = type.GetMembers(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Static)
            .Where(m=>m.Name.Contains("Group")||m.Name.Contains("Custom")||m.Name.Contains("Builtin")||m.Name.Contains("SizeIndex")||m.Name=="instance").Select(m=>m.ToString()).ToArray() };
}).ToArray();
