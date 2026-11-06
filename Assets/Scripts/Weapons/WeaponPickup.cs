using UnityEngine;
using System.Collections.Generic;
namespace Valle {
 public sealed class WeaponPickup:MonoBehaviour {
  public static readonly HashSet<WeaponPickup> Available=new HashSet<WeaponPickup>();
  public string displayName="Pistola";public bool automatic;
  public int magazineSize=12,ammo=12,reserve=60;public float damage=20,shotInterval=.25f,reloadSeconds=1.4f,range=35;
  public Transform model,muzzle;public GameObject marker;public AudioClip[] shots;public AudioClip equipSound,reloadSound;
  public PlayerWeapon Owner {get;private set;} Vector3 modelRest;float phase;
  void Awake(){modelRest=model.localPosition;phase=Random.value*6;}
  void OnEnable(){if(!Owner)Available.Add(this);}
  void OnDisable(){Available.Remove(this);}
  void Update(){if(Owner)return;model.localPosition=modelRest+Vector3.up*(.5f+Mathf.Sin(Time.time*2+phase)*.08f);model.Rotate(0,50*Time.deltaTime,0,Space.Self);}
  public void Attach(PlayerWeapon owner,Transform socket){Owner=owner;Available.Remove(this);transform.SetParent(socket,false);transform.localPosition=Vector3.zero;transform.localRotation=Quaternion.identity;transform.localScale=Vector3.one*.65f;model.localPosition=modelRest;model.localRotation=Quaternion.identity;if(marker)marker.SetActive(false);}
  public void Release(Vector3 ground){Owner=null;transform.SetParent(null);transform.position=ground;transform.rotation=Quaternion.identity;transform.localScale=Vector3.one;if(marker)marker.SetActive(true);Available.Add(this);}
 }
}
