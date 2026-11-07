using System.Linq;
using UnityEngine;
namespace Valle.Lan {
 // Valores de la noche. Se ajustan aquí al probar la partida.
 public static class LanNightRules {
  public const string Scene="NocheChapare";
  public const int Dea=0,Org=1;
  public const float DeploySeconds=90,NightSeconds=480;
  public const float ReducedSeconds=20,RecoverFraction=.4f;
  public const float CuffSeconds=5,CuffRange=1.5f;
  public const float WaveSeconds=30;
  public const int DeaReinforcements=12,OrgReinforcements=8;
  public const float CameraSize=7,BotVision=14;
  // La vista y las balas pasan sobre el agua; las cajas del río solo bloquean el paso.
  public const int SightMask=~(1<<4);
 }
 public enum LanOutcome{None,Captured,Dawn,DeaSpent}
 // Estado de la noche. El anfitrión lo decide; los clientes solo lo reciben.
 public sealed class LanNight {
  public int Leader=-1,DeaLeft,OrgLeft,Capturer=-1;
  public float Deploy,Wave,Capture,Clock;
  public LanOutcome Outcome;
  public bool Deploying=>Deploy>0;
  public void Begin(LanSlot[] slots){
   Deploy=LanNightRules.DeploySeconds;
   Wave=LanNightRules.WaveSeconds;
   DeaLeft=LanNightRules.DeaReinforcements;
   OrgLeft=LanNightRules.OrgReinforcements;
   Capture=0;Capturer=-1;Clock=0;Outcome=LanOutcome.None;
   if(Leader<0){
    var slot=slots.FirstOrDefault(s=>s.team==LanNightRules.Org&&s.bot)??slots.First(s=>s.team==LanNightRules.Org);
    Leader=slot.id;
   }
  }
  public void Write(LanPacket packet){
   packet.leader=Leader;packet.deaLeft=DeaLeft;packet.orgLeft=OrgLeft;packet.outcome=(int)Outcome;
   packet.deploy=Deploy;packet.wave=Wave;packet.capture=Capture;
  }
  public void Read(LanPacket packet){
   Leader=packet.leader;DeaLeft=packet.deaLeft;OrgLeft=packet.orgLeft;Outcome=(LanOutcome)packet.outcome;
   Deploy=packet.deploy;Wave=packet.wave;Capture=packet.capture;
  }
  public void Tick(float dt,LanSession session){
   Clock+=dt;
   if(Outcome!=LanOutcome.None)return;
   if(Deploying){Deploy=Mathf.Max(0,Deploy-dt);return;}
   var world=session.World;
   session.Remaining=Mathf.Max(0,session.Remaining-dt);
   Wave-=dt;
   if(Wave<=0){Wave=LanNightRules.WaveSeconds;ReleaseWave(world);}
   TickLeader(dt,world);
   if(Outcome!=LanOutcome.None)return;
   if(session.Remaining<=0)Outcome=LanOutcome.Dawn;
   else if(DeaLeft<=0&&!world.Pawns.Values.Any(p=>p.Team==LanNightRules.Dea&&p.State.hp>0))Outcome=LanOutcome.DeaSpent;
  }
  // Recibir daño corta las esposas de quien las estaba poniendo.
  public void Interrupt(int pawn){if(pawn==Capturer){Capturer=-1;Capture=0;}}
  void ReleaseWave(LanWorld world){
   foreach(var pawn in world.Pawns.Values){
    if(pawn.State.hp>0||pawn.Id==Leader)continue;
    if(pawn.Team==LanNightRules.Dea){if(DeaLeft<=0)continue;DeaLeft--;}
    else{if(OrgLeft<=0)continue;OrgLeft--;}
    world.Respawn(pawn.Id);
   }
  }
  void TickLeader(float dt,LanWorld world){
   var leader=world.Pawns[Leader];
   if(leader.State.down<=0){Capturer=-1;Capture=0;return;}
   var cuffer=FindCuffer(world,leader);
   if(cuffer){
    // Mientras lo esposan, el líder no se recupera.
    Capturer=cuffer.Id;
    Capture=Mathf.Min(1,Capture+dt/LanNightRules.CuffSeconds);
    if(Capture>=1)Outcome=LanOutcome.Captured;
    return;
   }
   Capturer=-1;Capture=0;
   leader.State.down=Mathf.Max(0,leader.State.down-dt);
   if(leader.State.down<=0)leader.Recover();
  }
  LanPawn FindCuffer(LanWorld world,LanPawn leader){
   bool Valid(LanPawn p)=>p.Team==LanNightRules.Dea&&p.State.hp>0&&p.Holding&&Vector3.Distance(p.transform.position,leader.transform.position)<=LanNightRules.CuffRange;
   if(Capturer>=0&&Valid(world.Pawns[Capturer]))return world.Pawns[Capturer];
   return world.Pawns.Values.FirstOrDefault(Valid);
  }
 }
}
