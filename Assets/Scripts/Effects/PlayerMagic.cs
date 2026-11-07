using UnityEngine;
namespace Valle {
 public sealed class PlayerMagic:MonoBehaviour {
  PlayerStats stats;bool alive;
  void Awake(){stats=GetComponent<PlayerStats>();}
  void Update(){bool now=stats&&!stats.IsDead;if(now&&!alive)Valle.Lan.LanFx.Dust(transform.position,14);alive=now;}
 }
}
