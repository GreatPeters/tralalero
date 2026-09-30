using System.IO;
using UnityEditor;
using UnityEngine;

// Captures the next exceptions with script stack traces (this project logs exceptions without stacks)
// into tmp/claude-probe/exceptions.txt, unpauses Play, and restores StackTraceLogType.None afterwards.
//   unity command --project-path . run_script --file tools/claude-catch-exceptions.cs --entry ClaudeCatchExceptions.Main
public static class ClaudeCatchExceptions
{
    static int count;
    const string File0 = "tmp/claude-probe/exceptions.txt";
    public static object Main()
    {
        count = 0; Directory.CreateDirectory("tmp/claude-probe"); File.WriteAllText(File0, "");
        Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.ScriptOnly);
        Application.logMessageReceived -= Handle; Application.logMessageReceived += Handle;
        EditorApplication.isPaused = false;
        return "hooked";
    }
    static void Handle(string condition, string stack, LogType type)
    {
        if (type != LogType.Exception && type != LogType.Error) return;
        File.AppendAllText(File0, $"[{type}] {condition}\n{stack}\n----\n");
        if (++count >= 6)
        {
            Application.logMessageReceived -= Handle;
            Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.None);
        }
    }
}
