using UnityEngine;
using UnityEngine.InputSystem;
namespace Valle {
 public sealed class PlayerCamera : MonoBehaviour {
  public Transform target; public float pitch=45,yaw=35,distance=10,rotationSpeed=100,followSharpness=12;
  Vector3 focus;bool initialized;
  void LateUpdate(){if(!target)return;var k=Keyboard.current;if(k!=null&&!(Valle.Lan.LanSession.Instance&&Valle.Lan.LanSession.Instance.MenuOpen))yaw+=((k.eKey.isPressed?1:0)-(k.qKey.isPressed?1:0))*rotationSpeed*Time.deltaTime;
   var desired=target.position+Vector3.up*.55f;focus=initialized?Vector3.Lerp(focus,desired,1-Mathf.Exp(-followSharpness*Time.deltaTime)):desired;initialized=true;
   var rotation=Quaternion.Euler(pitch,yaw,0);transform.SetPositionAndRotation(focus-rotation*Vector3.forward*distance,rotation);
  }
 }
}
