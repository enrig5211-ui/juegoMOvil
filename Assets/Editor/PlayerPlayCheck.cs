using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Valle;
using System.IO;
using System.Text;
[InitializeOnLoad]
public static class PlayerPlayCheck {
 static int stage=-1;static float start;static Vector3 origin;static float stamina,yaw,highest;static int stepCount;static bool silentAir=true;static StringBuilder report=new StringBuilder();
 static PlayerPlayCheck(){EditorApplication.delayCall+=Begin;EditorApplication.update+=Tick;}
 [MenuItem("Tools/Player/Comprobar en Play")]
 public static void Request(){if(EditorApplication.isPlaying)return;File.WriteAllText("Temp/player-test.request", "run");Begin();}
 static void Begin(){if(!File.Exists("Temp/player-test.request")||EditorApplication.isPlaying)return;File.Delete("Temp/player-test.request");stage=-1;report.Clear();SessionState.SetBool("VallePlayerTest",true);EditorApplication.isPlaying=true;}
 static void Keys(params Key[] keys){if(Keyboard.current!=null)InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(keys));}
 static void Check(bool condition,string label){report.AppendLine((condition?"PASS: ":"FAIL: ")+label);}
 static void Next(){stage++;start=Time.time;}
 static void Tick(){if(!SessionState.GetBool("VallePlayerTest",false)||!EditorApplication.isPlaying)return;var motor=Object.FindFirstObjectByType<PlayerMotor>();if(!motor)return;var stats=motor.GetComponent<PlayerStats>();var cam=Object.FindFirstObjectByType<PlayerCamera>();float elapsed=Time.time-start;
 if(stage<0){var cc=motor.GetComponent<CharacterController>();cc.enabled=false;motor.transform.position=new Vector3(0,.08f,-3.4641f);cc.enabled=true;Keys();Next();return;}
 switch(stage){
 case 0:if(elapsed<.5f)return;Check(motor.Grounded,"Player stands on terrain");origin=motor.transform.position;stepCount=motor.GetComponent<PlayerFootsteps>().PlayedSteps;Keys(Key.W);Next();break;
 case 1:if(elapsed<.55f)return;Check(motor.GetComponent<PlayerFootsteps>().PlayedSteps>stepCount,"Footstep clips trigger while walking");Check(Vector3.Distance(origin,motor.transform.position)>.5f,"W moves player relative to camera");Keys();Next();break;
 case 2:if(elapsed<.3f)return;origin=motor.transform.position;stamina=stats.Stamina;highest=origin.y;stepCount=motor.GetComponent<PlayerFootsteps>().PlayedSteps;silentAir=true;Keys(Key.Space);Next();break;
 case 3:if(!motor.Grounded&&motor.GetComponent<PlayerFootsteps>().PlayedSteps!=stepCount)silentAir=false;highest=Mathf.Max(highest,motor.transform.position.y);if(elapsed>.12f)Keys();if(elapsed<1.1f)return;Check(silentAir,"No footsteps in midair");Check(highest-origin.y>.5f,"Jump gains height");Check(motor.Grounded,"Jump lands on terrain");Check(stats.Stamina<stamina,"Jump consumes stamina");yaw=cam.yaw;Keys(Key.Q);Next();break;
 case 4:if(elapsed<.4f)return;Check(cam.yaw<yaw-10,"Q rotates camera");Check(Mathf.Abs(cam.pitch-45)<.01f,"Camera pitch is 45 degrees");Keys(Key.E);yaw=cam.yaw;Next();break;
 case 5:if(elapsed<.4f)return;Check(cam.yaw>yaw+10,"E rotates camera opposite direction");stamina=stats.Stamina;Keys(Key.W,Key.LeftShift);Next();break;
 case 6:if(elapsed<.5f)return;Check(stats.Stamina<stamina,"Sprint consumes stamina");Keys();stamina=stats.Stamina;Next();break;
 case 7:if(elapsed<1.6f)return;Check(stats.Stamina>stamina,"Stamina regenerates after delay");stats.TakeDamage(23);Check(Mathf.Abs(stats.Health-80)<.1f,"Defense 15 mitigates damage: 23 raw -> 20");stats.TakeDamage(10000);Check(stats.IsDead,"Health clamps at zero and death activates");Keys(Key.R);Next();break;
 case 8:if(elapsed<.3f)return;Keys();Check(!stats.IsDead&&stats.Health==stats.maxHealth,"R respawns and restores stats");Check(motor.visual!=motor.transform,"Animation deforms visual only, not collision root");var music=Object.FindFirstObjectByType<AmbientMusic>();var audio=music.GetComponent<AudioSource>();Check(audio.isPlaying&&audio.loop&&audio.clip.name.Contains("Moonspire"),"Moonspire playing in loop");Check(Mathf.Abs(audio.volume-.12f)<.01f,"Music fades to soft 12 percent volume");Check(audio.clip.loadType==AudioClipLoadType.Streaming,"Music uses streaming");File.WriteAllText("Documentation/Player-play-check.txt",report.ToString());SessionState.SetBool("VallePlayerTest",false);EditorApplication.isPlaying=false;break;
 }
 }
}
