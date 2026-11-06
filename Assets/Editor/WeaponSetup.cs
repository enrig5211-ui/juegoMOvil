using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Valle;
using System.IO;
using System.Linq;
public static class WeaponSetup {
 const string Models="Assets/Ishikawa1116/LOW-POLY GUNS PACK_SAMPLE/Prefabs/Black and common color/";
 static AudioClip Clip(string path)=>AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/FreeWeaponSFX_v1.1/"+path+".wav");
 [MenuItem("Tools/Player/Preparar armas")]
 public static void Build(){if(EditorApplication.isPlaying)return;var motor=Object.FindFirstObjectByType<PlayerMotor>();if(motor.GetComponent<PlayerWeapon>())return;
 EditorSceneManager.SaveScene(motor.gameObject.scene,"Assets/Scenes/ValleDelRio_AntesDeArmas.unity",true);Directory.CreateDirectory("Assets/Weapons");
 var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetColor("_BaseColor",new Color(.3f,.95f,1));AssetDatabase.CreateAsset(material,"Assets/Weapons/PickupGlow.mat");
 var system=motor.gameObject.AddComponent<PlayerWeapon>();var socket=new GameObject("Arma - soporte").transform;socket.SetParent(motor.transform,false);socket.localPosition=new Vector3(.2f,.32f,.08f);system.socket=socket;
 var audioObject=new GameObject("Audio - Armas");audioObject.transform.SetParent(motor.transform,false);system.audioSource=audioObject.AddComponent<AudioSource>();system.audioSource.playOnAwake=false;system.audioSource.spatialBlend=0;system.audioSource.priority=70;
 var tracerObject=new GameObject("Disparo - trazo");tracerObject.transform.SetParent(motor.transform,false);var line=tracerObject.AddComponent<LineRenderer>();line.sharedMaterial=material;line.positionCount=2;line.startWidth=.025f;line.endWidth=.008f;line.useWorldSpace=true;line.enabled=false;system.tracer=line;
 var group=new GameObject("ARMAS - Recogibles").transform;
 Create(false,Ground(motor.transform.position+new Vector3(.7f,0,0)),group,material);
 Create(true,Ground(motor.transform.position+new Vector3(-.7f,0,0)),group,material);
 Create(false,new Vector3(0,.025f,-3.4641f),group,material);Create(true,new Vector3(2,.025f,-3.4641f),group,material);
 // Practice props are ordinary scene objects and can be moved or removed.
 var targetPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/KayKit/Packs/KayKit - Medieval Hexagon Pack (for Unity)/Prefabs/decoration/props/target.prefab");
 for(int i=0;i<2;i++){var root=new GameObject("Diana de practica "+(i+1));root.transform.position=Ground(new Vector3(2+i*1.2f,0,-.5f));var model=(GameObject)PrefabUtility.InstantiatePrefab(targetPrefab,root.transform);var target=root.AddComponent<WeaponTarget>();var box=root.AddComponent<BoxCollider>();box.center=new Vector3(0,.4f,0);box.size=new Vector3(.55f,.8f,.3f);}
 EditorSceneManager.SaveScene(motor.gameObject.scene);AssetDatabase.SaveAssets();File.WriteAllText("Temp/weapons-result.txt","4 pickups; 2 practice targets; pistol/rifle audio; F/G/R and mouse.\n");
 }
 static Vector3 Ground(Vector3 p){if(Physics.Raycast(p+Vector3.up*5,Vector3.down,out var hit,20,~0,QueryTriggerInteraction.Ignore))return hit.point+Vector3.up*.025f;return p;}
 static void Create(bool rifle,Vector3 pos,Transform parent,Material glow){var go=new GameObject(rifle?"Rifle - F para recoger":"Pistola - F para recoger");go.transform.SetParent(parent);go.transform.position=pos;var pickup=go.AddComponent<WeaponPickup>();pickup.displayName=rifle?"Rifle":"Pistola";pickup.automatic=rifle;pickup.magazineSize=rifle?30:12;pickup.ammo=pickup.magazineSize;pickup.reserve=rifle?120:60;pickup.shotInterval=rifle?.12f:.25f;pickup.damage=rifle?14:24;pickup.reloadSeconds=rifle?2:1.4f;
 var pivot=new GameObject("Modelo flotante").transform;pivot.SetParent(go.transform,false);pickup.model=pivot;
 var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Models+(rifle?"Rifle/Rifle_00":"Pistol/Pistol_00")+".prefab");var model=(GameObject)PrefabUtility.InstantiatePrefab(prefab,pivot);
 var rs=model.GetComponentsInChildren<Renderer>();Bounds b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);float scale=(rifle?.8f:.5f)/Mathf.Max(b.size.x,b.size.y,b.size.z);var offset=model.transform.position-b.center;model.transform.localScale*=scale;model.transform.localPosition=offset*scale;
 foreach(var c in model.GetComponentsInChildren<Collider>())c.enabled=false;foreach(var t in model.GetComponentsInChildren<Transform>())GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
 var muzzle=new GameObject("Salida del disparo").transform;muzzle.SetParent(pivot,false);muzzle.localPosition=new Vector3(0,.04f,rifle?.4f:.25f);pickup.muzzle=muzzle;
 var marker=new GameObject("Aro visible");marker.transform.SetParent(go.transform,false);pickup.marker=marker;var ring=marker.AddComponent<LineRenderer>();ring.sharedMaterial=glow;ring.useWorldSpace=false;ring.loop=true;ring.positionCount=32;ring.widthMultiplier=.025f;for(int i=0;i<32;i++){float a=i*Mathf.PI*2/32;ring.SetPosition(i,new Vector3(Mathf.Cos(a)*.36f,.02f,Mathf.Sin(a)*.36f));}
 string folder=rifle?"AssaultRifle":"Handgun",prefix=rifle?"ar":"hgn";
 pickup.shots=Enumerable.Range(1,3).Select(i=>Clip($"{folder}/Gunshots/sfx_wpn_{prefix}_gunshot_{i:00}")).ToArray();pickup.equipSound=Clip($"{folder}/Foley/sfx_wpn_{prefix}_foley_equip_01");pickup.reloadSound=Clip(rifle?"AssaultRifle/Reload/sfx_wpn_ar_reload_03_magIn":"Handgun/Reload/sfx_wpn_hgn_reload_04_magIn");
 if(pickup.shots.Any(x=>!x)||!pickup.equipSound||!pickup.reloadSound)throw new System.Exception("Missing weapon audio");
 }
}
