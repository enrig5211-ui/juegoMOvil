using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
namespace Valle {
 // Scene-authored UI: references remain editable in the Inspector.
 public sealed class ValleInterface : MonoBehaviour {
  public bool mainMenu;
  public PlayerStats stats; public PlayerWeapon weapon;
  public Image healthFill,staminaFill;
  public Text healthText,staminaText,defenseText,weaponText,ammoText,promptText;
  public GameObject hud,weaponPanel,promptPanel,pausePanel,controlsPanel,deathPanel;
  public Button playButton,controlsButton,quitButton,resumeButton,menuButton,backButton,respawnButton;
  public static bool BlocksInput {get;private set;}
  bool loading; GameObject returnSelection;
  void Awake(){BlocksInput=false;Time.timeScale=1;
   if(playButton)playButton.onClick.AddListener(()=>Load("ValleDelRio"));
   if(controlsButton)controlsButton.onClick.AddListener(ShowControls);
   if(quitButton)quitButton.onClick.AddListener(Quit);
   if(resumeButton)resumeButton.onClick.AddListener(()=>Pause(false));
   if(menuButton)menuButton.onClick.AddListener(()=>Load("MenuInicio"));
   if(backButton)backButton.onClick.AddListener(HideControls);
   if(respawnButton)respawnButton.onClick.AddListener(()=>{stats.GetComponent<PlayerMotor>().Respawn();});
  }
  void Start(){if(mainMenu&&playButton)playButton.Select();}
  void Update(){
   if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame&&!loading){if(controlsPanel.activeSelf)HideControls();else if(!mainMenu&&!stats.IsDead)Pause(!pausePanel.activeSelf);}
   if(mainMenu||!stats)return;
   bool dead=stats.IsDead;deathPanel.SetActive(dead);hud.SetActive(!dead&&!BlocksInput);
   healthFill.fillAmount=Mathf.MoveTowards(healthFill.fillAmount,stats.Health/Mathf.Max(1,stats.maxHealth),Time.unscaledDeltaTime*2);
   staminaFill.fillAmount=stats.Stamina/Mathf.Max(1,stats.maxStamina);
   healthText.text=$"VIDA    {Mathf.CeilToInt(stats.Health)} / {stats.maxHealth:0}";
   staminaText.text=$"ESTAMINA    {Mathf.CeilToInt(stats.Stamina)} / {stats.maxStamina:0}";
   defenseText.text=$"DEFENSA  {stats.defense:0}";
   var held=weapon?weapon.Held:null;weaponPanel.SetActive(held!=null);
   if(held){weaponText.text=held.displayName.ToUpperInvariant();ammoText.text=weapon.Reloading?"Recargando…":$"<size=32>{held.ammo:00}</size>  /  {held.reserve:000}";}
   var near=weapon?weapon.Nearby:null;promptPanel.SetActive(near!=null&&!dead&&!BlocksInput);
   if(near)promptText.text=$"{(held?"Cambiar por":"Recoger")} {near.displayName}";
  }
  public void Pause(bool paused){BlocksInput=paused;Time.timeScale=paused?0:1;pausePanel.SetActive(paused);controlsPanel.SetActive(false);if(paused)resumeButton.Select();else if(EventSystem.current)EventSystem.current.SetSelectedGameObject(null);}
  public void ShowControls(){returnSelection=EventSystem.current?EventSystem.current.currentSelectedGameObject:null;controlsPanel.SetActive(true);backButton.Select();}
  public void HideControls(){controlsPanel.SetActive(false);if(EventSystem.current)EventSystem.current.SetSelectedGameObject(returnSelection);}
  public void Load(string scene){if(loading)return;loading=true;BlocksInput=true;Time.timeScale=1;if(playButton)playButton.interactable=false;SceneManager.LoadSceneAsync(scene);}
  void OnDestroy(){BlocksInput=false;Time.timeScale=1;}
  void Quit(){
#if UNITY_EDITOR
   UnityEditor.EditorApplication.isPlaying=false;
#else
   Application.Quit();
#endif
  }
 }
}
