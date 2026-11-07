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
// Genera el mapa hexagonal del Chapare con las piezas de KayKit.
// Las piezas son prefabs editables; las colisiones son cajas simples aparte, en "COLISIONES".
[InitializeOnLoad]
public static class ChapareSetup {
 const string ScenePath="Assets/Scenes/NocheChapare.unity",NavPath="Assets/Resources/Multiplayer/ChapareNavMesh.asset",OldArena="Assets/Scenes/ValleMultijugador.unity";
 const string Kit="Assets/KayKit/Packs/KayKit - Medieval Hexagon Pack (for Unity)/Prefabs/";
 const int Radius=30,Skirt=3,Seed=20261107,WaterLayer=4,NotWalkable=1;
 enum Kind{Grass,Forest,River,Bridge,Road,Dirt,Building,Mountain}
 static readonly Vector2Int[] Directions={new Vector2Int(1,0),new Vector2Int(0,1),new Vector2Int(-1,1),new Vector2Int(-1,0),new Vector2Int(0,-1),new Vector2Int(1,-1)};
 // Conexiones de cada pieza de río o camino (A..M), como en RoadFix.
 static readonly int[][] Ports={new[]{0,3},new[]{0,4},new[]{0,5},new[]{0,2,4},new[]{0,3,4},new[]{0,2,3},new[]{0,1,5},new[]{0,1,3,5},new[]{1,2,4,5},new[]{0,1,2,3},new[]{0,1,2,4,5},new[]{0,1,2,3,4,5},new[]{0}};
 static readonly (string name,Vector3 at)[] Entries={("Río",new Vector3(-50,0,-2)),("Camino",new Vector3(2,0,-44)),("Pista",new Vector3(40,0,-14)),("Selva",new Vector3(-8,0,44))};
 static readonly Vector3[] Hideouts={new Vector3(-22,0,24),new Vector3(24,0,28),new Vector3(-22,0,-22),new Vector3(26,0,-24),new Vector3(8,0,20),new Vector3(30,0,8)};
 static readonly Vector3[] ExtraZones={Vector3.zero,new Vector3(-26,0,-34),new Vector3(-44,0,-16),new Vector3(38,0,18),new Vector3(2,0,-30),new Vector3(-8,0,36)};
 static readonly Vector3[] OrgSpawns={new Vector3(-2,0,-1.7f),new Vector3(2,0,-1.7f),new Vector3(-2,0,1.7f),new Vector3(2,0,1.7f)};
 static readonly Vector3[] RiverPath={new Vector3(-22,0,60),new Vector3(-34,0,28),new Vector3(-40,0,0),new Vector3(-34,0,-28),new Vector3(-22,0,-60)};
 static readonly float[] BridgeZ={30,0,-30};
 static readonly Rect Strip=Rect.MinMaxRect(34,-20,48,20);
 static readonly (string prefab,Vector3 at)[] Camp={
  ("building_townhall_red",new Vector3(0,0,6.9f)),("building_tavern_red",new Vector3(-6,0,3.5f)),("building_market_red",new Vector3(6,0,3.5f)),
  ("building_home_A_red",new Vector3(-8,0,-3.5f)),("building_home_B_red",new Vector3(8,0,-3.5f)),("building_barracks_red",new Vector3(-4,0,-6.9f)),
  ("building_watchtower_red",new Vector3(4,0,-6.9f)),("building_well_red",new Vector3(-3,0,5.2f))};
 static readonly string[] ForestModels={"trees_A_large","trees_B_large","trees_A_medium","trees_B_medium","hills_A_trees","hills_B_trees"};
 static readonly string[] MountainModels={"mountain_A_grass_trees","mountain_B_grass","mountain_C_grass_trees","mountain_A_grass","mountain_B_grass_trees","mountain_C"};
 static System.Random rng;
 static ChapareSetup(){EditorApplication.delayCall+=EnsureBuildList;}

 [MenuItem("Tools/Multijugador/Crear mapa del Chapare")]
 public static void Build(){
  if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
  rng=new System.Random(Seed);
  var map=Layout(out var riverOrder,out var roadPorts,out var bridges);
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  CreateViewpoint();
  var root=new GameObject("MAPA HEXAGONAL").transform;
  var colliders=new GameObject("COLISIONES").transform;
  Build(map,riverOrder,roadPorts,bridges,root,colliders);
  BakeNavigation();
  var world=new GameObject("PARTIDA - Noche").AddComponent<LanWorld>();
  world.deaEntries=Entries.Select(e=>Point(world.transform,e.name,Snap(e.at))).ToArray();
  world.orgSpawns=OrgSpawns.Select((p,i)=>Point(world.transform,"Organizacion "+(i+1),p)).ToArray();
  world.hideouts=Hideouts.Select((p,i)=>Point(world.transform,"Refugio "+(i+1),Snap(p))).ToArray();
  world.zones=world.hideouts.Concat(ExtraZones.Select((p,i)=>Point(world.transform,"Zona "+(i+1),Snap(p)))).ToArray();
  world.tacticalCenter=Vector3.forward*4.5f;world.tacticalSize=62.5f;
  Validate(world);
  if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new Exception("No se pudo guardar "+ScenePath);
  EnsureBuildList();AssetDatabase.SaveAssets();
  var counts=map.Values.GroupBy(k=>k).OrderBy(g=>g.Key).Select(g=>$"{g.Key} {g.Count()}");
  Debug.Log($"Mapa del Chapare creado: {ScenePath} · {map.Count} hexágonos · "+string.Join(", ",counts));
 }

 // ---------- Diseño ----------
 static Dictionary<Vector2Int,Kind> Layout(out List<Vector2Int> river,out Dictionary<Vector2Int,HashSet<int>> roads,out List<Vector2Int> bridges){
  var map=new Dictionary<Vector2Int,Kind>();
  for(int q=-Radius-Skirt;q<=Radius+Skirt;q++)for(int r=-Radius-Skirt;r<=Radius+Skirt;r++){var c=new Vector2Int(q,r);int d=Dist(c);if(d<=Radius+Skirt)map[c]=d>=Radius?Kind.Mountain:Kind.Grass;}
  var flow=Chain(RiverPath.Select(Cell).ToArray()).Where(map.ContainsKey).ToList();river=flow;
  foreach(var c in flow)map[c]=Kind.River;
  bridges=new List<Vector2Int>();
  foreach(float z in BridgeZ){
   var best=Enumerable.Range(1,flow.Count-2).Where(i=>Dist(flow[i])<Radius-2&&Straight(flow,i)).OrderBy(i=>Mathf.Abs(World(flow[i]).z-z)).First();
   bridges.Add(flow[best]);map[flow[best]]=Kind.Bridge;
  }
  roads=new Dictionary<Vector2Int,HashSet<int>>();
  var middle=bridges[1];
  int east=Side(flow,middle,+1),west=Side(flow,middle,-1);
  Road(map,roads,Chain(Cell(Entries[1].at),Cell(new Vector3(0,0,-6))));
  Road(map,roads,Chain(Cell(new Vector3(6,0,0)),Cell(new Vector3(37,0,0))));
  Road(map,roads,Chain(Cell(new Vector3(-6,0,0)),middle+Directions[east]).Append(middle).Append(middle+Directions[west]).ToList());
  Road(map,roads,Chain(middle+Directions[west],Cell(Entries[0].at)));
  foreach(var c in map.Keys.ToList())if(map[c]==Kind.Grass&&Strip.Contains(new Vector2(World(c).x,World(c).z)))map[c]=Kind.Dirt;
  foreach(var b in Camp)map[Cell(b.at)]=Kind.Building;
  var keep=new HashSet<Vector2Int>();
  void Clear(Vector2Int center,int radius){foreach(var c in map.Keys)if(Dist(c-center)<=radius)keep.Add(c);}
  Clear(Vector2Int.zero,6);foreach(var e in Entries)Clear(Cell(e.at),3);foreach(var h in Hideouts)Clear(Cell(h),1);foreach(var z in ExtraZones)Clear(Cell(z),1);
  foreach(var b in bridges)foreach(var d in Directions)Clear(b+d,1);
  // Selva agrupada con ruido; los refugios quedan rodeados de árboles.
  foreach(var c in map.Keys.ToList()){
   if(map[c]!=Kind.Grass||keep.Contains(c))continue;
   var p=World(c);float noise=Mathf.PerlinNoise(p.x*.055f+31.7f,p.z*.055f+12.3f);
   bool ring=Hideouts.Any(h=>Dist(c-Cell(h))==2);
   if(noise>.46f||ring&&rng.NextDouble()<.8)map[c]=Kind.Forest;
  }
  Connect(map);
  return map;
 }
 static bool Straight(List<Vector2Int> river,int i){var a=Direction(river[i-1]-river[i]);var b=Direction(river[i+1]-river[i]);return (a+3)%6==b;}
 // Dirección libre (sin río) que más apunta al este (+1) o al oeste (-1).
 static int Side(List<Vector2Int> river,Vector2Int bridge,int sign){
  return Enumerable.Range(0,6).Where(d=>!river.Contains(bridge+Directions[d])).OrderByDescending(d=>sign*World(Directions[d]).x).First();
 }
 static void Road(Dictionary<Vector2Int,Kind> map,Dictionary<Vector2Int,HashSet<int>> roads,List<Vector2Int> path){
  for(int i=0;i<path.Count;i++){
   var c=path[i];
   if(map[c]==Kind.River)throw new Exception("Un camino cruza el río fuera de un puente: "+c);
   if(!roads.ContainsKey(c))roads[c]=new HashSet<int>();
   if(i>0)roads[c].Add(Direction(path[i-1]-c));
   if(i<path.Count-1)roads[c].Add(Direction(path[i+1]-c));
   if(map[c]!=Kind.Bridge)map[c]=Kind.Road;
  }
 }
 // Si un punto clave quedó encerrado por la selva, abre el sendero más corto hacia la zona conectada.
 static void Connect(Dictionary<Vector2Int,Kind> map){
  bool Walk(Kind k)=>k==Kind.Grass||k==Kind.Road||k==Kind.Dirt||k==Kind.Bridge;
  var targets=Entries.Select(e=>Cell(e.at)).Concat(Hideouts.Select(Cell)).Concat(ExtraZones.Select(Cell)).Concat(OrgSpawns.Select(Cell)).ToList();
  foreach(var target in targets){
   var reach=Flood(map,Vector2Int.zero,Walk);
   if(reach.Contains(target))continue;
   var previous=new Dictionary<Vector2Int,Vector2Int>{[target]=target};var queue=new Queue<Vector2Int>();queue.Enqueue(target);Vector2Int found=target;bool ok=false;
   while(queue.Count>0&&!ok){var c=queue.Dequeue();foreach(var d in Directions){var n=c+d;if(previous.ContainsKey(n)||!map.TryGetValue(n,out var k)||Dist(n)>=Radius)continue;if(!Walk(k)&&k!=Kind.Forest)continue;previous[n]=c;if(reach.Contains(n)){found=n;ok=true;break;}queue.Enqueue(n);}}
   if(!ok)throw new Exception("No se pudo conectar el punto "+target);
   for(var c=found;c!=target;c=previous[c])if(map[c]==Kind.Forest)map[c]=Kind.Grass;
   if(map[target]==Kind.Forest)map[target]=Kind.Grass;
  }
 }
 static HashSet<Vector2Int> Flood(Dictionary<Vector2Int,Kind> map,Vector2Int start,Func<Kind,bool> walk){
  var seen=new HashSet<Vector2Int>{start};var queue=new Queue<Vector2Int>();queue.Enqueue(start);
  while(queue.Count>0){var c=queue.Dequeue();foreach(var d in Directions){var n=c+d;if(seen.Contains(n)||!map.TryGetValue(n,out var k)||Dist(n)>=Radius||!walk(k))continue;seen.Add(n);queue.Enqueue(n);}}
  return seen;
 }

 // ---------- Construcción ----------
 static void Build(Dictionary<Vector2Int,Kind> map,List<Vector2Int> river,Dictionary<Vector2Int,HashSet<int>> roads,List<Vector2Int> bridges,Transform root,Transform colliders){
  Transform ground=Group(root,"01 - Suelo"),water=Group(root,"02 - Rio y puentes"),paths=Group(root,"03 - Caminos y pista"),forest=Group(root,"04 - Selva"),camp=Group(root,"05 - Campamento"),edge=Group(root,"06 - Montanas"),detail=Group(root,"07 - Detalles");
  Box(colliders,"Suelo",new Vector3(0,-.5f,0),new Vector3(4*(Radius+Skirt)+4,1,3.5f*(Radius+Skirt)+4),0);
  for(int i=0;i<river.Count;i++){
   var c=river[i];
   int a=i==0?Direction(river[0]-river[1]):Direction(river[i-1]-c);
   int b=i==river.Count-1?Direction(river[i]-river[i-1]):Direction(river[i+1]-c);
   if(i==0)a=(Direction(river[1]-c)+3)%6;
   if(i==river.Count-1)b=(Direction(river[i-1]-c)+3)%6;
   var (shape,yaw)=Shape(new HashSet<int>{a,b});
   if(shape=="A"&&i%2==1)shape="A_curvy";
   Place("tiles/rivers/hex_river_"+shape,water,World(c),yaw).name=$"Rio {i+1:00} [{c.x}, {c.y}]";
   if(map[c]==Kind.Bridge){Place("buildings/neutral/building_bridge_A",water,World(c),yaw).name="Puente";continue;}
   var block=Pillar(colliders,"Rio",World(c),1.05f,WaterLayer);
   var modifier=block.AddComponent<NavMeshModifier>();modifier.overrideArea=true;modifier.area=NotWalkable;
   if(rng.NextDouble()<.25)Place("decoration/nature/waterlily_"+(rng.Next(2)==0?"A":"B"),detail,World(c)+new Vector3(.3f,-.095f,.2f),rng.Next(6)*60,1.1f);
  }
  foreach(var pair in roads){
   if(map[pair.Key]==Kind.Bridge)continue;
   var (shape,yaw)=Shape(pair.Value);
   Place("tiles/roads/hex_road_"+shape,paths,World(pair.Key),yaw).name=$"Camino [{pair.Key.x}, {pair.Key.y}]";
  }
  foreach(var pair in map){
   var c=pair.Key;var p=World(c);float yaw=rng.Next(6)*60;
   switch(pair.Value){
    case Kind.Grass:Place("tiles/base/hex_grass",ground,p).name=$"Hex [{c.x}, {c.y}]";if(Open(map,c))Scatter(c,p,detail,colliders);break;
    case Kind.Dirt:Place("buildings/neutral/building_dirt",paths,p).name=$"Pista [{c.x}, {c.y}]";break;
    case Kind.Forest:
     Place("tiles/base/hex_grass",ground,p).name=$"Hex [{c.x}, {c.y}]";
     Place("decoration/nature/"+ForestModels[rng.Next(ForestModels.Length)],forest,p,yaw,.9f+(float)rng.NextDouble()*.2f);
     Pillar(colliders,"Selva",p,.9f,0);break;
    case Kind.Mountain:
     Place("tiles/base/hex_grass",ground,p);
     Place("decoration/nature/"+MountainModels[rng.Next(MountainModels.Length)],edge,p,yaw,1+(float)rng.NextDouble()*.25f);
     if(Dist(c)<=Radius+1)Pillar(colliders,"Montana",p,1.05f,0);break;
    case Kind.Building:Place("tiles/base/hex_grass",ground,p);break;
   }
  }
  foreach(var b in Camp){var p=World(Cell(b.at));Place("buildings/red/"+b.prefab,camp,p,180+rng.Next(3)*60-60);Pillar(colliders,"Edificio",p,.9f,0);}
  // Coberturas del campamento y de la pista.
  string[] cover={"crate_A_big","crate_B_big","barrel","crate_long_A","haybale","sack"};
  var reserved=OrgSpawns.Concat(Entries.Select(e=>Snap(e.at))).Concat(Hideouts.Select(Snap)).Concat(ExtraZones.Select(Snap)).ToList();
  void Cover(Vector3 at){if(reserved.Any(r=>Vector3.Distance(r,at)<2.4f))return;var c=Cell(at);if(!map.TryGetValue(c,out var k)||k==Kind.Building||k==Kind.Forest||k==Kind.River)return;Place("decoration/props/"+cover[rng.Next(cover.Length)],camp,at,rng.Next(12)*30,2.6f);Box(colliders,"Cobertura",at+Vector3.up*.4f,new Vector3(.8f,.8f,.8f),0);}
  for(int i=0;i<14;i++){float angle=i*Mathf.PI*2/14;float radius=6.5f+(float)rng.NextDouble()*4.5f;Cover(new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius));}
  for(int i=0;i<10;i++)Cover(new Vector3(Mathf.Lerp(Strip.xMin+1,Strip.xMax-1,(float)rng.NextDouble()),0,Mathf.Lerp(Strip.yMin+1,Strip.yMax-1,(float)rng.NextDouble())));
  foreach(var h in Hideouts){var p=Snap(h);Place("decoration/props/tent",camp,p+new Vector3(.7f,0,.6f),rng.Next(6)*60,1.8f);Box(colliders,"Tienda",p+new Vector3(.7f,.4f,.6f),new Vector3(.8f,.8f,.8f),0);}
  StaticFlags(root);
 }
 // Solo hay adornos con colisión donde los 6 vecinos son transitables: nunca cierran un pasillo.
 static bool Open(Dictionary<Vector2Int,Kind> map,Vector2Int c)=>Directions.All(d=>map.TryGetValue(c+d,out var k)&&(k==Kind.Grass||k==Kind.Road||k==Kind.Dirt));
 // Árboles sueltos y rocas: algo de cobertura en el pasto abierto.
 static void Scatter(Vector2Int c,Vector3 p,Transform detail,Transform colliders){
  if(Dist(c)>=Radius-1||rng.NextDouble()>.07)return;
  var near=Entries.Select(e=>Cell(e.at)).Concat(Hideouts.Select(Cell)).Concat(ExtraZones.Select(Cell)).Concat(OrgSpawns.Select(Cell));
  if(near.Any(n=>Dist(n-c)<=1))return;
  var offset=new Vector3((float)rng.NextDouble()-.5f,0,(float)rng.NextDouble()-.5f);
  bool tree=rng.NextDouble()<.6;
  Place(tree?"decoration/nature/tree_single_"+(rng.Next(2)==0?"A":"B"):"decoration/nature/rock_single_"+"ABCDE"[rng.Next(5)],detail,p+offset,rng.Next(6)*60,tree?.9f:2.4f);
  Box(colliders,tree?"Arbol":"Roca",p+offset+Vector3.up*.5f,tree?new Vector3(.45f,1,.45f):new Vector3(.6f,.6f,.6f),0);
 }
 static void CreateViewpoint(){
  var eye=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));eye.tag="MainCamera";
  var camera=eye.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=LanNightRules.CameraSize;camera.nearClipPlane=.1f;camera.farClipPlane=300;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.16f,.23f,.27f);
  var sun=new GameObject("Luz principal").AddComponent<Light>();sun.type=LightType.Directional;sun.color=new Color(1,.91f,.77f);sun.intensity=1.65f;sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(48,-35,0);
  RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.64f,.73f,.8f);RenderSettings.ambientEquatorColor=new Color(.45f,.53f,.52f);RenderSettings.ambientGroundColor=new Color(.24f,.29f,.31f);RenderSettings.fog=false;
  new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
 }
 static void BakeNavigation(){
  var surface=new GameObject("Navegacion").AddComponent<NavMeshSurface>();
  surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;surface.overrideVoxelSize=true;surface.voxelSize=.12f;surface.BuildNavMesh();
  if(!surface.navMeshData||NavMesh.CalculateTriangulation().vertices.Length==0)throw new Exception("La navegación quedó vacía");
  if(File.Exists(NavPath))AssetDatabase.DeleteAsset(NavPath);
  AssetDatabase.CreateAsset(surface.navMeshData,NavPath);
 }
 static void Validate(LanWorld world){
  if(!NavMesh.SamplePosition(Vector3.zero,out var center,3,NavMesh.AllAreas))throw new Exception("El centro no tiene navegación");
  foreach(var point in world.deaEntries.Concat(world.orgSpawns).Concat(world.zones)){
   var path=new NavMeshPath();
   bool ok=NavMesh.SamplePosition(point.position,out var hit,1.5f,NavMesh.AllAreas)&&NavMesh.CalculatePath(center.position,hit.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
   if(!ok)throw new Exception("Punto sin conexión: "+point.name);
  }
 }
 static void EnsureBuildList(){
  if(!File.Exists(ScenePath))return;
  var scenes=EditorBuildSettings.scenes.ToList();
  if(scenes.Any(s=>s.path==ScenePath))return;
  scenes.RemoveAll(s=>s.path==OldArena);
  scenes.Add(new EditorBuildSettingsScene(ScenePath,true));
  EditorBuildSettings.scenes=scenes.ToArray();
 }

 // ---------- Hexágonos (coordenadas axiales: X = 2q + r, Z = √3 r) ----------
 static Vector3 World(Vector2Int c)=>new Vector3(2*c.x+c.y,0,Mathf.Sqrt(3)*c.y);
 static Vector3 Snap(Vector3 p)=>World(Cell(p))+Vector3.up*.05f;
 static int Dist(Vector2Int c)=>(Mathf.Abs(c.x)+Mathf.Abs(c.y)+Mathf.Abs(c.x+c.y))/2;
 static Vector2Int Cell(Vector3 p){float r=p.z/Mathf.Sqrt(3);return Round((p.x-r)/2,r);}
 static Vector2Int Round(float q,float r){
  float s=-q-r;int rq=Mathf.RoundToInt(q),rr=Mathf.RoundToInt(r),rs=Mathf.RoundToInt(s);
  float dq=Mathf.Abs(rq-q),dr=Mathf.Abs(rr-r),ds=Mathf.Abs(rs-s);
  if(dq>dr&&dq>ds)rq=-rr-rs;else if(dr>ds)rr=-rq-rs;
  return new Vector2Int(rq,rr);
 }
 static List<Vector2Int> Chain(params Vector2Int[] points){
  var cells=new List<Vector2Int>{points[0]};
  for(int i=1;i<points.Length;i++){
   var a=points[i-1];var b=points[i];int n=Dist(b-a);
   for(int j=1;j<=n;j++){float t=(float)j/n;var c=Round(Mathf.Lerp(a.x,b.x,t)+1e-4f,Mathf.Lerp(a.y,b.y,t)+2e-4f);if(c!=cells[cells.Count-1])cells.Add(c);}
  }
  return cells;
 }
 static int Direction(Vector2Int delta){for(int i=0;i<6;i++)if(Directions[i]==delta)return i;throw new Exception("Hexágonos no contiguos: "+delta);}
 static (string,float) Shape(HashSet<int> ports){
  for(int j=0;j<Ports.Length;j++)for(int k=0;k<6;k++)if(ports.SetEquals(Ports[j].Select(x=>(x+k)%6)))return (((char)('A'+j)).ToString(),-60*k);
  throw new Exception("No hay pieza para las conexiones "+string.Join(",",ports));
 }

 // ---------- Utilidades ----------
 static GameObject Place(string relative,Transform parent,Vector3 at,float yaw=0,float scale=1){
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Kit+relative+".prefab");
  if(!prefab)throw new Exception("Falta el prefab "+relative);
  var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);
  go.transform.SetPositionAndRotation(at,Quaternion.Euler(0,yaw,0));go.transform.localScale=Vector3.one*scale;
  return go;
 }
 static GameObject Box(Transform parent,string name,Vector3 center,Vector3 size,int layer){
  var go=new GameObject(name,typeof(BoxCollider));go.transform.SetParent(parent);go.transform.position=center;go.GetComponent<BoxCollider>().size=size;go.layer=layer;go.isStatic=true;
  return go;
 }
 // Cilindro que encaja en el hexágono: deja pasillos de 1,6 unidades en cualquier dirección.
 static GameObject Pillar(Transform parent,string name,Vector3 at,float radius,int layer){
  var go=new GameObject(name,typeof(CapsuleCollider));go.transform.SetParent(parent);go.transform.position=at+Vector3.up*1.5f;
  var capsule=go.GetComponent<CapsuleCollider>();capsule.radius=radius;capsule.height=3;go.layer=layer;go.isStatic=true;
  return go;
 }
 static Transform Group(Transform parent,string name){var go=new GameObject(name);go.transform.SetParent(parent);return go.transform;}
 static Transform Point(Transform parent,string name,Vector3 at){var go=new GameObject(name);go.transform.SetParent(parent);go.transform.position=at;return go.transform;}
 static void StaticFlags(Transform root){
  foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true)){
   GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,StaticEditorFlags.BatchingStatic);
   if(renderer.transform.parent&&renderer.transform.parent.name=="01 - Suelo"||renderer.name.StartsWith("hex_"))renderer.shadowCastingMode=ShadowCastingMode.Off;
  }
 }
}
