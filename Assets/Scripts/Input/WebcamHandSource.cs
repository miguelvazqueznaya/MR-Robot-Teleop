using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/*
 * implements IHandSource
 */
public class WebcamHandSource : MonoBehaviour, IHandSource
{
    //holds 5 values, tracked - whether hand was tracked in frame, x,y where middle finger knucle is in image,
    ///size is apparent size and pinch is thumb to index distance ration
    [System.Serializable] class Packet { public bool tracked; public float x, y, size, pinch; }

    //udp port unity listens on.
    [SerializeField] int port = 5052;

    //metres the full camera image covers and scaled to 0.6m by 0.4m, moving hand moves sphere 0.6m
    [SerializeField] Vector2 workspace = new Vector2(0.6f, 0.4f); 

    //hand size that counts as the neutral distance from the camera
    [SerializeField] float refSize = 0.21f;

    //how many metres of z movement a size change produces. 0 means depth has no effect. 
    [SerializeField] float depthRange = 0f;   // keep 0 for now

    UdpClient client; //object that represents listening end of connection. 
    Thread thread; //waits for packets without freezing unity.

    //way of preventing race conditions and allows for locks on threads
    readonly object gate = new object(); 
    Packet latest = new Packet(); //holds the latest packet


    //unity main thread reading: get key, read, release
    //while thread holds key, receiver thread cannot swap in a new packet, so tracked xy,size,pinch always comes from same packet. 
    public bool IsTracked { get { lock (gate) return latest.tracked; } }

    //x,y, size all read inside lock so they can't be mixed between packets. 
    public Vector3 Position
    {
        get
        {
            lock (gate)
            {
                float z = (latest.size - refSize) / refSize * depthRange;
                return new Vector3((latest.x - 0.5f) * workspace.x, (0.5f - latest.y) * workspace.y, z); //-0.5 to recentre 0to1 coords around zero and mult by workspace converts to metres.
            }
        }
    }

    //convert raw pinch ratio from python. inputs reversed because smaller ratio means a tighter pinch. 
    //Read by Handmarker for colour. 
    public float PinchStrength { get { lock (gate) return Mathf.InverseLerp(0.7f, 0.2f, latest.pinch); } }

    void OnEnable()
    {
        client = new UdpClient(port); //create udp client on 5052 (go to unity)
        thread = new Thread(() => { //seperate thread outside of Unitys main loop. 
            var ep = new IPEndPoint(IPAddress.Any, 0); //endpoint for senders details, however Unity never actually sends anything. 
            while (true)
            {
                try
                {
                    var p = JsonUtility.FromJson<Packet>(Encoding.UTF8.GetString(client.Receive(ref ep))); //turn raw bytes into text
                    lock (gate) latest = p; //set fresh packet as the latest one. 
                }
                catch { break; }
            }
        })
        { IsBackground = true }; //decides whether a thread is allowed to keep the program alive. true because thread is only a helper. 
        thread.Start();
    }
    void OnDisable() { client?.Close(); }
}