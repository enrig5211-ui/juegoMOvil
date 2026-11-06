using UnityEngine;
namespace Valle {
 public sealed class WeaponTarget:MonoBehaviour {
  public float maxHealth=100;public float Health{get;private set;}float restoreAt;
  void Awake(){Health=maxHealth;}
  public void TakeDamage(float amount){Health=Mathf.Max(0,Health-amount);if(Health==0){restoreAt=Time.time+3;transform.localScale=Vector3.one*.7f;}}
  void Update(){if(restoreAt>0&&Time.time>=restoreAt){restoreAt=0;Health=maxHealth;transform.localScale=Vector3.one;}}
 }
}
