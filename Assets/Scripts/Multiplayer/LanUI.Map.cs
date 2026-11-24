using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace Valle.Lan {
 // Plan de despliegue de la DEA, minimapa, mapa grande (M) y avisos de disparos, daño, granadas,
 // bombardeos y rastreos. Todo se calcula en cada cliente con lo que ya trae la instantánea.
 public sealed partial class LanUI {
  const float HearRange=45,FixedPpu=540/134f,MiniPpu=3.2f;
  static readonly Color DeaTone=new Color(.35f,.78f,1),OrgTone=new Color(1,.45f,.32f),EnemyTone=new Color(1,.22f,.2f),Cyan=new Color(.45f,.9f,1),Fire=new Color(1,.55f,.18f);
  MapWidget miniMap,bigMap,deployMap;GameObject deployScreen,mapScreen,miniRoot;
  Text alertText,scanPointer,miniNorth,deployClock,campLabel,grenadeLabel;Image deployBar,hurtFlash,grenadeArrow;RectTransform sweep;
  readonly List<EntryView> entryViews=new List<EntryView>();
  readonly List<(Vector3 at,float time)> shots=new List<(Vector3,float)>(),hits=new List<(Vector3,float)>();
  readonly int[] seenShots=new int[LanSession.MaxSlots];readonly Dictionary<int,int> seenVehicleShots=new Dictionary<int,int>();readonly Image[] chevrons=new Image[8],arcs=new Image[4];
  float lastHp=-1,hurtUntil,alertUntil;int lastScan,lastStrike;bool primed;string alert;Color alertColor;
  public bool DeployShown=>deployScreen&&deployScreen.activeInHierarchy;
  public bool MiniMapShown=>miniRoot&&miniRoot.activeInHierarchy;
  public bool BigMapShown=>mapScreen&&mapScreen.activeInHierarchy;
  public int ShotPings=>shots.Count;public int HitMarks=>hits.Count;public string Alert=>alertText&&alertText.gameObject.activeSelf?alertText.text:"";
  public int MiniIcons=>miniMap!=null?miniMap.Used:0;
  sealed class EntryView{public Button Card,Marker;public Image Back,Diamond;public Text Name,Info,Count,Tag,MarkerLabel;public Image[] Route;public Outline Edge;}

  // ---------- Construcción ----------
  void BuildMaps(){
   LanIcons.Build();
   var parent=hud.transform;
   hurtFlash=Panel(parent,"Daño",Vector2.zero,new Vector2(1600,1000)).GetComponent<Image>();hurtFlash.sprite=LanIcons.Vignette;hurtFlash.color=new Color(.9f,.05f,.03f,0);hurtFlash.raycastTarget=false;
   for(int i=0;i<chevrons.Length;i++)chevrons[i]=Icon(parent,"Aviso de disparo",LanIcons.Chevron,new Vector2(46,30),EnemyTone);
   for(int i=0;i<arcs.Length;i++)arcs[i]=Icon(parent,"Daño recibido",LanIcons.Arc,new Vector2(260,260),EnemyTone);
   grenadeArrow=Icon(parent,"Granada cerca",LanIcons.Chevron,new Vector2(58,38),new Color(1,.3f,.2f));
   grenadeLabel=Label(parent,"¡GRANADA!",Vector2.zero,new Vector2(200,30),20,new Color(1,.35f,.25f));grenadeLabel.fontStyle=FontStyle.Bold;
   alertText=Label(parent,"",new Vector2(0,232),new Vector2(900,40),26,Gold);alertText.fontStyle=FontStyle.Bold;var shadow=alertText.gameObject.AddComponent<Outline>();shadow.effectColor=new Color(0,0,0,.8f);shadow.effectDistance=new Vector2(2,-2);
   scanPointer=Label(parent,"",Vector2.zero,new Vector2(240,30),17,new Color(1,.4f,.3f));
   // Minimapa: arriba a la izquierda, gira con la cámara.
   miniRoot=Rect("Minimapa",parent,new Vector2(-532,252),new Vector2(196,196)).gameObject;
   miniMap=new MapWidget(this,miniRoot.transform,Vector2.zero,180,true,MiniPpu);
   var ring=Icon(miniRoot.transform,"Borde",LanIcons.Ring,new Vector2(196,196),new Color(1,.72f,.25f,.55f));
   miniNorth=Label(miniRoot.transform,"N",Vector2.zero,new Vector2(24,24),15,Gold);miniNorth.fontStyle=FontStyle.Bold;
   Label(miniRoot.transform,"M · MAPA",new Vector2(0,-110),new Vector2(160,20),12,new Color(.75f,.8f,.85f));
   // Mapa grande con M.
   mapScreen=Panel(parent,"Mapa grande",Vector2.zero,new Vector2(4000,4000)).gameObject;mapScreen.GetComponent<Image>().color=new Color(.01f,.02f,.04f,.96f);
   Label(mapScreen.transform,"MAPA DE OPERACIONES",new Vector2(0,322),new Vector2(600,36),26,Gold).fontStyle=FontStyle.Bold;
   bigMap=new MapWidget(this,mapScreen.transform,new Vector2(0,-8),600,false,600/134f);Frame(mapScreen.transform,new Vector2(0,-8),600);
   Label(mapScreen.transform,"<color=#FFFFFF>▲ Tú</color>      <color=#59C7FF>● Aliados</color>      <color=#FF3833>● Rivales vistos y disparos</color>      <color=#FF8C2E>● Fuego</color>      <color=#FF4D40>○ Bombardeo</color>      <color=#FF6A55>◊ Rastreo</color>          M · cerrar",new Vector2(0,-330),new Vector2(1100,24),15,new Color(.8f,.85f,.9f));
   BuildDeploy(parent);
  }
  void BuildDeploy(Transform parent){
   deployScreen=Panel(parent,"Plan de despliegue",Vector2.zero,new Vector2(4000,4000)).gameObject;deployScreen.GetComponent<Image>().color=new Color(.012f,.025f,.045f,1);
   var grid=new GameObject("Cuadricula",typeof(RectTransform)).AddComponent<RawImage>();grid.rectTransform.SetParent(deployScreen.transform,false);grid.rectTransform.sizeDelta=new Vector2(4000,4000);
   grid.texture=LanIcons.Grid;grid.uvRect=new Rect(0,0,4000/48f,4000/48f);grid.color=new Color(.4f,.75f,1,.05f);grid.raycastTarget=false;
   var page=Rect("Contenido",deployScreen.transform,Vector2.zero,new Vector2(1280,720));
   Label(page,"OPERACIÓN NOCHE EN EL CHAPARE  ·  DEA",new Vector2(-250,326),new Vector2(560,20),13,Gold).fontStyle=FontStyle.Bold;
   Label(page,"PLAN DE DESPLIEGUE",new Vector2(-250,298),new Vector2(560,40),31,Paper).fontStyle=FontStyle.Bold;
   deployMap=new MapWidget(this,page,new Vector2(-250,-22),540,false,FixedPpu);
   // Cuadrícula de coordenadas cada 20 unidades, con letras y números.
   for(int k=-3;k<=3;k++){
    float offset=k*20*FixedPpu;
    Line(deployMap.Root,new Vector2(offset,0),new Vector2(1,540),new Color(.45f,.85f,1,.13f));Line(deployMap.Root,new Vector2(0,offset),new Vector2(540,1),new Color(.45f,.85f,1,.13f));
    if(k<3){Label(page,((char)('A'+k+3)).ToString(),new Vector2(-250+offset+10*FixedPpu,256),new Vector2(30,16),12,new Color(.5f,.7f,.85f));Label(page,(4-k).ToString(),new Vector2(-532,-22+offset+10*FixedPpu),new Vector2(20,16),12,new Color(.5f,.7f,.85f));}
   }
   sweep=Icon(deployMap.Root,"Barrido",LanIcons.Sweep,new Vector2(820,820),new Color(.3f,.95f,.85f,.16f)).rectTransform;
   Frame(page,new Vector2(-250,-22),540);
   campLabel=Label(deployMap.Content,"CAMPAMENTO ENEMIGO",Vector2.zero,new Vector2(220,20),13,new Color(1,.45f,.38f,.9f));campLabel.fontStyle=FontStyle.Bold;
   // Columna derecha: reloj, puntos de entrada y ayuda.
   Label(page,"LA NOCHE EMPIEZA EN",new Vector2(330,312),new Vector2(440,20),14,Gold).alignment=TextAnchor.MiddleLeft;
   deployClock=Label(page,"",new Vector2(330,276),new Vector2(440,52),48,Paper);deployClock.alignment=TextAnchor.MiddleLeft;deployClock.fontStyle=FontStyle.Bold;
   var track=Panel(page,"Tiempo",new Vector2(330,238),new Vector2(440,6));track.GetComponent<Image>().color=new Color(.15f,.2f,.27f);
   deployBar=Panel(track,"Restante",Vector2.zero,new Vector2(440,6)).GetComponent<Image>();deployBar.sprite=fillSprite;deployBar.type=Image.Type.Filled;deployBar.fillMethod=Image.FillMethod.Horizontal;deployBar.color=Gold;
   Label(page,"PUNTO DE ENTRADA",new Vector2(330,206),new Vector2(440,22),15,Gold).alignment=TextAnchor.MiddleLeft;
   for(int i=0;i<4;i++){
    int point=i;var view=new EntryView();
    view.Card=Button(page,"",new Vector2(330,148-i*96),new Vector2(440,86),()=>session.ChoosePoint(point));view.Back=view.Card.GetComponent<Image>();view.Edge=view.Card.GetComponent<Outline>();
    view.Card.GetComponentInChildren<Text>().text="";
    view.Name=Label(view.Card.transform,"",new Vector2(-62,20),new Vector2(290,30),23,Paper);view.Name.alignment=TextAnchor.MiddleLeft;view.Name.fontStyle=FontStyle.Bold;
    view.Info=Label(view.Card.transform,"",new Vector2(-30,-6),new Vector2(354,22),14,new Color(.75f,.8f,.86f));view.Info.alignment=TextAnchor.MiddleLeft;
    view.Count=Label(view.Card.transform,"",new Vector2(-30,-28),new Vector2(354,20),13,Cyan);view.Count.alignment=TextAnchor.MiddleLeft;
    view.Tag=Label(view.Card.transform,"",new Vector2(150,20),new Vector2(120,22),13,Gold);view.Tag.alignment=TextAnchor.MiddleRight;view.Tag.fontStyle=FontStyle.Bold;
    // Marcador en el mapa: rombo que se puede pulsar, con nombre y ruta de inserción.
    view.Route=new Image[10];for(int k=0;k<view.Route.Length;k++)view.Route[k]=Icon(deployMap.Content,"Ruta",LanIcons.Circle,new Vector2(6,6),Gold);
    var marker=Icon(deployMap.Content,"Entrada",LanIcons.Diamond,new Vector2(30,30),Cyan);marker.raycastTarget=true;view.Diamond=marker;
    view.Marker=marker.gameObject.AddComponent<Button>();view.Marker.onClick.AddListener(()=>session.ChoosePoint(point));
    view.MarkerLabel=Label(marker.transform,"",new Vector2(0,-27),new Vector2(160,22),15,Paper);view.MarkerLabel.fontStyle=FontStyle.Bold;var outline=view.MarkerLabel.gameObject.AddComponent<Outline>();outline.effectColor=new Color(0,0,0,.85f);
    entryViews.Add(view);
   }
   Label(page,"Clic en el mapa o en una tarjeta para elegir.\nTu equipo ve tu elección al instante y entra contigo al empezar la noche.",new Vector2(330,-262),new Vector2(440,44),13,new Color(.65f,.72f,.8f)).alignment=TextAnchor.MiddleLeft;
  }
  Image Icon(Transform parent,string name,Sprite sprite,Vector2 size,Color color){var image=new GameObject(name,typeof(RectTransform)).AddComponent<Image>();image.rectTransform.SetParent(parent,false);image.sprite=sprite;image.color=color;image.rectTransform.sizeDelta=size;image.raycastTarget=false;return image;}
  void Line(Transform parent,Vector2 at,Vector2 size,Color color){var line=Icon(parent,"Linea",null,size,color);line.rectTransform.anchoredPosition=at;}
  // Esquinas ámbar alrededor de un mapa.
  void Frame(Transform parent,Vector2 center,float size){
   float h=size*.5f+6,len=34;
   foreach(var (sx,sy) in new[]{(-1,1),(1,1),(-1,-1),(1,-1)}){
    var corner=center+new Vector2(sx*h,sy*h);
    Line(parent,corner-new Vector2(sx*len*.5f,0),new Vector2(len,3),Gold);Line(parent,corner-new Vector2(0,sy*len*.5f),new Vector2(3,len),Gold);
   }
  }

  // ---------- Cada cuadro ----------
  void UpdateMaps(LanPawn me){
   var world=session.World;bool deploying=session.Night.Deploying&&me.Team==LanNightRules.Dea;
   deployScreen.SetActive(deploying);
   bool big=session.MapOpen&&!deploying;mapScreen.SetActive(big);miniRoot.SetActive(!deploying&&!big);
   TrackCombat(me);
   if(deploying){UpdateDeploy(me);HideWarnings();return;}
   if(big){bigMap.Begin(world,Vector3.zero,0);DrawWorld(bigMap,me);bigMap.End();HideWarnings();return;}
   else{
    float yaw=Camera.main?Camera.main.transform.eulerAngles.y:0;
    miniMap.Begin(world,me.transform.position,yaw);DrawWorld(miniMap,me);miniMap.End();
    var north=Quaternion.Euler(0,0,yaw)*Vector2.up*104;miniNorth.rectTransform.anchoredPosition=north;
   }
   UpdateWarnings(me);
  }
  void UpdateDeploy(LanPawn me){
   var world=session.World;var night=session.Night;var mine=session.Slots[session.LocalSlot];float now=Time.unscaledTime;
   int time=Mathf.CeilToInt(night.Deploy);deployClock.text=$"{time/60:00}:{time%60:00}";deployBar.fillAmount=night.Deploy/LanNightRules.DeploySeconds;
   sweep.localEulerAngles=new Vector3(0,0,-now*55%360);
   deployMap.Begin(world,Vector3.zero,0);
   if(world.orgSpawns!=null&&world.orgSpawns.Length>0){var camp=world.orgSpawns.Aggregate(Vector3.zero,(a,t)=>a+t.position)/world.orgSpawns.Length;campLabel.rectTransform.anchoredPosition=new Vector2(camp.x,camp.z)*FixedPpu;deployMap.Icon(LanIcons.Ring,camp,new Color(1,.35f,.3f,.55f),46);}
   var entries=world.deaEntries;
   for(int i=0;i<entryViews.Count;i++){
    var view=entryViews[i];bool used=i<entries.Length;view.Card.gameObject.SetActive(used);view.Diamond.gameObject.SetActive(used);
    foreach(var dot in view.Route)dot.gameObject.SetActive(false);
    if(!used)continue;
    var entry=entries[i];var agents=session.Slots.Where(s=>s.team==LanNightRules.Dea&&s.point==i).ToList();bool selected=mine.point==i;
    var at=new Vector2(entry.position.x,entry.position.z)*FixedPpu;
    float pulse=1+(selected?.18f:.08f)*Mathf.Sin(now*5+i);
    view.Diamond.rectTransform.anchoredPosition=at;view.Diamond.rectTransform.localScale=Vector3.one*pulse;view.Diamond.color=selected?Gold:agents.Count>0?Cyan:new Color(.45f,.9f,1,.55f);
    view.MarkerLabel.text=entry.name.ToUpper();view.MarkerLabel.color=selected?Gold:Paper;
    // Ruta del helicóptero desde el borde del mapa hasta el punto.
    if(selected||agents.Count>0){
     var outward=at.normalized;var start=outward*270;if(Vector2.Dot(at,outward)>250)start=at+outward*40;
     for(int k=0;k<view.Route.Length;k++){float t=(k+Mathf.Repeat(now*1.4f,1))/view.Route.Length;var dot=view.Route[k];dot.gameObject.SetActive(true);dot.rectTransform.anchoredPosition=Vector2.Lerp(start,at,t);dot.color=selected?new Color(1,.72f,.25f,.35f+.65f*t):new Color(.45f,.9f,1,.25f+.35f*t);}
    }
    // Fichas de los agentes que entran por aquí.
    for(int k=0;k<agents.Count;k++){var chipAt=entry.position+new Vector3((k-(agents.Count-1)*.5f)*3.6f,0,6.5f);deployMap.Icon(LanIcons.Circle,chipAt,agents[k].id==mine.id?Gold:DeaTone,15);deployMap.Label(Initial(agents[k].name),chipAt,new Color(.02f,.04f,.06f),11,Vector2.zero);}
    view.Name.text=entry.name.ToUpper();view.Info.text=EntryInfo(entry.name);
    view.Count.text=agents.Count==0?"Nadie entra por aquí":$"{agents.Count} {(agents.Count==1?"AGENTE":"AGENTES")} · "+string.Join(", ",agents.Select(a=>a.id==mine.id?"tú":a.name));
    view.Tag.text=selected?"✓ TU ENTRADA":"";
    view.Back.color=selected?new Color(1,.85f,.6f):Color.white;view.Edge.effectColor=selected?Gold:new Color(1,.72f,.25f,.25f);view.Edge.effectDistance=selected?new Vector2(3,-3):new Vector2(1.5f,-1.5f);
   }
   deployMap.End();
  }
  static string Initial(string name)=>string.IsNullOrEmpty(name)?"?":name.Substring(0,1).ToUpper();
  static string EntryInfo(string name){
   switch(name){
    case "Río":return "Cruce por el agua al oeste · cubierto por la selva";
    case "Camino":return "Entrada rápida por el sur · directo al campamento";
    case "Pista":return "Pista clandestina al este · terreno abierto";
    case "Selva":return "Norte selvático · lento pero discreto";
    default:return "Punto de entrada";
   }
  }
  static float Heading(Vector3 direction)=>Mathf.Atan2(-direction.x,direction.z)*Mathf.Rad2Deg;
  static float Flat(Vector3 v){v.y=0;return v.magnitude;}
  // Lo que ve un mapa: marcas, disparos oídos, aliados, rivales a la vista y uno mismo encima.
  void DrawWorld(MapWidget map,LanPawn me){
   var world=session.World;int team=me.Team;float now=Time.unscaledTime;bool blink=Mathf.Repeat(now,.6f)<.3f;
   foreach(var mark in world.Marks){
    switch(mark.kind){
     case LanItems.FireMark:map.Icon(LanIcons.Soft,mark.position,new Color(1,.5f,.15f,.85f),Mathf.Max(10,(mark.size+1)*2*map.Ppu));break;
     case LanItems.FlareMark:map.Icon(LanIcons.Soft,mark.position,new Color(1,.6f,.3f,.45f),LanNightRules.FlareRadius*1.2f*map.Ppu);break;
     case LanItems.StrikeMark:{float pulse=.5f+.5f*Mathf.Sin(now*(mark.fired?16:7));float size=(mark.size+LanExplosives.ShellRadius*.5f)*2*map.Ppu;map.Icon(LanIcons.Circle,mark.position,new Color(1,.15f,.1f,.18f+.12f*pulse),size);map.Icon(LanIcons.Ring,mark.position,new Color(1,.2f,.15f,.5f+.5f*pulse),size);break;}
     case LanItems.WallMark:if(!mark.fired)map.Icon(LanIcons.Square,mark.position,mark.team==team?new Color(.85f,.8f,.6f):new Color(.7f,.6f,.5f),new Vector2(LanAbilities.WallWidth*map.Ppu,Mathf.Max(3,LanAbilities.WallDepth*map.Ppu)),Heading(mark.from));break;
     case LanItems.GrenadeMark:if(Flat(mark.position-me.transform.position)<24)map.Icon(LanIcons.Circle,mark.position,new Color(1,.25f,.2f,blink?1:.4f),7);break;
     case LanItems.ScanMark:{float wave=Mathf.Repeat(now*.9f,1);map.Icon(LanIcons.Ring,mark.position,new Color(1,.3f,.2f,1-wave),14+wave*30);map.Icon(LanIcons.Diamond,mark.position,new Color(1,.4f,.3f),13);map.Label(team==mark.team?"LÍDER":"UBICADO",mark.position,new Color(1,.5f,.4f),11,new Vector2(0,-15));break;}
     case LanItems.MedStationMark:case LanItems.CampfireMark:if(mark.team==team&&!mark.fired)map.Icon(LanIcons.Ring,mark.position,mark.kind==LanItems.MedStationMark?new Color(.35f,1,.5f,.8f):new Color(1,.6f,.25f,.8f),mark.size*2*map.Ppu);if(mark.kind==LanItems.CampfireMark&&!mark.fired)map.Icon(LanIcons.Soft,mark.position,new Color(1,.55f,.2f,.7f),12);break;
     case LanItems.DroneStationMark:if(mark.team==team&&!mark.fired)map.Icon(LanIcons.Square,mark.position,new Color(.5f,.85f,1),9);break;
     case LanItems.HeliMark:map.Icon(LanIcons.Diamond,LanWorld.HeliPosition(mark,mark.age),mark.team==team?new Color(.5f,.85f,1):EnemyTone,14);break;
     case LanItems.ChutoMark:map.Icon(LanIcons.Square,mark.position,mark.team==team?new Color(1,.62f,.3f):EnemyTone,new Vector2(7,11),Heading(mark.from));break;
     case LanItems.SmokeMark:map.Icon(LanIcons.Soft,mark.position,new Color(.7f,.72f,.75f,.75f),mark.size*2.4f*map.Ppu);break;
     case LanItems.DroneMark:if(mark.team==team){map.Icon(LanIcons.Ring,mark.position,new Color(.4f,.85f,1,.8f),mark.size*2*map.Ppu);map.Icon(LanIcons.Diamond,mark.position,new Color(.4f,.85f,1),9);}break;
     case LanItems.ShieldMark:if(mark.team==team)map.Icon(LanIcons.Ring,mark.position,new Color(.4f,.85f,1,.8f),mark.size*2*map.Ppu);break;
     case LanItems.AlarmMark:case LanItems.TrapMark:if(mark.team==team)map.Icon(mark.kind==LanItems.AlarmMark?LanIcons.Diamond:LanIcons.Circle,mark.position,mark.fired&&blink?Color.red:LanItems.Colors[LanItems.Alarm],7);break;
    }
   }
   foreach(var npc in world.Npcs)if(npc.team==team)map.Icon(npc.kind==LanWorld.SwarmDrone?LanIcons.Diamond:LanIcons.Circle,npc.position,npc.kind==LanWorld.Kamikaze?new Color(1,.25f,.15f):team==LanNightRules.Dea?new Color(.5f,.85f,1):new Color(1,.62f,.3f),npc.kind==LanWorld.Kamikaze?7:5);
   foreach(var v in world.Vehicles)if(v.respawn<=0&&(v.team==team||v.driver>=0))map.Icon(LanIcons.Square,v.position,v.team==team?DeaTone:EnemyTone,new Vector2(7,11),-v.yaw);
   foreach(var mark in world.Marks)if(mark.kind==LanItems.SupplyMark)map.Icon(LanIcons.Diamond,mark.position,new Color(.5f,1,.5f,blink?1:.5f),9);
   foreach(var shot in shots){float age=(now-shot.time)/1.6f;if(age>=1)continue;map.Icon(LanIcons.Ring,shot.at,new Color(1,.22f,.18f,1-age),9+age*20);map.Icon(LanIcons.Circle,shot.at,new Color(1,.25f,.2f,1-age),6);}
   foreach(var p in world.Pawns.Values){
    if(p==me)continue;
    if(p.Team==team){
     if(p.IsLeader){map.Icon(LanIcons.Diamond,p.transform.position,p.State.down>0&&blink?EnemyTone:Gold,14);continue;}
     if(p.State.hp<=0)continue;
     map.Icon(LanIcons.Circle,p.transform.position,team==LanNightRules.Dea?DeaTone:OrgTone,10);
    }else if(p.State.hp>0&&world.Shows(p))map.Icon(p.State.marked>0?LanIcons.Diamond:LanIcons.Circle,p.transform.position,EnemyTone,p.State.marked>0?12:9);
   }
   if(me.State.hp>0||me.State.down>0)map.Icon(LanIcons.Arrow,me.transform.position,Color.white,16,Heading(me.State.aim));
  }
  // Disparos rivales oídos (minimapa y flechas en el borde) y daño recibido (arcos alrededor del centro).
  void TrackCombat(LanPawn me){
   var world=session.World;float now=Time.unscaledTime;var mine=me.transform.position;
   foreach(var p in world.Pawns.Values){
    int shot=p.State.shot;if(shot==seenShots[p.Id])continue;seenShots[p.Id]=shot;
    if(!primed||p.Team==me.Team)continue;
    if(Flat(p.transform.position-mine)<=HearRange)shots.Add((p.transform.position,now));
    if(Flat(p.State.hit-mine)<.9f)AddHit(p.transform.position,now);
   }
   foreach(var v in world.Vehicles){
    seenVehicleShots.TryGetValue(v.id,out int seen);if(v.shot==seen)continue;seenVehicleShots[v.id]=v.shot;
    if(!primed||v.team==me.Team)continue;
    if(Flat(v.position-mine)<=HearRange)shots.Add((v.position,now));
    if(Flat(v.hit-mine)<.9f)AddHit(v.position,now);
   }
   if(primed&&lastHp>=0&&me.State.hp<lastHp-.5f){
    hurtUntil=now+.45f;
    if(!hits.Any(h=>now-h.time<.2f)){
     var source=world.Marks.Where(m=>m.kind==LanItems.ExplosionMark||m.kind==LanItems.FireMark).OrderBy(m=>Flat(m.position-mine)).FirstOrDefault();
     if(source!=null)AddHit(source.position,now);
     else{var npc=world.Npcs.OrderBy(n=>Flat(n.position-mine)).FirstOrDefault();if(npc!=null&&Flat(npc.position-mine)<3)AddHit(npc.position,now);}
    }
   }
   lastHp=me.State.hp;primed=true;
   shots.RemoveAll(s=>now-s.time>1.6f);hits.RemoveAll(h=>now-h.time>1.2f);
  }
  void AddHit(Vector3 at,float now){if(hits.Any(h=>now-h.time<.3f&&Flat(h.at-at)<2))return;hits.Add((at,now));}
  void HideWarnings(){foreach(var c in chevrons)c.gameObject.SetActive(false);foreach(var a in arcs)a.gameObject.SetActive(false);grenadeArrow.gameObject.SetActive(false);grenadeLabel.gameObject.SetActive(false);alertText.gameObject.SetActive(false);scanPointer.gameObject.SetActive(false);hurtFlash.color=new Color(.9f,.05f,.03f,0);}
  void UpdateWarnings(LanPawn me){
   var world=session.World;var camera=Camera.main;float now=Time.unscaledTime;var mine=me.transform.position;
   if(!camera){HideWarnings();return;}
   Vector2 Toward(Vector3 at){var a=camera.WorldToViewportPoint(mine);var b=camera.WorldToViewportPoint(at);var d=new Vector2((b.x-a.x)*camera.aspect,b.y-a.y);return d.sqrMagnitude<1e-6f?Vector2.up:d.normalized;}
   bool OnScreen(Vector3 at){var v=camera.WorldToViewportPoint(at);return v.z>0&&v.x>.04f&&v.x<.96f&&v.y>.06f&&v.y<.94f;}
   // Flechas rojas hacia disparos fuera de pantalla.
   int used=0;
   foreach(var shot in shots){
    if(used>=chevrons.Length)break;float age=(now-shot.time)/1.2f;if(age>=1||OnScreen(shot.at))continue;
    var d=Toward(shot.at);var c=chevrons[used++];c.gameObject.SetActive(true);c.rectTransform.anchoredPosition=new Vector2(d.x*300,d.y*215);
    c.rectTransform.localEulerAngles=new Vector3(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg-90);c.color=new Color(1,.2f,.15f,(1-age)*.95f);
   }
   for(int i=used;i<chevrons.Length;i++)chevrons[i].gameObject.SetActive(false);
   // Arcos de daño: apuntan hacia quien te dio.
   used=0;
   foreach(var hit in hits){
    if(used>=arcs.Length)break;float age=(now-hit.time)/1.2f;if(age>=1)continue;
    var d=Toward(hit.at);var a=arcs[used++];a.gameObject.SetActive(true);a.rectTransform.anchoredPosition=Vector2.zero;
    a.rectTransform.localEulerAngles=new Vector3(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg-90);a.color=new Color(1,.12f,.08f,(1-age)*.85f);
   }
   for(int i=used;i<arcs.Length;i++)arcs[i].gameObject.SetActive(false);
   hurtFlash.color=new Color(.9f,.05f,.03f,Mathf.Clamp01((hurtUntil-now)/.45f)*.45f);
   // Granada rival cerca: flecha y aviso.
   LanMark grenade=null;float closest=LanExplosives.GrenadeRadius+1.5f;
   foreach(var mark in world.Marks)if(mark.kind==LanItems.GrenadeMark&&mark.team!=me.Team){float d=Flat(mark.position-mine);if(d<closest){closest=d;grenade=mark;}}
   bool warn=grenade!=null&&me.State.hp>0;grenadeArrow.gameObject.SetActive(warn);grenadeLabel.gameObject.SetActive(warn);
   if(warn){var d=Toward(grenade.position);grenadeArrow.rectTransform.anchoredPosition=d*120;grenadeArrow.rectTransform.localEulerAngles=new Vector3(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg-90);grenadeLabel.rectTransform.anchoredPosition=d*120+new Vector2(0,-34);float blink=Mathf.Repeat(now*4,1)<.5f?1:.45f;grenadeArrow.color=new Color(1,.3f,.2f,blink);}
   // Avisos centrales: rastreo y bombardeo.
   var scan=world.LastScan;
   if(scan!=null&&scan.id!=lastScan){lastScan=scan.id;if(me.Team==scan.team)Say("RASTREO · LÍDER UBICADO",Cyan,4);else if(me.IsLeader)Say("¡TE UBICARON! CAMBIA DE ESCONDITE",EnemyTone,5);else Say("¡RASTREARON A TU LÍDER!",EnemyTone,4);}
   var strike=world.Marks.LastOrDefault(m=>m.kind==LanItems.StrikeMark);
   if(strike!=null&&strike.id!=lastStrike){lastStrike=strike.id;if(strike.team==me.Team)Say("BOMBARDEO EN CAMINO",Gold,3);}
   bool danger=me.State.hp>0&&world.Marks.Any(m=>m.kind==LanItems.StrikeMark&&Flat(m.position-mine)<=m.size+LanExplosives.ShellRadius+1);
   if(danger){alertText.gameObject.SetActive(true);alertText.text="¡BOMBARDEO! SAL DE LA ZONA";alertText.color=Mathf.Repeat(now*3,1)<.5f?EnemyTone:Gold;}
   else if(now<alertUntil){alertText.gameObject.SetActive(true);alertText.text=alert;alertText.color=alertColor;}
   else alertText.gameObject.SetActive(false);
   // La DEA ve hacia dónde quedó el rastreo aunque esté fuera de pantalla.
   bool tracked=scan!=null&&scan.team==me.Team;
   Point(scanPointer,tracked?scan.position:(Vector3?)null,"LÍDER RASTREADO",true);
  }
  void Say(string text,Color color,float seconds){alert=text;alertColor=color;alertUntil=Time.unscaledTime+seconds;}

  // Mapa reutilizable: fijo (despliegue, mapa grande) o centrado en el jugador y girando con la cámara.
  sealed class MapWidget {
   public readonly RectTransform Root,Rotator,Content;public readonly RawImage Image;public readonly float Ppu;
   readonly List<Image> icons=new List<Image>();readonly List<Text> labels=new List<Text>();readonly LanUI ui;int usedIcons,usedLabels;float yaw;
   public int Used=>usedIcons;
   public MapWidget(LanUI owner,Transform parent,Vector2 position,float size,bool round,float ppu){
    ui=owner;Ppu=ppu;Root=ui.Rect("Mapa",parent,position,new Vector2(size,size));
    var back=Root.gameObject.AddComponent<Image>();back.sprite=round?LanIcons.Circle:null;back.color=new Color(.02f,.04f,.065f,.94f);back.raycastTarget=false;
    if(round)Root.gameObject.AddComponent<Mask>().showMaskGraphic=true;else Root.gameObject.AddComponent<RectMask2D>();
    Rotator=ui.Rect("Giro",Root,Vector2.zero,Vector2.zero);Content=ui.Rect("Contenido",Rotator,Vector2.zero,Vector2.zero);
    Image=new GameObject("Imagen del mapa",typeof(RectTransform)).AddComponent<RawImage>();Image.rectTransform.SetParent(Content,false);Image.raycastTarget=false;
   }
   public void Begin(LanWorld world,Vector3 center,float rotation){
    yaw=rotation;Rotator.localEulerAngles=new Vector3(0,0,yaw);Content.anchoredPosition=-new Vector2(center.x,center.z)*Ppu;
    if(Image.texture!=world.mapTexture)Image.texture=world.mapTexture;Image.enabled=world.mapTexture;
    Image.rectTransform.anchoredPosition=new Vector2(world.mapCenter.x,world.mapCenter.z)*Ppu;Image.rectTransform.sizeDelta=Vector2.one*world.mapSize*Ppu;
    usedIcons=usedLabels=0;
   }
   public Image Icon(Sprite sprite,Vector3 at,Color color,float size,float? heading=null)=>Icon(sprite,at,color,new Vector2(size,size),heading);
   public Image Icon(Sprite sprite,Vector3 at,Color color,Vector2 size,float? heading=null){
    if(usedIcons==icons.Count){var made=new GameObject("Icono",typeof(RectTransform)).AddComponent<Image>();made.rectTransform.SetParent(Content,false);made.raycastTarget=false;icons.Add(made);}
    var icon=icons[usedIcons++];icon.gameObject.SetActive(true);icon.sprite=sprite;icon.color=color;icon.rectTransform.sizeDelta=size;
    icon.rectTransform.anchoredPosition=new Vector2(at.x,at.z)*Ppu;icon.rectTransform.localEulerAngles=new Vector3(0,0,heading??-yaw);icon.rectTransform.SetAsLastSibling();
    return icon;
   }
   public Text Label(string text,Vector3 at,Color color,int size,Vector2 offset){
    if(usedLabels==labels.Count){var made=ui.Label(Content,"",Vector2.zero,new Vector2(120,18),size);made.fontStyle=FontStyle.Bold;labels.Add(made);}
    var label=labels[usedLabels++];label.gameObject.SetActive(true);label.text=text;label.color=color;label.fontSize=size;
    label.rectTransform.anchoredPosition=new Vector2(at.x,at.z)*Ppu+(Vector2)(Quaternion.Euler(0,0,-yaw)*offset);label.rectTransform.localEulerAngles=new Vector3(0,0,-yaw);label.rectTransform.SetAsLastSibling();
    return label;
   }
   public void End(){for(int i=usedIcons;i<icons.Count;i++)icons[i].gameObject.SetActive(false);for(int i=usedLabels;i<labels.Count;i++)labels[i].gameObject.SetActive(false);}
  }
 }
}
