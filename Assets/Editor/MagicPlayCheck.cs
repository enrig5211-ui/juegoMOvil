using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Valle;
using Valle.Lan;
[InitializeOnLoad]
public static class MagicPlayCheck {
 static int stage;static double next;static StringBuilder log=new StringBuilder();static Vector3 origin;static MagicFx protection;
 static MagicPlayCheck(){EditorApplication.update+=Tick;}
 static void Check(bool ok,string text){log.AppendLine((ok?"PASS: ":"FAIL: ")+text);File.WriteAllText("Documentation/Magic-play-check.txt",log.ToString());}
 static void Tick(){if(!SessionState.GetBool("MagicTest",false)){if(!File.Exists("Temp/magic-test.request")||File.Exists("Temp/magic-setup.request")||EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete("Temp/magic-test.request");EditorSceneManager.OpenScene("Assets/Scenes/MenuInicio.unity");SessionState.SetBool("MagicTest",true);EditorApplication.isPlaying=true;return;}if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.3;
  try{var s=LanSession.Instance;switch(stage++){
   case 0:ShaderUtil.allowAsyncCompilation=false;var palette=Resources.Load<MagicPalette>("Effects/MagicPalette");var prefabs=new[]{palette.windSpawn,palette.earthSpawn,palette.fireSpawn,palette.protection,palette.impact};Check(prefabs.All(p=>p),"Five PixPlays URP variants assigned");Check(prefabs.SelectMany(p=>p.GetComponentsInChildren<Renderer>(true)).All(r=>r.sharedMaterials.All(m=>m&&m.shader&&m.shader.isSupported&&!ShaderUtil.ShaderHasError(m.shader))),"All VFX materials and shaders supported");Check(prefabs.All(p=>p.GetComponentsInChildren<MonoBehaviour>(true).Length==0&&p.GetComponentsInChildren<Collider>(true).Length==0),"Visual prefabs contain no gameplay or vendor controllers");LanSession.Open();LanSession.Instance.Host();LanSession.Instance.StartMatch();break;
   case 1:if(!s.World){stage--;return;}origin=s.World.Pawns[s.LocalSlot].transform.position;MagicFx.Spawn(origin+Vector3.left*1.3f,0);MagicFx.Spawn(origin+Vector3.right*1.3f,1);MagicFx.Spawn(origin+Vector3.forward*1.5f,2);MagicFx.Impact(origin+Vector3.back);protection=MagicFx.Protect(s.World.Pawns[s.LocalSlot].transform,2);break;
   case 2:Check(MagicFx.ActiveCount>0,"Spawn/shield/impact effects activate during LAN match");Check(Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Any(p=>p.particleCount>0),"Particles actually emit");ScreenCapture.CaptureScreenshot("Documentation/Magic-in-game.png");Check(Vector3.Distance(protection.transform.position,s.World.Pawns[s.LocalSlot].transform.position+Vector3.up*.3f)<.05f,"Protection follows the player");protection.StopEffect();break;
   case 3:Check(!protection||!protection.gameObject.activeSelf,"Protection ends when gameplay protection ends");for(int i=0;i<70;i++)MagicFx.Impact(origin);Check(MagicFx.ActiveCount<=24,"Impact burst respects 24-instance cap");foreach(var pawn in s.World.Pawns.Values){pawn.State.hp=0;pawn.State.respawn=100;}next=EditorApplication.timeSinceStartup+2.5;break;
   case 4:Check(MagicFx.ActiveCount==0,"Expired one-shot effects are released for reuse");Finish();break;
  }}catch(System.Exception e){Check(false,e.ToString());Finish();}
 }
 static void Finish(){ShaderUtil.allowAsyncCompilation=true;if(LanSession.Instance)LanSession.Instance.Leave();SessionState.SetBool("MagicTest",false);EditorApplication.isPlaying=false;}
}
