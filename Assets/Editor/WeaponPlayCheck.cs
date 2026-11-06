using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Valle;
using System.IO;
using System.Linq;
using System.Text;
[InitializeOnLoad]
public static class WeaponPlayCheck {
 static int stage=-1;static float start;static WeaponPickup weapon;static int ammo;static Vector3 bob;static Quaternion spin;static StringBuilder log=new StringBuilder();
 static WeaponPlayCheck(){EditorApplication.delayCall+=Begin;EditorApplication.update+=Tick;}
 [MenuItem("Tools/Player/Comprobar armas en Play")]
 public static void Request(){if(EditorApplication.isPlaying)return;File.WriteAllText("Temp/weapon-test.request","run");Begin();}
 static void Begin(){if(!File.Exists("Temp/weapon-test.request")||EditorApplication.isPlaying)return;File.Delete("Temp/weapon-test.request");SessionState.SetBool("WeaponTest",true);stage=-1;log.Clear();EditorApplication.isPlaying=true;}
 static void Check(bool ok,string message){log.AppendLine((ok?"PASS ":"FAIL ")+message);}
 static void Keys(params Key[] keys){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(keys));}
 static void Next(){stage++;start=Time.time;}
 static void Tick(){if(!SessionState.GetBool("WeaponTest",false)||!EditorApplication.isPlaying)return;var player=Object.FindFirstObjectByType<PlayerWeapon>();if(!player)return;float t=Time.time-start;
 try{switch(stage){
 case -1:Keys();var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=new Vector3(0,.06f,-3.4641f);cc.enabled=true;weapon=WeaponPickup.Available.OrderBy(x=>Vector3.Distance(x.transform.position,player.transform.position)).First();bob=weapon.model.localPosition;spin=weapon.model.localRotation;Check(WeaponPickup.Available.Count==4,"Four pickups exist");Next();break;
 case 0:if(t<.7f)return;Check(Vector3.Distance(bob,weapon.model.localPosition)>.01f&&Quaternion.Angle(spin,weapon.model.localRotation)>10,"Pickup floats and rotates");var target=Object.FindFirstObjectByType<WeaponTarget>();target.transform.position=player.transform.position+Vector3.forward*1.2f;Physics.SyncTransforms();InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=(Vector2)Camera.main.WorldToScreenPoint(target.transform.position+Vector3.up*.3f)});Keys(Key.F);Next();break;
 case 1:if(t<.3f)return;Keys();Check(player.Held!=null,"F picks up nearby weapon");if(!player.Held)throw new System.Exception("Cannot pick up weapon");weapon=player.Held;ammo=weapon.ammo;Check(!weapon.marker.activeSelf&&!WeaponPickup.Available.Contains(weapon),"Held marker hidden and pickup unavailable");var aimPoint=Object.FindFirstObjectByType<WeaponTarget>().transform.position+Vector3.up*.3f;Vector2 screenPoint=Camera.main.WorldToScreenPoint(aimPoint);Check(Vector3.Dot(player.AimDirection(screenPoint),Vector3.forward)>.99f,"Screen cursor projects toward target");Check(player.TryFireAtScreen(screenPoint),"Weapon fires");Check(Object.FindFirstObjectByType<WeaponTarget>().Health<100,"Shot ray damages practice target");log.AppendLine("Shot hit: "+(player.LastHit?player.LastHit.name:"none")+" direction "+player.LastShotDirection);Check(weapon.ammo==ammo-1,"Shot consumes one round");Check(!player.TryFire(),"Fire rate limits repeated shot");Check(player.audioSource.isPlaying,"Weapon audio plays");Check(player.BeginReload(),"Reload starts");Next();break;
 case 2:if(t<2.2f)return;Check(weapon.ammo==weapon.magazineSize&&weapon.reserve<(weapon.automatic?120:60),"Reload transfers reserve ammunition");var aim=Object.FindFirstObjectByType<WeaponTarget>();float health=aim.Health;var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.name="Test shot blocker";blocker.transform.position=(player.transform.position+aim.transform.position)*.5f+Vector3.up*.3f;blocker.transform.localScale=new Vector3(.8f,1,.1f);Physics.SyncTransforms();Check(player.TryFireAtScreen(Camera.main.WorldToScreenPoint(aim.transform.position+Vector3.up*.3f)),"Second shot fires toward cover");Check(player.LastHit==blocker.GetComponent<Collider>()&&aim.Health==health,"Obstacle blocks damage to target");blocker.GetComponent<Collider>().enabled=false;Object.Destroy(blocker);ammo=weapon.ammo;player.BeginReload();Keys(Key.G);Next();break;
 case 3:if(t<.25f)return;Keys();Check(!player.Held&&weapon.Owner==null,"G drops weapon");Check(!player.Reloading&&weapon.ammo==ammo,"Dropping cancels reload and preserves ammo");Check(weapon.marker.activeSelf&&WeaponPickup.Available.Contains(weapon),"Dropped weapon becomes visible pickup");Check(player.TryPickup(weapon),"Dropped weapon can be picked up again");var stats=player.GetComponent<PlayerStats>();stats.TakeDamage(100000);Check(!player.Held&&weapon.Owner==null,"Death drops weapon immediately");Check(!player.TryPickup(weapon),"Dead player cannot pick up");stats.Restore();Next();break;
 case 4:if(t<.25f)return;Check(player.TryPickup(weapon),"Pickup after revive");var other=WeaponPickup.Available.First();other.transform.position=player.transform.position+Vector3.right*.7f;Check(player.TryPickup(other),"Swapping equips second weapon");Check(weapon.Owner==null&&WeaponPickup.Available.Contains(weapon),"Swapping returns previous weapon to world");Check(Object.FindObjectsByType<WeaponPickup>().Length==4,"No weapon duplicated or lost");player.Drop();File.WriteAllText("Documentation/Weapons-play-check.txt",log.ToString());SessionState.SetBool("WeaponTest",false);Keys();EditorApplication.isPlaying=false;break;
 }}catch(System.Exception e){log.AppendLine("FAIL "+e);File.WriteAllText("Documentation/Weapons-play-check.txt",log.ToString());SessionState.SetBool("WeaponTest",false);Keys();EditorApplication.isPlaying=false;}
 }
}
