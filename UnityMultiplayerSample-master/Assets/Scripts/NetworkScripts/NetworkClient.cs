using UnityEngine;

using Unity.Collections;
using Unity.Networking.Transport;
using System;

//Game States 
public enum ClientState
{
    Connect,
    WaitingList,
    GameSelect,
    GameWaitingForOpp,    
    GameContinue,
    Disconnect
}

public class NetworkClient : MonoBehaviour
{
    [SerializeField]
    GameScript gameScript;

    public UdpNetworkDriver m_Driver;
    public NetworkConnection m_Connection;
    public bool m_Done;

    States? figure = null;
    bool? continueGame = null;
    ClientState state = ClientState.Connect;
    public string ErrorMessage { get; private set; }

    //Select figure calls when figure button is clicked 
    public void SelectFigure(States figure)
    {
        this.figure = figure;
    }

    public void ContinueGame(bool continueGame)
    {
        this.continueGame = continueGame;
    }

    //Connection to the driver 
    public bool Connect(ushort port = 9000, string address = "localhost")
    {
        var endpoint = NetworkEndPoint.LoopbackIpv4;
        endpoint.Port = 9000;//NetworkEndPoint.Parse(address, port);
        m_Connection = m_Driver.Connect(endpoint);
        if (!m_Connection.IsCreated)
            ErrorMessage = "Bad Gateway - Failed to connect to server.";

        return m_Connection.IsCreated;
    }
    

    void Start ()
    {
        m_Driver = new UdpNetworkDriver(new INetworkParameter[0]);
        m_Connection = default(NetworkConnection);
        

        //var endpoint = NetworkEndPoint.Parse("52.15.219.197",12345);
        //var endpoint = NetworkEndPoint.LoopbackIpv4;
        //endpoint.Port = 12345;
        //m_Connection = m_Driver.Connect(endpoint);
    }

    //OnDeestroy sicsonnect driver and dispose it 
    public void OnDestroy()
    {
        if(m_Connection.IsCreated)
            m_Connection.Disconnect(m_Driver);
        m_Driver.Dispose();
    }

    void Update()
    {
        m_Driver.ScheduleUpdate().Complete();

        if (!m_Connection.IsCreated)
            return;

        //If connection was created then :
        switch(state)
        {
            case ClientState.Connect:
                ConnectionStateProcess();
                // Timeot process
                break;
            case ClientState.Disconnect:
                Disconnect();
                break;
            case ClientState.GameContinue:
                GameContinue();
                break;
            case ClientState.GameSelect:
                GameSelectProcess();
                break;
            case ClientState.GameWaitingForOpp:
                GameWaitingForOppProcess();
                break;
            case ClientState.WaitingList:
                WaitingListProcess();
                break;
        }


        //while ((cmd = m_Connection.PopEvent(m_Driver, out stream)) !=
        //       NetworkEvent.Type.Empty)
        //{
        //    if (cmd == NetworkEvent.Type.Connect)
        //    {

        //        Debug.Log("We are now connected to the server");
        //        //Debug.Log("We are now connected to the server");

        //        //var value = 1;
        //        //using (var writer = new DataStreamWriter(4, Allocator.Temp))
        //        //{
        //        //    writer.Write(value);
        //        //    m_Connection.Send(m_Driver, writer);
        //        //}
        //    }
        //    else if (cmd == NetworkEvent.Type.Data)
        //    {
        //        var readerCtx = default(DataStreamReader.Context);
        //        uint value = stream.ReadUInt(ref readerCtx);
        //        Debug.Log("Got the value = " + value + " back from the server");
        //    }
        //    else if (cmd == NetworkEvent.Type.Disconnect)
        //    {
        //        Debug.Log("Client got disconnected from server");
        //        m_Connection = default(NetworkConnection);
        //    }
        //}

        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //    var value = 1;
        //    using (var writer = new DataStreamWriter(4, Allocator.Temp))
        //    {
        //        writer.Write(value);
        //        m_Connection.Send(m_Driver, writer);
        //    }
        //}
    }

    private void Disconnect()
    {   //Disconnect driver, reset connection, change state to 'Connect' 
        m_Connection.Disconnect(m_Driver);
        m_Connection = default(NetworkConnection);
        state = ClientState.Connect;
    }

    //?
    private void GameContinue()
    {
        DataStreamReader stream;
        NetworkEvent.Type cmd;
        if ((cmd = m_Connection.PopEvent(m_Driver, out stream)) != NetworkEvent.Type.Empty)
        {   //Pop an event from driver 
            if (cmd == NetworkEvent.Type.Disconnect)
            {   //If event 'Disconnect' then reset connection 
                Debug.Log("Client got disconnected from server");
                m_Connection = default(NetworkConnection);
            }
            else
            {
                ErrorMessage = "Bad message from server!";
                Disconnect();
            }
            return;
        }
            

        if (continueGame != null)
        {   //Instantiate writer 
            using (var writer = new DataStreamWriter(4, Allocator.Temp))
            {   //Compare if value from client is  1 - change ClientState to waitingList and send value to server, else disconnect client, reset figure and game bool
                int value = continueGame.Value ? 1 : 0;
                writer.Write(value);
                m_Connection.Send(m_Driver, writer);
                //state = ClientState.GameWaitingForOpp;
                if (continueGame.Value)
                    state = ClientState.WaitingList;
                else
                    state = ClientState.Disconnect;

                figure = null;
                continueGame = null;
            }
        }
    }

    private void GameWaitingForOppProcess()
    {
        DataStreamReader stream;
        NetworkEvent.Type cmd;
        if ((cmd = m_Connection.PopEvent(m_Driver, out stream)) != NetworkEvent.Type.Empty)
        {   //Pop event from driver 
            if (cmd == NetworkEvent.Type.Data)
            {   //If event 'Data' - instantiate reader, save data to value, call OpponentTurn(), and move client to 'GameContinue' state 
                var readerCtx = default(DataStreamReader.Context);
                uint value = stream.ReadUInt(ref readerCtx);
                Debug.Log("Server received a value " + value);
                gameScript.OpponentTurn((States)value);
                state = ClientState.GameContinue;
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log("Client got disconnected from server");
                m_Connection = default(NetworkConnection);
            }
        }
    }

    private void GameSelectProcess()
    {
        DataStreamReader stream;
        NetworkEvent.Type cmd;
        if ((cmd = m_Connection.PopEvent(m_Driver, out stream)) != NetworkEvent.Type.Empty)
        {   //Pop event from driver, check if message was valid or 'Disconnect' 
            if (cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log("Client got disconnected from server");
                m_Connection = default(NetworkConnection);
            }
            else
            {
                ErrorMessage = "Bad message from server!";
                m_Connection.Disconnect(m_Driver);
                m_Connection = default(NetworkConnection);
            }
            return;
        }

        if (figure != null)
        {   //If figure was selected - instantiate writer 
            using (var writer = new DataStreamWriter(4, Allocator.Temp))
            {   //Save figure value to value, send it to server, move Client to GameWaitingForOpp state 
                int value = (int)figure.Value;
                writer.Write(value);
                m_Connection.Send(m_Driver, writer);
                state = ClientState.GameWaitingForOpp;
            }
        }
    }

    private void WaitingListProcess()
    {
        DataStreamReader stream;
        NetworkEvent.Type cmd;
        if ((cmd = m_Connection.PopEvent(m_Driver, out stream)) != NetworkEvent.Type.Empty)
        {   //Pop event from driver, if event 'Data' - instantiate reader, save data to gameId and move client to 'GameSelect' state 
            if (cmd == NetworkEvent.Type.Data)
            {
                var readerCtx = default(DataStreamReader.Context);
                ulong gameId = stream.ReadULong(ref readerCtx);
                Debug.Log("Client is connected to the game with ID: " + gameId);
                state = ClientState.GameSelect;
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log("Client got disconnected from server");
                m_Connection = default(NetworkConnection);
            }
        }
    }

    //Pop an event from driver
    private void ConnectionStateProcess()
    {
        DataStreamReader stream;
        NetworkEvent.Type cmd;
        if ((cmd = m_Connection.PopEvent(m_Driver, out stream)) != NetworkEvent.Type.Empty)
        {   //If event 'Connect'
            if (cmd == NetworkEvent.Type.Connect)
            {   //Changes game state to waiting list 
                Debug.Log("We are now connected to the server"); // Move message to GameEvents Log
                state = ClientState.WaitingList;
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {   //If event 'Disconnect' then reset connection 
                Debug.Log("Client got disconnected from server");
                m_Connection = default(NetworkConnection);
            }
            else
            {   //Else disconnect driver and reset connection 
                ErrorMessage = "Bad message from server!";
                m_Connection.Disconnect(m_Driver);
                m_Connection = default(NetworkConnection);
            }
        }
    }
}