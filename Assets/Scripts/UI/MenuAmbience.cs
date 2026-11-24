using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Valle.Lan;
namespace Valle {
 // Da vida al menú principal: fondo que se desplaza despacio, luciérnagas, degradado detrás del panel,
 // título que brilla y una vitrina con la DEA frente a la organización.
 public sealed class MenuAmbience:MonoBehaviour {
  const int Fireflies=28;
  RawImage view;Text title;Shadow glow;RectTransform canvas;
  readonly List<RectTransform> flies=new List<RectTransform>();readonly List<Vector2> seeds=new List<Vector2>();
  readonly List<Transform> actors=new List<Transform>();readonly List<Vector3> scales=new List<Vector3>();
  GameObject studio;RenderTexture showcase;
  void Start(){
   canvas=(RectTransform)transform;
   var background=transform.Find("Vista del valle");if(background)view=background.GetComponent<RawImage>();
   foreach(var text in GetComponentsInChildren<Text>(true))if(text.fontSize>=50){title=text;break;}
   if(title){glow=title.gameObject.AddComponent<Shadow>();glow.effectDistance=new Vector2(0,-3);}
   var panel=transform.Find("Menu principal");
   if(panel)Gradient(panel);
   for(int i=0;i<Fireflies;i++){
    var fly=new GameObject("Luciernaga",typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();fly.SetParent(transform,false);fly.SetSiblingIndex(2);
    float size=Random.Range(4f,8f);fly.sizeDelta=new Vector2(size,size);var image=fly.GetComponent<Image>();image.raycastTarget=false;image.color=new Color(.95f,1,.55f,.8f);
    flies.Add(fly);seeds.Add(new Vector2(Random.value*100,Random.value*100));
   }
   Showcase();
  }
  void Gradient(Transform panel){
   var texture=new Texture2D(256,1,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp};
   for(int x=0;x<256;x++){float k=x/255f;texture.SetPixel(x,0,new Color(.02f,.03f,.06f,Mathf.Lerp(.96f,0,k*k)));}
   texture.Apply();
   var fade=new GameObject("Degradado",typeof(RectTransform),typeof(RawImage)).GetComponent<RawImage>();fade.texture=texture;fade.raycastTarget=false;
   fade.rectTransform.SetParent(transform,false);fade.rectTransform.SetSiblingIndex(panel.GetSiblingIndex());
   fade.rectTransform.anchorMin=new Vector2(0,0);fade.rectTransform.anchorMax=new Vector2(.75f,1);fade.rectTransform.offsetMin=fade.rectTransform.offsetMax=Vector2.zero;
  }
  // Vitrina: tres de la DEA frente a tres de la organización, en un estudio fuera de la vista.
  void Showcase(){
   var assets=Resources.Load<LanAssets>("Multiplayer/LanAssets");if(!assets||assets.characters==null||assets.characters.Length<13)return;
   studio=new GameObject("Vitrina del menu");studio.transform.position=new Vector3(0,-600,0);
   var light=new GameObject("Luz").AddComponent<Light>();light.transform.SetParent(studio.transform,false);light.transform.localPosition=new Vector3(0,3,3);light.type=LightType.Point;light.range=12;light.intensity=4;
   var rim=new GameObject("Contraluz").AddComponent<Light>();rim.transform.SetParent(studio.transform,false);rim.transform.localPosition=new Vector3(0,2,-3);rim.type=LightType.Point;rim.range=10;rim.intensity=3;rim.color=new Color(1,.6f,.3f);
   int[] picks={0,1,3,LanClass.Roles+3,LanClass.Roles*2,LanClass.Roles+4};
   for(int i=0;i<picks.Length;i++){
    bool dea=i<3;var model=Instantiate(assets.characters[picks[i]],studio.transform).transform;
    model.localPosition=new Vector3(dea?-1.05f+i*.42f:.21f+(i-3)*.42f,0,dea?(i==1?.15f:0):(i==4?.15f:0));model.localRotation=Quaternion.Euler(0,dea?120:240,0);
    actors.Add(model);scales.Add(model.localScale);
   }
   var eye=new GameObject("Camara").AddComponent<Camera>();eye.transform.SetParent(studio.transform,false);eye.transform.localPosition=new Vector3(0,.55f,3);eye.transform.LookAt(studio.transform.position+Vector3.up*.3f);
   eye.orthographic=true;eye.orthographicSize=.5f;eye.clearFlags=CameraClearFlags.SolidColor;eye.backgroundColor=new Color(0,0,0,0);eye.farClipPlane=8;
   showcase=new RenderTexture(900,360,16){antiAliasing=4};eye.targetTexture=showcase;
   var shown=new GameObject("Vitrina",typeof(RectTransform),typeof(RawImage)).GetComponent<RawImage>();shown.texture=showcase;shown.raycastTarget=false;
   shown.rectTransform.SetParent(transform,false);shown.rectTransform.anchorMin=shown.rectTransform.anchorMax=new Vector2(.74f,.24f);shown.rectTransform.sizeDelta=new Vector2(480,192);
   var versus=new GameObject("VS",typeof(RectTransform),typeof(Text)).GetComponent<Text>();versus.rectTransform.SetParent(shown.rectTransform,false);versus.rectTransform.sizeDelta=new Vector2(120,60);versus.rectTransform.anchoredPosition=new Vector2(0,112);
   versus.text="VS";versus.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");versus.fontSize=34;versus.fontStyle=FontStyle.Bold;versus.alignment=TextAnchor.MiddleCenter;versus.color=new Color(1,.72f,.25f);versus.raycastTarget=false;
  }
  void Update(){
   float t=Time.unscaledTime;
   if(view){float zoom=.9f+.04f*Mathf.Sin(t*.07f);view.uvRect=new Rect((1-zoom)*(.5f+.45f*Mathf.Sin(t*.05f)),(1-zoom)*(.5f+.45f*Mathf.Cos(t*.04f)),zoom,zoom);}
   if(glow)glow.effectColor=new Color(1,.55f,.1f,.35f+.25f*Mathf.Sin(t*2));
   var size=canvas.rect.size;
   for(int i=0;i<flies.Count;i++){
    var s=seeds[i];var fly=flies[i];
    float x=Mathf.PerlinNoise(s.x,t*.05f),y=Mathf.PerlinNoise(s.y,t*.04f);
    fly.anchoredPosition=new Vector2((x-.5f)*size.x*1.2f,(y-.5f)*size.y*1.2f);
    var image=fly.GetComponent<Image>();var color=image.color;color.a=Mathf.Clamp01(.15f+.85f*Mathf.PerlinNoise(s.x+t*.6f,s.y));image.color=color;
   }
   for(int i=0;i<actors.Count;i++){float hop=Mathf.Abs(Mathf.Sin(t*2.6f+i*.9f));actors[i].localScale=Vector3.Scale(scales[i],new Vector3(1+hop*.03f,1-hop*.05f+.05f,1+hop*.03f));}
  }
  void OnDestroy(){if(studio)Destroy(studio);if(showcase)showcase.Release();}
 }
}
