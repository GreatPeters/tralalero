from pathlib import Path

source = Path("tools/verify-noryangjin-revamp-fix.cs").read_text(encoding="utf-8")
header = source[:source.index("public static class")]
body = source[source.index("    public static object Begin("):]
prefix = '''public static class PlayNoryangjinFeedbackV3
{
    const string Root = "outputs/noryangjin-feedback-v3-2026-09-28";
    const string Preview = "tmp/image-previews/noryangjin-feedback-v3-2026-09-28";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static string Json(object value) => (string)AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert").GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{value});
    public static object Inside()=>Begin("inside-"+DateTime.Now.ToString("HHmmss"),0,true,1);
    public static object Outside()=>Begin("outside-"+DateTime.Now.ToString("HHmmss"),1,true,1);
'''
Path("tools/play-noryangjin-feedback-v3.cs").write_text(header+prefix+body,encoding="utf-8")
