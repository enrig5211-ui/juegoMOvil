using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;
using Valle;
public static class PlayerAudioSetup {
 static AudioClip[] Clips(string folder)=>Directory.GetFiles("Assets/Footsteps Pack Expanded/SingleSteps/"+folder,"*.wav").OrderBy(x=>x).Take(8).Select(AssetDatabase.LoadAssetAtPath<AudioClip>).ToArray();
 [MenuItem("Tools/Player/Configurar pasos y Moonspire")]
 public static void Build(){if(EditorApplication.isPlaying)return;var motor=Object.FindFirstObjectByType<PlayerMotor>();if(!motor)throw new System.Exception("No hay player");EditorSceneManager.SaveScene(motor.gameObject.scene,"Assets/Scenes/ValleDelRio_AntesDeAudio.unity",true);
 var footsteps=motor.GetComponent<PlayerFootsteps>();if(!footsteps)footsteps=motor.gameObject.AddComponent<PlayerFootsteps>();
 footsteps.grass=Clips("GrassSteps");footsteps.earth=Clips("EarthgroundSteps");footsteps.stone=Clips("ConcreteSteps");footsteps.water=Clips("WaterSteps");
 if(!footsteps.source){var go=new GameObject("Audio - Pasos");go.transform.SetParent(motor.transform,false);footsteps.source=go.AddComponent<AudioSource>();}
 var src=footsteps.source;src.playOnAwake=false;src.loop=false;src.spatialBlend=0;src.volume=1;src.priority=100;
 const string path="Assets/10 Fantasy Medieval Ambient Tracks/Loops/1. Moonspire.wav";
 var importer=(AudioImporter)AssetImporter.GetAtPath(path);var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.Streaming;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.7f;importer.defaultSampleSettings=settings;importer.SaveAndReimport();
 var music=Object.FindFirstObjectByType<AmbientMusic>();if(!music){var go=new GameObject("AMBIENTE - Moonspire");go.AddComponent<AudioSource>();music=go.AddComponent<AmbientMusic>();}
 var audio=music.GetComponent<AudioSource>();audio.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);audio.playOnAwake=false;audio.loop=true;audio.spatialBlend=0;audio.volume=.12f;audio.priority=160;music.volume=.12f;music.fadeSeconds=3;
 if(Object.FindObjectsByType<AudioListener>().Length!=1)throw new System.Exception("Revisar AudioListeners: debe existir uno.");
 EditorSceneManager.SaveScene(motor.gameObject.scene);File.WriteAllText("Temp/player-audio-result.txt",$"Footstep banks: {footsteps.grass.Length}/{footsteps.earth.Length}/{footsteps.stone.Length}/{footsteps.water.Length}\nMusic: {audio.clip.name}; {audio.clip.length:F1}s; Streaming; loop; 12% volume; fade 3s\n");
 }
}
