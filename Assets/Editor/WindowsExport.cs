using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
public static class WindowsExport {
 [MenuItem("Tools/Exportar/Windows x64")]
 public static void Build(){
  if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,BuildTarget.StandaloneWindows64)||!Directory.Exists(Path.Combine(EditorApplication.applicationContentsPath,"PlaybackEngines/WindowsStandaloneSupport"))){EditorUtility.DisplayDialog("Falta el módulo Windows","En Unity Hub > Installs > Unity 6000.4.0f1 > Add modules, instala Windows Build Support (Mono). Después vuelve a esta opción.","Entendido");return;}
  if(!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
  PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
  Directory.CreateDirectory("Builds/Windows");var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/MenuInicio.unity","Assets/Scenes/ValleDelRio.unity","Assets/Scenes/ValleMultijugador.unity"},locationPathName="Builds/Windows/ValleDelRio.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.CompressWithLz4HC});
  if(result.summary.result==BuildResult.Succeeded)EditorUtility.RevealInFinder("Builds/Windows/ValleDelRio.exe");else EditorUtility.DisplayDialog("Exportación no completada","Revisa los errores de compilación en Console.","Entendido");
 }
}
