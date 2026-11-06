using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Valle.Lan {
 public sealed class LanUI:MonoBehaviour {
  LanSession session;LanAssets art;Font body;Transform root;GameObject connection,lobby,hud,pause,results;InputField nameField,ipField;
  Sprite fillSprite;
  Text status,roleLabel,teamLabel,rosterBlue,rosterRed,lobbyClass,lobbyStatus,score,health,stamina,ammo,hint,resultText;
  Image healthBar,staminaBar;Button start,ready,resultLobby;Text readyLabel;string previousPhase;
  static readonly Color Gold=new Color(.84f,.7f,.43f),Paper=new Color(.94f,.91f,.82f),Ink=new Color(.035f,.065f,.075f,.97f);
  void Start(){session=GetComponent<LanSession>();art=Resources.Load<LanAssets>("Multiplayer/LanAssets");body=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");Build();}
  void Build(){fillSprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),new Vector2(.5f,.5f));var go=new GameObject("UI - Multijugador",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));go.transform.SetParent(transform,false);root=go.transform;var c=go.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=100;var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
   connection=Screen("Conexión LAN");var card=Panel(connection.transform,"Conectar",Vector2.zero,new Vector2(860,650));Title(card,"EL VALLE · 4 CONTRA 4",255);Label(card,"Misma red Wi-Fi o cable · Sin cuentas ni servicios externos",new Vector2(0,203),new Vector2(740,30),19);
   Label(card,"TU NOMBRE",new Vector2(-210,151),new Vector2(300,24),14,Gold);nameField=Field(card,new Vector2(-190,109),new Vector2(350,44),"Jugador");
   Label(card,"IP DEL ANFITRIÓN",new Vector2(190,151),new Vector2(350,24),14,Gold);ipField=Field(card,new Vector2(190,109),new Vector2(350,44),"192.168.1.20");ipField.characterLimit=15;
   teamLabel=Button(card,"Equipo: Azul",new Vector2(-190,40),new Vector2(350,48),()=>{session.Team=1-session.Team;}).GetComponentInChildren<Text>();
   Button(card,"Cambiar clase",new Vector2(190,40),new Vector2(350,48),()=>session.ChooseClass((session.Role+1)%3));
   roleLabel=Label(card,"",new Vector2(0,-24),new Vector2(760,70),20);
   Button(card,"Crear sala",new Vector2(-190,-115),new Vector2(350,52),()=>{session.PlayerName=nameField.text;session.Host();});Button(card,"Unirse por IP",new Vector2(190,-115),new Vector2(350,52),()=>{session.PlayerName=nameField.text;session.Join(ipField.text);});
   status=Label(card,"",new Vector2(0,-196),new Vector2(760,80),17);Button(card,"Volver",new Vector2(0,-270),new Vector2(240,44),()=>session.Leave());
   lobby=Screen("Sala");var room=Panel(lobby.transform,"Equipos",Vector2.zero,new Vector2(1050,660));Title(room,"PREPARA TU EQUIPO",278);
   lobbyStatus=Label(room,"",new Vector2(0,227),new Vector2(950,40),18);
   var blue=Panel(room,"Azul",new Vector2(-245,30),new Vector2(460,320));var red=Panel(room,"Rojo",new Vector2(245,30),new Vector2(460,320));Title(blue,"AZUL",125,new Color(.35f,.75f,1));Title(red,"ROJO",125,new Color(1,.44f,.36f));
   rosterBlue=Label(blue,"",new Vector2(0,-25),new Vector2(410,238),21);rosterRed=Label(red,"",new Vector2(0,-25),new Vector2(410,238),21);rosterBlue.alignment=rosterRed.alignment=TextAnchor.UpperLeft;
   lobbyClass=Label(room,"",new Vector2(0,-168),new Vector2(950,35),18);Button(room,"Cambiar clase",new Vector2(-330,-225),new Vector2(250,46),()=>session.ChooseClass((session.Role+1)%3));
   start=Button(room,"Iniciar partida",new Vector2(0,-225),new Vector2(300,46),()=>session.StartMatch());ready=Button(room,"Estoy listo",new Vector2(0,-225),new Vector2(300,46),()=>session.Ready());readyLabel=ready.GetComponentInChildren<Text>();
   Button(room,"Abandonar sala",new Vector2(330,-225),new Vector2(250,46),()=>session.Leave());Label(room,"Los puestos libres juegan como bots · 20 bajas o 5 minutos · Sin fuego amigo",new Vector2(0,-285),new Vector2(980,28),16);
   hud=Rect("HUD LAN",root,Vector2.zero,new Vector2(1280,720)).gameObject;
   var scoreboard=Panel(hud.transform,"Marcador",new Vector2(0,310),new Vector2(390,72));score=Label(scoreboard,"",Vector2.zero,new Vector2(360,60),24);
   var stats=Panel(hud.transform,"Estado",new Vector2(-435,-262),new Vector2(360,135));health=Label(stats,"",new Vector2(0,37),new Vector2(320,28),19);healthBar=Bar(stats,14,new Color(.8f,.23f,.27f));stamina=Label(stats,"",new Vector2(0,-14),new Vector2(320,28),17);staminaBar=Bar(stats,-37,new Color(.24f,.75f,.53f));
   var weapons=Panel(hud.transform,"Munición",new Vector2(445,-264),new Vector2(340,130));ammo=Label(weapons,"",new Vector2(0,12),new Vector2(310,100),22);
   hint=Label(hud.transform,"",new Vector2(0,-275),new Vector2(470,100),20);Label(hud.transform,"ESC  ·  Opciones",new Vector2(500,320),new Vector2(220,30),17);
   pause=Screen("Opciones de partida");var options=Panel(pause.transform,"Opciones",Vector2.zero,new Vector2(580,540));Title(options,"EN PARTIDA",211);Label(options,"La partida continúa mientras consultas este menú.",new Vector2(0,158),new Vector2(530,42),17);
   Label(options,"WASD / Flechas   Mover      Shift   Correr\nEspacio   Saltar      Q / E   Girar cámara\nClic   Disparar      R   Recargar\nF   Recoger / cambiar      G   Soltar",new Vector2(0,34),new Vector2(530,150),20);Button(options,"Continuar",new Vector2(0,-112),new Vector2(320,48),()=>session.MenuOpen=false);Button(options,"Salir de la partida",new Vector2(0,-181),new Vector2(320,48),()=>session.Leave());
   results=Screen("Resultado");var end=Panel(results.transform,"Fin de partida",Vector2.zero,new Vector2(680,580));Title(end,"FIN DE LA PARTIDA",230);resultText=Label(end,"",new Vector2(0,50),new Vector2(610,275),23);resultLobby=Button(end,"Volver a la sala",new Vector2(0,-155),new Vector2(330,48),()=>{if(session.IsHost)session.ReturnLobby();});Button(end,"Salir al inicio",new Vector2(0,-222),new Vector2(330,44),()=>session.Leave());
  }
  void Update(){if(!session||!connection)return;string phase=session.Phase;connection.SetActive(phase=="offline"||phase=="connecting");lobby.SetActive(phase=="lobby");hud.SetActive(phase=="match");pause.SetActive(phase=="match"&&session.MenuOpen);results.SetActive(phase=="result");resultLobby.interactable=session.IsHost;
   status.text=session.Status;roleLabel.text=LanClass.Get(session.Role).Name+"\n"+LanClass.Get(session.Role).Description;teamLabel.text="Equipo: "+(session.Team==0?"Azul":"Rojo");
   if(phase=="lobby"&&session.Slots!=null){rosterBlue.text=Roster(0);rosterRed.text=Roster(1);lobbyStatus.text=session.IsHost?"Comparte tu IP: "+session.LocalAddress+"  ·  Puerto 7777":"Conectado · Equipo "+(session.LocalSlot<4?"azul":"rojo");lobbyClass.text="Tu clase: "+LanClass.Get(session.Slots[session.LocalSlot].role).Description;start.gameObject.SetActive(session.IsHost);start.interactable=session.CanStart;ready.gameObject.SetActive(!session.IsHost);readyLabel.text=session.Slots[session.LocalSlot].ready?"Listo ✓ (cambiar)":"Estoy listo";}
   if((phase=="match"||phase=="result")&&session.World&&session.LocalSlot>=0){var pawn=session.World.Pawns[session.LocalSlot];var state=pawn.State;var role=pawn.Class;score.text=$"AZUL  {session.BlueScore}   —   {session.RedScore}  ROJO\n<size=17>{Mathf.CeilToInt(session.Remaining)/60:00}:{Mathf.CeilToInt(session.Remaining)%60:00}   ·   Meta: 20</size>";
    health.text=$"{role.Name.ToUpper()}  ·  VIDA {state.hp:0}/{role.Health:0}";stamina.text=$"ESTAMINA {state.stamina:0}   ·   DEF {role.Defense:0}";healthBar.fillAmount=state.hp/role.Health;staminaBar.fillAmount=state.stamina/role.Stamina;
    ammo.text=state.weapon==0?"SIN ARMA\n<size=16>F · Recoger del suelo</size>":$"{(state.weapon==1?"PISTOLA":"RIFLE")}   {state.ammo} / {state.reserve}\n<size=16>{(state.reload>0?"Recargando…":"CLIC Disparar   R Recargar   G Soltar")}</size>";
    var drop=session.World.Nearest(pawn);hint.text=state.hp<=0?$"Reapareces en {Mathf.CeilToInt(state.respawn)}…":drop!=null?"[ F ]  Recoger / cambiar arma":state.shield>0?"Protección de reaparición":"";
    if(phase=="result"){string result=session.BlueScore==session.RedScore?"EMPATE":session.BlueScore>session.RedScore?"VICTORIA AZUL":"VICTORIA ROJA";resultText.text=$"{result}\n\n{session.BlueScore}   —   {session.RedScore}\n\nTus bajas: {session.Slots[session.LocalSlot].kills}    Muertes: {session.Slots[session.LocalSlot].deaths}\n\n<size=17>{(session.IsHost?"Puedes iniciar otra ronda desde la sala.":"Espera a que el anfitrión vuelva a la sala.")}</size>";}
   }
   if(phase!=previousPhase){previousPhase=phase;if(EventSystem.current)EventSystem.current.SetSelectedGameObject(null);if(phase=="offline")nameField.Select();if(phase=="lobby"){if(session.IsHost)start.Select();else ready.Select();}}
  }
  void OnDestroy(){if(fillSprite)Destroy(fillSprite);}
  string Roster(int team){var s="";foreach(var slot in session.Slots)if(slot.team==team)s+=$"{slot.name}{(slot.id==session.LocalSlot?" (tú)":"")}\n<size=16>{LanClass.Get(slot.role).Name} · {(slot.bot?"BOT":slot.ready?"LISTO":"Preparando…")}</size>\n\n";return s;}
  GameObject Screen(string name){var r=Panel(root,name,Vector2.zero,new Vector2(4000,4000));r.GetComponent<Image>().color=new Color(.02f,.04f,.05f,.97f);return r.gameObject;}
  RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.sizeDelta=size;r.anchoredPosition=pos;return r;}
  RectTransform Panel(Transform parent,string name,Vector2 pos,Vector2 size){var r=Rect(name,parent,pos,size);r.gameObject.AddComponent<Image>().color=Ink;return r;}
  Text Label(Transform parent,string text,Vector2 pos,Vector2 size,int fontSize,Color? color=null){var r=Rect("Texto",parent,pos,size);var t=r.gameObject.AddComponent<Text>();t.font=body;t.fontSize=fontSize;t.color=color??Paper;t.text=text;t.alignment=TextAnchor.MiddleCenter;t.verticalOverflow=VerticalWrapMode.Overflow;t.raycastTarget=false;return t;}
  void Title(Transform p,string text,float y,Color? color=null){var title=Label(p,text,new Vector2(0,y),new Vector2(760,65),32,color??Gold);title.font=art.font;}
  Button Button(Transform parent,string text,Vector2 pos,Vector2 size,UnityEngine.Events.UnityAction action){var r=Panel(parent,text,pos,size);var image=r.GetComponent<Image>();image.sprite=art.button;image.color=new Color(.65f,.67f,.6f);var button=r.gameObject.AddComponent<Button>();button.onClick.AddListener(action);var colors=button.colors;colors.highlightedColor=new Color(1,.9f,.6f);colors.selectedColor=colors.highlightedColor;button.colors=colors;Label(r,text,Vector2.zero,size,20);return button;}
  InputField Field(Transform p,Vector2 pos,Vector2 size,string initial){var r=Panel(p,"Entrada",pos,size);r.GetComponent<Image>().color=new Color(.14f,.18f,.19f);var text=Label(r,initial,Vector2.zero,size-new Vector2(20,0),21);text.supportRichText=false;text.alignment=TextAnchor.MiddleLeft;var field=r.gameObject.AddComponent<InputField>();field.textComponent=text;field.text=initial;field.characterLimit=16;return field;}
  Image Bar(Transform p,float y,Color color){var r=Panel(p,"Barra",new Vector2(0,y),new Vector2(320,8));r.GetComponent<Image>().color=new Color(.2f,.23f,.24f);var fill=Panel(r,"Valor",Vector2.zero,new Vector2(320,8)).GetComponent<Image>();fill.sprite=fillSprite;fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;fill.color=color;return fill;}
 }
}
