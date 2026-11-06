using UnityEngine;
namespace Valle {
 public sealed class PlayerMagic:MonoBehaviour {
  PlayerStats stats;bool alive;
  void Awake(){stats=GetComponent<PlayerStats>();}
  void Update(){bool now=stats&&!stats.IsDead;if(now&&!alive)MagicFx.Spawn(transform.position,0);alive=now;}
 }
}
