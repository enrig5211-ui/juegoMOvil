using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
namespace Valle.Lan {
 // Aliados NPC de la organización. El anfitrión los mueve y decide; los clientes solo dibujan lo que reciben.
 // - Lanceros del líder: 5 defensores frágiles que lo rodean y atacan cuerpo a cuerpo a la DEA cercana.
 // - Comuneros del Reclutador: 3 aliados que lo siguen y pelean igual; cada uno puede salir kamikaze (30 %):
 //   corre hacia el agente más cercano y estalla. Si lo matan antes, estalla donde cae.
 // - Soldados de la DEA (helicóptero) y pistoleros de la organización (chutos, con 2 granadas): disparan a distancia.
 // - Drones del enjambre: vuelan buscando rivales y se estrellan contra ellos.
 public sealed partial class LanWorld {
  public const int Lancer=0,Villager=1,Kamikaze=2,Soldier=3,Gunman=4,SwarmDrone=5;
  public const int SpearCount=6,RecruitCount=6,EscortCount=3;
  public const float SpearHealth=35,SpearSeconds=25,SpearDamage=12,SpearReach=1.3f,SpearSpeed=3.6f,SpearGuard=7,SpearAttackDelay=.9f,SpearSight=6;
  public const float RecruitHealth=45,RecruitSeconds=40,RecruitDamage=10,KamikazeChance=.3f,KamikazeSpeed=4.6f,KamikazeSight=10,KamikazeRadius=4.5f,KamikazeDamage=120;
  public const float SoldierHealth=70,GunmanHealth=60,ArmedSeconds=45,NpcSight=8,NpcSightExposed=16,NpcRange=18,NpcDamage=10,NpcFireEvery=.5f;
  public const float DroneSpeed=6,DroneDive=9,DroneSense=7,DroneBlast=2.6f,DroneDamage=50,DroneLife=60;
  public readonly List<LanNpc> Npcs=new List<LanNpc>();
  readonly Dictionary<int,LanSpearman> spearmen=new Dictionary<int,LanSpearman>();int npcId;
  readonly Dictionary<int,(Vector3 at,float until)> swarmTargets=new Dictionary<int,(Vector3,float)>();
  public bool SwarmCalled(int owner)=>swarmTargets.TryGetValue(owner,out var call)&&session.Night.Clock<call.until;
  // Lanceros del líder y, además, hasta 3 escoltas armados que lo siguen.
  public int SummonSpearmen(LanPawn leader){
   int count=Summon(leader,SpearCount,SpearHealth,SpearSeconds,_=>Lancer);if(count==0)return 0;
   int escorts=EscortCount-Npcs.Count(n=>n.owner==leader.Id&&n.kind==Gunman);
   for(int i=0;i<escorts;i++){var npc=SpawnArmed(Gunman,leader,leader.transform.position+Quaternion.Euler(0,i*120+60,0)*Vector3.forward*1.4f);npc.home=leader.transform.position;}
   return count;
  }
  public int Recruit(LanPawn pawn)=>Summon(pawn,RecruitCount,RecruitHealth,RecruitSeconds,_=>UnityEngine.Random.value<KamikazeChance?Kamikaze:Villager);
  // Uno por invocador: mientras le queden aliados, no puede llamar más.
  int Summon(LanPawn owner,int count,float health,float seconds,Func<int,int> kind){
   if(Npcs.Any(n=>n.owner==owner.Id&&n.kind<=Kamikaze))return 0;
   for(int i=0;i<count;i++){
    var at=owner.transform.position+Quaternion.Euler(0,i*360f/count,0)*Vector3.forward*1.8f;
    if(NavMesh.SamplePosition(at,out var hit,2,NavMesh.AllAreas))at=hit.position;
    Npcs.Add(new LanNpc{id=++npcId,kind=kind(i),owner=owner.Id,team=owner.Team,position=at+Vector3.up*.05f,home=at,aim=owner.State.aim,hp=health,time=seconds});
   }
   return count;
  }
  // Soldados, pistoleros y drones: los traen el helicóptero, los chutos o la base de drones.
  public LanNpc SpawnArmed(int kind,LanPawn owner,Vector3 at){
   if(NavMesh.SamplePosition(at,out var hit,2.5f,NavMesh.AllAreas)&&kind!=SwarmDrone)at=hit.position;
   var npc=new LanNpc{id=++npcId,kind=kind,owner=owner.Id,team=owner.Team,position=at+Vector3.up*.05f,home=at,aim=owner.State.aim,
    hp=kind==Soldier?SoldierHealth:kind==Gunman?GunmanHealth:1,time=kind==SwarmDrone?DroneLife:ArmedSeconds,grenades=kind==Gunman?2:0};
   Npcs.Add(npc);return npc;
  }
  public bool HitSpearman(Collider collider,LanPawn shooter,float damage){
   var body=collider.GetComponent<LanSpearman>();if(!body)return false;
   var npc=Npcs.Find(n=>n.id==body.Id);if(npc!=null&&npc.team!=shooter.Team)npc.hp-=damage;
   return true;
  }
  public int Detonations{get;private set;}
  void Detonate(LanNpc npc){Detonations++;Explode(npc.position,LanNightRules.Org,npc.owner,KamikazeRadius,KamikazeDamage,2,1.6f,5);}
  void TickSpearmen(float dt){
   var leader=LeaderPawn;
   for(int i=Npcs.Count-1;i>=0;i--){
    if(i>=Npcs.Count)continue;
    var npc=Npcs[i];npc.time-=dt;
    Pawns.TryGetValue(npc.owner,out var owner);
    if(!owner||owner.State.hp<=0&&!(owner.IsLeader&&owner.State.down>0))owner=leader;
    if(npc.kind==SwarmDrone){TickDrone(npc,dt);continue;}
    if(npc.kind==Soldier||npc.kind==Gunman){if(npc.time<=0||npc.hp<=0)Npcs.Remove(npc);else TickArmed(npc,dt);continue;}
    bool kamikaze=npc.kind==Kamikaze;
    if(npc.hp<=0&&kamikaze){Npcs.Remove(npc);Detonate(npc);continue;}
    if(npc.time<=0||npc.hp<=0||!owner){Npcs.Remove(npc);continue;}
    var body=Spearman(npc);var position=body.transform.position;
    // Blanco: un agente cerca del protegido (o, el kamikaze, cualquiera a la vista).
    LanPawn target=null;float best=kamikaze?KamikazeSight:SpearSight;
    foreach(var p in Pawns.Values){
     if(p.Team!=LanNightRules.Dea||p.State.hp<=0)continue;
     if(p.State.ride>0&&!kamikaze)continue;
     float toNpc=Flat(p.transform.position-position);
     if(toNpc<best&&(kamikaze||Flat(p.transform.position-owner.transform.position)<=SpearGuard)){best=toNpc;target=p;}
    }
    var mates=Npcs.Where(n=>n.owner==npc.owner).ToList();int index=mates.IndexOf(npc);
    var goal=target?target.transform.position:owner.transform.position+Quaternion.Euler(0,index*360f/Mathf.Max(1,mates.Count)+Time.time*20,0)*Vector3.forward*1.8f;
    var move=goal-position;move.y=0;float distance=move.magnitude;
    body.Cooldown-=dt;
    if(target&&distance<=SpearReach){
     if(kamikaze){Npcs.Remove(npc);Detonate(npc);continue;}
     if(body.Cooldown<=0){
      body.Cooldown=SpearAttackDelay;npc.attack++;
      float hit=npc.kind==Villager?RecruitDamage:SpearDamage;
      target.State.hp=Mathf.Max(0,target.State.hp-hit*100/(100+target.Class.Defense));session.Night.Interrupt(target.Id);
      if(target.State.hp<=0){CountDown(target,"lanza");target.Fall();session.ScoreKill(npc.owner>=0?npc.owner:leader.Id,target.Id);}
     }
     move=Vector3.zero;
    }else if(distance<.3f)move=Vector3.zero;
    float speed=kamikaze&&target?KamikazeSpeed:SpearSpeed;
    var step=move.sqrMagnitude>.01f?move.normalized*speed:Vector3.zero;
    body.Body.Move((step+Vector3.down*2)*dt);
    if(move.sqrMagnitude>.01f)npc.aim=move.normalized;
    npc.position=body.transform.position;if(kamikaze)npc.attack=target?1:0;
   }
  }
  // Soldados y pistoleros: acompañan al aliado más cercano (o a su dueño), atacan a los rivales que ven ellos o su
  // equipo, se acercan a distancia de tiro por la navegación, se apartan de granadas y fuego, y los pistoleros
  // lanzan sus dos granadas a media distancia.
  void TickArmed(LanNpc npc,float dt){
   var body=Spearman(npc);var position=body.transform.position;var eye=position+Vector3.up*.35f;
   LanPawn anchor=null;float near=30;
   if(Pawns.TryGetValue(npc.owner,out var owner)&&owner.State.hp>0&&Flat(owner.transform.position-position)<near)anchor=owner;
   else foreach(var p in Pawns.Values){if(p.Team!=npc.team||p.State.hp<=0)continue;float d=Flat(p.transform.position-position);if(d<near){near=d;anchor=p;}}
   LanPawn target=null;float best=float.MaxValue;
   foreach(var p in Pawns.Values){
    if(p.Team==npc.team||p.State.hp<=0)continue;
    float d=Flat(p.transform.position-position);if(d>NpcRange+4||d>=best)continue;
    bool exposed=p.State.lamp||p.FiredRecently||p.State.marked>0||p.State.ride>0||IsLit(p.transform.position);
    bool spotted=d<=(exposed?NpcSightExposed:NpcSight)||Pawns.Values.Any(a=>a.Team==npc.team&&a.State.hp>0&&Flat(a.transform.position-p.transform.position)<=LanNightRules.NearVision+1);
    if(spotted){best=d;target=p;}
   }
   bool clear=target&&LanVision.Clear(eye,target.transform.position+Vector3.up*.35f);
   // A dónde ir: huir del peligro, buscar ángulo de tiro, o acompañar al aliado.
   Vector3 goal;
   var danger=Marks.FirstOrDefault(m=>m.team!=npc.team&&(m.kind==LanItems.GrenadeMark&&Flat(m.position-position)<LanExplosives.GrenadeRadius+.8f||m.kind==LanItems.FireMark&&Flat(m.position-position)<m.size+.5f||m.kind==LanItems.StrikeMark&&Flat(m.position-position)<m.size+2));
   if(danger!=null){var away=position-danger.position;away.y=0;goal=position+(away.sqrMagnitude>.01f?away.normalized:Vector3.forward)*5;}
   else if(target&&(!clear||best>NpcRange*.7f))goal=target.transform.position;
   else if(target)goal=position+Quaternion.Euler(0,90,0)*(target.transform.position-position).normalized*Mathf.Sin(Time.time*.8f+npc.id)*1.5f;
   else if(anchor)goal=anchor.transform.position+Quaternion.Euler(0,npc.id*137,0)*Vector3.forward*2.4f;
   else goal=npc.home;
   var step=Route(body,position,goal,dt);
   body.Body.Move((step*SpearSpeed+Vector3.down*2)*dt);npc.position=body.transform.position;
   body.Cooldown-=dt;
   if(!target){if(step.sqrMagnitude>.01f)npc.aim=step.normalized;return;}
   var aim=target.transform.position-position;aim.y=0;npc.aim=aim.normalized;
   if(!owner)return;
   if(npc.grenades>0&&best>5&&best<11&&UnityEngine.Random.value<dt*.5f){
    npc.grenades--;var g=AddMark(LanItems.GrenadeMark,npc.team,target.transform.position+Vector3.up*.08f,LanExplosives.GrenadeFuse);g.from=position+Vector3.up*.6f;g.owner=npc.owner;g.size=LanExplosives.GrenadeRadius;
   }
   if(!clear||body.Cooldown>0||best>NpcRange)return;
   body.Cooldown=NpcFireEvery;var spread=Quaternion.Euler(0,UnityEngine.Random.Range(-4f,4f),0)*npc.aim;
   Shoot(owner,eye,spread,NpcRange,NpcDamage,body.gameObject,out var end,out _);npc.hit=end;npc.attack++;
  }
  // Ruta por la navegación hacia el objetivo, recalculada cada 0,6 s.
  Vector3 Route(LanSpearman body,Vector3 position,Vector3 goal,float dt){
   body.Repath-=dt;
   if(body.Repath<=0){
    body.Repath=.6f;var path=new NavMeshPath();
    body.Path=NavMesh.SamplePosition(position,out var a,1.5f,NavMesh.AllAreas)&&NavMesh.SamplePosition(goal,out var b,2,NavMesh.AllAreas)&&NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,path)&&path.status!=NavMeshPathStatus.PathInvalid?path.corners:new[]{position,goal};
    body.Corner=body.Path.Length>1?1:0;
   }
   while(body.Corner<body.Path.Length-1&&Flat(body.Path[body.Corner]-position)<.4f)body.Corner++;
   var to=body.Path[Mathf.Min(body.Corner,body.Path.Length-1)]-position;to.y=0;
   return to.magnitude<.3f?Vector3.zero:to.normalized;
  }
  // Dron del enjambre: recorre el mapa; al encontrar un rival baja en picada, estalla y lo deja marcado.
  void TickDrone(LanNpc npc,float dt){
   if(npc.time<=0||npc.hp<=0){Npcs.Remove(npc);return;}
   LanPawn target=null;float best=DroneSense;
   foreach(var p in Pawns.Values){if(p.Team==npc.team||p.State.hp<=0)continue;float d=Flat(p.transform.position-npc.position);if(d<best){best=d;target=p;}}
   Vector3 goal;float speed;
   // Cuando un dron encuentra a un rival, los demás del enjambre van a esa ubicación.
   if(target)swarmTargets[npc.owner]=(target.transform.position,session.Night.Clock+6);
   if(target){goal=target.transform.position;speed=DroneDive;npc.attack=1;}
   else if(swarmTargets.TryGetValue(npc.owner,out var call)&&session.Night.Clock<call.until&&Flat(call.at-npc.position)>1){goal=call.at;speed=DroneSpeed*1.4f;npc.attack=0;}
   else{
    npc.attack=0;
    if(Flat(npc.home-npc.position)<1.5f){var places=(zones??new Transform[0]).Concat(hideouts??new Transform[0]).Concat(itemSpots??new Transform[0]).ToArray();var pick=places.Length>0?places[UnityEngine.Random.Range(0,places.Length)].position:npc.position;var r=UnityEngine.Random.insideUnitCircle*4;npc.home=pick+new Vector3(r.x,0,r.y);}
    goal=npc.home;speed=DroneSpeed;
   }
   var move=goal-npc.position;move.y=0;
   if(target&&move.magnitude<1){
    Npcs.Remove(npc);Detonations++;
    foreach(var p in Pawns.Values)if(p.Team!=npc.team&&p.State.hp>0&&Flat(p.transform.position-npc.position)<=DroneBlast+3)p.State.marked=LanAbilities.MarkSeconds;
    Explode(new Vector3(npc.position.x,.05f,npc.position.z),npc.team,npc.owner,DroneBlast,DroneDamage,1,0,0);return;
   }
   if(move.sqrMagnitude>.01f){npc.aim=move.normalized;npc.position+=move.normalized*Mathf.Min(speed*dt,move.magnitude);}
  }
  LanSpearman Spearman(LanNpc npc){
   if(spearmen.TryGetValue(npc.id,out var view)&&view)return view;
   var go=new GameObject((npc.kind==Lancer?"Lancero ":npc.kind==Kamikaze?"Kamikaze ":"Comunero ")+npc.id);go.transform.SetParent(transform);go.transform.position=npc.position;
   view=go.AddComponent<LanSpearman>();view.Id=npc.id;view.Kind=npc.kind;
   bool flying=npc.kind==SwarmDrone;
   if(!flying){view.Body=go.AddComponent<CharacterController>();view.Body.height=.65f;view.Body.radius=.2f;view.Body.center=new Vector3(0,.325f,0);view.Body.enabled=session.IsHost;}
   var model=flying?(assets.drone?assets.drone:null):npc.kind==Lancer?assets.Spearman:npc.kind==Soldier?assets.Character(LanNightRules.Dea,false,0):npc.kind==Gunman?assets.Character(LanNightRules.Org,false,0):assets.Character(LanNightRules.Org,false,3);
   view.Model=model?Instantiate(model,go.transform).transform:LanModels.Drone(go.transform);if(flying)view.Model.localScale=Vector3.one*.6f;view.Scale=view.Model.localScale;
   if(npc.kind==Soldier||npc.kind==Gunman){var gun=LanPawn.MakeWeapon(2,go.transform,assets);gun.localPosition=new Vector3(.2f,.34f,.04f);
    view.Tracer=new GameObject("Trazo").AddComponent<LineRenderer>();view.Tracer.transform.SetParent(go.transform,false);view.Tracer.sharedMaterial=assets.glow;view.Tracer.positionCount=2;view.Tracer.widthMultiplier=.02f;view.Tracer.startColor=new Color(1,.85f,.5f);view.Tracer.endColor=new Color(1,.6f,.3f,.3f);view.Tracer.enabled=false;
    view.Voice=go.AddComponent<AudioSource>();view.Voice.spatialBlend=1;view.Voice.minDistance=2;view.Voice.maxDistance=26;view.Voice.rolloffMode=AudioRolloffMode.Linear;}
   if(flying){view.Fuse=new GameObject("Luz").AddComponent<Light>();view.Fuse.transform.SetParent(go.transform,false);view.Fuse.transform.localPosition=Vector3.up*2.3f;view.Fuse.type=LightType.Point;view.Fuse.range=2.5f;view.Fuse.intensity=2;view.Fuse.color=new Color(1,.3f,.2f);view.Fuse.shadows=LightShadows.None;}
   if(npc.kind==Kamikaze){var bomb=assets.dynamite?Instantiate(assets.dynamite,view.Model).transform:LanModels.Dynamite(view.Model);bomb.localPosition=Vector3.zero;bomb.localRotation=Quaternion.identity;bomb.localScale=new Vector3(1/Mathf.Max(.01f,view.Scale.x),1/Mathf.Max(.01f,view.Scale.y),1/Mathf.Max(.01f,view.Scale.z));
    view.Fuse=new GameObject("Mecha").AddComponent<Light>();view.Fuse.transform.SetParent(view.Model,false);view.Fuse.transform.localPosition=Vector3.up*.35f/Mathf.Max(.01f,view.Scale.y);view.Fuse.type=LightType.Point;view.Fuse.range=1.6f;view.Fuse.intensity=2.5f;view.Fuse.color=new Color(1,.25f,.1f);view.Fuse.shadows=LightShadows.None;}
   spearmen[npc.id]=view;LanFx.Dust(npc.position,10);
   return view;
  }
  // Dibujo en cada pantalla: aparecen con polvo, saltan al moverse y embisten al atacar. El kamikaze parpadea y echa chispas.
  void UpdateSpearmen(){
   foreach(var id in spearmen.Keys.ToArray())if(!Npcs.Any(n=>n.id==id)){var gone=spearmen[id];if(gone){LanFx.Dust(gone.transform.position,10);Destroy(gone.gameObject);}spearmen.Remove(id);}
   int team=session.LocalSlot>=0?session.Slots[session.LocalSlot].team:LanNightRules.Org;
   foreach(var npc in Npcs){
    var view=Spearman(npc);
    if(!session.IsHost||npc.kind==SwarmDrone)view.transform.position=Vector3.Lerp(view.transform.position,npc.position,1-Mathf.Exp(-14*Time.deltaTime));
    bool seen=npc.team==team||IsLit(npc.position)||Pawns.Values.Any(p=>p.Team==team&&p.State.hp>0&&Flat(p.transform.position-npc.position)<=LanNightRules.NearVision+(npc.kind==SwarmDrone?4:1));
    view.Model.gameObject.SetActive(seen);
    if(npc.kind==SwarmDrone){view.Model.localPosition=Vector3.up*(2.2f+Mathf.Sin(Time.time*3+npc.id)*.12f-(npc.attack>0?1.2f:0));view.transform.rotation=Quaternion.LookRotation(npc.aim.sqrMagnitude>.01f?npc.aim:Vector3.forward);foreach(Transform rotor in view.Model)if(rotor.name.StartsWith("Rotor"))rotor.Rotate(0,2400*Time.deltaTime,0,Space.Self);if(view.Fuse)view.Fuse.enabled=seen&&Mathf.Repeat(Time.time*(npc.attack>0?8:2),1)<.5f;continue;}
    if(view.Tracer){if(npc.attack!=view.Attacks){view.Attacks=npc.attack;var muzzle=view.transform.position+Vector3.up*.35f+npc.aim*.45f;view.Tracer.SetPosition(0,muzzle);view.Tracer.SetPosition(1,npc.hit);view.Shot=Time.time+.06f;if(seen)LanFx.Muzzle(muzzle);var clips=assets.rifleShots;if(clips!=null&&clips.Length>0)view.Voice.PlayOneShot(clips[UnityEngine.Random.Range(0,clips.Length)],.3f);}view.Tracer.enabled=Time.time<view.Shot;}
    float speed=Flat(view.transform.position-view.Last)/Mathf.Max(.001f,Time.deltaTime);view.Last=view.transform.position;
    view.Speed=Mathf.Lerp(view.Speed,speed,1-Mathf.Exp(-8*Time.deltaTime));
    bool charging=npc.kind==Kamikaze&&npc.attack>0;
    if(npc.kind!=Kamikaze&&!view.Tracer&&npc.attack!=view.Attacks){view.Attacks=npc.attack;view.Lunge=1;}
    view.Lunge=Mathf.MoveTowards(view.Lunge,0,Time.deltaTime/.25f);
    if(view.Speed>.5f)view.Phase+=Time.deltaTime*(charging?16:10);
    float hop=view.Speed>.5f?Mathf.Abs(Mathf.Sin(view.Phase)):0;
    view.Model.localPosition=Vector3.up*hop*.14f+Vector3.forward*view.Lunge*.25f;
    view.Model.localScale=Vector3.Scale(view.Scale,new Vector3(1-hop*.06f,1+hop*.1f,1-hop*.06f));
    view.transform.rotation=Quaternion.LookRotation(npc.aim.sqrMagnitude>.01f?npc.aim:Vector3.forward)*Quaternion.Euler(view.Lunge*-15+(charging?12:0),0,0);
    if(view.Fuse){view.Fuse.enabled=seen&&Mathf.Repeat(Time.time*(charging?8:2),1)<.5f;if(seen&&charging&&Time.time>=view.NextSpark){view.NextSpark=Time.time+.08f;LanFx.Impact(view.transform.position+Vector3.up*.4f);}}
   }
  }
 }
 public sealed class LanSpearman:MonoBehaviour{public int Id,Kind,Attacks;public CharacterController Body;public Transform Model;public Light Fuse;public LineRenderer Tracer;public AudioSource Voice;public Vector3 Scale,Last;public Vector3[] Path;public int Corner;public float Cooldown,Speed,Phase,Lunge,NextSpark,Shot,Repath;}
}
