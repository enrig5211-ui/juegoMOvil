using UnityEngine;
using UnityEngine.InputSystem;
namespace Valle {
 [RequireComponent(typeof(PlayerStats))]
 public sealed class PlayerWeapon:MonoBehaviour {
  public Transform socket;public AudioSource audioSource;public LineRenderer tracer;
  public float interactRange=1.4f;public WeaponPickup Held {get;private set;}
  public WeaponPickup Nearby=>nearby;
  public bool Reloading=>reloadEnds>0;public int ShotsFired{get;private set;}
  public Collider LastHit {get;private set;} public Vector3 LastShotDirection {get;private set;}
  PlayerStats stats;PlayerMotor motor;WeaponPickup nearby;float nextShot,reloadEnds,flashEnds;Vector3 safeGround;Camera cam;
  readonly RaycastHit[] hits=new RaycastHit[64];GUIStyle label;
  void Awake(){stats=GetComponent<PlayerStats>();motor=GetComponent<PlayerMotor>();cam=Camera.main;safeGround=transform.position;}
  void OnEnable(){if(!stats)stats=GetComponent<PlayerStats>();stats.Died+=OnDeath;}
  void OnDisable(){if(stats)stats.Died-=OnDeath;}
  void OnDeath(){Drop();}
  void Update(){if(ValleInterface.BlocksInput)return;if(motor.Grounded)safeGround=transform.position;nearby=Nearest();
   if(tracer)tracer.enabled=Time.time<flashEnds;
   if(stats.IsDead)return;
   if(Held&&Reloading&&Time.time>=reloadEnds){int amount=Mathf.Min(Held.magazineSize-Held.ammo,Held.reserve);Held.ammo+=amount;Held.reserve-=amount;reloadEnds=0;}
   var keyboard=Keyboard.current;if(keyboard!=null){if(keyboard.fKey.wasPressedThisFrame&&nearby)TryPickup(nearby);if(keyboard.gKey.wasPressedThisFrame)Drop();if(keyboard.rKey.wasPressedThisFrame)BeginReload();}
   if(Held&&Mouse.current!=null){if((Held.automatic&&Mouse.current.leftButton.isPressed)||(!Held.automatic&&Mouse.current.leftButton.wasPressedThisFrame))TryFire();}
  }
  void LateUpdate(){if(ValleInterface.BlocksInput||!Held||stats.IsDead)return;socket.rotation=Quaternion.LookRotation(AimDirection());}
  Vector3 AimDirection()=>Mouse.current!=null?AimDirection(Mouse.current.position.ReadValue()):transform.forward;
  public Vector3 AimDirection(Vector2 screenPosition){Vector3 origin=transform.position+Vector3.up*.3f;if(cam){var ray=cam.ScreenPointToRay(screenPosition);if(new Plane(Vector3.up,origin).Raycast(ray,out float t)){var d=ray.GetPoint(t)-origin;d.y=0;if(d.sqrMagnitude>.04f)return d.normalized;}}return transform.forward;}
  WeaponPickup Nearest(){WeaponPickup best=null;float distance=interactRange;foreach(var w in WeaponPickup.Available){if(!w||w.Owner)continue;float d=Vector3.Distance(transform.position,w.transform.position);if(d<distance&&Clear(w)){distance=d;best=w;}}return best;}
  bool Clear(WeaponPickup w){Vector3 a=transform.position+Vector3.up*.4f,b=w.transform.position+Vector3.up*.5f;return !Physics.Linecast(a,b,~0,QueryTriggerInteraction.Ignore);}
  public bool TryPickup(WeaponPickup w){if(!w||w.Owner||stats.IsDead||Vector3.Distance(transform.position,w.transform.position)>interactRange||!Clear(w))return false;Drop();Held=w;w.Attach(this,socket);reloadEnds=0;nextShot=Time.time+.15f;if(w.equipSound)audioSource.PlayOneShot(w.equipSound,.45f);return true;}
  public void Drop(){if(!Held)return;var weapon=Held;Held=null;reloadEnds=0;audioSource.Stop();Vector3 p=motor.Grounded?transform.position:safeGround;Vector3 candidate=p+transform.right*.55f;
   if(Physics.Linecast(p+Vector3.up*.4f,candidate+Vector3.up*.4f,out var obstruction,~0,QueryTriggerInteraction.Ignore)&&!obstruction.collider.transform.IsChildOf(transform))candidate=p;
   if(Physics.Raycast(candidate+Vector3.up*2,Vector3.down,out var hit,4,~0,QueryTriggerInteraction.Ignore)&&!hit.collider.transform.IsChildOf(transform))candidate=hit.point+Vector3.up*.025f;else candidate=p;
   weapon.Release(candidate);}
  public bool BeginReload(){if(!Held||stats.IsDead||Reloading||Held.ammo>=Held.magazineSize||Held.reserve<=0)return false;reloadEnds=Time.time+Held.reloadSeconds;if(Held.reloadSound)audioSource.PlayOneShot(Held.reloadSound,.4f);return true;}
  public bool TryFire()=>TryFireAtScreen(Mouse.current!=null?Mouse.current.position.ReadValue():new Vector2(Screen.width*.5f,Screen.height*.5f));
  public bool TryFireAtScreen(Vector2 screenPosition){if(!Held||stats.IsDead||Reloading||Held.ammo<=0||Time.time<nextShot)return false;Held.ammo--;nextShot=Time.time+Held.shotInterval;ShotsFired++;
   if(Held.shots!=null&&Held.shots.Length>0)audioSource.PlayOneShot(Held.shots[Random.Range(0,Held.shots.Length)],.45f);
   Vector3 origin=transform.position+Vector3.up*.3f,dir=AimDirection(screenPosition),end=origin+dir*Held.range;int count=Physics.RaycastNonAlloc(origin,dir,hits,Held.range,~0,QueryTriggerInteraction.Ignore);float closest=Held.range;Collider collider=null;
   for(int i=0;i<count;i++)if(!hits[i].collider.transform.IsChildOf(transform)&&hits[i].distance<closest){closest=hits[i].distance;end=hits[i].point;collider=hits[i].collider;}
   LastHit=collider;LastShotDirection=dir;
   if(collider){MagicFx.Impact(end);var target=collider.GetComponentInParent<PlayerStats>();if(target&&target!=stats)target.TakeDamage(Held.damage);var practice=collider.GetComponentInParent<WeaponTarget>();if(practice)practice.TakeDamage(Held.damage);}
   if(tracer){tracer.SetPosition(0,Held.muzzle?Held.muzzle.position:origin);tracer.SetPosition(1,end);flashEnds=Time.time+.055f;tracer.enabled=true;}return true;
  }
 }
}
