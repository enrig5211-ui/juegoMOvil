using System.Linq;
using UnityEngine;
namespace Valle.Lan {
 // Luz de cada cliente: atardecer en el despliegue, noche cerrada y amanecer al final.
 // Los colores de día son los de la escena; los de noche, estos.
 public sealed class LanNightCycle:MonoBehaviour {
  static readonly Color MoonColor=new Color(.5f,.6f,1),NightBack=new Color(.01f,.015f,.03f),NightSky=new Color(.07f,.09f,.17f),NightEquator=new Color(.04f,.055f,.09f),NightGround=new Color(.02f,.025f,.035f);
  const float MoonIntensity=.22f,PostIntensity=4;
  LanSession session;LanWorld world;Light sun;Camera eye;Light[] posts;
  Color daySun,dayBack,daySky,dayEquator,dayGround;float dayIntensity;
  public float Level{get;private set;}
  public void Initialize(LanSession owner,LanWorld map){
   session=owner;world=map;
   sun=FindObjectsByType<Light>().FirstOrDefault(l=>l.type==LightType.Directional);
   if(sun){daySun=sun.color;dayIntensity=sun.intensity;}
   eye=Camera.main;dayBack=eye?eye.backgroundColor:Color.black;
   daySky=RenderSettings.ambientSkyColor;dayEquator=RenderSettings.ambientEquatorColor;dayGround=RenderSettings.ambientGroundColor;
   posts=(map.lamps??new Transform[0]).Where(t=>t).Select(t=>t.GetComponentInChildren<Light>()).Where(l=>l).ToArray();
   Apply();
  }
  void Update(){if(session)Apply();}
  void Apply(){
   float level=Mathf.SmoothStep(0,1,world.Tactical?1:LanVision.Daylight(session));Level=level;
   if(sun){sun.color=Color.Lerp(MoonColor,daySun,level);sun.intensity=Mathf.Lerp(MoonIntensity,dayIntensity,level);}
   if(eye)eye.backgroundColor=Color.Lerp(NightBack,dayBack,level);
   RenderSettings.ambientSkyColor=Color.Lerp(NightSky,daySky,level);
   RenderSettings.ambientEquatorColor=Color.Lerp(NightEquator,dayEquator,level);
   RenderSettings.ambientGroundColor=Color.Lerp(NightGround,dayGround,level);
   foreach(var post in posts)post.intensity=PostIntensity*(1-level);
  }
 }
}
