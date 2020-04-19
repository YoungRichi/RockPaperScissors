using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;
using System;
using System.Text;
using UnityEngine.UI;
using System.Collections.Generic;



public class Player
{
    public string connectionName;
    public GameObject connectionObject;
    public int connectionId;
}




public class oClient : MonoBehaviour
{
    public NetworkDriver m_Driver;
    public NetworkConnection m_Connection;
    public bool m_Done;

    public GameObject playerPrefab;
    public Dictionary<int, Player> players = new Dictionary<int, Player>();
    string playerName;
    int ourClientId;

    void Start()
    {
        m_Driver = NetworkDriver.Create();
        m_Connection = default(NetworkConnection);


    }

    //Enter the name before you connect to server.
    public void Connect()
    {
        //Does the player have a name?
        string inputName = GameObject.Find("NameInput").GetComponent<InputField>().text;
        if (inputName == "")
        {
            //Error: No name.
            return;
        }
        playerName = inputName; 

       
        // Standard Connect
        var endpoint = NetworkEndPoint.LoopbackIpv4;
        endpoint.Port = 9000;
        m_Connection = m_Driver.Connect(endpoint);

        //Remove Canvas
        GameObject.Find("Canvas").SetActive(false);
    }

    public void OnDestroy()
    {
        m_Driver.Dispose();
    }

    void Update()
    {
        m_Driver.ScheduleUpdate().Complete();

        if (!m_Connection.IsCreated)
        {
            if (!m_Done)
                Debug.Log("Something went wrong during connect");
            return;
        }

        DataStreamReader stream;
        NetworkEvent.Type cmd;

        while ((cmd = m_Connection.PopEvent(m_Driver, out stream)) != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                Debug.Log("We are now connected to the server");

                uint value = 3;
                var writer = m_Driver.BeginSend(m_Connection);
                writer.WriteUInt(value);
                m_Driver.EndSend(writer);
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                //uint value = stream.ReadUInt();
                //Debug.Log("Got the value = " + value + " back from the server");


                readMessage(stream);
                // TO DO: Can we remove this?
                //m_Done = true;
                
                //m_Connection.Disconnect(m_Driver);
                //m_Connection = default(NetworkConnection);
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log("Client got disconnected from server");
                m_Connection = default(NetworkConnection);
            }
        }
    }

    public void sendMessage(string message)
    {
        NativeArray<byte> buffer = new NativeArray<byte>(Encoding.ASCII.GetBytes(message), Allocator.Temp);
        var writer = m_Driver.BeginSend(m_Connection);
        writer.WriteBytes(buffer);
        m_Driver.EndSend(writer);
    }


    public void readMessage(DataStreamReader stream)
    {
        // Get bytes from the server.
        NativeArray<byte> bytes = new NativeArray<byte>(stream.Length, Allocator.Temp);
        stream.ReadBytes(bytes);

        // Get string from the server. 
        string msg = Encoding.ASCII.GetString(bytes.ToArray());
        Debug.Log("Recieving: " + msg + " From Server :" );

        // Split the string into an array.
        string[] splitData = msg.Split('|');

        // Reading the first set of data.
        switch (splitData[0])
        {
            case "ASKNAME":
                onNameRequest(splitData);
                break;

            case "CONNECT":
                SpawnPlayer(splitData[1], int.Parse(splitData[2]));
                break;

            case "DISCONNECT":
                DisconnectPlayer(int.Parse(splitData[1]));
                break;

            default:
                break;
        }
    }

    //Server asking for a name
    void onNameRequest(string[] data)
    {
        //Set this client's ID
        ourClientId = int.Parse(data[1]);

        //Send name back to to the server
        sendMessage("NAMEIS|" + playerName);

        //Create other players except yourself.
        for (int i = 2; i < data.Length - 1; i++)
        {
            string[] d = data[i].Split('%');
            SpawnPlayer(d[0], int.Parse(d[1]));
        }
        
    }

    void SpawnPlayer(string playerName, int connectID)
    {
        GameObject go = Instantiate(playerPrefab) as GameObject;

        //Spawning himself.
        if (connectID == ourClientId)
        {
            //ADdd input
        }

        Player p = new Player();
        p.connectionObject = go;
        p.connectionName = playerName;
        p.connectionId = connectID;
        p.connectionObject.GetComponentInChildren<TextMesh>().text = playerName;
        players.Add(connectID, p);
    }

    void DisconnectPlayer (int connectID)
    {
        Destroy(players[connectID].connectionObject);
        players.Remove(connectID);


        m_Done = true;
        m_Connection.Disconnect(m_Driver);
        m_Connection = default(NetworkConnection);
    }

}
