using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Valle.Lan;
// Comprueba la Etapa 2: luz, visión, linternas, items, bengalas, alarmas y su paso por la red.
[InitializeOnLoad]
public static class LanVisionCheck {
 sealed class Guest:IDisposable {
  public LanPeer Peer;public int Slot=-1;public LanPacket Last;public LanInput Input=new LanInput();public bool SawItems,SawMarks;public int Partial;bool loaded;double nextSend;
  public Guest(){var socket=new TcpClient();socket.Connect("127.0.0.1",7777);Peer=new LanPeer(socket);Send(new LanPacket{type="hello",name="Guardia",team=1});}
  public void Send(LanPacket packet)=>Peer.Send(JsonUtility.ToJson(packet));
  public void Tick(){
   while(Peer.Incoming.TryDequeue(out var json)){
    var p=JsonUtility.FromJson<LanPacket>(json);
    if(p.type=="welcome")Slot=p.slot;
    if(p.type!="snapshot")continue;
    Last=p;
    if(p.hasDrops&&p.drops!=null&&p.drops.Any(d=>d.item>0))SawItems=true;
    if(!p.hasDrops&&p.phase=="match")Partial++;
    if(p.marks!=null&&p.marks.Length>0)SawMarks=true;
    if(p.phase=="match"&&!loaded){loaded=true;Send(new LanPacket{type="loaded"});}
   }
   if(EditorApplication.timeSinceStartup<nextSend)return;
   nextSend=EditorApplication.timeSinceStartup+.05;
   Send(new LanPacket{type=loaded?"input":"ping",input=Input});
  }
  public void Dispose()=>Peer.Dispose();
 }
 static readonly Vector3 Arena=new Vector3(100,.05f,100);
 static readonly StringBuilder log=new StringBuilder();
 static Guest guest;static int stage;static double next,started;static LanNightCycle cycle;
 static LanVisionCheck(){EditorApplication.update+=Tick;}
 [MenuItem("Tools/Multijugador/Comprobar noche, vision e items")]
 static void Request(){File.WriteAllText("Temp/lan-vision-test.request","");}
 static void Check(bool ok,string text){log.AppendLine((ok?"PASS: ":"FAIL: ")+text);File.WriteAllText("Documentation/LAN-vision-check.txt",log.ToString());}
 static void Tick(){
  if(!SessionState.GetBool("LanVisionTest",false)){
   if(!File.Exists("Temp/lan-vision-test.request")||EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)return;
   File.Delete("Temp/lan-vision-test.request");EditorSceneManager.OpenScene("Assets/Scenes/MenuInicio.unity");SessionState.SetBool("LanVisionTest",true);EditorApplication.isPlaying=true;return;
  }
  if(!EditorApplication.isPlaying)return;
  if(started==0)started=EditorApplication.timeSinceStartup;
  if(EditorApplication.timeSinceStartup-started>150){Check(false,"Vision check timed out");Finish();return;}
  try{guest?.Tick();if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.5;Advance(LanSession.Instance);}
  catch(Exception e){Check(false,e.ToString());Finish();}
 }
 static void Run(LanSession s,float seconds){for(int i=0;i<Mathf.CeilToInt(seconds/.05f)&&s.Phase=="match";i++)s.Step(.05f);}
 static void Place(LanPawn pawn,Vector3 at){pawn.ResetLife(at);pawn.State.shield=0;Physics.SyncTransforms();}
 static LanPawn G(LanSession s)=>s.World.Pawns[guest.Slot];
 static bool Sees(LanSession s,LanPawn a,LanPawn b)=>LanVision.Sees(s.World,a,b);
 static void Advance(LanSession s){
  switch(stage++){
   case 0:LanSession.Open();s=LanSession.Instance;s.PlayerName="Agente";s.Host();guest=new Guest();break;
   case 1:if(guest.Slot<0){stage--;return;}guest.Send(new LanPacket{type="ready"});break;
   case 2:s.StartMatch();break;
   case 3:{if(!s.World){stage--;return;}
    int items=s.World.Drops.Count(d=>d.item>0);
    Check(s.World.itemSpots.Length>=12&&items==s.World.itemSpots.Length,"Items appear on every spot ("+items+")");
    Check(s.World.lamps.Length>0&&s.World.IsLit(s.World.lamps[0].position),"Camp lamp posts light their area");
    float dusk=LanVision.Daylight(s);Check(dusk>.3f&&dusk<=LanNightRules.DuskLight,"Deployment happens at dusk");
    s.Night.Deploy=.05f;Run(s,.1f);
    Check(LanVision.Daylight(s)==0,"Night is fully dark");
    float clock=s.Remaining;s.Remaining=LanNightRules.DawnSeconds/2;
    Check(Mathf.Approximately(LanVision.Daylight(s),.5f),"Dawn brightens during the last minutes");
    s.Remaining=clock;s.Night.Wave=9999;
    var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Suelo de prueba";floor.transform.position=new Vector3(100,-.5f,100);floor.transform.localScale=new Vector3(40,1,40);Physics.SyncTransforms();
    break;}
   case 4:{
    var viewer=s.World.Pawns[1];var g=G(s);
    Place(viewer,Arena);viewer.State.aim=Vector3.right;
    Place(g,Arena+Vector3.right*3);Check(Sees(s,viewer,g),"Close enemies are visible in the dark");
    Place(g,Arena+Vector3.right*8);Check(!Sees(s,viewer,g),"Without light, enemies beyond 5 units stay hidden");
    viewer.State.lamp=true;Check(Sees(s,viewer,g),"A flashlight reveals enemies inside its cone");
    Place(g,Arena+Vector3.forward*8);Check(!Sees(s,viewer,g),"A flashlight does not reveal outside its cone");viewer.State.lamp=false;
    Place(g,Arena+Vector3.right*18);g.State.lamp=true;Check(Sees(s,viewer,g),"A lit flashlight gives away its carrier");
    Place(g,Arena+Vector3.right*23);g.State.lamp=true;Check(!Sees(s,viewer,g),"Flashlights give away their carrier only up to 20 units");
    viewer.State.vision=10;Place(g,Arena+Vector3.right*10);Check(Sees(s,viewer,g),"Night vision extends sight");viewer.State.vision=0;
    Place(g,Arena+Vector3.right*3);var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=Arena+Vector3.right*1.5f+Vector3.up*.5f;wall.transform.localScale=new Vector3(.2f,2,2);Physics.SyncTransforms();
    Check(!Sees(s,viewer,g),"Cover blocks sight");UnityEngine.Object.DestroyImmediate(wall);Physics.SyncTransforms();
    Place(g,Arena+Vector3.right*15);g.MarkShot();Check(Sees(s,viewer,g),"Firing reveals the shooter");
    s.Night.Clock+=1;Check(!Sees(s,viewer,g),"The muzzle flash reveals only briefly");
    s.World.AddMark(LanItems.FlareMark,0,g.transform.position,5);Check(Sees(s,viewer,g),"Flares reveal enemies near them");s.World.Marks.Clear();
    Place(g,Arena+Vector3.right*8);Check(s.World.Enemy(viewer)==null,"Bots cannot see beyond 5 units in the dark");
    Place(g,Arena+Vector3.right*3);Check(s.World.Enemy(viewer)==g,"Bots see close enemies");
    viewer.ResetLife(s.World.Spawn(viewer.Id));
    Place(g,Arena+Vector3.forward*6);s.World.PlaceItem(g.transform.position,LanItems.Medkit);guest.Input.pickup++;
    break;}
   case 5:{var g=G(s);Check(g.State.item==LanItems.Medkit,"Picking up stores the item");g.State.hp=g.Class.Health;guest.Input.use++;break;}
   case 6:{var g=G(s);Check(g.State.item==LanItems.Medkit,"A medkit is kept at full health");g.State.hp=50;guest.Input.use++;break;}
   case 7:{var g=G(s);Check(g.State.item==0&&Mathf.Approximately(g.State.hp,Mathf.Min(g.Class.Health,50+LanNightRules.MedkitHeal)),"A medkit heals and is spent");
    s.World.PlaceItem(g.transform.position,LanItems.Ammo);guest.Input.pickup++;break;}
   case 8:{var g=G(s);Check(g.State.item==LanItems.Ammo,"Ammunition can be picked up");
    s.World.PlaceItem(g.transform.position+Vector3.right*.2f,LanItems.Flare);guest.Input.pickup++;break;}
   case 9:{var g=G(s);
    Check(g.State.item==LanItems.Flare&&s.World.Drops.Any(d=>d.item==LanItems.Ammo&&Vector3.Distance(d.position,g.transform.position)<2),"Picking up another item swaps it");
    g.State.aim=Vector3.right;guest.Input.use++;break;}
   case 10:{var g=G(s);var flare=s.World.Marks.FirstOrDefault(m=>m.kind==LanItems.FlareMark);
    Check(flare!=null&&g.State.item==0&&Vector3.Distance(flare.position,g.transform.position+Vector3.right*LanNightRules.FlareThrow)<1.5f,"A flare is thrown ahead");
    int thrown=flare!=null?flare.id:-1;Run(s,LanNightRules.FlareSeconds+.5f);Check(!s.World.Marks.Any(m=>m.id==thrown),"Flares burn out");
    g.State.item=LanItems.NightVision;guest.Input.use++;break;}
   case 11:{var g=G(s);Check(g.State.vision>LanNightRules.NightVisionSeconds-3,"Night vision goggles activate");
    g.State.item=LanItems.Alarm;guest.Input.use++;break;}
   case 12:{var g=G(s);var alarm=s.World.Marks.FirstOrDefault(m=>m.kind==LanItems.AlarmMark);
    Check(alarm!=null&&alarm.team==LanNightRules.Org&&g.State.item==0,"An alarm is placed for its team");
    var ally=s.World.Pawns.Values.First(p=>p.Team==LanNightRules.Org&&p!=g&&!p.IsLeader);
    Place(ally,alarm.position+Vector3.right*2);Run(s,.2f);Check(!alarm.fired,"Allies do not trigger their own alarm");
    ally.ResetLife(s.World.Spawn(ally.Id));var agent=s.World.Pawns[1];Place(agent,alarm.position+Vector3.right*2);Run(s,.2f);
    Check(alarm.fired,"An enemy triggers the alarm");agent.ResetLife(s.World.Spawn(agent.Id));
    g.State.item=LanItems.Medkit;g.State.hp=0;g.Fall();
    Check(g.State.item==0&&s.World.Drops.Any(d=>d.item==LanItems.Medkit&&Vector3.Distance(d.position,g.transform.position)<2.5f),"Fallen players drop their item");
    s.World.Drops.RemoveAll(d=>d.item>0);s.Night.Wave=.05f;Run(s,.1f);s.Night.Wave=9999;
    Check(s.World.Drops.Count(d=>d.item>0)==LanNightRules.RefillPerWave,"Each wave restocks empty item spots");
    Place(g,Arena+Vector3.forward*6);guest.Input.lamp++;break;}
   case 13:{var g=G(s);Check(g.State.lamp,"T toggles the flashlight over the network");
    Check(guest.SawItems&&guest.SawMarks,"Clients receive items and marks");
    Check(guest.Partial>0,"Snapshots skip unchanged items");
    s.MenuOpen=true;var local=s.World.Pawns[s.LocalSlot];Place(local,Arena+Vector3.back*12);
    Place(g,local.transform.position+Vector3.right*8);
    foreach(var p in s.World.Pawns.Values)if(p.Team==LanNightRules.Dea&&p!=local)p.ResetLife(s.World.Spawn(p.Id));
    Physics.SyncTransforms();break;}
   case 14:{var g=G(s);
    Check(!g.Seen&&g.GetComponentsInChildren<MeshRenderer>(false).Length==0,"Hidden enemies are not drawn");
    g.State.lamp=true;break;}
   case 15:{var g=G(s);
    Check(g.Seen&&g.LampLight.enabled,"An enemy's flashlight is drawn and reveals them");
    cycle=UnityEngine.Object.FindAnyObjectByType<LanNightCycle>();
    Check(cycle&&cycle.Level<.05f,"The scene is dark at night");
    s.Remaining=10;break;}
   case 16:Check(cycle&&cycle.Level>.8f,"Dawn lights the scene");
    s.Remaining=400;s.MenuOpen=false;var me=s.World.Pawns[s.LocalSlot];Place(me,new Vector3(4,.05f,-12));me.State.lamp=true;break;
   case 17:ScreenCapture.CaptureScreenshot("Documentation/Noche-linterna.png");
    s.World.AddMark(LanItems.FlareMark,0,new Vector3(8,.05f,-9),20);break;
   case 18:break;
   case 19:ScreenCapture.CaptureScreenshot("Documentation/Noche-bengala.png");Finish();break;
  }
 }
 static void Finish(){guest?.Dispose();guest=null;if(LanSession.Instance)LanSession.Instance.Leave();SessionState.SetBool("LanVisionTest",false);EditorApplication.isPlaying=false;}
}
