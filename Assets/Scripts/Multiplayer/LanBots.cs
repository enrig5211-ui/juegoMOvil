using System.Linq;
using UnityEngine;
using UnityEngine.AI;
namespace Valle.Lan {
 // Decide qué hace un bot según su rol: líder, guardia o agente de la DEA.
 public sealed class LanBotBrain {
  const float StrafeRange=7,RememberSeconds=6,SearchSeconds=45,FleeSeconds=6;
  readonly LanPawn pawn;readonly LanWorld world;readonly LanSession session;
  const float GuardRange=7;
  Vector3[] corners;int corner;float nextPath,nextChoice,rememberUntil,nextGrenade;Vector3 remembered,destination;bool hasDestination;int seenScan;
  float Now=>session.Night.Clock;
  public LanBotBrain(LanPawn owner,LanWorld map,LanSession host){pawn=owner;world=map;session=host;}
  public void Reset(){corners=null;nextPath=0;hasDestination=false;rememberUntil=0;nextChoice=0;nextGrenade=Now+Random.Range(2f,6f);}
  public LanInput Think(LanInput input){
   var position=pawn.transform.position;
   var enemy=world.Enemy(pawn);
   float enemyDistance=float.MaxValue;
   if(enemy){
    var delta=enemy.transform.position-position;delta.y=0;enemyDistance=delta.magnitude;
    var aim=Quaternion.Euler(0,Mathf.Sin(Now*1.7f+pawn.Id)*4,0)*delta.normalized;
    input.ax=aim.x;input.az=aim.z;input.fire=true;input.sprint=pawn.IsLeader||enemyDistance>10;
    remembered=enemy.transform.position;rememberUntil=Now+RememberSeconds;
   }
   if(pawn.State.ammo==0&&pawn.State.reserve>0)input.reload++;
   if(pawn.State.weapon==0)input.pickup++;
   var goal=pawn.IsLeader?LeaderGoal(enemy):pawn.Team==LanNightRules.Org?GuardGoal(enemy,enemyDistance):AgentGoal(enemy,input);
   bool fleeing=Danger(position,out var away);
   if(fleeing){goal=position+away*6;input.hold=false;input.sprint=true;}
   var direction=Steer(goal);
   if(fleeing&&direction.sqrMagnitude<.01f)direction=away;
   if(enemy&&enemyDistance<StrafeRange&&!pawn.IsLeader&&!input.hold&&!fleeing)direction=Strafe(enemy,position);
   if(!enemy&&direction.sqrMagnitude>.01f){input.ax=direction.x;input.az=direction.z;}
   input.x=direction.x;input.z=direction.z;
   Equip(input,enemy,enemyDistance);
   if(WantsAbility(enemy,enemyDistance)){input.ability++;input.reach=pawn.Ability==LanAbilities.Smoke?Mathf.Clamp(enemyDistance*.5f,1,6):enemy?enemyDistance:3;}
   // Granada: a rivales a media distancia, cada tantos segundos.
   else if(enemy&&pawn.State.grenades>0&&Now>=nextGrenade&&enemyDistance>4.5f&&enemyDistance<11&&!session.Night.Deploying){input.grenade++;input.reach=enemyDistance;nextGrenade=Now+Random.Range(7f,12f);}
   return input;
  }
  // Linterna e items. La DEA busca con linterna; los guardias la encienden solo si están alerta.
  void Equip(LanInput input,LanPawn enemy,float enemyDistance){
   var state=pawn.State;float day=LanVision.Daylight(session);
   bool lamp=day<.5f&&(pawn.Team==LanNightRules.Dea||!pawn.IsLeader&&Now<rememberUntil);
   if(lamp!=state.lamp)input.lamp++;
   if(state.item==0){var drop=world.Nearest(pawn);if(drop!=null&&drop.item>0)input.pickup++;}
   else if(Wants(state.item,enemy,enemyDistance,day))input.use++;
  }
  bool WantsAbility(LanPawn enemy,float distance){
   if(pawn.State.ability>0)return false;
   var leader=world.LeaderPawn;bool nearLeader=leader&&leader!=pawn&&Vector3.Distance(leader.transform.position,pawn.transform.position)<=GuardRange;
   bool mine(int kind)=>world.Marks.Any(m=>m.kind==kind&&m.owner==pawn.Id&&!m.fired);
   bool hurtNear=world.Pawns.Values.Any(p=>p.Team==pawn.Team&&p.State.hp>0&&p.State.hp<p.Class.Health*.7f&&Vector3.Distance(p.transform.position,pawn.transform.position)<=5);
   switch(pawn.Ability){
    case LanAbilities.Supply:{var s=pawn.State;return !enemy&&!session.Night.Deploying&&(s.reserve<pawn.Capacity*2||s.grenades<2||s.hp<pawn.Class.Health*.6f);}
    case LanAbilities.Shield:return enemy;
    case LanAbilities.Scan:return !enemy&&!session.Night.Deploying&&world.LastScan==null&&session.Night.Clock>=world.ScanReady;
    case LanAbilities.Burst:return enemy&&distance<12;
    case LanAbilities.Wall:return enemy&&distance>5&&distance<16&&(nearLeader||pawn.State.hp<pawn.Class.Health*.6f);
    case LanAbilities.Recruit:return enemy&&distance<18;
    case LanAbilities.Rally:return enemy;
    case LanAbilities.MedStation:return hurtNear&&!mine(LanItems.MedStationMark);
    case LanAbilities.Campfire:return !session.Night.Deploying&&(nearLeader||hurtNear)&&!mine(LanItems.CampfireMark);
    case LanAbilities.Barrage:return enemy&&distance>4&&distance<LanAbilities.BarrageRange;
    case LanAbilities.Strike:return enemy&&distance>7&&distance<LanAbilities.StrikeRange;
    case LanAbilities.Helicopter:case LanAbilities.Chutos:return enemy&&distance<LanAbilities.CallRange;
    case LanAbilities.Swarm:return !session.Night.Deploying&&!mine(LanItems.DroneStationMark);
    case LanAbilities.Frenzy:return enemy&&distance<12;
    case LanAbilities.Smoke:return enemy&&distance<12&&(nearLeader||pawn.State.hp<pawn.Class.Health*.6f);
    case LanAbilities.Mine:return !session.Night.Deploying&&(nearLeader||enemy)&&!world.Marks.Any(m=>m.kind==LanItems.TrapMark&&m.team==pawn.Team&&Vector3.Distance(m.position,pawn.transform.position)<6);
   }
   return false;
  }
  bool Wants(int item,LanPawn enemy,float enemyDistance,float day){
   var state=pawn.State;
   switch(item){
    case LanItems.Medkit:return state.hp<pawn.Class.Health*.6f;
    case LanItems.Ammo:return state.weapon!=0&&state.reserve<pawn.Capacity;
    case LanItems.NightVision:return day<.5f&&state.vision<=0;
    case LanItems.Flare:return enemy&&enemyDistance>6;
    case LanItems.Alarm:return !pawn.IsLeader;
   }
   return false;
  }
  Vector3 LeaderGoal(LanPawn enemy){
   // Si la DEA lo rastreó, se cambia a otro refugio.
   var scan=world.LastScan;
   if(scan!=null&&scan.team!=pawn.Team&&scan.id!=seenScan){seenScan=scan.id;var places=world.hideouts;if(places!=null&&places.Length>1){destination=places.OrderBy(h=>Vector3.Distance(h.position,scan.position)).Skip(1).OrderBy(_=>Random.value).First().position;hasDestination=true;nextChoice=Now+FleeSeconds;}}
   if(!hasDestination||(enemy&&Now>=nextChoice)){destination=ChooseHideout(enemy);hasDestination=true;nextChoice=Now+FleeSeconds;}
   return destination;
  }
  // Granadas rivales caídas cerca, bombardeos marcados y fuego: se aleja.
  bool Danger(Vector3 position,out Vector3 away){
   away=Vector3.zero;float worst=0;
   foreach(var mark in world.Marks){
    if(mark.team==pawn.Team)continue;
    float radius=mark.kind==LanItems.GrenadeMark?LanExplosives.GrenadeRadius+1:mark.kind==LanItems.StrikeMark?LanAbilities.StrikeSpread+LanExplosives.ShellRadius:mark.kind==LanItems.FireMark?mark.size+.6f:0;
    if(radius<=0)continue;
    var delta=Flat(position-mark.position);float distance=delta.magnitude;
    if(distance>=radius||radius-distance<=worst)continue;
    worst=radius-distance;away=distance>.1f?delta/distance:Quaternion.Euler(0,pawn.Id*45,0)*Vector3.forward;
   }
   // Los agentes se apartan de los kamikazes que vienen a por ellos.
   if(pawn.Team==LanNightRules.Dea)foreach(var npc in world.Npcs){
    if(npc.kind!=LanWorld.Kamikaze)continue;var delta=Flat(position-npc.position);float distance=delta.magnitude;
    if(distance<5&&5-distance>worst){worst=5-distance;away=distance>.1f?delta/distance:Vector3.forward;}
   }
   return worst>0;
  }
  Vector3 ChooseHideout(LanPawn enemy){
   var places=world.hideouts;
   if(places==null||places.Length==0)return pawn.transform.position;
   if(enemy)return places.OrderByDescending(h=>(h.position-enemy.transform.position).sqrMagnitude).First().position;
   return places[Random.Range(0,places.Length)].position;
  }
  Vector3 GuardGoal(LanPawn enemy,float enemyDistance){
   var leader=world.LeaderPawn;
   if(!leader)return pawn.transform.position;
   bool threat=enemy&&enemyDistance<LanNightRules.BotVision*.7f&&Vector3.Distance(enemy.transform.position,leader.transform.position)<16;
   if(threat)return enemy.transform.position;
   return leader.transform.position+Quaternion.Euler(0,pawn.Id*120,0)*Vector3.forward*3.5f;
  }
  Vector3 AgentGoal(LanPawn enemy,LanInput input){
   var leader=world.LeaderPawn;
   if(leader&&leader.State.down>0){
    float distance=Vector3.Distance(pawn.transform.position,leader.transform.position);
    if(distance<=LanNightRules.CuffRange*.8f){input.hold=true;return pawn.transform.position;}
    if(distance<30)return leader.transform.position;
   }
   if(enemy)return enemy.transform.position;
   // Un rastreo del equipo indica dónde estaba el líder: van a buscar ahí.
   var scan=world.LastScan;
   if(scan!=null&&scan.team==pawn.Team&&scan.id!=seenScan){seenScan=scan.id;remembered=scan.position;rememberUntil=Now+25;}
   if(Now<rememberUntil){if(Vector3.Distance(pawn.transform.position,remembered)>3)return remembered;rememberUntil=0;}
   bool arrived=hasDestination&&Vector3.Distance(pawn.transform.position,destination)<2.5f;
   if(!hasDestination||arrived||Now>=nextChoice){destination=ChooseZone();hasDestination=true;nextChoice=Now+SearchSeconds;}
   return destination;
  }
  // La DEA no sabe dónde están los refugios: busca en zonas generales y puntos de items.
  Vector3 ChooseZone(){
   bool NearHideout(Transform t)=>world.hideouts!=null&&world.hideouts.Any(h=>Vector3.Distance(h.position,t.position)<7);
   var places=world.zones.Concat(world.itemSpots??new Transform[0]).Where(z=>!NearHideout(z)).ToArray();
   if(places==null||places.Length==0)return pawn.transform.position;
   return places[Random.Range(0,places.Length)].position;
  }
  Vector3 Steer(Vector3 goal){
   var position=pawn.transform.position;
   if(Now>=nextPath){
    nextPath=Now+.55f+pawn.Id*.02f;
    var path=new NavMeshPath();
    bool routed=NavMesh.SamplePosition(position,out var start,2,NavMesh.AllAreas)&&NavMesh.SamplePosition(goal,out var end,2,NavMesh.AllAreas)&&NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
    if(routed){corners=path.corners;corner=path.corners.Length>1?1:0;}else corners=null;
   }
   if(corners==null||corner>=corners.Length)return Vector3.zero;
   while(corner<corners.Length-1&&Flat(corners[corner]-position).magnitude<.4f)corner++;
   var direction=Flat(corners[corner]-position);
   return direction.magnitude<.25f?Vector3.zero:direction.normalized;
  }
  Vector3 Strafe(LanPawn enemy,Vector3 position){
   var side=Vector3.Cross(Vector3.up,Flat(enemy.transform.position-position).normalized)*Mathf.Sin(Now+pawn.Id);
   return NavMesh.SamplePosition(position+side*.7f,out _,.4f,NavMesh.AllAreas)?side.normalized:Vector3.zero;
  }
  static Vector3 Flat(Vector3 value){value.y=0;return value;}
 }
}
