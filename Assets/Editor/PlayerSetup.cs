using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Valle;
using System.IO;
public static class PlayerSetup {
 [MenuItem("Tools/Player/Preparar jugador y camara")]
 public static void Build(){if(Object.FindFirstObjectByType<PlayerMotor>())return;var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();EditorSceneManager.SaveScene(scene,"Assets/Scenes/ValleDelRio_AntesDelPlayer.unity",true);
 var visual=GameObject.Find("player");if(!visual){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/KayKit/Packs/KayKit - Medieval Hexagon Pack (for Unity)/Prefabs/units/blue/unit_blue_accent.prefab");visual=(GameObject)PrefabUtility.InstantiatePrefab(prefab);visual.transform.position=new Vector3(1,1,-3);}
 var spawn=visual.transform.position;var root=new GameObject("player");root.transform.position=spawn;visual.name="Visual - personaje cartoon";visual.transform.SetParent(root.transform,false);visual.transform.localScale=Vector3.one*1.6f;visual.transform.localPosition=Vector3.zero;visual.transform.localRotation=Quaternion.identity;
 var mf=visual.GetComponent<MeshFilter>();float height=mf.sharedMesh.bounds.size.y*visual.transform.localScale.y;visual.transform.localPosition=Vector3.up*(-mf.sharedMesh.bounds.min.y*visual.transform.localScale.y);
 foreach(var t in visual.GetComponentsInChildren<Transform>())GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
 int count=0;var landscape=GameObject.Find("VALLE DEL RIO");foreach(var mesh in landscape.GetComponentsInChildren<MeshFilter>()){if(mesh.GetComponent<Collider>()||mesh.name.Contains("waterlily")||mesh.name.Contains("waterplant")||mesh.name.Contains("cloud"))continue;var col=mesh.gameObject.AddComponent<MeshCollider>();col.sharedMesh=mesh.sharedMesh;count++;}
 Physics.SyncTransforms();if(Physics.Raycast(spawn+Vector3.up*30,Vector3.down,out var hit,100))root.transform.position=new Vector3(spawn.x,hit.point.y+.06f,spawn.z);
 var cc=root.AddComponent<CharacterController>();cc.height=Mathf.Max(height,.55f);cc.radius=.17f;cc.center=Vector3.up*(cc.height*.5f);cc.skinWidth=.025f;cc.stepOffset=.16f;cc.slopeLimit=48;cc.minMoveDistance=0;
 var stats=root.AddComponent<PlayerStats>();var motor=root.AddComponent<PlayerMotor>();motor.visual=visual.transform;motor.viewCamera=Camera.main;var hud=root.AddComponent<PlayerHUD>();hud.stats=stats;
 var camera=Camera.main;camera.orthographic=true;camera.orthographicSize=3.5f;camera.nearClipPlane=.1f;camera.farClipPlane=100;var follow=camera.gameObject.AddComponent<PlayerCamera>();follow.target=root.transform;camera.transform.rotation=Quaternion.Euler(45,35,0);camera.transform.position=root.transform.position+Vector3.up*.55f-camera.transform.forward*10;
 Selection.activeGameObject=root;EditorSceneManager.SaveScene(scene);File.WriteAllText("Temp/player-setup-result.txt",$"Player height: {height}\nStatic mesh colliders added: {count}\nSpawn: {root.transform.position}\n");
 }
}
