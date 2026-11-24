using System.Collections.Generic;
using UnityEngine;
namespace Valle.Lan {
 public enum LanPropKind{Tree,Bush,Rock,Crate,Barrel,Tent,Lamp,Forest,Plane}
 // Objeto destructible del mapa. Lo crea Tools > Multijugador > Crear mapa del Chapare:
 // la colisión está en este objeto y el modelo visible en "visual".
 public sealed class LanProp:MonoBehaviour {
  public LanPropKind kind;public Transform visual;public GameObject source;
  public int Index{get;set;}public bool Broken{get;set;}
  public static float Health(LanPropKind kind){switch(kind){case LanPropKind.Tree:return 60;case LanPropKind.Bush:return 35;case LanPropKind.Rock:return 150;case LanPropKind.Crate:return 45;case LanPropKind.Barrel:return 25;case LanPropKind.Tent:return 50;case LanPropKind.Lamp:return 30;case LanPropKind.Plane:return 200;default:return 220;}}
  // Cajas, barriles, tiendas y faroles también se rompen a balazos (apagar un farol a tiros oscurece la zona).
  public bool Shootable=>kind==LanPropKind.Crate||kind==LanPropKind.Barrel||kind==LanPropKind.Lamp||kind==LanPropKind.Tent||kind==LanPropKind.Plane;
 }
 // Entorno destructible: el anfitrión lleva la vida de cada objeto y envía la lista de rotos.
 public sealed partial class LanWorld {
  public LanProp[] props;
  public readonly List<int> Broken=new List<int>();public bool BrokenDirty;
  float[] propHealth;bool brokenSynced;readonly Collider[] overlap=new Collider[192];
  void InitProps(){
   if(props==null)props=new LanProp[0];propHealth=new float[props.Length];
   for(int i=0;i<props.Length;i++){if(!props[i])continue;props[i].Index=i;propHealth[i]=LanProp.Health(props[i].kind);}
  }
  public void DamageProps(Vector3 at,float radius,float damage){
   if(!session.IsHost||props.Length==0)return;
   int count=Physics.OverlapSphereNonAlloc(at+Vector3.up*.5f,radius,overlap,Physics.AllLayers,QueryTriggerInteraction.Ignore);
   var hits=new List<(LanProp prop,float distance)>();
   for(int i=0;i<count;i++){var prop=overlap[i].GetComponent<LanProp>();if(prop&&!prop.Broken)hits.Add((prop,Flat(overlap[i].ClosestPoint(at)-at)));}
   foreach(var (prop,distance) in hits)DamageProp(prop,Mathf.Lerp(damage,damage*.4f,Mathf.Clamp01(distance/radius)),at);
  }
  // Bala contra un objeto del mapa: true si chocó con uno.
  bool HitProp(Collider collider,float damage,Vector3 from){
   var prop=collider.GetComponent<LanProp>();if(!prop)return false;
   if(prop.Shootable)DamageProp(prop,damage,from);return true;
  }
  void DamageProp(LanProp prop,float damage,Vector3 from){
   if(prop.Broken||prop.Index>=propHealth.Length)return;
   propHealth[prop.Index]-=damage;if(propHealth[prop.Index]<=0)BreakProp(prop,from,true);
  }
  void BreakProp(LanProp prop,Vector3 from,bool animate){
   if(prop.Broken)return;prop.Broken=true;
   foreach(var c in prop.GetComponents<Collider>())c.enabled=false;
   var at=prop.visual?prop.visual.position:prop.transform.position;
   LanDebris.Break(prop,from,animate);
   if(animate&&assets.crash&&prop.kind!=LanPropKind.Barrel)AudioSource.PlayClipAtPoint(assets.crash,at,.5f);
   if(!session.IsHost)return;
   Broken.Add(prop.Index);BrokenDirty=true;
   // Los barriles explotan y pueden encadenar otros.
   if(prop.kind==LanPropKind.Plane)Explode(new Vector3(at.x,.05f,at.z),-1,-1,4.5f,90,2,2.2f,10);
   if(prop.kind==LanPropKind.Barrel)Explode(new Vector3(at.x,.05f,at.z),-1,-1,LanExplosives.BarrelRadius,LanExplosives.BarrelDamage,1.5f,LanExplosives.BarrelFire,LanExplosives.BarrelBurn);
  }
  // Clientes: la primera lista llega sin animación (lo que ya estaba roto al entrar).
  void ApplyBroken(int[] list){
   if(list!=null)foreach(int id in list)if(id>=0&&id<props.Length&&props[id]&&!props[id].Broken)BreakProp(props[id],props[id].transform.position+Random.insideUnitSphere,brokenSynced);
   brokenSynced=true;
  }
 }
 // Restos: árboles que caen quemados, rocas partidas, tablas y sacos que salen volando.
 public static class LanDebris {
  static readonly Color Charred=new Color(.3f,.26f,.22f),Wood=new Color(.5f,.36f,.22f),Canvas=new Color(.72f,.65f,.5f),Stump=new Color(.16f,.12f,.09f);
  static MaterialPropertyBlock block;
  public static int Pieces{get;private set;}
  public static void Break(LanProp prop,Vector3 from,bool animate){
   var visual=prop.visual;if(!visual)return;
   var at=visual.position;var rotation=visual.rotation;var scale=visual.lossyScale;
   visual.gameObject.SetActive(false);
   var away=at-from;away.y=0;if(away.sqrMagnitude<.01f){var r=Random.insideUnitCircle;away=new Vector3(r.x,0,r.y);}away.Normalize();
   bool wooded=prop.kind==LanPropKind.Tree||prop.kind==LanPropKind.Bush||prop.kind==LanPropKind.Forest;
   if(wooded)Stumps(at,prop.kind==LanPropKind.Forest?3:1,scale.x);
   // La avioneta queda como chatarra quemada.
   if(prop.kind==LanPropKind.Plane){if(prop.source){var wreck=Object.Instantiate(prop.source,at,rotation);wreck.transform.localScale=scale*.95f;wreck.transform.Rotate(8,0,-6);Tint(wreck,Charred);}if(animate)LanFx.Burn(at,1.6f,10);return;}
   if(!animate)return;
   switch(prop.kind){
    case LanPropKind.Tree:case LanPropKind.Bush:case LanPropKind.Forest:case LanPropKind.Lamp:
     Fall(prop.source,at,rotation,scale,away,prop.kind==LanPropKind.Forest?55:prop.kind==LanPropKind.Lamp?88:80,prop.kind!=LanPropKind.Lamp);
     LanFx.Dust(at+Vector3.up*.3f,prop.kind==LanPropKind.Forest?26:12);
     if(wooded)LanFx.Burn(at,prop.kind==LanPropKind.Forest?.9f:.45f,prop.kind==LanPropKind.Forest?7:4);
     else{LanFx.Impact(at+Vector3.up*1.4f);LanFx.Flash(at+Vector3.up*1.4f,new Color(1,.8f,.5f),.1f);}
     break;
    case LanPropKind.Rock:
     for(int i=0;i<5;i++)if(prop.source)Throw(Object.Instantiate(prop.source),at+Vector3.up*.2f,scale*.35f,away,5,Charred,false);
     LanFx.Dust(at,20);break;
    default:
     var color=prop.kind==LanPropKind.Tent?Canvas:Wood;
     for(int i=0;i<7;i++){var plank=GameObject.CreatePrimitive(PrimitiveType.Cube);Object.DestroyImmediate(plank.GetComponent<Collider>());Throw(plank,at+Vector3.up*.35f,new Vector3(.45f,.05f,.12f)*Random.Range(.7f,1.3f),away,6,color,true);}
     LanFx.Dust(at,16);break;
   }
  }
  // Cada hijo del objeto sale volando (sacos de una muralla).
  public static void Scatter(Transform group,Vector3 from,float force){
   if(!group)return;
   foreach(Transform child in new List<Transform>(GetChildren(group))){
    var away=child.position-from;away.y=0;if(away.sqrMagnitude<.01f)away=Random.insideUnitSphere;away.y=0;away.Normalize();
    child.SetParent(null,true);var piece=child.gameObject.AddComponent<LanPiece>();piece.Launch(away*Random.Range(force*.5f,force)+Vector3.up*Random.Range(2,4.5f));Pieces++;
   }
  }
  static IEnumerable<Transform> GetChildren(Transform t){foreach(Transform c in t)yield return c;}
  static void Throw(GameObject go,Vector3 at,Vector3 scale,Vector3 away,float force,Color color,bool tint){
   go.transform.position=at+Random.insideUnitSphere*.25f;go.transform.rotation=Random.rotation;go.transform.localScale=scale;
   foreach(var c in go.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
   if(tint)Tint(go,color);
   var spread=Quaternion.Euler(0,Random.Range(-70f,70f),0)*away;
   go.AddComponent<LanPiece>().Launch(spread*Random.Range(force*.4f,force)+Vector3.up*Random.Range(3,6.5f));Pieces++;
  }
  static void Fall(GameObject source,Vector3 at,Quaternion rotation,Vector3 scale,Vector3 away,float angle,bool charred){
   if(!source)return;
   var go=Object.Instantiate(source,at,rotation);go.transform.localScale=scale;
   foreach(var c in go.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
   foreach(var l in go.GetComponentsInChildren<Light>())l.enabled=false;
   if(charred)Tint(go,Charred);
   go.AddComponent<LanFalling>().Begin(Vector3.Cross(Vector3.up,away),angle);
  }
  static void Stumps(Vector3 at,int count,float scale){
   for(int i=0;i<count;i++){
    var stump=GameObject.CreatePrimitive(PrimitiveType.Cylinder);Object.DestroyImmediate(stump.GetComponent<Collider>());stump.name="Tocon";
    var offset=count==1?Vector3.zero:Quaternion.Euler(0,i*120+Random.Range(-20f,20f),0)*Vector3.forward*.5f;
    float s=Mathf.Clamp(scale,.6f,1.4f);stump.transform.position=at+offset+Vector3.up*.08f*s;stump.transform.localScale=new Vector3(.2f,.08f,.2f)*s;
    stump.GetComponent<Renderer>().material.color=Stump;
   }
  }
  static void Tint(GameObject go,Color color){
   if(block==null)block=new MaterialPropertyBlock();
   block.Clear();block.SetColor("_BaseColor",color);block.SetColor("_Color",color);
   foreach(var r in go.GetComponentsInChildren<Renderer>())r.SetPropertyBlock(block);
  }
 }
 // Árbol o farol que cae girando sobre su base, queda en el suelo y luego se hunde.
 public sealed class LanFalling:MonoBehaviour {
  Vector3 axis;float angle,t;Quaternion start;Vector3 origin;
  public void Begin(Vector3 fallAxis,float degrees){axis=fallAxis;angle=degrees;start=transform.rotation;origin=transform.position;}
  void Update(){
   t+=Time.deltaTime;
   float fall=Mathf.Clamp01(t/.9f);transform.rotation=Quaternion.AngleAxis(angle*fall*fall,axis)*start;
   if(t>3.5f)transform.position=origin+Vector3.down*(t-3.5f)*.8f;
   if(t>5.5f)Destroy(gameObject);
  }
 }
 // Pedazo con caída simple (sin física): sale volando, rebota una vez y se encoge.
 public sealed class LanPiece:MonoBehaviour {
  Vector3 velocity,spin;float life;bool rested;Vector3 size;
  public void Launch(Vector3 v){velocity=v;spin=Random.insideUnitSphere*540;size=transform.localScale;}
  void Update(){
   float dt=Time.deltaTime;life+=dt;
   if(!rested){
    velocity+=Vector3.down*18*dt;transform.position+=velocity*dt;transform.Rotate(spin*dt,Space.World);
    if(transform.position.y<=.04f&&velocity.y<0){var p=transform.position;p.y=.04f;transform.position=p;if(velocity.y<-3){velocity=new Vector3(velocity.x*.4f,-velocity.y*.3f,velocity.z*.4f);spin*=.4f;}else rested=true;}
   }
   if(life>4)transform.localScale=size*Mathf.Clamp01(1-(life-4)/.6f);
   if(life>4.6f)Destroy(gameObject);
  }
 }
}
