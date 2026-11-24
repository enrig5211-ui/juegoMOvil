using UnityEngine;
namespace Valle.Lan {
 public sealed class LanAssets:ScriptableObject {
  public GameObject bluePawn,redPawn,pistol,rifle;
  public Material blueMaterial,redMaterial,glow;
  public AudioClip[] pistolShots,rifleShots,steps;
  public AudioClip reload,equip;
  public Font font;public Sprite button;
  // Personajes: DEA 0-7, organización 8-15, líder 16 y lancero 17. Los crea Tools > Multijugador > Crear personajes.
  public GameObject[] characters;
  // Efectos y sonidos propios (Tools > Interfaz > Aplicar estilo Noche en el Chapare).
  public Material fxAlpha,fxAdditive;
  public AudioClip nightLoop,tensionMusic,helicopter,radio,alarm,flare,cuffs,selectMove,selectConfirm;
  // Explosivos y destrucción: sonidos generados, fuego de PixPlays (opcional) y sacos para la muralla.
  public AudioClip explosion,whistle,scan,build,fireLoop,bounce,crash;
  public GameObject fireBlast,fireArea,sandbag;
  // Modelos low poly (Tools > Multijugador > Crear modelos low poly) y sonidos del blindado y la avioneta.
  public GameObject vehicle,plane,supply,mine,dynamite,drone,chuto,heli,medStation,campfire,droneStation;
  // Camioneta negra del convoy que llama el Sicario (si falta, el convoy usa el chuto).
  public GameObject convoy;
  public AudioClip engine,planeLoop,horn,skid;
  const int LeaderIndex=LanClass.Roles*2;
  public GameObject Character(int team,bool leader,int role)=>characters!=null&&characters.Length>LeaderIndex?characters[leader?LeaderIndex:Mathf.Clamp(team,0,1)*LanClass.Roles+Mathf.Clamp(role,0,LanClass.Roles-1)]:team==0?bluePawn:redPawn;
  public GameObject Spearman=>characters!=null&&characters.Length>LeaderIndex+1?characters[LeaderIndex+1]:redPawn;
 }
}
