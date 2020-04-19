using UnityEngine;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Networking.Transport;
using System;
using System.Text;
using System.Collections.Generic;


//The internal definition of client but ONLY for server. (Not being used anywhere else.)
public class ServerClient
{
    public int connectionId;
    public string playerName;
}


public class oServer : MonoBehaviour
{
    public NetworkDriver m_Driver;
    private NativeList<NetworkConnection> m_Connections;

    //List of clients for server only
    public List<ServerClient> clients = new List<ServerClient>();

    public GameObject playerObject;
    public Dictionary<int, GameObject> players = new Dictionary<int, GameObject>();

    int playerCount = 0;

    void Start()
    {
        m_Driver = NetworkDriver.Create();
        var endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = 9000;
        if (m_Driver.Bind(endpoint) != 0)
            Debug.Log("Failed to bind to port 9000");
        else
            m_Driver.Listen();

        m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
    }

    public void OnDestroy()
    {
        m_Driver.Dispose();
        m_Connections.Dispose();
    }


    // Keep track of id for every player.
    void OnConnect(NetworkConnection c)
    {
        // Add to a list of client.
        m_Connections.Add(c);
        Debug.Log("Accepted a connection from: " + c.InternalId);
        GameObject temp = Instantiate(playerObject, transform.position, transform.rotation);
        players.Add(c.InternalId, temp);

        // Add to another list of client.
        ServerClient sClient = new ServerClient();
        sClient.connectionId = c.InternalId;
        sClient.playerName = "TEMP";
        clients.Add(sClient);

        //Request player's name and send the name of all the other players.
        string msg = "ASKNAME|" + c.InternalId + "|";
        foreach (ServerClient sc in clients)
            msg += sc.playerName + '%' + sc.connectionId + '|';

        msg = msg.Trim('|');

        sendMessage(msg, c, true);
    }


    void OnDisconnect(int i, NetworkConnection c)
    {
        //Remove this player from list.
        Debug.Log("Client disconnected from server");
        m_Connections[i] = default(NetworkConnection);
        clients.Remove(clients.Find(x => x.connectionId == i));
        string msg = "DISCONNECT|" + c.InternalId;
        //Tell other users.
        sendMessage(msg, c, true);
    }

    void Update()
    {
        m_Driver.ScheduleUpdate().Complete();

        // CleanUpConnections
        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated)
            {
                m_Connections.RemoveAtSwapBack(i);
                --i;
            }
        }
        // AcceptNewConnections
        NetworkConnection c;
        while ((c = m_Driver.Accept()) != default(NetworkConnection))
        {
            //Add to a list of client.
            OnConnect(c);
            playerCount++;
            Debug.Log("We have " + playerCount + " player(s)!");
        }

        // It will be used in case any Data event was received.
        DataStreamReader stream;
        for (int i = 0; i < m_Connections.Length; i++)
        {
            Assert.IsTrue(m_Connections[i].IsCreated);

            //Find out which client is talking to you.
            NetworkEvent.Type cmd;
            while ((cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream)) != NetworkEvent.Type.Empty)
            {
                // Parse msg & take action. 
                if (cmd == NetworkEvent.Type.Data)
                {
                    ////Try to read a uint from the stream and output what we have received:
                    //uint number = stream.ReadUInt();

                    //Debug.Log("Got " + number + " from the Client adding + 2 to it.");
                    //number += 2;

                    //// To send anything with the NetworkDriver we need a instance of a DataStreamWriter.
                    ////  You get a DataStreamWriter when you start sending a message by calling BeginSend.
                    //var writer = m_Driver.BeginSend(NetworkPipeline.Null, m_Connections[i]);
                    //writer.WriteUInt(number);
                    //m_Driver.EndSend(writer);

                    //Read Position from Clients.
                    ReadData(stream, i);
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client " + c.InternalId + " disconnected from server");
                    Debug.Log("Connection " + m_Connections[i] + " disconnected from server");

                    OnDisconnect(c.InternalId, c);
                }
            }
        }


        //Ask Player for their "position".

    }


    public void ReadData(DataStreamReader stream, int i)
    {
        // Get bytes from the client.
        NativeArray<byte> bytes = new NativeArray<byte>(stream.Length, Allocator.Temp);
        stream.ReadBytes(bytes);

        // Get string from the client. 
        string msg = Encoding.ASCII.GetString(bytes.ToArray());
        Debug.Log("Recieving: " + msg + " From Player-" + i);

        // Split the string into an array.
        string[] splitData = msg.Split('|');

        // Reading the first set of data.
        switch (splitData[0])
        {
            case "MOVE":
                Move(splitData[1], splitData[2], players[i]);
                break;

            case "NAMEIS":
                NameIs(i, splitData[1], m_Connections[i]);
                break;
        }
    }


    void Move(string x, string y, GameObject obj)
    {
        float xMov = float.Parse(x);
        float yMov = float.Parse(y);
        obj.transform.Translate(xMov, 0.0f, yMov);
    }

    void NameIs(int clientId, string playerName, NetworkConnection c)
    {
        //Link the name to client Id.
        clients[clientId].playerName = playerName;
        //Broadcast new player's name to every one.
        string msg = "CONNECT|" + playerName + "|" + clientId;
        sendMessage(msg, c , true);
    }


    //Send message to client
    public void sendMessage(string message, NetworkConnection c, bool sendToAll)
    {
        Debug.Log("Sending : " + message);

        //// To send anything with the NetworkDriver we need a instance of a DataStreamWriter.
        ////  You get a DataStreamWriter when you start sending a message by calling BeginSend.
        NativeArray<byte> bytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(message), Allocator.Temp);

        if (sendToAll)
        {
            //Send message to all client
            foreach (NetworkConnection connection in m_Connections)
            {
                var writer = m_Driver.BeginSend(NetworkPipeline.Null, connection);
                writer.WriteBytes(bytes);
                m_Driver.EndSend(writer);
            }
        }
        else
        {
            var writer = m_Driver.BeginSend(NetworkPipeline.Null, c);
            writer.WriteBytes(bytes);
            m_Driver.EndSend(writer);
        }
    

    }

    




}

