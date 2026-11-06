using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
public static class ValleRusticBase {
 const string P="Assets/KayKit/Packs/KayKit - Medieval Hexagon Pack (for Unity)/Prefabs/";
 static Vector3 Pos(int q,int r)=>new Vector3(2*q+r,0,Mathf.Sqrt(3)*r);
 static int Radius(int q,int r)=>Mathf.Max(Mathf.Abs(q),Mathf.Abs(r),Mathf.Abs(q+r));
 static Transform Group(string n,Transform p){var g=new GameObject(n);g.transform.SetParent(p,false);return g.transform;}
 static GameObject Place(string path,Transform parent,Vector3 p,float yaw=0,float scale=1,bool small=false){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(P+path+".prefab");if(!prefab)throw new Exception(path);var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);g.transform.position=p;g.transform.rotation=Quaternion.Euler(0,yaw,0);g.transform.localScale=Vector3.one*scale;
 foreach(var r in g.GetComponentsInChildren<MeshRenderer>()){GameObjectUtility.SetStaticEditorFlags(r.gameObject,GameObjectUtility.GetStaticEditorFlags(r.gameObject)|StaticEditorFlags.BatchingStatic);if(small)r.shadowCastingMode=ShadowCastingMode.Off;}return g;}
 static GameObject Building(string name,Transform parent,int q,int r,float yaw=0){var g=Place("buildings/yellow/building_"+name+"_yellow",parent,Pos(q,r),yaw);var rs=g.GetComponentsInChildren<Renderer>();Bounds b=rs[0].bounds;foreach(var rr in rs)b.Encapsulate(rr.bounds);float factor=Mathf.Min(1,1.7f/Mathf.Max(b.size.x,b.size.z));g.transform.localScale*=factor;g.transform.position+=Vector3.up*(-b.min.y*factor);g.name=name+" yellow ["+q+", "+r+"]";return g;}
 [MenuItem("Tools/Escenario/Crear base rustica yellow")]
 public static void Build(){var root=GameObject.Find("VALLE DEL RIO");if(!root)throw new Exception("Abre ValleDelRio");if(root.transform.Find("09 - Base rustica Yellow"))return;
 EditorSceneManager.SaveScene(root.scene,"Assets/Scenes/ValleDelRio_AntesDeBase.unity",true);
 var ground=root.transform.GetChild(0);var river=root.transform.GetChild(1);var woods=root.transform.GetChild(2);var hills=root.transform.GetChild(3);var detail=root.transform.GetChild(4);
 var rng=new System.Random(149);
 for(int r=-10;r<=10;r++)for(int q=-10;q<=10;q++){if(Radius(q,r)!=10)continue;var p=Pos(q,r);if(q==-10&&r==0){Place("tiles/rivers/hex_river_A_curvy",river,p,0,1,true).name="Rio ampliado [-10, 0]";continue;}Place("tiles/base/hex_grass",ground,p,0,1,true).name=$"Pradera [{q}, {r}]";
 if(r>5&&rng.NextDouble()<.6)Place("decoration/nature/mountain_A_grass_trees",hills,p,rng.Next(6)*60,.9f);
 else if(rng.NextDouble()<.55)Place("decoration/nature/trees_B_small",woods,p,rng.Next(6)*60,.85f);
 }
 var center=new Vector2Int(0,-4);
 // Clear only the settlement footprint; keep existing river, lake and terrain untouched.
 int cleared=0;foreach(var group in new[]{woods,hills,detail})foreach(Transform t in group.Cast<Transform>().ToArray()){int r=Mathf.RoundToInt(t.position.z/Mathf.Sqrt(3));int q=Mathf.RoundToInt((t.position.x-r)/2);if(Radius(q-center.x,r-center.y)<=3){UnityEngine.Object.DestroyImmediate(t.gameObject);cleared++;}}
 var settlement=Group("09 - Base rustica Yellow",root.transform);
 var houses=Group("01 - Casas y plaza",settlement);var work=Group("02 - Talleres y granja",settlement);var paths=Group("03 - Tierra y senderos",settlement);var props=Group("04 - Cajas barriles y cercas",settlement);var trees=Group("05 - Arboles del poblado",settlement);
 Building("townhall",houses,-1,-2,180);Building("home_A",houses,-1,-3,120);Building("home_B",houses,1,-4,240);Building("home_A",houses,0,-5,0);Building("tavern",houses,-1,-4,60);Building("market",houses,0,-3,180);Building("well",houses,0,-4);
 Building("workshop",work,1,-5,240);Building("lumbermill",work,-2,-3,60);Building("stables",work,0,-6,0);Building("windmill",work,-2,-4,60);Building("watchtower",work,2,-6,0);Building("blacksmith",work,2,-5,240);
 var cells=new[]{new Vector2Int(-1,-2),new Vector2Int(-1,-3),new Vector2Int(1,-4),new Vector2Int(0,-5),new Vector2Int(-1,-4),new Vector2Int(0,-3),new Vector2Int(0,-4),new Vector2Int(1,-5),new Vector2Int(-2,-3),new Vector2Int(0,-6),new Vector2Int(-2,-4),new Vector2Int(2,-6),new Vector2Int(2,-5)};
 foreach(var c in cells){if(c==center)continue;var p=Pos(c.x,c.y);Place("decoration/props/barrel",props,p+new Vector3(.65f,0,.55f),20,.8f,true);if(rng.NextDouble()<.65)Place("decoration/props/crate_A_small",props,p+new Vector3(.65f,0,.2f),rng.Next(4)*90,.85f,true);}
 Place("decoration/props/resource_lumber",props,Pos(-2,-3)+new Vector3(-.7f,0,.2f),90,.8f,true);
 Place("decoration/props/haybale",props,Pos(0,-6)+new Vector3(.65f,0,.5f),0,.85f,true);
 Place("decoration/props/trough",props,Pos(0,-6)+new Vector3(-.65f,0,.5f),0,.8f,true);
 Place("decoration/props/wheelbarrow",props,Pos(1,-5)+new Vector3(.65f,0,-.5f),-25,.8f,true);
 Place("decoration/props/flag_yellow",props,Pos(0,-4)+new Vector3(-.65f,0,-.4f),0,.9f);
 // Short rustic fences, leaving the village entrances open.
 foreach(var c in new[]{new Vector2Int(-2,-4),new Vector2Int(-2,-3),new Vector2Int(0,-6),new Vector2Int(1,-6)})Place("buildings/neutral/fence_wood_straight",props,Pos(c.x,c.y)+new Vector3(0,0,-.85f),0,.8f);
 foreach(var c in new[]{new Vector2Int(-3,-3),new Vector2Int(-2,-2),new Vector2Int(1,-6),new Vector2Int(3,-5),new Vector2Int(2,-3),new Vector2Int(-1,-6)}){Place("decoration/nature/tree_single_B",trees,Pos(c.x,c.y)+new Vector3(-.3f,0,.2f),rng.Next(6)*60,1);Place("decoration/nature/rock_single_E",trees,Pos(c.x,c.y)+new Vector3(.4f,0,-.2f),45,1.2f,true);}
 var cam=Camera.main;cam.transform.position=new Vector3(24,34,-42);cam.transform.LookAt(new Vector3(0,0,.5f));cam.orthographicSize=21.5f;
 var used=new HashSet<Vector3>();foreach(var group in new[]{ground,river,root.transform.GetChild(6)})foreach(Transform t in group)if(!used.Add(t.position))throw new Exception("Duplicate terrain cell "+t.name);
 if(used.Count!=331)throw new Exception("Unexpected map size: "+used.Count);
 EditorSceneManager.SaveScene(root.scene);
 ValleHexagonalBuilder.Capture(cam);File.Copy("Temp/ValleDelRio.png","Documentation/ValleDelRio-base-rustica.png",true);
 var oldP=cam.transform.position;var oldR=cam.transform.rotation;float oldSize=cam.orthographicSize;
 cam.transform.position=Pos(0,-4)+new Vector3(7,12,-12);cam.transform.LookAt(Pos(0,-4));cam.orthographicSize=7;
 ValleHexagonalBuilder.Capture(cam);File.Copy("Temp/ValleDelRio.png","Documentation/BaseYellow-detalle.png",true);
 cam.transform.SetPositionAndRotation(oldP,oldR);cam.orthographicSize=oldSize;EditorSceneManager.SaveScene(root.scene);
 File.WriteAllText("Documentation/BaseYellow-validacion.txt",$"Unique terrain cells: {used.Count}\nYellow buildings: {houses.childCount+work.childCount}\nTotal new settlement renderers: {settlement.GetComponentsInChildren<MeshRenderer>().Length}\nCleared decoration objects: {cleared}\n");
 RoadFix.Apply();
 Selection.activeGameObject=settlement.gameObject;if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.LookAt(Pos(0,-4),Quaternion.Euler(48,25,0),9,true,true);
 }
}
