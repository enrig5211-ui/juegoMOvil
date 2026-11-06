using UnityEngine;
namespace Valle.Lan {
 public sealed class LanAssets:ScriptableObject {
  public GameObject bluePawn,redPawn,pistol,rifle;
  public Material blueMaterial,redMaterial,glow;
  public AudioClip[] pistolShots,rifleShots,steps;
  public AudioClip reload,equip;
  public Font font;public Sprite button;
 }
}
