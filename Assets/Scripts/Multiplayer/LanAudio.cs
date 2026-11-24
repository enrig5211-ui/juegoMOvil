using System.Collections.Generic;
using UnityEngine;
namespace Valle.Lan {
 // Sonido de la partida en cada cliente: noche y música de fondo, y avisos según lo que ocurre.
 public sealed class LanAudio:MonoBehaviour {
  LanSession session;LanWorld world;LanAssets art;AudioSource night,music,cues;
  bool wasDeploying=true,leaderDown;float lastWave,lastCapture;readonly HashSet<int> alarms=new HashSet<int>(),scans=new HashSet<int>();
  public string LastCue{get;private set;}
  public readonly List<string> History=new List<string>();
  public bool NightPlaying=>night&&night.isPlaying&&night.clip;
  public bool MusicPlaying=>music&&music.isPlaying&&music.clip;
  public void Initialize(LanSession owner,LanWorld map,LanAssets assets){
   session=owner;world=map;art=assets;
   night=Loop(art.nightLoop);music=Loop(art.tensionMusic);
   cues=gameObject.AddComponent<AudioSource>();cues.playOnAwake=false;cues.spatialBlend=0;
   wasDeploying=session.Night.Deploying;lastWave=session.Night.Wave;
  }
  AudioSource Loop(AudioClip clip){var source=gameObject.AddComponent<AudioSource>();source.clip=clip;source.loop=true;source.spatialBlend=0;source.volume=0;if(clip)source.Play();return source;}
  void Update(){
   if(!session)return;
   var state=session.Night;float day=LanVision.Daylight(session);
   night.volume=Mathf.MoveTowards(night.volume,.45f*(1-day),Time.deltaTime*.5f);
   music.volume=Mathf.MoveTowards(music.volume,session.Phase=="result"?.06f:.2f,Time.deltaTime*.3f);
   if(session.Phase!="match")return;
   // La DEA llega en helicóptero cuando termina el despliegue.
   if(wasDeploying&&!state.Deploying)Cue(art.helicopter,.6f,"helicoptero");wasDeploying=state.Deploying;
   // Radio: cada oleada de refuerzos y cuando el líder cae.
   if(!state.Deploying&&state.Wave>lastWave+20)Cue(art.radio,.45f,"radio");lastWave=state.Wave;
   var leader=world.LeaderPawn;bool down=leader&&leader.State.down>0;if(down&&!leaderDown)Cue(art.radio,.6f,"radio");leaderDown=down;
   if(state.Capture>0&&lastCapture<=0)Cue(art.cuffs,.8f,"esposas");lastCapture=state.Capture;
   int team=session.LocalSlot>=0?session.Slots[session.LocalSlot].team:-1;
   foreach(var mark in world.Marks)if(mark.kind==LanItems.AlarmMark&&mark.fired&&mark.team==team&&alarms.Add(mark.id))Cue(art.alarm,.4f,"alarma");
   // Rastreo: los dos bandos oyen el pitido (la DEA lo lanzó; el líder sabe que lo ubicaron).
   foreach(var mark in world.Marks)if(mark.kind==LanItems.ScanMark&&scans.Add(mark.id))Cue(art.scan,.6f,"rastreo");
  }
  void Cue(AudioClip clip,float volume,string name){LastCue=name;History.Add(name);if(clip)cues.PlayOneShot(clip,volume);}
 }
}
