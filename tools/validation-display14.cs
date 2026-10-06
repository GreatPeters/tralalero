using System;using System.IO;using System.Linq;using System.Reflection;using System.Runtime.InteropServices;using UnityEditor;using UnityEngine;
public static class ValidationDisplay14 {
 const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static|BindingFlags.FlattenHierarchy;
 const string Root="outputs/chapter45-detailed-design-2026-10-03/validation-cycle14/";
 [StructLayout(LayoutKind.Sequential)]struct Pt{public int x,y;}
 [DllImport("user32.dll")]static extern bool SetCursorPos(int x,int y);
 [DllImport("user32.dll")]static extern bool GetCursorPos(out Pt p);
 [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
 public sealed class Item{public string name;public object value;}
 public sealed class State{public int windowId,focusedId;public int[] cursor;public long foreground;public uint foregroundPid;public float[] position;public bool maximized;public Item[] values;}
 static Type J=>AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert");
 static string Json(object x)=>(string)J.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{x});
 static T Decode<T>(string s)=>(T)J.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)}).Invoke(null,new object[]{s,typeof(T)});
 static EditorWindow View()=>Resources.FindObjectsOfTypeAll(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Cast<EditorWindow>().Single();
 static object Get(object o,string n)=>o.GetType().GetProperty(n,F)?.GetValue(o)??o.GetType().GetField(n,F)?.GetValue(o);
 static void Set(object o,string n,object x)=>o.GetType().GetProperty(n,F).SetValue(o,x);
 public static object Size(string index){int i=int.Parse(index);if(i!=22&&i!=23)throw new Exception("Only existing two supported portrait presets");var w=View();Set(w,"selectedSizeIndex",i);w.Repaint();return new{index=i};}
 public static object RestoreSize(){var s=Decode<State>(File.ReadAllText(Root+"original-display-state.json"));var w=View();if(w.GetInstanceID()!=s.windowId)throw new Exception("GameView instance changed");Set(w,"selectedSizeIndex",Convert.ToInt32(s.values.Single(x=>x.name=="selectedSizeIndex").value));return new{restoredSize=true};}
 public static object Restore(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Clean Edit required");var s=Decode<State>(File.ReadAllText(Root+"original-display-state.json"));var w=View();if(w.GetInstanceID()!=s.windowId||w.maximized!=s.maximized||w.position!=new Rect(s.position[0],s.position[1],s.position[2],s.position[3]))throw new Exception("Unexpected window layout changed");var z=Get(w,"m_ZoomArea");var items=Decode<Item[]>(Json(s.values.Single(x=>x.name=="m_ZoomArea").value));var area=Decode<float[]>(Json(items.Single(x=>x.name=="shownArea").value));Set(z,"shownArea",new Rect(area[0],area[1],area[2],area[3]));Set(w,"lowResolutionForAspectRatios",Convert.ToBoolean(s.values.Single(x=>x.name=="lowResolutionForAspectRatios").value));GetWindowThreadProcessId(GetForegroundWindow(),out var pid);if(pid!=s.foregroundPid)throw new Exception("External foreground changed; do not steal it");GetCursorPos(out var beforeCursor);string owned=SessionState.GetString("Chapter45.Validation14.Cursor",s.cursor[0]+","+s.cursor[1]);if(owned!=beforeCursor.x+","+beforeCursor.y)throw new Exception("Cursor ownership lost; preserve external cursor");var focus=Resources.FindObjectsOfTypeAll<EditorWindow>().Single(x=>x.GetInstanceID()==s.focusedId);focus.Focus();if(!SetCursorPos(s.cursor[0],s.cursor[1]))throw new Exception("Cursor restore failed");GetCursorPos(out var current);SessionState.EraseString("Chapter45.Validation14.Cursor");w.Repaint();return new{restored=true,index=Get(w,"selectedSizeIndex"),cursor=new[]{current.x,current.y},foregroundPid=pid,focusedId=focus.GetInstanceID(),zoomScale=new[]{((Vector2)Get(z,"scale")).x,((Vector2)Get(z,"scale")).y},zoomTranslation=new[]{((Vector2)Get(z,"translation")).x,((Vector2)Get(z,"translation")).y}};}
}
