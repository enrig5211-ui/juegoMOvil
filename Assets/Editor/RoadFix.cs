using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
public static class RoadFix {
 const string P="Assets/KayKit/Packs/KayKit - Medieval Hexagon Pack (for Unity)/Prefabs/";
 static readonly Vector2Int[] D={new Vector2Int(1,0),new Vector2Int(0,1),new Vector2Int(-1,1),new Vector2Int(-1,0),new Vector2Int(0,-1),new Vector2Int(1,-1)};
 static Vector3 Pos(Vector2Int c)=>new Vector3(c.x*2+c.y,0,Mathf.Sqrt(3)*c.y);
 static Vector2Int Cell(Vector3 p){int r=Mathf.RoundToInt(p.z/Mathf.Sqrt(3));return new Vector2Int(Mathf.RoundToInt((p.x-r)*.5f),r);}
 static GameObject Place(string path,Transform parent,Vector2Int c,float angle){var src=AssetDatabase.LoadAssetAtPath<GameObject>(P+path+".prefab");if(!src)throw new Exception(path);var go=(GameObject)PrefabUtility.InstantiatePrefab(src,parent);go.transform.position=Pos(c);go.transform.rotation=Quaternion.Euler(0,angle,0);GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic);go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;return go;}
 [MenuItem("Tools/Escenario/Sustituir dirt por caminos hexagonales")]
 public static void Apply(){var root=GameObject.Find("VALLE DEL RIO");var village=root.transform.Find("09 - Base rustica Yellow");var group=village.GetChild(2);if(group.name=="03 - Caminos hexagonales")return;
 EditorSceneManager.SaveScene(root.scene,"Assets/Scenes/ValleDelRio_AntesDeCaminos.unity",true);
 int removed=group.childCount;foreach(Transform t in group.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);group.name="03 - Caminos hexagonales";
 var graph=new Dictionary<Vector2Int,HashSet<int>>();
 Action<Vector2Int,Vector2Int> edge=(a,b)=>{int i=Array.IndexOf(D,b-a);if(i<0)throw new Exception("Non adjacent road");if(!graph.ContainsKey(a))graph[a]=new HashSet<int>();if(!graph.ContainsKey(b))graph[b]=new HashSet<int>();graph[a].Add(i);graph[b].Add((i+3)%6);};
 var main=new[]{new Vector2Int(1,-3),new Vector2Int(1,-2),new Vector2Int(2,-2),new Vector2Int(2,-1),new Vector2Int(2,0),new Vector2Int(2,1)};
 for(int i=1;i<main.Length;i++)edge(main[i-1],main[i]);edge(new Vector2Int(1,-2),new Vector2Int(0,-1));edge(new Vector2Int(0,-1),new Vector2Int(-1,-1));
 int[][] ports={new[]{0,3},new[]{0,4},new[]{0,5},new[]{0,2,4},new[]{0,3,4},new[]{0,2,3},new[]{0,1,5},new[]{0,1,3,5},new[]{1,2,4,5},new[]{0,1,2,3},new[]{0,1,2,4,5},new[]{0,1,2,3,4,5},new[]{0}};
 var terrain=root.transform.GetChild(0);var river=root.transform.GetChild(1);
 foreach(var item in graph){var c=item.Key;bool crossing=c==new Vector2Int(2,0);var old=(crossing?river:terrain).Cast<Transform>().FirstOrDefault(t=>Cell(t.position)==c);if(!old)throw new Exception("Missing tile "+c);
 string shape=null;float yaw=0;
 if(!crossing){for(int j=0;j<ports.Length&&shape==null;j++)for(int k=0;k<6;k++)if(item.Value.SetEquals(ports[j].Select(x=>(x+k)%6))){shape=((char)('A'+j)).ToString();yaw=-60*k;break;}if(shape==null)throw new Exception("Missing road shape");}
 UnityEngine.Object.DestroyImmediate(old.gameObject);
 var tile=Place(crossing?"tiles/rivers/hex_river_crossing_B":"tiles/roads/hex_road_"+shape,crossing?river:group,c,yaw);tile.name=(crossing?"Cruce del puente":"Camino")+$" [{c.x}, {c.y}]";
 foreach(var gi in new[]{2,3,4})foreach(Transform t in root.transform.GetChild(gi).Cast<Transform>().ToArray())if(Cell(t.position)==c)UnityEngine.Object.DestroyImmediate(t.gameObject);
 }
 var bridge=root.transform.GetChild(5).GetChild(0);bridge.rotation=Quaternion.Euler(0,30,0);
 // Keep every road-to-road edge reciprocal; there is one terrain prefab per axial cell.
 foreach(var item in graph)foreach(int d in item.Value)if(!graph[item.Key+D[d]].Contains((d+3)%6))throw new Exception("Disconnected road");
 var cells=new HashSet<Vector2Int>();foreach(var g in new[]{terrain,river,root.transform.GetChild(6),group})foreach(Transform t in g)if(!cells.Add(Cell(t.position)))throw new Exception("Duplicate terrain");if(cells.Count!=331)throw new Exception("Missing terrain");
 EditorSceneManager.SaveScene(root.scene);
 var cam=Camera.main;ValleHexagonalBuilder.Capture(cam);File.Copy("Temp/ValleDelRio.png","Documentation/ValleDelRio-base-rustica.png",true);var p=cam.transform.position;var rot=cam.transform.rotation;float size=cam.orthographicSize;cam.transform.position=new Vector3(-4,0,-6.9282f)+new Vector3(7,12,-12);cam.transform.LookAt(new Vector3(-2,0,-5));cam.orthographicSize=8;ValleHexagonalBuilder.Capture(cam);File.Copy("Temp/ValleDelRio.png","Documentation/BaseYellow-detalle.png",true);cam.transform.SetPositionAndRotation(p,rot);cam.orthographicSize=size;EditorSceneManager.SaveScene(root.scene);
 File.WriteAllText("Documentation/Caminos-validacion.txt",$"Removed dirt objects: {removed}\nRoad prefabs: {group.childCount}\nRiver crossing: 1\nUnique terrain cells: {cells.Count}\nAll road graph connections reciprocal; rotation multiples of 60 degrees.\n");Selection.activeGameObject=group.gameObject;
 }
}
