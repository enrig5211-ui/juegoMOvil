using UnityEngine;
namespace Valle.Lan {
 // Estudio fuera de la vista: cada personaje tiene su cámara y se dibuja en alta resolución, suavizado.
 public sealed class LanPortraits:MonoBehaviour {
  const int Width=256,Height=300,Count=LanClass.Roles;
  readonly RenderTexture[] textures=new RenderTexture[Count];readonly Transform[] models=new Transform[Count];readonly Vector3[] scales=new Vector3[Count];
  readonly bool[] hovered=new bool[Count],confirmed=new bool[Count];readonly float[] confirmedAt=new float[Count];
  public int Team{get;private set;}public bool Leader{get;private set;}
  public void Build(LanAssets art,int team,bool leader){
   Team=team;Leader=leader;transform.position=new Vector3(0,-400,0);
   var light=new GameObject("Luz").AddComponent<Light>();light.transform.SetParent(transform,false);light.transform.localPosition=new Vector3(4,3,5);light.type=LightType.Point;light.range=25;light.intensity=5;light.shadows=LightShadows.None;
   int count=leader?1:Count;
   for(int i=0;i<count;i++){
    var slot=new GameObject("Retrato "+i).transform;slot.SetParent(transform,false);slot.localPosition=new Vector3(i*4,0,0);
    models[i]=Instantiate(art.Character(team,leader,i),slot).transform;scales[i]=models[i].localScale;
    var camera=new GameObject("Cámara").AddComponent<Camera>();camera.transform.SetParent(slot,false);camera.transform.localPosition=new Vector3(0,.42f,2.4f);camera.transform.LookAt(slot.position+Vector3.up*.3f);
    camera.orthographic=true;camera.orthographicSize=.42f;camera.nearClipPlane=.1f;camera.farClipPlane=6;camera.clearFlags=CameraClearFlags.SolidColor;
    camera.backgroundColor=team==LanNightRules.Dea?new Color(.08f,.13f,.28f):new Color(.28f,.07f,.07f);
    textures[i]=new RenderTexture(Width,Height,16){filterMode=FilterMode.Bilinear,antiAliasing=4,name="Retrato "+i};camera.targetTexture=textures[i];
   }
  }
  public Texture Texture(int index)=>textures[Mathf.Clamp(index,0,Count-1)];
  public void SetState(int index,bool hover,bool locked){
   if(locked&&!confirmed[index])confirmedAt[index]=Time.unscaledTime;
   hovered[index]=hover;confirmed[index]=locked;
  }
  // Reposo respirando, salto al estar señalado y pose con giro al confirmar.
  void Update(){
   float t=Time.unscaledTime;
   for(int i=0;i<Count;i++){
    var model=models[i];if(!model)continue;
    float since=t-confirmedAt[i];Vector3 offset=Vector3.zero,squash=Vector3.one;float yaw=Mathf.Sin(t*.6f+i)*115;
    if(confirmed[i]&&since<.7f){float k=since/.7f;offset.y=Mathf.Sin(k*Mathf.PI)*.25f;yaw=k*360;squash=new Vector3(1,1+Mathf.Sin(k*Mathf.PI)*.15f,1);}
    else if(confirmed[i]){float bounce=Mathf.Abs(Mathf.Sin(t*4));offset.y=bounce*.04f;squash=new Vector3(1+bounce*.03f,1-bounce*.03f,1+bounce*.03f);yaw=0;}
    else if(hovered[i]){float hop=Mathf.Abs(Mathf.Sin(t*6));offset.y=hop*.08f;squash=new Vector3(1-hop*.05f,1+hop*.1f,1-hop*.05f);}
    else{float breath=Mathf.Sin(t*2.2f+i);squash=new Vector3(1,1+breath*.03f,1);}
    model.localPosition=offset;model.localRotation=Quaternion.Euler(0,yaw,0);model.localScale=Vector3.Scale(scales[i],squash);
   }
  }
  void OnDestroy(){foreach(var texture in textures)if(texture)texture.Release();}
 }
}
