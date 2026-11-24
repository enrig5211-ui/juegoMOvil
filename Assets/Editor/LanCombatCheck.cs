using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Valle.Lan;
// Comprueba granadas, explosiones, fuego, entorno destructible, el plan de despliegue, el minimapa,
// el mapa grande y los avisos de disparos, daño, bombardeo y rastreo.
[InitializeOnLoad]
public static class LanCombatCheck {
 sealed class Guest:IDisposable {
  public LanPeer Peer;public int Slot=-1;public LanPacket Last;public LanInput Input=new LanInput();public int[] Broken=new int[0];bool loaded;double nextSend;
  public Guest(string name="Cocalero",int team=1,int role=0){var socket=new TcpClient();socket.Connect("127.0.0.1",7777);Peer=new LanPeer(socket);Send(new LanPacket{type="hello",name=name,team=team,role=role});}
  public void Send(LanPacket packet)=>Peer.Send(JsonUtility.ToJson(packet));
  public void Tick(){
   while(Peer.Incoming.TryDequeue(out var json)){var p=JsonUtility.FromJson<LanPacket>(json);if(p.type=="welcome"||p.type=="seat")Slot=p.slot;if(p.type!="snapshot")continue;Last=p;if(p.hasBroken&&p.broken!=null)Broken=p.broken;if(p.phase=="match"&&!loaded){loaded=true;Send(new LanPacket{type="loaded"});}}
   if(EditorApplication.timeSinceStartup<nextSend)return;nextSend=EditorApplication.timeSinceStartup+.05;
   Send(new LanPacket{type=loaded?"input":"ping",input=Input});
  }
  public void Dispose()=>Peer.Dispose();
 }
 static readonly Vector3 Arena=new Vector3(100,.05f,100);
 static readonly StringBuilder log=new StringBuilder();
 static Guest guest,driver;static Vector3 parked;static bool driverReady;static int riderShots;static int stage,blasts,wait;static double next,started;static float healthA,healthB,healthAlly;static LanProp barrel,forest,lamp;
 static LanCombatCheck(){EditorApplication.update+=Tick;}
 [MenuItem("Tools/Multijugador/Comprobar explosivos y mapas")]
 static void Request(){File.WriteAllText("Temp/lan-combat-test.request","");}
 static void Check(bool ok,string text){log.AppendLine((ok?"PASS: ":"FAIL: ")+text);File.WriteAllText("Documentation/LAN-combat-check.txt",log.ToString());}
 static void Info(string text){log.AppendLine("INFO: "+text);File.WriteAllText("Documentation/LAN-combat-check.txt",log.ToString());}
 static void Tick(){
  if(!SessionState.GetBool("LanCombatTest",false)){
   if(!File.Exists("Temp/lan-combat-test.request")||EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)return;
   File.Delete("Temp/lan-combat-test.request");log.Clear();EditorSceneManager.OpenScene("Assets/Scenes/MenuInicio.unity");SessionState.SetBool("LanCombatTest",true);EditorApplication.isPlaying=true;return;
  }
  if(!EditorApplication.isPlaying)return;
  if(started==0)started=EditorApplication.timeSinceStartup;
  if(EditorApplication.timeSinceStartup-started>200){Check(false,"Combat check timed out");Finish();return;}
  try{guest?.Tick();driver?.Tick();if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.5;Advance(LanSession.Instance);}
  catch(Exception e){Check(false,e.ToString());Finish();}
 }
 static void Run(LanSession s,float seconds){Physics.simulationMode=SimulationMode.Script;for(int i=0;i<Mathf.CeilToInt(seconds/.05f)&&s.Phase=="match";i++){s.Step(.05f);Physics.Simulate(.05f);}Physics.simulationMode=SimulationMode.FixedUpdate;}
 static void Place(LanPawn pawn,Vector3 at,Vector3 aim){pawn.ResetLife(at);pawn.State.shield=0;pawn.State.aim=aim;Physics.SyncTransforms();}
 static void Shot(string name){ScreenCapture.CaptureScreenshot("Documentation/"+name);}
 static void Advance(LanSession s){
  switch(stage++){
   case 0:LanSession.Open();s=LanSession.Instance;s.PlayerName="Agente";s.Team=0;s.Host();guest=new Guest();break;
   case 1:if(guest.Slot<0){stage--;return;}guest.Send(new LanPacket{type="ready"});driver=new Guest("Chofer",0,0);break;
   case 2:if(driver.Slot<0){stage--;return;}if(!driverReady){driverReady=true;driver.Send(new LanPacket{type="ready"});}if(!s.CanStart){stage--;return;}
    // El anfitrión agranda los dos bandos a 8: las personas conservan su bando.
    if(s.Slots.Length==8){bool dea=s.ResizeTeam(LanNightRules.Dea,4),org=s.ResizeTeam(LanNightRules.Org,4);
     Check(dea&&org&&s.Slots.Length==16&&s.TeamSize(LanNightRules.Dea)==8&&s.TeamSize(LanNightRules.Org)==8,"The host sets 8 against 8 in the lobby");
     Check(!s.ResizeTeam(LanNightRules.Dea,1)&&!s.ResizeTeam(LanNightRules.Org,1),"Teams stay between 2 and 8");stage--;return;}
    if(guest.Slot<8||driver.Slot>=8||guest.Last==null||guest.Last.slots==null||guest.Last.slots.Length!=16){stage--;return;}
    Check(s.Slots[guest.Slot].team==LanNightRules.Org&&!s.Slots[guest.Slot].bot&&s.Slots[driver.Slot].team==LanNightRules.Dea&&!s.Slots[driver.Slot].bot&&s.Slots[s.LocalSlot].team==LanNightRules.Dea,"Players keep their side and seat after resizing");
    s.StartMatch();break;
   case 3:{if(!s.World||s.UI==null){stage--;return;}var w=s.World;
    Check(w.Pawns.Count==16&&s.Night.DeaLeft==LanNightRules.DeaPerAgent*8&&s.Night.OrgLeft==LanNightRules.OrgPerMember*8,"An 8 against 8 night has 16 combatants and more reinforcements");
    Check(w.mapTexture&&w.mapTexture.width>=512,"The scene has a tactical map image");
    Check(s.UI.DeployShown&&!s.UI.MiniMapShown,"The DEA plans its entry on the tactical map");
    Check(w.props!=null&&w.props.Length>300&&w.props.Any(p=>p.kind==LanPropKind.Forest)&&w.props.Any(p=>p.kind==LanPropKind.Barrel)&&w.props.Count(p=>p.kind==LanPropKind.Lamp)==w.lamps.Length,"The map has destructible forest, props, barrels and lamps");
    Info($"Destructibles: {w.props.Length} · "+string.Join(", ",w.props.GroupBy(p=>p.kind).Select(g=>$"{g.Key} {g.Count()}")));
    s.ChoosePoint(2);Shot("Despliegue.png");break;}
   case 4:Check(s.Slots[s.LocalSlot].point==2,"Choosing an entry from the plan reaches the session");s.Night.Deploy=.05f;break;
   case 5:{
    Check(!s.UI.DeployShown&&s.UI.MiniMapShown,"The minimap replaces the plan when the night starts");
    Check(s.UI.MiniIcons>=4,"The minimap draws you and your allies");
    s.MapOpen=true;break;}
   case 6:Check(s.UI.BigMapShown&&!s.UI.MiniMapShown,"M opens the large map");Shot("Mapa-grande.png");break;
   case 7:{s.MapOpen=false;var w=s.World;s.Night.Wave=9999;
    // Granada lanzada por un cliente: la víctima expuesta recibe más daño que la que está tras una caja.
    var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Suelo de prueba";floor.transform.position=new Vector3(100,-.5f,100);floor.transform.localScale=new Vector3(40,1,40);
    var g=w.Pawns[guest.Slot];Place(g,Arena,Vector3.right);guest.Input.ax=1;guest.Input.az=0;
    var a=w.Pawns[1];var b=w.Pawns[2];var ally=w.Pawns.Values.First(p=>p.Team==LanNightRules.Org&&p!=g&&!p.IsLeader);
    Place(a,Arena+new Vector3(6,0,2),Vector3.left);Place(b,Arena+new Vector3(6,0,-2),Vector3.left);Place(ally,Arena+new Vector3(7,0,0),Vector3.left);
    var crate=GameObject.CreatePrimitive(PrimitiveType.Cube);crate.name="Caja de prueba";crate.transform.position=Arena+new Vector3(6,.6f,-1.1f);crate.transform.localScale=new Vector3(1.4f,1.2f,.4f);
    foreach(var p in new[]{a,b,ally})p.State.stun=30;Physics.SyncTransforms();
    healthA=a.State.hp;healthB=b.State.hp;healthAlly=ally.State.hp;blasts=w.Explosions;
    Check(g.State.grenades==LanExplosives.GrenadeOrg,"Each life starts with grenades");
    guest.Input.reach=6;guest.Input.grenade++;wait=0;break;}
   case 8:{var w=s.World;var g=w.Pawns[guest.Slot];
    var thrown=w.Marks.FirstOrDefault(m=>m.kind==LanItems.GrenadeMark);
    if(thrown==null&&w.Explosions==blasts&&wait++<6){stage--;return;}
    Check(g.State.grenades==LanExplosives.GrenadeOrg-1,"G throws a grenade over the network");
    var copy=guest.Last.actors.First(x=>x.id==g.Id);Check(copy.grenades==g.State.grenades,"The grenade count reaches the client");
    if(thrown!=null)Check(Vector3.Distance(thrown.position,Arena+Vector3.right*6)<1,"The grenade lands where the player aims");
    break;}
   case 9:{var w=s.World;if(w.Explosions==blasts&&wait++<12){stage--;return;}
    var a=w.Pawns[1];var b=w.Pawns[2];var ally=w.Pawns.Values.First(p=>p.Team==LanNightRules.Org&&p.Id!=guest.Slot&&!p.IsLeader);
    float hurtA=healthA-a.State.hp,hurtB=healthB-b.State.hp;
    Info($"Granada: expuesto {hurtA:0.0}, tras la caja {hurtB:0.0}, aliado {healthAlly-ally.State.hp:0.0}");
    Check(w.Explosions>blasts&&hurtA>20,"The grenade explodes and hurts nearby enemies");
    Check(hurtB<hurtA*.5f,"Cover absorbs most of the blast");
    Check(Mathf.Approximately(ally.State.hp,healthAlly),"Grenades do not hurt allies");
    Check(w.Marks.Any(m=>m.kind==LanItems.FireMark)&&guest.Last.marks.Any(m=>m.kind==LanItems.FireMark||m.kind==LanItems.ExplosionMark),"The blast leaves fire that reaches clients");
    Check(LanFx.Particles>0&&LanFx.Scorches>0,"Explosions draw fire, smoke and a scorch mark");
    // Fuego: hace daño a quien se queda dentro.
    a.State.hp=a.Class.Health;var fire=w.Marks.First(m=>m.kind==LanItems.FireMark);Place(a,fire.position,Vector3.left);a.State.stun=5;Run(s,1);
    Check(a.State.hp<a.Class.Health,"Standing in the fire burns");
    foreach(var p in w.Pawns.Values)p.State.stun=0;
    UnityEngine.Object.DestroyImmediate(GameObject.Find("Caja de prueba"));
    // Entorno destructible en el mapa real.
    barrel=w.props.First(p=>p.kind==LanPropKind.Barrel);forest=w.props.First(p=>p.kind==LanPropKind.Forest);lamp=w.props.First(p=>p.kind==LanPropKind.Lamp);
    blasts=w.Explosions;var at=barrel.visual.position;w.Explode(new Vector3(at.x,.05f,at.z)+Vector3.right*.8f,LanNightRules.Dea,1,LanExplosives.GrenadeRadius,LanExplosives.GrenadeDamage,LanExplosives.PropGrenade,0,0);
    Check(barrel.Broken&&w.Explosions>=blasts+2&&!barrel.GetComponent<Collider>().enabled&&!barrel.visual.gameObject.activeSelf,"Explosions break props and barrels explode in a chain");
    var center=forest.transform.position;w.Explode(new Vector3(center.x,.05f,center.z),LanNightRules.Dea,1,LanExplosives.ShellRadius,LanExplosives.ShellDamage,LanExplosives.PropShell,0,0);Physics.SyncTransforms();
    Check(forest.Broken&&LanVision.Clear(new Vector3(center.x-.7f,.4f,center.z),new Vector3(center.x+.7f,.4f,center.z)),"A shell clears the jungle and opens the view");
    Check(LanDebris.Pieces>0||UnityEngine.Object.FindObjectsByType<LanFalling>().Length>0,"Broken props leave falling trees and debris");
    // Farol apagado a balazos.
    var shooter=w.Pawns[1];var lampAt=lamp.transform.position;Place(shooter,new Vector3(lampAt.x+1.6f,.05f,lampAt.z),Vector3.left);
    for(int i=0;i<4&&!lamp.Broken;i++)w.Fire(shooter,Vector3.left);
    Check(lamp.Broken&&!lamp.visual.gameObject.activeInHierarchy,"Shooting a lamp post turns its light off");
    break;}
   case 10:{var w=s.World;
    var expected=new[]{barrel.Index,forest.Index,lamp.Index};
    if(!expected.All(i=>guest.Broken.Contains(i))&&wait++<20){stage--;return;}
    Check(expected.All(i=>guest.Broken.Contains(i)),"Clients receive the list of broken props");
    // Avisos de disparo y de daño para el jugador local (DEA).
    var me=w.Pawns[s.LocalSlot];var enemy=w.Pawns.Values.First(p=>p.Team==LanNightRules.Org&&p.Id!=guest.Slot&&!p.IsLeader);
    Place(me,Arena+new Vector3(-10,0,8),Vector3.right);Place(enemy,Arena+new Vector3(4,0,8),Vector3.left);
    w.Fire(enemy,Vector3.left);break;}
   case 11:{
    Check(s.UI.ShotPings>0,"Enemy gunfire shows on the minimap");
    Check(s.UI.HitMarks>0,"Taking a hit shows where it came from");
    var w=s.World;var me=w.Pawns[s.LocalSlot];me.State.hp=me.Class.Health;
    w.Explode(me.transform.position+new Vector3(3.2f,0,1.5f),LanNightRules.Org,-1,LanExplosives.GrenadeRadius,LanExplosives.GrenadeDamage,LanExplosives.PropGrenade,LanExplosives.GrenadeFire,LanExplosives.GrenadeBurn);
    w.CallStrike(w.Pawns[1],me.transform.position+Vector3.forward*2);break;}
   case 12:{Check(s.UI.Alert.Contains("BOMBARDEO"),"Standing in a marked strike zone shows a warning");Shot("Combate.png");break;}
   case 13:{var w=s.World;w.Marks.RemoveAll(m=>m.kind==LanItems.StrikeMark||m.kind==LanItems.FireMark);var me=w.Pawns[s.LocalSlot];var at=me.transform.position+new Vector3(2.5f,0,1.5f);
    w.AddMark(LanItems.ExplosionMark,-1,at,.8f).size=LanExplosives.GrenadeRadius;w.AddMark(LanItems.FireMark,LanNightRules.Dea,at,5).size=LanExplosives.GrenadeFire;
    next=EditorApplication.timeSinceStartup+.3;break;}
   case 14:Shot("Explosion.png");break;
   case 15:{var w=s.World;w.Marks.RemoveAll(m=>m.kind==LanItems.StrikeMark);var tracker=w.Pawns[2];s.Slots[2].role=2;Place(tracker,w.Pawns[s.LocalSlot].transform.position+Vector3.left*2,Vector3.left);tracker.State.ability=0;s.Night.Clock=Mathf.Max(s.Night.Clock,w.ScanReady);
    w.LeaderPawn.State.hp=w.LeaderPawn.Class.Health;LanAbilities.Trigger(w,tracker,new LanInput());break;}
   case 16:{Check(s.UI.Alert.Contains("RASTREO"),"Tracking the leader shows an alert for the DEA");
    // Granada en la mano (tecla 2): con ella elegida, el clic no dispara el rifle.
    var w=s.World;var g=w.Pawns[guest.Slot];g.ResetLife(g.transform.position);g.State.shield=0;guest.Input.equip=1;guest.Input.fire=true;blasts=g.State.shot;break;}
   case 17:{var w=s.World;var g=w.Pawns[guest.Slot];
    Check(g.State.equip==1&&g.State.shot==blasts,"With the grenade in hand, clicking does not fire the rifle");
    guest.Input.equip=0;guest.Input.fire=false;
    // Blindado: junto a la entrada del Camino; el cliente DEA sube con V y conduce.
    var v=w.Vehicles.FirstOrDefault();Check(v!=null&&v.team==LanNightRules.Dea&&v.hp==LanWorld.VehicleHealth&&GameObject.Find("Blindado "+v.id),"The DEA has an armored vehicle near its entry");
    var d=w.Pawns[driver.Slot];Place(d,v.position+Quaternion.Euler(0,v.yaw,0)*Vector3.left*1.2f,Vector3.forward);parked=v.position;
    var org=w.Pawns[guest.Slot];Place(org,v.position+Quaternion.Euler(0,v.yaw,0)*Vector3.right*1.2f,Vector3.forward);Check(!w.Board(org),"The organization cannot board the DEA vehicle");
    var truck=w.Vehicles.First(x=>x.kind==LanWorld.Truck);Place(org,truck.position+Vector3.right*1.2f,Vector3.forward);Check(w.Vehicles.Count(x=>x.kind==LanWorld.Armored&&x.team==LanNightRules.Dea)==2&&w.Board(org)&&truck.driver==org.Id&&truck.team==LanNightRules.Org,"The organization drives its own chuto");w.Leave(org);Place(org,v.position+Vector3.right*20,Vector3.forward);
    driver.Input.vehicle++;wait=0;break;}
   case 18:{var w=s.World;var v=w.Vehicles[0];var d=w.Pawns[driver.Slot];
    if(d.State.ride==0&&wait++<6){stage--;return;}
    Check(d.State.ride==v.id&&v.driver==d.Id,"V boards the vehicle as driver");
    var toward=-v.position;toward.y=0;toward.Normalize();driver.Input.x=toward.x;driver.Input.z=toward.z;break;}
   case 19:{var w=s.World;var v=w.Vehicles[0];var d=w.Pawns[driver.Slot];
    if(Vector3.Distance(v.position,parked)<2&&wait++<10){stage--;return;}
    Info($"Blindado: recorrió {Vector3.Distance(v.position,parked):0.0} unidades a {v.speed:0.0} u/s");
    Check(Vector3.Distance(v.position,parked)>2&&Vector3.Distance(d.transform.position,v.position)<.5f,"The driver moves the vehicle and rides along");
    Check(driver.Last.vehicles!=null&&driver.Last.vehicles.Length==w.Vehicles.Count&&w.Vehicles.Count==4&&driver.Last.vehicles[0].driver==d.Id,"Vehicles reach the clients");
    driver.Input.x=driver.Input.z=0;var me=w.Pawns[s.LocalSlot];me.ResetLife(v.position+Quaternion.Euler(0,v.yaw,0)*Vector3.left*3);me.State.shield=0;Physics.SyncTransforms();next=EditorApplication.timeSinceStartup+1.2;break;}
   case 20:Shot("Blindado.png");break;
   case 21:{var w=s.World;var v=w.Vehicles[0];var d=w.Pawns[driver.Slot];var me=w.Pawns[s.LocalSlot];
    // Una sola plaza: con el conductor a bordo nadie más sube; él mismo dispara la ametralladora.
    Place(me,v.position+Quaternion.Euler(0,v.yaw,0)*Vector3.back*1.2f,Vector3.forward);Check(!w.Board(me)&&v.driver==d.Id,"The vehicle seats a single player");
    var target=w.Pawns.Values.First(p=>p.Team==LanNightRules.Org&&!p.IsLeader&&p.Id!=guest.Slot);var aim=Quaternion.Euler(0,v.yaw,0)*Vector3.forward;
    foreach(var other in w.Pawns.Values)if(other!=target&&other.State.ride==0)other.ResetLife(Arena+new Vector3(other.Id*3,0,-12));w.Npcs.Clear();
    Place(target,v.position+aim*7,-aim);target.State.stun=10;healthA=target.State.hp;driver.Input.ax=aim.x;driver.Input.az=aim.z;driver.Input.fire=true;blasts=v.shot;riderShots=d.State.shot;next=EditorApplication.timeSinceStartup+1;break;}
   case 22:{var w=s.World;var v=w.Vehicles[0];var d=w.Pawns[driver.Slot];var target=w.Pawns.Values.First(p=>p.Team==LanNightRules.Org&&!p.IsLeader&&p.Id!=guest.Slot);
    Info($"Ametralladora: {v.shot-blasts} disparos, vida del objetivo {healthA:0} → {target.State.hp:0} · golpeó «{w.LastStruck}»");
    Check(v.shot>blasts&&target.State.hp<healthA&&d.State.shot==riderShots,"The driver fires the machine gun with the mouse");
    driver.Input.fire=false;target.State.stun=0;
    float crewHp=d.State.hp,hull=v.hp;
    var shooter=w.Pawns.Values.First(p=>p.Team==LanNightRules.Org&&!p.IsLeader);Place(shooter,v.position+Vector3.right*6,Vector3.left);
    for(int i=0;i<3;i++)w.Fire(shooter,(v.position+Vector3.up*.4f-shooter.transform.position-Vector3.up*.35f).normalized);
    w.Explode(v.position+Vector3.right*1.2f,LanNightRules.Org,shooter.Id,LanExplosives.GrenadeRadius,LanExplosives.GrenadeDamage,LanExplosives.PropGrenade,0,0);
    Info($"Blindado: vida {hull:0} → {v.hp:0}; conductor {crewHp:0} → {d.State.hp:0}");
    Check(v.hp<hull-60&&Mathf.Approximately(d.State.hp,crewHp),"Bullets and blasts hit the vehicle, not its driver");
    driver.Input.vehicle++;wait=0;break;}
   case 23:{var w=s.World;var v=w.Vehicles[0];var d=w.Pawns[driver.Slot];
    if(d.State.ride!=0&&wait++<6){stage--;return;}
    Check(d.State.ride==0&&v.driver<0&&Vector3.Distance(d.transform.position,v.position)<2,"V again gets off beside the vehicle");
    var me=w.Pawns[s.LocalSlot];Place(me,v.position+Quaternion.Euler(0,v.yaw,0)*Vector3.back*1.2f,Vector3.forward);Check(w.Board(me)&&v.driver==me.Id,"Once free, someone else can take it");
    break;}
   case 24:{var w=s.World;var v=w.Vehicles[0];var me=w.Pawns[s.LocalSlot];var shooter=w.Pawns.Values.First(p=>p.Team==LanNightRules.Org&&!p.IsLeader);
    w.DamageVehicle(v,9999,shooter.Id);
    Check(v.respawn>0&&me.State.ride==0&&v.driver<0,"A destroyed vehicle throws out its driver and comes back later");
    break;}
   case 25:{var w=s.World;
    // Suministro cayendo y la pista decorada, para las capturas.
    var me=w.Pawns[s.LocalSlot];me.ResetLife(new Vector3(42.5f,.06f,4));me.State.shield=0;me.State.aim=Vector3.forward;Physics.SyncTransforms();
    w.CallSupply(w.Pawns[1],me.transform.position+Vector3.left*2.5f);next=EditorApplication.timeSinceStartup+3.2;break;}
   case 26:Shot("Pista.png");break;
   case 27:{var w=s.World;var me=w.Pawns[s.LocalSlot];me.ResetLife(new Vector3(24,.06f,-1.5f));me.State.shield=0;me.State.aim=Vector3.left;Physics.SyncTransforms();
    var hitman=w.Pawns.Values.First(p=>p.Team==LanNightRules.Org&&!p.IsLeader);Check(w.CallChutos(hitman,new Vector3(22,0,1.5f)),"The hitman can call the convoy on the east road");next=EditorApplication.timeSinceStartup+4.6;break;}
   case 28:Shot("Convoy.png");break;
   case 29:{var w=s.World;var guestPawn=w.Pawns[guest.Slot];driver.Dispose();driver=null;
    // Bots en el mapa real: ¿lanzan granadas y rompen cosas?
    foreach(var p in w.Pawns.Values)p.ResetLife(w.Spawn(p.Id));s.Slots[0].bot=true;s.Night.Wave=LanNightRules.WaveSeconds;
    blasts=w.Explosions;int broken=w.Broken.Count;Run(s,120);
    int thrown=w.Explosions-blasts;Info($"Bots en 2 min: {thrown} explosiones, {w.Broken.Count-broken} objetos rotos, granadas que quedan {string.Join(",",w.Pawns.Values.Select(p=>p.State.grenades))}");
    Check(thrown>0,"Bots use grenades or explosives");
    Finish();break;}
  }
 }
 static void Finish(){guest?.Dispose();guest=null;driver?.Dispose();driver=null;driverReady=false;stage=0;started=0;if(LanSession.Instance)LanSession.Instance.Leave();SessionState.SetBool("LanCombatTest",false);EditorApplication.isPlaying=false;}
}
