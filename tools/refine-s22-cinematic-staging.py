"""Revise the offline staging tool; never overwrite supplied production movies here."""
from pathlib import Path
p=Path(__file__).with_name('render-s22-cinematics.cs')
s=p.read_text(encoding='utf-8')
s=s.replace('s22-polish-2026-10-01/cinematics"','s22-polish-2026-10-01/cinematics-v2"')
s=s.replace('go.transform.localRotation=Quaternion.Euler(0,yaw,0);','go.transform.localRotation=Quaternion.Euler(0,yaw,0)*go.transform.localRotation;')
s=s.replace('camera.targetTexture=null;rt.Release();Object.Destroy(root);','camera.enabled=false;camera.targetTexture=null;rt.Release();Object.Destroy(root);')
s=s.replace('new Vector3(0,4.3f,18)','new Vector3(0,4.3f,-2)')
s=s.replace('new Vector3(0,4.3f,17.8f)','new Vector3(0,4.3f,-1.8f)')
s=s.replace('  var keyLight=', '''  if(shot==3)s.root.transform.Find("Readable sign").localRotation=Quaternion.Euler(0,180,0);
  if(shot==5){foreach(Transform t in s.root.transform.Cast<Transform>().Where(t=>t.name=="Food hall"||t.name=="Hall windows").ToArray())Object.Destroy(t.gameObject);const string prod="Assets/ShooterSurvival/Prefabs/RestStopProduction20260925/";for(int i=-4;i<=4;i++){if(i==0)continue;s.Prop(prod+"B05.prefab",new Vector3(i*2.6f,0,14),4.6f);if(i%2==0)s.Prop(prod+"B04.prefab",new Vector3(i*2.6f-1.3f,0,14),4.8f);}s.Prop(prod+"B06.prefab",new Vector3(0,0,13.8f),4.6f);}
  var keyLight=''')
s=s.replace(' public static object Preview()=>Begin(true);',' public static object Preview()=>Begin(true);\n public static object PreviewEntries()=>Begin(true,3);\n public static object RecordEntries()=>Begin(false,3);')
s=s.replace('static object Begin(bool preview){','static object Begin(bool preview,int firstShot=0){')
s=s.replace('Guard(Run(preview))','Guard(Run(preview,firstShot))')
s=s.replace('static IEnumerator Run(bool preview){var world=Camera.main;bool enabled=world.enabled;world.enabled=false;','static IEnumerator Run(bool preview,int firstShot){var world=Camera.main;int worldMask=world.cullingMask;world.cullingMask=0;')
s=s.replace('for(int shot=0;shot<6;shot++)','for(int shot=firstShot;shot<6;shot++)')
s=s.replace('world.enabled=enabled;Time.captureFramerate=oldCapture;','world.cullingMask=worldMask;Time.captureFramerate=oldCapture;')
s=s.replace('Six native animated shots completed;24fps;opening 8/12/10/9s;entries121frames.','Native animated selection completed;24fps;shot3=9s;entries121frames. Check selected directories.')
s=s.replace('p.Animate(time<1.2f?"idle":"run"','p.Animate(time<1.2f?"call":"run"')
p.write_text(s,encoding='utf-8')
