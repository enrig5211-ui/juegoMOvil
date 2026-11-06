using UnityEngine;
namespace Valle {
 public sealed class PlayerStats : MonoBehaviour {
  [Min(1)] public float maxHealth=100,maxStamina=100;
  [Min(0)] public float defense=15,attack=10,staminaRecovery=20,recoveryDelay=.8f;
  public float Health {get;private set;} public float Stamina {get;private set;}
  public event System.Action Died;
  public bool IsDead=>Health<=0; float lastSpend=-100;
  void Awake(){Restore();}
  void Update(){if(!IsDead&&Time.time-lastSpend>=recoveryDelay)Stamina=Mathf.Min(maxStamina,Stamina+staminaRecovery*Time.deltaTime);}
  public bool Spend(float amount){if(IsDead||amount<0||Stamina<amount)return false;Stamina-=amount;lastSpend=Time.time;return true;}
  public void TakeDamage(float rawDamage){if(rawDamage<=0||IsDead)return;Health=Mathf.Max(0,Health-rawDamage*100f/(100f+Mathf.Max(0,defense)));if(IsDead)Died?.Invoke();}
  public void Heal(float amount){if(!IsDead)Health=Mathf.Min(maxHealth,Health+Mathf.Max(0,amount));}
  public void Restore(){Health=maxHealth;Stamina=maxStamina;lastSpend=-100;}
  [ContextMenu("Prueba: recibir 25 de dano")] void TestDamage(){if(Application.isPlaying)TakeDamage(25);}
 }
}
