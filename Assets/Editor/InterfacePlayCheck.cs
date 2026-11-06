using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using Valle;
[InitializeOnLoad]
public static class InterfacePlayCheck {
 static int stage;static double next;static StringBuilder report=new StringBuilder();
 static InterfacePlayCheck(){if(File.Exists("Temp/ui-test.request")&&!EditorApplication.isPlayingOrWillChangePlaymode)SessionState.SetBool("ValleUITest",false);EditorApplication.update+=Tick;}
 [MenuItem("Tools/Interfaz/Comprobar en Play")]
 static void Request(){if(EditorApplication.isPlayingOrWillChangePlaymode)return;if(!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;stage=0;report.Clear();SessionState.SetBool("ValleUITest",false);UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MenuInicio.unity");File.WriteAllText("Temp/ui-test.request","");}
 static void Check(bool value,string text){report.AppendLine((value?"PASS: ":"FAIL: ")+text);}
 static void Tick(){
  if(!SessionState.GetBool("ValleUITest",false)){
   if(File.Exists("Temp/ui-setup.request")||!File.Exists("Temp/ui-test.request")||!File.Exists("Assets/Scenes/MenuInicio.unity")||EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)return;
   File.Delete("Temp/ui-test.request");SessionState.SetBool("ValleUITest",true);EditorApplication.isPlaying=true;return;
  }
  if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<next)return;
  next=EditorApplication.timeSinceStartup+1;
  try{var ui=Object.FindAnyObjectByType<ValleInterface>();if(!ui)return;
   switch(stage++){
    case 0:Check(ui.mainMenu,"Separate start scene");Check(ui.playButton&&ui.controlsButton&&ui.quitButton,"Three main menu buttons");Check(EventSystem.current&&EventSystem.current.currentInputModule,"Input System UI navigation");ScreenCapture.CaptureScreenshot("Documentation/MenuInicio.png");break;
    case 1:ui.controlsButton.onClick.Invoke();Check(ui.controlsPanel.activeSelf,"Controls open");ui.backButton.onClick.Invoke();Check(!ui.controlsPanel.activeSelf,"Controls return");ui.playButton.onClick.Invoke();break;
    case 2:if(ui.mainMenu){stage--;return;}Check(SceneManager.GetActiveScene().name=="ValleDelRio","Play loads valley");Check(ui.stats&&ui.weapon&&ui.healthFill&&ui.staminaFill,"HUD bound to player");ScreenCapture.CaptureScreenshot("Documentation/HUD.png");break;
    case 3:ui.Pause(true);Check(Time.timeScale==0&&ValleInterface.BlocksInput&&ui.pausePanel.activeSelf,"Pause blocks gameplay");ui.controlsButton.onClick.Invoke();Check(ui.controlsPanel.activeSelf,"Pause controls");ui.backButton.onClick.Invoke();ui.resumeButton.onClick.Invoke();Check(Time.timeScale==1&&!ValleInterface.BlocksInput,"Resume restores gameplay");ui.stats.TakeDamage(25);ui.stats.Spend(20);break;
    case 4:Check(ui.healthFill.fillAmount<1&&ui.staminaFill.fillAmount<1,"Health and stamina reflect stats");ui.stats.TakeDamage(10000);break;
    case 5:Check(ui.deathPanel.activeSelf&&!ui.hud.activeSelf,"Death UI replaces HUD");ui.respawnButton.onClick.Invoke();break;
    case 6:Check(!ui.stats.IsDead,"Respawn button");ui.Pause(true);ui.menuButton.onClick.Invoke();break;
    case 7:if(!ui.mainMenu){stage--;return;}Check(Time.timeScale==1&&!ValleInterface.BlocksInput,"Return to menu resets pause");Check(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length==1,"Single audio listener after scene transition");Finish();break;
   }
  }catch(System.Exception e){report.AppendLine("FAIL: "+e);Finish();}
 }
 static void Finish(){Directory.CreateDirectory("Documentation");File.WriteAllText("Documentation/UI-play-check.txt",report.ToString());SessionState.SetBool("ValleUITest",false);EditorApplication.isPlaying=false;}
}
