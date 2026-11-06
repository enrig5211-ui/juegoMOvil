using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Authoring tool only: the saved scene contains ordinary, editable prefab instances.
public static class ValleHexagonalBuilder
{
    const string Root = "Assets/KayKit/Packs/KayKit - Medieval Hexagon Pack (for Unity)/Prefabs/";
    const string Output = "Assets/Scenes/ValleDelRio.unity";
    static readonly Vector2Int[] Directions = { new Vector2Int(1,0),new Vector2Int(0,1),new Vector2Int(-1,1),new Vector2Int(-1,0),new Vector2Int(0,-1),new Vector2Int(1,-1) };
    static readonly Vector2Int[] River = {
        new Vector2Int(-5,0),new Vector2Int(-4,0),new Vector2Int(-3,0),new Vector2Int(-2,0),
        new Vector2Int(-2,1),new Vector2Int(-1,1),new Vector2Int(0,1),new Vector2Int(1,0),
        new Vector2Int(2,0),new Vector2Int(3,0),new Vector2Int(3,-1),new Vector2Int(4,-1),new Vector2Int(5,-1)
    };
    static Vector3 Position(Vector2Int c) { return new Vector3(2*c.x+c.y,0,Mathf.Sqrt(3)*c.y); }
    static Transform Group(string name, Transform parent) {
        var g=new GameObject(name); g.transform.SetParent(parent,false); return g.transform;
    }
    static GameObject Place(string relative, Transform parent, Vector3 pos, float angle=0, float scale=1) {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+relative+".prefab");
        if(!prefab) throw new InvalidOperationException("Missing prefab: "+relative);
        var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);
        go.transform.localPosition=pos; go.transform.localRotation=Quaternion.Euler(0,angle,0); go.transform.localScale=Vector3.one*scale;
        return go;
    }
    static int Direction(Vector2Int delta) { for(int i=0;i<6;i++) if(Directions[i]==delta) return i; throw new Exception("Nonadjacent river tiles"); }
    static void RiverShape(int a,int b,out string shape,out float angle) {
        int[] ports={3,4,5}; string[] names={"A","B","C"};
        for(int j=0;j<3;j++) for(int k=0;k<6;k++) {
            int p=k, q=(ports[j]+k)%6;
            if((p==a&&q==b)||(p==b&&q==a)) { shape=names[j];angle=-60*k;return; }
        }
        throw new Exception("Invalid river ports");
    }
    [MenuItem("Tools/Escenario/Crear Valle del Rio")]
    public static void Build() {
        if(GameObject.Find("VALLE DEL RIO")) { Debug.Log("El valle ya existe. Edita sus prefabs en Hierarchy.");return; }
        var scene=SceneManager.GetActiveScene();
        if(!scene.IsValid() || EditorApplication.isPlayingOrWillChangePlaymode) return;
        // Preserve the original scene, including any unsaved changes, before authoring.
        if(!File.Exists("Assets/Scenes/AntesDelValle.unity")) EditorSceneManager.SaveScene(scene,"Assets/Scenes/AntesDelValle.unity",true);
        var root=new GameObject("VALLE DEL RIO"); Undo.RegisterCreatedObjectUndo(root,"Crear valle hexagonal");
        var ground=Group("01 - Bases hexagonales",root.transform);
        var water=Group("02 - Rio conectado",root.transform);
        var forest=Group("03 - Bosques",root.transform);
        var hills=Group("04 - Montanas y colinas",root.transform);
        var detail=Group("05 - Rocas y vegetacion de ribera",root.transform);
        var bridge=Group("06 - Puente",root.transform);
        var rng=new System.Random(2410);
        var riverCells=new HashSet<Vector2Int>(River);
        int tileCount=0;
        for(int r=-5;r<=5;r++) for(int q=-5;q<=5;q++) {
            if(Mathf.Abs(q+r)>5) continue;
            var cell=new Vector2Int(q,r); var p=Position(cell); tileCount++;
            if(riverCells.Contains(cell)) continue;
            var tile=Place("tiles/base/hex_grass",ground,p);
            tile.name=$"Pradera [{q}, {r}]";
        }
        for(int i=0;i<River.Length;i++) {
            int a=i==0?3:Direction(River[i-1]-River[i]);
            int b=i==River.Length-1?0:Direction(River[i+1]-River[i]);
            RiverShape(a,b,out string shape,out float rot);
            if(shape=="A" && i%2==1) shape="A_curvy";
            var tile=Place("tiles/rivers/hex_river_"+shape,water,Position(River[i]),rot);
            tile.name=$"Rio {i+1:00} [{River[i].x}, {River[i].y}]";
        }
        // Rear ridge and scattered groves; reserve the central meadow and both river banks.
        for(int r=-5;r<=5;r++) for(int q=-5;q<=5;q++) {
            if(Mathf.Abs(q+r)>5) continue;
            var cell=new Vector2Int(q,r); if(riverCells.Contains(cell)) continue;
            Vector3 p=Position(cell); float pick=(float)rng.NextDouble();
            float yaw=rng.Next(6)*60;
            bool bank=false; foreach(var d in Directions) if(riverCells.Contains(cell+d)) bank=true;
            if(r>=3 && !bank && pick<0.78f) {
                string[] variants={"mountain_A_grass_trees","mountain_B_grass","mountain_C_grass_trees","hills_A_trees"};
                Place("decoration/nature/"+variants[rng.Next(variants.Length)],hills,p,yaw,r>=4?1.05f:0.9f);
            } else if(!bank && (p.x < -2 || p.x>4 || r<=-3) && pick<0.76f) {
                string[] variants={"trees_A_medium","trees_B_medium","trees_A_small","trees_B_small","hills_B_trees"};
                Place("decoration/nature/"+variants[rng.Next(variants.Length)],forest,p,yaw,0.82f+(float)rng.NextDouble()*0.16f);
            } else if(pick<0.48f) {
                Vector3 offset=new Vector3(-0.35f,0,0.3f);
                Place("decoration/nature/tree_single_"+(rng.Next(2)==0?"A":"B"),forest,p+offset,yaw,0.8f+(float)rng.NextDouble()*0.3f);
                Place("decoration/nature/rock_single_C",detail,p+new Vector3(0.4f,0,0.1f),yaw,1.5f);
            }
            if(bank) {
                Place("decoration/nature/rock_single_"+(rng.Next(2)==0?"B":"E"),detail,p+new Vector3(0.2f,0,-0.35f),yaw,1.1f);
                Place("decoration/nature/waterplant_C",detail,p+new Vector3(-0.3f,0,-0.4f),yaw,1.3f);
            }
        }
        Place("buildings/neutral/building_bridge_A",bridge,Position(new Vector2Int(2,0)),0);
        foreach(int i in new[]{1,5,8,11}) Place("decoration/nature/waterlily_B",detail,Position(River[i])+new Vector3(0,-0.095f,0.18f),i*35,1.1f);
        var cam=Camera.main;
        if(!cam) { var cg=new GameObject("Main Camera");cam=cg.AddComponent<Camera>();cg.tag="MainCamera";cg.AddComponent<AudioListener>(); }
        Undo.RecordObject(cam,"Encuadrar valle");Undo.RecordObject(cam.transform,"Encuadrar valle");
        cam.transform.position=new Vector3(13,18,-22);cam.transform.LookAt(new Vector3(0,0,0.6f));
        cam.orthographic=true;cam.orthographicSize=11.4f;cam.nearClipPlane=0.1f;cam.farClipPlane=150;
        cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(0.16f,0.23f,0.27f);
        foreach(var light in UnityEngine.Object.FindObjectsByType<Light>()) if(light.type==LightType.Directional) {
            Undo.RecordObject(light,"Luz del valle");Undo.RecordObject(light.transform,"Luz del valle");
            light.transform.rotation=Quaternion.Euler(48,-35,0); light.color=new Color(1,0.91f,0.77f);light.intensity=1.65f; light.shadows=LightShadows.Soft;
        }
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(0.64f,0.73f,0.80f);
        RenderSettings.ambientEquatorColor=new Color(0.45f,0.53f,0.52f);
        RenderSettings.ambientGroundColor=new Color(0.24f,0.29f,0.31f);
        RenderSettings.fog=false;
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene,Output);
        Selection.activeGameObject=root;
        if(SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.LookAt(new Vector3(0,0,0.6f),cam.transform.rotation,15,true,true);
        Capture(cam);
        File.WriteAllText("Temp/valle-result.txt",$"Scene: {Output}\nTiles: {tileCount}\nRiver: {River.Length}\nPrefab instances: {root.GetComponentsInChildren<MeshRenderer>().Length}\n");
        Debug.Log("Valle del Rio guardado con prefabs editables: "+Output);
    }
    public static void Capture(Camera cam) {
        var rt=new RenderTexture(1600,1100,24); var old=cam.targetTexture;var active=RenderTexture.active;
        try { cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
            var tex=new Texture2D(1600,1100,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,1100),0,0);tex.Apply();
            File.WriteAllBytes("Temp/ValleDelRio.png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
        } finally { cam.targetTexture=old;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(rt); }
    }
}
