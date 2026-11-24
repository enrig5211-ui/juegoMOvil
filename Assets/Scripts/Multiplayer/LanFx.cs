using System.Collections.Generic;
using UnityEngine;
namespace Valle.Lan {
 // Efectos de combate sin magia: polvo, chispas, fogonazos, explosiones, humo, llamas y quemaduras.
 // Pocos sistemas de partículas por escena emiten todas las ráfagas; las luces se reutilizan.
 public static class LanFx {
  const int MaxLights=8;
  static GameObject root;static ParticleSystem dust,sparks,flashes,fireballs,flames,smoke,scorch;static LanAssets art;
  static readonly List<Light> lights=new List<Light>();static readonly List<float> lightEnds=new List<float>(),lightStarts=new List<float>(),lightPeaks=new List<float>();
  // Fuegos y columnas de humo que siguen emitiendo un tiempo.
  sealed class Burner{public Vector3 At;public float Radius,Until,Rate,Carry;public bool Smoke,Red,Cloud;}
  static readonly List<Burner> burners=new List<Burner>();
  public static float Shake;
  public static int Particles=>Ready()?dust.particleCount+sparks.particleCount+flashes.particleCount+fireballs.particleCount+flames.particleCount+smoke.particleCount:0;
  public static int Scorches=>Ready()?scorch.particleCount:0;
  public static int Burning=>burners.Count;
  public static int ActiveLights{get{int count=0;foreach(var light in lights)if(light&&light.enabled)count++;return count;}}
  public static void Dust(Vector3 at,int count){if(Ready())Emit(dust,at,count);}
  public static void Impact(Vector3 at){if(!Ready())return;Emit(sparks,at,8);Emit(dust,at,3);}
  public static void Muzzle(Vector3 at){if(!Ready())return;Emit(flashes,at,2);Flash(at,new Color(1,.8f,.45f),.06f);}
  // Explosión: destello, bola de fuego, chispas, polvo, humo, quemadura en el suelo y sacudida si está cerca.
  public static void Explosion(Vector3 at,float radius,Vector3 viewer){
   if(!Ready())return;
   float scale=Mathf.Clamp(radius/3.5f,.5f,1.6f);
   Flash(at+Vector3.up*1.2f,new Color(1,.62f,.3f),.35f,radius*3.2f,9);
   for(int i=0;i<Mathf.RoundToInt(26*scale);i++)Emit(fireballs,at+Vector3.up*.3f+Random.insideUnitSphere*radius*.3f,1);
   Emit(sparks,at+Vector3.up*.3f,Mathf.RoundToInt(30*scale));Emit(dust,at,Mathf.RoundToInt(22*scale));
   for(int i=0;i<Mathf.RoundToInt(14*scale);i++)Emit(smoke,at+Vector3.up*.4f+Random.insideUnitSphere*radius*.35f,1);
   scorch.Emit(new ParticleSystem.EmitParams{position=at+Vector3.up*.03f,startSize=radius*1.5f,rotation=Random.Range(0,360f),applyShapeToPosition=false},1);
   if(art&&art.fireBlast)Spawn(art.fireBlast,at,Vector3.one*scale,3);
   float distance=Vector3.Distance(Flat(viewer),Flat(at));Shake=Mathf.Max(Shake,Mathf.Clamp01(1-distance/(radius*5))*.45f*scale);
  }
  // Fuego que sigue ardiendo (granadas, bombardeo, árboles quemados). Ilumina y echa humo.
  public static void Burn(Vector3 at,float radius,float seconds){
   if(!Ready())return;
   burners.Add(new Burner{At=at,Radius=radius,Until=Time.time+seconds,Rate=18*radius});
   if(art&&art.fireArea)Spawn(art.fireArea,at,Vector3.one*radius*.55f,seconds);
  }
  // Columna de humo de color (el humo rojo marca un bombardeo).
  public static void SmokeColumn(Vector3 at,float seconds,bool red){if(Ready())burners.Add(new Burner{At=at,Radius=.3f,Until=Time.time+seconds,Rate=14,Smoke=true,Red=red});}
  // Nube densa de humo gris (bomba de humo del Contrabandista).
  public static void SmokeCloud(Vector3 at,float radius,float seconds){if(!Ready())return;burners.Add(new Burner{At=at,Radius=radius*.85f,Until=Time.time+seconds-1.5f,Rate=34*radius,Smoke=true,Cloud=true});Dust(at,24);}
  public static void Flash(Vector3 at,Color color,float seconds=.08f,float range=3.5f,float intensity=5){
   if(!Ready())return;
   int index=-1;float oldest=float.MaxValue;
   for(int i=0;i<lights.Count;i++){if(!lights[i].enabled){index=i;break;}if(lightEnds[i]<oldest){oldest=lightEnds[i];index=i;}}
   var light=lights[index];light.transform.position=at;light.color=color;light.range=range;light.intensity=intensity;light.enabled=true;
   lightEnds[index]=Time.time+seconds;lightStarts[index]=Time.time;lightPeaks[index]=intensity;
  }
  internal static void Tick(){
   float now=Time.time;
   for(int i=0;i<lights.Count;i++){
    if(!lights[i].enabled)continue;
    if(now>=lightEnds[i]){lights[i].enabled=false;continue;}
    float span=Mathf.Max(.01f,lightEnds[i]-lightStarts[i]);lights[i].intensity=lightPeaks[i]*Mathf.Clamp01((lightEnds[i]-now)/span*1.5f);
   }
   for(int i=burners.Count-1;i>=0;i--){
    var b=burners[i];if(now>=b.Until){burners.RemoveAt(i);continue;}
    b.Carry+=b.Rate*Time.deltaTime;int count=(int)b.Carry;b.Carry-=count;
    for(int k=0;k<count;k++){
     var offset=Random.insideUnitCircle*b.Radius;var at=b.At+new Vector3(offset.x,.05f,offset.y);
     if(b.Cloud){smoke.Emit(new ParticleSystem.EmitParams{position=at+Vector3.up*Random.Range(.2f,1.2f),startColor=new Color(.55f,.56f,.58f,.85f),startSize=Random.Range(1.3f,2.2f),startLifetime=Random.Range(2,3.2f),applyShapeToPosition=true},1);continue;}
     if(b.Smoke){smoke.Emit(new ParticleSystem.EmitParams{position=at+Vector3.up*.3f,startColor=b.Red?new Color(.85f,.18f,.12f,.75f):new Color(.2f,.19f,.18f,.6f),applyShapeToPosition=true},1);continue;}
     Emit(flames,at,1);if(Random.value<.25f)Emit(smoke,at+Vector3.up*.8f,1);
    }
   }
   Shake=Mathf.MoveTowards(Shake,0,Time.deltaTime*1.2f);
  }
  static void Spawn(GameObject prefab,Vector3 at,Vector3 scale,float seconds){
   var go=Object.Instantiate(prefab,at,Quaternion.identity,root.transform);go.transform.localScale=scale;
   foreach(var behaviour in go.GetComponentsInChildren<MonoBehaviour>(true))behaviour.enabled=false;
   foreach(var system in go.GetComponentsInChildren<ParticleSystem>(true)){system.gameObject.SetActive(true);var main=system.main;main.loop=false;main.scalingMode=ParticleSystemScalingMode.Hierarchy;system.Play();}
   Object.Destroy(go,seconds+2);
  }
  static Vector3 Flat(Vector3 v){v.y=0;return v;}
  static void Emit(ParticleSystem system,Vector3 at,int count)=>system.Emit(new ParticleSystem.EmitParams{position=at,applyShapeToPosition=true},count);
  static bool Ready(){
   if(root)return true;
   art=Resources.Load<LanAssets>("Multiplayer/LanAssets");
   if(!art||!art.fxAlpha||!art.fxAdditive)return false;
   root=new GameObject("Efectos de combate");root.AddComponent<LanFxClock>();burners.Clear();
   dust=Make("Polvo",art.fxAlpha,.5f,.95f,.5f,1.6f,.14f,.32f,new Color(.58f,.52f,.43f,.75f),new Color(.4f,.37f,.32f,.6f),.12f,.15f,1);
   sparks=Make("Chispas",art.fxAdditive,.15f,.35f,2,4.5f,.03f,.07f,new Color(1,.88f,.45f),new Color(1,.6f,.2f),1.2f,.05f,0);
   flashes=Make("Fogonazos",art.fxAdditive,.05f,.08f,0,.2f,.25f,.4f,new Color(1,.92f,.6f),new Color(1,.7f,.3f),0,.03f,0);
   fireballs=Make("Bola de fuego",art.fxAdditive,.25f,.55f,1.5f,5,.7f,1.5f,new Color(1,.9f,.55f),new Color(1,.45f,.12f),-.2f,.2f,0);
   flames=Make("Llamas",art.fxAdditive,.35f,.8f,.4f,1.3f,.25f,.55f,new Color(1,.78f,.32f),new Color(1,.38f,.08f),-.35f,.05f,0,true);
   smoke=Make("Humo",art.fxAlpha,1.6f,3.2f,.3f,1,.5f,1.1f,new Color(.16f,.15f,.14f,.65f),new Color(.3f,.29f,.28f,.45f),-.12f,.2f,2,true);
   scorch=Make("Quemaduras",art.fxAlpha,40,40,0,0,1,1,new Color(.03f,.025f,.02f,.8f),new Color(.05f,.04f,.03f,.7f),0,0,-1);
   var flat=scorch.GetComponent<ParticleSystemRenderer>();flat.renderMode=ParticleSystemRenderMode.HorizontalBillboard;flat.sortingFudge=50;var sMain=scorch.main;sMain.maxParticles=48;
   lights.Clear();lightEnds.Clear();lightStarts.Clear();lightPeaks.Clear();
   for(int i=0;i<MaxLights;i++){var light=new GameObject("Destello").AddComponent<Light>();light.transform.SetParent(root.transform,false);light.type=LightType.Point;light.range=3.5f;light.intensity=5;light.shadows=LightShadows.None;light.enabled=false;lights.Add(light);lightEnds.Add(0);lightStarts.Add(0);lightPeaks.Add(0);}
   return true;
  }
  // growth: 1 crece (polvo), 2 crece mucho (humo), 0 se encoge, -1 fijo (quemaduras que se desvanecen al final).
  static ParticleSystem Make(string name,Material material,float lifeMin,float lifeMax,float speedMin,float speedMax,float sizeMin,float sizeMax,Color a,Color b,float gravity,float radius,int growth,bool upward=false){
   var go=new GameObject(name);go.transform.SetParent(root.transform,false);
   var system=go.AddComponent<ParticleSystem>();system.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
   var main=system.main;main.loop=true;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=growth==2?1200:upward?600:300;
   main.startLifetime=new ParticleSystem.MinMaxCurve(lifeMin,lifeMax);main.startSpeed=new ParticleSystem.MinMaxCurve(speedMin,speedMax);main.startSize=new ParticleSystem.MinMaxCurve(sizeMin,sizeMax);
   main.startColor=new ParticleSystem.MinMaxGradient(a,b);main.gravityModifier=gravity;main.startRotation=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);
   var emission=system.emission;emission.enabled=false;
   var shape=system.shape;shape.enabled=radius>0;shape.shapeType=upward?ParticleSystemShapeType.Hemisphere:ParticleSystemShapeType.Sphere;shape.radius=Mathf.Max(.01f,radius);if(upward)shape.rotation=new Vector3(-90,0,0);
   var fade=system.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();
   gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},growth==-1?new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(1,.8f),new GradientAlphaKey(0,1)}:new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});fade.color=gradient;
   var size=system.sizeOverLifetime;size.enabled=growth!=-1;
   size.size=new ParticleSystem.MinMaxCurve(1,growth==2?AnimationCurve.Linear(0,.5f,1,2.4f):growth==1?AnimationCurve.Linear(0,.6f,1,1.5f):AnimationCurve.Linear(0,1,1,.3f));
   go.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;
   system.Play();
   return system;
  }
 }
 sealed class LanFxClock:MonoBehaviour{void Update()=>LanFx.Tick();}
 // Sacudida de cámara tras las explosiones cercanas. Corre después de la cámara del jugador.
 [DefaultExecutionOrder(1000)]
 public sealed class LanShake:MonoBehaviour{
  void LateUpdate(){if(LanFx.Shake<=0)return;float s=LanFx.Shake;transform.position+=new Vector3(Mathf.PerlinNoise(Time.time*30,1)-.5f,Mathf.PerlinNoise(1,Time.time*30)-.5f,Mathf.PerlinNoise(Time.time*30,7)-.5f)*s;}
 }
}
