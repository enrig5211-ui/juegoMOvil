using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
namespace Valle.Lan {
 // Lanceros del líder: 5 defensores frágiles que lo rodean y atacan cuerpo a cuerpo a la DEA cercana.
 // El anfitrión los mueve y decide; los clientes solo dibujan lo que reciben.
 public sealed partial class LanWorld {
  public const int SpearCount=5;
  public const float SpearHealth=35,SpearSeconds=25,SpearDamage=12,SpearReach=1.3f,SpearSpeed=3.6f,SpearGuard=7,SpearAttackDelay=.9f,SpearSight=6;
  public readonly List<LanNpc> Npcs=new List<LanNpc>();
  readonly Dictionary<int,LanSpearman> spearmen=new Dictionary<int,LanSpearman>();int npcId;
  public int SummonSpearmen(LanPawn leader){
   if(Npcs.Count>0)return 0;
   for(int i=0;i<SpearCount;i++){
    var at=leader.transform.position+Quaternion.Euler(0,i*360f/SpearCount,0)*Vector3.forward*1.8f;
    if(NavMesh.SamplePosition(at,out var hit,2,NavMesh.AllAreas))at=hit.position;
    Npcs.Add(new LanNpc{id=++npcId,position=at+Vector3.up*.05f,aim=leader.State.aim,hp=SpearHealth,time=SpearSeconds});
   }
   return SpearCount;
  }
  public bool HitSpearman(Collider collider,LanPawn shooter,float damage){
   var body=collider.GetComponent<LanSpearman>();if(!body)return false;
   if(shooter.Team==LanNightRules.Dea){var npc=Npcs.Find(n=>n.id==body.Id);if(npc!=null)npc.hp-=damage;}
   return true;
  }
  void TickSpearmen(float dt){
   var leader=LeaderPawn;
   for(int i=Npcs.Count-1;i>=0;i--){
    var npc=Npcs[i];npc.time-=dt;
    if(npc.time<=0||npc.hp<=0||!leader){Npcs.RemoveAt(i);continue;}
    var body=Spearman(npc);var position=body.transform.position;
    LanPawn target=null;float best=SpearSight;
    foreach(var p in Pawns.Values){
     if(p.Team!=LanNightRules.Dea||p.State.hp<=0)continue;
     float toNpc=Flat(p.transform.position-position);
     if(toNpc<best&&Flat(p.transform.position-leader.transform.position)<=SpearGuard){best=toNpc;target=p;}
    }
    int index=Npcs.IndexOf(npc);
    var goal=target?target.transform.position:leader.transform.position+Quaternion.Euler(0,index*360f/SpearCount+Time.time*20,0)*Vector3.forward*1.8f;
    var move=goal-position;move.y=0;float distance=move.magnitude;
    body.Cooldown-=dt;
    if(target&&distance<=SpearReach){
     if(body.Cooldown<=0){
      body.Cooldown=SpearAttackDelay;npc.attack++;
      target.State.hp=Mathf.Max(0,target.State.hp-SpearDamage*100/(100+target.Class.Defense));session.Night.Interrupt(target.Id);
      if(target.State.hp<=0){target.Fall();session.ScoreKill(leader.Id,target.Id);}
     }
     move=Vector3.zero;
    }else if(distance<.3f)move=Vector3.zero;
    var step=move.sqrMagnitude>.01f?move.normalized*SpearSpeed:Vector3.zero;
    body.Body.Move((step+Vector3.down*2)*dt);
    if(move.sqrMagnitude>.01f)npc.aim=move.normalized;
    npc.position=body.transform.position;
   }
  }
  LanSpearman Spearman(LanNpc npc){
   if(spearmen.TryGetValue(npc.id,out var view)&&view)return view;
   var go=new GameObject("Lancero "+npc.id);go.transform.SetParent(transform);go.transform.position=npc.position;
   view=go.AddComponent<LanSpearman>();view.Id=npc.id;
   view.Body=go.AddComponent<CharacterController>();view.Body.height=.65f;view.Body.radius=.2f;view.Body.center=new Vector3(0,.325f,0);view.Body.enabled=session.IsHost;
   view.Model=Instantiate(assets.Spearman,go.transform).transform;view.Scale=view.Model.localScale;
   spearmen[npc.id]=view;LanFx.Dust(npc.position,10);
   return view;
  }
  // Dibujo en cada pantalla: aparecen con polvo, saltan al moverse y embisten al atacar.
  void UpdateSpearmen(){
   foreach(var id in spearmen.Keys.ToArray())if(!Npcs.Any(n=>n.id==id)){var gone=spearmen[id];if(gone){LanFx.Dust(gone.transform.position,10);Destroy(gone.gameObject);}spearmen.Remove(id);}
   int team=session.LocalSlot>=0?session.Slots[session.LocalSlot].team:LanNightRules.Org;
   foreach(var npc in Npcs){
    var view=Spearman(npc);
    if(!session.IsHost)view.transform.position=Vector3.Lerp(view.transform.position,npc.position,1-Mathf.Exp(-14*Time.deltaTime));
    bool seen=team==LanNightRules.Org||IsLit(npc.position)||Pawns.Values.Any(p=>p.Team==team&&p.State.hp>0&&Flat(p.transform.position-npc.position)<=LanNightRules.NearVision+1);
    view.Model.gameObject.SetActive(seen);
    float speed=Flat(view.transform.position-view.Last)/Mathf.Max(.001f,Time.deltaTime);view.Last=view.transform.position;
    view.Speed=Mathf.Lerp(view.Speed,speed,1-Mathf.Exp(-8*Time.deltaTime));
    if(npc.attack!=view.Attacks){view.Attacks=npc.attack;view.Lunge=1;}
    view.Lunge=Mathf.MoveTowards(view.Lunge,0,Time.deltaTime/.25f);
    if(view.Speed>.5f)view.Phase+=Time.deltaTime*10;
    float hop=view.Speed>.5f?Mathf.Abs(Mathf.Sin(view.Phase)):0;
    view.Model.localPosition=Vector3.up*hop*.14f+Vector3.forward*view.Lunge*.25f;
    view.Model.localScale=Vector3.Scale(view.Scale,new Vector3(1-hop*.06f,1+hop*.1f,1-hop*.06f));
    view.transform.rotation=Quaternion.LookRotation(npc.aim.sqrMagnitude>.01f?npc.aim:Vector3.forward)*Quaternion.Euler(view.Lunge*-15,0,0);
   }
  }
 }
 public sealed class LanSpearman:MonoBehaviour{public int Id,Attacks;public CharacterController Body;public Transform Model;public Vector3 Scale,Last;public float Cooldown,Speed,Phase,Lunge;}
}
