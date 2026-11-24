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
    Check(LanClass.For(1,0).Name=="Pistolero"&&LanClass.For(1,1).Name=="Guardaespaldas"&&LanClass.For(1,2).Name=="Reclutador","Organization characters");
    Check(LanClass.For(1,1).Health==LanClass.Get(1).Health&&LanClass.For(0,2).Reserve==LanClass.Get(2).Reserve,"Characters keep the class statistics");
    Check(w.Pawns[3].Ability==LanAbilities.Supply&&w.Pawns[1].Ability==LanAbilities.Shield&&w.Pawns[2].Ability==LanAbilities.Scan,"DEA abilities by character");
    Check(g.Ability==LanAbilities.Burst&&w.Pawns[7].Ability==LanAbilities.Wall&&w.Pawns[6].Ability==LanAbilities.Recruit&&leader.Ability==LanAbilities.Rally,"Organization and leader abilities");
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
    // Suministro aéreo del Asaltante: la caja cae donde apunta y deja botiquín, munición, granadas y un rifle.
    var caller=w.Pawns[3];Place(caller,Arena,Vector3.right);int drops=w.Drops.Count;
    LanAbilities.Trigger(w,caller,new LanInput{reach=5});var supply=w.Marks.FirstOrDefault(m=>m.kind==LanItems.SupplyMark);
    Check(supply!=null&&Vector3.Distance(supply.position,Arena+Vector3.right*5)<1.5f,"The assaulter calls a supply drop where he aims");
    Run(s,LanAbilities.SupplyTime-1);Check(!supply.fired&&w.Marks.Contains(supply),"The crate is still falling");
    Run(s,1.2f);var dropped=w.Drops.Where(d=>Vector3.Distance(d.position,supply.position)<2.5f).ToList();
    Check(dropped.Any(d=>d.item==LanItems.Medkit)&&dropped.Any(d=>d.item==LanItems.Ammo)&&dropped.Any(d=>d.item==LanItems.Grenades)&&dropped.Any(d=>d.weapon==2),"The crate lands with a medkit, ammo, grenades and a rifle");
    // Escudo
    var tank=w.Pawns[1];var shooter=w.Pawns[6];
    Place(tank,Arena+Vector3.back*8,Vector3.right);Place(shooter,Arena+Vector3.back*8+Vector3.right*4,Vector3.left);
    float open=Hit(s,shooter,tank,Vector3.left);LanAbilities.Trigger(w,tank,new LanInput());
    float front=Hit(s,shooter,tank,Vector3.left);
    Place(shooter,Arena+Vector3.back*8-Vector3.right*4,Vector3.right);tank.State.aim=Vector3.right;
    float back=Hit(s,shooter,tank,Vector3.right);
    Info($"Daño al Pesado: sin escudo {open:0.0}, de frente {front:0.0}, por detrás {back:0.0}");
    Check(front>0&&front<open*.3f,"The shield blocks most frontal damage");Check(Mathf.Abs(back-open*LanAbilities.DomeFactor)<.05f,"From behind only the dome protects");
    // Cúpula del Pesado: un aliado dentro recibe la mitad.
    var ally=w.Pawns[0];var dome=w.Marks.FirstOrDefault(m=>m.kind==LanItems.ShieldMark);
    Place(ally,Arena+Vector3.back*8+Vector3.forward*2,Vector3.right);Place(shooter,Arena+Vector3.back*6+Vector3.right*5,Vector3.left);float inside=Hit(s,shooter,ally,Vector3.left);
    Place(ally,Arena+Vector3.back*8+Vector3.forward*6,Vector3.right);Place(shooter,Arena+Vector3.back*2+Vector3.right*5,Vector3.left);float outside=Hit(s,shooter,ally,Vector3.left);
    Info($"Cúpula: aliado dentro {inside:0.0}, fuera {outside:0.0}");
    Check(dome!=null&&dome.team==LanNightRules.Dea&&inside>0&&Mathf.Abs(inside-outside*LanAbilities.DomeFactor)<.05f,"The heavy's dome halves damage for allies inside");
    // Rastreo: ubica al líder en todo el mapa (donde estaba en ese momento).
    var tracker=w.Pawns[2];var hunted=w.LeaderPawn;w.Marks.Clear();
    Place(tracker,Arena+Vector3.forward*4,Vector3.right);Place(hunted,Arena+new Vector3(-16,0,16),Vector3.left);
    s.Night.Clock=Mathf.Max(s.Night.Clock,w.ScanReady);LanAbilities.Trigger(w,tracker,new LanInput());var scan=w.LastScan;
    Check(scan!=null&&scan.team==LanNightRules.Dea&&Vector3.Distance(scan.position,hunted.transform.position)<.1f,"Tracking locates the leader anywhere on the map");
    tracker.State.ability=0;LanAbilities.Trigger(w,tracker,new LanInput());Check(w.Marks.Count(m=>m.kind==LanItems.ScanMark)==1&&w.ScanReady>=s.Night.Clock+LanAbilities.ScanEvery-1,"Tracking waits a minute between scans for the whole team");
    Place(hunted,Arena+new Vector3(-16,0,24),Vector3.left);Check(Vector3.Distance(scan.position,hunted.transform.position)>5,"The scan shows where the leader was, not where he goes");
    // Muralla del Guardaespaldas: frena balas, se desgasta y las explosiones la rompen.
    var leader=w.LeaderPawn;var builder=w.Pawns[7];var agent=w.Pawns[1];
    Place(leader,Arena+Vector3.back*14,Vector3.left);Place(builder,Arena+Vector3.back*20,Vector3.right);Place(agent,Arena+Vector3.back*14+Vector3.right*7,Vector3.left);
    float plain=Hit(s,agent,leader,Vector3.left);
    Place(builder,Arena+Vector3.back*14+Vector3.right,Vector3.right);LanAbilities.Trigger(w,builder,new LanInput());
    var wall=w.Marks.FirstOrDefault(m=>m.kind==LanItems.WallMark);
    Check(wall!=null&&builder.State.ability>0&&GameObject.Find("Muralla"),"The bodyguard raises a sandbag wall");
    float covered=Hit(s,agent,leader,Vector3.left);
    Info($"Daño al líder: sin muralla {plain:0.0}, tras la muralla {covered:0.0}, vida de la muralla {wall?.hp:0}");
    Check(plain>0&&covered==0&&wall.hp<LanAbilities.WallHealth,"The wall stops bullets and wears down");
    for(int i=0;i<3&&!wall.fired;i++)w.Explode(wall.position+Vector3.right*.6f,LanNightRules.Dea,agent.Id,LanExplosives.GrenadeRadius,LanExplosives.GrenadeDamage,LanExplosives.PropGrenade,0,0);
    Place(builder,Arena+Vector3.back*20,Vector3.right);float open2=Hit(s,agent,leader,Vector3.left);
    Check(wall.fired&&open2>0,"Explosions break the wall and open the line of fire");
    break;}
   case 6:{var w=s.World;
    Check(guest.Last.marks!=null&&guest.Last.marks.Any(m=>m.kind==LanItems.ScanMark),"The organization learns its leader was tracked");
    // Reclutador: comuneros que lo siguen; un kamikaze corre hacia el agente y estalla.
    var recruiter=w.Pawns[6];Place(recruiter,Arena+Vector3.forward*16,Vector3.right);w.Marks.Clear();w.Npcs.Clear();
    LanAbilities.Trigger(w,recruiter,new LanInput());var mine=w.Npcs.Where(n=>n.owner==recruiter.Id).ToList();
    Check(mine.Count==LanWorld.RecruitCount&&mine.All(n=>n.kind==LanWorld.Villager||n.kind==LanWorld.Kamikaze)&&recruiter.State.ability>0,"The recruiter calls villagers");
    Info($"Reclutas: {mine.Count(n=>n.kind==LanWorld.Kamikaze)} kamikazes de {mine.Count}");
    recruiter.State.ability=0;LanAbilities.Trigger(w,recruiter,new LanInput());Check(w.Npcs.Count(n=>n.owner==recruiter.Id)==LanWorld.RecruitCount,"Only one group of recruits at a time");
    foreach(var n in mine)n.kind=LanWorld.Kamikaze;mine[0].hp=0;int boom=w.Detonations;var runner=w.Pawns[2];Place(runner,recruiter.transform.position+Vector3.right*5,Vector3.left);runner.State.stun=10;float runnerHp=runner.State.hp;
    Run(s,2.5f);
    Info($"Kamikazes: {w.Detonations-boom} estallidos, vida del agente {runnerHp:0} → {runner.State.hp:0}");
    Check(w.Detonations-boom>=2&&runner.State.hp<runnerHp,"Kamikazes blow up when shot down and when they reach an agent");
    runner.State.stun=0;w.Npcs.Clear();
    // Habilidades nuevas: se cambia el personaje de algunos bots para probarlas.
    var medic=w.Pawns[3];s.Slots[3].role=3;var patient=w.Pawns[1];
    Place(medic,Arena+Vector3.back*6,Vector3.right);Place(patient,Arena+Vector3.back*6+Vector3.right*2,Vector3.left);patient.State.hp=50;
    LanAbilities.Trigger(w,medic,new LanInput());var station=w.Marks.FirstOrDefault(m=>m.kind==LanItems.MedStationMark);patient.State.stun=3;Run(s,1);
    Info($"Estación médica: herido 50 → {patient.State.hp:0} en 1 s");
    Check(medic.Ability==LanAbilities.MedStation&&station!=null&&station.time>LanAbilities.StationSeconds-2&&patient.State.hp>=50+LanAbilities.StationHeal*.9f,"The paramedic's station heals allies around it");
    var grenadier=w.Pawns[2];s.Slots[2].role=4;var dazed=w.Pawns[7];
    Place(grenadier,Arena+Vector3.back*12,Vector3.right);Place(dazed,Arena+Vector3.back*12+Vector3.right*8,Vector3.left);
    int boomsBefore=w.Explosions,stunsBefore=w.Stuns;LanAbilities.Trigger(w,grenadier,new LanInput{reach=8});var barrage=w.Marks.FirstOrDefault(m=>m.kind==LanItems.BarrageMark);grenadier.State.stun=12;
    Run(s,4.2f);bool dazedNow=w.Stuns>stunsBefore;
    Info($"Andanada: {barrage?.shots} lanzadas en 4 s, {w.Explosions-boomsBefore} explosiones, aturdido {dazedNow}");
    Check(grenadier.Ability==LanAbilities.Barrage&&barrage!=null&&barrage.shots>=4&&w.Explosions>boomsBefore&&dazedNow,"The grenadier's barrage throws grenades and flashbangs for 10 s");
    grenadier.State.stun=0;dazed.State.stun=0;w.Marks.RemoveAll(m=>m.kind==LanItems.BarrageMark||m.kind==LanItems.GrenadeMark);
    var hider=w.Pawns[7];s.Slots[7].role=3;var seeker=w.Pawns[1];
    Place(hider,Arena+Vector3.forward*8,Vector3.left);Place(seeker,Arena+Vector3.forward*8+Vector3.left*4,Vector3.right);seeker.State.lamp=true;
    hider.State.ability=0;LanAbilities.Trigger(w,hider,new LanInput());
    Check(hider.Ability==LanAbilities.Frenzy&&LanAbilities.DamageBonus(hider)==LanAbilities.FrenzyDamage&&hider.CocaLeft==LanAbilities.CocaDrops,"The coca grower's frenzy doubles his damage and carries coca leaves");
    seeker.State.item=LanItems.Coca;Check(LanItems.Use(w,seeker)&&seeker.State.boost>0&&LanAbilities.DamageBonus(seeker)==LanAbilities.CocaDamage*(LanAbilities.Sniper(seeker)?LanAbilities.SniperDamage:1),"Coca leaves raise the damage of whoever chews them");
    seeker.State.boost=0;seeker.State.lamp=false;
    var trapper=w.Pawns[6];s.Slots[6].role=5;Place(trapper,Arena+Vector3.forward*14,Vector3.right);w.Marks.Clear();
    LanAbilities.Trigger(w,trapper,new LanInput());var trap=w.Marks.FirstOrDefault(m=>m.kind==LanItems.TrapMark);
    Check(trap!=null&&LanAbilities.Cooldown(LanAbilities.Mine)==15&&trapper.State.ability>14,"The trapper lays a mine every 15 s");
    var victim=w.Pawns[1];Place(victim,trapper.transform.position+Vector3.right*3,Vector3.left);Run(s,.3f);Place(victim,trap!=null?trap.position:Arena,Vector3.left);victim.State.stun=5;Run(s,.3f);
    Check(w.Marks.Contains(trap),"The mine needs a second to arm");
    int blastsBefore=w.Explosions;Run(s,LanAbilities.MineArm);
    Info($"Mina: vida del agente {victim.State.hp:0}/{victim.Class.Health:0}");
    Check(!w.Marks.Contains(trap)&&w.Explosions>blastsBefore&&victim.State.hp<victim.Class.Health&&(victim.State.hp<=0||victim.State.marked>0),"Stepping on the mine blows it up and marks the survivor");
    // Bombardeo del Explorador nocturno: humo rojo, unos segundos de aviso y varias explosiones.
    var scout=w.Pawns[1];s.Slots[1].role=5;var bombed=w.Pawns[6];Place(scout,Arena+Vector3.back*16,Vector3.right);Place(bombed,Arena+Vector3.back*16+Vector3.right*10,Vector3.left);bombed.State.stun=20;
    int blasts=w.Explosions;LanAbilities.Trigger(w,scout,new LanInput{reach=10});var strike=w.Marks.FirstOrDefault(m=>m.kind==LanItems.StrikeMark);
    Check(scout.Ability==LanAbilities.Strike&&strike!=null&&Vector3.Distance(strike.position,bombed.transform.position)<.6f,"The night scout marks a position for a strike");
    Run(s,LanAbilities.StrikeDelay-.4f);Check(!strike.fired&&Mathf.Approximately(strike.hp,LanAbilities.StrikeShells)&&w.Marks.Contains(strike),"The strike gives a few seconds of warning");
    Run(s,.4f+LanAbilities.StrikeShells*LanAbilities.StrikeInterval+.2f);
    Info($"Bombardeo: {w.Explosions-blasts} explosiones, vida del objetivo {bombed.State.hp:0}/{bombed.Class.Health:0}");
    Check(w.Explosions-blasts>=LanAbilities.StrikeShells&&bombed.State.hp<bombed.Class.Health*.5f,"The strike drops several shells that hurt whoever stays");
    bombed.State.stun=0;
    // Francotirador: alcance y daño pasivos; su habilidad trae un helicóptero que dispara y deja dos soldados.
    var sniper=w.Pawns[1];s.Slots[1].role=6;var far=w.Pawns[7];Place(sniper,Arena+new Vector3(-15,0,18),Vector3.right);Place(far,Arena+new Vector3(15,0,18),Vector3.left);far.State.stun=5;
    float longShot=Hit(s,sniper,far,Vector3.right);
    Info($"Francotirador a 30 unidades: {longShot:0.0} de daño");
    Check(LanAbilities.Sniper(sniper)&&longShot>20,"The sniper reaches far and hits hard without using his ability");
    Place(far,Arena+new Vector3(1,0,18),Vector3.left);far.State.stun=20;float farHp=far.State.hp;int heliShots=w.HeliShots;
    sniper.State.ability=0;LanAbilities.Trigger(w,sniper,new LanInput{reach=10});var heli=w.Marks.FirstOrDefault(m=>m.kind==LanItems.HeliMark);
    Run(s,LanWorld.HeliDrop+2.5f);var soldiers=w.Npcs.Count(n=>n.kind==LanWorld.Soldier&&n.team==LanNightRules.Dea);
    Info($"Helicóptero: {w.HeliShots-heliShots} disparos, soldados {soldiers}, vida del objetivo {farHp:0} → {far.State.hp:0}");
    Check(sniper.Ability==LanAbilities.Helicopter&&heli!=null&&soldiers==2&&w.HeliShots>heliShots&&far.State.hp<farHp,"The helicopter gives covering fire and drops two soldiers");
    Info($"Helicóptero: siguió al objetivo hasta {Vector3.Distance(new Vector3(heli.focus.x,0,heli.focus.z),new Vector3(far.transform.position.x,0,far.transform.position.z)):0.0} unidades (llamado a 6)");
    Check(Vector3.Distance(new Vector3(heli.focus.x,0,heli.focus.z),new Vector3(far.transform.position.x,0,far.transform.position.z))<2.5f,"The helicopter chases its target");
    far.State.stun=0;
    // Operador de dron: base que lanza drones kamikaze (6 a la vez, 40 en total).
    var pilot=w.Pawns[2];s.Slots[2].role=7;var spotted=w.Pawns[6];Place(pilot,Arena+new Vector3(-6,0,-8),Vector3.right);
    pilot.State.ability=0;LanAbilities.Trigger(w,pilot,new LanInput());var hive=w.Marks.FirstOrDefault(m=>m.kind==LanItems.DroneStationMark);
    Run(s,LanWorld.DroneEvery+.4f);var first=w.Npcs.FirstOrDefault(n=>n.kind==LanWorld.SwarmDrone);
    Place(spotted,(first!=null?first.position:hive.position)+Vector3.right*3,Vector3.left);spotted.State.stun=20;float spottedHp=spotted.State.hp;int blastsBefore2=w.Detonations;Run(s,2);
    int crashes=w.Detonations-blastsBefore2;float hpAfter=spotted.State.hp;Place(spotted,Arena+new Vector3(18,0,-18),Vector3.left);
    Run(s,LanWorld.DroneEvery*7);int flying=w.Npcs.Count(n=>n.kind==LanWorld.SwarmDrone);
    Info($"Enjambre: {flying} drones en el aire, quedan {hive.size:0} en la base, {crashes} choques, vida del objetivo {spottedHp:0} → {hpAfter:0}");
    Check(pilot.Ability==LanAbilities.Swarm&&hive!=null&&flying==LanWorld.DroneActive&&hive.size<=LanWorld.DroneTotal-6,"The drone base keeps six drones flying from its forty");
    Check(crashes>0&&hpAfter<spottedHp,"Drones crash into enemies they find");
    Check(w.SwarmCalled(pilot.Id),"A spotted enemy calls the rest of the swarm to its position");
    spotted.State.stun=0;w.Npcs.Clear();w.Marks.Remove(hive);
    // Sicario: sin pasiva; su habilidad trae un convoy de tres chutos: uno artillado y dos con dos pistoleros cada uno.
    var hitman=w.Pawns[6];s.Slots[6].role=6;var plain=w.Pawns[7];Place(hitman,Arena+new Vector3(-6,0,10),Vector3.right);w.Fire(hitman,Vector3.right);
    Check(hitman.FiredRecently,"The hitman has no silencer passive");
    int vehiclesBefore=w.Vehicles.Count,parkedBefore=w.Parked;w.Npcs.Clear();
    Place(hitman,new Vector3(14,.05f,0),Vector3.right);hitman.State.ability=0;LanAbilities.Trigger(w,hitman,new LanInput{reach=12});
    for(int i=0;i<36&&w.Parked<parkedBefore+3;i++)Run(s,.5f);
    var convoy=w.Vehicles.Where(v=>v.temp).ToList();var gunmen=w.Npcs.Where(n=>n.kind==LanWorld.Gunman).ToList();var technical=convoy.FirstOrDefault(v=>v.ai&&v.crew==2);
    Info($"Convoy: {convoy.Count} chutos, {w.Parked-parkedBefore} estacionados, pistoleros {gunmen.Count}, artillado con {technical?.crew} a bordo");
    Check(hitman.Ability==LanAbilities.Chutos&&LanAbilities.Cooldown(LanAbilities.Chutos)==40&&convoy.Count==3&&w.Parked-parkedBefore==3,"Three chutos drive in as real vehicles");
    Check(gunmen.Count==4&&gunmen.All(n=>n.grenades==2)&&convoy.Count(v=>!v.ai&&v.crew==0&&v.team==LanNightRules.Org)==2,"Two chutos drop their gunmen and stay for the organization to drive");
    var crewBefore=w.Npcs.Count(n=>n.kind==LanWorld.Gunman);if(technical!=null)w.DamageVehicle(technical,9999,1);
    Check(technical!=null&&w.Npcs.Count(n=>n.kind==LanWorld.Gunman)==crewBefore+2&&technical.respawn>0,"The armed chuto's crew jumps out when it is destroyed");
    Run(s,3.5f);Check(technical!=null&&!w.Vehicles.Contains(technical),"Destroyed convoy chutos do not come back");
    w.Npcs.Clear();
    // Curandera: fogata que cura 15 por segundo sin límite de tiempo y se puede destruir.
    var healer=w.Pawns[7];s.Slots[7].role=4;var hurt=w.Pawns[6];Place(healer,Arena+new Vector3(8,0,-10),Vector3.right);Place(hurt,Arena+new Vector3(9,0,-10),Vector3.right);hurt.State.hp=40;hurt.State.stun=3;
    healer.State.ability=0;LanAbilities.Trigger(w,healer,new LanInput());var fire=w.Marks.FirstOrDefault(m=>m.kind==LanItems.CampfireMark);Run(s,1);
    Check(healer.Ability==LanAbilities.Campfire&&fire!=null&&fire.time>1000&&hurt.State.hp>=40+LanAbilities.CampfireHeal*.9f,"The healer's campfire heals allies with no time limit");
    for(int i=0;i<3&&fire!=null&&!fire.fired;i++)w.Explode(fire.position,LanNightRules.Dea,1,LanExplosives.GrenadeRadius,LanExplosives.GrenadeDamage,LanExplosives.PropGrenade,0,0);
    Check(fire!=null&&fire.fired,"The campfire can be destroyed");
    foreach(var p in w.Pawns.Values)p.State.stun=0;
    var smuggler=w.Pawns.Values.First(p=>p.Team==LanNightRules.Org&&!p.IsLeader&&p.Id!=guest.Slot&&p.Id!=6);s.Slots[smuggler.Id].role=7;var looker=w.Pawns[1];var hidden2=w.Pawns[guest.Slot];
    Place(smuggler,Arena+new Vector3(4,0,4),Vector3.right);Place(looker,Arena+new Vector3(3,0,4),Vector3.right);Place(hidden2,Arena+new Vector3(11,0,4),Vector3.left);looker.State.lamp=true;hidden2.State.stun=5;
    bool clearView=LanVision.Sees(w,looker,hidden2);smuggler.State.ability=0;LanAbilities.Trigger(w,smuggler,new LanInput{reach=3});
    var smoke=w.Marks.FirstOrDefault(m=>m.kind==LanItems.SmokeMark);
    Info($"Humo: vista antes {clearView}, después {LanVision.Sees(w,looker,hidden2)}, marcado {hidden2.State.marked:0.0}, humo {(smoke!=null?smoke.position.ToString():"no")}, habilidad {LanAbilities.Name(smuggler.Ability)}, recarga {smuggler.State.ability:0.0}");
    Check(smuggler.Ability==LanAbilities.Smoke&&smoke!=null&&clearView&&!LanVision.Sees(w,looker,hidden2),"The smuggler's smoke blocks the view");
    looker.State.lamp=false;w.Marks.Remove(smoke);foreach(var p in w.Pawns.Values)p.State.stun=0;
    // Lanceros del líder
    var leader=w.LeaderPawn;leader.State.ability=0;w.Npcs.Clear();var post=Arena+new Vector3(-12,0,12);Place(leader,post,Vector3.right);
    LanAbilities.Trigger(w,leader,new LanInput());
    Check(w.Npcs.Count(n=>n.kind==LanWorld.Lancer)==LanWorld.SpearCount&&w.Npcs.Count(n=>n.kind==LanWorld.Gunman&&n.owner==leader.Id)==LanWorld.EscortCount&&leader.State.ability>LanAbilities.Cooldown(LanAbilities.Rally)-1,"The leader summons six spearmen and three armed escorts");
    Run(s,.2f);var npc=w.Npcs[0];var gunner=w.Pawns[1];
    var toward=npc.position-post;toward.y=0;Place(gunner,post+toward.normalized*5.5f,-toward.normalized);
    float before=npc.hp;w.Fire(gunner,(npc.position-gunner.transform.position).normalized);Check(npc.hp<before,"Bullets hurt the spearmen");
    var raider=w.Pawns[3];Place(raider,post+Vector3.right*2,Vector3.left);raider.State.hp=raider.Class.Health;float health=raider.State.hp;
    Run(s,1.5f);Check(raider.State.hp<health,"Spearmen strike nearby DEA agents");
    Run(s,LanWorld.SpearSeconds);Check(!w.Npcs.Any(n=>n.kind==LanWorld.Lancer&&n.owner==leader.Id),"Spearmen leave after their time");
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
