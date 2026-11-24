using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
namespace Valle.Lan {
 // Apoyo llamado con habilidades: estación médica, fogata, base de drones, andanada de granadas,
 // helicóptero con soldados y chutos con pistoleros. Todo viaja como marcas.
 public sealed partial class LanWorld {
  public const float HeliSeconds=24,HeliArrive=3,HeliLeave=21,HeliDrop=4,HeliRadius=16,HeliLeash=30,HeliChase=7,HeliDamage=8,HeliEvery=.15f,HeliAltitude=6;
  public const float DroneEvery=2;public const int DroneTotal=40,DroneActive=6;
  public static bool Structure(int kind)=>kind==LanItems.WallMark||kind==LanItems.MedStationMark||kind==LanItems.CampfireMark||kind==LanItems.DroneStationMark;
  // ---------- Estaciones (una por dueño; la nueva reemplaza a la anterior) ----------
  public void PlaceStation(LanPawn pawn,int kind){
   foreach(var old in Marks.Where(m=>m.kind==kind&&m.owner==pawn.Id&&!m.fired).ToList()){old.fired=true;old.hp=0;old.time=.6f;RefreshWall(old);}
   var at=pawn.transform.position+pawn.State.aim*1.1f;if(NavMesh.SamplePosition(at,out var hit,1.5f,NavMesh.AllAreas))at=hit.position;
   float time=kind==LanItems.MedStationMark?LanAbilities.StationSeconds:99999;
   var mark=AddMark(kind,pawn.Team,at,time);mark.owner=pawn.Id;
   mark.hp=kind==LanItems.MedStationMark?LanAbilities.StationHealth:kind==LanItems.CampfireMark?LanAbilities.CampfireHealth:120;
   mark.size=kind==LanItems.MedStationMark?LanAbilities.StationRadius:kind==LanItems.CampfireMark?LanAbilities.CampfireRadius:DroneTotal;
   View(mark);Physics.SyncTransforms();
  }
  // Cura a los aliados en su radio; la base de drones lanza uno cada 2 s hasta 40, con 6 a la vez.
  void TickStation(LanMark mark,float dt){
   if(mark.kind==LanItems.DroneStationMark){
    int active=Npcs.Count(n=>n.kind==SwarmDrone&&n.owner==mark.owner);
    if(mark.size<=0){if(active==0){mark.fired=true;mark.time=.6f;RefreshWall(mark);}return;}
    if(mark.age-mark.from.x>=DroneEvery&&active<DroneActive&&Pawns.TryGetValue(mark.owner,out var owner)){mark.from.x=mark.age;mark.size--;var drone=SpawnArmed(SwarmDrone,owner,mark.position);drone.home=mark.position;}
    return;
   }
   float rate=mark.kind==LanItems.MedStationMark?LanAbilities.StationHeal:LanAbilities.CampfireHeal;
   foreach(var p in Pawns.Values)if(p.Team==mark.team&&p.State.hp>0&&p.State.down<=0&&Flat(p.transform.position-mark.position)<=mark.size)p.State.hp=Mathf.Min(p.Class.Health,p.State.hp+rate*dt);
  }
  // ---------- Andanada del Granadero ----------
  public void StartBarrage(LanPawn pawn,Vector3 at){var mark=AddMark(LanItems.BarrageMark,pawn.Team,at,LanAbilities.BarrageSeconds);mark.owner=pawn.Id;mark.size=LanAbilities.BarrageSpread;}
  void TickBarrage(LanMark mark){
   if(mark.age<mark.shots*LanAbilities.BarrageEvery)return;
   if(!Pawns.TryGetValue(mark.owner,out var owner)||owner.State.hp<=0){mark.time=0;return;}
   mark.shots++;var r=Random.insideUnitCircle*mark.size;
   var grenade=AddMark(LanItems.GrenadeMark,mark.team,mark.position+new Vector3(r.x,.08f,r.y),LanExplosives.GrenadeFuse);
   grenade.from=owner.transform.position+Vector3.up*.6f;grenade.owner=owner.Id;grenade.size=LanExplosives.GrenadeRadius;grenade.fired=mark.shots%2==0;
  }
  // Granada aturdidora: destello y polvo; inmoviliza a los rivales cercanos.
  public int Stuns{get;private set;}
  void Stun(LanMark grenade){Stuns++;
   AddMark(LanItems.BlastMark,grenade.team,grenade.position,.5f);
   foreach(var p in Pawns.Values)if(p.Team!=grenade.team&&p.State.hp>0&&p.State.ride==0&&Flat(p.transform.position-grenade.position)<=LanAbilities.StunRadius)p.State.stun=LanAbilities.StunSeconds;
  }
  // ---------- Helicóptero del Francotirador ----------
  public int HeliShots{get;private set;}
  public void CallHelicopter(LanPawn pawn,Vector3 at){
   if(NavMesh.SamplePosition(at,out var hit,3,NavMesh.AllAreas))at=hit.position;
   var mark=AddMark(LanItems.HeliMark,pawn.Team,at,HeliSeconds);mark.owner=pawn.Id;var dir=Random.insideUnitCircle.normalized;mark.from=new Vector3(dir.x,0,dir.y);mark.focus=at;
  }
  public static Vector3 HeliPosition(LanMark mark,float age){
   var center=mark.focus.sqrMagnitude>.01f?mark.focus:mark.position;
   var hover=center+Vector3.up*HeliAltitude+Quaternion.Euler(0,age*40,0)*Vector3.forward*2.2f;
   if(age<HeliArrive)return Vector3.Lerp(mark.position-mark.from*45+Vector3.up*(HeliAltitude+3),hover,Mathf.SmoothStep(0,1,age/HeliArrive));
   if(age>HeliLeave)return Vector3.Lerp(hover,center+mark.from*45+Vector3.up*(HeliAltitude+4),Mathf.SmoothStep(0,1,(age-HeliLeave)/(HeliSeconds-HeliLeave)));
   return hover;
  }
  // Persigue al rival más cercano (sin alejarse más de 30 del punto marcado) y le dispara si lo tiene a la vista.
  void TickHeli(LanMark mark,float dt){
   if(!Pawns.TryGetValue(mark.owner,out var owner))return;
   if(!mark.fired&&mark.age>=HeliDrop){mark.fired=true;foreach(int side in new[]{-1,1}){var soldier=SpawnArmed(Soldier,owner,mark.position+new Vector3(side*1.2f,0,.6f));soldier.home=mark.position;}LanFx.Dust(mark.position,30);}
   if(mark.age<HeliArrive||mark.age>HeliLeave)return;
   LanPawn target=null;float best=float.MaxValue;
   foreach(var p in Pawns.Values){
    if(p.Team==mark.team||p.State.hp<=0)continue;
    float fromFocus=Flat(p.transform.position-mark.focus),fromCall=Flat(p.transform.position-mark.position);
    if(fromFocus>HeliRadius||fromCall>HeliLeash||fromFocus>=best)continue;best=fromFocus;target=p;
   }
   var goal=target?target.transform.position:mark.position;var step=goal-mark.focus;step.y=0;
   mark.focus+=Vector3.ClampMagnitude(step,HeliChase*dt*(target?1:.5f));
   if(!target||mark.age<mark.hp)return;
   mark.hp=mark.age+HeliEvery;
   var from=HeliPosition(mark,mark.age);var at=target.transform.position+Vector3.up*.3f;
   if(!LanVision.Clear(from,at))return;
   bool hit=Random.value<.7f;mark.aim=hit?at:at+new Vector3(Random.Range(-1.2f,1.2f),0,Random.Range(-1.2f,1.2f));mark.shots++;HeliShots++;
   if(!hit)return;
   if(target.State.ride>0){var v=VehicleOf(target);if(v!=null&&v.team!=mark.team)DamageVehicle(v,HeliDamage,owner.Id);return;}
   if(target.State.shield>0)return;
   Hurt(target,HeliDamage*100/(100+target.Class.Defense)*LanAbilities.Protection(this,from,target),owner.Id,"helicóptero");
  }
  // ---------- Dibujo ----------
  GameObject MakeSupport(LanMark mark,GameObject go,LanMarkView view){
   GameObject Model(GameObject prefab,System.Func<Transform,Transform> fallback,Transform parent){var t=prefab?Instantiate(prefab,parent).transform:fallback(parent);t.localPosition=Vector3.zero;return t.gameObject;}
   switch(mark.kind){
    case LanItems.MedStationMark:case LanItems.CampfireMark:case LanItems.DroneStationMark:{
     bool med=mark.kind==LanItems.MedStationMark,fire=mark.kind==LanItems.CampfireMark;
     go.name=med?"Estacion medica":fire?"Fogata":"Base de drones";
     view.Body=Model(med?assets.medStation:fire?assets.campfire:assets.droneStation,med?LanModels.MedStation:fire?(System.Func<Transform,Transform>)LanModels.Campfire:LanModels.DroneStation,go.transform).transform;
     var box=go.AddComponent<BoxCollider>();box.size=new Vector3(.6f,.5f,.6f);box.center=Vector3.up*.25f;
     if(!fire&&!med)return go;
     view.Ring=LanPawn.Ring(go.transform,assets.glow,med?new Color(.35f,1,.5f,.45f):new Color(1,.6f,.25f,.35f),mark.size).GetComponent<LineRenderer>();view.Ring.widthMultiplier=.04f;
     view.Glow=PointLight(go.transform,med?new Color(.4f,1,.55f):new Color(1,.55f,.2f),med?4:mark.size+3.5f,med?2:3,Vector3.up*.7f);
     if(fire){view.Sound=Sound(go,18);view.Sound.clip=assets.fireLoop;view.Sound.loop=true;view.Sound.volume=.4f;if(view.Sound.clip)view.Sound.Play();}
     return go;}
    case LanItems.BarrageMark:{go.name="Andanada";view.Ring=LanPawn.Ring(go.transform,assets.glow,new Color(1,.4f,.2f,.5f),mark.size).GetComponent<LineRenderer>();view.Ring.widthMultiplier=.05f;return go;}
    case LanItems.HeliMark:{
     go.name="Helicoptero";view.Plane=Model(assets.heli,LanModels.Helicopter,transform).transform;view.Plane.position=HeliPosition(mark,mark.age);
     view.Glow=new GameObject("Reflector").AddComponent<Light>();view.Glow.transform.SetParent(view.Plane,false);view.Glow.transform.localRotation=Quaternion.Euler(70,0,0);view.Glow.type=LightType.Spot;view.Glow.range=14;view.Glow.spotAngle=55;view.Glow.intensity=8;view.Glow.color=new Color(1,.95f,.85f);view.Glow.shadows=LightShadows.None;
     view.Line=go.AddComponent<LineRenderer>();view.Line.sharedMaterial=assets.glow;view.Line.positionCount=2;view.Line.widthMultiplier=.04f;view.Line.startColor=new Color(1,.85f,.5f);view.Line.endColor=new Color(1,.6f,.3f,.3f);view.Line.enabled=false;
     view.Sound=Sound(view.Plane.gameObject,60);view.Sound.clip=assets.helicopter;view.Sound.loop=true;view.Sound.volume=.8f;if(view.Sound.clip)view.Sound.Play();
     return go;}
   }
   return go;
  }
  void UpdateSupport(LanMark mark,LanMarkView view,GameObject go){
   switch(mark.kind){
    case LanItems.CampfireMark:
     if(view.Glow)view.Glow.intensity=2.5f+Mathf.PerlinNoise(Time.time*8,mark.id)*2;
     if(Time.time>=view.Born+view.Spent){view.Spent=Time.time-view.Born+8;LanFx.Burn(mark.position+Vector3.up*.05f,.32f,8.5f);}
     break;
    case LanItems.MedStationMark:if(view.Ring)view.Ring.startColor=view.Ring.endColor=new Color(.35f,1,.5f,.25f+.25f*Mathf.Sin(Time.time*4));if(view.Glow)view.Glow.enabled=Mathf.Repeat(Time.time,1.2f)<.8f;break;
    case LanItems.HeliMark:{
     float age=Time.time-view.Born;var at=HeliPosition(mark,age);var ahead=HeliPosition(mark,age+.2f)-at;
     view.Plane.position=Vector3.Lerp(view.Plane.position,at,1-Mathf.Exp(-6*Time.deltaTime));view.Plane.rotation=Quaternion.LookRotation(ahead.sqrMagnitude>.0001f?new Vector3(ahead.x,0,ahead.z):mark.from)*Quaternion.Euler(Mathf.Clamp(ahead.magnitude*20,0,14),0,0);
     var rotor=view.Plane.Find("Rotor");if(rotor)rotor.Rotate(0,1800*Time.deltaTime,0,Space.Self);var tail=view.Plane.Find("Rotor de cola");if(tail)tail.Rotate(2400*Time.deltaTime,0,0,Space.Self);
     if(view.Glow)view.Glow.transform.rotation=Quaternion.LookRotation(mark.position-view.Glow.transform.position);
     if(mark.shots!=view.Shots){view.Shots=mark.shots;view.Line.SetPosition(0,at+Vector3.down*.4f);view.Line.SetPosition(1,mark.aim);view.Flash=Time.time+.05f;LanFx.Impact(mark.aim);var clips=assets.rifleShots;if(clips!=null&&clips.Length>0)view.Sound.PlayOneShot(clips[Random.Range(0,clips.Length)],.5f);}
     view.Line.enabled=Time.time<view.Flash;
     if(age>HeliDrop-.5f&&age<HeliDrop+1&&!view.Started){view.Started=true;LanFx.Dust(mark.position,30);}
     break;}
   }
  }
 }
}
