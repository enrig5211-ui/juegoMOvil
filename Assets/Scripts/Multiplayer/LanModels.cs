using System;
using System.Collections.Generic;
using UnityEngine;
namespace Valle.Lan {
 // Modelos low poly hechos con piezas simples: blindado, avioneta, caja con paracaídas, mina, dinamita,
 // manga de viento, luces y marcas de pista. Tools > Multijugador > Crear modelos low poly los guarda
 // como prefabs editables (Assets/Art/Modelos); en partida se usan esos prefabs.
 // El frente de cada modelo es +Z y la base está en y = 0.
 public static class LanModels {
  // Material por nombre: en el editor devuelve los materiales guardados; en partida, unos creados al vuelo.
  public static Func<string,Material> Materials=RuntimeMaterial;
  static readonly Dictionary<string,Material> cache=new Dictionary<string,Material>();
  public static readonly string[] Palette={"Oliva","Oliva oscuro","Negro","Gris","Gris oscuro","Vidrio","Amarillo","Blanco","Rojo","Madera","Lona","Naranja","Pintura","Luz amarilla","Luz roja","Adobe","Cal","Teja","Calamina","Jatata","Madera oscura","Piedra","Tanque","Azul ventana","Granate","Verde luz","Ceniza","Hoja","Hoja oscura","Hoja clara","Tronco","Verde bandera","Magenta","Turquesa","Morado","Barro","Lona azul","Ladrillo","Ladrillo oscuro","Concreto","Celeste","Rosado","Tierra","Agua turbia","Piel","Piel morena","Pelo","Crema","Luz fuego","Luz azul","Azul DEA","Mezclilla","Cuero"};
  public static Color ColorOf(string name){
   switch(name){
    case "Oliva":return new Color(.27f,.32f,.2f);case "Oliva oscuro":return new Color(.17f,.2f,.13f);case "Negro":return new Color(.06f,.06f,.07f);
    case "Gris":return new Color(.45f,.47f,.5f);case "Gris oscuro":return new Color(.22f,.23f,.25f);case "Vidrio":return new Color(.22f,.36f,.45f);
    case "Amarillo":return new Color(.98f,.8f,.15f);case "Blanco":return new Color(.92f,.92f,.9f);case "Rojo":return new Color(.8f,.15f,.12f);
    case "Madera":return new Color(.55f,.38f,.2f);case "Lona":return new Color(.82f,.78f,.66f);case "Naranja":return new Color(1,.5f,.1f);
    case "Pintura":return new Color(.9f,.9f,.86f);case "Luz amarilla":return new Color(1,.85f,.4f);case "Luz roja":return new Color(1,.15f,.1f);
    case "Adobe":return new Color(.66f,.47f,.32f);case "Cal":return new Color(.9f,.88f,.82f);case "Teja":return new Color(.72f,.3f,.18f);case "Calamina":return new Color(.6f,.63f,.66f);
    case "Jatata":return new Color(.7f,.58f,.33f);case "Madera oscura":return new Color(.32f,.21f,.12f);case "Piedra":return new Color(.48f,.46f,.43f);case "Tanque":return new Color(.12f,.12f,.13f);
    case "Azul ventana":return new Color(.2f,.42f,.7f);case "Granate":return new Color(.5f,.1f,.12f);case "Verde luz":return new Color(.35f,1,.45f);case "Ceniza":return new Color(.2f,.18f,.16f);
    // Selva del Chapare y objetos bolivianos hechos en Blender (Assets/Art/Modelos/Blender).
    case "Hoja":return new Color(.18f,.58f,.3f);case "Hoja oscura":return new Color(.1f,.42f,.25f);case "Hoja clara":return new Color(.45f,.72f,.25f);case "Tronco":return new Color(.52f,.42f,.3f);
    case "Verde bandera":return new Color(0,.55f,.27f);case "Magenta":return new Color(.85f,.15f,.5f);case "Turquesa":return new Color(.1f,.62f,.66f);case "Morado":return new Color(.42f,.2f,.58f);
    case "Barro":return new Color(.66f,.33f,.18f);case "Lona azul":return new Color(.15f,.35f,.75f);
    // Casas típicas, pueblo, pista y personajes (segunda tanda de Blender).
    case "Ladrillo":return new Color(.7f,.33f,.2f);case "Ladrillo oscuro":return new Color(.55f,.24f,.15f);case "Concreto":return new Color(.62f,.6f,.57f);case "Celeste":return new Color(.45f,.72f,.88f);
    case "Rosado":return new Color(.95f,.55f,.65f);case "Tierra":return new Color(.45f,.32f,.2f);case "Agua turbia":return new Color(.32f,.36f,.18f);case "Crema":return new Color(.93f,.88f,.75f);
    case "Piel":return new Color(.87f,.66f,.5f);case "Piel morena":return new Color(.62f,.42f,.3f);case "Pelo":return new Color(.08f,.06f,.05f);
    case "Luz fuego":return new Color(1,.55f,.15f);case "Luz azul":return new Color(.25f,.45f,1);case "Azul DEA":return new Color(.13f,.17f,.3f);case "Mezclilla":return new Color(.25f,.33f,.5f);case "Cuero":return new Color(.35f,.22f,.13f);
   }
   return Color.magenta;
  }
  public static bool Glows(string name)=>name.StartsWith("Luz")||name=="Verde luz";
  public static Material Configure(Material material,string name){
   var color=ColorOf(name);material.SetColor("_BaseColor",color);material.color=color;material.SetFloat("_Smoothness",name=="Vidrio"?.85f:name=="Negro"?.5f:.25f);
   if(Glows(name)){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",color*6f);material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.None;}
   return material;
  }
  static Material RuntimeMaterial(string name){
   if(cache.TryGetValue(name,out var cached)&&cached)return cached;
   var material=Configure(new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name},name);cache[name]=material;return material;
  }
  static GameObject Part(Transform parent,PrimitiveType type,string name,Vector3 at,Vector3 scale,string material,Vector3 euler=default){
   var go=GameObject.CreatePrimitive(type);go.name=name;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
   go.transform.SetParent(parent,false);go.transform.localPosition=at;go.transform.localRotation=Quaternion.Euler(euler);go.transform.localScale=scale;
   go.GetComponent<Renderer>().sharedMaterial=Materials(material);return go;
  }
  // Barra entre dos puntos (cuerdas, puntales).
  static GameObject Between(Transform parent,string name,Vector3 a,Vector3 b,float thickness,string material){
   var go=Part(parent,PrimitiveType.Cylinder,name,(a+b)*.5f,new Vector3(thickness,(b-a).magnitude*.5f,thickness),material);
   go.transform.localRotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);return go;
  }
  static Transform Root(Transform parent,string name){var root=new GameObject(name).transform;if(parent)root.SetParent(parent,false);return root;}
  static Transform Pivot(Transform parent,string name,Vector3 at){var pivot=new GameObject(name).transform;pivot.SetParent(parent,false);pivot.localPosition=at;return pivot;}

  public static Transform Vehicle(Transform parent){
   var root=Root(parent,"Blindado");
   Part(root,PrimitiveType.Cube,"Casco",new Vector3(0,.58f,0),new Vector3(1.6f,.55f,2.9f),"Oliva");
   Part(root,PrimitiveType.Cube,"Cabina",new Vector3(0,1.05f,-.25f),new Vector3(1.48f,.45f,1.9f),"Oliva");
   Part(root,PrimitiveType.Cube,"Capó",new Vector3(0,.88f,1.05f),new Vector3(1.5f,.22f,.95f),"Oliva oscuro");
   Part(root,PrimitiveType.Cube,"Frente inclinado",new Vector3(0,.66f,1.48f),new Vector3(1.55f,.4f,.35f),"Oliva",new Vector3(-30,0,0));
   Part(root,PrimitiveType.Cube,"Parabrisas",new Vector3(0,1.12f,.72f),new Vector3(1.3f,.3f,.06f),"Vidrio",new Vector3(-22,0,0));
   foreach(int side in new[]{-1,1}){
    Part(root,PrimitiveType.Cube,"Ventana",new Vector3(side*.75f,1.08f,-.1f),new Vector3(.04f,.2f,.9f),"Vidrio");
    Part(root,PrimitiveType.Cube,"Franja",new Vector3(side*.81f,.7f,0),new Vector3(.02f,.09f,2.7f),"Amarillo");
    Part(root,PrimitiveType.Cube,"Faro",new Vector3(side*.55f,.74f,1.62f),new Vector3(.28f,.14f,.06f),"Luz amarilla");
    var beam=new GameObject("Luz del faro").AddComponent<Light>();beam.transform.SetParent(root,false);beam.transform.localPosition=new Vector3(side*.55f,.8f,1.7f);beam.transform.localRotation=Quaternion.Euler(12,0,0);
    beam.type=LightType.Spot;beam.range=14;beam.spotAngle=70;beam.innerSpotAngle=40;beam.intensity=7;beam.color=new Color(1,.93f,.78f);beam.shadows=LightShadows.None;
   }
   // Rótulo en el capó, legible desde la cámara cenital.
   var label=new GameObject("DEA").AddComponent<TextMesh>();label.transform.SetParent(root,false);label.transform.localPosition=new Vector3(0,1.0f,1.08f);label.transform.localRotation=Quaternion.Euler(90,0,0);
   label.text="DEA";label.fontSize=64;label.characterSize=.06f;label.anchor=TextAnchor.MiddleCenter;label.color=ColorOf("Amarillo");label.fontStyle=FontStyle.Bold;
   Part(root,PrimitiveType.Cube,"Parachoques",new Vector3(0,.38f,1.62f),new Vector3(1.75f,.2f,.15f),"Negro");
   Part(root,PrimitiveType.Cube,"Puerta trasera",new Vector3(0,.88f,-1.47f),new Vector3(1f,.5f,.05f),"Oliva oscuro");
   Part(root,PrimitiveType.Cylinder,"Antena",new Vector3(-.6f,1.75f,-1.1f),new Vector3(.025f,.5f,.025f),"Negro");
   int index=0;
   foreach(var (x,z) in new[]{(-.86f,.98f),(.86f,.98f),(-.86f,-.98f),(.86f,-.98f)}){
    var wheel=Pivot(root,"Rueda "+(++index),new Vector3(x,.38f,z));
    Part(wheel,PrimitiveType.Cylinder,"Llanta",Vector3.zero,new Vector3(.76f,.12f,.76f),"Negro",new Vector3(0,0,90));
    Part(wheel,PrimitiveType.Cylinder,"Tapa",new Vector3(Mathf.Sign(x)*.02f,0,0),new Vector3(.36f,.125f,.36f),"Gris",new Vector3(0,0,90));
    Part(wheel,PrimitiveType.Cube,"Taco",new Vector3(Mathf.Sign(x)*.13f,.25f,0),new Vector3(.02f,.08f,.12f),"Gris oscuro");
   }
   var turret=Pivot(root,"Torreta",new Vector3(0,1.3f,-.3f));
   Part(turret,PrimitiveType.Cylinder,"Aro",Vector3.zero,new Vector3(.7f,.08f,.7f),"Gris oscuro");
   Part(turret,PrimitiveType.Cube,"Escudo",new Vector3(0,.28f,.35f),new Vector3(.62f,.38f,.05f),"Oliva oscuro");
   Part(turret,PrimitiveType.Cube,"Cuerpo del arma",new Vector3(0,.2f,.15f),new Vector3(.18f,.16f,.5f),"Negro");
   Part(turret,PrimitiveType.Cylinder,"Cañón",new Vector3(0,.2f,.75f),new Vector3(.06f,.38f,.06f),"Negro",new Vector3(90,0,0));
   Part(turret,PrimitiveType.Cube,"Caja de balas",new Vector3(.18f,.12f,.05f),new Vector3(.12f,.12f,.22f),"Oliva");
   return root;
  }
  public static Transform Plane(Transform parent){
   var root=Root(parent,"Avioneta");
   Part(root,PrimitiveType.Cube,"Fuselaje",new Vector3(0,.78f,0),new Vector3(.62f,.56f,2.6f),"Blanco");
   Part(root,PrimitiveType.Cube,"Franja",new Vector3(0,.72f,0),new Vector3(.64f,.09f,2.62f),"Rojo");
   Part(root,PrimitiveType.Cylinder,"Morro",new Vector3(0,.76f,1.42f),new Vector3(.5f,.16f,.5f),"Blanco",new Vector3(90,0,0));
   Part(root,PrimitiveType.Cube,"Cabina",new Vector3(0,1.1f,.45f),new Vector3(.56f,.22f,.75f),"Vidrio");
   Part(root,PrimitiveType.Cube,"Cola",new Vector3(0,.88f,-1.6f),new Vector3(.34f,.36f,.9f),"Blanco");
   Part(root,PrimitiveType.Cube,"Ala",new Vector3(0,1.08f,.35f),new Vector3(4.6f,.07f,.8f),"Blanco");
   foreach(int side in new[]{-1,1}){
    Part(root,PrimitiveType.Cube,"Punta del ala",new Vector3(side*2.15f,1.09f,.35f),new Vector3(.32f,.08f,.82f),"Rojo");
    Between(root,"Puntal",new Vector3(side*.3f,.6f,.3f),new Vector3(side*1.3f,1.05f,.35f),.03f,"Gris");
    Between(root,"Pata",new Vector3(side*.3f,.55f,.3f),new Vector3(side*.6f,.2f,.3f),.04f,"Gris");
    Part(root,PrimitiveType.Cylinder,"Rueda",new Vector3(side*.62f,.18f,.3f),new Vector3(.32f,.06f,.32f),"Negro",new Vector3(0,0,90));
   }
   Part(root,PrimitiveType.Cylinder,"Rueda delantera",new Vector3(0,.15f,1.15f),new Vector3(.24f,.05f,.24f),"Negro",new Vector3(0,0,90));
   Part(root,PrimitiveType.Cube,"Estabilizador",new Vector3(0,.98f,-1.95f),new Vector3(1.6f,.05f,.45f),"Blanco");
   Part(root,PrimitiveType.Cube,"Timón",new Vector3(0,1.32f,-1.95f),new Vector3(.05f,.7f,.5f),"Rojo");
   var propeller=Pivot(root,"Helice",new Vector3(0,.76f,1.62f));
   Part(propeller,PrimitiveType.Cube,"Pala",Vector3.zero,new Vector3(1.25f,.1f,.03f),"Negro");
   Part(propeller,PrimitiveType.Sphere,"Cono",Vector3.zero,Vector3.one*.16f,"Rojo");
   return root;
  }
  public static Transform Supply(Transform parent){
   var root=Root(parent,"Suministro");
   Part(root,PrimitiveType.Cube,"Caja",new Vector3(0,.3f,0),new Vector3(.7f,.6f,.7f),"Madera");
   Part(root,PrimitiveType.Cube,"Faja",new Vector3(0,.3f,0),new Vector3(.72f,.12f,.72f),"Oliva oscuro");
   Part(root,PrimitiveType.Cube,"Faja vertical",new Vector3(0,.3f,0),new Vector3(.12f,.62f,.72f),"Oliva oscuro");
   Part(root,PrimitiveType.Cube,"Tapa",new Vector3(0,.61f,0),new Vector3(.5f,.02f,.5f),"Amarillo");
   var chute=Pivot(root,"Paracaidas",Vector3.zero);
   Part(chute,PrimitiveType.Sphere,"Lona",new Vector3(0,2.3f,0),new Vector3(2.2f,.75f,2.2f),"Lona");
   Part(chute,PrimitiveType.Sphere,"Franja",new Vector3(0,2.32f,0),new Vector3(1.2f,.78f,1.2f),"Naranja");
   foreach(var (x,z) in new[]{(-1,-1),(1,-1),(-1,1),(1,1)})Between(chute,"Cuerda",new Vector3(x*.78f,2.1f,z*.78f),new Vector3(x*.3f,.6f,z*.3f),.015f,"Negro");
   return root;
  }
  // Cuadricóptero de la DEA: cuerpo, cuatro brazos con rotores y luces de posición.
  public static Transform Drone(Transform parent){
   var root=Root(parent,"Dron");
   Part(root,PrimitiveType.Cube,"Cuerpo",Vector3.zero,new Vector3(.28f,.09f,.36f),"Gris oscuro");
   Part(root,PrimitiveType.Cube,"Tapa",new Vector3(0,.06f,0),new Vector3(.18f,.04f,.24f),"Negro");
   Part(root,PrimitiveType.Sphere,"Camara",new Vector3(0,-.07f,.12f),Vector3.one*.08f,"Vidrio");
   int i=0;
   foreach(var (x,z) in new[]{(-1,-1),(1,-1),(-1,1),(1,1)}){
    var tip=new Vector3(x*.26f,.02f,z*.26f);Between(root,"Brazo",Vector3.zero,tip,.025f,"Negro");
    var rotor=Pivot(root,"Rotor "+(++i),tip+Vector3.up*.05f);
    Part(rotor,PrimitiveType.Cube,"Pala",Vector3.zero,new Vector3(.3f,.008f,.03f),"Gris");Part(rotor,PrimitiveType.Cube,"Pala",Vector3.zero,new Vector3(.03f,.008f,.3f),"Gris");
    Part(root,PrimitiveType.Cube,"Luz",tip+Vector3.down*.03f,Vector3.one*.035f,z>0?"Luz amarilla":"Luz roja");
   }
   return root;
  }
  // ---------- Vehículos y apoyo ----------
  // Chuto: camioneta doble cabina tipo Toyota de los años 90, con ametralladora en la carrocería. Ya a escala del juego.
  public static Transform Chuto(Transform parent){
   var root=Root(parent,"Chuto");
   Part(root,PrimitiveType.Cube,"Chasis",new Vector3(0,.3f,0),new Vector3(.86f,.2f,1.75f),"Granate");
   Part(root,PrimitiveType.Cube,"Capo",new Vector3(0,.45f,.6f),new Vector3(.84f,.14f,.55f),"Granate");
   Part(root,PrimitiveType.Cube,"Cabina",new Vector3(0,.6f,.05f),new Vector3(.8f,.34f,.62f),"Granate");
   Part(root,PrimitiveType.Cube,"Parabrisas",new Vector3(0,.63f,.37f),new Vector3(.72f,.24f,.03f),"Vidrio",new Vector3(-18,0,0));
   foreach(int side in new[]{-1,1}){Part(root,PrimitiveType.Cube,"Ventana",new Vector3(side*.405f,.63f,.05f),new Vector3(.02f,.18f,.5f),"Vidrio");Part(root,PrimitiveType.Cube,"Barandilla",new Vector3(side*.41f,.47f,-.55f),new Vector3(.03f,.12f,.62f),"Granate");}
   Part(root,PrimitiveType.Cube,"Carroceria",new Vector3(0,.42f,-.55f),new Vector3(.84f,.04f,.64f),"Negro");
   Part(root,PrimitiveType.Cube,"Compuerta",new Vector3(0,.47f,-.86f),new Vector3(.84f,.12f,.03f),"Granate");
   Part(root,PrimitiveType.Cube,"Parachoques",new Vector3(0,.26f,.89f),new Vector3(.9f,.08f,.06f),"Gris");
   Part(root,PrimitiveType.Cube,"Defensa",new Vector3(0,.4f,.9f),new Vector3(.5f,.12f,.03f),"Negro");
   foreach(int side in new[]{-1,1})Part(root,PrimitiveType.Cube,"Faro",new Vector3(side*.3f,.45f,.885f),new Vector3(.14f,.07f,.02f),"Luz amarilla");
   foreach(int side in new[]{-1,1}){var beam=new GameObject("Luz del faro").AddComponent<Light>();beam.transform.SetParent(root,false);beam.transform.localPosition=new Vector3(side*.3f,.45f,.95f);beam.transform.localRotation=Quaternion.Euler(10,0,0);beam.type=LightType.Spot;beam.range=10;beam.spotAngle=65;beam.intensity=5;beam.color=new Color(1,.92f,.75f);beam.shadows=LightShadows.None;}
   int index=0;
   foreach(var (x,z) in new[]{(-.43f,.55f),(.43f,.55f),(-.43f,-.55f),(.43f,-.55f)}){var wheel=Pivot(root,"Rueda "+(++index),new Vector3(x,.17f,z));Part(wheel,PrimitiveType.Cylinder,"Llanta",Vector3.zero,new Vector3(.34f,.06f,.34f),"Negro",new Vector3(0,0,90));Part(wheel,PrimitiveType.Cylinder,"Tapa",new Vector3(Mathf.Sign(x)*.01f,0,0),new Vector3(.16f,.062f,.16f),"Gris",new Vector3(0,0,90));}
   var turret=Pivot(root,"Torreta",new Vector3(0,.68f,-.55f));
   Part(turret,PrimitiveType.Cylinder,"Poste",new Vector3(0,-.1f,0),new Vector3(.05f,.12f,.05f),"Negro");
   Part(turret,PrimitiveType.Cube,"Ametralladora",new Vector3(0,.04f,.1f),new Vector3(.08f,.08f,.3f),"Negro");
   Part(turret,PrimitiveType.Cylinder,"Cañon",new Vector3(0,.04f,.36f),new Vector3(.03f,.16f,.03f),"Negro",new Vector3(90,0,0));
   return root;
  }
  // Helicóptero de la DEA: cabina, botalón de cola, rotores y patines.
  public static Transform Helicopter(Transform parent){
   var root=Root(parent,"Helicoptero");
   Part(root,PrimitiveType.Sphere,"Cabina",new Vector3(0,0,.35f),new Vector3(1.1f,.9f,1.6f),"Oliva oscuro");
   Part(root,PrimitiveType.Sphere,"Cristal",new Vector3(0,.12f,.85f),new Vector3(.8f,.55f,.7f),"Vidrio");
   Part(root,PrimitiveType.Cube,"Cuerpo",new Vector3(0,-.05f,-.25f),new Vector3(.9f,.65f,1.1f),"Oliva oscuro");
   Part(root,PrimitiveType.Cube,"Franja",new Vector3(0,-.05f,-.25f),new Vector3(.92f,.1f,1.12f),"Amarillo");
   Part(root,PrimitiveType.Cylinder,"Cola",new Vector3(0,.12f,-1.6f),new Vector3(.18f,.9f,.18f),"Oliva oscuro",new Vector3(90,0,0));
   Part(root,PrimitiveType.Cube,"Aleta",new Vector3(0,.4f,-2.45f),new Vector3(.05f,.55f,.35f),"Oliva oscuro");
   Part(root,PrimitiveType.Cylinder,"Mastil",new Vector3(0,.5f,0),new Vector3(.12f,.1f,.12f),"Negro");
   var rotor=Pivot(root,"Rotor",new Vector3(0,.62f,0));Part(rotor,PrimitiveType.Cube,"Pala",Vector3.zero,new Vector3(3.6f,.03f,.16f),"Negro");Part(rotor,PrimitiveType.Cube,"Pala",Vector3.zero,new Vector3(.16f,.03f,3.6f),"Negro");
   var tail=Pivot(root,"Rotor de cola",new Vector3(.09f,.4f,-2.45f));Part(tail,PrimitiveType.Cube,"Pala",Vector3.zero,new Vector3(.02f,.7f,.08f),"Negro");
   foreach(int side in new[]{-1,1}){Part(root,PrimitiveType.Cube,"Patin",new Vector3(side*.5f,-.58f,0),new Vector3(.06f,.06f,1.6f),"Negro");Between(root,"Soporte",new Vector3(side*.35f,-.3f,.3f),new Vector3(side*.5f,-.56f,.3f),.03f,"Negro");Between(root,"Soporte",new Vector3(side*.35f,-.3f,-.4f),new Vector3(side*.5f,-.56f,-.4f),.03f,"Negro");}
   Part(root,PrimitiveType.Cube,"Ametralladora",new Vector3(.5f,-.2f,.4f),new Vector3(.08f,.08f,.45f),"Negro");
   Part(root,PrimitiveType.Cube,"Luz",new Vector3(0,-.35f,.8f),Vector3.one*.08f,"Luz roja");
   return root;
  }
  public static Transform MedStation(Transform parent){
   var root=Root(parent,"Estacion medica");
   Part(root,PrimitiveType.Cube,"Base",new Vector3(0,.12f,0),new Vector3(.5f,.24f,.4f),"Blanco");
   Part(root,PrimitiveType.Cube,"Cruz",new Vector3(0,.15f,.205f),new Vector3(.24f,.07f,.01f),"Rojo");Part(root,PrimitiveType.Cube,"Cruz",new Vector3(0,.15f,.205f),new Vector3(.07f,.2f,.01f),"Rojo");
   Part(root,PrimitiveType.Cylinder,"Antena",new Vector3(.18f,.42f,0),new Vector3(.02f,.18f,.02f),"Gris");
   Part(root,PrimitiveType.Sphere,"Faro",new Vector3(.18f,.62f,0),Vector3.one*.07f,"Verde luz");
   Part(root,PrimitiveType.Cube,"Pantalla",new Vector3(-.1f,.26f,0),new Vector3(.18f,.04f,.2f),"Verde luz");
   return root;
  }
  public static Transform Campfire(Transform parent){
   var root=Root(parent,"Fogata");
   for(int i=0;i<8;i++){float a=i*45*Mathf.Deg2Rad;Part(root,PrimitiveType.Sphere,"Piedra",new Vector3(Mathf.Sin(a)*.28f,.04f,Mathf.Cos(a)*.28f),new Vector3(.12f,.08f,.12f),"Piedra");}
   for(int i=0;i<4;i++)Part(root,PrimitiveType.Cylinder,"Leño",new Vector3(0,.07f,0),new Vector3(.06f,.2f,.06f),"Madera oscura",new Vector3(70,i*45,0));
   Part(root,PrimitiveType.Cylinder,"Brasas",new Vector3(0,.02f,0),new Vector3(.36f,.02f,.36f),"Ceniza");
   return root;
  }
  public static Transform DroneStation(Transform parent){
   var root=Root(parent,"Base de drones");
   Part(root,PrimitiveType.Cube,"Caja",new Vector3(0,.15f,0),new Vector3(.6f,.3f,.45f),"Oliva oscuro");
   Part(root,PrimitiveType.Cube,"Plataforma",new Vector3(0,.32f,0),new Vector3(.66f,.04f,.5f),"Negro");
   Part(root,PrimitiveType.Cube,"Letrero",new Vector3(0,.17f,.228f),new Vector3(.3f,.08f,.01f),"Amarillo");
   Part(root,PrimitiveType.Cylinder,"Antena",new Vector3(-.25f,.55f,-.15f),new Vector3(.015f,.22f,.015f),"Gris");
   Part(root,PrimitiveType.Cube,"Luz",new Vector3(.25f,.36f,.2f),Vector3.one*.05f,"Luz roja");
   return root;
  }
  // ---------- Arquitectura boliviana, a escala de los personajes (unos 0,65 de alto) ----------
  // Casa de adobe o encalada, con techo a dos aguas de calamina (con piedras encima) o de teja colonial.
  public static Transform House(Transform parent,string name,string walls,bool tiles){
   var root=Root(parent,name);
   Part(root,PrimitiveType.Cube,"Cimiento",new Vector3(0,.06f,0),new Vector3(1.74f,.12f,1.29f),"Piedra");
   Part(root,PrimitiveType.Cube,"Muros",new Vector3(0,.44f,0),new Vector3(1.7f,.7f,1.25f),walls);
   Part(root,PrimitiveType.Cube,"Puerta",new Vector3(0,.33f,.63f),new Vector3(.3f,.5f,.03f),tiles?"Azul ventana":"Madera oscura");
   foreach(int side in new[]{-1,1}){Part(root,PrimitiveType.Cube,"Ventana",new Vector3(side*.52f,.5f,.63f),new Vector3(.24f,.2f,.03f),"Vidrio");Part(root,PrimitiveType.Cube,"Marco",new Vector3(side*.52f,.5f,.628f),new Vector3(.3f,.26f,.02f),tiles?"Azul ventana":"Madera");}
   string roof=tiles?"Teja":"Calamina";
   foreach(int side in new[]{-1,1})Part(root,PrimitiveType.Cube,"Techo",new Vector3(0,.93f,side*.36f),new Vector3(1.9f,.05f,.85f),roof,new Vector3(side*28,0,0));
   Part(root,PrimitiveType.Cube,"Cumbrera",new Vector3(0,1.13f,0),new Vector3(1.92f,.06f,.1f),tiles?"Teja":"Gris");
   foreach(int side in new[]{-1,1})Part(root,PrimitiveType.Cube,"Hastial",new Vector3(side*.85f,.92f,0),new Vector3(.02f,.38f,.8f),walls,new Vector3(0,0,0));
   if(!tiles){Part(root,PrimitiveType.Sphere,"Piedra",new Vector3(.4f,1.02f,.25f),new Vector3(.12f,.08f,.1f),"Piedra");Part(root,PrimitiveType.Sphere,"Piedra",new Vector3(-.5f,.98f,-.32f),new Vector3(.1f,.07f,.1f),"Piedra");}
   else Part(root,PrimitiveType.Cube,"Banco",new Vector3(.55f,.1f,.82f),new Vector3(.4f,.06f,.12f),"Madera");
   return root;
  }
  // Palafito del trópico: casa de madera sobre pilotes, con techo empinado de hojas de jatata y escalera.
  public static Transform StiltHouse(Transform parent){
   var root=Root(parent,"Palafito");
   foreach(float x in new[]{-.7f,0,.7f})foreach(float z in new[]{-.5f,.5f})Part(root,PrimitiveType.Cylinder,"Pilote",new Vector3(x,.2f,z),new Vector3(.07f,.2f,.07f),"Madera oscura");
   Part(root,PrimitiveType.Cube,"Piso",new Vector3(0,.42f,0),new Vector3(1.6f,.06f,1.2f),"Madera");
   Part(root,PrimitiveType.Cube,"Paredes",new Vector3(0,.72f,-.08f),new Vector3(1.4f,.55f,.95f),"Madera");
   Part(root,PrimitiveType.Cube,"Puerta",new Vector3(0,.65f,.4f),new Vector3(.26f,.42f,.03f),"Madera oscura");
   foreach(int side in new[]{-1,1})Part(root,PrimitiveType.Cube,"Techo",new Vector3(0,1.2f,side*.37f),new Vector3(1.85f,.07f,.95f),"Jatata",new Vector3(side*42,0,0));
   Part(root,PrimitiveType.Cube,"Cumbrera",new Vector3(0,1.5f,0),new Vector3(1.86f,.08f,.12f),"Jatata");
   for(int i=0;i<3;i++)Part(root,PrimitiveType.Cube,"Escalon",new Vector3(.45f,.08f+i*.12f,.85f-i*.12f),new Vector3(.36f,.04f,.12f),"Madera oscura");
   Part(root,PrimitiveType.Cube,"Baranda",new Vector3(0,.55f,.58f),new Vector3(1.6f,.04f,.03f),"Madera oscura");
   return root;
  }
  // Tienda de barrio: muros encalados, toldo de calamina, mostrador, letrero y cajas de refrescos.
  public static Transform Shop(Transform parent){
   var root=House(parent,"Tienda","Cal",false);
   Part(root,PrimitiveType.Cube,"Toldo",new Vector3(0,.78f,.86f),new Vector3(1.5f,.03f,.5f),"Calamina",new Vector3(-15,0,0));
   foreach(int side in new[]{-1,1})Part(root,PrimitiveType.Cylinder,"Poste",new Vector3(side*.68f,.37f,1.05f),new Vector3(.03f,.37f,.03f),"Madera");
   Part(root,PrimitiveType.Cube,"Mostrador",new Vector3(.45f,.22f,.75f),new Vector3(.5f,.3f,.16f),"Madera");
   var sign=new GameObject("Letrero").AddComponent<TextMesh>();sign.transform.SetParent(root,false);sign.transform.localPosition=new Vector3(0,.9f,.65f);sign.transform.localRotation=Quaternion.Euler(0,180,0);
   sign.text="TIENDA";sign.fontSize=48;sign.characterSize=.035f;sign.anchor=TextAnchor.MiddleCenter;sign.color=ColorOf("Rojo");sign.fontStyle=FontStyle.Bold;
   for(int i=0;i<3;i++)Part(root,PrimitiveType.Cube,"Caja",new Vector3(-.55f+i*.14f,.08f+(i==1?.12f:0),.85f),new Vector3(.12f,.12f,.12f),i==1?"Rojo":"Amarillo");
   return root;
  }
  // Tanque de agua elevado (tinaco negro) sobre una torre de madera.
  public static Transform WaterTank(Transform parent){
   var root=Root(parent,"Tanque de agua");
   foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})Part(root,PrimitiveType.Cylinder,"Pata",new Vector3(x*.32f,.5f,z*.32f),new Vector3(.06f,.5f,.06f),"Madera oscura");
   Part(root,PrimitiveType.Cube,"Plataforma",new Vector3(0,1.02f,0),new Vector3(.8f,.05f,.8f),"Madera");
   Part(root,PrimitiveType.Cylinder,"Tanque",new Vector3(0,1.27f,0),new Vector3(.62f,.22f,.62f),"Tanque");
   Part(root,PrimitiveType.Cylinder,"Tapa",new Vector3(0,1.5f,0),new Vector3(.2f,.02f,.2f),"Tanque");
   for(int i=0;i<5;i++)Part(root,PrimitiveType.Cube,"Peldaño",new Vector3(0,.15f+i*.18f,.36f),new Vector3(.22f,.02f,.03f),"Madera oscura");
   return root;
  }
  // Pozo de piedra con armazón de madera y balde.
  public static Transform Well(Transform parent){
   var root=Root(parent,"Pozo");
   Part(root,PrimitiveType.Cylinder,"Brocal",new Vector3(0,.14f,0),new Vector3(.6f,.14f,.6f),"Piedra");
   Part(root,PrimitiveType.Cylinder,"Agua",new Vector3(0,.26f,0),new Vector3(.48f,.01f,.48f),"Azul ventana");
   foreach(int side in new[]{-1,1})Part(root,PrimitiveType.Cube,"Poste",new Vector3(side*.28f,.45f,0),new Vector3(.05f,.6f,.05f),"Madera oscura");
   Part(root,PrimitiveType.Cylinder,"Rodillo",new Vector3(0,.68f,0),new Vector3(.05f,.3f,.05f),"Madera",new Vector3(0,0,90));
   Part(root,PrimitiveType.Cube,"Techito",new Vector3(0,.8f,0),new Vector3(.75f,.04f,.45f),"Teja");
   Part(root,PrimitiveType.Cylinder,"Balde",new Vector3(.12f,.5f,0),new Vector3(.08f,.06f,.08f),"Calamina");
   return root;
  }
  public static Transform Mine(Transform parent){
   var root=Root(parent,"Mina");
   Part(root,PrimitiveType.Cylinder,"Cuerpo",new Vector3(0,.04f,0),new Vector3(.42f,.04f,.42f),"Oliva oscuro");
   Part(root,PrimitiveType.Cylinder,"Placa",new Vector3(0,.09f,0),new Vector3(.2f,.02f,.2f),"Gris");
   Part(root,PrimitiveType.Cube,"Testigo",new Vector3(0,.11f,.13f),new Vector3(.05f,.03f,.05f),"Luz roja");
   return root;
  }
  // Cartuchos de dinamita en la cintura del kamikaze (medidas del mundo: el personaje mide unas 0,65).
  public static Transform Dynamite(Transform parent){
   var root=Root(parent,"Dinamita");
   for(int i=0;i<5;i++){float a=(i-2)*28*Mathf.Deg2Rad;Part(root,PrimitiveType.Cylinder,"Cartucho",new Vector3(Mathf.Sin(a)*.15f,.27f,Mathf.Cos(a)*.15f),new Vector3(.055f,.08f,.055f),"Rojo");}
   Part(root,PrimitiveType.Cube,"Cinta",new Vector3(0,.27f,.15f),new Vector3(.3f,.03f,.03f),"Negro");
   Part(root,PrimitiveType.Cube,"Detonador",new Vector3(0,.3f,.17f),new Vector3(.07f,.05f,.03f),"Luz roja");
   return root;
  }
  public static Transform Windsock(Transform parent){
   var root=Root(parent,"Manga de viento");
   Part(root,PrimitiveType.Cylinder,"Poste",new Vector3(0,1.1f,0),new Vector3(.06f,1.1f,.06f),"Gris");
   string[] colors={"Naranja","Blanco","Naranja","Blanco"};
   for(int i=0;i<4;i++){float size=.28f-i*.045f;Part(root,PrimitiveType.Cylinder,"Manga",new Vector3(.2f+i*.26f,2.05f-i*.05f,0),new Vector3(size,.13f,size),colors[i],new Vector3(0,0,90-i*4));}
   return root;
  }
  public static Transform RunwayLight(Transform parent){
   var root=Root(parent,"Luz de pista");
   Part(root,PrimitiveType.Cube,"Base",new Vector3(0,.07f,0),new Vector3(.22f,.14f,.22f),"Gris oscuro");
   Part(root,PrimitiveType.Cube,"Foco",new Vector3(0,.18f,0),new Vector3(.17f,.09f,.17f),"Luz amarilla");
   return root;
  }
  // Línea central discontinua y franjas de umbral en los dos extremos.
  public static Transform RunwayMarks(Transform parent,float length,float width){
   var root=Root(parent,"Marcas de pista");
   float half=length*.5f-2.2f;
   for(float z=-half+2;z<=half-2;z+=3.5f)Part(root,PrimitiveType.Cube,"Linea central",new Vector3(0,.01f,z),new Vector3(.24f,.02f,1.8f),"Pintura");
   foreach(int end in new[]{-1,1})for(int i=0;i<6;i++){float x=(i-2.5f)*(width-3)/6f;Part(root,PrimitiveType.Cube,"Umbral",new Vector3(x,.01f,end*half),new Vector3(.45f,.02f,1.6f),"Pintura");}
   return root;
  }
 }
}
