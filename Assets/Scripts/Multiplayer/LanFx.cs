using System.Collections.Generic;
using UnityEngine;
namespace Valle.Lan {
 // Efectos de combate sin magia: polvo, chispas y fogonazos con un destello de luz.
 // Tres sistemas de partículas por escena emiten todas las ráfagas; las luces se reutilizan.
 public static class LanFx {
  const int MaxLights=6;
  static GameObject root;static ParticleSystem dust,sparks,flashes;
  static readonly List<Light> lights=new List<Light>();static readonly List<float> lightEnds=new List<float>();
  public static int Particles=>Ready()?dust.particleCount+sparks.particleCount+flashes.particleCount:0;
  public static int ActiveLights{get{int count=0;foreach(var light in lights)if(light&&light.enabled)count++;return count;}}
  public static void Dust(Vector3 at,int count){if(Ready())Emit(dust,at,count);}
  public static void Impact(Vector3 at){if(!Ready())return;Emit(sparks,at,8);Emit(dust,at,3);}
  public static void Muzzle(Vector3 at){if(!Ready())return;Emit(flashes,at,2);Flash(at,new Color(1,.8f,.45f),.06f);}
  public static void Flash(Vector3 at,Color color,float seconds=.08f){
   if(!Ready())return;
   int index=-1;float oldest=float.MaxValue;
   for(int i=0;i<lights.Count;i++){if(!lights[i].enabled){index=i;break;}if(lightEnds[i]<oldest){oldest=lightEnds[i];index=i;}}
   var light=lights[index];light.transform.position=at;light.color=color;light.enabled=true;lightEnds[index]=Time.time+seconds;
  }
  internal static void Tick(){for(int i=0;i<lights.Count;i++)if(lights[i].enabled&&Time.time>=lightEnds[i])lights[i].enabled=false;}
  static void Emit(ParticleSystem system,Vector3 at,int count)=>system.Emit(new ParticleSystem.EmitParams{position=at,applyShapeToPosition=true},count);
  static bool Ready(){
   if(root)return true;
   var assets=Resources.Load<LanAssets>("Multiplayer/LanAssets");
   if(!assets||!assets.fxAlpha||!assets.fxAdditive)return false;
   root=new GameObject("Efectos de combate");root.AddComponent<LanFxClock>();
   dust=Make("Polvo",assets.fxAlpha,.5f,.95f,.5f,1.6f,.14f,.32f,new Color(.58f,.52f,.43f,.75f),new Color(.4f,.37f,.32f,.6f),.12f,.15f,true);
   sparks=Make("Chispas",assets.fxAdditive,.15f,.35f,2,4.5f,.03f,.07f,new Color(1,.88f,.45f),new Color(1,.6f,.2f),1.2f,.05f,false);
   flashes=Make("Fogonazos",assets.fxAdditive,.05f,.08f,0,.2f,.25f,.4f,new Color(1,.92f,.6f),new Color(1,.7f,.3f),0,.03f,false);
   lights.Clear();lightEnds.Clear();
   for(int i=0;i<MaxLights;i++){var light=new GameObject("Destello").AddComponent<Light>();light.transform.SetParent(root.transform,false);light.type=LightType.Point;light.range=3.5f;light.intensity=5;light.shadows=LightShadows.None;light.enabled=false;lights.Add(light);lightEnds.Add(0);}
   return true;
  }
  static ParticleSystem Make(string name,Material material,float lifeMin,float lifeMax,float speedMin,float speedMax,float sizeMin,float sizeMax,Color a,Color b,float gravity,float radius,bool grows){
   var go=new GameObject(name);go.transform.SetParent(root.transform,false);
   var system=go.AddComponent<ParticleSystem>();system.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
   var main=system.main;main.loop=true;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=300;
   main.startLifetime=new ParticleSystem.MinMaxCurve(lifeMin,lifeMax);main.startSpeed=new ParticleSystem.MinMaxCurve(speedMin,speedMax);main.startSize=new ParticleSystem.MinMaxCurve(sizeMin,sizeMax);
   main.startColor=new ParticleSystem.MinMaxGradient(a,b);main.gravityModifier=gravity;
   var emission=system.emission;emission.enabled=false;
   var shape=system.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=radius;
   var fade=system.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();
   gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});fade.color=gradient;
   var size=system.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,grows?AnimationCurve.Linear(0,.6f,1,1.5f):AnimationCurve.Linear(0,1,1,.3f));
   go.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;
   system.Play();
   return system;
  }
 }
 sealed class LanFxClock:MonoBehaviour{void Update()=>LanFx.Tick();}
}
