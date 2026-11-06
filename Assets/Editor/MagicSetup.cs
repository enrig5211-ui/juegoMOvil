using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Valle;
public static class MagicSetup {
 [MenuItem("Tools/Efectos/Preparar magia PixPlays")]
 public static void Build(){Directory.CreateDirectory("Assets/Resources/Effects");var palette=AssetDatabase.LoadAssetAtPath<MagicPalette>("Assets/Resources/Effects/MagicPalette.asset");if(!palette){palette=ScriptableObject.CreateInstance<MagicPalette>();AssetDatabase.CreateAsset(palette,"Assets/Resources/Effects/MagicPalette.asset");}
  palette.windSpawn=Variant("ElementalAuras/WindAura/Version_URP/WindAura.prefab","ReaparicionViento");palette.earthSpawn=Variant("ElementalAuras/EarthAura/Version_URP/EarthAura.prefab","ReaparicionTierra");palette.fireSpawn=Variant("ElementalAuras/FireAura/Version_URP/FireAura.prefab","ReaparicionFuego");palette.protection=Variant("ElementalShields/WaterShield/Version_URP/WaterShield.prefab","ProteccionAgua");palette.impact=Variant("ElementalProjectiles/Windbullet/Version_URP/WindbulletHit/WindbulletHit.prefab","ImpactoArcano");EditorUtility.SetDirty(palette);AssetDatabase.SaveAssets();}
 static GameObject Variant(string source,string name){var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PixPlays/"+source);if(!asset)throw new System.Exception("Missing URP prefab: "+source);var go=Object.Instantiate(asset);go.name=name;
  foreach(var b in go.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(b);
  foreach(var c in go.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
  foreach(var l in go.GetComponentsInChildren<Light>(true))Object.DestroyImmediate(l);
  foreach(var p in go.GetComponentsInChildren<ParticleSystem>(true)){p.gameObject.SetActive(true);var main=p.main;main.scalingMode=ParticleSystemScalingMode.Hierarchy;main.maxParticles=Mathf.Min(128,main.maxParticles);main.playOnAwake=false;main.stopAction=ParticleSystemStopAction.None;var lights=p.lights;lights.enabled=false;var collision=p.collision;collision.enabled=false;}
  foreach(var r in go.GetComponentsInChildren<Renderer>(true)){r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;foreach(var m in r.sharedMaterials)if(!m||!m.shader||m.shader.name.Contains("InternalError"))throw new System.Exception("Missing material/shader on "+r.name);}
  var saved=PrefabUtility.SaveAsPrefabAsset(go,"Assets/Resources/Effects/"+name+".prefab");Object.DestroyImmediate(go);return saved;
 }
}
