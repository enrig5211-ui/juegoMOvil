using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Valle.Lan;
// Crea los accesorios 3D propios y arma los 7 personajes con las unidades y piezas de KayKit.
// Medidas de la unidad (escala 1): cabeza esfera r=0,081 centrada en y=0,26; cuerpo cono r=0,10 (y=0,05) a r=0,03 (y=0,18).
// El frente del personaje es +Z.
public static class CharacterSetup {
 const string Units="Assets/KayKit/Packs/KayKit - Medieval Hexagon Pack (for Unity)/Prefabs/units/";
 const string Art="Assets/Art/Personajes",Output="Assets/Resources/Multiplayer/Personajes";
 public const float Scale=1.6f;
 struct Part{public Mesh mesh;public Matrix4x4 matrix;public Material material;}
 static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
 static readonly Dictionary<PrimitiveType,Mesh> primitives=new Dictionary<PrimitiveType,Mesh>();

 [MenuItem("Tools/Multijugador/Crear personajes")]
 public static void Build(){
  Directory.CreateDirectory(Art+"/Materiales");Directory.CreateDirectory(Art+"/Mallas");Directory.CreateDirectory(Output);
  materials.Clear();primitives.Clear();
  var cap=Accessory("Gorra",Sphere(new Vector3(0,.285f,0),new Vector3(.172f,.12f,.172f),"Azul marino"),Cylinder(new Vector3(0,.3f,.075f),new Vector3(.14f,.006f,.1f),"Azul marino"),Cylinder(new Vector3(0,.335f,0),new Vector3(.03f,.006f,.03f),"Amarillo"));
  var vest=Accessory("Chaleco",Frustum(.104f,.056f,.07f,.155f,"Azul marino"),Frustum(.093f,.088f,.112f,.13f,"Amarillo",.0f));
  var suit=Accessory("Traje",Frustum(.104f,.054f,.07f,.16f,"Negro"),Cube(new Vector3(0,.15f,.072f),new Vector3(.025f,.06f,.01f),"Blanco"));
  var glasses=Accessory("Gafas oscuras",Cube(new Vector3(-.03f,.272f,.074f),new Vector3(.045f,.025f,.014f),"Negro"),Cube(new Vector3(.03f,.272f,.074f),new Vector3(.045f,.025f,.014f),"Negro"),Cube(new Vector3(0,.276f,.08f),new Vector3(.03f,.006f,.008f),"Negro"));
  var bandana=Accessory("Panuelo",Sphere(new Vector3(0,.222f,.005f),new Vector3(.17f,.05f,.17f),"Negro"),Cube(new Vector3(0,.205f,.075f),new Vector3(.07f,.04f,.012f),"Negro"));
  var hat=Accessory("Sombrero",Cylinder(new Vector3(0,.312f,0),new Vector3(.27f,.006f,.27f),"Cuero"),Cylinder(new Vector3(0,.35f,0),new Vector3(.125f,.04f,.125f),"Cuero"),Cylinder(new Vector3(0,.326f,0),new Vector3(.13f,.009f,.13f),"Rojo oscuro"));
  var chain=Accessory("Cadena de oro",Torus(new Vector3(0,.19f,.01f),Quaternion.Euler(18,0,0),.058f,.009f,"Oro"),Sphere(new Vector3(0,.165f,.066f),new Vector3(.03f,.03f,.012f),"Oro"));
  var pack=Accessory("Mochila de radio",Cube(new Vector3(0,.135f,-.075f),new Vector3(.09f,.1f,.05f),"Oliva"),Cylinder(new Vector3(.03f,.25f,-.085f),new Vector3(.008f,.08f,.008f),"Negro"),Sphere(new Vector3(.03f,.33f,-.085f),new Vector3(.02f,.02f,.02f),"Rojo"));
  var binoculars=Accessory("Binoculares",Cylinder(new Vector3(-.018f,.175f,.098f),Quaternion.Euler(90,0,0),new Vector3(.024f,.025f,.024f),"Negro"),Cylinder(new Vector3(.018f,.175f,.098f),Quaternion.Euler(90,0,0),new Vector3(.024f,.025f,.024f),"Negro"),Torus(new Vector3(0,.19f,.01f),Quaternion.Euler(15,0,0),.06f,.004f,"Cuero"));
  var characters=new[]{
   Character("Asaltante","blue",(cap,Vector3.zero,Vector3.zero,1f),(vest,Vector3.zero,Vector3.zero,1f)),
   Character("Pesado","blue",(vest,Vector3.zero,Vector3.zero,1f),(Kit("blue/helmet_blue_full"),new Vector3(0,.25f,0),Vector3.zero,1f),(Kit("blue/shield_blue_full"),new Vector3(-.05f,.12f,.105f),new Vector3(0,180,0),.85f)),
   Character("Rastreador","blue",(cap,Vector3.zero,Vector3.zero,1f),(vest,Vector3.zero,Vector3.zero,1f),(pack,Vector3.zero,Vector3.zero,1f)),
   Character("Pistolero","red",(bandana,Vector3.zero,Vector3.zero,1f),(glasses,Vector3.zero,Vector3.zero,1f)),
   Character("Guardaespaldas","red",(suit,Vector3.zero,Vector3.zero,1f),(glasses,Vector3.zero,Vector3.zero,1f)),
   Character("Vigia","red",(binoculars,Vector3.zero,Vector3.zero,1f),(Kit("red/banner_red_full"),new Vector3(0,.04f,-.09f),Vector3.zero,.8f)),
   Character("Lider","red",(hat,Vector3.zero,Vector3.zero,1f),(chain,Vector3.zero,Vector3.zero,1f))};
  var assets=AssetDatabase.LoadAssetAtPath<LanAssets>("Assets/Resources/Multiplayer/LanAssets.asset");
  assets.characters=characters;EditorUtility.SetDirty(assets);AssetDatabase.SaveAssets();
  Debug.Log("Personajes creados: "+string.Join(", ",characters.Select(c=>c.name)));
 }

 // ---------- Personajes ----------
 static GameObject Character(string name,string color,params (GameObject prefab,Vector3 position,Vector3 euler,float scale)[] items){
  var root=new GameObject(name);root.transform.localScale=Vector3.one*Scale;
  var body=(GameObject)PrefabUtility.InstantiatePrefab(Kit($"{color}/unit_{color}_full"),root.transform);body.name="Cuerpo";
  foreach(var item in items){
   var part=(GameObject)PrefabUtility.InstantiatePrefab(item.prefab,root.transform);
   // Sin cambios propios heredados: la pieza usa siempre la malla actual de su prefab.
   PrefabUtility.RevertPrefabInstance(part,InteractionMode.AutomatedAction);
   part.transform.localPosition=item.position;part.transform.localRotation=Quaternion.Euler(item.euler);part.transform.localScale=Vector3.one*item.scale;
  }
  foreach(var renderer in root.GetComponentsInChildren<Renderer>())renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
  var saved=PrefabUtility.SaveAsPrefabAsset(root,$"{Output}/{name}.prefab");
  UnityEngine.Object.DestroyImmediate(root);
  return saved;
 }
 static GameObject Kit(string path){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Units+path+".prefab");if(!prefab)throw new Exception("Falta "+path);return prefab;}

 // ---------- Accesorios: varias piezas combinadas en una sola malla con un material por color ----------
 static GameObject Accessory(string name,params Part[] parts){
  var groups=parts.GroupBy(p=>p.material).ToList();
  var subMeshes=groups.Select(g=>{var mesh=new Mesh();mesh.CombineMeshes(g.Select(p=>new CombineInstance{mesh=p.mesh,transform=p.matrix}).ToArray(),true,true);return mesh;}).ToList();
  var combined=new Mesh{name=name};
  combined.CombineMeshes(subMeshes.Select(m=>new CombineInstance{mesh=m,transform=Matrix4x4.identity}).ToArray(),false,false);
  combined.RecalculateBounds();
  // Se actualiza la malla existente en su lugar: así los prefabs que la usan no pierden la referencia.
  string meshPath=$"{Art}/Mallas/{name}.asset";
  var existing=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
  if(existing){EditorUtility.CopySerialized(combined,existing);existing.name=name;UnityEngine.Object.DestroyImmediate(combined);combined=existing;EditorUtility.SetDirty(existing);}
  else AssetDatabase.CreateAsset(combined,meshPath);
  var go=new GameObject(name);go.AddComponent<MeshFilter>().sharedMesh=combined;go.AddComponent<MeshRenderer>().sharedMaterials=groups.Select(g=>g.Key).ToArray();
  var prefab=PrefabUtility.SaveAsPrefabAsset(go,$"{Art}/{name}.prefab");
  UnityEngine.Object.DestroyImmediate(go);foreach(var m in subMeshes)UnityEngine.Object.DestroyImmediate(m);
  return prefab;
 }
 static Part Sphere(Vector3 at,Vector3 size,string color)=>Primitive(PrimitiveType.Sphere,at,Quaternion.identity,size,color);
 static Part Cube(Vector3 at,Vector3 size,string color)=>Primitive(PrimitiveType.Cube,at,Quaternion.identity,size,color);
 // El cilindro de Unity mide 2 de alto: size.y es la mitad de la altura.
 static Part Cylinder(Vector3 at,Vector3 size,string color)=>Primitive(PrimitiveType.Cylinder,at,Quaternion.identity,size,color);
 static Part Cylinder(Vector3 at,Quaternion rotation,Vector3 size,string color)=>Primitive(PrimitiveType.Cylinder,at,rotation,size,color);
 static Part Primitive(PrimitiveType type,Vector3 at,Quaternion rotation,Vector3 size,string color){
  if(!primitives.TryGetValue(type,out var mesh)){var temp=GameObject.CreatePrimitive(type);mesh=temp.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(temp);primitives[type]=mesh;}
  return new Part{mesh=mesh,matrix=Matrix4x4.TRS(at,rotation,size),material=Mat(color)};
 }
 // Tronco de cono abierto (chaleco o traje) entre dos alturas.
 static Part Frustum(float bottom,float top,float y0,float y1,string color,float unused=0){
  const int segments=24;var mesh=new Mesh();var vertices=new List<Vector3>();var triangles=new List<int>();
  for(int i=0;i<=segments;i++){float a=i*Mathf.PI*2/segments;var dir=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));vertices.Add(dir*bottom+Vector3.up*y0);vertices.Add(dir*top+Vector3.up*y1);}
  for(int i=0;i<segments;i++){int b=i*2;triangles.AddRange(new[]{b,b+2,b+1,b+1,b+2,b+3});}
  mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();
  return new Part{mesh=mesh,matrix=Matrix4x4.identity,material=Mat(color)};
 }
 static Part Torus(Vector3 at,Quaternion rotation,float radius,float thickness,string color){
  const int ring=24,side=8;var mesh=new Mesh();var vertices=new List<Vector3>();var triangles=new List<int>();
  for(int i=0;i<=ring;i++){float a=i*Mathf.PI*2/ring;var center=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*radius;
   for(int j=0;j<=side;j++){float b=j*Mathf.PI*2/side;vertices.Add(center+(center.normalized*Mathf.Cos(b)+Vector3.up*Mathf.Sin(b))*thickness);}}
  for(int i=0;i<ring;i++)for(int j=0;j<side;j++){int a=i*(side+1)+j,b=a+side+1;triangles.AddRange(new[]{a,b,a+1,b,b+1,a+1});}
  mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();
  return new Part{mesh=mesh,matrix=Matrix4x4.TRS(at,rotation,Vector3.one),material=Mat(color)};
 }
 static Material Mat(string name){
  if(materials.TryGetValue(name,out var cached))return cached;
  var colors=new Dictionary<string,Color>{{"Azul marino",new Color(.1f,.13f,.24f)},{"Amarillo",new Color(.98f,.8f,.15f)},{"Negro",new Color(.05f,.05f,.06f)},{"Blanco",new Color(.92f,.92f,.9f)},{"Rojo oscuro",new Color(.45f,.08f,.08f)},{"Rojo",new Color(.9f,.15f,.12f)},{"Oro",new Color(1,.78f,.25f)},{"Cuero",new Color(.62f,.45f,.26f)},{"Oliva",new Color(.3f,.36f,.18f)}};
  string path=$"{Art}/Materiales/{name}.mat";
  var material=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
  material.SetColor("_BaseColor",colors[name]);
  material.SetFloat("_Smoothness",name=="Oro"?.75f:name=="Negro"?.6f:.3f);material.SetFloat("_Metallic",name=="Oro"?.8f:0);
  EditorUtility.SetDirty(material);materials[name]=material;
  return material;
 }
}
