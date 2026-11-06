using System.Linq;
using UnityEngine;
using UnityEngine.AI;
namespace Valle.Lan {
 // Decide qué hace un bot según su rol: líder, guardia o agente de la DEA.
 public sealed class LanBotBrain {
  const float StrafeRange=7,RememberSeconds=6,SearchSeconds=45,FleeSeconds=6;
  readonly LanPawn pawn;readonly LanWorld world;readonly LanSession session;
  Vector3[] corners;int corner;float nextPath,nextChoice,rememberUntil;Vector3 remembered,destination;bool hasDestination;
  float Now=>session.Night.Clock;
  public LanBotBrain(LanPawn owner,LanWorld map,LanSession host){pawn=owner;world=map;session=host;}
  public void Reset(){corners=null;nextPath=0;hasDestination=false;rememberUntil=0;nextChoice=0;}
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
   var direction=Steer(goal);
   if(enemy&&enemyDistance<StrafeRange&&!pawn.IsLeader&&!input.hold)direction=Strafe(enemy,position);
   if(!enemy&&direction.sqrMagnitude>.01f){input.ax=direction.x;input.az=direction.z;}
   input.x=direction.x;input.z=direction.z;
   return input;
  }
  Vector3 LeaderGoal(LanPawn enemy){
   if(!hasDestination||(enemy&&Now>=nextChoice)){destination=ChooseHideout(enemy);hasDestination=true;nextChoice=Now+FleeSeconds;}
   return destination;
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
   if(Now<rememberUntil)return remembered;
   bool arrived=hasDestination&&Vector3.Distance(pawn.transform.position,destination)<2.5f;
   if(!hasDestination||arrived||Now>=nextChoice){destination=ChooseZone();hasDestination=true;nextChoice=Now+SearchSeconds;}
   return destination;
  }
  Vector3 ChooseZone(){
   var places=world.zones;
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
