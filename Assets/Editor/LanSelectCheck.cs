using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Valle.Lan;
// Comprueba la Etapa 4: selección arcade por red, bots que eligen solos y personajes animados en partida.
[InitializeOnLoad]
public static class LanSelectCheck {
 sealed class Guest:IDisposable {
  public LanPeer Peer;public int Slot=-1;public LanPacket Last;bool loaded;double nextSend;
  public Guest(){var socket=new TcpClient();socket.Connect("127.0.0.1",7777);Peer=new LanPeer(socket);Send(new LanPacket{type="hello",name="Invitado",team=1,role=0});}
  public void Send(LanPacket packet)=>Peer.Send(JsonUtility.ToJson(packet));
  public void Tick(){
   while(Peer.Incoming.TryDequeue(out var json)){var p=JsonUtility.FromJson<LanPacket>(json);if(p.type=="welcome")Slot=p.slot;if(p.type!="snapshot")continue;Last=p;if(p.phase=="match"&&!loaded){loaded=true;Send(new LanPacket{type="loaded"});}}
   if(EditorApplication.timeSinceStartup<nextSend)return;nextSend=EditorApplication.timeSinceStartup+.05;Send(new LanPacket{type=loaded?"input":"ping",input=new LanInput()});
  }
  public void Dispose()=>Peer.Dispose();
 }
 static readonly StringBuilder log=new StringBuilder();
 static Guest guest;static int stage,waits;static double next,started;
 static LanSelectCheck(){EditorApplication.update+=Tick;}
 [MenuItem("Tools/Multijugador/Comprobar seleccion y personajes")]
 static void Request(){File.WriteAllText("Temp/lan-select-test.request","");}
 static void Check(bool ok,string text){log.AppendLine((ok?"PASS: ":"FAIL: ")+text);File.WriteAllText("Documentation/LAN-select-check.txt",log.ToString());}
 static void Tick(){
  if(!SessionState.GetBool("LanSelectTest",false)){
   if(!File.Exists("Temp/lan-select-test.request")||EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)return;
   File.Delete("Temp/lan-select-test.request");EditorSceneManager.OpenScene("Assets/Scenes/MenuInicio.unity");SessionState.SetBool("LanSelectTest",true);EditorApplication.isPlaying=true;return;
  }
  if(!EditorApplication.isPlaying)return;
  if(started==0)started=EditorApplication.timeSinceStartup;
  if(EditorApplication.timeSinceStartup-started>120){Check(false,"Select check timed out");Finish();return;}
  try{guest?.Tick();if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.5;Advance(LanSession.Instance);}
  catch(Exception e){Check(false,e.ToString());Finish();}
 }
 static void Advance(LanSession s){
  switch(stage++){
   case 0:LanSession.Open();s=LanSession.Instance;s.PlayerName="Agente";s.Team=0;s.Role=0;s.Host();guest=new Guest();break;
   case 1:if(guest.Slot<0){stage--;return;}guest.Send(new LanPacket{type="ready"});break;
   case 2:s.BeginSelect();break;
   case 3:{
    Check(s.Phase=="select"&&s.Remaining>LanNightRules.SelectSeconds-2,"Starting opens the character selection");
    var leader=s.Night.Leader;Check(leader>=4&&s.Slots[leader].locked,"The leader is decided and needs no choice");
    Check(s.Slots.Where(x=>x.bot).All(x=>x.role>=0&&x.role<LanClass.Roles),"Bots pick a character");
    Check(guest.Last!=null&&guest.Last.phase=="select","Clients enter the selection");
    guest.Send(new LanPacket{type="class",role=2});break;}
   case 4:Check(s.Slots[guest.Slot].role==2,"Clients change character during the selection");guest.Send(new LanPacket{type="lock"});break;
   case 5:Check(s.Slots[guest.Slot].locked,"Clients confirm their character");guest.Send(new LanPacket{type="class",role=0});break;
   case 6:Check(s.Slots[guest.Slot].role==2,"A confirmed character cannot change");s.ChooseClass(1);
    Check(s.Slots[0].role==1&&!s.Slots[0].locked,"The host moves the cursor without confirming");break;
   case 7:if(s.Slots.Where(x=>x.bot).Any(x=>!x.locked)&&++waits<14){stage--;return;}
    Check(s.Slots.Where(x=>x.bot).All(x=>x.locked),"Bots confirm on their own");
    Check(s.Phase=="select","The selection waits for the humans");
    {var studio=UnityEngine.Object.FindAnyObjectByType<LanPortraits>();Check(studio&&Enumerable.Range(0,LanClass.Roles).All(i=>studio.Texture(i)&&studio.Texture(i).filterMode==FilterMode.Bilinear&&studio.Texture(i).width>=256),"Six sharp portraits are drawn");}
    ScreenCapture.CaptureScreenshot("Documentation/Seleccion.png");break;
   case 8:s.LockCharacter();break;
   case 9:{if(!s.World){stage--;return;}
    var w=s.World;
    Check(s.Phase=="match","Confirming everyone starts the night");
    Check(w.Pawns[0].Ability==LanAbilities.Shield&&w.Pawns[guest.Slot].Ability==LanAbilities.Recruit,"The chosen characters reach the match");
    Check(w.Pawns[0].transform.Find("Pesado(Clone)")&&w.Pawns[guest.Slot].transform.Find("Reclutador(Clone)")&&w.LeaderPawn.transform.Find("Lider(Clone)"),"Each pawn shows its character model");
    // Escena para la captura: los siete personajes en fila, quietos y mirando a la cámara.
    s.Night.Deploy=0;s.Remaining=30;s.MenuOpen=true;
    var camera=Quaternion.Euler(45,35,0);var right=camera*Vector3.right;right.y=0;right.Normalize();var toward=-(camera*Vector3.forward);toward.y=0;toward.Normalize();
    var order=w.Pawns.Values.OrderBy(p=>p.Team).ThenBy(p=>p.IsLeader?9:p.Role).ToList();var origin=new Vector3(2,.05f,-12);
    for(int i=0;i<order.Count;i++){var p=order[i];if(p.Id!=guest.Slot&&p.Id!=0){s.Slots[p.Id].bot=false;s.Slots[p.Id].loaded=true;}p.ResetLife(origin+right*(i-3f)*1.05f);p.State.aim=toward;p.State.shield=0;}
    var focus=new GameObject("Foco de captura");focus.transform.position=origin;Camera.main.GetComponent<Valle.PlayerCamera>().target=focus.transform;Camera.main.orthographicSize=2.6f;
    Physics.SyncTransforms();break;}
   case 10:{var w=s.World;var order=w.Pawns.Values.OrderBy(p=>p.Team).ThenBy(p=>p.IsLeader?9:p.Role).ToList();
    Camera.main.orthographicSize=2.6f;foreach(var p in order)p.State.shield=0;order[1].State.shot++;order[2].State.hp-=20;order[4].State.ability=LanAbilities.Cooldown(order[4].Ability);break;}
   case 11:ScreenCapture.CaptureScreenshot("Documentation/Personajes-partida.png");
    // Los personajes de Blender son una sola malla con un material por color: uniforme, piel y equipo de su clase.
    Check(UnityEngine.Object.FindObjectsByType<LanPawn>().All(p=>p.GetComponentsInChildren<MeshRenderer>(true).Any(r=>r.sharedMaterials.Length>=4)),"Characters are drawn with their class outfit");
    Finish();break;
  }
 }
 static void Finish(){guest?.Dispose();guest=null;if(LanSession.Instance)LanSession.Instance.Leave();SessionState.SetBool("LanSelectTest",false);EditorApplication.isPlaying=false;}
}
