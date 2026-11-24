using UnityEngine;
namespace Valle.Lan {
 // Una habilidad por personaje, con clic derecho. El anfitrión decide sus efectos.
 public static class LanAbilities {
  public const int None=0,Supply=1,Shield=2,Scan=3,Burst=4,Wall=5,Recruit=6,Rally=7,MedStation=8,Barrage=9,Strike=10,Frenzy=11,Mine=12,Helicopter=13,Swarm=14,Chutos=15,Smoke=16,Campfire=17;
  public const float MarkSeconds=4,ShieldAngle=70;
  public const float ShieldFactor=.2f,BurstFactor=.5f;
  public const float StunRadius=3.5f,StunSeconds=2,ThrowReach=8,AmbushReveal=2;
  // Estación médica del Paramédico: cura 30 por segundo en 4 unidades durante 40 s; se puede destruir.
  public const float StationHeal=30,StationRadius=4,StationSeconds=40,StationHealth=150;
  // Fogata de la Curandera: cura 15 por segundo sin límite de tiempo hasta que la destruyan; ilumina.
  public const float CampfireHeal=15,CampfireRadius=3.5f,CampfireHealth=100;
  // Andanada del Granadero: 10 s lanzando granadas y aturdidoras, una por segundo, hacia donde apunta.
  public const float BarrageSeconds=10,BarrageEvery=1,BarrageRange=12,BarrageSpread=2.5f;
  // Frenesí del Cocalero: doble daño 8 s y va dejando 10 hojas de coca (quien las consume hace más daño 6 s).
  public const float FrenzyDamage=2,CocaDamage=1.5f,CocaSeconds=6,CocaStep=1.2f;public const int CocaDrops=10;
  // Pasiva del Francotirador: más alcance y daño.
  public const float SniperDamage=2.2f,SniperRange=36,SniperDelay=1.4f;
  // Mina del Trampero: se arma en 1 s, estalla al pisarla y marca a quien sobreviva. Hasta 3 por Trampero.
  public const float MineRadius=1.2f,MineArm=1,MineBlast=2.8f,MineDamage=80;public const int MinesPerOwner=3;
  // Cúpula del Pesado: los aliados dentro reciben la mitad de daño.
  public const float DomeRadius=3.5f,DomeSeconds=6,DomeFactor=.5f;
  // Suministro aéreo del Asaltante: una avioneta suelta una caja en paracaídas donde apunta.
  public const float SupplyRange=16,SupplyTime=6,SupplyStay=20;
  // Humo: tapa la vista en su radio.
  public const float SmokeReach=10,SmokeSeconds=10,SmokeRadius=4;
  // Helicóptero, enjambre y chutos: alcance del punto marcado.
  public const float CallRange=22;
  // Rastreo: la DEA ve dónde estaba el líder en ese momento, y el líder sabe que lo ubicaron.
  public const float ScanSeconds=8,ScanFirst=60,ScanEvery=60;
  // Bombardeo: humo rojo de aviso y luego varias explosiones alrededor del punto marcado.
  public const float StrikeRange=18,StrikeDelay=3.5f,StrikeSpread=4.5f,StrikeInterval=.4f;public const int StrikeShells=5;
  // Muralla: barricada de sacos que frena balas, paso, vista y metralla hasta que la destruyen.
  // Escolta (pasiva del Guardaespaldas): el líder recibe menos daño si tiene uno vivo cerca.
  public const float EscortRange=6,EscortFactor=.7f;
  public const float WallWidth=3.2f,WallHeight=1.1f,WallDepth=.5f,WallHealth=300,WallSeconds=60,WallDistance=1.5f;public const int WallsPerOwner=2;
  static readonly string[] names={"","Suministro","Escudo","Rastreo","Ráfaga","Muralla","Reclutar","Lanceros","Estación médica","Andanada","Bombardeo","Frenesí","Mina","Helicóptero","Enjambre","Chutos","Humo","Fogata"};
  static readonly float[] cooldowns={0,45,16,60,8,15,30,60,45,30,50,25,15,60,90,40,18,20};
  static readonly float[] durations={0,0,4,0,6,0,0,0,0,10,0,8,0,0,0,0,0,0};
  static readonly int[] dea={Supply,Shield,Scan,MedStation,Barrage,Strike,Helicopter,Swarm},org={Burst,Wall,Recruit,Frenzy,Campfire,Mine,Chutos,Smoke};
  public static bool Sniper(LanPawn pawn)=>!pawn.IsLeader&&pawn.Team==LanNightRules.Dea&&pawn.Role==6;
  // Daño de cada disparo: pasiva del Francotirador, Frenesí del Cocalero y hojas de coca.
  public static float DamageBonus(LanPawn pawn)=>(Sniper(pawn)?SniperDamage:1)*(Active(pawn,Frenzy)?FrenzyDamage:1)*(pawn.State.boost>0?CocaDamage:1);
  public static int For(LanPawn pawn)=>pawn.IsLeader?Rally:(pawn.Team==LanNightRules.Dea?dea:org)[Mathf.Clamp(pawn.Role,0,LanClass.Roles-1)];
  public static string Name(int ability)=>names[Mathf.Clamp(ability,0,names.Length-1)];
  public static float Cooldown(int ability)=>cooldowns[Mathf.Clamp(ability,0,cooldowns.Length-1)];
  public static float Cooldown(LanPawn pawn)=>Cooldown(pawn.Ability);
  public static bool Active(LanPawn pawn,int ability)=>pawn.Ability==ability&&pawn.State.active>0;
  public static void Trigger(LanWorld world,LanPawn pawn,LanInput input){
   var state=pawn.State;int ability=pawn.Ability;
   if(state.ability>0||state.hp<=0||state.stun>0)return;
   switch(ability){
    case Supply:world.CallSupply(pawn,Target(pawn,input.reach,SupplyRange));break;
    case Shield:world.RaiseDome(pawn);break;
    case Scan:if(!world.ScanLeader(pawn))return;break;
    case Recruit:if(world.Recruit(pawn)==0)return;break;
    case Mine:world.PlaceMine(pawn);break;
    case MedStation:world.PlaceStation(pawn,LanItems.MedStationMark);break;
    case Campfire:world.PlaceStation(pawn,LanItems.CampfireMark);break;
    case Swarm:world.PlaceStation(pawn,LanItems.DroneStationMark);break;
    case Barrage:world.StartBarrage(pawn,Target(pawn,input.reach,BarrageRange));break;
    case Helicopter:world.CallHelicopter(pawn,Target(pawn,input.reach,CallRange));break;
    case Chutos:if(!world.CallChutos(pawn,Target(pawn,input.reach,CallRange)))return;break;
    case Frenzy:pawn.CocaLeft=CocaDrops;break;
    case Smoke:world.ThrowSmoke(pawn,Throw(pawn,Mathf.Clamp(input.reach>0?input.reach:5,1,SmokeReach)));break;
    case Rally:if(world.SummonSpearmen(pawn)==0)return;break;
    case Wall:if(!world.BuildWall(pawn))return;break;
    case Strike:world.CallStrike(pawn,Target(pawn,input.reach,StrikeRange));break;
   }
   state.active=durations[ability];state.ability=Cooldown(pawn);pawn.AbilityUses++;
  }
  // Punto de lanzamiento: hasta 8 unidades al frente, sin atravesar obstáculos.
  public static Vector3 Ahead(LanPawn pawn)=>Throw(pawn,ThrowReach);
  public static Vector3 Throw(LanPawn pawn,float reach){
   var origin=pawn.transform.position+Vector3.up*.35f;
   if(Physics.Raycast(origin,pawn.State.aim,out var hit,reach,LanNightRules.SightMask,QueryTriggerInteraction.Ignore)&&!(hit.collider is CharacterController))reach=Mathf.Max(0,hit.distance-.5f);
   return pawn.transform.position+pawn.State.aim*reach;
  }
  // Punto apuntado con el ratón (o por el bot), sin pasar del alcance.
  public static Vector3 Target(LanPawn pawn,float reach,float max)=>pawn.transform.position+pawn.State.aim*Mathf.Clamp(reach>0?reach:max*.6f,2,max);
  // Daño recibido: escudo frontal del Pesado, cúpula del equipo y escolta del líder. Vale para balas y explosiones.
  public static float DamageTaken(LanWorld world,LanPawn shooter,LanPawn victim)=>Protection(world,shooter.transform.position,victim);
  public static float Protection(LanWorld world,Vector3 source,LanPawn victim){
   float factor=DamageFrom(source,victim)*(victim.IsLeader?Escort(world,victim):1);
   foreach(var mark in world.Marks)if(mark.kind==LanItems.ShieldMark&&mark.team==victim.Team&&Flat(mark.position-victim.transform.position)<=mark.size){factor*=DomeFactor;break;}
   return factor;
  }
  public static float Escort(LanWorld world,LanPawn leader){
   foreach(var guard in world.Pawns.Values)if(guard!=leader&&guard.Team==leader.Team&&guard.State.hp>0&&guard.Ability==Wall&&Flat(guard.transform.position-leader.transform.position)<=EscortRange)return EscortFactor;
   return 1;
  }
  public static float DamageFrom(Vector3 source,LanPawn victim){
   if(!Active(victim,Shield))return 1;
   var toSource=source-victim.transform.position;toSource.y=0;
   return Vector3.Angle(victim.State.aim,toSource)<ShieldAngle?ShieldFactor:1;
  }
  public static float FireDelay(LanPawn pawn)=>Active(pawn,Burst)?BurstFactor:Sniper(pawn)?SniperDelay:1;
  static float Flat(Vector3 v){v.y=0;return v.magnitude;}
 }
}
