using UnityEngine;
namespace Valle.Lan {
 // Iconos dibujados por código para mapas y avisos: sin imágenes externas.
 public static class LanIcons {
  public static Sprite Circle,Ring,Arrow,Diamond,Soft,Chevron,Arc,Sweep,Vignette,Square;
  public static Texture2D Grid;
  delegate float Shape(float x,float y);
  public static void Build(){
   if(Circle)return;
   Circle=Make(64,(x,y)=>Len(x,y)<=1?1:0);
   Ring=Make(64,(x,y)=>{float d=Len(x,y);return d<=1&&d>=.78f?1:0;});
   Arrow=Make(64,(x,y)=>{float top=.95f,bottom=-.75f;if(y>top||y<bottom)return 0;float half=(top-y)/(top-bottom)*.75f;bool notch=y<-.35f&&Mathf.Abs(x)<(-.35f-y)*1.2f;return Mathf.Abs(x)<=half&&!notch?1:0;});
   Diamond=Make(64,(x,y)=>Mathf.Abs(x)+Mathf.Abs(y)<=.95f?1:0);
   Soft=Make(64,(x,y)=>{float d=Len(x,y);return Mathf.Clamp01(1-d)*Mathf.Clamp01(1-d);});
   Chevron=Make(64,(x,y)=>{float v=.55f-Mathf.Abs(x)*.9f;return y<=v&&y>=v-.42f&&Mathf.Abs(x)<.9f?1:0;});
   Arc=Make(128,(x,y)=>{float d=Len(x,y);if(d>1||d<.86f)return 0;float angle=Mathf.Abs(Mathf.Atan2(x,y))*Mathf.Rad2Deg;return Mathf.Clamp01((38-angle)/14);});
   Sweep=Make(128,(x,y)=>{float d=Len(x,y);if(d>1)return 0;float angle=Mathf.Repeat(Mathf.Atan2(x,y)*Mathf.Rad2Deg,360);return angle>300?(angle-300)/60f*.9f:0;});
   Vignette=Make(128,(x,y)=>{float d=Len(x,y)/1.41f;return Mathf.SmoothStep(0,1,(d-.45f)/.55f);});
   Square=Make(8,(x,y)=>1);
   Grid=new Texture2D(32,32,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Bilinear,name="Cuadricula"};
   var pixels=new Color32[32*32];for(int y=0;y<32;y++)for(int x=0;x<32;x++)pixels[y*32+x]=new Color32(255,255,255,(byte)(x==0||y==0?255:0));
   Grid.SetPixels32(pixels);Grid.Apply();
  }
  static float Len(float x,float y)=>Mathf.Sqrt(x*x+y*y);
  // Cada píxel se muestrea 4×4 veces para bordes suaves.
  static Sprite Make(int size,Shape shape){
   var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
   var pixels=new Color32[size*size];
   for(int y=0;y<size;y++)for(int x=0;x<size;x++){
    float sum=0;
    for(int sy=0;sy<4;sy++)for(int sx=0;sx<4;sx++){float u=(x+(sx+.5f)/4)/size*2-1,v=(y+(sy+.5f)/4)/size*2-1;sum+=shape(u,v);}
    pixels[y*size+x]=new Color32(255,255,255,(byte)Mathf.RoundToInt(Mathf.Clamp01(sum/16)*255));
   }
   texture.SetPixels32(pixels);texture.Apply();
   return Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100);
  }
 }
}
