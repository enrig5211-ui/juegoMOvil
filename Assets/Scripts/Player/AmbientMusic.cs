using UnityEngine;
namespace Valle {
 [RequireComponent(typeof(AudioSource))]
 public sealed class AmbientMusic:MonoBehaviour {
  [Range(0,1)] public float volume=.12f;
  [Min(.1f)] public float fadeSeconds=3;
  AudioSource source;float elapsed;
  void OnEnable(){source=GetComponent<AudioSource>();source.loop=true;source.spatialBlend=0;source.volume=0;elapsed=0;if(source.clip)source.Play();}
  void Update(){elapsed+=Time.unscaledDeltaTime;source.volume=volume*Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/fadeSeconds));}
  void OnDisable(){if(source)source.Stop();}
 }
}
