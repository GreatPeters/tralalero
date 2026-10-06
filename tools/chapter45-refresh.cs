using UnityEditor;
public static class Chapter45Refresh { public static object Main(){ AssetDatabase.Refresh(); return new{refreshRequested=true}; } }
