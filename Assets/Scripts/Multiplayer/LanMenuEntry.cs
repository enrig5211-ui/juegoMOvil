using UnityEngine;
using UnityEngine.UI;
namespace Valle.Lan {
 public sealed class LanMenuEntry:MonoBehaviour {
  void Awake(){GetComponent<Button>().onClick.AddListener(LanSession.Open);}
 }
}
