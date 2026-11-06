using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Valle;
using Valle.Lan;
public static class LanSetup {
 const string Arena="Assets/Scenes/ValleMultijugador.unity",Menu="Assets/Scenes/MenuInicio.unity";
 [MenuItem("Tools/Multijugador/Preparar LAN 4v4")]
 public static void Build(){
  if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
  Directory.CreateDirectory("Assets/Resources/Multiplayer");EditorSceneManager.OpenScene("Assets/Scenes/ValleDelRio.unity");
  var assets=AssetDatabase.LoadAssetAtPath<LanAssets>("Assets/Resources/Multiplayer/LanAssets.asset");if(!assets){assets=ScriptableObject.CreateInstance<LanAssets>();AssetDatabase.CreateAsset(assets,"Assets/Resources/Multiplayer/LanAssets.asset");}
  assets.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Artsystack - Fantasy RPG GUI/Resources/Font/Kurale-Regular.ttf");assets.button=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Artsystack - Fantasy RPG GUI/Resources/Sprites/components/button_01.png");
  foreach(var color in new[]{"blue","red"}){var source=AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/KayKit/Packs/KayKit - Medieval Hexagon Pack (for Unity)/Prefabs/units/{color}/unit_{color}_accent.prefab");var model=UnityEngine.Object.Instantiate(source);model.transform.localScale=Vector3.one*1.6f;var prefab=PrefabUtility.SaveAsPrefabAsset(model,$"Assets/Resources/Multiplayer/{color}Pawn.prefab");UnityEngine.Object.DestroyImmediate(model);if(color=="blue")assets.bluePawn=prefab;else assets.redPawn=prefab;}
  assets.blueMaterial=Material("Blue",new Color(.2f,.6f,1));assets.redMaterial=Material("Red",new Color(1,.25f,.18f));assets.glow=AssetDatabase.LoadAssetAtPath<Material>("Assets/Weapons/PickupGlow.mat");
  var pickups=UnityEngine.Object.FindObjectsByType<WeaponPickup>(FindObjectsSortMode.None);
  foreach(bool rifle in new[]{false,true}){var pickup=pickups.First(p=>p.automatic==rifle);var model=UnityEngine.Object.Instantiate(pickup.model.gameObject);model.transform.position=Vector3.zero;model.transform.rotation=Quaternion.identity;var prefab=PrefabUtility.SaveAsPrefabAsset(model,$"Assets/Resources/Multiplayer/{(rifle?"Rifle":"Pistol")}.prefab");UnityEngine.Object.DestroyImmediate(model);if(rifle){assets.rifle=prefab;assets.rifleShots=pickup.shots;assets.reload=pickup.reloadSound;assets.equip=pickup.equipSound;}else{assets.pistol=prefab;assets.pistolShots=pickup.shots;}}
  assets.steps=AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/Footsteps Pack Expanded/SingleSteps/GrassSteps"}).Take(6).Select(g=>AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g))).ToArray();EditorUtility.SetDirty(assets);AssetDatabase.SaveAssets();
  EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),Arena,true);EditorSceneManager.OpenScene(Arena);
  foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())if(root.GetComponent<PlayerMotor>()||root.name=="UI - Valle"||root.name=="ARMAS - Recogibles"||root.name.StartsWith("Diana de practica"))UnityEngine.Object.DestroyImmediate(root);
  var follow=UnityEngine.Object.FindAnyObjectByType<PlayerCamera>();if(follow)UnityEngine.Object.DestroyImmediate(follow);
  var navigation=new GameObject("Navegacion - LAN").AddComponent<NavMeshSurface>();navigation.useGeometry=NavMeshCollectGeometry.PhysicsColliders;navigation.overrideVoxelSize=true;navigation.voxelSize=.1f;navigation.BuildNavMesh();
  if(!navigation.navMeshData||NavMesh.CalculateTriangulation().vertices.Length==0)throw new Exception("Empty navigation mesh");
  string navPath="Assets/Resources/Multiplayer/ValleNavMesh.asset";if(File.Exists(navPath))AssetDatabase.DeleteAsset(navPath);AssetDatabase.CreateAsset(navigation.navMeshData,navPath);
  var world=new GameObject("PARTIDA - 4 contra 4").AddComponent<LanWorld>();world.deaEntries=new Transform[4];world.orgSpawns=new Transform[4];
  if(!NavMesh.SamplePosition(new Vector3(0,0,-3.46f),out var center,8,NavMesh.AllAreas))throw new Exception("No central ground");
  for(int id=0;id<8;id++){var desired=new Vector3(id<4?-8:8,0,-3+id%4*1.2f);var point=FindSpawn(desired,center.position);var spawn=new GameObject((id<4?"Azul ":"Rojo ")+(id%4+1)).transform;spawn.SetParent(world.transform);spawn.position=point+Vector3.up*.05f;if(id<4)world.deaEntries[id]=spawn;else world.orgSpawns[id-4]=spawn;}
  EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
  EditorSceneManager.OpenScene(Menu);if(!File.Exists("Assets/Scenes/MenuInicio_AntesDeLAN.unity"))EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),"Assets/Scenes/MenuInicio_AntesDeLAN.unity",true);
  var ui=UnityEngine.Object.FindAnyObjectByType<ValleInterface>();if(!ui.playButton.transform.parent.Find("Multijugador LAN")){var button=UnityEngine.Object.Instantiate(ui.playButton,ui.playButton.transform.parent);button.name="Multijugador LAN";button.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,30);button.GetComponentInChildren<Text>().text="Multijugador LAN";button.gameObject.AddComponent<LanMenuEntry>();}
  ui.playButton.GetComponentInChildren<Text>().text="Explorar solo";ui.playButton.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-40);ui.controlsButton.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-110);ui.quitButton.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-180);EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
  var list=EditorBuildSettings.scenes.ToList();if(!list.Any(s=>s.path==Arena))list.Add(new EditorBuildSettingsScene(Arena,true));EditorBuildSettings.scenes=list.ToArray();AssetDatabase.SaveAssets();
 }
 static Vector3 FindSpawn(Vector3 desired,Vector3 center){for(float r=0;r<8;r+=.6f)for(int i=0;i<12;i++){var p=desired+new Vector3(Mathf.Cos(i*Mathf.PI/6)*r,0,Mathf.Sin(i*Mathf.PI/6)*r);if(!NavMesh.SamplePosition(p,out var hit,.8f,NavMesh.AllAreas)||hit.position.y>.6f)continue;var path=new NavMeshPath();if(NavMesh.CalculatePath(center,hit.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete)return hit.position;}throw new Exception("No connected spawn near "+desired);}
 static Material Material(string name,Color color){string path="Assets/Resources/Multiplayer/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",color);EditorUtility.SetDirty(m);return m;}
}
