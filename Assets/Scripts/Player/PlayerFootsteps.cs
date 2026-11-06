using UnityEngine;
namespace Valle {
 [RequireComponent(typeof(PlayerMotor),typeof(PlayerStats))]
 public sealed class PlayerFootsteps:MonoBehaviour {
  public AudioClip[] grass,earth,stone,water;
  [Range(0,1)] public float volume=.32f;
  public AudioSource source;
  public int PlayedSteps {get;private set;}
  public string LastSurface {get;private set;}
  PlayerMotor motor;PlayerStats stats;AudioClip previous;
  void Awake(){motor=GetComponent<PlayerMotor>();stats=GetComponent<PlayerStats>();}
  void OnEnable(){if(!motor)motor=GetComponent<PlayerMotor>();motor.FootContact+=Step;motor.Landed+=Land;}
  void OnDisable(){if(motor){motor.FootContact-=Step;motor.Landed-=Land;}if(source)source.Stop();}
  void Step(){Play(1);}
  void Land(){Play(.8f);}
  void Play(float strength){if(!source||stats.IsDead||!motor.Grounded)return;
   var clips=grass;LastSurface="Grass";
   if(Physics.Raycast(transform.position+Vector3.up*.3f,Vector3.down,out var hit,.8f,~0,QueryTriggerInteraction.Ignore)){
    var filter=hit.collider.GetComponent<MeshFilter>();string n=filter&&filter.sharedMesh?filter.sharedMesh.name.ToLowerInvariant():hit.collider.name.ToLowerInvariant();
    if(n.Contains("bridge")||n.Contains("rock")||n.Contains("mountain")||n.Contains("building_")){clips=stone;LastSurface="Stone";}
    else if(n.Contains("road")||n.Contains("coast")||n.Contains("crossing")){clips=earth;LastSurface="Earth";}
    else if(n.Contains("water")||n.Contains("river")){clips=water;LastSurface="Water";}
   }
   if(clips==null||clips.Length==0)clips=grass;if(clips==null||clips.Length==0)return;
   int index=Random.Range(0,clips.Length);if(clips.Length>1&&clips[index]==previous)index=(index+1)%clips.Length;var clip=clips[index];if(!clip)return;
   previous=clip;source.pitch=Random.Range(.95f,1.05f);source.PlayOneShot(clip,volume*strength*(motor.Speed>motor.walkSpeed+.2f?1.12f:1));PlayedSteps++;
  }
 }
}
