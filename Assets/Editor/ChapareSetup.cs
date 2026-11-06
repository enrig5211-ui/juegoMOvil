using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using Valle.Lan;
// Genera el mapa gris provisional. Sirve para probar reglas y distancias antes de hacer el arte.
[InitializeOnLoad]
public static class ChapareSetup {
 const string ScenePath="Assets/Scenes/NocheChapare.unity",NavPath="Assets/Resources/Multiplayer/ChapareNavMesh.asset",OldArena="Assets/Scenes/ValleMultijugador.unity";
 const float HalfX=60,HalfZ=50;
 static readonly (string name,Vector3 at)[] Entries={("Río",new Vector3(-54,0,-6)),("Camino",new Vector3(2,0,-44)),("Pista",new Vector3(46,0,-26)),("Selva",new Vector3(-8,0,44))};
 static readonly Vector3[] Hideouts={new Vector3(-24,0,24),new Vector3(24,0,28),new Vector3(-26,0,-22),new Vector3(26,0,-24),new Vector3(-48,0,30),new Vector3(30,0,8)};
 static readonly Vector3[] ExtraZones={Vector3.zero,new Vector3(-39,0,-30),new Vector3(-39,0,20),new Vector3(46,0,20),new Vector3(2,0,-30),new Vector3(-8,0,38)};
 static readonly Vector3[] OrgSpawns={new Vector3(-2,0,-2),new Vector3(2,0,-2),new Vector3(-2,0,2),new Vector3(2,0,2)};
 static readonly Rect Strip=Rect.MinMaxRect(38,-36,54,36),Road=Rect.MinMaxRect(-1,-50,5,-12),River=Rect.MinMaxRect(-42,-50,-36,50);
 static ChapareSetup(){EditorApplication.delayCall+=EnsureBuildList;}
 [MenuItem("Tools/Multijugador/Crear mapa gris del Chapare")]
 public static void Build(){
  if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.62f,.64f,.66f);
  CreateViewpoint();
  var map=new GameObject("MAPA GRIS").transform;
  var ground=Mat("GrisSuelo",new Color(.31f,.34f,.31f));
  var jungle=Mat("GrisSelva",new Color(.13f,.26f,.17f));
  var crate=Mat("GrisCobertura",new Color(.47f,.43f,.36f));
  var water=Mat("GrisAgua",new Color(.16f,.31f,.44f));
  var dirt=Mat("GrisCamino",new Color(.42f,.38f,.32f));
  var wall=Mat("GrisMuro",new Color(.19f,.19f,.22f));
  var hut=Mat("GrisRefugio",new Color(.52f,.41f,.28f));
  Block(map,"Suelo",new Vector3(0,-.5f,0),new Vector3(HalfX*2+4,1,HalfZ*2+4),ground,true);
  Surface(map,"Rio",River,water);Surface(map,"Camino",Road,dirt);Surface(map,"Pista",Strip,dirt);
  foreach(var side in new[]{-1,1}){
   Block(map,"Limite",new Vector3(side*(HalfX+.5f),1,0),new Vector3(1,2,HalfZ*2+2),wall,true);
   Block(map,"Limite",new Vector3(0,1,side*(HalfZ+.5f)),new Vector3(HalfX*2+2,2,1),wall,true);
  }
  UnityEngine.Random.InitState(20261106);
  CreateJungle(map,jungle);
  foreach(var point in Hideouts)CreateHut(map,point,hut);
  CreateCover(map,crate);
  BakeNavigation();
  var world=new GameObject("PARTIDA - Noche").AddComponent<LanWorld>();
  world.deaEntries=Entries.Select(e=>Point(world.transform,e.name,e.at)).ToArray();
  world.orgSpawns=OrgSpawns.Select((p,i)=>Point(world.transform,"Organizacion "+(i+1),p)).ToArray();
  world.hideouts=Hideouts.Select((p,i)=>Point(world.transform,"Refugio "+(i+1),p)).ToArray();
  world.zones=world.hideouts.Concat(ExtraZones.Select((p,i)=>Point(world.transform,"Zona "+(i+1),p))).ToArray();
  world.tacticalCenter=Vector3.forward*4.5f;world.tacticalSize=62.5f;
  Validate(world);
  Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
  if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new Exception("No se pudo guardar "+ScenePath);
  EnsureBuildList();AssetDatabase.SaveAssets();
  Debug.Log("Mapa gris creado: "+ScenePath);
 }
 static void EnsureBuildList(){
  if(!File.Exists(ScenePath))return;
  var scenes=EditorBuildSettings.scenes.ToList();
  if(scenes.Any(s=>s.path==ScenePath))return;
  scenes.RemoveAll(s=>s.path==OldArena);
  scenes.Add(new EditorBuildSettingsScene(ScenePath,true));
  EditorBuildSettings.scenes=scenes.ToArray();
 }
 static void CreateViewpoint(){
  var eye=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));eye.tag="MainCamera";
  var camera=eye.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=LanNightRules.CameraSize;camera.nearClipPlane=.1f;camera.farClipPlane=300;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.04f,.05f,.06f);
  var sun=new GameObject("Luz provisional").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1;sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(55,30,0);
  new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
 }
 static void CreateJungle(Transform map,Material material){
  var parent=Group(map,"Selva");
  for(float x=-55;x<=55;x+=10)for(float z=-45;z<=45;z+=10){
   var center=new Vector3(x+Jitter(.9f),.8f,z+Jitter(.9f));
   if(UnityEngine.Random.value<.12f||Reserved(center))continue;
   Block(parent,"Selva",center,new Vector3(UnityEngine.Random.Range(5.5f,6.4f),1.6f,UnityEngine.Random.Range(5.5f,6.4f)),material,true);
  }
 }
 static void CreateHut(Transform map,Vector3 at,Material material){
  var parent=Group(map,"Refugio");
  var open=Mathf.Abs(at.x)>Mathf.Abs(at.z)?new Vector3(-Mathf.Sign(at.x),0,0):new Vector3(0,0,-Mathf.Sign(at.z));
  var side=Vector3.Cross(Vector3.up,open);
  var length=new Vector3(Mathf.Abs(side.x)*5+Mathf.Abs(open.x)*.4f,1.2f,Mathf.Abs(side.z)*5+Mathf.Abs(open.z)*.4f);
  var depth=new Vector3(Mathf.Abs(open.x)*5+Mathf.Abs(side.x)*.4f,1.2f,Mathf.Abs(open.z)*5+Mathf.Abs(side.z)*.4f);
  Block(parent,"Pared",at-open*2.5f+Vector3.up*.6f,length,material,true);
  Block(parent,"Pared",at+side*2.5f+Vector3.up*.6f,depth,material,true);
  Block(parent,"Pared",at-side*2.5f+Vector3.up*.6f,depth,material,true);
 }
 static void CreateCover(Transform map,Material material){
  var parent=Group(map,"Coberturas");
  for(int i=0;i<14;i++){
   float angle=i*Mathf.PI*2/14,radius=UnityEngine.Random.Range(6.5f,11.5f);
   Crate(parent,new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius),material);
  }
  for(int i=0;i<10;i++)Crate(parent,new Vector3(UnityEngine.Random.Range(Strip.xMin+1,Strip.xMax-1),0,UnityEngine.Random.Range(Strip.yMin+1,Strip.yMax-1)),material);
  for(int i=0;i<6;i++)Crate(parent,new Vector3(UnityEngine.Random.Range(Road.xMin+.5f,Road.xMax-.5f),0,UnityEngine.Random.Range(Road.yMin+8,Road.yMax-2)),material);
 }
 static void Crate(Transform parent,Vector3 at,Material material){
  var keep=Entries.Select(e=>e.at).Concat(Hideouts).Concat(OrgSpawns).Concat(ExtraZones);
  if(keep.Any(p=>Vector3.Distance(p,at)<3.4f))return;
  Block(parent,"Cobertura",at+Vector3.up*.4f,new Vector3(1.2f,.8f,1.2f),material,true);
 }
 static bool Reserved(Vector3 center){
  var flat=new Vector2(center.x,center.z);
  bool Near(Vector3 p,float radius)=>Vector2.Distance(flat,new Vector2(p.x,p.z))<radius;
  bool Inside(Rect r){var wide=Rect.MinMaxRect(r.xMin-3.8f,r.yMin-3.8f,r.xMax+3.8f,r.yMax+3.8f);return wide.Contains(flat);}
  return Near(Vector3.zero,15)||Entries.Any(e=>Near(e.at,7.5f))||Hideouts.Any(h=>Near(h,7.5f))||ExtraZones.Any(z=>Near(z,5))||Inside(Strip)||Inside(Road)||Inside(River);
 }
 static void BakeNavigation(){
  var surface=new GameObject("Navegacion").AddComponent<NavMeshSurface>();
  surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;surface.overrideVoxelSize=true;surface.voxelSize=.12f;surface.BuildNavMesh();
  if(!surface.navMeshData||NavMesh.CalculateTriangulation().vertices.Length==0)throw new Exception("La navegación quedó vacía");
  if(File.Exists(NavPath))AssetDatabase.DeleteAsset(NavPath);
  AssetDatabase.CreateAsset(surface.navMeshData,NavPath);
 }
 static void Validate(LanWorld world){
  var points=world.deaEntries.Concat(world.orgSpawns).Concat(world.zones).ToList();
  if(!NavMesh.SamplePosition(Vector3.zero,out var center,3,NavMesh.AllAreas))throw new Exception("El centro no tiene navegación");
  foreach(var point in points){
   var path=new NavMeshPath();
   bool ok=NavMesh.SamplePosition(point.position,out var hit,2,NavMesh.AllAreas)&&NavMesh.CalculatePath(center.position,hit.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
   if(!ok)throw new Exception("Punto sin conexión: "+point.name);
  }
 }
 static float Jitter(float range)=>UnityEngine.Random.Range(-range,range);
 static Transform Group(Transform parent,string name){var found=parent.Find(name);if(found)return found;var go=new GameObject(name);go.transform.SetParent(parent);return go.transform;}
 static Transform Point(Transform parent,string name,Vector3 at){var go=new GameObject(name);go.transform.SetParent(parent);go.transform.position=at+Vector3.up*.05f;return go.transform;}
 static void Surface(Transform parent,string name,Rect area,Material material){Block(parent,name,new Vector3(area.center.x,.01f,area.center.y),new Vector3(area.width,.02f,area.height),material,false);}
 static void Block(Transform parent,string name,Vector3 center,Vector3 size,Material material,bool solid){
  var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent);go.transform.position=center;go.transform.localScale=size;
  go.GetComponent<MeshRenderer>().sharedMaterial=material;
  if(!solid)UnityEngine.Object.DestroyImmediate(go.GetComponent<BoxCollider>());
  GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic);
 }
 static Material Mat(string name,Color color){
  string path="Assets/Resources/Multiplayer/"+name+".mat";
  var material=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
  material.SetColor("_BaseColor",color);EditorUtility.SetDirty(material);
  return material;
 }
}
