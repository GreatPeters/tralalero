using UnityEditor;using UnityEngine;
public static class EnableHarborErrorTrace{public static object Main(){
 SessionState.SetInt("HarborRefinement.OriginalExceptionTrace",(int)Application.GetStackTraceLogType(LogType.Exception));
 Application.SetStackTraceLogType(LogType.Exception,StackTraceLogType.Full);Application.SetStackTraceLogType(LogType.Error,StackTraceLogType.ScriptOnly);EditorApplication.isPaused=false;return true;
}}
