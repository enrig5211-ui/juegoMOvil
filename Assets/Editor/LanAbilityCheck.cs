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
    Place(tracker,Arena+Vector3.forward*4,Vector3.right);Place(hidden,Arena+Vector3.forward*4+Vector3.right*12,Vector3.left);Place(far,Arena+Vector3.forward*4+Vector3.right*19,Vector3.left);
    var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=Arena+Vector3.forward*4+Vector3.right*6+Vector3.up;wall.transform.localScale=new Vector3(.3f,2,4);Physics.SyncTransforms();
    bool before=LanVision.Sees(w,tracker,hidden);LanAbilities.Trigger(w,tracker,new LanInput());
    Check(!before&&hidden.State.marked>0&&LanVision.Sees(w,tracker,hidden),"Pulse marks enemies behind cover");
    Check(far.State.marked<=0,"Pulse only reaches 14 units");
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
    // Habilidades nuevas: se cambia el personaje de algunos bots para probarlas.
    var medic=w.Pawns[3];s.Slots[3].role=3;var patient=w.Pawns[1];
    Place(medic,Arena+Vector3.back*6,Vector3.right);Place(patient,Arena+Vector3.back*6+Vector3.right*2,Vector3.left);patient.State.hp=50;
    LanAbilities.Trigger(w,medic,new LanInput());Check(medic.Ability==LanAbilities.Heal&&Mathf.Approximately(patient.State.hp,50+LanAbilities.HealAmount),"The paramedic heals nearby allies");
    var grenadier=w.Pawns[2];s.Slots[2].role=4;var dazed=w.Pawns[7];
    Place(grenadier,Arena+Vector3.back*12,Vector3.right);Place(dazed,Arena+Vector3.back*12+Vector3.right*8,Vector3.left);
    LanAbilities.Trigger(w,grenadier,new LanInput());var stay=dazed.transform.position;
    Check(grenadier.Ability==LanAbilities.Stun&&dazed.State.stun>0&&w.Marks.Any(m=>m.kind==LanItems.BlastMark),"The grenadier's grenade stuns enemies");
    Run(s,.5f);Check(Vector3.Distance(stay,dazed.transform.position)<.05f,"Stunned enemies cannot move");dazed.State.stun=0;
    var hider=w.Pawns[7];s.Slots[7].role=3;var seeker=w.Pawns[1];
    Place(hider,Arena+Vector3.forward*8,Vector3.left);Place(seeker,Arena+Vector3.forward*8+Vector3.left*4,Vector3.right);seeker.State.lamp=true;
    LanAbilities.Trigger(w,hider,new LanInput());bool hidden=!LanVision.Sees(w,seeker,hider);
    Place(seeker,Arena+Vector3.forward*8+Vector3.left*1.5f,Vector3.right);
    Check(hider.Ability==LanAbilities.Ambush&&hidden&&LanVision.Sees(w,seeker,hider),"Ambush hides the coca grower except up close");
    var trapper=w.Pawns[6];s.Slots[6].role=5;Place(trapper,Arena+Vector3.forward*14,Vector3.right);w.Marks.Clear();
    LanAbilities.Trigger(w,trapper,new LanInput());var trap=w.Marks.FirstOrDefault(m=>m.kind==LanItems.TrapMark);
    var victim=w.Pawns[1];Place(victim,trapper.transform.position+Vector3.right*3,Vector3.left);Run(s,.05f);Place(victim,trap!=null?trap.position:Arena,Vector3.left);Run(s,.15f);
    Check(trap!=null&&trap.fired&&victim.State.stun>0&&victim.State.marked>0,"The trapper's trap stuns and marks");
    var scout=w.Pawns[1];s.Slots[1].role=5;Place(scout,Arena+Vector3.back*16,Vector3.right);int flares=w.Marks.Count(m=>m.kind==LanItems.FlareMark);
    LanAbilities.Trigger(w,scout,new LanInput());Check(scout.Ability==LanAbilities.Flare&&w.Marks.Count(m=>m.kind==LanItems.FlareMark)==flares+1,"The night scout throws a flare");
    // Lanceros del líder
    var leader=w.LeaderPawn;leader.State.ability=0;w.Npcs.Clear();var post=Arena+new Vector3(-12,0,12);Place(leader,post,Vector3.right);
    LanAbilities.Trigger(w,leader,new LanInput());
    Check(w.Npcs.Count==LanWorld.SpearCount&&leader.State.ability>LanAbilities.Cooldown(LanAbilities.Rally)-1,"The leader summons five spearmen");
    Run(s,.2f);var npc=w.Npcs[0];var gunner=w.Pawns[1];
    var toward=npc.position-post;toward.y=0;Place(gunner,post+toward.normalized*5.5f,-toward.normalized);
    float before=npc.hp;w.Fire(gunner,(npc.position-gunner.transform.position).normalized);Check(npc.hp<before,"Bullets hurt the spearmen");
    var raider=w.Pawns[3];Place(raider,post+Vector3.right*2,Vector3.left);raider.State.hp=raider.Class.Health;float health=raider.State.hp;
    Run(s,1.5f);Check(raider.State.hp<health,"Spearmen strike nearby DEA agents");
    Run(s,LanWorld.SpearSeconds);Check(w.Npcs.Count==0,"Spearmen leave after their time");
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
