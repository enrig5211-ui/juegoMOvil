using UnityEngine;
namespace Valle.Lan {
 public sealed class LanAssets:ScriptableObject {
  public GameObject bluePawn,redPawn,pistol,rifle;
  public Material blueMaterial,redMaterial,glow;
  public AudioClip[] pistolShots,rifleShots,steps;
  public AudioClip reload,equip;
  public Font font;public Sprite button;
  // Personajes: DEA 0-5, organización 6-11, líder 12 y lancero 13. Los crea Tools > Multijugador > Crear personajes.
  public GameObject[] characters;
  // Efectos y sonidos propios (Tools > Interfaz > Aplicar estilo Noche en el Chapare).
  public Material fxAlpha,fxAdditive;
  public AudioClip nightLoop,tensionMusic,helicopter,radio,alarm,flare,cuffs,selectMove,selectConfirm;
  public GameObject Character(int team,bool leader,int role)=>characters!=null&&characters.Length>=13?characters[leader?12:Mathf.Clamp(team,0,1)*LanClass.Roles+Mathf.Clamp(role,0,LanClass.Roles-1)]:team==0?bluePawn:redPawn;
  public GameObject Spearman=>characters!=null&&characters.Length>=14?characters[13]:redPawn;
 }
}
