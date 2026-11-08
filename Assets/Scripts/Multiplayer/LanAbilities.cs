using UnityEngine;
namespace Valle.Lan {
 // Una habilidad por personaje, con clic derecho. El anfitrión decide sus efectos.
 public static class LanAbilities {
  public const int None=0,Dash=1,Shield=2,Pulse=3,Burst=4,Guard=5,Watch=6,Rally=7,Heal=8,Stun=9,Flare=10,Ambush=11,Trap=12;
  public const float DashSpeed=3.2f,PulseRange=14,MarkSeconds=4,GuardRange=6,ShieldAngle=70;
  public const float ShieldFactor=.2f,GuardFactor=.4f,BurstFactor=.5f;
  public const float HealAmount=40,HealRange=6,StunRadius=3.5f,StunSeconds=2,ThrowReach=8,AmbushReveal=2,TrapRadius=1.2f,TrapStun=2.5f;
  static readonly string[] names={"","Avance","Escudo","Pulso","Ráfaga","Proteger","Alarma","Lanceros","Curar","Aturdidora","Bengala","Emboscada","Trampa"};
  static readonly float[] cooldowns={0,8,14,22,12,15,20,90,18,16,20,18,20};
  static readonly float[] durations={0,.35f,4,0,3,5,0,0,0,0,0,5,0};
  static readonly int[] dea={Dash,Shield,Pulse,Heal,Stun,Flare},org={Burst,Guard,Watch,Ambush,Heal,Trap};
  public static int For(LanPawn pawn)=>pawn.IsLeader?Rally:(pawn.Team==LanNightRules.Dea?dea:org)[Mathf.Clamp(pawn.Role,0,LanClass.Roles-1)];
  public static string Name(int ability)=>names[Mathf.Clamp(ability,0,names.Length-1)];
  public static float Cooldown(int ability)=>cooldowns[Mathf.Clamp(ability,0,cooldowns.Length-1)];
  public static bool Active(LanPawn pawn,int ability)=>pawn.Ability==ability&&pawn.State.active>0;
  public static void Trigger(LanWorld world,LanPawn pawn,LanInput input){
   var state=pawn.State;int ability=pawn.Ability;
   if(state.ability>0||state.hp<=0||state.stun>0)return;
   switch(ability){
    case Dash:{var move=new Vector3(input.x,0,input.z);pawn.DashDirection=(move.sqrMagnitude>.01f?move:state.aim).normalized;break;}
    case Pulse:
     foreach(var enemy in world.Pawns.Values)if(enemy.Team!=pawn.Team&&enemy.State.hp>0&&Flat(enemy.transform.position-pawn.transform.position)<=PulseRange)enemy.State.marked=MarkSeconds;
     break;
    case Watch:world.AddMark(LanItems.AlarmMark,pawn.Team,pawn.transform.position,0);break;
    case Trap:world.AddMark(LanItems.TrapMark,pawn.Team,pawn.transform.position,0);break;
    case Rally:if(world.SummonSpearmen(pawn)==0)return;break;
    case Heal:
     foreach(var ally in world.Pawns.Values)if(ally.Team==pawn.Team&&ally.State.hp>0&&Flat(ally.transform.position-pawn.transform.position)<=HealRange)ally.State.hp=Mathf.Min(ally.Class.Health,ally.State.hp+HealAmount);
     break;
    case Flare:world.AddMark(LanItems.FlareMark,pawn.Team,Ahead(pawn),LanNightRules.FlareSeconds);break;
    case Stun:{
     var at=Ahead(pawn);world.AddMark(LanItems.BlastMark,pawn.Team,at,.5f);
     foreach(var enemy in world.Pawns.Values)if(enemy.Team!=pawn.Team&&enemy.State.hp>0&&Flat(enemy.transform.position-at)<=StunRadius)enemy.State.stun=StunSeconds;
     break;}
   }
   state.active=durations[ability];state.ability=cooldowns[ability];pawn.AbilityUses++;
  }
  // Punto de lanzamiento: hasta 8 unidades al frente, sin atravesar obstáculos.
  public static Vector3 Ahead(LanPawn pawn){
   var origin=pawn.transform.position+Vector3.up*.35f;float reach=ThrowReach;
   if(Physics.Raycast(origin,pawn.State.aim,out var hit,reach,LanNightRules.SightMask,QueryTriggerInteraction.Ignore))reach=Mathf.Max(0,hit.distance-.5f);
   return pawn.transform.position+pawn.State.aim*reach;
  }
  // Escudo del Pesado (solo de frente) y protección del Guardaespaldas sobre el líder.
  public static float DamageTaken(LanWorld world,LanPawn shooter,LanPawn victim){
   float factor=1;
   if(Active(victim,Shield)){var toShooter=shooter.transform.position-victim.transform.position;toShooter.y=0;if(Vector3.Angle(victim.State.aim,toShooter)<ShieldAngle)factor*=ShieldFactor;}
   if(victim.IsLeader)foreach(var guard in world.Pawns.Values)if(guard.Team==victim.Team&&Active(guard,Guard)&&Flat(guard.transform.position-victim.transform.position)<=GuardRange){factor*=GuardFactor;break;}
   return factor;
  }
  public static float FireDelay(LanPawn pawn)=>Active(pawn,Burst)?BurstFactor:1;
  static float Flat(Vector3 v){v.y=0;return v.magnitude;}
 }
}
