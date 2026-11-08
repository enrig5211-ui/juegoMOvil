using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace Valle.Lan {
 // Pantalla de selección arcade: tres retratos del propio bando, compañeros en vivo y confirmación con pose.
 public sealed partial class LanUI {
  GameObject select;Text selectTitle,selectClock,selectHint,selectRival,lockLabel;Button lockButton;Image flash;AudioSource sfx;
  const int Cards=LanClass.Roles;
  readonly GameObject[] cards=new GameObject[Cards];readonly Image[] frames=new Image[Cards];readonly RawImage[] portraits=new RawImage[Cards];
  readonly Text[] cardNames=new Text[Cards],cardInfo=new Text[Cards],cardTags=new Text[Cards];
  LanPortraits studio;bool wasLocked;int lastRole=-1;
  void BuildSelect(){
   select=Screen("Selección");
   selectTitle=Label(select.transform,"",new Vector2(0,300),new Vector2(900,60),40,Gold);selectTitle.font=body;selectTitle.fontStyle=FontStyle.Bold;
   selectClock=Label(select.transform,"",new Vector2(540,300),new Vector2(140,70),50,Gold);selectClock.font=body;selectClock.fontStyle=FontStyle.Bold;
   selectRival=Label(select.transform,"",new Vector2(0,258),new Vector2(900,30),17);
   for(int i=0;i<Cards;i++){
    int index=i;var card=Panel(select.transform,"Personaje "+i,new Vector2(-300+i%3*300,128-i/3*254),new Vector2(280,240));
    cards[i]=card.gameObject;frames[i]=card.GetComponent<Image>();
    var portrait=new GameObject("Retrato",typeof(RectTransform),typeof(RawImage)).GetComponent<RawImage>();
    portrait.rectTransform.SetParent(card,false);portrait.rectTransform.sizeDelta=new Vector2(128,150);portrait.rectTransform.anchoredPosition=new Vector2(-62,22);portraits[i]=portrait;
    cardNames[i]=Label(card,"",new Vector2(64,72),new Vector2(160,52),15,Gold);cardNames[i].font=body;cardNames[i].fontStyle=FontStyle.Bold;
    cardInfo[i]=Label(card,"",new Vector2(64,4),new Vector2(150,80),14);
    cardTags[i]=Label(card,"",new Vector2(0,-100),new Vector2(260,36),14,new Color(.45f,.85f,1));
    var click=card.gameObject.AddComponent<Button>();click.transition=Selectable.Transition.None;click.onClick.AddListener(()=>session.ChooseClass(index));
   }
   lockButton=Button(select.transform,"¡LISTO!",new Vector2(0,-268),new Vector2(320,50),()=>session.LockCharacter());lockLabel=lockButton.GetComponentInChildren<Text>();
   selectHint=Label(select.transform,"← →  o clic para elegir   ·   ENTER para confirmar",new Vector2(0,-310),new Vector2(900,30),17);
   flash=Panel(select.transform,"Destello",Vector2.zero,new Vector2(4000,4000)).GetComponent<Image>();flash.color=new Color(1,1,1,0);flash.raycastTarget=false;
   sfx=gameObject.AddComponent<AudioSource>();sfx.playOnAwake=false;sfx.spatialBlend=0;
  }
  void UpdateSelect(){
   bool active=session.Phase=="select"&&session.Slots!=null&&session.LocalSlot>=0;
   select.SetActive(active);
   if(!active){if(studio){Destroy(studio.gameObject);studio=null;}wasLocked=false;lastRole=-1;return;}
   var mine=session.Slots[session.LocalSlot];int team=mine.team;bool leader=mine.id==session.Night.Leader;
   if(!studio||studio.Team!=team||studio.Leader!=leader){if(studio)Destroy(studio.gameObject);studio=new GameObject("Retratos de selección").AddComponent<LanPortraits>();studio.Build(art,team,leader);}
   var keyboard=Keyboard.current;
   if(keyboard!=null&&!leader&&!mine.locked){
    if(keyboard.leftArrowKey.wasPressedThisFrame||keyboard.aKey.wasPressedThisFrame)session.ChooseClass((mine.role+Cards-1)%Cards);
    if(keyboard.rightArrowKey.wasPressedThisFrame||keyboard.dKey.wasPressedThisFrame)session.ChooseClass((mine.role+1)%Cards);
    if(keyboard.upArrowKey.wasPressedThisFrame||keyboard.wKey.wasPressedThisFrame||keyboard.downArrowKey.wasPressedThisFrame||keyboard.sKey.wasPressedThisFrame)session.ChooseClass((mine.role+3)%Cards);
    if(keyboard.enterKey.wasPressedThisFrame||keyboard.spaceKey.wasPressedThisFrame)session.LockCharacter();
   }
   selectTitle.text=leader?"ERES EL LÍDER":"¡ELIGE TU PERSONAJE!";
   selectClock.text=Mathf.CeilToInt(session.Remaining).ToString();
   int rivals=session.Slots.Count(s=>s.team!=team),rivalsReady=session.Slots.Count(s=>s.team!=team&&s.locked);
   selectRival.text=$"{(team==LanNightRules.Dea?"La organización":"La DEA")} está eligiendo…   {rivalsReady}/{rivals} listos";
   for(int i=0;i<Cards;i++){
    bool visible=!leader||i==1;cards[i].SetActive(visible);if(!visible)continue;
    int model=leader?0:i;portraits[i].texture=studio.Texture(model);
    var cls=leader?LanClass.Leader:LanClass.For(team,i);
    cardNames[i].text=cls.Name.ToUpper();
    int cut=cls.Description.IndexOf(" · ");cardInfo[i].text=cut>0?cls.Description.Substring(0,cut):cls.Description;
    var mates=session.Slots.Where(s=>s.team==team&&s.id!=mine.id&&s.id!=session.Night.Leader&&s.role==i).Select(s=>s.name+(s.locked?" (listo)":""));
    cardTags[i].text=leader?"Sobrevive hasta el amanecer":string.Join("   ",mates);
    bool hovered=leader||mine.role==i,confirmed=hovered&&(leader||mine.locked);
    frames[i].color=confirmed?new Color(.95f,.75f,.2f):hovered?new Color(.5f,.42f,.16f):Ink;
    studio.SetState(model,hovered,confirmed);
   }
   lockButton.gameObject.SetActive(!leader);lockButton.interactable=!mine.locked;lockLabel.text=mine.locked?"ESPERANDO A LOS DEMÁS…":"¡LISTO!";
   selectHint.gameObject.SetActive(!leader&&!mine.locked);
   if(lastRole>=0&&mine.role!=lastRole&&art.selectMove)sfx.PlayOneShot(art.selectMove,.5f);lastRole=mine.role;
   if(mine.locked&&!wasLocked){flash.color=new Color(1,1,1,.85f);var confirm=art.selectConfirm?art.selectConfirm:art.equip;if(confirm)sfx.PlayOneShot(confirm,.7f);}
   wasLocked=mine.locked;
   flash.color=new Color(1,1,1,Mathf.MoveTowards(flash.color.a,0,Time.unscaledDeltaTime*2.5f));
  }
 }
}
