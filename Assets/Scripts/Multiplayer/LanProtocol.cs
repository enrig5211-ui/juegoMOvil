using System;
using UnityEngine;
namespace Valle.Lan {
 [Serializable] public class LanInput {
  public float x,z,ax,az; public bool sprint,fire,hold; public int jump,reload,pickup,drop,lamp,use,ability;
  public bool Valid()=>Finite(x)&&Finite(z)&&Finite(ax)&&Finite(az);
  static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
 }
 [Serializable] public class LanSlot {
  public int id,team,role,kills,deaths,point;public string name;public bool bot=true,ready,loaded,locked;
 }
 [Serializable] public class LanActorState {
  public int id,weapon,ammo,reserve,shot,item;public Vector3 position,aim,hit;public float hp,stamina,reload,shield,down,vision,ability,active,marked,stun;public bool grounded,impact,lamp;
 }
 [Serializable] public class LanDrop {
  public int id,weapon,ammo,reserve,item;public Vector3 position;
 }
 // Bengala (ilumina un tiempo) o alarma (avisa a su equipo cuando pasa un rival).
 [Serializable] public class LanMark {
  public int id,kind,team;public Vector3 position;public float time;public bool fired;
 }
 // Lancero invocado por el líder.
 [Serializable] public class LanNpc {
  public int id,attack;public Vector3 position,aim;public float hp,time;
 }
 [Serializable] public class LanPacket {
  public const int Version=6;
  public string type,name,error,phase;public int version=Version,slot,team,role,blue,red,point,leader=-1,deaLeft,orgLeft,outcome;public float remaining,deploy,wave,capture;public bool hasDrops;
  public LanInput input;public LanSlot[] slots;public LanActorState[] actors;public LanDrop[] drops;public LanMark[] marks;public LanNpc[] npcs;
 }
 public readonly struct LanClass {
  public readonly string Name,Description;public readonly float Health,Stamina,Defense,Speed;public readonly int Reserve,Magazine;
  public LanClass(string n,string d,float hp,float sp,float def,float speed,int reserve,int mag){Name=n;Description=d;Health=hp;Stamina=sp;Defense=def;Speed=speed;Reserve=reserve;Magazine=mag;}
  public static readonly LanClass[] All={
   new LanClass("Explorador","Rápido · 90 vida · 10 defensa · 90 balas",90,130,10,4.2f,90,24),
   new LanClass("Guardián","Resistente · 160 vida · 40 defensa · 90 balas",160,85,40,2.8f,90,24),
   new LanClass("Artillero","Munición extra · 110 vida · 15 defensa · 210 balas",110,100,15,3.3f,210,40)
  };
  public static readonly LanClass Leader=new LanClass("Líder","Lanceros: invoca 5 defensores 25 s · 120 vida · 20 defensa · pistola",120,100,20,3.3f,60,12);
  public static LanClass Get(int id)=>All[Mathf.Clamp(id,0,2)];
  // Mismas estadísticas que las tres clases, con nombre y habilidad propios de cada bando.
  // Seis personajes por bando: nombre, habilidad, vida, estamina, defensa, velocidad, reserva y cargador.
  public const int Roles=6;
  static readonly LanClass[,] byTeam={
   {Make("Asaltante","Avance: carrera corta",90,130,10,4.2f,90,24),Make("Pesado","Escudo frontal 4 s",160,85,40,2.8f,90,24),Make("Rastreador","Pulso: marca rivales cercanos",110,100,15,3.3f,210,40),
    Make("Paramédico","Curar: +40 a los aliados cercanos",100,110,15,3.6f,90,24),Make("Granadero","Aturdidora: inmoviliza 2 s",120,95,25,3.1f,120,24),Make("Explorador nocturno","Lanza una bengala",90,125,10,3.9f,90,24)},
   {Make("Pistolero","Ráfaga: dispara el doble 3 s",90,130,10,4.2f,90,24),Make("Guardaespaldas","Proteger al líder cercano 5 s",160,85,40,2.8f,90,24),Make("Vigía","Coloca una alarma",110,100,15,3.3f,210,40),
    Make("Cocalero","Emboscada: invisible 5 s",110,120,15,3.8f,90,24),Make("Curandera","Curar: +40 a los aliados cercanos",100,110,10,3.6f,90,24),Make("Trampero","Trampa que aturde y marca",100,120,10,3.9f,90,24)}};
  static LanClass Make(string name,string ability,float hp,float stamina,float defense,float speed,int reserve,int magazine)=>new LanClass(name,$"{ability} · {hp:0} vida · {defense:0} defensa · {reserve} balas",hp,stamina,defense,speed,reserve,magazine);
  public static LanClass For(int team,int role)=>byTeam[Mathf.Clamp(team,0,1),Mathf.Clamp(role,0,Roles-1)];
 }
}
