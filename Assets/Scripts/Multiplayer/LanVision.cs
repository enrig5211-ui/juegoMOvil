using UnityEngine;
namespace Valle.Lan {
 // Qué ve cada combatiente de noche. Lo usan el anfitrión (bots) y cada cliente (qué dibujar).
 public static class LanVision {
  static readonly RaycastHit[] hits=new RaycastHit[24];
  // 0 = noche cerrada, 1 = día. El despliegue es el atardecer; amanece en los últimos minutos.
  public static float Daylight(LanSession session){
   var night=session.Night;
   if(night.Outcome==LanOutcome.Dawn)return 1;
   if(night.Deploying)return LanNightRules.DuskLight*night.Deploy/LanNightRules.DeploySeconds;
   return Mathf.Clamp01(1-session.Remaining/LanNightRules.DawnSeconds);
  }
  // Línea de visión: la bloquean selva, edificios y coberturas; no los combatientes ni el agua.
  public static bool Clear(Vector3 from,Vector3 to){
   var delta=to-from;float distance=delta.magnitude;if(distance<.01f)return true;
   int count=Physics.RaycastNonAlloc(from,delta/distance,hits,distance,LanNightRules.SightMask,QueryTriggerInteraction.Ignore);
   for(int i=0;i<count;i++)if(!(hits[i].collider is CharacterController))return false;
   return true;
  }
  // ¿Pasa el segmento (en el plano) a menos de radius del centro?
  public static bool Through(Vector3 a,Vector3 b,Vector3 center,float radius){
   var ab=new Vector2(b.x-a.x,b.z-a.z);var ac=new Vector2(center.x-a.x,center.z-a.z);float t=ab.sqrMagnitude<1e-4f?0:Mathf.Clamp01(Vector2.Dot(ac,ab)/ab.sqrMagnitude);
   return (ac-ab*t).sqrMagnitude<=radius*radius;
  }
  public static bool Sees(LanWorld world,LanPawn viewer,LanPawn target){
   var from=viewer.transform.position;var to=target.transform.position;var flat=to-from;flat.y=0;float distance=flat.magnitude;
   // Emboscada: solo se ve desde muy cerca, aunque esté marcado o iluminado.
   if(target.State.marked>0&&target.Team!=viewer.Team)return true;
   // El humo tapa la vista salvo muy de cerca.
   if(distance>LanAbilities.AmbushReveal)foreach(var mark in world.Marks)if(mark.kind==LanItems.SmokeMark&&Through(from,to,mark.position,mark.size))return false;
   float near=Mathf.Lerp(LanNightRules.NearVision,LanNightRules.BotVision,Daylight(world.Session));
   if(viewer.State.vision>0)near=Mathf.Max(near,LanNightRules.NightVisionRange);
   bool inRange=distance<=near
    ||viewer.State.lamp&&distance<=LanNightRules.LampRange&&Vector3.Angle(viewer.State.aim,flat)<=LanNightRules.LampHalfAngle
    ||distance<=LanNightRules.ExposedRange&&(target.State.lamp||target.FiredRecently||target.State.ride>0||world.IsLit(to));
   return inRange&&Clear(from+Vector3.up*.35f,to+Vector3.up*.35f);
  }
 }
}
