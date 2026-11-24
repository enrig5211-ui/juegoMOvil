using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
namespace Valle.Lan {
 public sealed partial class LanWorld:MonoBehaviour {
  [FormerlySerializedAs("blueSpawns")] public Transform[] deaEntries;
  [FormerlySerializedAs("redSpawns")] public Transform[] orgSpawns;
  public Transform[] hideouts,zones;
  // Mapa táctico: imagen cenital del escenario (la genera ChapareSetup) y el área que cubre.
  public Texture2D mapTexture;public Vector3 mapCenter;public float mapSize=136;
  public Transform[] itemSpots,lamps;
  public readonly List<LanMark> Marks=new List<LanMark>();
  public bool DropsDirty;
  public readonly Dictionary<int,LanPawn> Pawns=new Dictionary<int,LanPawn>();
  public readonly List<LanDrop> Drops=new List<LanDrop>();
  readonly Dictionary<int,GameObject> dropViews=new Dictionary<int,GameObject>();
  readonly Dictionary<int,GameObject> markViews=new Dictionary<int,GameObject>();
  LanSession session;LanAssets assets;int dropId,markId;float nextVision;
  public LanSession Session=>session;
  public LanPawn LeaderPawn=>session.Night.Leader>=0&&Pawns.TryGetValue(session.Night.Leader,out var pawn)?pawn:null;
  public void Initialize(LanSession owner){session=owner;assets=Resources.Load<LanAssets>("Multiplayer/LanAssets");
   for(int i=0;i<session.Slots.Length;i++){var go=new GameObject("Combatiente "+i);var pawn=go.AddComponent<LanPawn>();Pawns.Add(i,pawn);pawn.Initialize(this,owner,i,assets);}
   if(session.IsHost){foreach(var slot in session.Slots)if(slot.bot&&slot.team==LanNightRules.Dea)slot.point=Random.Range(0,deaEntries.Length);for(int i=0;i<session.Slots.Length;i++)Respawn(i);SpawnItems();SpawnVehicles();}
   InitProps();gameObject.AddComponent<LanNightCycle>().Initialize(owner,this);gameObject.AddComponent<LanAudio>().Initialize(owner,this,assets);
   var camera=Camera.main.GetComponent<Valle.PlayerCamera>();if(!camera)camera=Camera.main.gameObject.AddComponent<Valle.PlayerCamera>();camera.target=Pawns[session.LocalSlot].transform;if(!camera.GetComponent<LanShake>())camera.gameObject.AddComponent<LanShake>();camera.pitch=45;camera.distance=10;Camera.main.orthographic=true;Camera.main.orthographicSize=LanNightRules.CameraSize;
  }
  public int ClampEntry(int point)=>Mathf.Clamp(point,0,deaEntries.Length-1);
  // Lugar de aparición según el puesto dentro de su bando: la organización en el campamento, la DEA en su entrada.
  public Vector3 Spawn(int id){
   var slot=session.Slots[id];int k=session.Slots.Count(s=>s.team==slot.team&&s.id<id);
   if(slot.team==LanNightRules.Org){var at=orgSpawns[k%orgSpawns.Length].position;int ring=k/orgSpawns.Length;return ring==0?at:at+Quaternion.Euler(0,k*67,0)*Vector3.forward*(1.1f*ring);}
   var spread=new Vector3(k%2*2-1,0,k/2%2*2-1)*1.1f;if(k>=4)spread=Quaternion.Euler(0,45,0)*spread*1.7f;
   return deaEntries[ClampEntry(slot.point)].position+spread;
  }
  public bool LampShows(LanPawn pawn)=>!session.Night.Deploying||session.LocalSlot<0||pawn.Team==session.Slots[session.LocalSlot].team;
  public bool Shows(LanPawn pawn)=>LampShows(pawn)&&pawn.Seen;
  public void Respawn(int id){var p=Pawns[id];p.ResetLife(Spawn(id));}
  public void Tick(float dt){foreach(var p in Pawns.Values){p.ServerTick(dt);if(p.Ability==LanAbilities.Scan)p.State.ability=Mathf.Max(p.State.ability,ScanReady-session.Night.Clock);}TickVehicles(dt);TickMarks(dt);TickSpearmen(dt);Drops.RemoveAll(d=>d==null);UpdateDrops();}
  public void Apply(LanPacket packet){if(packet.actors!=null)foreach(var state in packet.actors)if(Pawns.TryGetValue(state.id,out var p))p.Apply(state);if(packet.hasDrops){Drops.Clear();if(packet.drops!=null)Drops.AddRange(packet.drops);}Marks.Clear();if(packet.marks!=null)Marks.AddRange(packet.marks);Npcs.Clear();if(packet.npcs!=null)Npcs.AddRange(packet.npcs);Vehicles.Clear();if(packet.vehicles!=null)Vehicles.AddRange(packet.vehicles);if(packet.hasBroken)ApplyBroken(packet.broken);UpdateDrops();}
  public LanActorState[] States()=>Pawns.Values.Select(p=>p.State).ToArray();
  public LanPawn Enemy(LanPawn self){if(session.Night.Deploying)return null;LanPawn best=null;float distance=LanNightRules.ExposedRange*LanNightRules.ExposedRange;foreach(var p in Pawns.Values){if(p==self||p.Team==self.Team||p.State.hp<=0)continue;float d=(p.transform.position-self.transform.position).sqrMagnitude;if(d>=distance||!LanVision.Sees(this,self,p))continue;distance=d;best=p;}return best;}
  public bool CanSee(LanPawn from,LanPawn to){var a=from.transform.position+Vector3.up*.35f;var b=to.transform.position+Vector3.up*.35f;var hits=Physics.RaycastAll(a,(b-a).normalized,(b-a).magnitude,LanNightRules.SightMask,QueryTriggerInteraction.Ignore);return !hits.Any(h=>h.collider.gameObject!=from.gameObject&&h.collider.gameObject!=to.gameObject);}
  public void Fire(LanPawn pawn,Vector3 aim){
   var state=pawn.State;
   Shoot(pawn,pawn.transform.position+Vector3.up*.35f,aim,LanAbilities.Sniper(pawn)?LanAbilities.SniperRange:24,(state.weapon==1?24:16)*LanAbilities.DamageBonus(pawn),null,out var end,out var impact);
   state.shot++;pawn.MarkShot();state.hit=end;state.impact=impact;
  }
  // Un disparo (de un combatiente o de la ametralladora del blindado): daña al primero que encuentra.
  public string LastStruck{get;private set;}
  public void Shoot(LanPawn shooter,Vector3 origin,Vector3 aim,float range,float damage,GameObject ignore,out Vector3 end,out bool impact){
   end=origin+aim*range;float distance=range;LanPawn victim=null;Collider struck=null;
   foreach(var hit in Physics.RaycastAll(origin,aim,range,LanNightRules.SightMask,QueryTriggerInteraction.Ignore)){var other=hit.collider.gameObject;if(other==shooter.gameObject||other==ignore)continue;if(hit.distance<distance){distance=hit.distance;end=hit.point;victim=hit.collider.GetComponent<LanPawn>();struck=hit.collider;}}
   impact=distance<range;LastStruck=struck?struck.name:"";
   if(!victim&&struck&&!HitSpearman(struck,shooter,damage)&&!HitWall(struck,shooter,damage)&&!HitVehicle(struck,shooter,damage))HitProp(struck,damage,origin);
   if(victim&&victim.Team!=shooter.Team&&victim.State.hp>0&&victim.State.shield<=0){victim.State.hp=Mathf.Max(0,victim.State.hp-damage*100/(100+victim.Class.Defense)*LanAbilities.DamageTaken(this,shooter,victim));session.Night.Interrupt(victim.Id);if(victim.State.hp<=0){CountDown(victim,"bala");victim.Fall();session.ScoreKill(shooter.Id,victim.Id);}}
  }
  public void Drop(LanPawn p){if(p.State.weapon==0)return;Vector3 pos=p.transform.position;if(NavMesh.SamplePosition(pos+p.transform.right*.7f,out var nav,2,NavMesh.AllAreas))pos=nav.position;
   AddDrop(new LanDrop{weapon=p.State.weapon,ammo=p.State.ammo,reserve=p.State.reserve,position=pos+Vector3.up*.025f});p.State.weapon=0;p.State.ammo=p.State.reserve=0;p.State.reload=0;
  }
  public LanDrop Nearest(LanPawn p){return Drops.Where(d=>Vector3.Distance(d.position,p.State.position)<1.6f&&!Physics.Linecast(p.State.position+Vector3.up*.4f,d.position+Vector3.up*.4f,LanNightRules.SightMask,QueryTriggerInteraction.Ignore)).OrderBy(d=>(d.position-p.State.position).sqrMagnitude).FirstOrDefault();}
  public void Pickup(LanPawn p){var drop=Nearest(p);if(drop==null)return;Drops.Remove(drop);DropsDirty=true;if(drop.item>0){DropItem(p);p.State.item=drop.item;return;}Drop(p);p.State.weapon=drop.weapon;p.State.ammo=drop.ammo;p.State.reserve=drop.reserve;p.State.reload=0;}
  void UpdateDrops(){foreach(var key in dropViews.Keys.ToArray())if(!Drops.Any(d=>d.id==key)){Destroy(dropViews[key]);dropViews.Remove(key);}foreach(var d in Drops){if(!dropViews.TryGetValue(d.id,out var go)){go=new GameObject("Arma flotante "+d.id);go.transform.SetParent(transform);var model=d.item>0?ItemModel(d.item,go.transform):LanPawn.MakeWeapon(d.weapon,go.transform,assets);model.localPosition=Vector3.up*.55f;LanPawn.Ring(go.transform,assets.glow,d.item>0?LanItems.Colors[d.item]:new Color(.35f,.95f,1),.36f);dropViews.Add(d.id,go);}go.SetActive(DropVisible(d));go.transform.position=d.position;var t=go.transform.GetChild(0);t.localPosition=Vector3.up*(.55f+Mathf.Sin(Time.time*2+d.id)*.08f);t.localRotation=Quaternion.Euler(0,Time.time*50,0);}}
  void Update(){if(!session)return;if(!session.IsHost)UpdateDrops();UpdateMarks();UpdateVehicles();UpdateVision();UpdateSpearmen();}
  // ---------- Items del suelo ----------
  public void SpawnItems(){if(itemSpots==null)return;foreach(var spot in itemSpots)PlaceItem(spot.position,LanItems.Pick());}
  public void RefillItems(int count){
   if(itemSpots==null)return;
   foreach(var spot in itemSpots.Where(s=>!Drops.Any(d=>Flat(d.position-s.position)<1)).OrderBy(_=>Random.value).Take(count))PlaceItem(spot.position,LanItems.Pick());
  }
  public void PlaceItem(Vector3 at,int item)=>AddDrop(new LanDrop{item=item,position=at+Vector3.up*.025f});
  public void DropItem(LanPawn p){
   if(p.State.item==0)return;Vector3 pos=p.transform.position;
   if(NavMesh.SamplePosition(pos-p.transform.right*.7f,out var nav,2,NavMesh.AllAreas))pos=nav.position;
   AddDrop(new LanDrop{item=p.State.item,position=pos+Vector3.up*.025f});p.State.item=0;
  }
  // Si se llena, se descarta primero un arma tirada, no un item del mapa.
  public void AddDrop(LanDrop drop){
   if(Drops.Count>=LanNightRules.MaxDrops){var old=Drops.FirstOrDefault(d=>d.item==0)??Drops[0];Drops.Remove(old);}
   drop.id=++dropId;Drops.Add(drop);DropsDirty=true;
  }
  bool DropVisible(LanDrop drop){
   if(session.LocalSlot<0||!Pawns.TryGetValue(session.LocalSlot,out var local))return true;
   return Flat(local.transform.position-drop.position)<=Mathf.Lerp(8,40,LanVision.Daylight(session))||IsLit(drop.position);
  }
  static Transform ItemModel(int item,Transform parent){
   var root=new GameObject("Item").transform;root.SetParent(parent,false);
   var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);DestroyImmediate(cube.GetComponent<Collider>());cube.transform.SetParent(root,false);cube.transform.localScale=Vector3.one*.28f;cube.GetComponent<Renderer>().material.color=LanItems.Colors[item];
   return root;
  }
  // ---------- Qué ve el equipo local ----------
  void UpdateVision(){
   if(Time.unscaledTime<nextVision||session.LocalSlot<0)return;nextVision=Time.unscaledTime+.1f;
   int team=session.Slots[session.LocalSlot].team;bool all=session.Phase!="match";
   foreach(var target in Pawns.Values){
    if(all||target.Team==team){target.Seen=true;continue;}
    bool seen=false;foreach(var ally in Pawns.Values)if(ally.Team==team&&ally.State.hp>0&&LanVision.Sees(this,ally,target)){seen=true;break;}
    target.Seen=seen;
   }
  }
  static float Flat(Vector3 v){v.y=0;return v.magnitude;}
 }
 public sealed class LanPawn:MonoBehaviour {
  public LanActorState State=new LanActorState();public int Id{get;private set;}public int Team=>session.Slots[Id].team;public LanClass Class=>IsLeader?LanClass.Leader:LanClass.For(Team,session.Slots[Id].role);
  public int Role=>session.Slots[Id].role;public int Ability=>LanAbilities.For(this);public int CocaLeft;float cocaWalk;public int AbilityUses;float renderedCooldown;
  public bool IsLeader=>session.Night.Leader==Id;public bool Holding{get;private set;}
  public bool Seen=true;public Light LampLight=>lamp;float firedAt=-99;Light lamp,halo;
  public bool FiredRecently=>session.Night.Clock-firedAt<LanNightRules.FiredReveal;
  public void MarkShot()=>firedAt=session.Night.Clock;
  LanSession session;LanWorld world;LanAssets assets;CharacterController controller;Transform visual,gun;GameObject ring;TextMesh label;AudioSource sound;LineRenderer tracer;
  LanBotBrain brain;Vector3 lastPosition;readonly LanCartoon cartoon=new LanCartoon();string lookKey;float vertical,shotCooldown,regenDelay,stepPhase,flash,stepAt;int renderedWeapon=-1,renderedShot;bool renderedReload,wasAlive;LineRenderer shieldArc;float nextDust,smoothSpeed;LanInput consumed=new LanInput();
  public void Initialize(LanWorld w,LanSession owner,int id,LanAssets art){Id=id;State.id=id;world=w;session=owner;assets=art;brain=new LanBotBrain(this,w,owner);
   controller=gameObject.AddComponent<CharacterController>();controller.height=.65f;controller.radius=.2f;controller.center=new Vector3(0,.325f,0);controller.stepOffset=.18f;controller.skinWidth=.025f;controller.enabled=session.IsHost;
   BuildLook();
   ring=Ring(transform,Team==LanNightRules.Dea?assets.blueMaterial:assets.redMaterial,Color.white,.31f);var nameObject=new GameObject("Nombre");nameObject.transform.SetParent(transform,false);nameObject.transform.localPosition=Vector3.up*.95f;label=nameObject.AddComponent<TextMesh>();label.fontSize=40;label.characterSize=.045f;label.anchor=TextAnchor.MiddleCenter;label.color=Team==LanNightRules.Dea?new Color(.45f,.85f,1):new Color(1,.55f,.45f);
   sound=gameObject.AddComponent<AudioSource>();sound.playOnAwake=false;sound.spatialBlend=id==session.LocalSlot?0:1;sound.minDistance=2;sound.maxDistance=24;sound.rolloffMode=AudioRolloffMode.Linear;
   tracer=new GameObject("Trazo").AddComponent<LineRenderer>();tracer.transform.SetParent(transform,false);tracer.sharedMaterial=assets.glow;tracer.positionCount=2;tracer.startWidth=.025f;tracer.endWidth=.008f;tracer.startColor=new Color(1,.9f,.6f);tracer.endColor=new Color(1,.65f,.3f,.35f);tracer.enabled=false;
   lamp=new GameObject("Linterna").AddComponent<Light>();lamp.transform.SetParent(transform,false);lamp.transform.localPosition=Vector3.up*.9f;lamp.type=LightType.Spot;lamp.range=LanNightRules.LampRange+5;lamp.spotAngle=LanNightRules.LampHalfAngle*2+10;lamp.innerSpotAngle=LanNightRules.LampHalfAngle*2-14;lamp.intensity=14;lamp.color=new Color(1,.95f,.82f);lamp.shadows=id==session.LocalSlot?LightShadows.Soft:LightShadows.None;lamp.enabled=false;
   if(id==session.LocalSlot){halo=new GameObject("Vista cercana").AddComponent<Light>();halo.transform.SetParent(transform,false);halo.transform.localPosition=Vector3.up*1.6f;halo.type=LightType.Point;halo.shadows=LightShadows.None;}
  }
  // La linterna se ve aunque su dueño esté oculto: así se delata.
  void UpdateLights(bool alive,bool shown){
   float day=LanVision.Daylight(session);
   bool on=alive&&State.lamp&&(shown||world.LampShows(this));lamp.enabled=on;
   if(on)lamp.transform.rotation=Quaternion.LookRotation(State.aim.sqrMagnitude>.01f?State.aim:Vector3.forward)*Quaternion.Euler(5,0,0);
   if(!halo)return;
   bool goggles=State.vision>0;halo.enabled=alive&&day<.95f;
   halo.range=(goggles?LanNightRules.NightVisionRange:LanNightRules.NearVision)+2;halo.intensity=(goggles?3:1.6f)*(1-day);halo.color=goggles?new Color(.55f,1,.6f):new Color(.75f,.8f,1);
  }
  public void ResetLife(Vector3 position){State.hp=Class.Health;State.stamina=Class.Stamina;State.weapon=IsLeader?1:2;State.ammo=Capacity;State.reserve=Class.Reserve;State.down=0;State.reload=0;State.lamp=false;State.vision=0;State.ability=State.active=State.marked=State.stun=0;State.ride=State.equip=0;State.boost=0;CocaLeft=0;State.grenades=LanExplosives.StartGrenades(this);State.shield=2;State.position=position;State.aim=Team==0?Vector3.right:Vector3.left;controller.enabled=false;transform.position=position;controller.enabled=session.IsHost;vertical=0;shotCooldown=.3f;brain.Reset();consumed=session.InputFor(Id).Counters();}
  // Un líder sin vida queda reducido; los demás caen y esperan su oleada.
  public void Fall(){State.lamp=false;State.vision=0;if(IsLeader){State.down=LanNightRules.ReducedSeconds;return;}world.Drop(this);world.DropItem(this);}
  public void Recover(){State.down=0;State.hp=Class.Health*LanNightRules.RecoverFraction;State.shield=1;}
  public void ClampToClass(){State.hp=Mathf.Min(State.hp,Class.Health);State.stamina=Mathf.Min(State.stamina,Class.Stamina);}
  public void Apply(LanActorState state){State=state;if((transform.position-state.position).sqrMagnitude>9)transform.position=state.position;}
  public void ServerTick(float dt){Holding=false;if(!session.Slots[Id].bot&&!session.Slots[Id].loaded){State.shield=2;return;}if(Team==LanNightRules.Dea&&session.Night.Deploying){State.shield=2;return;}if(State.hp<=0){controller.enabled=false;return;}var input=session.Slots[Id].bot?BotInput():session.InputFor(Id);if(input==null||!input.Valid())input=new LanInput();if(Riding(input,dt))return;
   controller.enabled=true;State.shield=Mathf.Max(0,State.shield-dt);State.vision=Mathf.Max(0,State.vision-dt);Holding=input.hold;if(State.stun>0){State.stun=Mathf.Max(0,State.stun-dt);var aimed=input;input=consumed.Counters();input.ax=aimed.ax;input.az=aimed.az;Holding=false;}State.ability=Mathf.Max(0,State.ability-dt);State.active=Mathf.Max(0,State.active-dt);State.marked=Mathf.Max(0,State.marked-dt);if(input.ability!=consumed.ability)LanAbilities.Trigger(world,this,input);if(input.grenade!=consumed.grenade)world.ThrowGrenade(this,input.reach);var movement=Vector3.ClampMagnitude(new Vector3(input.x,0,input.z),1);var aim=new Vector3(input.ax,0,input.az);if(aim.sqrMagnitude>.01f)State.aim=aim.normalized;State.equip=input.equip==1&&State.grenades>0?1:0;
   bool run=input.sprint&&State.stamina>2&&movement.sqrMagnitude>.01f;float speed=Class.Speed*(run?1.5f:1);if(run){State.stamina=Mathf.Max(0,State.stamina-dt*18);regenDelay=.8f;}else if((regenDelay-=dt)<=0)State.stamina=Mathf.Min(Class.Stamina,State.stamina+dt*22);
   if(controller.isGrounded&&vertical<0)vertical=-2;if(input.jump!=consumed.jump&&controller.isGrounded&&State.stamina>=12){vertical=7;State.stamina-=12;regenDelay=.8f;}
   vertical-=22*dt;controller.Move((movement*speed+Vector3.up*vertical)*dt);State.grounded=controller.isGrounded;State.position=transform.position;
   if(transform.position.y < -5){if(IsLeader){ResetLife(world.Spawn(Id));return;}world.Drop(this);State.hp=0;session.Slots[Id].deaths++;return;}
   State.boost=Mathf.Max(0,State.boost-dt);
   // Frenesí del Cocalero: deja hojas de coca por donde camina.
   if(CocaLeft>0){if(!LanAbilities.Active(this,LanAbilities.Frenzy))CocaLeft=0;else{cocaWalk+=movement.magnitude*speed*dt;if(cocaWalk>=LanAbilities.CocaStep){cocaWalk=0;CocaLeft--;world.PlaceItem(transform.position,LanItems.Coca);}}}
   if(State.reload>0){State.reload-=dt;if(State.reload<=0){int n=Mathf.Min(Capacity-State.ammo,State.reserve);State.ammo+=n;State.reserve-=n;}}
   if(input.reload!=consumed.reload&&State.weapon!=0&&State.reload<=0&&State.ammo<Capacity&&State.reserve>0)State.reload=1.7f;
   if(input.drop!=consumed.drop)world.Drop(this);if(input.pickup!=consumed.pickup)world.Pickup(this);if(input.lamp!=consumed.lamp)State.lamp=!State.lamp;if(input.use!=consumed.use&&State.item!=0&&LanItems.Use(world,this))State.item=0;
   shotCooldown-=dt;if(input.fire&&State.equip==0&&!session.Night.Deploying&&session.Night.Capturer!=Id&&State.weapon!=0&&State.reload<=0&&State.ammo>0&&shotCooldown<=0){State.ammo--;shotCooldown=(State.weapon==1?.3f:.16f)*LanAbilities.FireDelay(this);world.Fire(this,State.aim);}
   consumed.jump=input.jump;consumed.reload=input.reload;consumed.pickup=input.pickup;consumed.drop=input.drop;consumed.lamp=input.lamp;consumed.use=input.use;consumed.ability=input.ability;consumed.grenade=input.grenade;consumed.vehicle=input.vehicle;consumed.equip=input.equip;
  }
  // En el blindado no camina ni dispara: el vehículo usa sus controles. V sube o baja.
  bool Riding(LanInput input,float dt){
   bool toggle=input.vehicle!=consumed.vehicle;
   if(State.ride==0){if(!toggle||!world.Board(this))return false;}
   else if(toggle||session.Slots[Id].bot||!world.Seated(this)){world.Leave(this);consumed=input.Counters();return true;}
   controller.enabled=false;world.Steer(this,input);
   State.shield=Mathf.Max(0,State.shield-dt);State.vision=Mathf.Max(0,State.vision-dt);State.stun=0;State.lamp=false;State.equip=0;
   State.ability=Mathf.Max(0,State.ability-dt);State.active=Mathf.Max(0,State.active-dt);State.marked=Mathf.Max(0,State.marked-dt);State.stamina=Mathf.Min(Class.Stamina,State.stamina+dt*22);
   consumed=input.Counters();return true;
  }
  public int Capacity=>State.weapon==1?12:Class.Magazine;
  LanInput BotInput()=>brain.Think(consumed.Counters());
  void Update(){if(!session||session.Slots==null)return;if(!session.IsHost)transform.position=Vector3.Lerp(transform.position,State.position,1-Mathf.Exp(-18*Time.unscaledDeltaTime));
   if(LookKey()!=lookKey)BuildLook();bool alive=State.hp>0,dying=!alive&&State.down<=0&&cartoon.Dying,shown=(alive||State.down>0||dying)&&world.Shows(this)&&State.ride==0;if(alive&&shown&&!wasAlive)LanFx.Dust(transform.position,14);if(!alive&&wasAlive&&State.down<=0&&shown)LanFx.Dust(transform.position,10);wasAlive=alive;if(State.ability>renderedCooldown+.5f&&alive&&shown){LanFx.Dust(transform.position,8);LanFx.Flash(transform.position+Vector3.up*.6f,new Color(1,.85f,.55f),.12f);}renderedCooldown=State.ability;UpdateGuard(alive&&shown);visual.gameObject.SetActive(shown);ring.SetActive(shown&&!dying);label.gameObject.SetActive(shown&&!dying);label.text=LabelText();UpdateLights(alive,shown);if(Camera.main)label.transform.rotation=Camera.main.transform.rotation;
   float raw=(transform.position-lastPosition).magnitude/Mathf.Max(.001f,Time.unscaledDeltaTime);lastPosition=transform.position;smoothSpeed=Mathf.Lerp(smoothSpeed,Mathf.Min(raw,12),1-Mathf.Exp(-6*Time.unscaledDeltaTime));float speed=smoothSpeed;stepPhase+=Time.deltaTime*(speed>4?18:12);bool over=session.Phase=="result",teamWon=(Team==LanNightRules.Dea)==(session.Night.Outcome==LanOutcome.Captured);cartoon.Pose(this,speed,over&&teamWon,over&&!teamWon);
   if(State.reload>0&&!renderedReload&&assets.reload)sound.PlayOneShot(assets.reload,.25f);renderedReload=State.reload>0;
   if(renderedWeapon!=State.weapon){if(renderedWeapon>=0&&State.weapon>0&&assets.equip)sound.PlayOneShot(assets.equip,.25f);if(gun)Destroy(gun.gameObject);gun=State.weapon==0?null:MakeWeapon(State.weapon,transform,assets);renderedWeapon=State.weapon;}
   UpdateHeld(alive&&shown&&State.equip==1);if(gun){gun.gameObject.SetActive(alive&&shown&&State.equip==0);gun.localPosition=new Vector3(.2f,.34f,.04f);gun.rotation=Quaternion.LookRotation(State.aim.sqrMagnitude>.01f?State.aim:Vector3.forward);}
   if(State.shot!=renderedShot){renderedShot=State.shot;bool silent=false;if(!session.IsHost&&!silent)MarkShot();if(State.impact)LanFx.Impact(State.hit);if(!silent)LanFx.Muzzle(transform.position+Vector3.up*.35f+(State.aim.sqrMagnitude>.01f?State.aim:Vector3.forward)*.45f);tracer.SetPosition(0,transform.position+Vector3.up*.35f);tracer.SetPosition(1,State.hit);flash=silent?0:Time.time+.08f;var clips=State.weapon==1?assets.pistolShots:assets.rifleShots;if(clips.Length>0)sound.PlayOneShot(clips[Random.Range(0,clips.Length)],silent?.06f:.35f);}
   tracer.enabled=Time.time<flash;
   if(alive&&State.grounded&&speed>.3f&&Time.time>stepAt&&assets.steps.Length>0){stepAt=Time.time+(speed>4?.28f:.4f);sound.PlayOneShot(assets.steps[Random.Range(0,assets.steps.Length)],.15f);}
  }
  // Granada en la mano cuando el jugador la elige con la tecla 2.
  Transform held;
  void UpdateHeld(bool show){
   if(show&&!held){var ball=GameObject.CreatePrimitive(PrimitiveType.Sphere);DestroyImmediate(ball.GetComponent<Collider>());ball.name="Granada en mano";held=ball.transform;held.SetParent(transform,false);held.localScale=Vector3.one*.11f;ball.GetComponent<Renderer>().material.color=new Color(.22f,.27f,.16f);}
   if(!held)return;held.gameObject.SetActive(show);if(show)held.position=transform.position+Vector3.up*.38f+(State.aim.sqrMagnitude>.01f?State.aim:Vector3.forward)*.22f+Quaternion.Euler(0,90,0)*(State.aim.sqrMagnitude>.01f?State.aim:Vector3.forward)*.12f;
  }
  string LabelText(){var slot=session.Slots[Id];string text=slot.name+(slot.bot?" · BOT":"")+(State.shield>0?" ◇":"");bool ally=session.LocalSlot>=0&&session.Slots[session.LocalSlot].team==Team;if(!ally&&State.marked>0)text+=" · MARCADO";return IsLeader&&(ally||State.down>0)?"LÍDER · "+text:text;}
  // Protección al aparecer: el anillo parpadea en celeste. Escudo del Pesado: un arco al frente.
  void UpdateGuard(bool visible){
   var line=ring.GetComponent<LineRenderer>();line.startColor=line.endColor=visible&&State.shield>0&&Mathf.Repeat(Time.time,.4f)<.2f?new Color(.4f,.9f,1):Color.white;
   bool shielding=visible&&LanAbilities.Active(this,LanAbilities.Shield);
   if(shielding&&!shieldArc){
    shieldArc=new GameObject("Escudo").AddComponent<LineRenderer>();shieldArc.transform.SetParent(transform,false);shieldArc.useWorldSpace=false;shieldArc.sharedMaterial=assets.glow;shieldArc.widthMultiplier=.05f;shieldArc.positionCount=15;
    for(int i=0;i<15;i++){float a=Mathf.Deg2Rad*Mathf.Lerp(-70,70,i/14f);shieldArc.SetPosition(i,new Vector3(Mathf.Sin(a)*.5f,.32f,Mathf.Cos(a)*.5f));}
    shieldArc.startColor=shieldArc.endColor=new Color(.45f,.8f,1,.9f);
   }
   if(shieldArc){shieldArc.enabled=shielding;if(shielding)shieldArc.transform.rotation=Quaternion.LookRotation(State.aim.sqrMagnitude>.01f?State.aim:Vector3.forward);}
  }
  // El modelo depende del bando, del personaje y de si es el líder; se rehace si cambia.
  string LookKey()=>$"{Team}:{Role}:{IsLeader}";
  void BuildLook(){if(visual)Destroy(visual.gameObject);visual=Instantiate(assets.Character(Team,IsLeader,Role),transform).transform;cartoon.Bind(visual);lookKey=LookKey();}
  public static Transform MakeWeapon(int kind,Transform parent,LanAssets assets){var root=new GameObject("Arma visible").transform;root.SetParent(parent,false);Instantiate(kind==1?assets.pistol:assets.rifle,root);return root;}
  public static GameObject Ring(Transform parent,Material material,Color color,float radius){var go=new GameObject("Anillo de equipo");go.transform.SetParent(parent,false);var line=go.AddComponent<LineRenderer>();line.sharedMaterial=material;line.useWorldSpace=false;line.loop=true;line.positionCount=32;line.widthMultiplier=.025f;line.startColor=line.endColor=color;for(int i=0;i<32;i++){float a=i*Mathf.PI*2/32;line.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,.025f,Mathf.Sin(a)*radius));}return go;}
 }
}
