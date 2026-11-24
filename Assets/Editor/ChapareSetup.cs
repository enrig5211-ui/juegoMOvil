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
 const string ScenePath="Assets/Scenes/NocheChapare.unity",NavPath="Assets/Resources/Multiplayer/ChapareNavMesh.asset",OldArena="Assets/Scenes/ValleMultijugador.unity",MapImage="Assets/Resources/Multiplayer/MapaTactico.png";
 const float MapSize=136;
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
 // Segundo anillo del pueblo: casas típicas hechas en Blender alrededor del campamento, lejos de los caminos.
 static readonly (string model,Vector3 at)[] Town={
  ("Casa de ladrillo",new Vector3(4,0,11)),("Casa de madera pintada",new Vector3(8.5f,0,9.5f)),("Choza de motacu",new Vector3(12.5f,0,11)),("Casa de adobe con corredor",new Vector3(-4,0,11)),
  ("Tienda de barrio",new Vector3(-8.5f,0,10)),("Casa encalada",new Vector3(12,0,4.5f)),("Casa de tejas",new Vector3(16,0,5)),("Palafito",new Vector3(12,0,-4.5f)),
  ("Casa de ladrillo",new Vector3(16,0,-5)),("Choza de motacu",new Vector3(7,0,-10)),("Casa de madera pintada",new Vector3(10.5f,0,-12.5f)),("Casa de adobe con corredor",new Vector3(-7,0,-10)),
  ("Casa de tejas",new Vector3(-10.5f,0,-12.5f)),("Casa de ladrillo",new Vector3(-13,0,4.5f)),("Choza de motacu",new Vector3(-12.5f,0,-4.5f))};
 static readonly Dictionary<string,(string model,float size)> Bolivian=new Dictionary<string,(string,float)>{
  {"building_townhall_red",("Sede sindical",1.05f)},{"building_tavern_red",("Tienda de barrio",1)},{"building_market_red",("Casa de ladrillo",1)},{"building_home_A_red",("Casa de madera pintada",1)},
  {"building_home_B_red",("Choza de motacu",1)},{"building_barracks_red",("Casa de adobe con corredor",1)},{"building_watchtower_red",("Tanque de agua",1)},{"building_well_red",("Pozo",1)}};
 // Selva tropical del Chapare (modelos de Blender: palmeras, árboles de copa ancha, plataneros y matorrales).
 static readonly string[] ForestModels={"Selva A","Selva B","Selva C"};
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
  var lamps=CreateLamps(root,colliders);
  DecorateStrip(root,colliders);
  BakeNavigation();
  var world=new GameObject("PARTIDA - Noche").AddComponent<LanWorld>();
  world.deaEntries=Entries.Select(e=>Point(world.transform,e.name,Snap(e.at))).ToArray();
  world.orgSpawns=OrgSpawns.Select((p,i)=>Point(world.transform,"Organizacion "+(i+1),p)).ToArray();
  world.hideouts=Hideouts.Select((p,i)=>Point(world.transform,"Refugio "+(i+1),Snap(p))).ToArray();
  world.zones=world.hideouts.Concat(ExtraZones.Select((p,i)=>Point(world.transform,"Zona "+(i+1),Snap(p)))).ToArray();
  world.itemSpots=spotCells.Select((c,i)=>Point(world.transform,"Item "+(i+1),World(c)+Vector3.up*.05f)).ToArray();
  world.lamps=lamps;
  world.props=colliders.GetComponentsInChildren<LanProp>(true);
  Validate(world);
  world.mapTexture=RenderTacticalMap(map);world.mapCenter=Vector3.zero;world.mapSize=MapSize;
  if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new Exception("No se pudo guardar "+ScenePath);
  EnsureBuildList();AssetDatabase.SaveAssets();
  var counts=map.Values.GroupBy(k=>k).OrderBy(g=>g.Key).Select(g=>$"{g.Key} {g.Count()}");
  Debug.Log($"Mapa del Chapare creado: {ScenePath} · {map.Count} hexágonos · {world.itemSpots.Length} puntos de items · {world.props.Length} destructibles · "+string.Join(", ",counts));
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
  foreach(var t in Town)if(map[Cell(t.at)]==Kind.Grass)map[Cell(t.at)]=Kind.Building;
  var keep=new HashSet<Vector2Int>();
  void Clear(Vector2Int center,int radius){foreach(var c in map.Keys)if(Dist(c-center)<=radius)keep.Add(c);}
  Clear(Vector2Int.zero,9);foreach(var e in Entries)Clear(Cell(e.at),3);foreach(var h in Hideouts)Clear(Cell(h),1);foreach(var z in ExtraZones)Clear(Cell(z),1);
  foreach(var b in bridges)foreach(var d in Directions)Clear(b+d,1);
  // Selva agrupada con ruido; los refugios quedan rodeados de árboles.
  foreach(var c in map.Keys.ToList()){
   if(map[c]!=Kind.Grass||keep.Contains(c))continue;
   var p=World(c);float noise=Mathf.PerlinNoise(p.x*.055f+31.7f,p.z*.055f+12.3f);
   bool ring=Hideouts.Any(h=>Dist(c-Cell(h))==2);
   if(noise>.46f||ring&&rng.NextDouble()<.8)map[c]=Kind.Forest;
  }
  Connect(map);
  ChooseSpots(map);
  return map;
 }
 // Puntos de items: en hexágonos transitables, cerca de lugares de interés.
 static readonly Vector3[] SpotHints={new Vector3(0,0,-5),new Vector3(-5,0,-1),new Vector3(5,0,-1),new Vector3(0,0,4),new Vector3(40,0,8),new Vector3(42,0,-6),new Vector3(2,0,-20),new Vector3(2,0,-32),new Vector3(-48,0,-8),new Vector3(-46,0,16),new Vector3(-8,0,30),new Vector3(14,0,36),new Vector3(24,0,0),new Vector3(20,0,-12),new Vector3(-14,0,8),new Vector3(-12,0,-12)};
 static readonly List<Vector2Int> spotCells=new List<Vector2Int>();
 static void ChooseSpots(Dictionary<Vector2Int,Kind> map){
  spotCells.Clear();
  var avoid=Entries.Select(e=>Cell(e.at)).Concat(OrgSpawns.Select(Cell)).ToList();
  var hints=SpotHints.Concat(Hideouts.Select(h=>h+new Vector3(-2,0,0)));
  foreach(var hint in hints){
   var c=Cell(hint);
   if(!map.TryGetValue(c,out var k)||k!=Kind.Grass&&k!=Kind.Road&&k!=Kind.Dirt||Dist(c)>=Radius-1||avoid.Any(a=>Dist(a-c)<=1)||spotCells.Contains(c))continue;
   spotCells.Add(c);
  }
 }
 // Faroles del campamento: iluminan la zona de noche, así que también delatan a quien pase.
 static readonly Vector3[] LampSpots={new Vector3(-3,0,-3.5f),new Vector3(3,0,-3.5f),new Vector3(-5,0,2),new Vector3(5,0,2)};
 static readonly Vector3 LampHead=new Vector3(0,1.0f,.26f);const float LampYaw=0;
 static Transform[] CreateLamps(Transform root,Transform colliders){
  var group=Group(root,"08 - Faroles");
  return LampSpots.Select((p,i)=>{
   // La lámpara cuelga hacia el centro del campamento.
   var post=Model("Farol",group,p,Mathf.Atan2(-p.x,-p.z)*Mathf.Rad2Deg+LampYaw);post.name="Farol "+(i+1);
   Prop(Box(colliders,"Farol",p+Vector3.up*.8f,new Vector3(.3f,1.6f,.3f),0),post,LanPropKind.Lamp);
   var light=new GameObject("Luz").AddComponent<Light>();light.transform.SetParent(post.transform,false);light.transform.localPosition=LampHead;
   light.type=LightType.Point;light.range=LanNightRules.LampPostRadius+2;light.intensity=4;light.color=new Color(1,.75f,.45f);light.shadows=LightShadows.None;
   return post.transform;
  }).ToArray();
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
    case Kind.Grass:Place("tiles/base/hex_grass",ground,p).name=$"Hex [{c.x}, {c.y}]";if(Open(map,c))Scatter(c,p,detail,colliders);Clutter(map,c,p,detail);break;
    case Kind.Dirt:Place("buildings/neutral/building_dirt",paths,p).name=$"Pista [{c.x}, {c.y}]";break;
    case Kind.Forest:
     Place("tiles/base/hex_grass",ground,p).name=$"Hex [{c.x}, {c.y}]";
     var trees=Model(ForestModels[rng.Next(ForestModels.Length)],forest,p,yaw,.9f+(float)rng.NextDouble()*.2f);
     Prop(Pillar(colliders,"Selva",p,.9f,0),trees,LanPropKind.Forest);break;
    case Kind.Mountain:
     Place("tiles/base/hex_grass",ground,p);
     Place("decoration/nature/"+MountainModels[rng.Next(MountainModels.Length)],edge,p,yaw,1+(float)rng.NextDouble()*.25f);
     if(Dist(c)<=Radius+1)Pillar(colliders,"Montana",p,1.05f,0);break;
    case Kind.Building:Place("tiles/base/hex_grass",ground,p);break;
   }
  }
  // Campamento con arquitectura boliviana a escala de los personajes; cada casa mira al centro.
  foreach(var b in Camp){var p=World(Cell(b.at));rng.Next(3);var (model,size)=Bolivian[b.prefab];var face=-p;float houseYaw=Mathf.Atan2(face.x,face.z)*Mathf.Rad2Deg;var house=Model(model,camp,p,houseYaw);house.transform.localScale=Vector3.one*size;Pillar(colliders,"Edificio",p,.9f,0);
   if(model!="Tanque de agua"&&model!="Pozo")Model("Cantaros",camp,p+Quaternion.Euler(0,houseYaw,0)*new Vector3((rng.Next(2)*2-1)*1.02f*size,0,.45f*size),rng.Next(12)*30);}
  foreach(var t in Town){var c=Cell(t.at);if(map[c]!=Kind.Building)continue;var p=World(c);float houseYaw=Mathf.Atan2(-p.x,-p.z)*Mathf.Rad2Deg+rng.Next(-1,2)*15;
   Model(t.model,camp,p,houseYaw);Pillar(colliders,"Edificio",p,.9f,0);
   if(rng.NextDouble()<.7)Model("Cantaros",camp,p+Quaternion.Euler(0,houseYaw,0)*new Vector3((rng.Next(2)*2-1)*1.05f,0,.5f),rng.Next(12)*30);}
  // Coberturas del campamento y de la pista.
  string[] cover={"crate_A_big","crate_B_big","barrel","@Cajas de platano","@Carga con aguayo","sack"};
  var reserved=OrgSpawns.Concat(Entries.Select(e=>Snap(e.at))).Concat(Hideouts.Select(Snap)).Concat(ExtraZones.Select(Snap)).Concat(spotCells.Select(World)).Concat(LampSpots).ToList();
  void Cover(Vector3 at){if(reserved.Any(r=>Vector3.Distance(r,at)<2.4f))return;var c=Cell(at);if(!map.TryGetValue(c,out var k)||k==Kind.Building||k==Kind.Forest||k==Kind.River)return;var model=cover[rng.Next(cover.Length)];var visual=model[0]=='@'?Model(model.Substring(1),camp,at,rng.Next(12)*30):Place("decoration/props/"+model,camp,at,rng.Next(12)*30,2.6f);Prop(Box(colliders,"Cobertura",at+Vector3.up*.4f,new Vector3(.8f,.8f,.8f),0),visual,model=="barrel"?LanPropKind.Barrel:LanPropKind.Crate);}
  for(int i=0;i<14;i++){float angle=i*Mathf.PI*2/14;float radius=6.5f+(float)rng.NextDouble()*4.5f;Cover(new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius));}
  for(int i=0;i<10;i++)Cover(new Vector3(Mathf.Lerp(Strip.xMin+1,Strip.xMax-1,(float)rng.NextDouble()),0,Mathf.Lerp(Strip.yMin+1,Strip.yMax-1,(float)rng.NextDouble())));
  foreach(var h in Hideouts){var p=Snap(h);var tent=Model("Carpa azul",camp,p+new Vector3(.7f,0,.6f),rng.Next(6)*60);Prop(Box(colliders,"Tienda",p+new Vector3(.7f,.4f,.6f),new Vector3(.8f,.8f,.8f),0),tent,LanPropKind.Tent);}
  // Animales y carreta del campamento, a la escala de los personajes.
  foreach(var (prefab,at,yaw) in new[]{("@Vaca cebu",new Vector3(11,0,8),40f),("@Vaca cebu",new Vector3(12.5f,0,6.5f),200f),("@Chancho",new Vector3(-12,0,-9),120f),("@Mototaxi",new Vector3(-10,0,-11),80f),("@Mototaxi",new Vector3(-7.6f,0,1.4f),150f)}){
   var cell=Cell(at);if(!map.TryGetValue(cell,out var kind)||kind!=Kind.Grass&&kind!=Kind.Road||reserved.Any(r=>Vector3.Distance(r,at)<2.4f))continue;
   bool moto=prefab=="@Mototaxi";
   (prefab[0]=='@'?Model(prefab.Substring(1),camp,at,yaw):Place(prefab,camp,at,yaw,1.6f)).name=prefab[0]=='@'?prefab.Substring(1):"Caballo";
   var block=Box(colliders,moto?"Mototaxi":"Animal",at+Vector3.up*(moto?.3f:.5f),moto?new Vector3(.3f,.6f,.7f):new Vector3(.45f,1,1.2f),0);block.transform.rotation=Quaternion.Euler(0,yaw,0);
  }
  Bolivia(map,river,roads,bridges,camp,detail,colliders,reserved);
  StaticFlags(root);
 }
 // Toques bolivianos: banderas junto a la sede, puestos de mercado en el camino del sur y canoas junto a los puentes.
 static void Bolivia(Dictionary<Vector2Int,Kind> map,List<Vector2Int> river,Dictionary<Vector2Int,HashSet<int>> roads,List<Vector2Int> bridges,Transform camp,Transform detail,Transform colliders,List<Vector3> reserved){
  bool Free(Vector3 at,float gap)=>map.TryGetValue(Cell(at),out var k)&&k==Kind.Grass&&!reserved.Any(r=>Vector3.Distance(r,at)<gap);
  foreach(var (model,at) in new[]{("Bandera Bolivia",new Vector3(2.2f,0,5.6f)),("Wiphala",new Vector3(3.1f,0,6.9f))}){
   if(!Free(at,1.6f))continue;
   Model(model,camp,at,90);Box(colliders,"Mastil",at+Vector3.up*1f,new Vector3(.25f,2f,.25f),0);
  }
  // Puestos con toldo azul y aguayo a un lado del camino; junto a cada uno, cajas de plátano que sirven de cobertura.
  var south=roads.Keys.Where(c=>map[c]==Kind.Road&&World(c).z<-8&&World(c).z>-24).OrderByDescending(c=>World(c).z).ToList();
  int stalls=0;
  for(int i=1;i<south.Count&&stalls<3;i+=3){
   var c=south[i];
   foreach(var d in Directions){
    var n=c+d;var toRoad=World(c)-World(n);var at=World(n)+toRoad*.3f;
    if(roads.ContainsKey(n)||!Free(at,2.2f)||Dist(n)>=Radius-1)continue;
    float yaw=Mathf.Atan2(toRoad.x,toRoad.z)*Mathf.Rad2Deg;
    Model("Puesto de mercado",camp,at,yaw);
    var block=Box(colliders,"Puesto",at+Vector3.up*.45f,new Vector3(1.1f,.9f,.8f),0);block.transform.rotation=Quaternion.Euler(0,yaw,0);
    var side=at+Quaternion.Euler(0,yaw,0)*new Vector3(.95f,0,-.15f);
    if(Free(side,1.2f)){var crates=Model("Cajas de platano",camp,side,yaw+rng.Next(-2,3)*10);Prop(Box(colliders,"Cobertura",side+Vector3.up*.4f,new Vector3(.8f,.8f,.8f),0),crates,LanPropKind.Crate);}
    reserved.Add(at);stalls++;break;
   }
  }
  // Canoas con motor peque-peque, amarradas dos hexágonos río arriba y río abajo de cada puente.
  // Solo en tramos rectos sin curva (índice par), donde el agua pasa por el centro del hexágono.
  bool boatDocked=false;
  foreach(var bridge in bridges){
   int b=river.IndexOf(bridge);
   foreach(int side in new[]{-1,1})for(int k=2;k<=5;k++){
    int i=b+side*k;
    if(i<=0||i>=river.Count-1||i%2!=0||map[river[i]]!=Kind.River||!Straight(river,i))continue;
    var flow=World(river[i+1])-World(river[i-1]);var across=Vector3.Cross(Vector3.up,flow.normalized);
    // En el puente del medio, la primera embarcación es una lancha.
    bool boat=bridge==bridges[1]&&!boatDocked;boatDocked|=boat;
    Model(boat?"Lancha":"Canoa",detail,World(river[i])+across*side*.08f+Vector3.down*(boat?.14f:.1f),Mathf.Atan2(flow.x,flow.z)*Mathf.Rad2Deg+side*8);
    break;
   }
  }
  Village(map,roads,camp,detail,colliders,reserved);
 }
 // Pueblo y selva: capilla, secaderos de coca y fosas junto a los refugios, tendederos, cancha, animales sueltos,
 // árboles bibosí al borde de la selva y vehículos estacionados junto a los caminos.
 static void Village(Dictionary<Vector2Int,Kind> map,Dictionary<Vector2Int,HashSet<int>> roads,Transform camp,Transform detail,Transform colliders,List<Vector3> reserved){
  // Pasto libre y sin selva al lado (los árboles de la selva taparían el objeto).
  bool Free(Vector3 at,float gap)=>map.TryGetValue(Cell(at),out var k)&&k==Kind.Grass&&Dist(Cell(at))<Radius-1&&!reserved.Any(r=>Vector3.Distance(r,at)<gap)&&!Directions.Any(d=>map.TryGetValue(Cell(at)+d,out var n)&&n==Kind.Forest);
  // Centro de hexágono de pasto libre más cercano a un punto (busca en anillos de hasta 5 hexágonos).
  Vector3? Spot(Vector3 target,float gap){
   var start=Cell(target);
   for(int ring=0;ring<=5;ring++)foreach(var c in map.Keys.Where(c=>Dist(c-start)==ring).OrderBy(c=>(World(c)-target).sqrMagnitude)){var at=World(c);if(Free(at,gap))return at;}
   return null;
  }
  float Face(Vector3 at,Vector3 look){var d=look-at;return Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;}
  GameObject Put(string model,Vector3 target,float gap,Vector3 block,Transform parent,float? yaw=null){
   var spot=Spot(target,gap);if(spot==null)return null;var at=spot.Value;float y=yaw??Face(at,Vector3.zero);
   var go=Model(model,parent,at,y);reserved.Add(at);
   if(block!=Vector3.zero){var b=Box(colliders,model,at+Vector3.up*block.y*.5f,block,0);b.transform.rotation=Quaternion.Euler(0,y,0);}
   return go;
  }
  var chapel=Put("Capilla",new Vector3(-11,0,6),2.6f,Vector3.zero,camp);if(chapel)Pillar(colliders,"Capilla",chapel.transform.position,.9f,0);
  for(int i=0;i<Hideouts.Length;i++){
   var h=Hideouts[i];var outward=new Vector3(h.x,0,h.z).normalized;
   if(i%2==0)Put("Secadero de coca",h+outward*3f,2f,Vector3.zero,camp,rng.Next(6)*60);
   else Put("Fosa de maceracion",h+outward*3f,2f,new Vector3(2.1f,.5f,1.1f),camp,rng.Next(6)*60);
  }
  foreach(var t in new[]{new Vector3(-9.5f,0,-1.5f),new Vector3(9.5f,0,-1.5f),new Vector3(-6,0,8.5f)})Put("Tendedero",t,1.6f,Vector3.zero,camp,rng.Next(6)*60);
  // Cancha de tierra: dos arcos enfrentados.
  var pitch=Spot(new Vector3(-14,0,-4),2.5f);
  if(pitch!=null){var a=pitch.Value+Vector3.forward*3.4f;var b=pitch.Value+Vector3.back*3.4f;
   if(Free(a,1.6f)&&Free(b,1.6f)){Model("Arco de futbol",camp,a,0);Model("Arco de futbol",camp,b,180);reserved.Add(a);reserved.Add(b);}}
  foreach(var (model,t) in new[]{("Gallinas",new Vector3(-7,0,6)),("Gallinas",new Vector3(7.5f,0,-6.5f)),("Perro",new Vector3(3,0,-6)),("Perro",new Vector3(-21,0,20)),("Gallinas",new Vector3(22,0,25))})
   Put(model,t,1.4f,Vector3.zero,detail,rng.Next(12)*30);
  // Pueblo lleno: mercado al costado del camino del sur, tendederos, animales, carga en aguayo, tanques, pozo y mototaxis.
  foreach(var t in new[]{new Vector3(-3.5f,0,-8),new Vector3(-5.5f,0,-9.5f),new Vector3(4,0,-8.5f)})Put("Puesto de mercado",t,1.8f,new Vector3(1.1f,.9f,.8f),camp);
  for(int i=0;i<24;i++){
   float angle=i*137.5f*Mathf.Deg2Rad,r=8.5f+(i%5)*1.6f;var t=new Vector3(Mathf.Cos(angle)*r,0,Mathf.Sin(angle)*r);
   switch(i%8){
    case 0:Put("Tendedero",t,1.4f,Vector3.zero,camp,rng.Next(6)*60);break;
    case 1:Put(new[]{"Gallinas","Perro","Chancho"}[rng.Next(3)],t,1.2f,Vector3.zero,detail,rng.Next(12)*30);break;
    case 2:{var go=Put("Carga con aguayo",t,1.6f,Vector3.zero,camp,rng.Next(12)*30);if(go)Prop(Box(colliders,"Cobertura",go.transform.position+Vector3.up*.4f,new Vector3(.8f,.8f,.8f),0),go,LanPropKind.Crate);break;}
    case 3:{var go=Put("Cajas de platano",t,1.6f,Vector3.zero,camp,rng.Next(12)*30);if(go)Prop(Box(colliders,"Cobertura",go.transform.position+Vector3.up*.4f,new Vector3(.8f,.8f,.8f),0),go,LanPropKind.Crate);break;}
    case 4:Put(i<12?"Tanque de agua":"Pozo",t,2f,new Vector3(.8f,1.2f,.8f),camp);break;
    case 5:Put("Mototaxi",t,1.6f,new Vector3(.3f,.6f,.7f),camp,rng.Next(12)*30);break;
    case 6:Put(i<12?"Secadero de coca":"Tendedero",t,2f,Vector3.zero,camp,rng.Next(6)*60);break;
    default:Put("Gallinas",t,1.2f,Vector3.zero,detail,rng.Next(12)*30);break;
   }
  }
  // Bibosí: árboles gigantes en el pasto que toca la selva; se pueden derribar como un árbol.
  var edges=map.Keys.Where(c=>map[c]==Kind.Grass&&Dist(c)>5&&Dist(c)<Radius-3&&Directions.Count(d=>map.TryGetValue(c+d,out var k)&&k==Kind.Forest)>=2).OrderBy(c=>rng.Next()).ToList();
  int giants=0;
  foreach(var c in edges){if(giants>=8)break;var at=World(c);if(reserved.Any(r=>Vector3.Distance(r,at)<4))continue;
   var tree=Model("Bibosi",detail,at,rng.Next(6)*60,.9f+(float)rng.NextDouble()*.25f);reserved.Add(at);giants++;
   Prop(Box(colliders,"Arbol",at+Vector3.up*.8f,new Vector3(.6f,1.6f,.6f),0),tree,LanPropKind.Tree);}
  // Trufi y camión estacionados al costado del camino del este.
  var east=roads.Keys.Where(c=>map[c]==Kind.Road&&World(c).x>9&&World(c).x<30&&Mathf.Abs(World(c).z)<4).OrderBy(c=>World(c).x).ToList();
  int parked=0;
  foreach(var c in east){if(parked>=2)break;if(parked==0&&World(c).x<11||parked==1&&World(c).x<20)continue;
   foreach(var d in Directions){var n=c+d;if(roads.ContainsKey(n))continue;var at=World(n)+(World(c)-World(n))*.35f;if(!Free(at,2.4f))continue;
    string model=parked==0?"Trufi":"Camion de carga";Model(model,camp,at,90+rng.Next(-1,2)*8);reserved.Add(at);
    var b=Box(colliders,model,at+Vector3.up*.5f,parked==0?new Vector3(.8f,1,1.9f):new Vector3(.9f,1,2.9f),0);b.transform.rotation=Quaternion.Euler(0,90,0);parked++;break;}}
 }
 // Detalles pequeños sin colisión: piedras y plantas que llenan el pasto, y plantas de orilla junto al río.
 static void Clutter(Dictionary<Vector2Int,Kind> map,Vector2Int c,Vector3 p,Transform detail){
  bool bank=Directions.Any(d=>map.TryGetValue(c+d,out var k)&&(k==Kind.River||k==Kind.Bridge));
  if(rng.NextDouble()>(bank?.7:.3))return;
  var offset=new Vector3((float)rng.NextDouble()*1.2f-.6f,0,(float)rng.NextDouble()*1.2f-.6f);
  string model=bank?"decoration/nature/waterplant_"+"ABC"[rng.Next(3)]:rng.NextDouble()<.6?"decoration/nature/rock_single_"+"ABCDE"[rng.Next(5)]:"Coca";
  // En tierra firme, matas de coca en lugar de plantas de agua; junto a la selva, helechos, heliconias, tacuaras,
  // troncos caídos y termiteros en lugar de piedras.
  if(model!="Coca"&&!bank&&Directions.Any(d=>map.TryGetValue(c+d,out var k)&&k==Kind.Forest)&&rng.NextDouble()<.55)model=new[]{"Helechos","Heliconia","Tacuara","Tronco caido","Termitero","Helechos"}[rng.Next(6)];
  if(model[0]!='d'){Model(model,detail,p+offset,rng.Next(12)*30);return;}
  Place(model,detail,p+offset,rng.Next(12)*30,bank?1.4f:model.Contains("rock")?.9f+(float)rng.NextDouble()*.5f:1.2f);
 }
 // Solo hay adornos con colisión donde los 6 vecinos son transitables: nunca cierran un pasillo.
 static bool Open(Dictionary<Vector2Int,Kind> map,Vector2Int c)=>Directions.All(d=>map.TryGetValue(c+d,out var k)&&(k==Kind.Grass||k==Kind.Road||k==Kind.Dirt));
 // Árboles sueltos y rocas: algo de cobertura en el pasto abierto.
 static void Scatter(Vector2Int c,Vector3 p,Transform detail,Transform colliders){
  if(Dist(c)>=Radius-1||rng.NextDouble()>.2)return;
  var near=Entries.Select(e=>Cell(e.at)).Concat(Hideouts.Select(Cell)).Concat(ExtraZones.Select(Cell)).Concat(OrgSpawns.Select(Cell)).Concat(spotCells);
  if(near.Any(n=>Dist(n-c)<=1))return;
  var offset=new Vector3((float)rng.NextDouble()-.5f,0,(float)rng.NextDouble()-.5f);
  double pick=rng.NextDouble();
  if(pick<.45){var tree=Model("Palmera "+(rng.Next(2)==0?"A":"B"),detail,p+offset,rng.Next(6)*60,.8f+(float)rng.NextDouble()*.3f);Prop(Box(colliders,"Arbol",p+offset+Vector3.up*.5f,new Vector3(.45f,1,.45f),0),tree,LanPropKind.Tree);}
  else if(pick<.8){rng.Next(2);var bush=Model("Platanero",detail,p+offset*.5f,rng.Next(6)*60);Prop(Box(colliders,"Matorral",p+offset*.5f+Vector3.up*.5f,new Vector3(.9f,1,.9f),0),bush,LanPropKind.Bush);}
  else{var rock=Place("decoration/nature/rock_single_"+"ABCDE"[rng.Next(5)],detail,p+offset,rng.Next(6)*60,2.4f);Prop(Box(colliders,"Roca",p+offset+Vector3.up*.3f,new Vector3(.6f,.6f,.6f),0),rock,LanPropKind.Rock);}
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
  if(world.itemSpots.Length<12)throw new Exception("Muy pocos puntos de items: "+world.itemSpots.Length);
  foreach(var point in world.deaEntries.Concat(world.orgSpawns).Concat(world.zones).Concat(world.itemSpots)){
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
 // ---------- Pista clandestina ----------
 // Marcas pintadas, luces a los costados, manga de viento, una avioneta estacionada (destructible) y bidones.
 static void DecorateStrip(Transform root,Transform colliders){
  var group=Group(root,"09 - Pista");const float top=.055f;
  var center=new Vector3((Strip.xMin+Strip.xMax)*.5f,top,(Strip.yMin+Strip.yMax)*.5f);
  Model("Marcas de pista",group,center,0);
  for(float z=Strip.yMin+2;z<=Strip.yMax-2;z+=4)foreach(float x in new[]{Strip.xMin+.7f,Strip.xMax-.7f}){Model("Luz de pista",group,new Vector3(x,top,z),0);if(z+2<Strip.yMax-1)Model("Mechon",group,new Vector3(x,top,z+2),0);}
  // Llantas pintadas de blanco que marcan las cabeceras.
  for(float x=Strip.xMin+1.2f;x<=Strip.xMax-1.2f;x+=1.6f)foreach(float z in new[]{Strip.yMin+.6f,Strip.yMax-.6f})Model("Llanta de pista",group,new Vector3(x,top,z),0);
  foreach(var corner in new[]{new Vector3(Strip.xMin+.7f,0,Strip.yMin+2),new Vector3(Strip.xMax-.7f,0,Strip.yMin+2),new Vector3(Strip.xMin+.7f,0,Strip.yMax-2),new Vector3(Strip.xMax-.7f,0,Strip.yMax-2)}){
   var light=new GameObject("Luz de umbral").AddComponent<Light>();light.transform.SetParent(group,false);light.transform.position=corner+Vector3.up*.6f;
   light.type=LightType.Point;light.range=4;light.intensity=1.6f;light.color=new Color(1,.8f,.4f);light.shadows=LightShadows.None;
  }
  var planeAt=new Vector3(44.6f,top,11);var plane=Model("Avioneta Cessna",group,planeAt,205);
  var block=Box(colliders,"Avioneta",planeAt+Vector3.up*.7f,new Vector3(4.4f,1.4f,3.6f),0);block.transform.rotation=Quaternion.Euler(0,205,0);Prop(block,plane,LanPropKind.Plane);
  // Instalaciones de la pista: hangar, caseta de radio, torre de vigilancia, mesa con radio, generador, carga y camioneta.
  var extras=new List<(Vector3 at,float radius)>();
  void Site(string model,Vector3 at,float yaw,Vector3 size,LanPropKind? kind=null,float lift=0){
   var go=Model(model,group,at,yaw);extras.Add((at,Mathf.Max(size.x,size.z)*.5f+.9f));if(size==Vector3.zero)return;
   var b=Box(colliders,model,at+Vector3.up*(size.y*.5f+lift),size,0);b.transform.rotation=Quaternion.Euler(0,yaw,0);if(kind!=null)Prop(b,go,kind.Value);
  }
  Site("Hangar",new Vector3(46.2f,top,-12),-90,new Vector3(3f,1.6f,2.4f));
  Site("Caseta de radio",new Vector3(46.6f,top,-6.2f),-90,new Vector3(1f,1f,.9f));
  Site("Torre de vigilancia",new Vector3(35.6f,top,-16.8f),60,new Vector3(.9f,2.4f,.9f));
  Site("Torre de vigilancia",new Vector3(46.4f,top,17.4f),-120,new Vector3(.9f,2.4f,.9f));
  Site("Mesa con radio",new Vector3(45.6f,top,-3.6f),-90,new Vector3(.7f,.5f,.6f),LanPropKind.Crate);
  Site("Generador",new Vector3(46.8f,top,-4.6f),-90,new Vector3(.5f,.4f,.4f),LanPropKind.Crate);
  Site("Carga de paquetes",new Vector3(42.6f,top,8.4f),20,new Vector3(.75f,.6f,.6f),LanPropKind.Crate);
  Site("Carga de paquetes",new Vector3(41.6f,top,9.2f),-10,new Vector3(.75f,.6f,.6f),LanPropKind.Crate);
  Site("Carretilla",new Vector3(43.4f,top,7.4f),150,Vector3.zero);
  Site("Turriles con bomba",new Vector3(43.2f,top,14.6f),30,new Vector3(1f,.7f,.8f),LanPropKind.Barrel);
  Site("Camioneta 4x4",new Vector3(37.2f,top,6.5f),20,new Vector3(.85f,.9f,1.85f));
  var drums=new[]{new Vector3(41.4f,top,13.4f),new Vector3(41.9f,top,14.2f),new Vector3(41.1f,top,14.4f)};
  foreach(var at in drums){var drum=Place("decoration/props/barrel",group,at,0,2.4f);Prop(Box(colliders,"Bidon",at+Vector3.up*.35f,new Vector3(.5f,.7f,.5f),0),drum,LanPropKind.Barrel);}
  var sock=new Vector3(36,top,17.5f);Model("Manga de viento",group,sock,240);
  // Las coberturas sueltas que caían encima de la avioneta, los bidones o la manga se retiran.
  var keep=new[]{(planeAt,3.4f),(sock,1.4f)}.Concat(drums.Select(d=>(d,1.4f))).Concat(extras).ToArray();
  foreach(var prop in colliders.GetComponentsInChildren<LanProp>(true).ToArray()){
   if(prop.kind!=LanPropKind.Crate&&prop.kind!=LanPropKind.Barrel||prop.name!="Cobertura")continue;
   if(!keep.Any(k=>Vector3.Distance(new Vector3(k.Item1.x,0,k.Item1.z),new Vector3(prop.transform.position.x,0,prop.transform.position.z))<k.Item2))continue;
   UnityEngine.Object.DestroyImmediate(prop.visual.gameObject);UnityEngine.Object.DestroyImmediate(prop.gameObject);
  }
 }
 static GameObject Model(string name,Transform parent,Vector3 at,float yaw,float scale=1){
  var go=(GameObject)PrefabUtility.InstantiatePrefab(ModelSetup.Prefab(name),parent);go.transform.SetPositionAndRotation(at,Quaternion.Euler(0,yaw,0));go.transform.localScale=Vector3.one*scale;return go;
 }
 // Objeto destructible: la colisión lleva el componente; el modelo y su prefab sirven para los restos.
 static void Prop(GameObject collider,GameObject visual,LanPropKind kind){
  var prop=collider.AddComponent<LanProp>();prop.kind=kind;prop.visual=visual.transform;prop.source=PrefabUtility.GetCorrespondingObjectFromSource(visual);
 }
 // ---------- Mapa táctico ----------
 // Imagen cenital con paleta nocturna. El diseño del mapa da el fondo (terreno abierto, selva, montaña,
 // pista y campamento); una foto cenital con luz neutra aporta el río, los caminos y los árboles sueltos.
 // La usan el plan de despliegue, el minimapa y el mapa grande.
 static Texture2D RenderTacticalMap(Dictionary<Vector2Int,Kind> map){
  const int size=1024;
  var sun=UnityEngine.Object.FindObjectsByType<Light>().First(l=>l.type==LightType.Directional);
  var rotation=sun.transform.rotation;var color=sun.color;float intensity=sun.intensity;var shadows=sun.shadows;
  sun.transform.rotation=Quaternion.Euler(72,-30,0);sun.color=Color.white;sun.intensity=1.25f;sun.shadows=LightShadows.None;
  var eye=new GameObject("Camara del mapa").AddComponent<Camera>();eye.orthographic=true;eye.orthographicSize=MapSize*.5f;eye.clearFlags=CameraClearFlags.SolidColor;eye.backgroundColor=new Color(0,0,0,0);
  eye.nearClipPlane=.1f;eye.farClipPlane=300;eye.transform.SetPositionAndRotation(new Vector3(0,120,0),Quaternion.Euler(90,0,0));
  var target=new RenderTexture(size,size,24,RenderTextureFormat.ARGB32){antiAliasing=4};eye.targetTexture=target;eye.Render();eye.Render();
  RenderTexture.active=target;var shot=new Texture2D(size,size,TextureFormat.RGBA32,false);shot.ReadPixels(new Rect(0,0,size,size),0,0);shot.Apply();RenderTexture.active=null;
  eye.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(eye.gameObject);
  sun.transform.rotation=rotation;sun.color=color;sun.intensity=intensity;sun.shadows=shadows;
  var image=new Texture2D(size,size,TextureFormat.RGBA32,false);image.SetPixels(Paint(shot.GetPixels(),size,map));image.Apply();
  File.WriteAllBytes(MapImage,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(shot);UnityEngine.Object.DestroyImmediate(image);
  AssetDatabase.ImportAsset(MapImage);
  var importer=(TextureImporter)AssetImporter.GetAtPath(MapImage);importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.alphaIsTransparency=true;
  importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=1024;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
  return AssetDatabase.LoadAssetAtPath<Texture2D>(MapImage);
 }
 static readonly Color TOpen=new Color(.1f,.15f,.19f),TJungle=new Color(.05f,.2f,.13f),TRock=new Color(.17f,.16f,.18f),TRiverBed=new Color(.05f,.13f,.21f),TWater=new Color(.22f,.68f,.92f),
  TTrail=new Color(.86f,.66f,.32f),TStrip=new Color(.4f,.29f,.17f),TCamp=new Color(.42f,.16f,.14f),TRoof=new Color(.95f,.38f,.28f),TBush=new Color(.06f,.27f,.16f),TStone=new Color(.36f,.39f,.42f);
 static Color[] Paint(Color[] photo,int size,Dictionary<Vector2Int,Kind> map){
  var result=new Color[photo.Length];float unit=MapSize/size;
  for(int y=0;y<size;y++)for(int x=0;x<size;x++){
   int i=y*size+x;var world=new Vector3((x+.5f)*unit-MapSize*.5f,0,(y+.5f)*unit-MapSize*.5f);var cell=Cell(world);
   if(!map.TryGetValue(cell,out var kind)){result[i]=new Color(0,0,0,0);continue;}
   var local=world-World(cell);float edge=Mathf.Max(Mathf.Abs(local.x),Mathf.Abs(local.x*.5f+local.z*.866f),Mathf.Abs(-local.x*.5f+local.z*.866f));
   var c=photo[i];Color.RGBToHSV(c,out float h,out float s,out float v);
   bool water=c.b>c.r+.06f&&c.b>c.g+.02f&&s>.22f,trail=h>.02f&&h<.14f&&s>.12f&&v>.45f,roof=(h<.035f||h>.94f)&&s>.4f,tree=h>.2f&&h<.45f&&v<.62f&&s>.25f,stone=s<.12f&&v>.35f;
   float grain=Mathf.PerlinNoise(world.x*.9f,world.z*.9f);Color tone;
   switch(kind){
    case Kind.Forest:tone=TJungle*Mathf.Lerp(.75f,1.2f,Mathf.PerlinNoise(world.x*1.7f+5,world.z*1.7f));break;
    case Kind.Mountain:tone=TRock*Mathf.Lerp(.7f,1.35f,v);if(Dist(cell)>=Radius+1)tone*=.7f;break;
    case Kind.River:case Kind.Bridge:tone=water?Color.Lerp(TWater*.8f,TWater,v):kind==Kind.Bridge&&trail?TTrail*.8f:TRiverBed;break;
    case Kind.Dirt:tone=TStrip*Mathf.Lerp(.85f,1.1f,grain);break;
    case Kind.Building:tone=roof?TRoof:TCamp;break;
    default:tone=trail?TTrail*Mathf.Lerp(.75f,1,v):tree?TBush:stone?TStone:TOpen*Mathf.Lerp(.9f,1.08f,grain);break;
   }
   if(kind!=Kind.Building&&roof&&kind!=Kind.Dirt)tone=TRoof;
   // Rejilla hexagonal tenue, más marcada en la pista y el campamento.
   if(edge>.94f)tone=Color.Lerp(tone,new Color(.55f,.85f,.95f),kind==Kind.Dirt||kind==Kind.Building?.22f:.07f);
   tone.a=1;result[i]=tone;
  }
  return result;
 }
 // Solo rehace la imagen del mapa táctico sobre la escena ya creada (el diseño se recalcula con la misma semilla).
 [MenuItem("Tools/Multijugador/Rehacer mapa táctico")]
 public static void RebuildTacticalMap(){
  var scene=EditorSceneManager.OpenScene(ScenePath);var world=UnityEngine.Object.FindAnyObjectByType<LanWorld>();
  rng=new System.Random(Seed);var map=Layout(out _,out _,out _);
  world.mapTexture=RenderTacticalMap(map);world.mapCenter=Vector3.zero;world.mapSize=MapSize;EditorUtility.SetDirty(world);EditorSceneManager.SaveScene(scene);
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
