using System;
using UnityEngine;
namespace Valle.Lan {
 [Serializable] public class LanInput {
  public float x,z,ax,az,reach; public bool sprint,fire,hold; public int jump,reload,pickup,drop,lamp,use,ability,grenade,vehicle,equip;
  public bool Valid()=>Finite(x)&&Finite(z)&&Finite(ax)&&Finite(az)&&Finite(reach);
  // Solo los contadores de teclas pulsadas: así una tecla no se repite al perder entradas.
  public LanInput Counters()=>new LanInput{jump=jump,reload=reload,pickup=pickup,drop=drop,lamp=lamp,use=use,ability=ability,grenade=grenade,vehicle=vehicle,equip=equip};
  static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
 }
 [Serializable] public class LanSlot {
  public int id,team,role,kills,deaths,point;public string name;public bool bot=true,ready,loaded,locked;
 }
 [Serializable] public class LanActorState {
  public int id,weapon,ammo,reserve,shot,item,grenades,equip,ride;public Vector3 position,aim,hit;public float hp,stamina,reload,shield,down,vision,ability,active,marked,stun,boost;public bool grounded,impact,lamp;
 }
 [Serializable] public class LanDrop {
  public int id,weapon,ammo,reserve,item;public Vector3 position;
 }
 // Marcas del mapa: bengalas, alarmas, trampas, granadas, explosiones, fuego, bombardeos, murallas y rastreos.
 // from es el origen (granada) o la orientación (muralla); size es el radio; hp, la vida o las cargas que quedan.
 [Serializable] public class LanMark {
  public int id,kind,team,owner=-1,shots;public Vector3 position,from,aim,focus;public float time,size,hp,age;public bool fired;
 }
 // Aliado NPC: lancero (0), comunero (1) y kamikaze (2) de la organización; soldado del helicóptero (3),
 // pistolero de los chutos (4) y dron kamikaze del enjambre (5).
 [Serializable] public class LanNpc {
  public int id,attack,kind,owner,team=1,grenades;public Vector3 position,aim,hit,home;public float hp,time;
 }
 // Vehículo blindado de una plaza: el conductor también maneja la ametralladora (-1 si está libre).
 [Serializable] public class LanVehicleState {
  // crew: NPC a bordo; ai: lo maneja la máquina (convoy del Sicario); temp: llamado con una habilidad, no reaparece.
  public int id,kind,team,driver=-1,shot,crew;public Vector3 position,aim,hit;public float yaw,hp,speed,respawn;public bool impact,ai,temp;
 }
 [Serializable] public class LanPacket {
  public const int Version=12;
  public string type,name,error,phase;public int version=Version,slot,team,role,blue,red,point,leader=-1,deaLeft,orgLeft,outcome;public float remaining,deploy,wave,capture;public bool hasDrops,hasBroken;
  public LanInput input;public LanSlot[] slots;public LanActorState[] actors;public LanDrop[] drops;public LanMark[] marks;public LanNpc[] npcs;public LanVehicleState[] vehicles;public int[] broken;
 }
 public readonly struct LanClass {
  public readonly string Name,Description;public readonly float Health,Stamina,Defense,Speed;public readonly int Reserve,Magazine;
  public LanClass(string n,string d,float hp,float sp,float def,float speed,int reserve,int mag){Name=n;Description=d;Health=hp;Stamina=sp;Defense=def;Speed=speed;Reserve=reserve;Magazine=mag;}
  public static readonly LanClass[] All={
   new LanClass("Explorador","Rápido · 90 vida · 10 defensa · 90 balas",90,130,10,4.2f,90,24),
   new LanClass("Guardián","Resistente · 160 vida · 40 defensa · 90 balas",160,85,40,2.8f,90,24),
   new LanClass("Artillero","Munición extra · 110 vida · 15 defensa · 210 balas",110,100,15,3.3f,210,40)
  };
  public static readonly LanClass Leader=new LanClass("Líder","Lanceros y escoltas armados · 150 vida · 20 defensa · pistola",150,100,20,3.3f,60,12);
  public static LanClass Get(int id)=>All[Mathf.Clamp(id,0,2)];
  // Mismas estadísticas que las tres clases, con nombre y habilidad propios de cada bando.
  // Ocho personajes por bando: nombre, habilidad, vida, estamina, defensa, velocidad, reserva y cargador.
  public const int Roles=8;
  static readonly LanClass[,] byTeam={
   {Make("Asaltante","Suministro aéreo en paracaídas",90,130,10,4.2f,90,24),Make("Pesado","Escudo y cúpula para el equipo",160,85,40,2.8f,90,24),Make("Rastreador","Rastreo: ubica al líder en todo el mapa",110,100,15,3.3f,210,40),
    Make("Paramédico","Estación médica: cura en un área 40 s",100,110,15,3.6f,90,24),Make("Granadero","Andanada: granadas y aturdidoras 10 s",120,95,25,3.1f,120,24),Make("Explorador nocturno","Bombardeo: marca y llama un ataque",90,125,10,3.9f,90,24),
    Make("Francotirador","Helicóptero de apoyo · pasiva: alcance y daño",90,100,10,3.4f,60,10),Make("Operador de dron","Enjambre: base de 40 drones kamikaze",100,110,15,3.6f,90,24)},
   {Make("Pistolero","Ráfaga: dispara el doble 6 s",90,130,10,4.2f,90,24),Make("Guardaespaldas","Muralla y escolta al líder",160,85,40,2.8f,90,24),Make("Reclutador","Reclutar: 6 comuneros y kamikazes",110,100,15,3.3f,210,40),
    Make("Cocalero","Frenesí: doble daño y hojas de coca",110,120,15,3.8f,90,24),Make("Curandera","Fogata: cura en un área",100,110,10,3.6f,90,24),Make("Trampero","Mina: explota y marca",100,120,10,3.9f,90,24),
    Make("Sicario","Chutos: convoy armado de apoyo",100,120,10,4f,90,24),Make("Contrabandista","Bomba de humo: tapa la vista 10 s",110,110,20,3.6f,120,24)}};
  static LanClass Make(string name,string ability,float hp,float stamina,float defense,float speed,int reserve,int magazine)=>new LanClass(name,$"{ability} · {hp:0} vida · {defense:0} defensa · {reserve} balas",hp,stamina,defense,speed,reserve,magazine);
  public static LanClass For(int team,int role)=>byTeam[Mathf.Clamp(team,0,1),Mathf.Clamp(role,0,Roles-1)];
 }
}
