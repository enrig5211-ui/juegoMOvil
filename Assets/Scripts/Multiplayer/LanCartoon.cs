using UnityEngine;
namespace Valle.Lan {
 // Animación tipo caricatura para personajes sin esqueleto: todo es posición, giro y escala del modelo.
 // Lee el estado de red, así que se ve igual en el anfitrión y en cada cliente.
 public sealed class LanCartoon {
  Transform visual;Vector3 basePosition,baseScale;
  float phase,recoil,hurt,pop,spawn=1,lastHp=-1,lastCooldown,diedAt=-99;int lastShot;bool lastAlive;
  public bool Dying=>Time.time-diedAt<DeathSeconds;
  public const float DeathSeconds=1.3f;
  public void Bind(Transform model){visual=model;basePosition=model.localPosition;baseScale=model.localScale;}
  public void Pose(LanPawn pawn,float speed,bool celebrate,bool slump){
   if(!visual)return;
   var state=pawn.State;float dt=Time.deltaTime,t=Time.time+pawn.Id*.37f;bool alive=state.hp>0;
   // Eventos: disparo, golpe, aparición, muerte y habilidad.
   if(state.shot!=lastShot){lastShot=state.shot;recoil=1;}
   if(lastHp>=0&&alive&&state.hp<lastHp-.5f)hurt=1;
   if(lastAlive&&!alive&&state.down<=0)diedAt=Time.time;
   if(alive&&!lastAlive)spawn=0;
   if(state.ability>lastCooldown+.5f)pop=1;
   lastHp=state.hp;lastAlive=alive;lastCooldown=state.ability;
   recoil=Mathf.MoveTowards(recoil,0,dt/.14f);hurt=Mathf.MoveTowards(hurt,0,dt/.25f);pop=Mathf.MoveTowards(pop,0,dt/.35f);spawn=Mathf.MoveTowards(spawn,1,dt/.35f);
   var aim=state.aim.sqrMagnitude>.01f?state.aim:Vector3.forward;
   Vector3 offset=Vector3.zero,squash=Vector3.one;float pitch=0,yaw=0,roll=0;
   if(state.down>0){
    // Líder reducido: tumbado y tambaleándose.
    pitch=85;roll=Mathf.Sin(t*5)*8;offset.y=.03f;
   }else if(!alive){
    // Caída exagerada con rebote; luego se encoge y se hunde.
    float since=Time.time-diedAt,fall=Mathf.Clamp01(since/.35f),sink=Mathf.Clamp01((since-.8f)/.45f);
    pitch=BackOut(fall)*90;squash=Vector3.one*(1-sink);offset.y=-sink*.15f;
   }else{
    if(speed>.3f){
     phase+=dt*Mathf.Lerp(11,18,Mathf.InverseLerp(2,6.5f,speed));
     float hop=Mathf.Abs(Mathf.Sin(phase)),stretch=hop*2-1;
     offset.y=hop*.09f;squash=new Vector3(1-stretch*.06f,1+stretch*.1f,1-stretch*.06f);pitch=9;roll=Mathf.Sin(phase)*7;
    }else{
     float breath=Mathf.Sin(t*2.4f);squash=new Vector3(1-breath*.018f,1+breath*.035f,1-breath*.018f);roll=Mathf.Sin(t*1.1f)*1.5f;
    }
    if(!state.grounded)squash=new Vector3(.9f,1.15f,.9f);
    if(LanAbilities.Active(pawn,LanAbilities.Dash)){squash=new Vector3(.85f,.88f,1.4f);pitch=20;}
    if(celebrate){float jump=Mathf.Abs(Mathf.Sin(t*5.5f));offset.y=jump*.3f;yaw=t*300%360;squash=new Vector3(1-jump*.05f,1+jump*.12f,1-jump*.05f);}
    if(slump){pitch=28;squash=new Vector3(1.06f,.86f,1.06f);roll=Mathf.Sin(t*.8f)*4;}
    pitch-=recoil*16;
    squash=Vector3.Scale(squash,new Vector3(1+hurt*.2f,1-hurt*.25f,1+hurt*.2f));offset.x+=Mathf.Sin(t*70)*.025f*hurt;
    squash*=1+Elastic(pop)*.28f;
    squash*=BackOut(spawn);
   }
   visual.localPosition=basePosition+offset;
   visual.localRotation=Quaternion.LookRotation(aim)*Quaternion.Euler(pitch,yaw,roll);
   visual.localScale=Vector3.Scale(baseScale,squash);
  }
  static float BackOut(float k){const float c=1.70158f;k-=1;return 1+k*k*((c+1)*k+c);}
  static float Elastic(float k)=>k<=0?0:Mathf.Sin(k*Mathf.PI*3)*k;
 }
}
