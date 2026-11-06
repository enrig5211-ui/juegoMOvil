using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
namespace Valle.Lan {
 public sealed class LanWorld:MonoBehaviour {
  public Transform[] blueSpawns,redSpawns;
  public readonly Dictionary<int,LanPawn> Pawns=new Dictionary<int,LanPawn>();
  public readonly List<LanDrop> Drops=new List<LanDrop>();
  readonly Dictionary<int,GameObject> dropViews=new Dictionary<int,GameObject>();
  LanSession session;LanAssets assets;int dropId;
  public void Initialize(LanSession owner){session=owner;assets=Resources.Load<LanAssets>("Multiplayer/LanAssets");
   for(int i=0;i<8;i++){var go=new GameObject("Combatiente "+i);var pawn=go.AddComponent<LanPawn>();Pawns.Add(i,pawn);pawn.Initialize(this,owner,i,assets);}
   if(session.IsHost){for(int i=0;i<8;i++)Respawn(i);}
   var camera=Camera.main.GetComponent<Valle.PlayerCamera>();if(!camera)camera=Camera.main.gameObject.AddComponent<Valle.PlayerCamera>();camera.target=Pawns[session.LocalSlot].transform;camera.pitch=45;camera.distance=10;Camera.main.orthographic=true;Camera.main.orthographicSize=5.5f;
  }
  public Vector3 Spawn(int id){var points=id<4?blueSpawns:redSpawns;return points[id%4].position;}
  public void Respawn(int id){var p=Pawns[id];p.ResetLife(Spawn(id));}
  public void Tick(float dt){foreach(var p in Pawns.Values)p.ServerTick(dt);Drops.RemoveAll(d=>d==null);UpdateDrops();}
  public void Apply(LanPacket packet){if(packet.actors!=null)foreach(var state in packet.actors)if(Pawns.TryGetValue(state.id,out var p))p.Apply(state);Drops.Clear();if(packet.drops!=null)Drops.AddRange(packet.drops);UpdateDrops();}
  public LanActorState[] States()=>Pawns.Values.Select(p=>p.State).ToArray();
  public LanPawn Enemy(LanPawn self){LanPawn best=null;float distance=float.MaxValue;foreach(var p in Pawns.Values){if(p==self||p.Team==self.Team||p.State.hp<=0)continue;float d=(p.transform.position-self.transform.position).sqrMagnitude;bool clear=CanSee(self,p);float cost=d+(clear?0:1000);if(cost<distance){distance=cost;best=p;}}return best;}
  public bool CanSee(LanPawn from,LanPawn to){var a=from.transform.position+Vector3.up*.35f;var b=to.transform.position+Vector3.up*.35f;var hits=Physics.RaycastAll(a,(b-a).normalized,(b-a).magnitude,~0,QueryTriggerInteraction.Ignore);return !hits.Any(h=>h.collider.gameObject!=from.gameObject&&h.collider.gameObject!=to.gameObject);}
  public void Fire(LanPawn pawn,Vector3 aim){var state=pawn.State;var origin=pawn.transform.position+Vector3.up*.35f;var end=origin+aim*24;float distance=24;LanPawn victim=null;
   foreach(var hit in Physics.RaycastAll(origin,aim,24,~0,QueryTriggerInteraction.Ignore)){if(hit.collider.gameObject==pawn.gameObject)continue;if(hit.distance<distance){distance=hit.distance;end=hit.point;victim=hit.collider.GetComponent<LanPawn>();}}
   state.shot++;state.hit=end;state.impact=distance<24;
   if(victim&&victim.Team!=pawn.Team&&victim.State.hp>0&&victim.State.shield<=0){victim.State.hp=Mathf.Max(0,victim.State.hp-(state.weapon==1?24:16)*100/(100+victim.Class.Defense));if(victim.State.hp<=0){Drop(victim);victim.State.respawn=4;session.ScoreKill(pawn.Id,victim.Id);}}
  }
  public void Drop(LanPawn p){if(p.State.weapon==0)return;Vector3 pos=p.transform.position;if(NavMesh.SamplePosition(pos+p.transform.right*.7f,out var nav,2,NavMesh.AllAreas))pos=nav.position;
   if(Drops.Count>=32)Drops.RemoveAt(0);Drops.Add(new LanDrop{id=++dropId,weapon=p.State.weapon,ammo=p.State.ammo,reserve=p.State.reserve,position=pos+Vector3.up*.025f});p.State.weapon=0;p.State.ammo=p.State.reserve=0;p.State.reload=0;
  }
  public LanDrop Nearest(LanPawn p){return Drops.Where(d=>Vector3.Distance(d.position,p.State.position)<1.6f&&!Physics.Linecast(p.State.position+Vector3.up*.4f,d.position+Vector3.up*.4f,~0,QueryTriggerInteraction.Ignore)).OrderBy(d=>(d.position-p.State.position).sqrMagnitude).FirstOrDefault();}
  public void Pickup(LanPawn p){var drop=Nearest(p);if(drop==null)return;Drops.Remove(drop);Drop(p);p.State.weapon=drop.weapon;p.State.ammo=drop.ammo;p.State.reserve=drop.reserve;p.State.reload=0;}
  void UpdateDrops(){foreach(var key in dropViews.Keys.ToArray())if(!Drops.Any(d=>d.id==key)){Destroy(dropViews[key]);dropViews.Remove(key);}foreach(var d in Drops){if(!dropViews.TryGetValue(d.id,out var go)){go=new GameObject("Arma flotante "+d.id);go.transform.SetParent(transform);var model=LanPawn.MakeWeapon(d.weapon,go.transform,assets);model.localPosition=Vector3.up*.55f;LanPawn.Ring(go.transform,assets.glow,new Color(.35f,.95f,1),.36f);dropViews.Add(d.id,go);}go.transform.position=d.position;var t=go.transform.GetChild(0);t.localPosition=Vector3.up*(.55f+Mathf.Sin(Time.time*2+d.id)*.08f);t.localRotation=Quaternion.Euler(0,Time.time*50,0);}}
  void Update(){if(session&&!session.IsHost)UpdateDrops();}
 }
 public sealed class LanPawn:MonoBehaviour {
  public LanActorState State=new LanActorState();public int Id{get;private set;}public int Team=>Id/4;public LanClass Class=>LanClass.Get(session.Slots[Id].role);
  LanSession session;LanWorld world;LanAssets assets;CharacterController controller;Transform visual,gun;GameObject ring;TextMesh label;AudioSource sound;LineRenderer tracer;
  Vector3 visualBase,lastPosition;Vector3[] corners;int corner;float vertical,nextPath,shotCooldown,regenDelay,stepPhase,flash,stepAt;int renderedWeapon=-1,renderedShot;bool renderedReload,wasAlive;MagicFx shieldFx;LanInput consumed=new LanInput();
  public void Initialize(LanWorld w,LanSession owner,int id,LanAssets art){Id=id;State.id=id;world=w;session=owner;assets=art;
   controller=gameObject.AddComponent<CharacterController>();controller.height=.65f;controller.radius=.2f;controller.center=new Vector3(0,.325f,0);controller.stepOffset=.18f;controller.skinWidth=.025f;controller.enabled=session.IsHost;
   var body=Instantiate(id<4?assets.bluePawn:assets.redPawn,transform);visual=body.transform;visualBase=visual.localPosition;
   ring=Ring(transform,id<4?assets.blueMaterial:assets.redMaterial,Color.white,.31f);var nameObject=new GameObject("Nombre");nameObject.transform.SetParent(transform,false);nameObject.transform.localPosition=Vector3.up*.95f;label=nameObject.AddComponent<TextMesh>();label.fontSize=40;label.characterSize=.045f;label.anchor=TextAnchor.MiddleCenter;label.color=id<4?new Color(.45f,.85f,1):new Color(1,.55f,.45f);
   sound=gameObject.AddComponent<AudioSource>();sound.playOnAwake=false;sound.spatialBlend=id==session.LocalSlot?0:1;sound.minDistance=2;sound.maxDistance=24;sound.rolloffMode=AudioRolloffMode.Linear;
   tracer=new GameObject("Trazo").AddComponent<LineRenderer>();tracer.transform.SetParent(transform,false);tracer.sharedMaterial=assets.glow;tracer.positionCount=2;tracer.startWidth=.025f;tracer.endWidth=.008f;tracer.enabled=false;
  }
  public void ResetLife(Vector3 position){State.hp=Class.Health;State.stamina=Class.Stamina;State.weapon=2;State.ammo=Class.Magazine;State.reserve=Class.Reserve;State.respawn=0;State.reload=0;State.shield=2;State.position=position;State.aim=Team==0?Vector3.right:Vector3.left;controller.enabled=false;transform.position=position;controller.enabled=session.IsHost;vertical=0;shotCooldown=.3f;corners=null;var previous=session.InputFor(Id);consumed=new LanInput{jump=previous.jump,reload=previous.reload,pickup=previous.pickup,drop=previous.drop};}
  public void Apply(LanActorState state){State=state;if((transform.position-state.position).sqrMagnitude>9)transform.position=state.position;}
  public void ServerTick(float dt){if(!session.Slots[Id].bot&&!session.Slots[Id].loaded){State.shield=2;return;}if(State.hp<=0){controller.enabled=false;State.respawn-=dt;if(State.respawn<=0)ResetLife(world.Spawn(Id));return;}controller.enabled=true;State.shield=Mathf.Max(0,State.shield-dt);
   var input=session.Slots[Id].bot?BotInput():session.InputFor(Id);if(input==null||!input.Valid())input=new LanInput();var movement=Vector3.ClampMagnitude(new Vector3(input.x,0,input.z),1);var aim=new Vector3(input.ax,0,input.az);if(aim.sqrMagnitude>.01f)State.aim=aim.normalized;
   bool run=input.sprint&&State.stamina>2&&movement.sqrMagnitude>.01f;float speed=Class.Speed*(run?1.5f:1);if(run){State.stamina=Mathf.Max(0,State.stamina-dt*18);regenDelay=.8f;}else if((regenDelay-=dt)<=0)State.stamina=Mathf.Min(Class.Stamina,State.stamina+dt*22);
   if(controller.isGrounded&&vertical<0)vertical=-2;if(input.jump!=consumed.jump&&controller.isGrounded&&State.stamina>=12){vertical=7;State.stamina-=12;regenDelay=.8f;}
   vertical-=22*dt;controller.Move((movement*speed+Vector3.up*vertical)*dt);State.grounded=controller.isGrounded;State.position=transform.position;
   if(transform.position.y < -5){world.Drop(this);State.hp=0;State.respawn=4;session.Slots[Id].deaths++;return;}
   if(State.reload>0){State.reload-=dt;if(State.reload<=0){int n=Mathf.Min(Capacity-State.ammo,State.reserve);State.ammo+=n;State.reserve-=n;}}
   if(input.reload!=consumed.reload&&State.weapon!=0&&State.reload<=0&&State.ammo<Capacity&&State.reserve>0)State.reload=1.7f;
   if(input.drop!=consumed.drop)world.Drop(this);if(input.pickup!=consumed.pickup)world.Pickup(this);
   shotCooldown-=dt;if(input.fire&&State.weapon!=0&&State.reload<=0&&State.ammo>0&&shotCooldown<=0){State.ammo--;shotCooldown=State.weapon==1?.3f:.16f;world.Fire(this,State.aim);}
   consumed.jump=input.jump;consumed.reload=input.reload;consumed.pickup=input.pickup;consumed.drop=input.drop;
  }
  public int Capacity=>State.weapon==1?12:Class.Magazine;
  LanInput BotInput(){var input=new LanInput{jump=consumed.jump,reload=consumed.reload,pickup=consumed.pickup,drop=consumed.drop};var enemy=world.Enemy(this);if(!enemy)return input;
   var delta=enemy.transform.position-transform.position;delta.y=0;var aim=Quaternion.Euler(0,Mathf.Sin(Time.time*1.7f+Id)*4,0)*delta.normalized;input.ax=aim.x;input.az=aim.z;bool visible=world.CanSee(this,enemy);input.fire=visible&&delta.magnitude<14;input.sprint=delta.magnitude>10;
   if(State.ammo==0&&State.reserve>0)input.reload++;if(State.weapon==0){input.pickup++;}
   if(Time.time>nextPath){nextPath=Time.time+.55f+Id*.02f;var path=new NavMeshPath();if(NavMesh.CalculatePath(transform.position,enemy.transform.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete){corners=path.corners;corner=1;}else corners=null;}
   Vector3 direction=Vector3.zero;if(corners!=null&&corner<corners.Length){while(corner<corners.Length-1&&Vector3.Distance(transform.position,corners[corner])<.4f)corner++;direction=corners[corner]-transform.position;direction.y=0;}
   if(visible&&delta.magnitude<7){direction=Vector3.Cross(Vector3.up,delta.normalized)*Mathf.Sin(Time.time+Id);if(!NavMesh.SamplePosition(transform.position+direction*.7f,out var nav,.4f,NavMesh.AllAreas))direction=Vector3.zero;}
   direction=direction.normalized;input.x=direction.x;input.z=direction.z;return input;
  }
  void Update(){if(!session||session.Slots==null)return;if(!session.IsHost)transform.position=Vector3.Lerp(transform.position,State.position,1-Mathf.Exp(-18*Time.unscaledDeltaTime));
   bool alive=State.hp>0;if(alive&&!wasAlive)MagicFx.Spawn(transform.position,session.Slots[Id].role);wasAlive=alive;if(alive&&State.shield>0&&!shieldFx)shieldFx=MagicFx.Protect(transform,State.shield);if(shieldFx&&(!alive||State.shield<=0)){shieldFx.StopEffect();shieldFx=null;}visual.gameObject.SetActive(alive);ring.SetActive(alive);label.text=session.Slots[Id].name+(session.Slots[Id].bot?" · BOT":"")+(State.shield>0?" ◇":"");if(Camera.main)label.transform.rotation=Camera.main.transform.rotation;
   float speed=(transform.position-lastPosition).magnitude/Mathf.Max(.001f,Time.unscaledDeltaTime);lastPosition=transform.position;stepPhase+=Time.deltaTime*(speed>4?18:12);visual.localPosition=visualBase+Vector3.up*(alive&&speed>.2f?Mathf.Abs(Mathf.Sin(stepPhase))*.07f:0);visual.localRotation=Quaternion.LookRotation(State.aim.sqrMagnitude>.01f?State.aim:Vector3.forward)*Quaternion.Euler(alive?5:75,0,speed>.2f?Mathf.Sin(stepPhase)*7:0);
   if(State.reload>0&&!renderedReload&&assets.reload)sound.PlayOneShot(assets.reload,.25f);renderedReload=State.reload>0;
   if(renderedWeapon!=State.weapon){if(renderedWeapon>=0&&State.weapon>0&&assets.equip)sound.PlayOneShot(assets.equip,.25f);if(gun)Destroy(gun.gameObject);gun=State.weapon==0?null:MakeWeapon(State.weapon,transform,assets);renderedWeapon=State.weapon;}
   if(gun){gun.gameObject.SetActive(alive);gun.localPosition=new Vector3(.2f,.34f,.04f);gun.rotation=Quaternion.LookRotation(State.aim.sqrMagnitude>.01f?State.aim:Vector3.forward);}
   if(State.shot!=renderedShot){renderedShot=State.shot;if(State.impact)MagicFx.Impact(State.hit);tracer.SetPosition(0,transform.position+Vector3.up*.35f);tracer.SetPosition(1,State.hit);flash=Time.time+.08f;var clips=State.weapon==1?assets.pistolShots:assets.rifleShots;if(clips.Length>0)sound.PlayOneShot(clips[Random.Range(0,clips.Length)],.35f);}
   tracer.enabled=Time.time<flash;
   if(alive&&State.grounded&&speed>.3f&&Time.time>stepAt&&assets.steps.Length>0){stepAt=Time.time+(speed>4?.28f:.4f);sound.PlayOneShot(assets.steps[Random.Range(0,assets.steps.Length)],.15f);}
  }
  public static Transform MakeWeapon(int kind,Transform parent,LanAssets assets){var root=new GameObject("Arma visible").transform;root.SetParent(parent,false);Instantiate(kind==1?assets.pistol:assets.rifle,root);return root;}
  public static GameObject Ring(Transform parent,Material material,Color color,float radius){var go=new GameObject("Anillo de equipo");go.transform.SetParent(parent,false);var line=go.AddComponent<LineRenderer>();line.sharedMaterial=material;line.useWorldSpace=false;line.loop=true;line.positionCount=32;line.widthMultiplier=.025f;line.startColor=line.endColor=color;for(int i=0;i<32;i++){float a=i*Mathf.PI*2/32;line.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,.025f,Mathf.Sin(a)*radius));}return go;}
 }
}
