using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Text;
using System;


public class Player {


    public string playerName;
    public GameObject avatar;
    public int connectionId;

}
public class Client : MonoBehaviour {



    private const int MAX_CONNECTIONS = 8;
    private int hostid;
    private int wedId;
    private int host;
    private int port = 5888;
    private int reilableChnl;
    private int unReilableChnl;
    private int ourClientId;
    private int connectionId;
    private float connectionTime;
    private bool isConnected = false;
    private bool isStarted = false;
    private byte err;
    private string playerName;
     public GameObject playerPrefab;

    public Dictionary<int,Player> players = new Dictionary<int,Player>();

    public void Connect()
    {

        string pName = GameObject.Find("nameInput").GetComponent<InputField>().text;
        if (pName == "")
        {
            Debug.Log("Name empty please fill");
            return;
        }
        playerName = pName;
        Debug.Log("player name was initilaized");
        NetworkTransport.Init();

        ConnectionConfig cc = new ConnectionConfig();

        reilableChnl = cc.AddChannel(QosType.Reliable);
        unReilableChnl = cc.AddChannel(QosType.Unreliable);

        HostTopology topo = new HostTopology(cc, MAX_CONNECTIONS);
        hostid = NetworkTransport.AddHost(topo,0);
        connectionId = NetworkTransport.Connect(hostid,"127.0.0.1", port, 0, out err);
        connectionTime = Time.time;
        isConnected = true;
  

    }


    public void Update()
    {
        if (!isConnected)
            return;

        int recHostId;
        int connectionId;
        int channelId;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        byte error;
        NetworkEventType recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, bufferSize, out dataSize, out error);

        switch (recData){

            case NetworkEventType.DataEvent:       //3
                string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                Debug.Log("Recv : " + msg);
                string[] splitData = msg.Split('|');
                switch (splitData[0]){

                    case "ASKNAME":
                        OnAskName(splitData);
                        break;
                    case "CNN":
                        SpawnPlayer(splitData[1], int.Parse(splitData[2]));
                        break;
                    case "DC":
                        PlayerDisconnected(int.Parse(splitData[1]));
                        break;
                    case "ASKPOSITION":
                        onAskPosition(splitData);
                        break;
                    default:
                        Debug.Log("Other thing happned");
                        break;


                }
                break;
        }
    }
    private void onAskPosition(string[] data)
    {//update everyone else 
        if (!isStarted)
            return;
        for (int i = 1; i < data.Length; i++) {
            string[] d = data[i].Split('%');
            Debug.Log(i + " " +  float.Parse(d[1])
           +" "+ float.Parse(d[2]) + " " +
             float.Parse(d[3]) + " " + "     <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<");
            if (ourClientId != int.Parse(d[0]))
            {
                Vector3 position = Vector3.zero;
                position.x = float.Parse(d[1]);
                position.y = float.Parse(d[2]);
                position.z = float.Parse(d[3]);
                players[int.Parse(d[0])].avatar.transform.position = position;
               
            }

        }
        //send our own position
        Vector3 myPos = players[ourClientId].avatar.transform.position;
        string m = "MYPOSITION|" + myPos.x.ToString() +'|' + myPos.y.ToString() +'|' + myPos.z.ToString();

        Send(m, unReilableChnl);
    }
    private void OnAskName(string[] data) {
        ourClientId = int.Parse(data[1]);

        Send("NameIs|" + playerName, reilableChnl);
        for (int i = 2; i < data.Length - 1; i++) {

            string[] d = data[i].Split('%');

            if(d[0]!="Temp")
            SpawnPlayer(d[0], int.Parse(d[1]));

 
        }
    }
    private void SpawnPlayer(string playerName, int conId) {


   
        GameObject go = Instantiate(playerPrefab) as GameObject;
      if (conId == ourClientId) { // remove entery page (name info)

           // go.AddComponent<playerMotor>();
            GameObject.Find("Canvas").SetActive(false);
            isStarted = true;
            Debug.Log("IN OUT CLIENT");
      }

        Player P = new Player();
        P.avatar = go;
        P.playerName = playerName;
        P.connectionId = conId;
        P.avatar.GetComponentInChildren<TextMesh>().text = playerName;
        players.Add(conId,P);
    }                        
    private void Send(string message, int channelId)
    {

        Debug.Log("Sending : " + message);
        byte[] msg = Encoding.Unicode.GetBytes(message);
       
            NetworkTransport.Send(hostid,connectionId, channelId, msg, message.Length * sizeof(char), out err);

    }
    private void PlayerDisconnected(int conId){
        Destroy(players[conId].avatar);
        players.Remove(conId);
    }
}
