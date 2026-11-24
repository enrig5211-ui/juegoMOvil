using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
namespace Valle.Lan {
 // Valores de granadas, bombardeo, fuego y barriles. Se ajustan aquí al probar.
 public static class LanExplosives {
  // Cada agente de la DEA entra con 5 granadas; la organización, con 2, y el líder, con 1.
  public const int GrenadeDea=5,GrenadeOrg=2,GrenadeLeader=1,GrenadeMax=6;
  public const float GrenadeRange=12,GrenadeMin=2,GrenadeFlight=.7f,GrenadeFuse=1.8f,GrenadeRadius=3.8f,GrenadeDamage=95,GrenadeCooldown=1;
  public const float GrenadeFire=1.3f,GrenadeBurn=4.5f;
  public const float ShellRadius=3.2f,ShellDamage=65,ShellFire=1.6f,ShellBurn=5;
  public const float BarrelRadius=3,BarrelDamage=70,BarrelFire=1.5f,BarrelBurn=6;
  public const float FireDps=12,FireLight=2.5f,BlastLight=5,CoverFactor=.25f,WallBlast=2.2f,PropGrenade=2,PropShell=4;
  // El líder lleva chaleco: las explosiones le hacen menos daño.
  public const float LeaderBlast=.6f;
  public static int StartGrenades(LanPawn pawn)=>pawn.IsLeader?GrenadeLeader:pawn.Team==LanNightRules.Org?GrenadeOrg:GrenadeDea;
 }
 // Explosivos en el anfitrión: granadas, explosiones, fuego, bombardeo, murallas y rastreo del líder.
 // Todo viaja como marcas; cada cliente dibuja lo que recibe (LanWorld.Marks.cs).
 public sealed partial class LanWorld {
  readonly Dictionary<int,float> grenadeReady=new Dictionary<int,float>();
  public int Explosions{get;private set;}public int Scans{get;private set;}public int Walls{get;private set;}public int Strikes{get;private set;}
  // Qué derribó al líder en esta noche (para ajustar el equilibrio con bots).
  public readonly Dictionary<string,int> LeaderDowns=new Dictionary<string,int>();
  public void CountDown(LanPawn p,string cause){if(p.IsLeader)LeaderDowns[cause]=LeaderDowns.TryGetValue(cause,out var n)?n+1:1;}
  public bool ThrowGrenade(LanPawn pawn,float reach){
   var state=pawn.State;var night=session.Night;
   if(state.grenades<=0||state.hp<=0||state.stun>0||night.Deploying||night.Capturer==pawn.Id)return false;
   if(grenadeReady.TryGetValue(pawn.Id,out var ready)&&night.Clock<ready)return false;
   grenadeReady[pawn.Id]=night.Clock+LanExplosives.GrenadeCooldown;
   var target=LanAbilities.Throw(pawn,Mathf.Clamp(reach>0?reach:8,LanExplosives.GrenadeMin,LanExplosives.GrenadeRange));
   var mark=AddMark(LanItems.GrenadeMark,pawn.Team,target+Vector3.up*.08f,LanExplosives.GrenadeFuse);
   mark.from=pawn.transform.position+Vector3.up*.6f;mark.owner=pawn.Id;mark.size=LanExplosives.GrenadeRadius;
   state.grenades--;return true;
  }
  // Daña a los rivales del equipo que la causó (team -1: a todos), con menos daño tras una cobertura.
  public void Explode(Vector3 at,int team,int owner,float radius,float damage,float propFactor,float fireRadius,float fireSeconds){
   Explosions++;
   var blast=AddMark(LanItems.ExplosionMark,team,at,.8f);blast.size=radius;blast.owner=owner;
   foreach(var p in Pawns.Values){
    if(p.State.hp<=0||p.Team==team||p.State.shield>0||p.State.ride>0)continue;
    var delta=p.transform.position-at;if(Mathf.Abs(delta.y)>2.5f)continue;delta.y=0;float distance=delta.magnitude;if(distance>radius)continue;
    float cover=LanVision.Clear(at+Vector3.up*.5f,p.transform.position+Vector3.up*.35f)?1:LanExplosives.CoverFactor;
    Hurt(p,Mathf.Lerp(damage,damage*.25f,distance/radius)*100/(100+p.Class.Defense)*LanAbilities.Protection(this,at,p)*cover*(p.IsLeader?LanExplosives.LeaderBlast:1),owner,radius>LanExplosives.GrenadeRadius-.1f?"granada":"bombardeo");
   }
   foreach(var npc in Npcs){if(npc.team==team||npc.kind==SwarmDrone)continue;float distance=Flat(npc.position-at);if(distance<=radius)npc.hp-=Mathf.Lerp(damage,damage*.25f,distance/radius);}
   foreach(var wall in Marks.ToArray()){
    if(!Structure(wall.kind)||wall.fired||wall.team==team)continue;
    float reach=radius+(wall.kind==LanItems.WallMark?LanAbilities.WallWidth*.5f:.5f),distance=Flat(wall.position-at);
    if(distance<=reach)DamageWall(wall,Mathf.Lerp(damage,damage*.3f,distance/reach)*LanExplosives.WallBlast);
   }
   BlastVehicles(at,team,owner,radius,damage);
   DamageProps(at,radius*1.15f,damage*propFactor);
   if(fireRadius>0){var fire=AddMark(LanItems.FireMark,team,at,fireSeconds);fire.size=fireRadius;fire.owner=owner;}
  }
  public void Hurt(LanPawn p,float amount,int owner,string cause="fuego"){
   if(p.State.hp<=0||amount<=0)return;
   p.State.hp=Mathf.Max(0,p.State.hp-amount);session.Night.Interrupt(p.Id);
   if(p.State.hp>0)return;
   CountDown(p,cause);p.Fall();
   if(owner>=0&&Pawns.TryGetValue(owner,out var killer)&&killer.Team!=p.Team)session.ScoreKill(owner,p.Id);else session.Slots[p.Id].deaths++;
  }
  void TickFire(LanMark fire,float dt){
   foreach(var p in Pawns.Values){
    if(p.State.hp<=0||p.Team==fire.team||p.State.shield>0||p.State.ride>0||Flat(p.transform.position-fire.position)>fire.size)continue;
    Hurt(p,LanExplosives.FireDps*dt*100/(100+p.Class.Defense),fire.owner);
   }
  }
  // ---------- Bombardeo ----------
  public void CallStrike(LanPawn pawn,Vector3 at){
   var mark=AddMark(LanItems.StrikeMark,pawn.Team,at,LanAbilities.StrikeDelay);
   mark.hp=LanAbilities.StrikeShells;mark.owner=pawn.Id;mark.size=LanAbilities.StrikeSpread;Strikes++;
  }
  // Devuelve true cuando cayeron todas las cargas.
  bool TickStrike(LanMark mark,float dt){
   mark.time-=dt;
   if(mark.time>0)return false;
   if(mark.hp<=0)return true;
   bool first=!mark.fired;mark.fired=true;mark.hp--;mark.time=LanAbilities.StrikeInterval;
   var offset=Random.insideUnitCircle*(first?1:LanAbilities.StrikeSpread);
   Explode(mark.position+new Vector3(offset.x,0,offset.y),mark.team,mark.owner,LanExplosives.ShellRadius,LanExplosives.ShellDamage,LanExplosives.PropShell,LanExplosives.ShellFire,LanExplosives.ShellBurn);
   return false;
  }
  // ---------- Muralla ----------
  public bool BuildWall(LanPawn pawn){
   var direction=pawn.State.aim;direction.y=0;if(direction.sqrMagnitude<.01f)direction=Vector3.forward;direction.Normalize();
   var rotation=Quaternion.LookRotation(direction);var half=new Vector3(LanAbilities.WallWidth*.5f,LanAbilities.WallHeight*.5f,LanAbilities.WallDepth*.5f);
   foreach(float distance in new[]{LanAbilities.WallDistance,LanAbilities.WallDistance*.7f}){
    var center=pawn.transform.position+direction*distance;
    if(Physics.CheckBox(center+Vector3.up*(half.y+.08f),half,rotation,Physics.AllLayers,QueryTriggerInteraction.Ignore))continue;
    var mine=Marks.Where(m=>m.kind==LanItems.WallMark&&m.owner==pawn.Id&&!m.fired).ToList();
    if(mine.Count>=LanAbilities.WallsPerOwner){mine[0].fired=true;mine[0].hp=0;mine[0].time=.6f;RefreshWall(mine[0]);}
    var wall=AddMark(LanItems.WallMark,pawn.Team,new Vector3(center.x,pawn.transform.position.y,center.z),LanAbilities.WallSeconds);
    wall.from=direction;wall.hp=LanAbilities.WallHealth;wall.owner=pawn.Id;wall.size=LanAbilities.WallWidth;
    View(wall);Physics.SyncTransforms();Walls++;return true;
   }
   return false;
  }
  public void DamageWall(LanMark wall,float amount){
   if(wall.fired)return;
   wall.hp-=amount;
   if(wall.hp<=0){wall.fired=true;wall.time=.6f;RefreshWall(wall);}
  }
  // Las balas rivales también la desgastan; las propias solo chocan.
  bool HitWall(Collider collider,LanPawn shooter,float damage){
   var view=collider.GetComponent<LanMarkView>();if(!view)return false;
   var wall=Marks.Find(m=>m.id==view.Id);
   if(wall!=null&&wall.team!=shooter.Team)DamageWall(wall,damage);
   return true;
  }
  // ---------- Suministro, cúpula y minas ----------
  // Una avioneta cruza y suelta una caja en paracaídas; al tocar el suelo deja botiquín, munición, granadas y un rifle.
  public void CallSupply(LanPawn pawn,Vector3 at){
   if(NavMesh.SamplePosition(at,out var hit,3,NavMesh.AllAreas))at=hit.position;
   var mark=AddMark(LanItems.SupplyMark,pawn.Team,at,LanAbilities.SupplyTime);mark.owner=pawn.Id;
   var heading=Random.insideUnitCircle.normalized;mark.from=new Vector3(heading.x,0,heading.y);
  }
  public int Supplies{get;private set;}
  void DropSupply(LanMark mark){
   Supplies++;var at=mark.position;int k=0;
   Vector3 Spot(){var offset=Quaternion.Euler(0,k++*90+45,0)*Vector3.forward*1.1f;return NavMesh.SamplePosition(at+offset,out var hit,1.5f,NavMesh.AllAreas)?hit.position:at+offset;}
   PlaceItem(Spot(),LanItems.Medkit);PlaceItem(Spot(),LanItems.Ammo);PlaceItem(Spot(),LanItems.Grenades);
   AddDrop(new LanDrop{weapon=2,ammo=24,reserve=120,position=Spot()+Vector3.up*.025f});
  }
  public void RaiseDome(LanPawn pawn){var mark=AddMark(LanItems.ShieldMark,pawn.Team,pawn.transform.position,LanAbilities.DomeSeconds);mark.size=LanAbilities.DomeRadius;mark.owner=pawn.Id;}
  public void PlaceMine(LanPawn pawn){
   var mine=Marks.Where(m=>m.kind==LanItems.TrapMark&&m.owner==pawn.Id).ToList();
   if(mine.Count>=LanAbilities.MinesPerOwner)Marks.Remove(mine[0]);
   AddMark(LanItems.TrapMark,pawn.Team,pawn.transform.position,0).owner=pawn.Id;
  }
  // La mina se arma en 1 s; un rival encima la hace estallar y quien sobreviva queda marcado.
  void TickMine(LanMark mine){
   if(mine.age<LanAbilities.MineArm||session.Night.Deploying)return;
   if(!Pawns.Values.Any(p=>p.Team!=mine.team&&p.State.hp>0&&Flat(p.transform.position-mine.position)<=LanAbilities.MineRadius))return;
   Marks.Remove(mine);Explode(mine.position,mine.team,mine.owner,LanAbilities.MineBlast,LanAbilities.MineDamage,1.5f,.9f,3);
   foreach(var p in Pawns.Values)if(p.Team!=mine.team&&p.State.hp>0&&Flat(p.transform.position-mine.position)<=LanAbilities.MineBlast+1)p.State.marked=LanAbilities.MarkSeconds;
  }
  // ---------- Dron y humo ----------
  public void ThrowSmoke(LanPawn pawn,Vector3 at){var mark=AddMark(LanItems.SmokeMark,pawn.Team,at,LanAbilities.SmokeSeconds);mark.size=LanAbilities.SmokeRadius;mark.owner=pawn.Id;}
  // ---------- Rastreo ----------
  // El rastreo es del equipo: el primero, al minuto de empezar la noche; luego, uno por minuto aunque el rastreador caiga.
  public float ScanReady{get;private set;}=LanNightRules.DeploySeconds+LanAbilities.ScanFirst;
  public bool ScanLeader(LanPawn pawn){
   var leader=LeaderPawn;if(!leader||session.Night.Clock<ScanReady)return false;
   ScanReady=session.Night.Clock+LanAbilities.ScanEvery;
   var mark=AddMark(LanItems.ScanMark,pawn.Team,leader.transform.position,LanAbilities.ScanSeconds);
   mark.owner=pawn.Id;mark.size=3;Scans++;return true;
  }
  // Último rastreo vigente (para la interfaz y los bots).
  public LanMark LastScan=>Marks.LastOrDefault(m=>m.kind==LanItems.ScanMark);
 }
}
