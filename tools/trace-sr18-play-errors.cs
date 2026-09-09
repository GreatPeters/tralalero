const string key="SR18.LivePlaytest.20260909";
foreach(LogType type in new[]{LogType.Exception,LogType.Error,LogType.Warning}) {
 UnityEditor.SessionState.SetInt(key+".stack."+type,(int)Application.GetStackTraceLogType(type));
 Application.SetStackTraceLogType(type,StackTraceLogType.Full);
}
string folder=UnityEditor.SessionState.GetString(key+".folder","");
Application.LogCallback handler=null;
handler=(message,stack,type)=>{
 if(type==LogType.Exception||type==LogType.Error||type==LogType.Warning)
 System.IO.File.AppendAllText(folder+"/runtime-errors.txt",DateTime.UtcNow.ToString("O")+" "+type+"\n"+message+"\n"+stack+"\n");
};
Application.logMessageReceived+=handler;
UnityEditor.EditorApplication.CallbackFunction cleanup=null;
cleanup=()=>{if(UnityEditor.EditorApplication.isPlaying)return;Application.logMessageReceived-=handler;UnityEditor.EditorApplication.update-=cleanup;foreach(LogType type in new[]{LogType.Exception,LogType.Error,LogType.Warning})Application.SetStackTraceLogType(type,(StackTraceLogType)UnityEditor.SessionState.GetInt(key+".stack."+type,0));};
UnityEditor.EditorApplication.update+=cleanup;
return new{trace=true,wasPaused=UnityEditor.EditorApplication.isPaused};
