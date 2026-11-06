using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Valle.Lan;
[InitializeOnLoad]
public static class LanClientCheck {
 static TcpListener listener;static LanPeer peer;static LanPacket snapshot;static int stage,inputs,readyPackets,classPackets;static double next,started,sendAt;static StringBuilder log=new StringBuilder();static Vector3 start;
 static LanClientCheck(){EditorApplication.update+=Tick;}
 static void Check(bool ok,string text){log.AppendLine((ok?"PASS: ":"FAIL: ")+text);File.WriteAllText("Documentation/LAN-client-check.txt",log.ToString());}
 static void Tick(){if(!SessionState.GetBool("LanClientTest",false)){if(!File.Exists("Temp/lan-client-test.request")||EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete("Temp/lan-client-test.request");EditorSceneManager.OpenScene("Assets/Scenes/MenuInicio.unity");SessionState.SetBool("LanClientTest",true);EditorApplication.isPlaying=true;return;}
  if(!EditorApplication.isPlaying)return;if(started==0)started=EditorApplication.timeSinceStartup;if(EditorApplication.timeSinceStartup-started>70){Check(false,"Client check timeout");Finish();return;}
  try{
   if(listener!=null&&peer==null&&listener.Pending())peer=new LanPeer(listener.AcceptTcpClient());
   if(peer!=null){while(peer.Incoming.TryDequeue(out var json)){var packet=JsonUtility.FromJson<LanPacket>(json);if(packet.type=="hello"){Check(packet.name=="Cliente prueba","Client sends handshake and name");peer.Send(JsonUtility.ToJson(new LanPacket{type="welcome",slot=5}));}if(packet.type=="input")inputs++;if(packet.type=="ready")readyPackets++;if(packet.type=="class")classPackets++;}if(snapshot!=null&&EditorApplication.timeSinceStartup>=sendAt){sendAt=EditorApplication.timeSinceStartup+.05;peer.Send(JsonUtility.ToJson(snapshot));}}
   if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+1;var s=LanSession.Instance;
   switch(stage++){
    case 0:listener=new TcpListener(IPAddress.Loopback,7777);listener.Start();LanSession.Open();s=LanSession.Instance;s.PlayerName="Cliente prueba";s.Team=1;s.Role=1;s.Join("127.0.0.1");snapshot=new LanPacket{type="snapshot",phase="lobby",remaining=300,slots=Enumerable.Range(0,8).Select(i=>new LanSlot{id=i,team=i/4,name=i==5?"Cliente prueba":"Bot "+i,bot=i!=5,ready=true,loaded=true,role=i%3}).ToArray()};break;
    case 1:if(s.Phase!="lobby"){stage--;return;}Check(!s.IsHost&&s.LocalSlot==5&&s.Slots.Length==8,"Real client receives assigned red slot and lobby");s.Ready();s.ChooseClass(2);break;
    case 2:Check(readyPackets==1&&classPackets==1,"Ready and class changes sent to host");snapshot.phase="match";snapshot.actors=Enumerable.Range(0,8).Select(i=>new LanActorState{id=i,position=new Vector3(i-4,.05f,-3),hp=100,stamina=80,weapon=2,ammo=17,reserve=80,aim=Vector3.forward}).ToArray();snapshot.drops=new[]{new LanDrop{id=1,weapon=1,ammo=7,reserve=20,position=new Vector3(1,0,-2)}};break;
    case 3:if(!s.World){stage--;return;}Check(s.World.Pawns.Count==8&&s.World.Drops.Count==1,"Client builds eight avatars and received pickup");Check(s.World.Pawns.Values.All(p=>!p.GetComponent<CharacterController>().enabled),"Client cannot simulate authoritative controllers");Check(s.World.Pawns[5].State.ammo==17,"Client HUD receives ammo state");start=s.World.Pawns[5].transform.position;snapshot.actors[5].position+=Vector3.right;break;
    case 4:Check(s.World.Pawns[5].transform.position.x>start.x+.7f,"Remote snapshots interpolate avatars");Check(inputs>0,"Client sends movement/aim input packets");Check(Camera.main.GetComponent<Valle.PlayerCamera>().target==s.World.Pawns[5].transform,"Camera follows local client only");s.MenuOpen=true;break;
    case 5:Check(Time.timeScale==1,"Online options never pause shared world");s.MenuOpen=false;snapshot.phase="result";snapshot.blue=20;break;
    case 6:Check(s.Phase=="result","Client receives match results");snapshot.phase="lobby";snapshot.actors=null;snapshot.drops=null;break;
    case 7:if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="MenuInicio"){stage--;return;}Check(s.Phase=="lobby","Client returns to lobby with host");peer.Dispose();snapshot=null;break;
    case 8:Check(s.Phase=="offline"&&s.Status.Contains("conexión"),"Host disconnect shows recovery message");s.Leave();break;
    case 9:Finish();break;
   }
  }catch(Exception e){Check(false,e.ToString());Finish();}
 }
 static void Finish(){peer?.Dispose();listener?.Stop();if(LanSession.Instance)LanSession.Instance.Leave();SessionState.SetBool("LanClientTest",false);EditorApplication.isPlaying=false;}
}
