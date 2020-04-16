using UnityEngine;
using UnityEngine.Assertions;

using Unity.Collections;
using Unity.Networking.Transport;
using System;
using System.Collections.Generic;

public class NetworkServer : MonoBehaviour
{
    ulong m_curId = 0;

    List<ActiveGame> games;
    List<NetworkConnection> waitingList;

    public UdpNetworkDriver m_Driver;
    //private NativeList<NetworkConnection> m_Connections;

    void Start ()
    {
        games = new List<ActiveGame>();
        waitingList = new List<NetworkConnection>();

        m_Driver = new UdpNetworkDriver(new INetworkParameter[0]);
        var endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = 12345;
        if (m_Driver.Bind(endpoint) != 0)
            Debug.Log("Failed to bind to port 12345");
        else
            m_Driver.Listen();

        //m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
    }

    public void OnDestroy()
    {
        m_Driver.Dispose();
        //m_Connections.Dispose();
    }

    void Update ()
    {
        m_Driver.ScheduleUpdate().Complete();

        // CleanUpConnections
        CheckActiveGames();
        // AcceptNewConnections
        AcceptNewConnectionsAndCreateGames();

        GamesProcess();
        //DataStreamReader stream;
        //for (int i = 0; i < m_Connections.Length; i++)
        //{
        //    if (!m_Connections[i].IsCreated)
        //        Assert.IsTrue(true);

        //    NetworkEvent.Type cmd;
        //    while ((cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream)) !=
        //           NetworkEvent.Type.Empty)
        //    {
        //        if (cmd == NetworkEvent.Type.Data)
        //        {
        //            var readerCtx = default(DataStreamReader.Context);
        //            uint number = stream.ReadUInt(ref readerCtx);

        //            Debug.Log("Got " + number + " from the Client adding + 2 to it.");
        //            number += 2;

        //            using (var writer = new DataStreamWriter(4, Allocator.Temp))
        //            {
        //                writer.Write(number);
        //                m_Driver.Send(NetworkPipeline.Null, m_Connections[i], writer);
        //            }
        //        }
        //        else if (cmd == NetworkEvent.Type.Disconnect)
        //        {
        //            Debug.Log("Client disconnected from server");
        //            m_Connections[i] = default(NetworkConnection);
        //        }
        //    }
        //}
    }

    private void GamesProcess()
    {
        foreach (var game in games)
            game.GameProcess();
    }

    private void AcceptNewConnectionsAndCreateGames()
    {
        NetworkConnection c;
        while ((c = m_Driver.Accept()) != default(NetworkConnection))
        {
            PlaceToWaitingList(c);
            Debug.Log("Accepted a connection");
        }

        BuildGamesFromWaitingList();
    }

    private void BuildGamesFromWaitingList()
    {
        for(int i = 0; i < waitingList.Count - 1; i += 2)
        {
            ActiveGame ag = new ActiveGame(m_curId++, m_Driver, waitingList[i], waitingList[i + 1]);
            games.Add(ag);
        }
        if(waitingList.Count % 2 != 0)
        {
            var c = waitingList[waitingList.Count - 1];
            waitingList = new List<NetworkConnection>();
            waitingList.Add(c);
        }
        else
            waitingList = new List<NetworkConnection>();
    }

    private void PlaceToWaitingList(NetworkConnection c)
    {
        waitingList.Add(c);
    }

    private void CheckActiveGames()
    {
        for(int i = 0; i < games.Count; ++i)
        {
            if(!games[i].IsActive)
            {
                if (games[i].FirstActive)
                    PlaceToWaitingList(games[i].First);
                if(games[i].SecondActive)
                    PlaceToWaitingList(games[i].Second);
                games.RemoveAt(i);
                --i;
            }
        }
        //for (int i = 0; i < m_Connections.Length; i++)
        //{
        //    if (!m_Connections[i].IsCreated)
        //    {
        //        m_Connections.RemoveAtSwapBack(i);
        //        --i;
        //    }
        //}
    }

}