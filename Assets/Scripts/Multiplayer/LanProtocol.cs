using System;
using UnityEngine;
namespace Valle.Lan {
 [Serializable] public class LanInput {
  public float x,z,ax,az; public bool sprint,fire; public int jump,reload,pickup,drop;
  public bool Valid()=>Finite(x)&&Finite(z)&&Finite(ax)&&Finite(az);
  static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
 }
 [Serializable] public class LanSlot {
  public int id,team,role,kills,deaths;public string name;public bool bot=true,ready,loaded;
 }
 [Serializable] public class LanActorState {
  public int id,weapon,ammo,reserve,shot;public Vector3 position,aim,hit;public float hp,stamina,respawn,reload,shield;public bool grounded,impact;
 }
 [Serializable] public class LanDrop {
  public int id,weapon,ammo,reserve;public Vector3 position;
 }
 [Serializable] public class LanPacket {
  public string type,name,error,phase;public int version=1,slot,team,role,blue,red;public float remaining;
  public LanInput input;public LanSlot[] slots;public LanActorState[] actors;public LanDrop[] drops;
 }
 public readonly struct LanClass {
  public readonly string Name,Description;public readonly float Health,Stamina,Defense,Speed;public readonly int Reserve,Magazine;
  public LanClass(string n,string d,float hp,float sp,float def,float speed,int reserve,int mag){Name=n;Description=d;Health=hp;Stamina=sp;Defense=def;Speed=speed;Reserve=reserve;Magazine=mag;}
  public static readonly LanClass[] All={
   new LanClass("Explorador","Rápido · 90 vida · 10 defensa · 90 balas",90,130,10,4.2f,90,24),
   new LanClass("Guardián","Resistente · 160 vida · 40 defensa · 90 balas",160,85,40,2.8f,90,24),
   new LanClass("Artillero","Munición extra · 110 vida · 15 defensa · 210 balas",110,100,15,3.3f,210,40)
  };
  public static LanClass Get(int id)=>All[Mathf.Clamp(id,0,2)];
 }
}
