using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;




public class ServerClient{ //create list of connected clients to server
    public int connectionId;
    public string playerName;
    public Vector3 pos;
}

public class Server : MonoBehaviour
{
    private const int MAX_CONNECTIONS = 8;
    private int hostid;
    private int webHostId;
    private int port = 5888;
    private int reilableChnl;
    private int unReilableChnl;
    private bool isStarted = false;
    private byte err;
    private List<ServerClient> ClientList = new List<ServerClient>();

    private float lastMovmentUpdate;
    private float movmentUpdarteRate = 0.005f;

    private void Start() {
        Debug.Log("ON START");
        //init the server
        NetworkTransport.Init();
        ConnectionConfig cc = new ConnectionConfig();
        reilableChnl = cc.AddChannel(QosType.Reliable);
        unReilableChnl = cc.AddChannel(QosType.Unreliable);
        HostTopology topo = new HostTopology(cc, MAX_CONNECTIONS);
        hostid = NetworkTransport.AddHost(topo, port, null);
        webHostId = NetworkTransport.AddWebsocketHost(topo, port, null);
        isStarted = true;// the server is running
    }

    private void Update() {
        if (!isStarted)//if the server not running
            return;

        int recHostId;
        int connectionId;
        int channelId;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        byte error;

        //the recived data from the clients 
        NetworkEventType recData = NetworkTransport.Receive(out recHostId, out connectionId,
                                           out channelId, recBuffer, bufferSize, out dataSize, out error);

        switch (recData) {
            //can add a "Nothing" that runs each time no other command


            case NetworkEventType.ConnectEvent:    // if there was a connection Rrequest to server
                Debug.Log("Player" + connectionId + "has connected");
                Onconnection(connectionId);
                break;


            case NetworkEventType.DataEvent:   // the recived message was Data type
                string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                Debug.Log("Recv : " + msg);

                string[] splitData = msg.Split('|');
                switch (splitData[0])//type of data request like new palyer, message ...
                {
                    case "NameIs":
                        OnNameIs(connectionId, splitData[1]);
                        break;
                    case "MYPOSITION":
                        onMyPosition(connectionId,float.Parse(splitData[1]), float.Parse(splitData[2]), float.Parse(splitData[3]));
                        break;
                

                    default:
                        Debug.Log("Other thing happned");
                        break;
                }
                break;



            case NetworkEventType.DisconnectEvent: //the client has disconnected
                onDisconnection(connectionId);
                Debug.Log("  Player " + connectionId + "   has disconnected    ");
              
                break;
        }

        if (Time.time - lastMovmentUpdate > movmentUpdarteRate) {

            lastMovmentUpdate = Time.time;
            string m = "ASKPOSITION|";
            foreach (ServerClient sc in ClientList)
                 m += sc.connectionId.ToString() +'%'+ sc.pos.x.ToString() +'%'+ sc.pos.y.ToString()+ '%' + sc.pos.z.ToString() + '|';
            m = m.Trim('|');
            Send(m, unReilableChnl, ClientList);
        }
    }
    private void onMyPosition(int conId, float x, float y,float z)
    {

        ClientList.Find(c => c.connectionId == conId).pos = new Vector3(x, y, z);

    }

    private void Onconnection(int conId) {

        //we add the new client on connection to a clients list
        // aand send the clients list to the new connected player

        // the connection and name registrion must be sent in diffrent 
        ServerClient c = new ServerClient();
        c.connectionId = conId;
        c.playerName = "Temp";
        ClientList.Add(c);
        string msg = "ASKNAME|" + conId + "|";
        foreach (ServerClient sc in ClientList)
            msg += sc.playerName + "%" + sc.connectionId + '|';
        msg = msg.Trim('|');

        Send(msg, reilableChnl, conId);
    }


    private void Send(string message, int channelId, int conId) {
        //to avoid overloading the send with paramaters 
        //this function is pre actual send!
        List<ServerClient> c = new List<ServerClient>();
        c.Add(ClientList.Find(x => x.connectionId == conId));//find the client in the list

        Send(message, channelId, c); //the message sent from here
    }
    private void Send(string message, int channelId, List<ServerClient> c)
    {//the message sent 
        Debug.Log("Sending : " + message);
        byte[] msg = Encoding.Unicode.GetBytes(message); //encode message to 

        foreach (ServerClient sc in c) // sent to all the clients
            NetworkTransport.Send(hostid, sc.connectionId, channelId, msg, message.Length * sizeof(char), out err);
    }

    private void OnNameIs(int conId, string playerName) {//init the player name here

        ClientList.Find(x => x.connectionId == conId).playerName = playerName;
        Debug.Log("ON NAME IS FUNCTION");
        Send("CNN|" + playerName + '|' + conId, reilableChnl, ClientList);

    }
    private void onDisconnection(int conId) {

        ClientList.Remove(ClientList.Find(x => x.connectionId == conId));

        Send("DC|" + conId, reilableChnl, ClientList);
    }
}
