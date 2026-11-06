using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
namespace Valle.Lan {
 // TCP messages are length-prefixed: reads may return only part of a frame.
 // Socket workers never call Unity APIs; main thread consumes bounded queues.
 public sealed class LanPeer : IDisposable {
  const int MaxFrame=65536;
  readonly TcpClient client;readonly NetworkStream stream;readonly AutoResetEvent wake=new AutoResetEvent(false);
  readonly ConcurrentQueue<byte[]> outgoing=new ConcurrentQueue<byte[]>();
  public readonly ConcurrentQueue<string> Incoming=new ConcurrentQueue<string>();
  public volatile bool Closed;public int Slot=-1;public float LastSeen;
  public LanPeer(TcpClient socket){client=socket;client.NoDelay=true;client.SendTimeout=3000;stream=client.GetStream();new Thread(Read){IsBackground=true,Name="Valle LAN read"}.Start();new Thread(Write){IsBackground=true,Name="Valle LAN write"}.Start();}
  public void Send(string json){if(Closed)return;var data=Encoding.UTF8.GetBytes(json);if(data.Length>MaxFrame||outgoing.Count>64){Dispose();return;}outgoing.Enqueue(data);wake.Set();}
  void Read(){try{var header=new byte[4];while(!Closed){Exact(header);int n=IPAddress.NetworkToHostOrder(BitConverter.ToInt32(header,0));if(n<1||n>MaxFrame||Incoming.Count>=256)throw new System.IO.IOException("Invalid LAN frame");var data=new byte[n];Exact(data);Incoming.Enqueue(Encoding.UTF8.GetString(data));}}catch(Exception){Dispose();}}
  void Exact(byte[] data){int offset=0;while(offset<data.Length){int n=stream.Read(data,offset,data.Length-offset);if(n==0)throw new System.IO.EndOfStreamException();offset+=n;}}
  void Write(){try{while(!Closed){while(outgoing.TryDequeue(out var data)){var header=BitConverter.GetBytes(IPAddress.HostToNetworkOrder(data.Length));stream.Write(header,0,4);stream.Write(data,0,data.Length);}wake.WaitOne(1000);}}catch(Exception){Dispose();}}
  public void Dispose(){if(Closed)return;Closed=true;try{client.Close();wake.Set();}catch(ObjectDisposedException){} }
 }
 public sealed class LanTransport : IDisposable {
  public const int Port=7777;
  readonly ConcurrentQueue<LanPeer> accepted=new ConcurrentQueue<LanPeer>();
  public readonly ConcurrentQueue<string> Errors=new ConcurrentQueue<string>();
  TcpListener listener;TcpClient connecting;volatile bool stopped;
  public void Host(){listener=new TcpListener(IPAddress.Any,Port);listener.Start(8);new Thread(()=>{try{while(!stopped){var socket=listener.AcceptTcpClient();if(accepted.Count>=8){socket.Close();continue;}accepted.Enqueue(new LanPeer(socket));}}catch(Exception e){if(!stopped)Errors.Enqueue(e.Message);}}){IsBackground=true,Name="Valle LAN accept"}.Start();}
  public void Join(string address){
   if(!IPAddress.TryParse(address,out var ip)||ip.AddressFamily!=AddressFamily.InterNetwork||!IsLocal(ip))throw new ArgumentException("Introduce una IP local, por ejemplo 192.168.1.20.");
   connecting=new TcpClient(AddressFamily.InterNetwork);
   new Thread(()=>{try{var task=connecting.ConnectAsync(ip,Port);if(!task.Wait(5000))throw new TimeoutException("No se encontró al anfitrión. Revisa IP, Wi-Fi y firewall.");if(stopped){connecting.Close();return;}accepted.Enqueue(new LanPeer(connecting));}catch(Exception){if(!stopped)Errors.Enqueue("No se pudo conectar. Revisa IP, misma red y permiso del firewall (TCP 7777).");}}){IsBackground=true,Name="Valle LAN connect"}.Start();
  }
  public static bool IsLocal(IPAddress ip){var b=ip.GetAddressBytes();return b.Length==4&&(b[0]==10||b[0]==127||b[0]==192&&b[1]==168||b[0]==172&&b[1]>=16&&b[1]<=31||b[0]==169&&b[1]==254);}
  public bool Take(out LanPeer peer)=>accepted.TryDequeue(out peer);
  public void Dispose(){stopped=true;listener?.Stop();connecting?.Close();while(accepted.TryDequeue(out var p))p.Dispose();}
 }
}
