using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Valle.Lan;
// Juega noches completas solo con bots, acelerando la simulación, y comprueba cómo terminan.
// La física avanza junto a cada paso: sin eso, disparos y visión usarían posiciones antiguas.
[InitializeOnLoad]
public static class LanBotCheck {
 const int Nights=6,StepsPerUpdate=40;
 static int stage,slot=-1,night,captures,dawns,spent,shots;static double next,nextPing;static LanPeer guest;static bool loaded,escaped;static StringBuilder log=new StringBuilder();
 static LanBotCheck(){EditorApplication.update+=Tick;}
 [MenuItem("Tools/Multijugador/Comprobar noches con bots")]
 static void Request(){File.WriteAllText("Temp/lan-bot-test.request","");}
 static void Check(bool ok,string text){Line((ok?"PASS: ":"FAIL: ")+text);}
 static void Line(string text){log.AppendLine(text);File.WriteAllText("Documentation/LAN-bots-check.txt",log.ToString());}
 static void Tick(){
  if(!SessionState.GetBool("LanBotTest",false)){
   if(!File.Exists("Temp/lan-bot-test.request")||EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)return;
   File.Delete("Temp/lan-bot-test.request");EditorSceneManager.OpenScene("Assets/Scenes/MenuInicio.unity");SessionState.SetBool("LanBotTest",true);EditorApplication.isPlaying=true;return;
  }
  if(!EditorApplication.isPlaying)return;
  try{Guest();Advance(LanSession.Instance);}catch(Exception e){Check(false,e.ToString());Finish();}
 }
 static void Guest(){
  if(guest==null)return;
  while(guest.Incoming.TryDequeue(out var json)){
   var p=JsonUtility.FromJson<LanPacket>(json);
   if(p.type=="welcome")slot=p.slot;
   if(p.type=="snapshot"&&p.phase=="match"&&!loaded){loaded=true;guest.Send(JsonUtility.ToJson(new LanPacket{type="loaded"}));}
  }
  if(EditorApplication.timeSinceStartup<nextPing)return;
  nextPing=EditorApplication.timeSinceStartup+.05;
  guest.Send(JsonUtility.ToJson(new LanPacket{type="ping"}));
 }
 static void Advance(LanSession s){
  switch(stage){
   case 0:LanSession.Open();s=LanSession.Instance;s.Host();s.StartMatch();stage=1;break;
   case 1:if(!s.World)return;
    if(night==0)Check(s.Slots.Count(x=>x.bot)==7,"One human starts with seven bots");
    s.Slots[0].bot=true;s.Night.Deploy=5;stage=2;break;
   case 2:
    Physics.simulationMode=SimulationMode.Script;
    for(int i=0;i<StepsPerUpdate&&s.Phase=="match";i++){s.Step(.05f);Physics.Simulate(.05f);}
    Physics.simulationMode=SimulationMode.FixedUpdate;
    escaped|=s.World.Pawns.Values.Any(p=>p.State.position.y< -5);
    if(s.Phase!="result")return;
    Report(s);
    if(++night<Nights){s.ReturnLobby();stage=3;}else{Summarize();s.ReturnLobby();stage=4;}
    break;
   case 3:if(s.Phase=="lobby"&&!s.World&&!Loading()){s.StartMatch();stage=1;}break;
   case 4:if(s.Phase=="lobby"&&!s.World&&!Loading()){s.Slots[0].bot=false;s.StartMatch();stage=5;}break;
   case 5:if(!s.World)return;s.Night.Deploy=0;
    var socket=new TcpClient();socket.Connect("127.0.0.1",7777);guest=new LanPeer(socket);
    guest.Send(JsonUtility.ToJson(new LanPacket{type="hello",name="Invitado tardío",team=1,role=1}));
    next=EditorApplication.timeSinceStartup+1;stage=6;break;
   case 6:if(EditorApplication.timeSinceStartup<next)return;
    Check(slot>=4&&slot!=s.Night.Leader&&s.Slots.Count(x=>x.bot)==6,"Late join replaces a guard bot, never the leader");
    Check(slot>=0&&s.Slots[slot].loaded&&s.World.Pawns[slot].Class.Health==160,"Late client loads and receives selected class");
    guest.Dispose();next=EditorApplication.timeSinceStartup+1;stage=7;break;
   case 7:if(EditorApplication.timeSinceStartup<next)return;
    Check(s.Slots.Count(x=>x.bot)==7,"Late guest disconnect restores bot");Finish();break;
  }
 }
 static bool Loading()=>UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="MenuInicio";
 static void Report(LanSession s){
  var outcome=s.Night.Outcome;
  if(outcome==LanOutcome.Captured)captures++;if(outcome==LanOutcome.Dawn)dawns++;if(outcome==LanOutcome.DeaSpent)spent++;
  int fired=s.World.Pawns.Values.Sum(p=>p.State.shot);shots+=fired;
  Line($"INFO: Noche {night+1}: {outcome} a los {LanNightRules.NightSeconds-s.Remaining:0} s · disparos {fired} · bajas DEA {s.BlueScore} / Org {s.RedScore} · refuerzos DEA {s.Night.DeaLeft} / Org {s.Night.OrgLeft}");
 }
 static void Summarize(){
  Check(captures+dawns+spent==Nights,"Every night reaches an outcome");
  Check(shots>Nights*10,"Bots navigate, find enemies and fire");
  Check(captures>0,"Bots can reduce and handcuff the leader");
  Check(!escaped,"Combatants stay inside the map");
 }
 static void Finish(){guest?.Dispose();if(LanSession.Instance)LanSession.Instance.Leave();SessionState.SetBool("LanBotTest",false);EditorApplication.isPlaying=false;}
}
