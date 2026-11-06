using System.Collections.Generic;
using UnityEngine;
namespace Valle {
 // Bounded reusable visual effects. No damage or authoritative gameplay lives here.
 public sealed class MagicFx:MonoBehaviour {
  static readonly List<MagicFx> pool=new List<MagicFx>();
  static MagicPalette palette;public static int ActiveCount{get{pool.RemoveAll(p=>!p);return pool.FindAll(p=>p.gameObject.activeSelf).Count;}}
  const int MaxInstances=24;
  GameObject prefab;ParticleSystem[] particles;float stopAt,releaseAt;Transform follow;Vector3 offset;bool stopped;
  static MagicPalette Palette=>palette?palette:(palette=Resources.Load<MagicPalette>("Effects/MagicPalette"));
  public static void Spawn(Vector3 at,int role){if(!Palette)return;Emit(role==1?Palette.earthSpawn:role==2?Palette.fireSpawn:Palette.windSpawn,at,.38f,.65f,null);}
  public static void Impact(Vector3 at){if(Palette)Emit(Palette.impact,at,.18f,.15f,null);}
  public static MagicFx Protect(Transform actor,float duration){return Palette?Emit(Palette.protection,actor.position+Vector3.up*.3f,.38f,duration,actor):null;}
  static MagicFx Emit(GameObject source,Vector3 at,float scale,float duration,Transform target){if(!source)return null;pool.RemoveAll(p=>!p);MagicFx fx=pool.Find(p=>p.prefab==source&&!p.gameObject.activeSelf);
   if(!fx){if(pool.Count>=MaxInstances){var old=pool.Find(p=>!p.gameObject.activeSelf);if(!old){float oldest=float.MaxValue;foreach(var candidate in pool)if(candidate.releaseAt<oldest){old=candidate;oldest=candidate.releaseAt;}}pool.Remove(old);Destroy(old.gameObject);}
    var go=Instantiate(source);go.name="Magia - "+source.name;fx=go.AddComponent<MagicFx>();fx.prefab=source;fx.particles=go.GetComponentsInChildren<ParticleSystem>(true);pool.Add(fx);}
   fx.transform.SetPositionAndRotation(at,Quaternion.identity);fx.transform.localScale=Vector3.one*scale;fx.follow=target;fx.offset=target?at-target.position:Vector3.zero;fx.stopped=false;fx.stopAt=Time.time+duration;fx.releaseAt=fx.stopAt+1.2f;fx.gameObject.SetActive(true);
   foreach(var p in fx.particles){p.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);p.Play(false);}return fx;
  }
  public void StopEffect(){stopAt=releaseAt=Time.time;follow=null;}
  void Update(){if(follow)transform.position=follow.position+offset;if(!stopped&&Time.time>=stopAt){stopped=true;foreach(var p in particles)p.Stop(false,ParticleSystemStopBehavior.StopEmitting);}if(Time.time>=releaseAt)gameObject.SetActive(false);}
  void OnDestroy(){pool.Remove(this);}
 }
}
