using System.Linq;
using UnityEngine;
using UnityEngine.AI;
namespace Valle.Lan {
 // Marcas del mapa: el anfitrión las hace avanzar y cada pantalla las dibuja.
 public sealed partial class LanWorld {
  public const int MaxMarks=64;
  public LanMark AddMark(int kind,int team,Vector3 at,float time){
   if(Marks.Count>=MaxMarks){var old=Marks.FirstOrDefault(m=>m.kind!=LanItems.WallMark&&m.kind!=LanItems.StrikeMark&&m.kind!=LanItems.GrenadeMark)??Marks[0];Marks.Remove(old);}
   var mark=new LanMark{id=++markId,kind=kind,team=team,position=at,time=time};Marks.Add(mark);return mark;
  }
  void TickMarks(float dt){
   for(int i=Marks.Count-1;i>=0;i--){
    if(i>=Marks.Count)continue;
    var mark=Marks[i];mark.age+=dt;
    switch(mark.kind){
     case LanItems.GrenadeMark:
      mark.time-=dt;
      if(mark.time<=0){Marks.Remove(mark);if(mark.fired)Stun(mark);else Explode(mark.position,mark.team,mark.owner,LanExplosives.GrenadeRadius,LanExplosives.GrenadeDamage,LanExplosives.PropGrenade,LanExplosives.GrenadeFire,LanExplosives.GrenadeBurn);}
      continue;
     case LanItems.StrikeMark:if(TickStrike(mark,dt))Marks.Remove(mark);continue;
     case LanItems.WallMark:
      mark.time-=dt;
      if(mark.time<=0){if(mark.fired)Marks.Remove(mark);else{mark.fired=true;mark.time=.6f;RefreshWall(mark);}}
      continue;
     case LanItems.FireMark:TickFire(mark,dt);mark.time-=dt;if(mark.time<=0)Marks.Remove(mark);continue;
     case LanItems.SupplyMark:
      mark.time-=dt;
      if(mark.time<=0){if(mark.fired)Marks.Remove(mark);else{mark.fired=true;mark.time=LanAbilities.SupplyStay;DropSupply(mark);}}
      continue;
     case LanItems.TrapMark:TickMine(mark);continue;
     case LanItems.MedStationMark:case LanItems.CampfireMark:case LanItems.DroneStationMark:
      if(mark.fired){mark.time-=dt;if(mark.time<=0)Marks.Remove(mark);continue;}
      TickStation(mark,dt);mark.time-=dt;if(mark.time<=0&&!mark.fired){mark.fired=true;mark.time=.6f;RefreshWall(mark);}continue;
     case LanItems.BarrageMark:TickBarrage(mark);mark.time-=dt;if(mark.time<=0)Marks.Remove(mark);continue;
     case LanItems.HeliMark:TickHeli(mark,dt);mark.time-=dt;if(mark.time<=0)Marks.Remove(mark);continue;
     case LanItems.FlareMark:case LanItems.BlastMark:case LanItems.ExplosionMark:case LanItems.ScanMark:case LanItems.ShieldMark:case LanItems.SmokeMark:
      mark.time-=dt;if(mark.time<=0)Marks.Remove(mark);continue;
    }
    if(mark.fired){mark.time-=dt;if(mark.time<=0)Marks.Remove(mark);continue;}
    if(session.Night.Deploying)continue;
    // Alarmas: se activan cuando pasa un rival y lo marcan.
    var intruders=Pawns.Values.Where(p=>p.Team!=mark.team&&p.State.hp>0&&Flat(p.transform.position-mark.position)<=LanNightRules.AlarmRadius).ToList();
    if(intruders.Count>0){mark.fired=true;mark.time=LanNightRules.AlarmAlertSeconds;foreach(var p in intruders)p.State.marked=LanAbilities.MarkSeconds;}
   }
  }
  // Faroles, bengalas, fuego y el humo rojo del bombardeo iluminan y delatan a quien esté cerca.
  public bool IsLit(Vector3 at){
   if(lamps!=null)foreach(var lamp in lamps)if(lamp&&lamp.gameObject.activeInHierarchy&&Flat(lamp.position-at)<=LanNightRules.LampPostRadius)return true;
   foreach(var mark in Marks){
    float radius=mark.kind==LanItems.CampfireMark&&!mark.fired?6:mark.kind==LanItems.HeliMark&&mark.age>HeliArrive&&mark.age<HeliLeave?5:mark.kind==LanItems.FlareMark?LanNightRules.FlareRadius:mark.kind==LanItems.FireMark?mark.size+LanExplosives.FireLight:mark.kind==LanItems.StrikeMark?6:mark.kind==LanItems.ExplosionMark?LanExplosives.BlastLight:0;
    if(radius>0&&Flat(mark.position-at)<=radius)return true;
   }
   return false;
  }
  // ---------- Dibujo en cada pantalla ----------
  void UpdateMarks(){
   foreach(var key in markViews.Keys.ToArray())if(!Marks.Any(m=>m.id==key)){Destroy(markViews[key]);markViews.Remove(key);}
   int team=session.LocalSlot>=0?session.Slots[session.LocalSlot].team:-1;
   foreach(var mark in Marks){
    var go=View(mark);var view=go.GetComponent<LanMarkView>();
    bool everyone=mark.kind!=LanItems.AlarmMark&&mark.kind!=LanItems.TrapMark;
    go.SetActive(everyone||mark.team==team||mark.fired);
    if(mark.kind!=LanItems.GrenadeMark&&mark.kind!=LanItems.ChutoMark)go.transform.position=mark.position;
    if(Structure(mark.kind)&&mark.kind!=LanItems.WallMark&&mark.fired&&!view.Started)RefreshWall(mark);
    switch(mark.kind){
     case LanItems.AlarmMark:{var line=go.GetComponentInChildren<LineRenderer>();line.startColor=line.endColor=mark.fired&&Mathf.Repeat(Time.time,.5f)<.25f?Color.red:LanItems.Colors[LanItems.Alarm];break;}
     case LanItems.GrenadeMark:UpdateGrenade(mark,view);break;
     case LanItems.FireMark:if(view.Glow)view.Glow.intensity=2.5f+Mathf.PerlinNoise(Time.time*9,mark.id)*2.5f;break;
     case LanItems.StrikeMark:{
      float pulse=.5f+.5f*Mathf.Sin(Time.time*(mark.fired?18:8));view.Line.startColor=view.Line.endColor=new Color(1,.15f,.1f,.35f+.6f*pulse);
      if(mark.fired&&!view.Started){view.Started=true;if(assets.whistle)view.Sound.PlayOneShot(assets.whistle,.8f);}
      break;}
     case LanItems.WallMark:if(mark.fired&&!view.Started)RefreshWall(mark);break;
     case LanItems.ShieldMark:{float fade=Mathf.Clamp01(mark.time/1.2f)*(.55f+.25f*Mathf.Sin(Time.time*5));foreach(var line in view.Lines)line.startColor=line.endColor=new Color(.4f,.85f,1,fade);break;}
     case LanItems.SupplyMark:UpdateSupply(mark,view);break;
     case LanItems.MedStationMark:case LanItems.CampfireMark:case LanItems.HeliMark:UpdateSupport(mark,view,go);break;
     case LanItems.TrapMark:view.Ring.startColor=view.Ring.endColor=new Color(1,.3f,.2f,mark.age<LanAbilities.MineArm?.2f:.5f+.3f*Mathf.Sin(Time.time*4));break;
     case LanItems.ScanMark:{
      float wave=Mathf.Repeat(Time.time-view.Born,1.2f)/1.2f;view.Ring.transform.localScale=Vector3.one*(1+wave*5);
      view.Ring.startColor=view.Ring.endColor=new Color(1,.3f,.2f,1-wave);view.Line.widthMultiplier=.18f+.08f*Mathf.Sin(Time.time*6);
      break;}
    }
   }
  }
  GameObject View(LanMark mark){
   if(markViews.TryGetValue(mark.id,out var existing))return existing;
   var go=MakeMark(mark);var view=go.GetComponent<LanMarkView>();
   view.Id=mark.id;view.Born=Time.time-mark.age;markViews.Add(mark.id,go);return go;
  }
  GameObject MakeMark(LanMark mark){
   var go=new GameObject("Marca "+mark.id);go.transform.SetParent(transform);go.transform.position=mark.position;
   var view=go.AddComponent<LanMarkView>();
   switch(mark.kind){
    case LanItems.BlastMark:LanFx.Dust(mark.position,18);LanFx.Flash(mark.position+Vector3.up,Color.white,.15f);go.name="Aturdidora";return go;
    case LanItems.TrapMark:{
     go.name="Mina";var model=assets.mine?Instantiate(assets.mine,go.transform).transform:LanModels.Mine(go.transform);model.localPosition=Vector3.zero;
     view.Ring=LanPawn.Ring(go.transform,assets.glow,new Color(1,.3f,.2f,.5f),LanAbilities.MineRadius).GetComponent<LineRenderer>();view.Ring.widthMultiplier=.02f;return go;}
    case LanItems.ShieldMark:return MakeDome(mark,go,view);
    case LanItems.SupplyMark:return MakeSupply(mark,go,view);
    case LanItems.MedStationMark:case LanItems.CampfireMark:case LanItems.DroneStationMark:case LanItems.BarrageMark:case LanItems.HeliMark:return MakeSupport(mark,go,view);
    case LanItems.SmokeMark:go.name="Humo";LanFx.SmokeCloud(mark.position,mark.size,mark.time);return go;
    case LanItems.GrenadeMark:{
     go.name="Granada";view.Body=Primitive(PrimitiveType.Sphere,go.transform,Vector3.one*.16f,new Color(.22f,.27f,.16f)).transform;
     view.Glow=PointLight(go.transform,new Color(1,.15f,.1f),1.6f,2,Vector3.up*.2f);view.Sound=Sound(go,25);
     go.transform.position=mark.from;return go;}
    case LanItems.ExplosionMark:{
     go.name="Explosion";var local=session.LocalSlot>=0?Pawns[session.LocalSlot].transform.position:mark.position;
     LanFx.Explosion(mark.position,mark.size,local);view.Sound=Sound(go,70);if(assets.explosion)view.Sound.PlayOneShot(assets.explosion,1);
     return go;}
    case LanItems.FireMark:{
     go.name="Fuego";LanFx.Burn(mark.position,mark.size,mark.time);view.Glow=PointLight(go.transform,new Color(1,.5f,.15f),mark.size*3+3,3,Vector3.up*.8f);
     view.Sound=Sound(go,18);view.Sound.clip=assets.fireLoop;view.Sound.loop=true;view.Sound.volume=.45f;if(view.Sound.clip)view.Sound.Play();
     return go;}
    case LanItems.StrikeMark:{
     go.name="Bombardeo";LanFx.SmokeColumn(mark.position,LanAbilities.StrikeDelay+LanAbilities.StrikeShells*LanAbilities.StrikeInterval+1.5f,true);
     view.Glow=PointLight(go.transform,new Color(1,.2f,.15f),7,3.5f,Vector3.up*1.2f);
     view.Line=LanPawn.Ring(go.transform,assets.glow,new Color(1,.15f,.1f),mark.size).GetComponent<LineRenderer>();view.Line.widthMultiplier=.12f;
     view.Sound=Sound(go,60);if(assets.flare){view.Sound.clip=assets.flare;view.Sound.loop=true;view.Sound.volume=.4f;view.Sound.Play();}
     return go;}
    case LanItems.WallMark:return MakeWall(mark,go,view);
    case LanItems.ScanMark:{
     go.name="Rastreo";
     view.Line=go.AddComponent<LineRenderer>();view.Line.sharedMaterial=assets.glow;view.Line.useWorldSpace=false;view.Line.positionCount=2;view.Line.SetPosition(0,Vector3.zero);view.Line.SetPosition(1,Vector3.up*14);
     view.Line.startColor=new Color(1,.3f,.2f,.9f);view.Line.endColor=new Color(1,.3f,.2f,0);view.Line.widthMultiplier=.2f;
     view.Ring=LanPawn.Ring(go.transform,assets.glow,new Color(1,.3f,.2f),.5f).GetComponent<LineRenderer>();view.Ring.widthMultiplier=.06f;
     return go;}
   }
   // Bengala o alarma.
   bool flare=mark.kind==LanItems.FlareMark;var color=LanItems.Colors[flare?LanItems.Flare:LanItems.Alarm];go.name=flare?"Bengala":"Alarma";
   var core=Primitive(flare?PrimitiveType.Sphere:PrimitiveType.Cube,go.transform,Vector3.one*.22f,color);core.transform.localPosition=Vector3.up*.15f;
   LanPawn.Ring(go.transform,assets.glow,color,flare?.5f:.4f);
   if(flare){PointLight(go.transform,new Color(1,.45f,.25f),LanNightRules.FlareRadius+3,6,Vector3.up*1.6f);
    var hiss=Sound(go,25);hiss.clip=assets.flare;hiss.loop=true;hiss.volume=.5f;if(hiss.clip)hiss.Play();}
   return go;
  }
  // La granada vuela en arco hasta su punto, rebota un poco y parpadea cada vez más rápido.
  void UpdateGrenade(LanMark mark,LanMarkView view){
   float age=Time.time-view.Born,flight=LanExplosives.GrenadeFlight;
   var go=view.gameObject;
   if(age<flight){float t=age/flight;go.transform.position=Vector3.Lerp(mark.from,mark.position,t)+Vector3.up*2.2f*4*t*(1-t);}
   else{
    if(!view.Started){view.Started=true;if(assets.bounce)view.Sound.PlayOneShot(assets.bounce,.6f);LanFx.Dust(mark.position,3);}
    float bounce=Mathf.Max(0,.25f-(age-flight))*Mathf.Abs(Mathf.Sin((age-flight)*18))*.6f;go.transform.position=mark.position+Vector3.up*bounce;
   }
   view.Body.Rotate(400*Time.deltaTime,250*Time.deltaTime,0);
   float rate=Mathf.Lerp(3,14,age/LanExplosives.GrenadeFuse);view.Glow.enabled=Mathf.Repeat(age*rate,1)<.5f;
  }
  // Cúpula del Pesado: líneas brillantes en forma de media esfera.
  GameObject MakeDome(LanMark mark,GameObject go,LanMarkView view){
   go.name="Cupula";float r=mark.size;var lines=new System.Collections.Generic.List<LineRenderer>();
   LineRenderer Line(string name){var line=new GameObject(name).AddComponent<LineRenderer>();line.transform.SetParent(go.transform,false);line.useWorldSpace=false;line.sharedMaterial=assets.glow;line.widthMultiplier=.035f;lines.Add(line);return line;}
   for(int m=0;m<6;m++){var line=Line("Meridiano");line.positionCount=13;float yaw=m*30*Mathf.Deg2Rad;for(int i=0;i<13;i++){float a=i*Mathf.PI/12;line.SetPosition(i,new Vector3(Mathf.Cos(a)*Mathf.Cos(yaw)*r,Mathf.Sin(a)*r*.7f,Mathf.Cos(a)*Mathf.Sin(yaw)*r));}}
   foreach(float h in new[]{0f,.45f,.8f}){var line=Line("Paralelo");line.loop=true;line.positionCount=32;float ring=Mathf.Cos(Mathf.Asin(h))*r;for(int i=0;i<32;i++){float a=i*Mathf.PI*2/32;line.SetPosition(i,new Vector3(Mathf.Cos(a)*ring,h*r*.7f,Mathf.Sin(a)*ring));}}
   view.Lines=lines.ToArray();LanFx.Flash(mark.position+Vector3.up,new Color(.4f,.85f,1),.3f,6,4);LanFx.Dust(mark.position,12);
   return go;
  }
  // Suministro: la avioneta cruza a baja altura y la caja baja en paracaídas balanceándose.
  const float SupplyAltitude=8,PlanePass=1.5f,PlaneSpeed=30;
  GameObject MakeSupply(LanMark mark,GameObject go,LanMarkView view){
   go.name="Suministro";
   view.Body=assets.supply?Instantiate(assets.supply,go.transform).transform:LanModels.Supply(go.transform);
   view.Plane=assets.plane?Instantiate(assets.plane,transform).transform:LanModels.Plane(transform);view.Plane.localScale=Vector3.one*1.6f;view.Plane.name="Avioneta de suministro";
   view.Sound=Sound(view.Plane.gameObject,70);view.Sound.clip=assets.planeLoop;view.Sound.loop=true;view.Sound.volume=.8f;if(view.Sound.clip)view.Sound.Play();
   view.Glow=PointLight(view.Body,new Color(1,.3f,.2f),3,2,Vector3.up*.8f);
   return go;
  }
  void UpdateSupply(LanMark mark,LanMarkView view){
   float age=Time.time-view.Born;var heading=mark.from.sqrMagnitude>.01f?mark.from:Vector3.forward;
   if(view.Plane){
    view.Plane.position=mark.position+heading*PlaneSpeed*(age-PlanePass)+Vector3.up*SupplyAltitude;view.Plane.rotation=Quaternion.LookRotation(heading);
    var propeller=view.Plane.Find("Helice");if(propeller)propeller.Rotate(0,0,2000*Time.deltaTime,Space.Self);
    if(age>PlanePass+2.5f){Destroy(view.Plane.gameObject);view.Plane=null;}
   }
   float fall=Mathf.Clamp01((age-PlanePass)/(LanAbilities.SupplyTime-PlanePass));bool landed=mark.fired||fall>=1;
   view.Body.gameObject.SetActive(age>=PlanePass);
   float sway=landed?0:Mathf.Sin(age*2.2f)*8;
   view.Body.position=mark.position+Vector3.up*SupplyAltitude*(1-fall);view.Body.rotation=Quaternion.Euler(sway,age*20,sway*.5f);
   var chute=view.Body.Find("Paracaidas");
   if(chute)chute.localScale=landed?Vector3.MoveTowards(chute.localScale,new Vector3(1,.05f,1),Time.deltaTime*1.5f):Vector3.one;
   if(landed&&!view.Started){view.Started=true;LanFx.Dust(mark.position,22);if(view.Glow)view.Glow.intensity=1;}
   if(view.Glow)view.Glow.enabled=Mathf.Repeat(Time.time,1)<.5f;
  }
  GameObject MakeWall(LanMark mark,GameObject go,LanMarkView view){
   go.name="Muralla";go.transform.rotation=Quaternion.LookRotation(mark.from.sqrMagnitude>.01f?mark.from:Vector3.forward);
   float width=LanAbilities.WallWidth,height=LanAbilities.WallHeight,depth=LanAbilities.WallDepth;
   var box=go.AddComponent<BoxCollider>();box.size=new Vector3(width,height,depth);box.center=Vector3.up*height*.5f;
   var obstacle=go.AddComponent<NavMeshObstacle>();obstacle.shape=NavMeshObstacleShape.Box;obstacle.size=box.size;obstacle.center=box.center;obstacle.carving=true;obstacle.carveOnlyStationary=false;
   view.Body=new GameObject("Sacos").transform;view.Body.SetParent(go.transform,false);
   // Tres hileras de sacos, la de arriba más corta.
   int[] rows={4,4,3};float rowHeight=height/rows.Length;
   for(int r=0;r<rows.Length;r++)for(int i=0;i<rows[r];i++){
    float x=(i-(rows[r]-1)*.5f)*(width/4)+(r%2==1?.12f:0);
    var at=new Vector3(x,r*rowHeight,0);var size=new Vector3(width/4*1.05f,rowHeight*1.15f,depth);
    if(assets.sandbag){var sack=Instantiate(assets.sandbag,view.Body);Fit(sack.transform,at,size,(r*7+i)*23);}
    else{var block=Primitive(PrimitiveType.Cube,view.Body,size,new Color(.55f,.48f,.33f));block.transform.localPosition=at+Vector3.up*size.y*.5f;}
   }
   LanFx.Dust(mark.position,16);view.Sound=Sound(go,25);if(assets.build)view.Sound.PlayOneShot(assets.build,.7f);
   return go;
  }
  // Ajusta un modelo al tamaño de su hueco, apoyado en la base.
  static void Fit(Transform model,Vector3 at,Vector3 size,float yaw){
   model.localRotation=Quaternion.Euler(0,yaw%20-10,0);model.localPosition=Vector3.zero;model.localScale=Vector3.one;
   var renderers=model.GetComponentsInChildren<Renderer>();if(renderers.Length==0)return;
   var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
   var local=model.parent.InverseTransformVector(bounds.size);local=new Vector3(Mathf.Abs(local.x),Mathf.Abs(local.y),Mathf.Abs(local.z));
   model.localScale=new Vector3(size.x/Mathf.Max(.01f,local.x),size.y/Mathf.Max(.01f,local.y),size.z/Mathf.Max(.01f,local.z));
   model.localPosition=at;
  }
  // Una muralla destruida pierde su colisión al instante y sus sacos salen volando.
  void RefreshWall(LanMark mark){
   if(!markViews.TryGetValue(mark.id,out var go))return;var view=go.GetComponent<LanMarkView>();
   if(!mark.fired||view.Started)return;view.Started=true;
   foreach(var c in go.GetComponents<Collider>())c.enabled=false;var obstacle=go.GetComponent<NavMeshObstacle>();if(obstacle)obstacle.enabled=false;
   LanDebris.Scatter(view.Body,mark.position,4);LanFx.Dust(mark.position+Vector3.up*.4f,24);
   if(assets.crash){if(!view.Sound)view.Sound=Sound(go,25);view.Sound.PlayOneShot(assets.crash,.8f);}
  }
  static GameObject Primitive(PrimitiveType type,Transform parent,Vector3 scale,Color color){
   var go=GameObject.CreatePrimitive(type);DestroyImmediate(go.GetComponent<Collider>());go.transform.SetParent(parent,false);go.transform.localScale=scale;go.GetComponent<Renderer>().material.color=color;return go;
  }
  static Light PointLight(Transform parent,Color color,float range,float intensity,Vector3 at){
   var light=new GameObject("Luz").AddComponent<Light>();light.transform.SetParent(parent,false);light.transform.localPosition=at;
   light.type=LightType.Point;light.range=range;light.intensity=intensity;light.color=color;light.shadows=LightShadows.None;return light;
  }
  static AudioSource Sound(GameObject go,float range){
   var source=go.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=1;source.minDistance=Mathf.Min(6,range*.2f);source.maxDistance=range;source.rolloffMode=AudioRolloffMode.Linear;return source;
  }
 }
 public sealed class LanMarkView:MonoBehaviour{public int Id,Shots;public float Born,Flash,Spent;public bool Started;public Transform Body,Plane;public Light Glow;public LineRenderer Line,Ring;public LineRenderer[] Lines;public AudioSource Sound;void OnDestroy(){if(Plane)Destroy(Plane.gameObject);}}
}
