using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Valle;
using Valle.Lan;
// Comprueba la Etapa 5: efectos de combate sin magia, sonidos de la partida y estilo de la interfaz.
[InitializeOnLoad]
public static class LanFxCheck {
 static readonly StringBuilder log=new StringBuilder();
 static int stage,mark;static bool menuShot;static double next,started;static LanAudio sound;static LanPawn leader,agent;
 static LanFxCheck(){EditorApplication.update+=Tick;}
 [MenuItem("Tools/Multijugador/Comprobar efectos y sonido")]
 static void Request(){File.WriteAllText("Temp/fx-test.request","");}
 static void Check(bool ok,string text){log.AppendLine((ok?"PASS: ":"FAIL: ")+text);File.WriteAllText("Documentation/LAN-fx-check.txt",log.ToString());}
 static void Tick(){
  if(!SessionState.GetBool("LanFxTest",false)){
   if(!File.Exists("Temp/fx-test.request")||EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)return;
   File.Delete("Temp/fx-test.request");EditorSceneManager.OpenScene("Assets/Scenes/MenuInicio.unity");SessionState.SetBool("LanFxTest",true);EditorApplication.isPlaying=true;return;
  }
  if(!EditorApplication.isPlaying)return;
  if(started==0)started=EditorApplication.timeSinceStartup;
  if(EditorApplication.timeSinceStartup-started>90){Check(false,"Effects check timed out");Finish();return;}
  if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.6;
  try{Advance(LanSession.Instance);}catch(Exception e){Check(false,e.ToString());Finish();}
 }
 static void Advance(LanSession s){
  switch(stage++){
   case 0:{
    // La foto se toma al final del cuadro: primero el menú solo, y en la siguiente vuelta se abre la sala.
    if(!menuShot){menuShot=true;ScreenCapture.CaptureScreenshot("Documentation/Menu-principal.png");stage--;return;}
    var music=UnityEngine.Object.FindAnyObjectByType<AmbientMusic>();var assets=Resources.Load<LanAssets>("Multiplayer/LanAssets");
    Check(music&&music.GetComponent<AudioSource>().clip==assets.tensionMusic,"The main menu plays the tension music");
    LanSession.Open();s=LanSession.Instance;s.PlayerName="Agente";s.Role=1;s.Host();break;}
   case 1:{
    var ui=GameObject.Find("UI - Multijugador");var assets=Resources.Load<LanAssets>("Multiplayer/LanAssets");
    var buttons=ui.GetComponentsInChildren<Button>(true).Where(b=>!b.name.StartsWith("Personaje")&&b.name!="Entrada").ToArray();
    Check(buttons.Length>0&&buttons.All(b=>b.GetComponent<Image>().sprite==assets.button&&b.GetComponent<Outline>()),"Interface buttons use the night style");
    s.StartMatch();break;}
   case 2:{if(!s.World){stage--;return;}
    sound=s.World.GetComponent<LanAudio>();
    Check(sound&&sound.NightPlaying&&sound.MusicPlaying,"Night ambience and music play during the match");
    s.Night.Deploy=.05f;break;}
   case 3:{
    Check(sound.History.Contains("helicoptero"),"The DEA arrives with a helicopter when the night starts");
    var at=s.World.Pawns[0].transform.position+Vector3.right*2;
    LanFx.Impact(at);LanFx.Muzzle(at);LanFx.Dust(at,10);
    Check(LanFx.Particles>0&&LanFx.ActiveLights>0,"Impacts, muzzle flashes and dust emit particles and light");
    var shooter=s.World.Pawns[1];s.World.Fire(shooter,Vector3.right);
    var tank=s.World.Pawns.Values.First(p=>p.Ability==LanAbilities.Shield);LanAbilities.Trigger(s.World,tank,new LanInput());
    s.World.AddMark(LanItems.FlareMark,0,at,10);
    s.World.AddMark(LanItems.AlarmMark,0,at+Vector3.forward*3,0);s.World.Marks.Last().fired=true;s.World.Marks.Last().time=8;
    break;}
   case 4:{
    var tank=s.World.Pawns.Values.First(p=>p.Ability==LanAbilities.Shield);var arc=tank.transform.Find("Escudo");
    Check(arc&&arc.GetComponent<LineRenderer>().enabled,"The heavy's shield is drawn in front of him");
    Check(UnityEngine.Object.FindObjectsByType<MagicFx>(FindObjectsInactive.Include).Length==0,"No magic effects appear during the match");
    var flare=GameObject.Find("Bengala");Check(flare&&flare.GetComponent<AudioSource>()&&flare.GetComponent<AudioSource>().isPlaying,"Flares hiss while they burn");
    Check(sound.History.Contains("alarma"),"A triggered alarm sounds for its team");
    leader=s.World.LeaderPawn;agent=s.World.Pawns.Values.First(p=>p.Team==LanNightRules.Dea&&s.Slots[p.Id].bot);
    s.Night.Wave=9999;leader.ResetLife(new Vector3(0,.05f,-8));leader.State.shield=0;leader.State.hp=0;mark=sound.History.Count;leader.Fall();
    break;}
   case 5:Check(sound.History.Skip(mark).Contains("radio"),"The radio reports the reduced leader");mark=sound.History.Count;
    // Sin granadas, comuneros ni habilidades de la organización: solo se mide el sonido de las esposas.
    s.World.Marks.Clear();s.World.Npcs.Clear();foreach(var p in s.World.Pawns.Values){p.State.grenades=0;if(p.Team==LanNightRules.Org)p.State.ability=999;}
    foreach(var spear in UnityEngine.Object.FindObjectsByType<LanSpearman>())if(spear.Body)spear.Body.enabled=false;
    agent.ResetLife(leader.transform.position+Vector3.left);Physics.SyncTransforms();break;
   case 6:if(!sound.History.Skip(mark).Contains("esposas"))log.AppendLine($"INFO: esposas · líder caído {leader.State.down:0.0} vida {leader.State.hp:0} · agente {agent.Id} vida {agent.State.hp:0} a {Vector3.Distance(agent.transform.position,leader.transform.position):0.00} sujeta {agent.Holding} · captura {s.Night.Capture:0.00} por {s.Night.Capturer} · npcs {s.World.Npcs.Count} · marcas {string.Join(",",s.World.Marks.Select(m=>m.kind))} · aturdido {agent.State.stun:0.0} montado {agent.State.ride}");
    Check(sound.History.Skip(mark).Contains("esposas"),"Handcuffs click when the capture starts");mark=sound.History.Count;s.Night.Wave=.9f;break;
   case 7:break;
   case 8:Check(sound.History.Skip(mark).Contains("radio"),"The radio announces reinforcements");Finish();break;
  }
 }
 static void Finish(){menuShot=false;if(LanSession.Instance)LanSession.Instance.Leave();SessionState.SetBool("LanFxTest",false);EditorApplication.isPlaying=false;}
}
