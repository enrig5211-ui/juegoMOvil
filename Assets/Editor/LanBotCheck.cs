using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Valle.Lan;
[InitializeOnLoad]
public static class LanBotCheck {
 static int stage,slot=-1;static double next,deadline;static int shots;static LanPeer guest;static bool loaded;static StringBuilder log=new StringBuilder();
 static LanBotCheck(){EditorApplication.update+=Tick;}
 static void Check(bool ok,string text){log.AppendLine((ok?"PASS: ":"FAIL: ")+text);File.WriteAllText("Documentation/LAN-bots-check.txt",log.ToString());}
 static void Tick(){if(!SessionState.GetBool("LanBotTest",false)){if(!File.Exists("Temp/lan-bot-test.request")||EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete("Temp/lan-bot-test.request");EditorSceneManager.OpenScene("Assets/Scenes/MenuInicio.unity");SessionState.SetBool("LanBotTest",true);EditorApplication.isPlaying=true;return;}if(!EditorApplication.isPlaying)return;
  try{if(guest!=null){while(guest.Incoming.TryDequeue(out var json)){var p=JsonUtility.FromJson<LanPacket>(json);if(p.type=="welcome")slot=p.slot;if(p.type=="snapshot"&&p.phase=="match"&&!loaded){loaded=true;guest.Send(JsonUtility.ToJson(new LanPacket{type="loaded"}));}}guest.Send(JsonUtility.ToJson(new LanPacket{type="ping"}));}
   var s=LanSession.Instance;if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+1;
   switch(stage++){
    case 0:LanSession.Open();s=LanSession.Instance;s.Host();s.StartMatch();deadline=EditorApplication.timeSinceStartup+40;break;
    case 1:if(!s.World){stage--;return;}Check(s.Slots.Count(x=>x.bot)==7,"One human starts with seven bots");shots=s.World.Pawns.Values.Sum(p=>p.State.shot);break;
    case 2:if(EditorApplication.timeSinceStartup<deadline){stage--;return;}Check(s.World.Pawns.Values.Sum(p=>p.State.shot)>shots+10,"Bots navigate, acquire targets and fire during real arena simulation");Check(s.BlueScore+s.RedScore>0,"Bots can score kills in valley terrain");Check(s.World.Pawns.Values.All(p=>p.State.position.y> -5),"Combatants stay on terrain / respawn");ScreenCapture.CaptureScreenshot("Documentation/LAN-bots.png");break;
    case 3:var socket=new TcpClient();socket.Connect("127.0.0.1",7777);guest=new LanPeer(socket);guest.Send(JsonUtility.ToJson(new LanPacket{type="hello",name="Invitado tardío",team=1,role=1}));break;
    case 4:Check(slot>=4&&s.Slots.Count(x=>x.bot)==6,"Late join replaces a bot on requested team");Check(slot>=0&&s.Slots[slot].loaded&&s.World.Pawns[slot].Class.Health==160,"Late client loads and receives selected class");guest.Dispose();break;
    case 5:Check(s.Slots.Count(x=>x.bot)==7,"Late guest disconnect restores bot");Finish();break;
   }
  }catch(Exception e){Check(false,e.ToString());Finish();}
 }
 static void Finish(){guest?.Dispose();if(LanSession.Instance)LanSession.Instance.Leave();SessionState.SetBool("LanBotTest",false);EditorApplication.isPlaying=false;}
}
