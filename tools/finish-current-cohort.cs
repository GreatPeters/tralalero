var type=typeof(Sr18ProgressionPlaytest);
var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic;
var config=type.GetField("config",flags).GetValue(null);
if(config==null)throw new System.InvalidOperationException("No current cohort");
int attempt=(int)config.GetType().GetField("attempt").GetValue(config);
config.GetType().GetField("maxRuns").SetValue(config,attempt);
type.GetMethod("SaveConfig",flags).Invoke(null,null);
return new{finishAfterCurrentAttempt=attempt,status=Sr18ProgressionPlaytest.Status()};
