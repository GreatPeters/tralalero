var api=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEditor.TestTools.TestRunner.Api.TestRunnerApi")).First(t=>t!=null);
bool active=(bool)api.GetMethod("IsRunActive",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,null);
return new{testRunActive=active,playing=UnityEditor.EditorApplication.isPlaying};
