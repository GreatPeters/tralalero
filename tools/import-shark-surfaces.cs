// Remove only this task's delayed imports: headless editors may never drain delayCall.
int removed=0;
foreach(var callback in UnityEditor.EditorApplication.delayCall?.GetInvocationList() ?? System.Array.Empty<System.Delegate>())
{
    bool owned=false;
    try
    {
        var bytes=callback.Method.GetMethodBody()?.GetILAsByteArray();
        if(bytes!=null)for(int i=0;i+4<bytes.Length;i++)if(bytes[i]==0x72)
        {
            try{string value=callback.Method.Module.ResolveString(System.BitConverter.ToInt32(bytes,i+1));if(value=="BuildAndRecord"||value.Contains("skins-reststop-2026-09-12/unity-surface-import"))owned=true;}catch{}
        }
    }
    catch{}
    if(owned){UnityEditor.EditorApplication.delayCall-=(UnityEditor.EditorApplication.CallbackFunction)callback;removed++;}
}
// Reflection avoids stale metadata references in the live eval compiler after a reload.
typeof(SharkSurfaceImporter).GetMethod("BuildAndRecord").Invoke(null,null);
return new {removed,report="map-concepts/skins-reststop-2026-09-12/unity-surface-import.json"};
