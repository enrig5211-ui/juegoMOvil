using UnityEngine;
using UnityEngine.InputSystem;
namespace Valle {
 [RequireComponent(typeof(CharacterController),typeof(PlayerStats))]
 public sealed class PlayerMotor : MonoBehaviour {
  public Transform visual; public Camera viewCamera;
  public float walkSpeed=3.2f,runSpeed=5.5f,jumpHeight=1.2f,gravity=-22,sprintCost=18,jumpCost=12;
  public float acceleration=20,turnSpeed=14;
  CharacterController controller; PlayerStats stats; Vector3 velocity,spawn,visualPosition,visualScale;
  float vertical,phase,landing,groundTime=-10,jumpBuffer=-10,launchAt=-1;bool exhausted,wasGrounded;
  public event System.Action FootContact, Landed;
  public bool Grounded=>controller && controller.isGrounded;
  public float Speed=>new Vector2(controller.velocity.x,controller.velocity.z).magnitude;
  void Awake(){if(!GetComponent<PlayerMagic>())gameObject.AddComponent<PlayerMagic>();controller=GetComponent<CharacterController>();stats=GetComponent<PlayerStats>();spawn=transform.position;if(visual){visualPosition=visual.localPosition;visualScale=visual.localScale;}}
  void Update(){if(ValleInterface.BlocksInput)return;float dt=Time.deltaTime;if(dt<=0)return;var kb=Keyboard.current;
   if(transform.position.y < -8 || (stats.IsDead&&kb!=null&&kb.rKey.wasPressedThisFrame)){Respawn();return;}
   Vector2 input=Vector2.zero;if(kb!=null&&!stats.IsDead){input=new Vector2((kb.dKey.isPressed||kb.rightArrowKey.isPressed?1:0)-(kb.aKey.isPressed||kb.leftArrowKey.isPressed?1:0),(kb.wKey.isPressed||kb.upArrowKey.isPressed?1:0)-(kb.sKey.isPressed||kb.downArrowKey.isPressed?1:0));if(kb.spaceKey.wasPressedThisFrame)jumpBuffer=Time.time;}
   input=Vector2.ClampMagnitude(input,1);bool grounded=controller.isGrounded;if(grounded){groundTime=Time.time;if(vertical<0)vertical=-2;}
   if(exhausted&&stats.Stamina>=15)exhausted=false;
   bool running=kb!=null&&kb.leftShiftKey.isPressed&&input.sqrMagnitude>.01f&&!exhausted&&!stats.IsDead&&stats.Stamina>sprintCost*dt;
   var forward=viewCamera?Vector3.ProjectOnPlane(viewCamera.transform.forward,Vector3.up).normalized:Vector3.forward;var right=Vector3.Cross(Vector3.up,forward);
   Vector3 wish=(forward*input.y+right*input.x)*(running?runSpeed:walkSpeed);velocity=Vector3.MoveTowards(velocity,wish,acceleration*dt);
   if(!stats.IsDead&&launchAt<0&&Time.time-jumpBuffer<.15f&&Time.time-groundTime<.1f&&stats.Spend(jumpCost)){launchAt=Time.time+.08f;jumpBuffer=-10;}
   if(launchAt>=0&&Time.time>=launchAt){vertical=Mathf.Sqrt(-2*gravity*jumpHeight);launchAt=-1;groundTime=-10;}
   if(stats.IsDead){velocity=Vector3.zero;launchAt=-1;}
   vertical+=gravity*dt;var flags=controller.Move((velocity+Vector3.up*vertical)*dt);if((flags&CollisionFlags.Above)!=0&&vertical>0)vertical=0;
   if(running&&Speed>.1f&&!stats.Spend(sprintCost*dt))exhausted=true;if(stats.Stamina<1)exhausted=true;
   if(controller.isGrounded&&!wasGrounded){landing=.22f;if(vertical < -3) Landed?.Invoke();}wasGrounded=controller.isGrounded;
   if(visual){if(velocity.sqrMagnitude>.04f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(velocity),1-Mathf.Exp(-turnSpeed*dt));Animate(dt);}
  }
  void Animate(float dt){float speed=Speed;int oldContact=Mathf.FloorToInt(phase/Mathf.PI);phase+=dt*(speed>walkSpeed+.2f?18:12);
   if(controller.isGrounded&&speed>.1f&&launchAt<0&&landing<=0&&!stats.IsDead&&Mathf.FloorToInt(phase/Mathf.PI)!=oldContact)FootContact?.Invoke();landing=Mathf.Max(0,landing-dt);
   float squash=1,bob=0,roll=0,pitch=0;
   if(stats.IsDead){pitch=78;squash=.7f;}
   else if(launchAt>=0)squash=.72f;
   else if(!controller.isGrounded){squash=vertical>0?1.2f:1.08f;pitch=vertical>0?-8:9;}
   else if(landing>0)squash=1-.23f*Mathf.Sin(landing/.22f*Mathf.PI);
   else if(speed>.1f){float blend=Mathf.Clamp01(speed/walkSpeed);bob=Mathf.Abs(Mathf.Sin(phase))*.08f*blend;roll=Mathf.Sin(phase)*9*blend;pitch=6*blend;squash=1+Mathf.Sin(phase*2)*.045f*blend;}
   else squash=1+Mathf.Sin(Time.time*2.8f)*.015f;
   var scale=new Vector3(1/Mathf.Sqrt(squash),squash,1/Mathf.Sqrt(squash));visual.localScale=Vector3.Scale(visualScale,scale);visual.localPosition=visualPosition+Vector3.up*bob;visual.localRotation=Quaternion.Euler(pitch,0,roll);
  }
  public void Respawn(){controller.enabled=false;transform.position=spawn;controller.enabled=true;vertical=0;velocity=Vector3.zero;launchAt=-1;jumpBuffer=-10;groundTime=-10;stats.Restore();}
 }
}
