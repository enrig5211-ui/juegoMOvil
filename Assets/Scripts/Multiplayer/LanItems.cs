using System.Linq;
using UnityEngine;
namespace Valle.Lan {
 // Items del suelo: se recogen con F (un espacio) y se usan con C.
 public static class LanItems {
  public const int None=0,Medkit=1,Ammo=2,NightVision=3,Flare=4,Alarm=5;
  public const int FlareMark=1,AlarmMark=2,TrapMark=3,BlastMark=4;
  static readonly string[] names={"","Botiquín","Munición","Visor nocturno","Bengala","Alarma"};
  static readonly int[] weights={0,30,25,15,18,12};
  public static readonly Color[] Colors={Color.white,new Color(.95f,.3f,.3f),new Color(.95f,.8f,.3f),new Color(.35f,1,.45f),new Color(1,.55f,.2f),new Color(.6f,.6f,1)};
  public static string Name(int item)=>item>0&&item<names.Length?names[item]:"";
  public static int Pick(){int pick=Random.Range(0,weights.Sum());for(int i=1;i<weights.Length;i++){if(pick<weights[i])return i;pick-=weights[i];}return Medkit;}
  // Aplica el item en el anfitrión. Devuelve true si se gastó.
  public static bool Use(LanWorld world,LanPawn pawn){
   var state=pawn.State;
   switch(state.item){
    case Medkit:
     if(state.hp>=pawn.Class.Health)return false;
     state.hp=Mathf.Min(pawn.Class.Health,state.hp+LanNightRules.MedkitHeal);return true;
    case Ammo:
     if(state.weapon==0)return false;
     state.reserve=Mathf.Min(state.reserve+pawn.Class.Reserve/2+pawn.Capacity,pawn.Class.Reserve*2);return true;
    case NightVision:
     state.vision=LanNightRules.NightVisionSeconds;return true;
    case Flare:{
     var origin=pawn.transform.position+Vector3.up*.35f;float reach=LanNightRules.FlareThrow;
     if(Physics.Raycast(origin,state.aim,out var hit,reach,LanNightRules.SightMask,QueryTriggerInteraction.Ignore))reach=Mathf.Max(0,hit.distance-.5f);
     world.AddMark(FlareMark,pawn.Team,pawn.transform.position+state.aim*reach,LanNightRules.FlareSeconds);return true;}
    case Alarm:
     world.AddMark(AlarmMark,pawn.Team,pawn.transform.position,0);return true;
   }
   return false;
  }
 }
}
