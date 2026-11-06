using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using Valle;
public static class InterfaceSetup {
 const string Game="Assets/Scenes/ValleDelRio.unity", Menu="Assets/Scenes/MenuInicio.unity";
 static readonly Color Gold=new Color(.8f,.66f,.39f), Ink=new Color(.045f,.075f,.08f,.94f), Paper=new Color(.94f,.89f,.76f);
 static Font font; static Sprite buttonSprite;
 [MenuItem("Tools/Interfaz/Crear menu y HUD")]
 public static void Build(){
  Directory.CreateDirectory("Assets/UI");font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Artsystack - Fantasy RPG GUI/Resources/Font/Kurale-Regular.ttf");
  buttonSprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Artsystack - Fantasy RPG GUI/Resources/Sprites/components/button_01.png");
  if(SceneManager.GetActiveScene().path!=Game){if(!GameObject.Find("UI - Inicio")&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;EditorSceneManager.OpenScene(Game);}
  if(!File.Exists("Assets/Scenes/ValleDelRio_AntesDeInterfaz.unity"))EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),"Assets/Scenes/ValleDelRio_AntesDeInterfaz.unity",true);
  var old=GameObject.Find("UI - Valle");if(old)Object.DestroyImmediate(old);
  var ui=Canvas("UI - Valle");ui.stats=Object.FindFirstObjectByType<PlayerStats>();ui.weapon=Object.FindFirstObjectByType<PlayerWeapon>();
  ui.hud=Rect("HUD",ui.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(1280,720)).gameObject;
  var status=Panel("Estado",ui.hud.transform,new Vector2(0,1),new Vector2(194,-95),new Vector2(332,138));
  Label(status,"EXPLORADOR",new Vector2(0,43),new Vector2(284,25),16,Gold);
  ui.healthText=Label(status,"VIDA  100 / 100",new Vector2(0,16),new Vector2(284,22),16,Paper);ui.healthFill=Bar(status,-2,new Color(.77f,.23f,.25f));
  ui.staminaText=Label(status,"ESTAMINA  100 / 100",new Vector2(0,-24),new Vector2(284,22),14,Paper);ui.staminaFill=Bar(status,-41,new Color(.28f,.72f,.53f));
  ui.defenseText=Label(ui.hud.transform,"DEFENSA  15",new Vector2(92, -181),new Vector2(128,25),14,Paper,new Vector2(0,1));
  var help=Label(ui.hud.transform,"ESC   Pausa y controles",new Vector2(-144,-42),new Vector2(240,26),16,Paper,new Vector2(1,1));help.alignment=TextAnchor.MiddleRight;
  var wp=Panel("Arma equipada",ui.hud.transform,new Vector2(1,0),new Vector2(-178,96),new Vector2(300,140));ui.weaponPanel=wp.gameObject;
  ui.weaponText=Label(wp,"PISTOLA",new Vector2(0,43),new Vector2(252,24),16,Gold);
  ui.ammoText=Label(wp,"12 / 060",new Vector2(0,7),new Vector2(252,45),20,Paper);
  Label(wp,"CLIC  Disparar    R  Recargar    G  Soltar",new Vector2(0,-44),new Vector2(266,24),13,Paper);
  var prompt=Panel("Interaccion",ui.hud.transform,new Vector2(.5f,0),new Vector2(0,78),new Vector2(330,54));ui.promptPanel=prompt.gameObject;
  var key=Panel("Tecla F",prompt,new Vector2(.5f,.5f),new Vector2(-135,0),new Vector2(34,34));Label(key,"F",Vector2.zero,new Vector2(32,32),20,Gold);
  ui.promptText=Label(prompt,"Recoger Pistola",new Vector2(22,0),new Vector2(258,36),19,Paper);ui.promptText.alignment=TextAnchor.MiddleCenter;
  ui.pausePanel=Overlay(ui.transform,"Pausa");var pause=Card(ui.pausePanel.transform,"PAUSA","Un respiro en el valle");
  ui.resumeButton=Button(pause,"Continuar",45);ui.controlsButton=Button(pause,"Controles",-20);ui.menuButton=Button(pause,"Volver al inicio",-85);
  ui.deathPanel=Overlay(ui.transform,"Sin vida");var death=Card(ui.deathPanel.transform,"SIN VIDA","Tu arma espera donde cayó");ui.respawnButton=Button(death,"Reaparecer  ·  R",10);ui.deathPanel.SetActive(false);
  Controls(ui);ui.pausePanel.SetActive(false);ui.weaponPanel.SetActive(false);ui.promptPanel.SetActive(false);
  EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
  // Capture the existing valley as a lightweight menu background, with no duplicate gameplay scene.
  Directory.CreateDirectory("Assets/UI");var camObj=new GameObject("Menu capture");var cam=camObj.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=22;cam.transform.rotation=Quaternion.Euler(48,30,0);cam.transform.position=-cam.transform.forward*60;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.09f,.16f,.17f);
  var rt=new RenderTexture(1600,900,24);cam.targetTexture=rt;cam.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);tex.ReadPixels(new UnityEngine.Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes("Assets/UI/ValleMenu.png",tex.EncodeToPNG());RenderTexture.active=previous;cam.targetTexture=null;Object.DestroyImmediate(tex);Object.DestroyImmediate(rt);Object.DestroyImmediate(camObj);AssetDatabase.ImportAsset("Assets/UI/ValleMenu.png");
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));camera.tag="MainCamera";camera.GetComponent<Camera>().backgroundColor=Ink;camera.GetComponent<Camera>().clearFlags=CameraClearFlags.SolidColor;
  ui=Canvas("UI - Inicio");ui.mainMenu=true;
  var bg=Rect("Vista del valle",ui.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(1600,900));bg.anchoredPosition=new Vector2(230,0);var raw=bg.gameObject.AddComponent<RawImage>();raw.texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UI/ValleMenu.png");raw.color=new Color(.8f,.85f,.82f);raw.raycastTarget=false;
  var shade=Panel("Velo",ui.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(1280,720));shade.GetComponent<Image>().color=new Color(.025f,.055f,.065f,.38f);
  var menu=Panel("Menu principal",ui.transform,new Vector2(0,.5f),new Vector2(300,0),new Vector2(460,600));
  Label(menu,"U N A   A V E N T U R A",new Vector2(0,232),new Vector2(370,28),15,Gold).alignment=TextAnchor.MiddleCenter;
  Label(menu,"Valle del Río",new Vector2(0,155),new Vector2(420,90),54,Paper).alignment=TextAnchor.MiddleCenter;
  Label(menu,"Tu viaje comienza aquí",new Vector2(0,88),new Vector2(380,35),21,Paper).alignment=TextAnchor.MiddleCenter;
  ui.playButton=Button(menu,"Jugar",-10);ui.controlsButton=Button(menu,"Controles",-80);ui.quitButton=Button(menu,"Salir",-150);
  Label(menu,"EXPLORA  ·  DESCUBRE  ·  SOBREVIVE",new Vector2(0,-247),new Vector2(400,25),13,Gold).alignment=TextAnchor.MiddleCenter;
  Controls(ui);
  var music=new GameObject("AMBIENTE - Moonspire",typeof(AudioSource));music.GetComponent<AudioSource>().clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/10 Fantasy Medieval Ambient Tracks/Loops/1. Moonspire.wav");music.AddComponent<AmbientMusic>();
  EditorSceneManager.SaveScene(scene,Menu);
  var entries=new System.Collections.Generic.List<EditorBuildSettingsScene>{new EditorBuildSettingsScene(Menu,true),new EditorBuildSettingsScene(Game,true)};
  foreach(var entry in EditorBuildSettings.scenes)if(entry.path!=Menu&&entry.path!=Game)entries.Add(entry);EditorBuildSettings.scenes=entries.ToArray();AssetDatabase.SaveAssets();
 }
 static ValleInterface Canvas(string name){var go=new GameObject(name,typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));go.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
  if(!Object.FindFirstObjectByType<EventSystem>()){var ev=new GameObject("EventSystem",typeof(EventSystem));var module=ev.AddComponent<InputSystemUIInputModule>();module.actionsAsset=AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/InputSystem_Actions.inputactions");module.point=Ref(module,"Point");module.leftClick=Ref(module,"Click");module.move=Ref(module,"Navigate");module.submit=Ref(module,"Submit");module.cancel=Ref(module,"Cancel");module.scrollWheel=Ref(module,"ScrollWheel");}return go.AddComponent<ValleInterface>();}
 static UnityEngine.InputSystem.InputActionReference Ref(InputSystemUIInputModule module,string name){var action=module.actionsAsset.FindAction("UI/"+name,true);var path="Assets/UI/Input-"+name+".asset";var reference=AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionReference>(path);if(!reference){reference=UnityEngine.InputSystem.InputActionReference.Create(action);AssetDatabase.CreateAsset(reference,path);}return reference;}
 static RectTransform Rect(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=anchor;r.sizeDelta=size;r.anchoredPosition=pos;return r;}
 static RectTransform Panel(string name,Transform p,Vector2 a,Vector2 pos,Vector2 size){var r=Rect(name,p,a,pos,size);r.gameObject.AddComponent<Image>().color=Ink;return r;}
 static Text Label(Transform p,string text,Vector2 pos,Vector2 size,int fontSize,Color color,Vector2? anchor=null){var r=Rect(text,p,anchor??new Vector2(.5f,.5f),pos,size);var t=r.gameObject.AddComponent<Text>();t.verticalOverflow=VerticalWrapMode.Overflow;t.font=font;t.fontSize=fontSize;t.color=color;t.text=text;t.alignment=TextAnchor.MiddleLeft;t.raycastTarget=false;return t;}
 static Image Bar(Transform p,float y,Color color){var r=Panel("Barra",p,new Vector2(.5f,.5f),new Vector2(0,y),new Vector2(284,9));r.GetComponent<Image>().color=new Color(.15f,.2f,.2f);var fill=Rect("Relleno",r,new Vector2(.5f,.5f),Vector2.zero,new Vector2(284,9)).gameObject.AddComponent<Image>();fill.sprite=Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;fill.color=color;return fill;}
 static Button Button(Transform p,string text,float y){var r=Panel(text,p,new Vector2(.5f,.5f),new Vector2(0,y),new Vector2(310,54));var img=r.GetComponent<Image>();img.sprite=buttonSprite;img.type=Image.Type.Sliced;img.color=Color.white;var b=r.gameObject.AddComponent<Button>();var c=b.colors;c.highlightedColor=new Color(1,.88f,.58f);c.selectedColor=c.highlightedColor;c.pressedColor=new Color(.7f,.6f,.4f);b.colors=c;Label(r,text,Vector2.zero,new Vector2(290,48),23,Paper).alignment=TextAnchor.MiddleCenter;return b;}
 static GameObject Overlay(Transform p,string name){var r=Panel(name,p,new Vector2(.5f,.5f),Vector2.zero,new Vector2(4000,4000));r.GetComponent<Image>().color=new Color(.015f,.03f,.035f,.8f);return r.gameObject;}
 static Transform Card(Transform p,string title,string sub){var r=Panel(title,p,new Vector2(.5f,.5f),Vector2.zero,new Vector2(460,400));Label(r,title,new Vector2(0,145),new Vector2(400,55),40,Gold).alignment=TextAnchor.MiddleCenter;Label(r,sub,new Vector2(0,99),new Vector2(410,30),19,Paper).alignment=TextAnchor.MiddleCenter;return r;}
 static void Controls(ValleInterface ui){ui.controlsPanel=Overlay(ui.transform,"Controles");var p=Panel("Guia",ui.controlsPanel.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(560,590));Label(p,"CONTROLES",new Vector2(0,235),new Vector2(460,50),36,Gold).alignment=TextAnchor.MiddleCenter;
  string[] keys={"W A S D / Flechas","Shift","Espacio","Q / E","F","Clic izquierdo","R","G","Esc"};string[] actions={"Moverte","Correr","Saltar","Girar la cámara","Recoger / intercambiar","Disparar","Recargar / reaparecer","Soltar arma","Pausa"};for(int i=0;i<keys.Length;i++){Label(p,keys[i],new Vector2(-115,175-i*37),new Vector2(230,30),18,Gold);Label(p,actions[i],new Vector2(120,175-i*37),new Vector2(245,30),18,Paper);}ui.backButton=Button(p,"Volver",-228);ui.controlsPanel.SetActive(false);}
}
