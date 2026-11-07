using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
namespace Valle.Lan {
 [DefaultExecutionOrder(-100)]
 public sealed class LanSession:MonoBehaviour {
  public static LanSession Instance{get;private set;}
  public bool IsHost{get;private set;}public int LocalSlot{get;private set;}=-1;
  public string Phase{get;private set;}="offline";public string Status="Crea una sala o escribe la IP del anfitrión.";
  public LanSlot[] Slots{get;private set;}public LanWorld World{get;private set;}public LanUI UI{get;private set;}
  public int BlueScore,RedScore;public float Remaining=LanNightRules.NightSeconds;public string PlayerName="Jugador";public int Role,Team;public readonly LanNight Night=new LanNight();
  public bool MenuOpen;public string LocalAddress{get;private set;}
  readonly List<LanPeer> peers=new List<LanPeer>();readonly Dictionary<int,LanInput> inputs=new Dictionary<int,LanInput>();readonly Dictionary<int,float> inputAt=new Dictionary<int,float>();
  LanTransport transport;LanPeer server;float nextSend,nextFull,accumulator,connectAt;bool loading,closing;LanInput localInput=new LanInput();
  public static void Open(){if(Instance)return;new GameObject("Multijugador LAN").AddComponent<LanSession>();}
  void Awake(){if(Instance){Destroy(gameObject);return;}Instance=this;DontDestroyOnLoad(gameObject);Application.runInBackground=true;LocalAddress=Addresses();UI=gameObject.AddComponent<LanUI>();SceneManager.sceneLoaded+=SceneLoaded;}
  public void Host(){if(Phase!="offline")return;try{transport=new LanTransport();transport.Host();IsHost=true;Slots=Enumerable.Range(0,8).Select(i=>new LanSlot{id=i,team=i/4,role=i%3,name="Bot "+(i%4+1),bot=true,ready=true}).ToArray();LocalSlot=Team==0?0:4;Slots[LocalSlot].bot=false;Slots[LocalSlot].name=Clean(PlayerName);Slots[LocalSlot].role=Mathf.Clamp(Role,0,2);Phase="lobby";Status="Sala creada · IP: "+LocalAddress;}catch(Exception e){transport?.Dispose();transport=null;Status="No se pudo crear la sala: "+e.Message;}}
  public void Join(string address){if(Phase!="offline")return;try{transport=new LanTransport();transport.Join(address.Trim());connectAt=Time.realtimeSinceStartup;Phase="connecting";Status="Conectando…";}catch(Exception e){transport?.Dispose();transport=null;Status=e.Message;}}
  void Update(){if(closing)return;float now=Time.realtimeSinceStartup;if(!IsHost)Night.Clock+=Time.unscaledDeltaTime;
   if(transport!=null){while(transport.Errors.TryDequeue(out var error)){Disconnect(error);return;}while(transport.Take(out var accepted)){accepted.LastSeen=now;if(IsHost){if(peers.Count>=7){accepted.Dispose();}else peers.Add(accepted);}else{server=accepted;Send(accepted,new LanPacket{type="hello",name=Clean(PlayerName),role=Role,team=Team});}}
    if(IsHost){foreach(var p in peers.ToArray()){Pump(p);if(p.Closed||now-p.LastSeen>12){if(p.Slot>=0){if(Phase=="lobby"&&Night.Leader==p.Slot)Night.Leader=-1;Slots[p.Slot].bot=true;Slots[p.Slot].ready=true;Slots[p.Slot].name="Bot "+(p.Slot%4+1);inputs.Remove(p.Slot);inputAt.Remove(p.Slot);}p.Dispose();peers.Remove(p);}}}
    else if(server!=null){Pump(server);if(server.Closed||now-server.LastSeen>12){Disconnect("Se perdió la conexión con el anfitrión.");return;}}
    if(Phase=="connecting"&&now-connectAt>8){Disconnect("El anfitrión no respondió. Comprueba la red y la IP.");return;}
   }
   if(Phase=="match"&&World&&LocalSlot>=0)ReadInput();
   if(now>=nextSend){nextSend=now+.05f;if(IsHost){if(LocalSlot>=0){inputs[LocalSlot]=localInput;inputAt[LocalSlot]=now;}Broadcast();}else if(server!=null)Send(server,new LanPacket{type=World&&Phase=="match"?"input":"ping",input=localInput});}
   if(IsHost&&Phase=="match"&&World){accumulator+=Mathf.Min(Time.unscaledDeltaTime,.15f);while(accumulator>=.05f&&Phase=="match"){accumulator-=.05f;Step(.05f);}}
  }
  public void Step(float dt){World.Tick(dt);Night.Tick(dt,this);if(Night.Outcome!=LanOutcome.None)Phase="result";}
  void Pump(LanPeer peer){int budget=128;while(budget-->0&&peer.Incoming.TryDequeue(out var json)){try{var p=JsonUtility.FromJson<LanPacket>(json);if(p==null||p.version!=LanPacket.Version){peer.Dispose();return;}peer.LastSeen=Time.realtimeSinceStartup;if(IsHost)HandleClient(peer,p);else HandleServer(p);}catch(Exception e){Debug.LogWarning("LAN: mensaje rechazado: "+e.GetType().Name);peer.Dispose();return;}}}
  void HandleClient(LanPeer peer,LanPacket p){
   if(peer.Slot<0){if(p.type!="hello")return;int requested=Mathf.Clamp(p.team,0,1);var slot=Slots.FirstOrDefault(s=>s.bot&&s.team==requested&&s.id!=Night.Leader)??Slots.FirstOrDefault(s=>s.bot&&s.id!=Night.Leader)??Slots.FirstOrDefault(s=>s.bot);if(slot==null){peer.Dispose();return;}peer.Slot=slot.id;slot.bot=false;slot.name=Clean(p.name);slot.role=Mathf.Clamp(p.role,0,2);slot.ready=Phase!="lobby";slot.loaded=false;if(World)World.Pawns[slot.id].ClampToClass();Send(peer,new LanPacket{type="welcome",slot=slot.id});SendSnapshot(peer);return;}
   if(p.type=="loaded")Slots[peer.Slot].loaded=true;
   if(p.type=="input"&&Phase=="match"&&p.input!=null&&p.input.Valid()){inputs[peer.Slot]=p.input;inputAt[peer.Slot]=Time.realtimeSinceStartup;}
   if(p.type=="ready"&&Phase=="lobby")Slots[peer.Slot].ready=!Slots[peer.Slot].ready;
   if(p.type=="class"&&Phase=="lobby"){Slots[peer.Slot].role=Mathf.Clamp(p.role,0,2);Slots[peer.Slot].ready=false;}
   if(p.type=="leader"&&Phase=="lobby")ToggleLeader(peer.Slot);
   if(p.type=="point"&&Phase=="match")SetPoint(peer.Slot,p.point);
  }
  void HandleServer(LanPacket p){if(p.type=="welcome"){if(p.slot<0||p.slot>7)throw new Exception("slot");LocalSlot=p.slot;return;}if(p.type!="snapshot"||p.slots==null||p.slots.Length!=8||LocalSlot<0)return;
   Slots=p.slots;BlueScore=p.blue;RedScore=p.red;Remaining=p.remaining;Phase=p.phase;Night.Read(p);
   if(Phase=="match"||Phase=="result"){if(SceneManager.GetActiveScene().name!=LanNightRules.Scene){Load(LanNightRules.Scene);}else if(World)World.Apply(p);}
   else if(Phase=="lobby"&&SceneManager.GetActiveScene().name!="MenuInicio")Load("MenuInicio");
  }
  void SendSnapshot(LanPeer peer)=>Send(peer,Snapshot(true));
  // Los objetos del suelo solo viajan cuando cambian (y una vez por segundo): ahorra tráfico por Wi-Fi.
  LanPacket Snapshot(bool full){var packet=new LanPacket{type="snapshot",phase=Phase,slots=Slots,actors=World?World.States():null,hasDrops=full&&World,drops=full&&World?World.Drops.ToArray():null,marks=World?World.Marks.ToArray():null,blue=BlueScore,red=RedScore,remaining=Remaining};Night.Write(packet);return packet;}
  void Broadcast(){if(Slots==null)return;bool full=World&&(World.DropsDirty||Time.realtimeSinceStartup>=nextFull);if(full){nextFull=Time.realtimeSinceStartup+1;World.DropsDirty=false;}var json=JsonUtility.ToJson(Snapshot(full));foreach(var p in peers)if(p.Slot>=0&&!p.Closed)p.Send(json);}
  static void Send(LanPeer peer,LanPacket packet){peer?.Send(JsonUtility.ToJson(packet));}
  public bool CanStart=>IsHost&&Phase=="lobby"&&Slots.All(s=>s.bot||s.ready);
  public void StartMatch(){if(!CanStart)return;BlueScore=RedScore=0;Remaining=LanNightRules.NightSeconds;Night.Begin(Slots);foreach(var s in Slots){s.kills=s.deaths=0;s.loaded=false;}inputs.Clear();localInput=new LanInput();Phase="match";Broadcast();Load(LanNightRules.Scene);}
  public void Ready(){if(IsHost){Slots[LocalSlot].ready=!Slots[LocalSlot].ready;}else Send(server,new LanPacket{type="ready"});}
  public void ChooseClass(int value){Role=Mathf.Clamp(value,0,2);if(Phase!="lobby")return;if(IsHost)Slots[LocalSlot].role=Role;else Send(server,new LanPacket{type="class",role=Role});}
  public void ChooseLeader(){if(Phase!="lobby")return;if(IsHost)ToggleLeader(LocalSlot);else Send(server,new LanPacket{type="leader"});}
  public void ChoosePoint(int point){if(Phase!="match")return;if(IsHost)SetPoint(LocalSlot,point);else Send(server,new LanPacket{type="point",point=point});}
  void ToggleLeader(int slot){if(Slots[slot].team!=LanNightRules.Org)return;if(Night.Leader==slot)Night.Leader=-1;else if(Night.Leader<0)Night.Leader=slot;}
  void SetPoint(int slot,int point){if(!World||!Night.Deploying||Slots[slot].team!=LanNightRules.Dea)return;Slots[slot].point=World.ClampEntry(point);World.Respawn(slot);}
  public void ReturnLobby(){if(!IsHost)return;Phase="lobby";Night.Outcome=LanOutcome.None;if(Night.Leader>=0&&Slots[Night.Leader].bot)Night.Leader=-1;foreach(var s in Slots)s.ready=s.bot||s.id==LocalSlot;Broadcast();Load("MenuInicio");}
  void Load(string name){if(loading)return;loading=true;MenuOpen=false;Time.timeScale=1;SceneManager.LoadSceneAsync(name);}
  void SceneLoaded(Scene scene,LoadSceneMode mode){loading=false;World=null;if(scene.name==LanNightRules.Scene){World=FindAnyObjectByType<LanWorld>();if(World&&LocalSlot>=0){World.Initialize(this);if(IsHost)Slots[LocalSlot].loaded=true;else Send(server,new LanPacket{type="loaded"});}else Disconnect("No se pudo cargar el escenario de la partida.");}}
  public LanInput InputFor(int slot){if(!inputs.TryGetValue(slot,out var value))return new LanInput();if(!inputAt.TryGetValue(slot,out float t)||Time.realtimeSinceStartup-t>.5f)return new LanInput{jump=value.jump,reload=value.reload,pickup=value.pickup,drop=value.drop,lamp=value.lamp,use=value.use};return value;}
  void ReadInput(){var keyboard=Keyboard.current;if(keyboard!=null&&keyboard.escapeKey.wasPressedThisFrame)MenuOpen=!MenuOpen;localInput.x=localInput.z=0;localInput.fire=localInput.sprint=localInput.hold=false;if(MenuOpen)return;
   if(keyboard!=null){var move=new Vector2((keyboard.dKey.isPressed||keyboard.rightArrowKey.isPressed?1:0)-(keyboard.aKey.isPressed||keyboard.leftArrowKey.isPressed?1:0),(keyboard.wKey.isPressed||keyboard.upArrowKey.isPressed?1:0)-(keyboard.sKey.isPressed||keyboard.downArrowKey.isPressed?1:0));var forward=Vector3.ProjectOnPlane(Camera.main.transform.forward,Vector3.up).normalized;var world=Vector3.ClampMagnitude(forward*move.y+Vector3.Cross(Vector3.up,forward)*move.x,1);localInput.x=world.x;localInput.z=world.z;localInput.sprint=keyboard.leftShiftKey.isPressed;localInput.hold=keyboard.fKey.isPressed;if(keyboard.spaceKey.wasPressedThisFrame)localInput.jump++;if(keyboard.rKey.wasPressedThisFrame)localInput.reload++;if(keyboard.fKey.wasPressedThisFrame)localInput.pickup++;if(keyboard.gKey.wasPressedThisFrame)localInput.drop++;if(keyboard.tKey.wasPressedThisFrame)localInput.lamp++;if(keyboard.cKey.wasPressedThisFrame)localInput.use++;}
   if(Mouse.current!=null){var ray=Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());if(new Plane(Vector3.up,World.Pawns[LocalSlot].transform.position+Vector3.up*.35f).Raycast(ray,out float distance)){var aim=ray.GetPoint(distance)-World.Pawns[LocalSlot].transform.position;aim.y=0;aim.Normalize();localInput.ax=aim.x;localInput.az=aim.z;}localInput.fire=Mouse.current.leftButton.isPressed;}
  }
  public void ScoreKill(int killer,int victim){Slots[killer].kills++;Slots[victim].deaths++;if(killer<4)BlueScore++;else RedScore++;}
  public void Leave(){Close();if(SceneManager.GetActiveScene().name!="MenuInicio")SceneManager.LoadSceneAsync("MenuInicio");Destroy(gameObject);}
  void Disconnect(string reason){Close();IsHost=false;LocalSlot=-1;Night.Leader=-1;Phase="offline";Status=reason;Slots=null;MenuOpen=false;if(SceneManager.GetActiveScene().name!="MenuInicio")Load("MenuInicio");}
  void Close(){transport?.Dispose();transport=null;server?.Dispose();server=null;foreach(var p in peers)p.Dispose();peers.Clear();Time.timeScale=1;}
  void OnApplicationQuit(){closing=true;Close();}
  void OnDestroy(){if(Instance!=this)return;Close();Instance=null;SceneManager.sceneLoaded-=SceneLoaded;}
  static string Clean(string name){if(string.IsNullOrWhiteSpace(name))return "Jugador";return new string(name.Where(c=>char.IsLetterOrDigit(c)||c==' '||c=='_').Take(16).ToArray());}
  static string Addresses(){try{return string.Join(" / ",NetworkInterface.GetAllNetworkInterfaces().Where(n=>n.OperationalStatus==OperationalStatus.Up).SelectMany(n=>n.GetIPProperties().UnicastAddresses).Select(a=>a.Address).Where(a=>a.AddressFamily==System.Net.Sockets.AddressFamily.InterNetwork&&!IPAddress.IsLoopback(a)&&LanTransport.IsLocal(a)).Select(a=>a.ToString()).Distinct());}catch{return "Consulta la IP local en los ajustes de Wi-Fi";}}
 }
}
