using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
public static class ValleExpansion {
 const string P="Assets/KayKit/Packs/KayKit - Medieval Hexagon Pack (for Unity)/Prefabs/";
 static readonly Vector2Int[] D={new Vector2Int(1,0),new Vector2Int(0,1),new Vector2Int(-1,1),new Vector2Int(-1,0),new Vector2Int(0,-1),new Vector2Int(1,-1)};
 static Vector3 Pos(int q,int r)=>new Vector3(2*q+r,0,Mathf.Sqrt(3)*r);
 static int Radius(int q,int r)=>Mathf.Max(Mathf.Abs(q),Mathf.Abs(r),Mathf.Abs(q+r));
 static Transform Group(string n,Transform p){var g=new GameObject(n);g.transform.SetParent(p,false);return g.transform;}
 static GameObject Place(string path,Transform p,Vector3 pos,float yaw=0,float scale=1){var src=AssetDatabase.LoadAssetAtPath<GameObject>(P+path+".prefab");if(!src)throw new Exception(path);var g=(GameObject)PrefabUtility.InstantiatePrefab(src,p);g.transform.position=pos;g.transform.rotation=Quaternion.Euler(0,yaw,0);g.transform.localScale=Vector3.one*scale;return g;}
 // Shared corner classification makes adjacent coast tiles use the same shoreline endpoints.
 static int LakeMask(Vector3 p){int mask=0;var center=Pos(7,-2);for(int i=0;i<6;i++){float a=(30+60*i)*Mathf.Deg2Rad;var v=p+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*1.15470054f;float dx=(v.x-center.x)/3.9f;float dz=(v.z-center.z)/3.3f;if(dx*dx+dz*dz<1)mask|=1<<i;}return mask;}
 static bool Coast(int mask,out string name,out float yaw){int[] patterns={6,7,15,31,2};string[] names={"A","B","C","D","E"};for(int j=0;j<5;j++)for(int k=0;k<6;k++){int rotated=((patterns[j]<<k)|(patterns[j]>>(6-k)))&63;if(rotated==mask){name=names[j];yaw=-60*k;return true;}}name="";yaw=0;return false;}
 [MenuItem("Tools/Escenario/Ampliar valle con lago")]
 public static void Expand(){
 var root=GameObject.Find("VALLE DEL RIO");if(!root)throw new Exception("Abre ValleDelRio primero.");if(root.transform.Find("07 - Lago y playas"))return;
 EditorSceneManager.SaveScene(root.scene,"Assets/Scenes/ValleDelRio_AntesDeAmpliar.unity",true);
 var ground=root.transform.GetChild(0);var river=root.transform.GetChild(1);var woods=root.transform.GetChild(2);var hills=root.transform.GetChild(3);var details=root.transform.GetChild(4);
 var lake=Group("07 - Lago y playas",root.transform);var shore=Group("08 - Decoracion del lago",root.transform);
 var rng=new System.Random(842);int coastCount=0,waterCount=0;
 var occupied=new Dictionary<Vector2Int,Transform>();foreach(Transform g in new[]{ground,river})foreach(Transform t in g){int r=Mathf.RoundToInt(t.position.z/Mathf.Sqrt(3));int q=Mathf.RoundToInt((t.position.x-r)/2);occupied[new Vector2Int(q,r)]=t;}
 for(int r=-9;r<=9;r++)for(int q=-9;q<=9;q++){
 if(Radius(q,r)>9)continue;var c=new Vector2Int(q,r);var p=Pos(q,r);int mask=LakeMask(p);
 bool original=occupied.ContainsKey(c);bool isRiver=original && occupied[c].parent==river;
 // Preserve the existing river through its last bank; it discharges into the lake.
 if(isRiver)continue;
 if(original && mask==0)continue;
 if(original){UnityEngine.Object.DestroyImmediate(occupied[c].gameObject);foreach(var group in new[]{woods,hills,details})foreach(Transform t in group.Cast<Transform>().ToArray())if(Vector3.Distance(new Vector3(t.position.x,0,t.position.z),p)<1.12f)UnityEngine.Object.DestroyImmediate(t.gameObject);}
 if(mask==63){Place("tiles/base/hex_water",lake,p).name=$"Agua [{q}, {r}]";waterCount++;continue;}
 if(mask!=0){if(!Coast(mask,out var shape,out var yaw))throw new Exception("Invalid coast mask "+mask);Place("tiles/coast/hex_coast_"+shape,lake,p,yaw).name=$"Playa [{q}, {r}]";coastCount++;continue;}
 if(r==0&&q < -5){Place("tiles/rivers/hex_river_A_curvy",river,p).name=$"Rio ampliado [{q}, {r}]";continue;}
 Place("tiles/base/hex_grass",ground,p).name=$"Pradera [{q}, {r}]";
 float pick=(float)rng.NextDouble();float angle=rng.Next(6)*60;bool nearLake=Vector3.Distance(p,Pos(7,-2))<6;
 if(r>=5&&pick<.72f){string[] a={"mountain_A_grass_trees","mountain_B_grass","mountain_C_grass_trees","hills_C_trees"};Place("decoration/nature/"+a[rng.Next(a.Length)],hills,p,angle,.9f+(float)rng.NextDouble()*.2f);}
 else if(!nearLake && Mathf.Abs(r)>1 && pick<.62f){string[] a={"trees_A_medium","trees_B_large","trees_B_small","hills_B_trees","trees_A_small"};Place("decoration/nature/"+a[rng.Next(a.Length)],woods,p,angle,.85f);}
 else if(pick<.38f){Place("decoration/nature/tree_single_B",woods,p+new Vector3(-.3f,0,.25f),angle,.9f);Place("decoration/nature/rock_single_E",details,p+new Vector3(.35f,0,-.2f),angle,1.5f);}
 }
 // Sample actual surface height for reeds and beach rocks, never placing trees in water.
 foreach(Transform tile in lake){var mesh=tile.GetComponent<MeshFilter>().sharedMesh;for(int k=0;k<7;k++){Vector2 local=new Vector2((float)rng.NextDouble()*1.4f-.7f,(float)rng.NextDouble()*1.4f-.7f);float h=Height(mesh,local);var world=tile.TransformPoint(new Vector3(local.x,h,local.y));if(h>-.08f && k<3){Place("decoration/nature/rock_single_"+(k%2==0?"B":"E"),shore,world,rng.Next(360),1.2f);}else if(h<-.15f && k<2){Place("decoration/nature/waterlily_B",shore,world+Vector3.up*.005f,rng.Next(360),1.2f);}}}
 var cam=Camera.main;cam.transform.position=new Vector3(22,31,-38);cam.transform.LookAt(new Vector3(0,0,0.5f));cam.orthographicSize=19.8f;cam.farClipPlane=120;
 var renderers=root.GetComponentsInChildren<MeshRenderer>();long tris=0;int tiny=0;
 foreach(var renderer in renderers){var mf=renderer.GetComponent<MeshFilter>();tris+=mf.sharedMesh.triangles.Length/3;
 GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,GameObjectUtility.GetStaticEditorFlags(renderer.gameObject)|StaticEditorFlags.BatchingStatic);
 if(renderer.transform.parent==ground || renderer.transform.parent==river || renderer.transform.parent==lake || renderer.name.Contains("rock_single")||renderer.name.Contains("waterplant")||renderer.name.Contains("waterlily")){renderer.shadowCastingMode=ShadowCastingMode.Off;tiny++;}
 }
 // URP's SRP Batcher is already enabled. Keep shared materials and meshes, with no runtime generation.
 EditorSceneManager.MarkSceneDirty(root.scene);EditorSceneManager.SaveScene(root.scene);
 ValleHexagonalBuilder.Capture(cam);File.Copy("Temp/ValleDelRio.png","Documentation/ValleDelRio-ampliado.png",true);
 var report=$"Terrain cells: {ground.childCount+river.childCount+lake.childCount}\nLake water tiles: {waterCount}\nCoast tiles: {coastCount}\nRenderers: {renderers.Length}\nTriangles (all scene meshes): {tris}\nBatching static renderers: {renderers.Length}\nSmall decoration shadow casters disabled: {tiny}\nSRP Batcher: already enabled in PC pipeline\n";
 File.WriteAllText("Documentation/ValleDelRio-ampliado-validacion.txt",report);
 Selection.activeGameObject=root;if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.LookAt(Vector3.zero,cam.transform.rotation,26,true,true);
 Debug.Log("Valle ampliado: "+report);
 }
 static float Height(Mesh m,Vector2 p){float h=-100;var v=m.vertices;var t=m.triangles;for(int i=0;i<t.Length;i+=3){var a=v[t[i]];var b=v[t[i+1]];var c=v[t[i+2]];float den=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);if(Mathf.Abs(den)<.000001f)continue;float u=((b.z-c.z)*(p.x-c.x)+(c.x-b.x)*(p.y-c.z))/den;float w=((c.z-a.z)*(p.x-c.x)+(a.x-c.x)*(p.y-c.z))/den;float k=1-u-w;if(u>=-.0001f&&w>=-.0001f&&k>=-.0001f)h=Mathf.Max(h,u*a.y+w*b.y+k*c.y);}return h;}
}
