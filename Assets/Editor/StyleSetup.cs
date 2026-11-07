using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Valle;
using Valle.Lan;
// Aplica la identidad de "Noche en el Chapare": sonidos generados, materiales de efectos,
// botones planos y el menú principal con una imagen nocturna del mapa.
public static class StyleSetup {
 const string Audio="Assets/Audio/Generado/",Effects="Assets/Resources/Effects/",MenuImage="Assets/UI/MenuChapare.png";
 [MenuItem("Tools/Interfaz/Aplicar estilo Noche en el Chapare")]
 public static void Apply(){
  if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
  ConfigureAudio();
  var assets=AssetDatabase.LoadAssetAtPath<LanAssets>("Assets/Resources/Multiplayer/LanAssets.asset");
  assets.nightLoop=Clip("noche_grillos");assets.tensionMusic=Clip("musica_tension");assets.helicopter=Clip("helicoptero");assets.radio=Clip("radio");
  assets.alarm=Clip("alarma");assets.flare=Clip("bengala");assets.cuffs=Clip("esposas");assets.selectMove=Clip("seleccion_mover");assets.selectConfirm=Clip("seleccion_confirmar");
  assets.fxAlpha=ParticleMaterial("FxPolvo",false);assets.fxAdditive=ParticleMaterial("FxChispa",true);
  assets.button=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
  EditorUtility.SetDirty(assets);AssetDatabase.SaveAssets();
  RenderMenuImage();
  StyleMenu();
  Debug.Log("Estilo Noche en el Chapare aplicado");
 }
 static AudioClip Clip(string name){var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Audio+name+".wav");if(!clip)throw new System.Exception("Falta el sonido "+name+": ejecuta Tools/generar_sonidos.py");return clip;}
 static void ConfigureAudio(){
  foreach(var path in Directory.GetFiles(Audio,"*.wav")){
   var importer=(AudioImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));bool longClip=path.Contains("musica")||path.Contains("noche");
   var settings=importer.defaultSampleSettings;settings.loadType=longClip?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=longClip?AudioCompressionFormat.Vorbis:AudioCompressionFormat.PCM;settings.quality=.7f;
   importer.defaultSampleSettings=settings;importer.forceToMono=true;importer.SaveAndReimport();
  }
 }
 // Copia el material de partículas de URP: alfa para el polvo, aditivo para chispas y fogonazos.
 static Material ParticleMaterial(string name,bool additive){
  string path=Effects+name+".mat";
  if(!File.Exists(path))AssetDatabase.CopyAsset("Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat",path);
  var material=AssetDatabase.LoadAssetAtPath<Material>(path);
  material.SetFloat("_Blend",additive?2:0);material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)(additive?BlendMode.One:BlendMode.OneMinusSrcAlpha));material.SetFloat("_ZWrite",0);
  EditorUtility.SetDirty(material);return material;
 }
 // Foto nocturna del campamento con faroles y personajes, para el fondo del menú.
 static void RenderMenuImage(){
  EditorSceneManager.OpenScene("Assets/Scenes/NocheChapare.unity");
  var sun=Object.FindObjectsByType<Light>().First(l=>l.type==LightType.Directional);sun.color=new Color(.5f,.6f,1);sun.intensity=.25f;
  RenderSettings.ambientSkyColor=new Color(.09f,.11f,.2f);RenderSettings.ambientEquatorColor=new Color(.05f,.07f,.11f);RenderSettings.ambientGroundColor=new Color(.02f,.025f,.035f);
  var assets=AssetDatabase.LoadAssetAtPath<LanAssets>("Assets/Resources/Multiplayer/LanAssets.asset");
  var stage=new GameObject("Escena del menu");
  void Character(int index,Vector3 at,float yaw,bool lamp){
   var model=(GameObject)Object.Instantiate(assets.characters[index],stage.transform);model.transform.SetPositionAndRotation(at,Quaternion.Euler(0,yaw,0));
   if(!lamp)return;var light=new GameObject("Linterna").AddComponent<Light>();light.transform.SetParent(model.transform,false);light.transform.localPosition=new Vector3(0,.55f,.1f);light.transform.localRotation=Quaternion.Euler(6,0,0);
   light.type=LightType.Spot;light.range=16;light.spotAngle=60;light.intensity=14;light.color=new Color(1,.95f,.82f);
  }
  Character(0,new Vector3(-4.5f,0,-9.5f),20,true);Character(1,new Vector3(-2.8f,0,-10.5f),10,true);Character(2,new Vector3(-6,0,-11),35,true);
  Character(3,new Vector3(1.5f,0,-2.5f),200,false);Character(4,new Vector3(-1.2f,0,-3),170,false);Character(6,new Vector3(.2f,0,-1.2f),185,false);Character(5,new Vector3(3,0,-1),220,false);
  var eye=new GameObject("Camara del menu").AddComponent<Camera>();eye.orthographic=true;eye.orthographicSize=6.2f;eye.clearFlags=CameraClearFlags.SolidColor;eye.backgroundColor=new Color(.01f,.015f,.03f);eye.farClipPlane=300;
  var view=Quaternion.Euler(38,25,0);eye.transform.SetPositionAndRotation(new Vector3(-1.5f,.5f,-5)-view*Vector3.forward*40,view);
  var texture=new RenderTexture(1920,1080,24);eye.targetTexture=texture;eye.Render();eye.Render();RenderTexture.active=texture;
  var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();RenderTexture.active=null;eye.targetTexture=null;
  Directory.CreateDirectory("Assets/UI");File.WriteAllBytes(MenuImage,image.EncodeToPNG());texture.Release();
  AssetDatabase.ImportAsset(MenuImage);var importer=(TextureImporter)AssetImporter.GetAtPath(MenuImage);importer.mipmapEnabled=false;importer.SaveAndReimport();
 }
 // Menú principal: fondo nocturno, textos del juego, botones planos y música de tensión. El título no se toca.
 static void StyleMenu(){
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/MenuInicio.unity");
  var assets=AssetDatabase.LoadAssetAtPath<LanAssets>("Assets/Resources/Multiplayer/LanAssets.asset");
  var font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
  var root=GameObject.Find("UI - Inicio");
  var view=root.transform.Find("Vista del valle").GetComponent<RawImage>();view.texture=AssetDatabase.LoadAssetAtPath<Texture2D>(MenuImage);
  root.transform.Find("Velo").GetComponent<Image>().color=new Color(.01f,.02f,.05f,.35f);
  foreach(var image in root.GetComponentsInChildren<Image>(true))if(image.name=="Menu principal"||image.name=="Guia")image.color=new Color(.03f,.05f,.08f,.9f);
  void Say(string from,string to){foreach(var text in root.GetComponentsInChildren<Text>(true))if(text.text==from)text.text=to;}
  Say("U N A   A V E N T U R A","D E A   C O N T R A   E L   C A R T E L");
  Say("Tu viaje comienza aquí","Captura al líder antes del amanecer");
  Say("EXPLORA  ·  DESCUBRE  ·  SOBREVIVE","DESPLIÉGATE  ·  BUSCA  ·  CAPTURA");
  foreach(var text in root.GetComponentsInChildren<Text>(true)){
   text.font=font;bool heading=text.fontSize>=30;text.fontStyle=heading||text.GetComponentInParent<Button>()?FontStyle.Bold:FontStyle.Normal;
   if(heading)text.color=new Color(1,.72f,.25f);
  }
  foreach(var button in root.GetComponentsInChildren<Button>(true)){
   var image=button.GetComponent<Image>();image.sprite=assets.button;image.type=Image.Type.Sliced;image.color=Color.white;
   var colors=button.colors;colors.normalColor=new Color(.13f,.17f,.24f);colors.highlightedColor=new Color(.85f,.52f,.1f);colors.selectedColor=colors.highlightedColor;colors.pressedColor=new Color(.6f,.36f,.06f);button.colors=colors;
   var outline=button.GetComponent<Outline>();if(!outline)outline=button.gameObject.AddComponent<Outline>();outline.effectColor=new Color(1,.72f,.25f,.55f);outline.effectDistance=new Vector2(1.5f,-1.5f);
   foreach(var label in button.GetComponentsInChildren<Text>(true))label.color=new Color(.93f,.94f,.95f);
  }
  var music=Object.FindObjectsByType<AmbientMusic>().FirstOrDefault();
  if(music){music.name="AMBIENTE - Tension";var source=music.GetComponent<AudioSource>();source.clip=assets.tensionMusic;music.volume=.22f;}
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 }
}
