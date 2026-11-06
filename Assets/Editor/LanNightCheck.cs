using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Valle.Lan;
// Comprueba las reglas de la noche: despliegue, oleadas, reducción, esposas y los tres finales.
[InitializeOnLoad]
public static class LanNightCheck {
 sealed class Guest:IDisposable {
  public LanPeer Peer;public int Slot=-1;public LanPacket Last;bool loaded;double nextPing;
  public Guest(string name,int team){var socket=new TcpClient();socket.Connect("127.0.0.1",7777);Peer=new LanPeer(socket);Send(new LanPacket{type="hello",name=name,team=team});}
  public void Send(LanPacket packet)=>Peer.Send(JsonUtility.ToJson(packet));
  public void Tick(){
   while(Peer.Incoming.TryDequeue(out var json)){
    var p=JsonUtility.FromJson<LanPacket>(json);
    if(p.type=="welcome")Slot=p.slot;
    if(p.type!="snapshot")continue;
    Last=p;
    if(p.phase=="match"&&!loaded){loaded=true;Send(new LanPacket{type="loaded"});}
    if(p.phase=="lobby")loaded=false;
   }
   // Un cliente real envía 20 mensajes por segundo.
   if(EditorApplication.timeSinceStartup<nextPing)return;
   nextPing=EditorApplication.timeSinceStartup+.05;
   Send(new LanPacket{type="ping"});
  }
  public void Dispose()=>Peer.Dispose();
 }
 static readonly Vector3 Arena=new Vector3(100,.05f,100);
 static readonly StringBuilder log=new StringBuilder();
 static Guest guest,late;static int stage,reinforcements,drops;static double next,started;
 static LanNightCheck(){EditorApplication.update+=Tick;}
 [MenuItem("Tools/Multijugador/Comprobar reglas de la noche")]
 static void Request(){File.WriteAllText("Temp/lan-night-test.request","");}
 static void Check(bool ok,string text){log.AppendLine((ok?"PASS: ":"FAIL: ")+text);File.WriteAllText("Documentation/LAN-night-check.txt",log.ToString());}
 static void Tick(){
  if(!SessionState.GetBool("LanNightTest",false)){
   if(!File.Exists("Temp/lan-night-test.request")||EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)return;
   File.Delete("Temp/lan-night-test.request");EditorSceneManager.OpenScene("Assets/Scenes/MenuInicio.unity");SessionState.SetBool("LanNightTest",true);EditorApplication.isPlaying=true;return;
  }
  if(!EditorApplication.isPlaying)return;
  if(started==0)started=EditorApplication.timeSinceStartup;
  if(EditorApplication.timeSinceStartup-started>120){Check(false,"Night check timed out");Finish();return;}
  try{guest?.Tick();late?.Tick();if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.5;Advance(LanSession.Instance);}
  catch(Exception e){Check(false,e.ToString());Finish();}
 }
 static void Run(LanSession s,float seconds){for(int i=0;i<Mathf.CeilToInt(seconds/.05f)&&s.Phase=="match";i++)s.Step(.05f);}
 static void Place(LanPawn pawn,Vector3 at){pawn.ResetLife(at);pawn.State.shield=0;Physics.SyncTransforms();}
 static void Shoot(LanSession s,LanPawn shooter,LanPawn victim){victim.State.shield=0;for(int i=0;i<40&&victim.State.hp>0;i++)s.World.Fire(shooter,Vector3.right);}
 static void Advance(LanSession s){
  switch(stage++){
   case 0:LanSession.Open();s=LanSession.Instance;s.PlayerName="Agente";s.Host();guest=new Guest("Guardia",1);break;
   case 1:if(guest.Slot<0){stage--;return;}guest.Send(new LanPacket{type="ready"});break;
   case 2:s.StartMatch();break;
   case 3:{if(!s.World){stage--;return;}
    var leader=s.World.LeaderPawn;
    Check(leader&&leader.Team==LanNightRules.Org&&s.Slots[leader.Id].bot,"Bot takes the leader when nobody claims it");
    Check(leader.Class.Name=="Líder"&&leader.State.weapon==1,"Leader uses its own class and pistol");
    Check(s.Night.Deploying&&s.Remaining==LanNightRules.NightSeconds,"Deployment holds the night clock");
    Check(s.World.deaEntries.Length==4&&s.World.hideouts.Length>0&&s.World.zones.Length>s.World.hideouts.Length,"Map provides entries, hideouts and search zones");
    s.ChoosePoint(2);
    Check(Vector3.Distance(s.World.Pawns[0].State.position,s.World.deaEntries[2].position)<2,"DEA agent moves to the chosen entry point");
    var before=s.World.Pawns[1].State.position;var leaderFrom=leader.State.position;
    Run(s,3);
    Check(s.World.Pawns.Values.Sum(p=>p.State.shot)==0,"Nobody fires during deployment");
    Check(s.World.Pawns[1].State.position==before,"DEA stays off the field during deployment");
    Check(Vector3.Distance(leaderFrom,leader.State.position)>.3f,"Organization moves during deployment");
    s.Night.Deploy=.05f;float clock=s.Remaining;Run(s,.05f);Run(s,.2f);
    Check(!s.Night.Deploying&&s.Remaining<clock,"Night clock starts after deployment");
    s.Night.Wave=9999;
    var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Suelo de prueba";floor.transform.position=new Vector3(100,-.5f,100);floor.transform.localScale=new Vector3(30,1,30);Physics.SyncTransforms();
    break;}
   case 4:{
    var agent=s.World.Pawns[1];var bot=s.World.Pawns.Values.First(p=>p.Team==LanNightRules.Org&&!p.IsLeader&&s.Slots[p.Id].bot);
    Place(agent,Arena);Place(bot,Arena+Vector3.right*2);
    reinforcements=s.Night.OrgLeft;Shoot(s,agent,bot);
    Check(bot.State.hp<=0&&bot.State.weapon==0,"Guard falls and drops its weapon");
    Run(s,2);Check(bot.State.hp<=0&&s.Night.OrgLeft==reinforcements,"Fallen guard waits for the wave");
    s.Night.Wave=.05f;Run(s,.1f);s.Night.Wave=9999;
    Check(bot.State.hp>0&&s.Night.OrgLeft==reinforcements-1,"Wave returns the guard and spends one reinforcement");
    break;}
   case 5:{
    var agent=s.World.Pawns[1];var leader=s.World.LeaderPawn;
    Place(agent,Arena);Place(leader,Arena+Vector3.right*2);
    drops=s.World.Drops.Count;reinforcements=s.Night.OrgLeft;Shoot(s,agent,leader);
    Check(leader.State.hp<=0&&leader.State.down==LanNightRules.ReducedSeconds,"Leader without health is reduced");
    Check(s.Phase=="match"&&s.Night.Outcome==LanOutcome.None&&s.World.Drops.Count==drops,"Reducing the leader neither ends the night nor drops the weapon");
    Place(agent,Arena+Vector3.left*3);
    Run(s,LanNightRules.ReducedSeconds+.2f);
    Check(Mathf.Approximately(leader.State.down,0)&&Mathf.Approximately(leader.State.hp,leader.Class.Health*LanNightRules.RecoverFraction),"Leader recovers if nobody handcuffs in time");
    Check(s.Night.OrgLeft==reinforcements,"Leader recovery spends no reinforcements");
    break;}
   case 6:{
    var agent=s.World.Pawns[1];var leader=s.World.LeaderPawn;var bot=s.World.Pawns.Values.First(p=>p.Team==LanNightRules.Org&&!p.IsLeader&&s.Slots[p.Id].bot);
    Place(leader,Arena+Vector3.right*2);Place(bot,Arena+Vector3.right*4);Place(agent,Arena);
    Shoot(s,agent,leader);leader.State.down=15;
    bot.State.hp=0;bot.Fall();
    late=new Guest("Tardío",1);break;}
   case 7:{if(late.Slot<0){stage--;return;}
    var leader=s.World.LeaderPawn;var joined=s.World.Pawns[late.Slot];
    Check(!s.Slots[late.Slot].bot&&s.Slots[late.Slot].id!=s.Night.Leader,"Late join never takes the leader");
    Check(joined.State.hp<=0,"Late join does not revive a fallen guard");
    Check(leader.State.hp<=0&&leader.State.down>0,"Late join does not revive a reduced leader");
    late.Dispose();late=null;break;}
   case 8:{
    var agent=s.World.Pawns[1];var leader=s.World.LeaderPawn;
    leader.State.down=15;Place(agent,leader.State.position+Vector3.left);
    Run(s,2);
    Check(s.Night.Capturer==agent.Id&&s.Night.Capture>.3f&&s.Night.Capture<.6f,"Holding next to the leader advances the handcuffs");
    Check(leader.State.down==15,"Leader does not recover while being handcuffed");
    Check(s.Night.Outcome==LanOutcome.None,"Two seconds are not enough to handcuff");
    var guard=s.World.Pawns.Values.First(p=>p.Team==LanNightRules.Org&&!p.IsLeader&&p.State.hp>0);
    float shot=agent.State.hp;Place(guard,agent.State.position+Vector3.left*2);agent.State.hp=shot;agent.State.shield=0;guard.State.aim=Vector3.right;
    s.World.Fire(guard,Vector3.right);
    Check(agent.State.hp<shot&&s.Night.Capture==0,"Hitting the agent interrupts the handcuffs");
    guard.ResetLife(s.World.Spawn(guard.Id));Physics.SyncTransforms();
    agent.State.hp=agent.Class.Health;
    Run(s,LanNightRules.CuffSeconds+.5f);
    Check(s.Night.Outcome==LanOutcome.Captured&&s.Phase=="result","Finishing the handcuffs wins for the DEA");
    break;}
   case 9:Check(!guest.Peer.Closed,"Guest stays connected through the night");Check(guest.Last.phase=="result"&&guest.Last.outcome==(int)LanOutcome.Captured,"Outcome reaches clients");s.ReturnLobby();break;
   case 10:if(s.Phase!="lobby"||s.World||Scene()!="MenuInicio"){stage--;return;}
    s.ChooseLeader();Check(s.Night.Leader<0,"DEA cannot claim the leader");
    guest.Send(new LanPacket{type="leader"});guest.Send(new LanPacket{type="ready"});break;
   case 11:Check(s.Night.Leader==guest.Slot&&guest.Last.leader==guest.Slot,"Organization player claims the leader");s.StartMatch();break;
   case 12:if(!s.World){stage--;return;}
    Check(s.Night.Leader==guest.Slot&&!s.Slots[guest.Slot].bot,"Human keeps the leader in the match");
    s.Night.Deploy=0;s.Remaining=.1f;Run(s,.3f);
    Check(s.Night.Outcome==LanOutcome.Dawn&&s.Phase=="result","Dawn wins for the organization");break;
   case 13:s.ReturnLobby();break;
   case 14:if(s.Phase!="lobby"||s.World||Scene()!="MenuInicio"){stage--;return;}guest.Send(new LanPacket{type="ready"});break;
   case 15:s.StartMatch();break;
   case 16:if(!s.World){stage--;return;}
    s.Night.Deploy=0;s.Night.DeaLeft=0;
    foreach(var pawn in s.World.Pawns.Values)if(pawn.Team==LanNightRules.Dea)pawn.State.hp=0;
    Run(s,.2f);
    Check(s.Night.Outcome==LanOutcome.DeaSpent&&s.Phase=="result","DEA without agents or reinforcements loses");
    Finish();break;
  }
 }
 static string Scene()=>UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
 static void Finish(){guest?.Dispose();late?.Dispose();if(LanSession.Instance)LanSession.Instance.Leave();SessionState.SetBool("LanNightTest",false);EditorApplication.isPlaying=false;}
}
