using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Valle.Lan;
// Comprueba la Etapa 3: personajes por bando y el efecto de cada habilidad, también usada por bots.
[InitializeOnLoad]
public static class LanAbilityCheck {
 sealed class Guest:IDisposable {
  public LanPeer Peer;public int Slot=-1;public LanPacket Last;public LanInput Input=new LanInput();bool loaded;double nextSend;
  public Guest(){var socket=new TcpClient();socket.Connect("127.0.0.1",7777);Peer=new LanPeer(socket);Send(new LanPacket{type="hello",name="Pistolero",team=1,role=0});}
  public void Send(LanPacket packet)=>Peer.Send(JsonUtility.ToJson(packet));
  public void Tick(){
   while(Peer.Incoming.TryDequeue(out var json)){var p=JsonUtility.FromJson<LanPacket>(json);if(p.type=="welcome")Slot=p.slot;if(p.type!="snapshot")continue;Last=p;if(p.phase=="match"&&!loaded){loaded=true;Send(new LanPacket{type="loaded"});}}
   if(EditorApplication.timeSinceStartup<nextSend)return;nextSend=EditorApplication.timeSinceStartup+.05;
   Send(new LanPacket{type=loaded?"input":"ping",input=Input});
  }
  public void Dispose()=>Peer.Dispose();
 }
 static readonly Vector3 Arena=new Vector3(100,.05f,100);
 static readonly StringBuilder log=new StringBuilder();
 static Guest guest;static int stage;static double next,started;
 static LanAbilityCheck(){EditorApplication.update+=Tick;}
 [MenuItem("Tools/Multijugador/Comprobar habilidades")]
 static void Request(){File.WriteAllText("Temp/lan-ability-test.request","");}
 static void Check(bool ok,string text){log.AppendLine((ok?"PASS: ":"FAIL: ")+text);File.WriteAllText("Documentation/LAN-ability-check.txt",log.ToString());}
 static void Info(string text){log.AppendLine("INFO: "+text);File.WriteAllText("Documentation/LAN-ability-check.txt",log.ToString());}
 static void Tick(){
  if(!SessionState.GetBool("LanAbilityTest",false)){
   if(!File.Exists("Temp/lan-ability-test.request")||EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)return;
   File.Delete("Temp/lan-ability-test.request");EditorSceneManager.OpenScene("Assets/Scenes/MenuInicio.unity");SessionState.SetBool("LanAbilityTest",true);EditorApplication.isPlaying=true;return;
  }
  if(!EditorApplication.isPlaying)return;
  if(started==0)started=EditorApplication.timeSinceStartup;
  if(EditorApplication.timeSinceStartup-started>150){Check(false,"Ability check timed out");Finish();return;}
  try{guest?.Tick();if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.5;Advance(LanSession.Instance);}
  catch(Exception e){Check(false,e.ToString());Finish();}
 }
 static void Run(LanSession s,float seconds){Physics.simulationMode=SimulationMode.Script;for(int i=0;i<Mathf.CeilToInt(seconds/.05f)&&s.Phase=="match";i++){s.Step(.05f);Physics.Simulate(.05f);}Physics.simulationMode=SimulationMode.FixedUpdate;}
 static void Place(LanPawn pawn,Vector3 at,Vector3 aim){pawn.ResetLife(at);pawn.State.shield=0;pawn.State.aim=aim;Physics.SyncTransforms();}
 static float Hit(LanSession s,LanPawn shooter,LanPawn victim,Vector3 direction){victim.State.hp=victim.Class.Health;victim.State.shield=0;s.World.Fire(shooter,direction);return victim.Class.Health-victim.State.hp;}
 static void Advance(LanSession s){
  switch(stage++){
   case 0:LanSession.Open();s=LanSession.Instance;s.PlayerName="Agente";s.Host();guest=new Guest();break;
   case 1:if(guest.Slot<0){stage--;return;}guest.Send(new LanPacket{type="ready"});break;
   case 2:s.Slots[6].role=2;s.StartMatch();break;
   case 3:{if(!s.World){stage--;return;}
    var w=s.World;var g=w.Pawns[guest.Slot];var leader=w.LeaderPawn;
    Check(LanClass.For(0,0).Name=="Asaltante"&&LanClass.For(0,1).Name=="Pesado"&&LanClass.For(0,2).Name=="Rastreador","DEA characters");
    Check(LanClass.For(1,0).Name=="Pistolero"&&LanClass.For(1,1).Name=="Guardaespaldas"&&LanClass.For(1,2).Name=="Vigía","Organization characters");
    Check(LanClass.For(1,1).Health==LanClass.Get(1).Health&&LanClass.For(0,2).Reserve==LanClass.Get(2).Reserve,"Characters keep the class statistics");
    Check(w.Pawns[3].Ability==LanAbilities.Dash&&w.Pawns[1].Ability==LanAbilities.Shield&&w.Pawns[2].Ability==LanAbilities.Pulse,"DEA abilities by character");
    Check(g.Ability==LanAbilities.Burst&&w.Pawns[7].Ability==LanAbilities.Guard&&w.Pawns[6].Ability==LanAbilities.Watch&&leader.Ability==LanAbilities.Rally,"Organization and leader abilities");
    s.Night.Deploy=.05f;Run(s,.1f);s.Night.Wave=9999;
    var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Suelo de prueba";floor.transform.position=new Vector3(100,-.5f,100);floor.transform.localScale=new Vector3(40,1,40);Physics.SyncTransforms();
    Place(g,Arena+Vector3.forward*10,Vector3.right);guest.Input.ability++;break;}
   case 4:{var g=s.World.Pawns[guest.Slot];
    Check(g.State.active>0&&g.State.ability>LanAbilities.Cooldown(LanAbilities.Burst)-2,"Right click triggers the ability over the network");
    Check(LanAbilities.FireDelay(g)==LanAbilities.BurstFactor,"Burst doubles the fire rate");
    var copy=guest.Last.actors.First(a=>a.id==g.Id);Check(copy.ability>0,"Cooldown reaches the client");
    float cooling=g.State.ability;guest.Input.ability++;next=EditorApplication.timeSinceStartup+.5;
    Info($"Recarga de Ráfaga tras activarla: {cooling:0.0} s");break;}
   case 5:{var w=s.World;var g=w.Pawns[guest.Slot];
    Check(g.State.ability<LanAbilities.Cooldown(LanAbilities.Burst)-.3f,"Pressing again during cooldown does nothing");
    // Avance
    var dasher=w.Pawns[3];Place(dasher,Arena,Vector3.right);
    LanAbilities.Trigger(w,dasher,new LanInput{x=1});Run(s,.5f);
    float moved=dasher.transform.position.x-Arena.x;Info($"Avance recorrió {moved:0.0} unidades");
    Check(moved>3&&moved<8,"Dash moves a short distance fast");
    // Escudo
    var tank=w.Pawns[1];var shooter=w.Pawns[6];
    Place(tank,Arena+Vector3.back*8,Vector3.right);Place(shooter,Arena+Vector3.back*8+Vector3.right*4,Vector3.left);
    float open=Hit(s,shooter,tank,Vector3.left);LanAbilities.Trigger(w,tank,new LanInput());
    float front=Hit(s,shooter,tank,Vector3.left);
    Place(shooter,Arena+Vector3.back*8-Vector3.right*4,Vector3.right);tank.State.aim=Vector3.right;
    float back=Hit(s,shooter,tank,Vector3.right);
    Info($"Daño al Pesado: sin escudo {open:0.0}, de frente {front:0.0}, por detrás {back:0.0}");
    Check(front>0&&front<open*.3f,"The shield blocks most frontal damage");Check(Mathf.Approximately(back,open),"The shield does not cover the back");
    // Pulso
    var tracker=w.Pawns[2];var hidden=w.Pawns[7];var far=w.Pawns[4];
    Place(tracker,Arena+Vector3.forward*4,Vector3.right);Place(hidden,Arena+Vector3.forward*4+Vector3.right*15,Vector3.left);Place(far,Arena+Vector3.forward*4+Vector3.right*25,Vector3.left);
    var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=Arena+Vector3.forward*4+Vector3.right*6+Vector3.up;wall.transform.localScale=new Vector3(.3f,2,4);Physics.SyncTransforms();
    bool before=LanVision.Sees(w,tracker,hidden);LanAbilities.Trigger(w,tracker,new LanInput());
    Check(!before&&hidden.State.marked>0&&LanVision.Sees(w,tracker,hidden),"Pulse marks enemies behind cover");
    Check(far.State.marked<=0,"Pulse only reaches 18 units");
    Check(LanVision.Sees(w,w.Pawns[0],hidden),"Marked enemies are visible to the whole DEA team");
    UnityEngine.Object.DestroyImmediate(wall);Physics.SyncTransforms();
    // Proteger al líder
    var leader=w.LeaderPawn;var guard=w.Pawns[7];var agent=w.Pawns[1];
    Place(leader,Arena+Vector3.back*14,Vector3.left);Place(guard,Arena+Vector3.back*14+Vector3.left*3,Vector3.left);Place(agent,Arena+Vector3.back*14+Vector3.right*4,Vector3.left);
    float plain=Hit(s,agent,leader,Vector3.left);LanAbilities.Trigger(w,guard,new LanInput());float covered=Hit(s,agent,leader,Vector3.left);
    Info($"Daño al líder: sin protección {plain:0.0}, protegido {covered:0.0}");
    Check(covered>0&&covered<plain*.5f,"A nearby bodyguard protects the leader");
    break;}
   case 6:{var w=s.World;
    // Alarma del Vigía: avisa y marca al intruso
    var watcher=w.Pawns[6];Place(watcher,Arena+Vector3.forward*16,Vector3.right);w.Marks.Clear();
    LanAbilities.Trigger(w,watcher,new LanInput());var alarm=w.Marks.FirstOrDefault(m=>m.kind==LanItems.AlarmMark);
    Check(alarm!=null&&alarm.team==LanNightRules.Org&&watcher.State.ability>0,"The lookout places an alarm without items");
    var intruder=w.Pawns[2];Place(intruder,alarm.position+Vector3.right*2,Vector3.left);Run(s,.15f);
    Check(alarm.fired&&intruder.State.marked>0,"A triggered alarm marks the intruder");
    // Refuerzos del líder
    var leader=w.LeaderPawn;leader.State.ability=0;leader.State.hp=leader.Class.Health;
    LanAbilities.Trigger(w,leader,new LanInput());Check(leader.State.ability==0,"Reinforcements are not spent with nobody down");
    var a=w.Pawns[6];var b=w.Pawns[7];a.State.hp=0;a.Fall();b.State.hp=0;b.Fall();int left=s.Night.OrgLeft;
    LanAbilities.Trigger(w,leader,new LanInput());
    Check(a.State.hp>0&&b.State.hp>0&&s.Night.OrgLeft==left-2&&leader.State.ability>0,"The leader brings fallen guards back early");
    // Bots: noche acelerada en el mapa real
    foreach(var p in w.Pawns.Values){p.AbilityUses=0;p.ResetLife(w.Spawn(p.Id));}
    s.Slots[0].bot=true;s.Night.Wave=LanNightRules.WaveSeconds;Run(s,120);
    var used=w.Pawns.Values.Where(p=>p.AbilityUses>0).Select(p=>LanAbilities.Name(p.Ability)).Distinct().ToList();
    Info($"Habilidades usadas por bots en 2 min: {string.Join(", ",used)} · usos {w.Pawns.Values.Sum(p=>p.AbilityUses)}");
    Check(used.Count>=3,"Bots use their abilities");
    Finish();break;}
  }
 }
 static void Finish(){guest?.Dispose();guest=null;if(LanSession.Instance)LanSession.Instance.Leave();SessionState.SetBool("LanAbilityTest",false);EditorApplication.isPlaying=false;}
}
