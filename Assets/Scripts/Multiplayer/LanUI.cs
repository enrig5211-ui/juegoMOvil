using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Valle.Lan {
 public sealed partial class LanUI:MonoBehaviour {
  LanSession session;LanAssets art;Font body;Transform root;GameObject connection,lobby,hud,pause,results,statsPanel,ammoPanel;InputField nameField,ipField;
  Sprite fillSprite;
  Text status,abilityLabel,roleLabel,teamLabel,rosterBlue,rosterRed,lobbyClass,lobbyStatus,score,health,stamina,ammo,hint,resultText;
  readonly Text[] sizeLabels=new Text[2];readonly Button[] sizeButtons=new Button[4];
  Image healthBar,staminaBar,nightVision;Text alarmMark,leaderMark;Button start,ready,resultLobby,leaderButton;Text readyLabel,leaderLabel;string previousPhase;
  // Estilo de operación nocturna: azul noche, ámbar y texto en negrita.
  static readonly Color Gold=new Color(1,.72f,.25f),Paper=new Color(.93f,.94f,.95f),Ink=new Color(.03f,.05f,.08f,.96f),Slate=new Color(.13f,.17f,.24f),AmberDeep=new Color(.85f,.52f,.1f);
  void Start(){session=GetComponent<LanSession>();art=Resources.Load<LanAssets>("Multiplayer/LanAssets");body=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");Build();}
  void Build(){fillSprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),new Vector2(.5f,.5f));var go=new GameObject("UI - Multijugador",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));go.transform.SetParent(transform,false);root=go.transform;var c=go.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=100;var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
   connection=Screen("Conexión LAN");var card=Panel(connection.transform,"Conectar",Vector2.zero,new Vector2(860,650));Title(card,"NOCHE EN EL CHAPARE",255);Label(card,"Misma red Wi-Fi o cable · Sin cuentas ni servicios externos",new Vector2(0,203),new Vector2(740,30),19);
   Label(card,"TU NOMBRE",new Vector2(-210,151),new Vector2(300,24),14,Gold);nameField=Field(card,new Vector2(-190,109),new Vector2(350,44),"Jugador");
   Label(card,"IP DEL ANFITRIÓN",new Vector2(190,151),new Vector2(350,24),14,Gold);ipField=Field(card,new Vector2(190,109),new Vector2(350,44),"192.168.1.20");ipField.characterLimit=15;
   teamLabel=Button(card,"Bando: DEA",new Vector2(-190,40),new Vector2(350,48),()=>{session.Team=1-session.Team;}).GetComponentInChildren<Text>();
   Button(card,"Cambiar clase",new Vector2(190,40),new Vector2(350,48),()=>session.ChooseClass((session.Role+1)%LanClass.Roles));
   roleLabel=Label(card,"",new Vector2(0,-24),new Vector2(760,70),20);
   Button(card,"Crear sala",new Vector2(-190,-115),new Vector2(350,52),()=>{session.PlayerName=nameField.text;session.Host();});Button(card,"Unirse por IP",new Vector2(190,-115),new Vector2(350,52),()=>{session.PlayerName=nameField.text;session.Join(ipField.text);});
   status=Label(card,"",new Vector2(0,-196),new Vector2(760,80),17);Button(card,"Volver",new Vector2(0,-270),new Vector2(240,44),()=>session.Leave());
   lobby=Screen("Sala");var room=Panel(lobby.transform,"Equipos",Vector2.zero,new Vector2(1050,660));Title(room,"PREPARA TU EQUIPO",278);
   lobbyStatus=Label(room,"",new Vector2(0,227),new Vector2(950,40),18);
   var blue=Panel(room,"Azul",new Vector2(-245,30),new Vector2(460,320));var red=Panel(room,"Rojo",new Vector2(245,30),new Vector2(460,320));Title(blue,"DEA",125,new Color(.35f,.75f,1));Title(red,"ORGANIZACIÓN",125,new Color(1,.44f,.36f));
   rosterBlue=Label(blue,"",new Vector2(0,-48),new Vector2(420,200),17);rosterRed=Label(red,"",new Vector2(0,-48),new Vector2(420,200),17);rosterBlue.alignment=rosterRed.alignment=TextAnchor.UpperLeft;
   // Tamaño de cada bando: el anfitrión lo cambia con − y +.
   for(int t=0;t<2;t++){int team=t;var panel=t==0?blue:red;sizeLabels[t]=Label(panel,"",new Vector2(0,88),new Vector2(220,30),16,Paper);
    sizeButtons[t*2]=Button(panel,"−",new Vector2(-130,88),new Vector2(48,34),()=>session.ResizeTeam(team,-1));sizeButtons[t*2+1]=Button(panel,"+",new Vector2(130,88),new Vector2(48,34),()=>session.ResizeTeam(team,1));}
   lobbyClass=Label(room,"",new Vector2(-165,-168),new Vector2(600,40),16);leaderButton=Button(room,"Ser el líder",new Vector2(330,-168),new Vector2(250,40),()=>session.ChooseLeader());leaderLabel=leaderButton.GetComponentInChildren<Text>();Button(room,"Cambiar clase",new Vector2(-330,-225),new Vector2(250,46),()=>session.ChooseClass((session.Role+1)%LanClass.Roles));
   start=Button(room,"Iniciar partida",new Vector2(0,-225),new Vector2(300,46),()=>session.BeginSelect());ready=Button(room,"Estoy listo",new Vector2(0,-225),new Vector2(300,46),()=>session.Ready());readyLabel=ready.GetComponentInChildren<Text>();
   Button(room,"Abandonar sala",new Vector2(330,-225),new Vector2(250,46),()=>session.Leave());Label(room,"Los puestos libres juegan como bots · Captura al líder antes del amanecer · Sin fuego amigo",new Vector2(0,-285),new Vector2(980,28),16);
   hud=Rect("HUD LAN",root,Vector2.zero,new Vector2(1280,720)).gameObject;
   nightVision=Panel(hud.transform,"Visor nocturno",Vector2.zero,new Vector2(4000,4000)).GetComponent<Image>();nightVision.color=new Color(.2f,1,.35f,.14f);nightVision.raycastTarget=false;
   var scoreboard=Panel(hud.transform,"Marcador",new Vector2(0,310),new Vector2(390,72));score=Label(scoreboard,"",Vector2.zero,new Vector2(360,60),24);
   var stats=Panel(hud.transform,"Estado",new Vector2(-435,-252),new Vector2(360,155));statsPanel=stats.gameObject;health=Label(stats,"",new Vector2(0,47),new Vector2(320,28),19);healthBar=Bar(stats,24,new Color(.8f,.23f,.27f));stamina=Label(stats,"",new Vector2(0,-4),new Vector2(320,28),17);staminaBar=Bar(stats,-27,new Color(.24f,.75f,.53f));abilityLabel=Label(stats,"",new Vector2(0,-52),new Vector2(330,26),16,Gold);
   var weapons=Panel(hud.transform,"Munición",new Vector2(445,-264),new Vector2(360,130));ammoPanel=weapons.gameObject;ammo=Label(weapons,"",new Vector2(0,4),new Vector2(340,116),21);
   hint=Label(hud.transform,"",new Vector2(0,-275),new Vector2(470,100),20);Label(hud.transform,"ESC  ·  Opciones",new Vector2(500,320),new Vector2(220,30),17);
   alarmMark=Label(hud.transform,"",Vector2.zero,new Vector2(240,30),19,new Color(1,.4f,.35f));leaderMark=Label(hud.transform,"",Vector2.zero,new Vector2(240,30),17,Gold);
   pause=Screen("Opciones de partida");var options=Panel(pause.transform,"Opciones",Vector2.zero,new Vector2(580,540));Title(options,"EN PARTIDA",211);Label(options,"La partida continúa mientras consultas este menú.",new Vector2(0,158),new Vector2(530,42),17);
   Label(options,"WASD / Flechas   Mover      Shift   Correr\nEspacio   Saltar      Q / E   Girar cámara\nClic   Disparar      R   Recargar      G   Granada\n1 / 2   Arma o granada      X   Soltar arma\nF   Recoger      Mantener F   Esposar      M   Mapa\nT   Linterna   C   Item   V   Blindado   Clic der.   Habilidad",new Vector2(0,34),new Vector2(530,150),19);Button(options,"Continuar",new Vector2(0,-112),new Vector2(320,48),()=>session.MenuOpen=false);Button(options,"Salir de la partida",new Vector2(0,-181),new Vector2(320,48),()=>session.Leave());
   results=Screen("Resultado");var end=Panel(results.transform,"Fin de partida",Vector2.zero,new Vector2(680,580));Title(end,"FIN DE LA PARTIDA",230);resultText=Label(end,"",new Vector2(0,50),new Vector2(610,275),23);resultLobby=Button(end,"Volver a la sala",new Vector2(0,-155),new Vector2(330,48),()=>{if(session.IsHost)session.ReturnLobby();});Button(end,"Salir al inicio",new Vector2(0,-222),new Vector2(330,44),()=>session.Leave());
   BuildSelect();
   BuildMaps();
  }
  void Update(){if(!session||!connection)return;string phase=session.Phase;connection.SetActive(phase=="offline"||phase=="connecting");lobby.SetActive(phase=="lobby");UpdateSelect();hud.SetActive(phase=="match");bool planning=phase=="match"&&session.Night.Deploying&&session.LocalSlot>=0&&session.Slots[session.LocalSlot].team==LanNightRules.Dea;bool covered=planning||phase=="match"&&session.MapOpen;statsPanel.SetActive(!covered);ammoPanel.SetActive(!covered);pause.SetActive(phase=="match"&&session.MenuOpen);results.SetActive(phase=="result");resultLobby.interactable=session.IsHost;
   status.text=session.Status;var chosen=LanClass.For(session.Team,session.Role);roleLabel.text=chosen.Name+"\n"+chosen.Description;teamLabel.text="Bando: "+(session.Team==LanNightRules.Dea?"DEA":"Organización");
   if(phase=="lobby"&&session.Slots!=null){rosterBlue.text=Roster(0);rosterRed.text=Roster(1);for(int t=0;t<2;t++){int size=session.TeamSize(t);sizeLabels[t].text=$"{size} {(t==0?"agentes":"integrantes")}  ·  de {LanSession.MinTeam} a {LanSession.MaxTeam}";sizeButtons[t*2].gameObject.SetActive(session.IsHost);sizeButtons[t*2+1].gameObject.SetActive(session.IsHost);sizeButtons[t*2].interactable=size>LanSession.MinTeam;sizeButtons[t*2+1].interactable=size<LanSession.MaxTeam;}lobbyStatus.text=session.IsHost?"Comparte tu IP: "+session.LocalAddress+"  ·  Puerto 7777":"Conectado · "+(session.Slots[session.LocalSlot].team==LanNightRules.Dea?"DEA":"Organización");lobbyClass.text=LobbyRole();UpdateLeaderButton();start.gameObject.SetActive(session.IsHost);start.interactable=session.CanStart;ready.gameObject.SetActive(!session.IsHost);readyLabel.text=session.Slots[session.LocalSlot].ready?"Listo ✓ (cambiar)":"Estoy listo";}
   if((phase=="match"||phase=="result")&&session.World&&session.LocalSlot>=0){var pawn=session.World.Pawns[session.LocalSlot];var state=pawn.State;var role=pawn.Class;score.text=Clock();if(phase=="match")UpdateMaps(pawn);
    health.text=$"{role.Name.ToUpper()}  ·  VIDA {state.hp:0}/{role.Health:0}";stamina.text=$"ESTAMINA {state.stamina:0}   ·   DEF {role.Defense:0}";abilityLabel.text=AbilityText(pawn);healthBar.fillAmount=state.hp/role.Health;staminaBar.fillAmount=state.stamina/role.Stamina;
    ammo.text=AmmoText(pawn);
    hint.text=Hint(pawn);UpdateNight(pawn);
    if(phase=="result")resultText.text=ResultText();
   }
   if(phase!=previousPhase){previousPhase=phase;primed=false;lastHp=-1;shots.Clear();hits.Clear();if(EventSystem.current)EventSystem.current.SetSelectedGameObject(null);if(phase=="offline")nameField.Select();if(phase=="lobby"){if(session.IsHost)start.Select();else ready.Select();}}
  }
  void OnDestroy(){if(fillSprite)Destroy(fillSprite);}
  string LobbyRole(){
   var mine=session.Slots[session.LocalSlot];
   return mine.id==session.Night.Leader?"Eres el líder: "+LanClass.Leader.Description:LanClass.For(mine.team,mine.role).Name+": "+LanClass.For(mine.team,mine.role).Description;
  }
  void UpdateLeaderButton(){
   var mine=session.Slots[session.LocalSlot];
   int leader=session.Night.Leader;
   leaderButton.gameObject.SetActive(mine.team==LanNightRules.Org);
   leaderButton.interactable=leader<0||leader==mine.id;
   leaderLabel.text=leader==mine.id?"Dejar de ser líder":leader<0?"Ser el líder":"Líder ocupado";
  }
  string Clock(){
   var night=session.Night;
   int time=Mathf.CeilToInt(night.Deploying?night.Deploy:session.Remaining);
   string clock=$"{time/60:00}:{time%60:00}";
   if(!night.Deploying)return $"AMANECE EN  {clock}\n<size=17>Refuerzos   DEA {night.DeaLeft}   ·   Organización {night.OrgLeft}</size>";
   bool dea=session.Slots[session.LocalSlot].team==LanNightRules.Dea;
   return $"DESPLIEGUE  {clock}\n<size=17>{(dea?"Entras al empezar la noche":"Esconde al líder y prepara la defensa")}</size>";
  }
  string Hint(LanPawn pawn){
   var night=session.Night;
   var state=pawn.State;
   var leader=session.World.LeaderPawn;
   bool dea=pawn.Team==LanNightRules.Dea;
   int progress=Mathf.RoundToInt(night.Capture*100);
   if(pawn.IsLeader&&state.down>0)return progress>0?$"¡Te están esposando!  {progress} %":$"Reducido  ·  Te recuperas en {Mathf.CeilToInt(state.down)}…";
   if(state.hp<=0)return (dea?night.DeaLeft:night.OrgLeft)>0?$"Refuerzos en {Mathf.CeilToInt(night.Wave)}…":"Sin refuerzos";
   if(progress>0)return dea?$"Esposando al líder  {progress} %":$"¡Están esposando al líder!  {progress} %";
   if(dea&&leader&&leader.State.down>0&&Vector3.Distance(leader.transform.position,pawn.transform.position)<=LanNightRules.CuffRange)return "[ Mantén F ]  Esposar al líder";
   var drop=session.World.Nearest(pawn);
   if(drop!=null)return drop.item>0?$"[ F ]  Recoger {LanItems.Name(drop.item).ToLower()}":"[ F ]  Recoger / cambiar arma";
   if(!night.Deploying&&state.ride==0&&state.hp>0){var near=session.World.Vehicles.FirstOrDefault(v=>v.team==pawn.Team&&v.respawn<=0&&Vector3.Distance(v.position,pawn.transform.position)<=LanWorld.VehicleBoard);if(near!=null&&near.driver<0)return near.kind==LanWorld.Truck?"[ V ]  Subir al chuto":"[ V ]  Subir al blindado";}
   return state.shield>0&&!night.Deploying?"Protección":"";
  }
  string AbilityText(LanPawn pawn){var state=pawn.State;string name=LanAbilities.Name(pawn.Ability).ToUpper();string status=state.active>0?"activa":state.ability>0?$"{Mathf.CeilToInt(state.ability)} s":"lista";return $"CLIC DER · {name} · {status}";}
  // Ranuras: 1 arma y 2 granadas; la activa va en ámbar. En el blindado, su vida y el puesto.
  string AmmoText(LanPawn pawn){
   var state=pawn.State;var vehicle=session.World.VehicleOf(pawn);
   if(vehicle!=null)return $"{LanWorld.KindName(vehicle.kind)}   {Mathf.Max(0,vehicle.hp):0} / {LanWorld.MaxHealth(vehicle.kind):0}\n<size=16>WASD Conducir   ·   CLIC Ametralladora\nV · Bajar</size>";
   bool grenade=state.equip==1||session.Equipped==1&&state.grenades>0;
   string gun=state.weapon==0?"SIN ARMA":$"{(state.weapon==1?"PISTOLA":"RIFLE")} {state.ammo}/{state.reserve}";
   string slots=$"{(grenade?"":"<color=#FFB840>")}1 · {gun}{(grenade?"":"</color>")}    {(grenade?"<color=#FFB840>":"")}2 · GRANADAS ×{state.grenades}{(grenade?"</color>":"")}";
   string help=grenade?"CLIC Lanzar granada   G también lanza":state.weapon==0?"F · Recoger del suelo":state.reload>0?"Recargando…":"CLIC Disparar   R Recargar   X Soltar";
   string item=state.item>0?$"C · {LanItems.Name(state.item)}":"Sin item";
   return $"<size=19>{slots}</size>\n<size=16>{help}\n{item}   ·   T · Linterna {(state.lamp?"encendida":"apagada")}</size>";
  }
  // Visor nocturno, aviso de alarma del equipo y, para los guardias, dónde está el líder.
  void UpdateNight(LanPawn pawn){
   nightVision.gameObject.SetActive(pawn.State.vision>0&&pawn.State.hp>0);
   var world=session.World;LanMark alarm=null;
   foreach(var mark in world.Marks)if(mark.kind==LanItems.AlarmMark&&mark.fired&&mark.team==pawn.Team){alarm=mark;break;}
   Point(alarmMark,alarm!=null?alarm.position:(Vector3?)null,"¡ALARMA!",false);
   var leader=world.LeaderPawn;bool guard=pawn.Team==LanNightRules.Org&&leader&&leader!=pawn;
   Point(leaderMark,guard?leader.transform.position:(Vector3?)null,guard&&leader.State.down>0?"¡LÍDER REDUCIDO!":"LÍDER",true);
  }
  void Point(Text mark,Vector3? target,string text,bool onlyOffscreen){
   var camera=Camera.main;if(target==null||!camera){mark.gameObject.SetActive(false);return;}
   var view=camera.WorldToViewportPoint(target.Value);var offset=new Vector2(view.x-.5f,view.y-.5f);
   bool inside=view.z>0&&Mathf.Abs(offset.x)<.44f&&Mathf.Abs(offset.y)<.4f;
   if(onlyOffscreen&&inside){mark.gameObject.SetActive(false);return;}
   if(view.z<0)offset=-offset;offset/=Mathf.Max(Mathf.Abs(offset.x)/.44f,Mathf.Abs(offset.y)/(offset.y>0?.33f:.4f),1);
   var size=((RectTransform)root).rect.size;mark.gameObject.SetActive(true);mark.text=text;mark.rectTransform.anchoredPosition=new Vector2(offset.x*size.x,offset.y*size.y);
  }
  string ResultText(){
   var outcome=session.Night.Outcome;
   var slot=session.Slots[session.LocalSlot];
   string title=outcome==LanOutcome.Captured?"LA DEA CAPTURÓ AL LÍDER":outcome==LanOutcome.Dawn?"AMANECIÓ · EL LÍDER SIGUE LIBRE":outcome==LanOutcome.DeaSpent?"LA DEA SE QUEDÓ SIN REFUERZOS":"PARTIDA TERMINADA";
   bool deaWon=outcome==LanOutcome.Captured;
   string verdict=outcome==LanOutcome.None?"":(slot.team==LanNightRules.Dea)==deaWon?"VICTORIA":"DERROTA";
   string next=session.IsHost?"Puedes iniciar otra ronda desde la sala.":"Espera a que el anfitrión vuelva a la sala.";
   return $"{title}\n\n{verdict}\n\nBajas   DEA {session.BlueScore}   —   {session.RedScore}   Organización\n\nTus bajas: {slot.kills}    Muertes: {slot.deaths}\n\n<size=17>{next}</size>";
  }
  string Roster(int team){var s="";foreach(var slot in session.Slots)if(slot.team==team)s+=$"{(slot.id==session.LocalSlot?"<color=#FFB840>":"")}{slot.name}{(slot.id==session.LocalSlot?" (tú)</color>":"")}  <size=14>· {(slot.id==session.Night.Leader?"LÍDER":LanClass.For(slot.team,slot.role).Name)} · {(slot.bot?"BOT":slot.ready?"LISTO":"Preparando…")}</size>\n";return s;}
  GameObject Screen(string name){var r=Panel(root,name,Vector2.zero,new Vector2(4000,4000));r.GetComponent<Image>().color=new Color(.02f,.04f,.05f,1);return r.gameObject;}
  RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.sizeDelta=size;r.anchoredPosition=pos;return r;}
  RectTransform Panel(Transform parent,string name,Vector2 pos,Vector2 size){var r=Rect(name,parent,pos,size);r.gameObject.AddComponent<Image>().color=Ink;return r;}
  Text Label(Transform parent,string text,Vector2 pos,Vector2 size,int fontSize,Color? color=null){var r=Rect("Texto",parent,pos,size);var t=r.gameObject.AddComponent<Text>();t.font=body;t.fontSize=fontSize;t.color=color??Paper;t.text=text;t.alignment=TextAnchor.MiddleCenter;t.verticalOverflow=VerticalWrapMode.Overflow;t.raycastTarget=false;return t;}
  void Title(Transform p,string text,float y,Color? color=null){var title=Label(p,text,new Vector2(0,y),new Vector2(760,65),32,color??Gold);title.font=body;title.fontStyle=FontStyle.Bold;}
  Button Button(Transform parent,string text,Vector2 pos,Vector2 size,UnityEngine.Events.UnityAction action){var r=Panel(parent,text,pos,size);var image=r.GetComponent<Image>();image.sprite=art.button;image.type=Image.Type.Sliced;image.color=Color.white;var button=r.gameObject.AddComponent<Button>();button.onClick.AddListener(action);var colors=button.colors;colors.normalColor=Slate;colors.highlightedColor=AmberDeep;colors.selectedColor=AmberDeep;colors.pressedColor=new Color(.6f,.36f,.06f);colors.disabledColor=new Color(.1f,.12f,.16f,.7f);button.colors=colors;var outline=r.gameObject.AddComponent<Outline>();outline.effectColor=new Color(1,.72f,.25f,.55f);outline.effectDistance=new Vector2(1.5f,-1.5f);var label=Label(r,text,Vector2.zero,size,20);label.fontStyle=FontStyle.Bold;return button;}
  InputField Field(Transform p,Vector2 pos,Vector2 size,string initial){var r=Panel(p,"Entrada",pos,size);r.GetComponent<Image>().color=new Color(.14f,.18f,.19f);var text=Label(r,initial,Vector2.zero,size-new Vector2(20,0),21);text.supportRichText=false;text.alignment=TextAnchor.MiddleLeft;var field=r.gameObject.AddComponent<InputField>();field.textComponent=text;field.text=initial;field.characterLimit=16;return field;}
  Image Bar(Transform p,float y,Color color){var r=Panel(p,"Barra",new Vector2(0,y),new Vector2(320,8));r.GetComponent<Image>().color=new Color(.2f,.23f,.24f);var fill=Panel(r,"Valor",Vector2.zero,new Vector2(320,8)).GetComponent<Image>();fill.sprite=fillSprite;fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;fill.color=color;return fill;}
 }
}
