using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Valle.Lan;
// Guarda como prefabs editables los modelos low poly de LanModels (blindado, avioneta, suministro, mina,
// dinamita, manga de viento, luz y marcas de pista), con materiales propios en Assets/Art/Modelos.
public static class ModelSetup {
 public const string Art="Assets/Art/Modelos";
 [MenuItem("Tools/Multijugador/Crear modelos low poly")]
 public static void Build(){
  Directory.CreateDirectory(Art+"/Materiales");
  var previous=LanModels.Materials;LanModels.Materials=Material;
  try{
   Save(LanModels.Vehicle(null),"Blindado");Save(LanModels.Plane(null),"Avioneta");Save(LanModels.Supply(null),"Suministro");
   Save(LanModels.Mine(null),"Mina");
   Save(LanModels.Chuto(null),"Chuto");Save(LanModels.Helicopter(null),"Helicoptero");Save(LanModels.MedStation(null),"Estacion medica");Save(LanModels.Campfire(null),"Fogata");Save(LanModels.DroneStation(null),"Base de drones");
   Save(LanModels.House(null,"Casa de adobe","Adobe",false),"Casa de adobe");Save(LanModels.House(null,"Casa encalada","Cal",true),"Casa encalada");Save(LanModels.House(null,"Casa de tejas","Adobe",true),"Casa de tejas");
   Save(LanModels.StiltHouse(null),"Palafito");Save(LanModels.Shop(null),"Tienda");Save(LanModels.WaterTank(null),"Tanque de agua");Save(LanModels.Well(null),"Pozo");Save(LanModels.Drone(null),"Dron");Save(LanModels.Dynamite(null),"Dinamita");Save(LanModels.Windsock(null),"Manga de viento");
   Save(LanModels.RunwayLight(null),"Luz de pista");Save(LanModels.RunwayMarks(null,40,14),"Marcas de pista");
  }finally{LanModels.Materials=previous;}
  if(Directory.Exists(BlenderFolder))BuildBlender();
  AssetDatabase.SaveAssets();Debug.Log("Modelos low poly creados en "+Art);
 }
 public static GameObject Prefab(string name){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>($"{Art}/{name}.prefab");if(!prefab)throw new System.Exception("Falta el modelo "+name+": ejecuta Tools > Multijugador > Crear modelos low poly");return prefab;}
 static Material Material(string name){
  string path=$"{Art}/Materiales/{name}.mat";
  var material=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
  LanModels.Configure(material,name);EditorUtility.SetDirty(material);return material;
 }

 // ---------- Modelos hechos en Blender ----------
 // Los FBX de Assets/Art/Modelos/Blender usan los nombres de la paleta de LanModels como materiales: al importarlos se
 // cambian por los materiales del juego y se guarda un prefab a la escala de los personajes (0,55 de alto).
 // Vegetación de la selva tropical y objetos bolivianos (banderas, aguayo, mercado, mototaxi, canoa).
 public const string BlenderFolder=Art+"/Blender";
 static Vector3 V(float s)=>Vector3.one*s;
 static readonly Dictionary<string,Vector3> BlenderScale=new Dictionary<string,Vector3>{
  {"Palmera A",V(.6f)},{"Palmera B",V(.6f)},{"Platanero",V(.85f)},{"Coca",V(.8f)},
  {"Selva A",new Vector3(.72f,.6f,.72f)},{"Selva B",new Vector3(.68f,.6f,.68f)},{"Selva C",new Vector3(.75f,.62f,.75f)},
  {"Farol",V(.78f)},{"Bandera Bolivia",V(.8f)},{"Wiphala",V(.8f)},{"Puesto de mercado",V(.8f)},{"Carga con aguayo",V(.8f)},
  {"Cajas de platano",V(.85f)},{"Cantaros",V(.8f)},{"Carpa azul",V(.85f)},{"Mototaxi",V(.65f)},{"Canoa",V(.7f)},
  // Segunda tanda: en Blender el personaje mide 1, así que casi todo va a 0,55 (la altura de los personajes del juego).
  {"Casa de ladrillo",V(.5f)},{"Choza de motacu",V(.5f)},{"Casa de adobe con corredor",V(.5f)},{"Casa de madera pintada",V(.5f)},{"Tienda de barrio",V(.5f)},{"Capilla",V(.5f)},{"Sede sindical",V(.5f)},
  {"Avioneta Cessna",V(.72f)},{"Camioneta 4x4",V(.4f)},{"Trufi",V(.4f)},{"Camion de carga",V(.4f)},{"Lancha",V(.32f)},
  // Vehículos jugables: mismo tamaño que los de LanModels (el blindado además se reduce a VehicleScale en partida).
  {"Blindado DEA",V(.65f)},{"Chuto camioneta",V(.38f)},{"Chuto sicario",V(.38f)},{"Helicoptero DEA",V(.45f)}};
 const float DefaultScale=.55f;
 // El helicóptero se centra en su origen, como el de LanModels (vuela y gira alrededor de ese punto).
 static readonly Dictionary<string,Vector3> BlenderOffset=new Dictionary<string,Vector3>{{"Helicoptero DEA",new Vector3(0,-.65f,0)}};
 // Faros de los vehículos jugables (posición y alcance), como en LanModels.Vehicle.
 static readonly Dictionary<string,(Vector3 at,float x)> Headlights=new Dictionary<string,(Vector3,float)>{{"Blindado DEA",(new Vector3(0,.75f,1.55f),.49f)},{"Chuto camioneta",(new Vector3(0,.36f,.88f),.26f)},{"Chuto sicario",(new Vector3(0,.36f,.87f),.26f)}};
 [MenuItem("Tools/Multijugador/Importar modelos de Blender")]
 public static void BuildBlender()=>BuildBlenderModels(null);
 // only: nombres de los modelos a importar (null importa todos los FBX de la carpeta).
 public static void BuildBlenderModels(string[] only){
  int count=0;
  foreach(var file in Directory.GetFiles(BlenderFolder,"*.fbx").OrderBy(f=>f)){
   var asset=file.Replace('\\','/');var name=Path.GetFileNameWithoutExtension(asset);
   if(only!=null&&!only.Contains(name))continue;
   var importer=(ModelImporter)AssetImporter.GetAtPath(asset);
   importer.animationType=ModelImporterAnimationType.None;importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;importer.importBlendShapes=false;
   importer.globalScale=1;importer.useFileScale=true;importer.isReadable=false;importer.meshCompression=ModelImporterMeshCompression.Off;
   importer.materialImportMode=ModelImporterMaterialImportMode.ImportViaMaterialDescription;importer.materialLocation=ModelImporterMaterialLocation.InPrefab;
   foreach(var pair in importer.GetExternalObjectMap().ToList())importer.RemoveRemap(pair.Key);
   importer.SaveAndReimport();
   var names=AssetDatabase.LoadAllAssetsAtPath(asset).OfType<Material>().Select(m=>m.name).Distinct().ToList();
   foreach(var material in names){
    if(LanModels.ColorOf(material)==Color.magenta)Debug.LogWarning($"{name}: el material {material} no está en la paleta de LanModels");
    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),material),Material(material));
   }
   importer.SaveAndReimport();
   var root=new GameObject(name);
   var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(asset),root.transform);
   model.transform.localScale=BlenderScale.TryGetValue(name,out var scale)?scale:Vector3.one*DefaultScale;
   if(BlenderOffset.TryGetValue(name,out var offset))model.transform.localPosition=offset;
   // Modelos con piezas móviles (Torreta, Rueda, Rotor, Helice): las piezas pasan a ser hijas directas del prefab,
   // que es donde las busca el código del juego. Blender agrega ".001" a los nombres repetidos: se quita.
   if(model.transform.childCount>0&&!model.GetComponent<MeshFilter>()){
    PrefabUtility.UnpackPrefabInstance(model,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
    foreach(Transform part in model.transform.Cast<Transform>().ToList()){part.SetParent(root.transform,true);part.name=System.Text.RegularExpressions.Regex.Replace(part.name,@"\.\d+$","");}
    Object.DestroyImmediate(model);
   }
   if(Headlights.TryGetValue(name,out var lamp))foreach(int side in new[]{-1,1}){
    var beam=new GameObject("Luz del faro").AddComponent<Light>();beam.transform.SetParent(root.transform,false);beam.transform.localPosition=lamp.at+Vector3.right*side*lamp.x;beam.transform.localRotation=Quaternion.Euler(12,0,0);
    beam.type=LightType.Spot;beam.range=14;beam.spotAngle=70;beam.innerSpotAngle=40;beam.intensity=7;beam.color=new Color(1,.93f,.78f);beam.shadows=LightShadows.None;
   }
   Save(root.transform,name);count++;
  }
  AssetDatabase.SaveAssets();Debug.Log($"Modelos de Blender importados: {count} prefabs en {Art}");
 }
 static void Save(Transform root,string name){PrefabUtility.SaveAsPrefabAsset(root.gameObject,$"{Art}/{name}.prefab");Object.DestroyImmediate(root.gameObject);}
}
