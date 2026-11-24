using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
namespace Valle.Lan {
 // Vehículos de una plaza: blindados de la DEA (tipo 0) y chutos de la organización (tipo 1).
 // Blindado de la DEA para un jugador: sube con V, conduce con WASD y dispara la ametralladora con el ratón.
 // Del tamaño de la escala del juego: unas 1,75 de largo junto a personajes de 0,65 de alto.
 // El anfitrión lo mueve, dispara y lleva su vida; los clientes lo dibujan con lo que reciben.
 public sealed partial class LanWorld {
  public const float VehicleHealth=700,VehicleSpeed=7,VehicleReverse=3,VehicleTurn=130,VehicleBoard=1.8f,VehicleRespawn=90,VehicleScale=.6f;
  public const float GunRange=28,GunDamage=12,GunDelay=.1f,GunSpread=.8f,VehicleBlast=1.3f,CameraRiding=8.5f;
  public readonly List<LanVehicleState> Vehicles=new List<LanVehicleState>();
  readonly Dictionary<int,LanVehicleView> vehicleViews=new Dictionary<int,LanVehicleView>();
  public const int Armored=0,Truck=1;
  public static float MaxHealth(int kind)=>kind==Truck?420:VehicleHealth;
  static float TopSpeed(int kind)=>kind==Truck?9:VehicleSpeed;
  static float GunHit(int kind)=>kind==Truck?10:GunDamage;
  public static string KindName(int kind)=>kind==Truck?"CHUTO":"BLINDADO";
  readonly Dictionary<int,(Vector3 at,float yaw)> vehicleSpawns=new Dictionary<int,(Vector3,float)>();int vehicleId;
  // Sobre el camino, unos pasos adentro de su entrada y mirando hacia el campamento.
  // Dos blindados (entradas Camino y Pista) y dos chutos en el camino este del campamento.
  void SpawnVehicles(){
   if(deaEntries!=null)foreach(int index in new[]{1,2})if(index<deaEntries.Length){var entry=deaEntries[index].position;var inward=-entry;inward.y=0;AddVehicle(Armored,LanNightRules.Dea,entry+inward.normalized*4.5f);}
   foreach(float z in new[]{1.3f,-1.3f})AddVehicle(Truck,LanNightRules.Org,new Vector3(9,0,z),90);
  }
  void AddVehicle(int kind,int team,Vector3 at,float? yaw=null){
   if(NavMesh.SamplePosition(at,out var hit,4,NavMesh.AllAreas))at=hit.position;
   var toward=-at;toward.y=0;float facing=yaw??Mathf.Atan2(toward.x,toward.z)*Mathf.Rad2Deg;
   var v=new LanVehicleState{id=++vehicleId,kind=kind,team=team,position=at,yaw=facing,hp=MaxHealth(kind),aim=Quaternion.Euler(0,facing,0)*Vector3.forward};
   Vehicles.Add(v);vehicleSpawns[v.id]=(at,facing);
  }
  public LanVehicleState VehicleOf(LanPawn pawn)=>pawn.State.ride>0?Vehicles.Find(v=>v.id==pawn.State.ride):null;
  public bool Board(LanPawn pawn){
   var state=pawn.State;
   if(state.hp<=0||state.stun>0||session.Night.Deploying||session.Night.Capturer==pawn.Id)return false;
   var vehicle=Vehicles.Where(v=>v.team==pawn.Team&&!v.ai&&v.respawn<=0&&v.hp>0&&v.driver<0&&Flat(v.position-pawn.transform.position)<=VehicleBoard).OrderBy(v=>Flat(v.position-pawn.transform.position)).FirstOrDefault();
   if(vehicle==null)return false;
   vehicle.driver=pawn.Id;state.ride=vehicle.id;state.lamp=false;state.reload=0;
   var view=VehicleView(vehicle);view.Aim=pawn.State.aim;var controller=pawn.GetComponent<CharacterController>();if(controller)controller.enabled=false;
   pawn.transform.position=vehicle.position;state.position=vehicle.position;return true;
  }
  public bool Seated(LanPawn pawn){var v=VehicleOf(pawn);return v!=null&&v.respawn<=0&&v.driver==pawn.Id;}
  // Baja al costado libre más cercano.
  public void Leave(LanPawn pawn){
   var v=VehicleOf(pawn);pawn.State.ride=0;
   if(v==null)return;
   if(v.driver==pawn.Id)v.driver=-1;
   var rotation=Quaternion.Euler(0,v.yaw,0);var at=v.position+rotation*Vector3.left*1.1f;
   foreach(var side in new[]{Vector3.left,Vector3.right,Vector3.back,Vector3.forward}){var probe=v.position+rotation*side*(side.z!=0?1.4f:1.1f);if(NavMesh.SamplePosition(probe,out var hit,.8f,NavMesh.AllAreas)){at=hit.position;break;}}
   pawn.transform.position=at+Vector3.up*.05f;pawn.State.position=pawn.transform.position;Physics.SyncTransforms();
  }
  // El conductor da la dirección con WASD, apunta la torreta con el ratón y dispara con clic.
  public void Steer(LanPawn pawn,LanInput input){
   var v=VehicleOf(pawn);if(v==null||v.driver!=pawn.Id)return;var view=VehicleView(v);
   view.Drive=Vector3.ClampMagnitude(new Vector3(input.x,0,input.z),1);
   var aim=new Vector3(input.ax,0,input.az);if(aim.sqrMagnitude>.01f)view.Aim=aim.normalized;view.Firing=input.fire;
  }
  void TickVehicles(float dt){
   TickConvoy();var gone=new List<LanVehicleState>();
   foreach(var v in Vehicles.ToArray()){
    var view=VehicleView(v);
    if(v.temp&&!v.ai&&v.driver<0&&v.respawn<=0&&autopilots.TryGetValue(v.id,out var parked)&&session.Night.Clock>parked.Expire){gone.Add(v);continue;}
    if(v.respawn>0){
     v.respawn-=dt;
     if(v.respawn<=0&&v.temp){gone.Add(v);continue;}
     if(v.respawn<=0){var home=vehicleSpawns[v.id];v.hp=MaxHealth(v.kind);v.position=home.at;v.yaw=home.yaw;v.speed=0;view.Body.enabled=false;view.transform.SetPositionAndRotation(v.position,Quaternion.Euler(0,v.yaw,0));view.Body.enabled=true;}
     continue;
    }
    // Conducción arcade: gira hacia donde apunta el conductor y acelera; si pide ir hacia atrás, retrocede.
    view.Boost=1;
    if(v.ai)AiDrive(v,view,dt);else if(v.driver<0)view.Drive=Vector3.zero;
    float target=0;
    if(view.Drive.sqrMagnitude>.01f){
     var forward=Quaternion.Euler(0,v.yaw,0)*Vector3.forward;float along=Vector3.Dot(forward,view.Drive.normalized);
     if(along>-.35f){float wanted=Mathf.Atan2(view.Drive.x,view.Drive.z)*Mathf.Rad2Deg;v.yaw=Mathf.MoveTowardsAngle(v.yaw,wanted,VehicleTurn*dt*Mathf.Clamp01(.35f+Mathf.Abs(v.speed)/TopSpeed(v.kind)));target=TopSpeed(v.kind)*view.Boost*view.Drive.magnitude*Mathf.Clamp01(.3f+along);}
     else target=-VehicleReverse*view.Drive.magnitude;
    }
    v.speed=Mathf.MoveTowards(v.speed,target,(Mathf.Abs(target)>Mathf.Abs(v.speed)?6:10)*dt);
    view.transform.rotation=Quaternion.Euler(0,v.yaw,0);
    if(view.Body.enabled){view.Fall=view.Body.isGrounded?-1:view.Fall-20*dt;var before=view.transform.position;view.Body.Move((view.transform.forward*v.speed+Vector3.up*view.Fall)*dt);
     float moved=Vector3.Dot(view.transform.position-before,view.transform.forward)/Mathf.Max(dt,1e-4f);if(Mathf.Abs(v.speed)>.5f&&Mathf.Abs(moved)<Mathf.Abs(v.speed)*.3f)v.speed*=.5f;}
    v.position=view.transform.position;
    // Ametralladora.
    if(v.driver<0&&!v.ai)view.Firing=false;
    v.aim=Vector3.RotateTowards(v.aim.sqrMagnitude>.01f?v.aim:view.transform.forward,view.Aim.sqrMagnitude>.01f?view.Aim:v.aim,4*dt,0).normalized;
    view.Cooldown-=dt;
    int shooter=v.driver>=0?v.driver:autopilots.TryGetValue(v.id,out var pilot)?pilot.Owner.Id:-1;
    if(view.Firing&&!session.Night.Deploying&&view.Cooldown<=0&&Pawns.TryGetValue(shooter,out var gunner)){
     view.Cooldown=GunDelay;var spread=Quaternion.Euler(0,Random.Range(-GunSpread,GunSpread),0)*v.aim;
     // La bala sale a la altura del pecho para alcanzar a los combatientes; el fogonazo se dibuja en la torreta.
     Shoot(gunner,v.position+Vector3.up*.35f+v.aim*.75f,spread,GunRange,GunHit(v.kind),view.gameObject,out var end,out var impact);
     v.shot++;v.hit=end;v.impact=impact;if(v.driver>=0)gunner.MarkShot();
    }
    if(v.driver>=0&&Pawns.TryGetValue(v.driver,out var rider)){rider.transform.position=v.position;rider.State.position=v.position;rider.State.aim=v.aim;}
   }
   foreach(var v in gone){Vehicles.Remove(v);autopilots.Remove(v.id);}
  }
  // ---------- Convoy del Sicario ----------
  // Tres chutos llegan en fila por los caminos: el primero, artillado, entra disparando; los otros dos frenan
  // derrapando junto al punto marcado y de cada uno bajan dos pistoleros. Después la organización puede manejarlos.
  public const float ConvoyBoost=1.35f,ConvoyGap=1.1f,ConvoyLife=150,TechnicalRange=20;
  sealed class Convoy{public float At;public LanPawn Owner;public Vector3[] Path;public bool Armed;public int Side;}
  sealed class Autopilot{public Vector3[] Path;public int Corner;public LanPawn Owner;public bool Armed,Parked,Drifting;public int Side;public float Drift,Stuck,Unstick,Expire;public Vector3 Last;}
  readonly List<Convoy> convoy=new List<Convoy>();readonly Dictionary<int,Autopilot> autopilots=new Dictionary<int,Autopilot>();
  public int Parked{get;private set;}
  public bool CallChutos(LanPawn pawn,Vector3 at){
   if(!NavMesh.SamplePosition(at,out var end,3,NavMesh.AllAreas))return false;at=end.position;
   var starts=(orgSpawns??new Transform[0]).Concat(deaEntries??new Transform[0]).Concat(zones??new Transform[0]).Select(t=>t.position).OrderByDescending(p=>{float d=Flat(p-at);return d>14&&d<40?40-Mathf.Abs(d-24):-d;});
   foreach(var start in starts){
    var path=new NavMeshPath();
    if(!NavMesh.SamplePosition(start,out var from,3,NavMesh.AllAreas)||!NavMesh.CalculatePath(from.position,at,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete||path.corners.Length<2)continue;
    var approach=at-path.corners[path.corners.Length-2];approach.y=0;approach=approach.sqrMagnitude>.01f?approach.normalized:Vector3.forward;var side=Vector3.Cross(Vector3.up,approach);
    var stops=new[]{at-approach*.5f,at+side*2.4f-approach*2.6f,at-side*2.4f-approach*4.4f};
    for(int i=0;i<3;i++){
     var own=new NavMeshPath();var stop=stops[i];
     var corners=NavMesh.SamplePosition(stop,out var spot,2,NavMesh.AllAreas)&&NavMesh.CalculatePath(from.position,spot.position,NavMesh.AllAreas,own)&&own.status==NavMeshPathStatus.PathComplete?own.corners:path.corners;
     convoy.Add(new Convoy{At=session.Night.Clock+i*ConvoyGap,Owner=pawn,Path=corners,Armed=i==0,Side=i==2?-1:1});
    }
    return true;
   }
   return false;
  }
  void TickConvoy(){
   for(int i=convoy.Count-1;i>=0;i--){
    var c=convoy[i];if(session.Night.Clock<c.At)continue;convoy.RemoveAt(i);
    var heading=c.Path[1]-c.Path[0];heading.y=0;heading=heading.sqrMagnitude>.01f?heading.normalized:Vector3.forward;float yaw=Mathf.Atan2(heading.x,heading.z)*Mathf.Rad2Deg;
    var v=new LanVehicleState{id=++vehicleId,kind=Truck,team=c.Owner.Team,position=c.Path[0],yaw=yaw,hp=MaxHealth(Truck),aim=heading,crew=2,ai=true,temp=true,speed=TopSpeed(Truck)};
    Vehicles.Add(v);autopilots[v.id]=new Autopilot{Path=c.Path,Corner=1,Owner=c.Owner,Armed=c.Armed,Side=c.Side,Last=c.Path[0]};
   }
  }
  void AiDrive(LanVehicleState v,LanVehicleView view,float dt){
   if(!autopilots.TryGetValue(v.id,out var ap)){v.ai=false;return;}
   if(ap.Armed)AiGun(v,view,ap);else view.Firing=false;
   if(ap.Parked){view.Drive=Vector3.zero;return;}
   var pos=v.position;
   while(ap.Corner<ap.Path.Length-1&&Flat(ap.Path[ap.Corner]-pos)<1.4f)ap.Corner++;
   float left=Flat(ap.Path[ap.Path.Length-1]-pos);
   if(!ap.Drifting&&ap.Corner>=ap.Path.Length-1&&left<3.2f){ap.Drifting=true;ap.Drift=0;}
   if(ap.Drifting){
    // Frenada de película: gira de costado mientras se detiene.
    ap.Drift+=dt;var forward=Quaternion.Euler(0,v.yaw,0)*Vector3.forward;view.Drive=Quaternion.Euler(0,85*ap.Side,0)*forward*.4f;
    v.speed=Mathf.MoveTowards(v.speed,0,12*dt);if(ap.Drift>.8f||Mathf.Abs(v.speed)<.4f)Park(v,ap);return;
   }
   // Si se atasca, retrocede un momento y vuelve a intentar.
   ap.Stuck=Flat(pos-ap.Last)<.05f*dt*60?ap.Stuck+dt:0;ap.Last=pos;
   if(ap.Stuck>1.2f){ap.Unstick=.7f;ap.Stuck=0;}
   var to=ap.Path[Mathf.Min(ap.Corner,ap.Path.Length-1)]-pos;to.y=0;
   if(ap.Unstick>0){ap.Unstick-=dt;view.Drive=-(Quaternion.Euler(0,v.yaw,0)*Vector3.forward);return;}
   view.Drive=to.sqrMagnitude>.01f?to.normalized:Vector3.zero;view.Boost=ConvoyBoost;
  }
  void Park(LanVehicleState v,Autopilot ap){
   ap.Parked=true;ap.Drifting=false;v.speed=0;ap.Expire=session.Night.Clock+ConvoyLife;Parked++;
   if(ap.Armed)return;
   // Bajan los dos pistoleros y el chuto queda libre para la organización.
   var right=Quaternion.Euler(0,v.yaw,0)*Vector3.right;
   for(int i=0;i<v.crew;i++){var npc=SpawnArmed(Gunman,ap.Owner,v.position+right*(i==0?1.1f:-1.1f));npc.home=v.position;}
   v.crew=0;v.ai=false;
  }
  // Ametralladora del chuto artillado: al rival más cercano que tenga a la vista.
  void AiGun(LanVehicleState v,LanVehicleView view,Autopilot ap){
   LanPawn target=null;float best=TechnicalRange;var eye=v.position+Vector3.up*.9f;
   foreach(var p in Pawns.Values){if(p.Team==v.team||p.State.hp<=0)continue;float d=Flat(p.transform.position-v.position);if(d<best&&LanVision.Clear(eye,p.transform.position+Vector3.up*.35f)){best=d;target=p;}}
   view.Firing=target;if(target){var aim=target.transform.position-v.position;aim.y=0;view.Aim=aim.normalized;}
  }
  public static Vector3 Muzzle(LanVehicleState v)=>v.kind==Truck?v.position+Vector3.up*.95f+Quaternion.Euler(0,v.yaw,0)*Vector3.back*.35f+v.aim*.5f:v.position+Vector3.up*1.5f*VehicleScale+v.aim*.9f*VehicleScale;
  bool HitVehicle(Collider collider,LanPawn shooter,float damage){
   var view=collider.GetComponent<LanVehicleView>();if(!view)return false;
   var v=Vehicles.Find(x=>x.id==view.Id);if(v!=null&&v.team!=shooter.Team)DamageVehicle(v,damage,shooter.Id);
   return true;
  }
  public void DamageVehicle(LanVehicleState v,float damage,int attacker){
   if(v.respawn>0||v.hp<=0)return;
   v.hp-=damage;if(v.hp>0)return;
   // Destruido: los ocupantes salen heridos, explota y vuelve a la entrada en 90 s.
   if(v.driver>=0&&Pawns.TryGetValue(v.driver,out var rider)){Leave(rider);Hurt(rider,35,attacker,"blindado");}
   // La tripulación del chuto artillado salta y sigue peleando.
   if(v.crew>0&&autopilots.TryGetValue(v.id,out var crew)){var right=Quaternion.Euler(0,v.yaw,0)*Vector3.right;for(int i=0;i<v.crew;i++){var npc=SpawnArmed(Gunman,crew.Owner,v.position+right*(i==0?1.2f:-1.2f));npc.home=v.position;}v.crew=0;}
   v.driver=-1;v.hp=0;v.speed=0;v.ai=false;v.respawn=v.temp?3:VehicleRespawn;
   var view=VehicleView(v);view.Body.enabled=false;
   Explode(v.position+Vector3.up*.1f,-1,attacker,3,50,2,1.5f,8);
  }
  // Explosiones cerca del blindado lo dañan más que las balas.
  void BlastVehicles(Vector3 at,int team,int owner,float radius,float damage){
   foreach(var v in Vehicles.ToArray()){if(v.team==team||v.respawn>0)continue;float distance=Flat(v.position-at);float reach=radius+.8f;if(distance<=reach)DamageVehicle(v,Mathf.Lerp(damage,damage*.3f,distance/reach)*VehicleBlast,owner);}
  }
  LanVehicleView VehicleView(LanVehicleState v){
   if(vehicleViews.TryGetValue(v.id,out var view)&&view)return view;
   var go=new GameObject((v.kind==Truck?"Chuto ":"Blindado ")+v.id);go.transform.SetParent(transform);go.transform.SetPositionAndRotation(v.position,Quaternion.Euler(0,v.yaw,0));
   view=go.AddComponent<LanVehicleView>();view.Id=v.id;view.Build(assets,session.IsHost,v.kind,v.temp);view.Aim=v.aim;vehicleViews[v.id]=view;
   return view;
  }
  // Dibujo: interpolación en los clientes, torreta, ruedas, fogonazos, faros y motor.
  void UpdateVehicles(){
   foreach(var id in vehicleViews.Keys.ToArray())if(!Vehicles.Any(v=>v.id==id)){if(vehicleViews[id])Destroy(vehicleViews[id].gameObject);vehicleViews.Remove(id);}
   foreach(var v in Vehicles)VehicleView(v).Show(v,session.IsHost);
   if(session.LocalSlot>=0&&Camera.main&&Camera.main.orthographic){float wanted=Pawns[session.LocalSlot].State.ride>0?CameraRiding:LanNightRules.CameraSize;Camera.main.orthographicSize=Mathf.MoveTowards(Camera.main.orthographicSize,wanted,Time.deltaTime*6);}
  }
 }
 public sealed class LanVehicleView:MonoBehaviour {
  public int Id;public CharacterController Body;public Vector3 Drive,Aim;public bool Firing;public float Cooldown,Fall,Boost=1;
  Transform model,turret;readonly List<Transform> wheels=new List<Transform>();LineRenderer tracer;AudioSource engine,gun;Light[] beams;LanAssets art;int shots=-1;float flash,spin,lastYaw,nextDust;bool shown=true,honked,skidding;
  public void Build(LanAssets assets,bool host,int kind=0,bool convoy=false){
   art=assets;
   Body=gameObject.AddComponent<CharacterController>();Body.radius=.55f;Body.height=1.1f;Body.center=new Vector3(0,.55f,0);Body.stepOffset=.2f;Body.skinWidth=.03f;Body.enabled=host;
   bool pickup=kind==LanWorld.Truck;var prefab=pickup?(convoy&&assets.convoy?assets.convoy:assets.chuto):assets.vehicle;
   model=prefab?Instantiate(prefab,transform).transform:pickup?LanModels.Chuto(transform):LanModels.Vehicle(transform);model.localPosition=Vector3.zero;model.localRotation=Quaternion.identity;model.localScale=Vector3.one*(pickup?1:LanWorld.VehicleScale);
   foreach(var beam in model.GetComponentsInChildren<Light>())beam.range*=.8f;
   turret=model.Find("Torreta");foreach(Transform child in model)if(child.name.StartsWith("Rueda"))wheels.Add(child);
   foreach(var c in model.GetComponentsInChildren<Collider>())Destroy(c);
   beams=model.GetComponentsInChildren<Light>();
   tracer=new GameObject("Trazo").AddComponent<LineRenderer>();tracer.transform.SetParent(transform,false);tracer.sharedMaterial=assets.glow;tracer.positionCount=2;tracer.startWidth=.03f;tracer.endWidth=.012f;tracer.startColor=new Color(1,.85f,.5f);tracer.endColor=new Color(1,.6f,.3f,.3f);tracer.enabled=false;
   engine=gameObject.AddComponent<AudioSource>();engine.clip=assets.engine;engine.loop=true;engine.spatialBlend=1;engine.minDistance=3;engine.maxDistance=40;engine.rolloffMode=AudioRolloffMode.Linear;engine.volume=.35f;if(engine.clip)engine.Play();
   gun=gameObject.AddComponent<AudioSource>();gun.spatialBlend=1;gun.minDistance=3;gun.maxDistance=45;gun.rolloffMode=AudioRolloffMode.Linear;
  }
  public void Show(LanVehicleState v,bool host){
   bool alive=v.respawn<=0;
   if(alive!=shown){shown=alive;model.gameObject.SetActive(alive);if(engine.clip){if(alive)engine.Play();else engine.Stop();}}
   if(!host){transform.position=Vector3.Lerp(transform.position,v.position,1-Mathf.Exp(-14*Time.deltaTime));if((transform.position-v.position).sqrMagnitude>16)transform.position=v.position;transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.Euler(0,v.yaw,0),1-Mathf.Exp(-12*Time.deltaTime));}
   if(!alive)return;
   if(turret&&v.aim.sqrMagnitude>.01f)turret.rotation=Quaternion.Slerp(turret.rotation,Quaternion.LookRotation(v.aim),1-Mathf.Exp(-16*Time.deltaTime));
   spin+=v.speed*Time.deltaTime*160;foreach(var w in wheels)w.localRotation=Quaternion.Euler(spin,0,0);
   engine.pitch=.7f+Mathf.Abs(v.speed)/LanWorld.VehicleSpeed*.6f+(v.kind==LanWorld.Truck?.2f:0);
   foreach(var beam in beams)beam.enabled=v.driver>=0||v.ai;
   // Entrada de película: bocina al aparecer, polvo detrás a toda velocidad y derrape al frenar.
   if(v.temp&&v.ai&&!honked){honked=true;if(art.horn)gun.PlayOneShot(art.horn,.9f);LanFx.Dust(transform.position,18);}
   float yawRate=Mathf.Abs(Mathf.DeltaAngle(lastYaw,v.yaw))/Mathf.Max(Time.deltaTime,1e-4f);lastYaw=v.yaw;
   bool skid=yawRate>80&&Mathf.Abs(v.speed)>2;
   if(skid&&!skidding){if(art.skid)gun.PlayOneShot(art.skid,.7f);var local=LanSession.Instance&&LanSession.Instance.World&&LanSession.Instance.LocalSlot>=0?LanSession.Instance.World.Pawns[LanSession.Instance.LocalSlot].transform.position:transform.position;if(Vector3.Distance(local,transform.position)<9)LanFx.Shake=Mathf.Max(LanFx.Shake,.12f);}
   skidding=skid;
   if((Mathf.Abs(v.speed)>5||skid)&&Time.time>=nextDust){nextDust=Time.time+(skid?.05f:.12f);LanFx.Dust(transform.position-transform.forward*.7f,skid?4:2);}
   if(shots<0)shots=v.shot;
   if(v.shot!=shots){shots=v.shot;var muzzle=LanWorld.Muzzle(v);LanFx.Muzzle(muzzle);if(v.impact)LanFx.Impact(v.hit);tracer.SetPosition(0,muzzle);tracer.SetPosition(1,v.hit);flash=Time.time+.05f;
    var clips=art.rifleShots;if(clips!=null&&clips.Length>0)gun.PlayOneShot(clips[Random.Range(0,clips.Length)],.45f);}
   tracer.enabled=Time.time<flash;
  }
 }
}
