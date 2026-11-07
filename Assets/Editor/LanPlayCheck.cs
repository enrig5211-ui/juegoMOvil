using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Valle.Lan;
[InitializeOnLoad]
public static class LanPlayCheck {
 sealed class Client:IDisposable {
  public LanPeer Peer;public int Slot=-1;public LanPacket Last;public LanInput Input=new LanInput();public bool Loaded;public LanDrop[] Drops=new LanDrop[0];double nextSend;
  public Client(int index){var socket=new TcpClient();socket.Connect("127.0.0.1",7777);Peer=new LanPeer(socket);Send(new LanPacket{type="hello",name="Prueba "+index,team=index<3?0:1,role=index%3});}
  public void Send(LanPacket p)=>Peer.Send(JsonUtility.ToJson(p));
  public void Tick(){while(Peer.Incoming.TryDequeue(out var json)){var p=JsonUtility.FromJson<LanPacket>(json);if(p.type=="welcome")Slot=p.slot;if(p.type=="snapshot"){Last=p;if(p.hasDrops)Drops=p.drops??new LanDrop[0];if(p.phase=="match"&&!Loaded){Loaded=true;Send(new LanPacket{type="loaded"});}}}if(EditorApplication.timeSinceStartup<nextSend)return;nextSend=EditorApplication.timeSinceStartup+.05;Send(new LanPacket{type=Loaded?"input":"ping",input=Input});}
  public void Dispose()=>Peer.Dispose();
 }
 static List<Client> clients=new List<Client>();static Client extra;static StringBuilder log=new StringBuilder();static int stage;static double next,started;static Vector3 before;static float healthBefore;static GameObject ground,cover;static int storedAmmo,reinforcements;
 static LanPlayCheck(){EditorApplication.update+=Tick;}
 [MenuItem("Tools/Multijugador/Comprobar LAN en Play")]
 static void Request(){if(EditorApplication.isPlayingOrWillChangePlaymode)return;if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;EditorSceneManager.OpenScene("Assets/Scenes/MenuInicio.unity");File.WriteAllText("Temp/lan-test.request","");}
 static void Check(bool ok,string message){log.AppendLine((ok?"PASS: ":"FAIL: ")+message);File.WriteAllText("Documentation/LAN-play-check.txt",log.ToString());}
 static void Tick(){if(!SessionState.GetBool("LanTest",false)){if(!File.Exists("Temp/lan-test.request")||File.Exists("Temp/lan-setup.request")||EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Assets/Scenes/NocheChapare.unity"))return;File.Delete("Temp/lan-test.request");SessionState.SetBool("LanTest",true);EditorApplication.isPlaying=true;return;}
  if(!EditorApplication.isPlaying)return;if(started==0)started=EditorApplication.timeSinceStartup;if(EditorApplication.timeSinceStartup-started>100){Check(false,"Test timed out");Finish();return;}
  try{foreach(var client in clients)client.Tick();extra?.Tick();if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.8;var s=LanSession.Instance;
   switch(stage++){
    case 0:LanSession.Open();s=LanSession.Instance;s.PlayerName="Anfitrión prueba";s.Host();Check(s.IsHost&&s.Slots.Length==8&&s.Slots.Count(x=>x.bot)==7,"Host creates eight slots, seven bots");Check(LanClass.Get(0).Speed>LanClass.Get(1).Speed&&LanClass.Get(1).Health>LanClass.Get(0).Health&&LanClass.Get(2).Reserve>LanClass.Get(0).Reserve,"Three distinct classes");for(int i=0;i<7;i++)clients.Add(new Client(i));break;
    case 1:if(clients.Any(c=>c.Slot<0)){stage--;return;}Check(s.Slots.Count(x=>!x.bot)==8&&s.Slots.Count(x=>x.team==0)==4&&s.Slots.Count(x=>x.team==1)==4,"Eight humans and balanced 4v4");Check(clients.Select(c=>c.Slot).Distinct().Count()==7,"Unique server-assigned slots");Check(!s.CanStart,"Host waits for ready players");extra=new Client(8);foreach(var c in clients)c.Send(new LanPacket{type="ready"});clients[6].Send(new LanPacket{type="leader"});clients[0].Send(new LanPacket{type="leader"});break;
    case 2:Check(extra.Peer.Closed&&s.Slots.Count(x=>!x.bot)==8,"Ninth connection rejected");extra.Dispose();extra=null;Check(s.CanStart,"All ready enables start");Check(s.Night.Leader==clients[6].Slot,"Organization player claims leader and DEA claim is refused");ScreenCapture.CaptureScreenshot("Documentation/LAN-lobby.png");break;
    case 3:s.StartMatch();break;
    case 4:if(!s.World){stage--;return;}Check(s.World.Pawns.Count==8,"Eight network combatants in arena");Check(s.Night.Deploying,"Match opens in deployment");s.Night.Deploy=0;Check(clients.All(c=>c.Last!=null&&c.Last.phase=="match"),"Match start reaches every client");Check(clients.All(c=>c.Last.actors!=null&&c.Last.actors.Length==8),"Snapshots include all actors");
     ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.name="Test floor";ground.transform.position=new Vector3(100,-.5f,100);ground.transform.localScale=new Vector3(20,1,20);
     var p=s.World.Pawns[clients[0].Slot];p.ResetLife(new Vector3(100,.05f,100));before=p.State.position;clients[0].Input.x=1;break;
    case 5:var moving=s.World.Pawns[clients[0].Slot];Check(moving.State.position.x>before.x+.3f,"Remote input moves authoritative controller");Check(clients[0].Last.actors.First(a=>a.id==clients[0].Slot).position.x>before.x+.1f,"Authoritative position replicated");clients[0].Input.x=0;
     moving.ResetLife(new Vector3(100,.05f,100));s.World.Pawns[4].ResetLife(new Vector3(102,.05f,100));s.World.Pawns[4].State.shield=0;healthBefore=s.World.Pawns[4].State.hp;clients[0].Input.ax=1;clients[0].Input.fire=true;break;
    case 6:Check(s.World.Pawns[4].State.hp<healthBefore,"Remote fire damages enemy on host");Check(s.World.Pawns[clients[0].Slot].State.ammo<s.World.Pawns[clients[0].Slot].Class.Magazine,"Server consumes ammunition");clients[0].Input.fire=false;clients[0].Input.reload++;break;
    case 7:Check(s.World.Pawns[clients[0].Slot].State.reload>0,"Reload synchronized");next=EditorApplication.timeSinceStartup+1.8;break;
    case 8:var shooter=s.World.Pawns[clients[0].Slot];Check(shooter.State.ammo==shooter.Class.Magazine&&shooter.State.reserve<shooter.Class.Reserve,"Reload transfers reserve on host");storedAmmo=shooter.State.ammo;clients[0].Input.drop++;break;
    case 9:Check(s.World.Pawns[clients[0].Slot].State.weapon==0&&Weapons(s)==1,"Remote drop replicated");Check(clients[1].Drops.Count(d=>d.item==0)==1,"Other clients receive dropped weapon");clients[0].Input.pickup++;break;
    case 10:Check(s.World.Pawns[clients[0].Slot].State.weapon==2&&s.World.Pawns[clients[0].Slot].State.ammo==storedAmmo,"Pickup preserves ammunition without duplication");
     cover=GameObject.CreatePrimitive(PrimitiveType.Cube);cover.transform.position=new Vector3(101,.5f,100);cover.transform.localScale=new Vector3(.2f,1,1);Physics.SyncTransforms();healthBefore=s.World.Pawns[4].State.hp;clients[0].Input.fire=true;break;
    case 11:Check(s.World.Pawns[4].State.hp==healthBefore,"Cover blocks network damage");clients[0].Input.fire=false;UnityEngine.Object.DestroyImmediate(cover);
     var ally=s.World.Pawns[2];ally.ResetLife(new Vector3(101,.05f,100));ally.State.shield=0;healthBefore=ally.State.hp;s.World.Fire(s.World.Pawns[clients[0].Slot],Vector3.right);Check(ally.State.hp==healthBefore,"No friendly fire");ally.ResetLife(s.World.Spawn(2));
     var victim=s.World.Pawns[4];victim.State.shield=0;for(int i=0;i<20&&victim.State.hp>0;i++)s.World.Fire(s.World.Pawns[clients[0].Slot],Vector3.right);Check(victim.State.hp<=0&&s.BlueScore==1&&Weapons(s)==1,"Kill scores once and drops weapon");reinforcements=s.Night.OrgLeft;s.Night.Wave=.1f;break;
    case 12:Check(s.World.Pawns[4].State.hp>0&&s.World.Pawns[4].State.weapon==2&&s.Night.OrgLeft==reinforcements-1,"Reinforcement wave restores class loadout and spends one");foreach(var pawn in s.World.Pawns.Values)pawn.ResetLife(s.World.Spawn(pawn.Id));clients[6].Dispose();before=s.World.Pawns[clients[6].Slot].State.position;UnityEngine.Object.DestroyImmediate(ground);break;
    case 13:Check(s.Slots[clients[6].Slot].bot&&s.Night.Leader==clients[6].Slot,"Disconnected leader continues as bot");next=EditorApplication.timeSinceStartup+3;break;
    case 14:Check(Vector3.Distance(before,s.World.Pawns[clients[6].Slot].State.position)>.3f,"Bot leader moves using baked navigation");ScreenCapture.CaptureScreenshot("Documentation/LAN-match.png");s.Remaining=.01f;break;
    case 15:Check(s.Phase=="result"&&s.Night.Outcome==LanOutcome.Dawn,"Dawn finishes round");Check(clients[0].Last.phase=="result","Results replicated to clients");s.ReturnLobby();break;
    case 16:Check(s.Phase=="lobby"&&UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="MenuInicio","Return to lobby retains session");Check(s.Slots.Count(x=>x.bot)==1,"Bot replacement persists in room");s.Leave();break;
    case 17:Check(clients.Take(6).All(c=>c.Peer.Closed),"Host exit closes client sockets");Finish();break;
   }
  }catch(Exception e){Check(false,e.ToString());Finish();}
 }
 static int Weapons(LanSession s)=>s.World.Drops.Count(d=>d.item==0);
 static void Finish(){foreach(var c in clients)c.Dispose();extra?.Dispose();if(LanSession.Instance)LanSession.Instance.Leave();SessionState.SetBool("LanTest",false);EditorApplication.isPlaying=false;}
}
