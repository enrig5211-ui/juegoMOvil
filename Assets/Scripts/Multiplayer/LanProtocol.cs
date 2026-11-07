using System;
using UnityEngine;
namespace Valle.Lan {
 [Serializable] public class LanInput {
  public float x,z,ax,az; public bool sprint,fire,hold; public int jump,reload,pickup,drop,lamp,use;
  public bool Valid()=>Finite(x)&&Finite(z)&&Finite(ax)&&Finite(az);
  static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
 }
 [Serializable] public class LanSlot {
  public int id,team,role,kills,deaths,point;public string name;public bool bot=true,ready,loaded;
 }
 [Serializable] public class LanActorState {
  public int id,weapon,ammo,reserve,shot,item;public Vector3 position,aim,hit;public float hp,stamina,reload,shield,down,vision;public bool grounded,impact,lamp;
 }
 [Serializable] public class LanDrop {
  public int id,weapon,ammo,reserve,item;public Vector3 position;
 }
 // Bengala (ilumina un tiempo) o alarma (avisa a su equipo cuando pasa un rival).
 [Serializable] public class LanMark {
  public int id,kind,team;public Vector3 position;public float time;public bool fired;
 }
 [Serializable] public class LanPacket {
  public const int Version=3;
  public string type,name,error,phase;public int version=Version,slot,team,role,blue,red,point,leader=-1,deaLeft,orgLeft,outcome;public float remaining,deploy,wave,capture;public bool hasDrops;
  public LanInput input;public LanSlot[] slots;public LanActorState[] actors;public LanDrop[] drops;public LanMark[] marks;
 }
 public readonly struct LanClass {
  public readonly string Name,Description;public readonly float Health,Stamina,Defense,Speed;public readonly int Reserve,Magazine;
  public LanClass(string n,string d,float hp,float sp,float def,float speed,int reserve,int mag){Name=n;Description=d;Health=hp;Stamina=sp;Defense=def;Speed=speed;Reserve=reserve;Magazine=mag;}
  public static readonly LanClass[] All={
   new LanClass("Explorador","Rápido · 90 vida · 10 defensa · 90 balas",90,130,10,4.2f,90,24),
   new LanClass("Guardián","Resistente · 160 vida · 40 defensa · 90 balas",160,85,40,2.8f,90,24),
   new LanClass("Artillero","Munición extra · 110 vida · 15 defensa · 210 balas",110,100,15,3.3f,210,40)
  };
  public static readonly LanClass Leader=new LanClass("Líder","Objetivo · 120 vida · 20 defensa · pistola",120,100,20,3.3f,60,12);
  public static LanClass Get(int id)=>All[Mathf.Clamp(id,0,2)];
 }
}
