using UnityEngine;
namespace Valle.Lan {
 public sealed class LanAssets:ScriptableObject {
  public GameObject bluePawn,redPawn,pistol,rifle;
  public Material blueMaterial,redMaterial,glow;
  public AudioClip[] pistolShots,rifleShots,steps;
  public AudioClip reload,equip;
  public Font font;public Sprite button;
  // Personajes: DEA 0-2, organización 0-2 y líder (índice 6). Los crea Tools > Multijugador > Crear personajes.
  public GameObject[] characters;
  public GameObject Character(int team,bool leader,int role)=>characters!=null&&characters.Length==7?characters[leader?6:Mathf.Clamp(team,0,1)*3+Mathf.Clamp(role,0,2)]:team==0?bluePawn:redPawn;
 }
}
