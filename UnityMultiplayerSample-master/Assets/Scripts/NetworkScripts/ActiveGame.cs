using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using System;

public enum ActivGamePlayerState
{
    NewGameStart, // send to player game ID -> WaitingTurn
    WaitingTurn, // wating from player turn -> WaitingOpponentTurn
    WaitingOpponentTurn, // waining second WaitingOpponentTurn -> SendOppTurn
    SendOpponentTurn, // send turn of oponent -> WitingContinue
    WaitingContinue, // wating from player continue -> WaitingOpponentContinue
    WaitingOpponentContinue, // waining second WaitingOpponentContinue -> CheckGameActual
    CheckGameActuality // if both continue -> NewGameStart, else -> IsActual false and clear bad connection
}

public class ActiveGame
{
    ulong m_id;

    private UdpNetworkDriver m_driver;
    private NetworkConnection m_con1;
    private NetworkConnection m_con2;
        
    public bool IsActive { get; private set; }
    public bool FirstActive { get { return m_con1.IsCreated; } }
    public bool SecondActive { get { return m_con2.IsCreated; } }

    public NetworkConnection First { get { return m_con1; } }
    public NetworkConnection Second { get { return m_con2; } }

    ActivGamePlayerState firstState = ActivGamePlayerState.NewGameStart;
    ActivGamePlayerState secondState = ActivGamePlayerState.NewGameStart;

    States firstTurn;
    States secondTurn;

    bool firstContinue;
    bool secondContinue;

    public ActiveGame(ulong id, UdpNetworkDriver driver, NetworkConnection con1, NetworkConnection con2)
    {
        m_driver = driver;
        m_con1 = con1;
        m_con2 = con2;
        IsActive = true;
    }

    // Process
    public void GameProcess()
    {
        //if (m_con1.IsCreated)
            PlayerProcess(ref m_con1, ref firstState, secondState, ref firstTurn, secondTurn, ref firstContinue, secondContinue);
        //else
        //    FlushConnection(m_con2);

        //if(m_con2.IsCreated)
            PlayerProcess(ref m_con2, ref secondState, firstState, ref secondTurn, firstTurn, ref secondContinue, firstContinue);
        //else
        //    FlushConnection(m_con2);

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

    //private void FlushConnection(NetworkConnection m_con2)
    //{
    //    //Send info to client
    //    //ReadAll from client
    //    //deactivate Game
    //    throw new NotImplementedException();
    //}

    void NewGameStartProcess(NetworkConnection con, ref ActivGamePlayerState playerState)
    {
        using (var writer = new DataStreamWriter(8, Allocator.Temp))
        {
            writer.Write(m_id);
            m_driver.Send(NetworkPipeline.Null, con, writer);
            Debug.Log("Send NewGameStartProcess");
        }
        playerState = ActivGamePlayerState.WaitingTurn;
    }

    void WaitingTurnProcess(NetworkConnection con, ref ActivGamePlayerState playerState, ref States playerTurn)
    {
        DataStreamReader reader;
        NetworkEvent.Type cmd;
        if ((cmd = m_driver.PopEventForConnection(con, out reader)) !=
                NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Data)
            {
                var readerCtx = default(DataStreamReader.Context);
                int number = reader.ReadInt(ref readerCtx);
                playerTurn = (States)number;
                Debug.Log(string.Format("Client 1 from game {0} send {1}", m_id, number));
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log(string.Format("Client 1 of game {0} disconnected from server", m_id));
                con = default(NetworkConnection);
            }
            playerState = ActivGamePlayerState.WaitingOpponentTurn;
        }        
    }

    private void PlayerProcess(
        ref NetworkConnection con,
        ref ActivGamePlayerState playerState,
        ActivGamePlayerState opponentState,
        ref States playerTurn,
        States opponentTurn,
        ref bool playerContinue,
        bool opponentContinue)
    {
        switch (playerState)
        {
            case ActivGamePlayerState.NewGameStart:
                NewGameStartProcess(con, ref playerState);
                break;
            case ActivGamePlayerState.WaitingTurn:
                WaitingTurnProcess(con, ref playerState, ref playerTurn);
                break;
            case ActivGamePlayerState.WaitingOpponentTurn:
                WaitingOpponentTurn(ref playerState, opponentState);
                break;
            case ActivGamePlayerState.SendOpponentTurn:
                SendOpponentTurnProcess(con, ref playerState, opponentTurn);
                break;
            case ActivGamePlayerState.WaitingContinue:
                WaitingContinueProcess(con, ref playerState, ref playerContinue);
                break;
            case ActivGamePlayerState.WaitingOpponentContinue:
                WaitingOpponentContinue(ref playerState, opponentState);
                break;
            case ActivGamePlayerState.CheckGameActuality:
                CheckGameActualityProcess(ref con, ref playerState, playerContinue, opponentContinue);
                break;
        }

        //DataStreamReader reader;
        //NetworkEvent.Type cmd;
        //while ((cmd = m_driver.PopEventForConnection(con, out reader)) !=
        //        NetworkEvent.Type.Empty)
        //{
        //    if (cmd == NetworkEvent.Type.Data)
        //    {
        //        var readerCtx = default(DataStreamReader.Context);
        //        uint number = reader.ReadUInt(ref readerCtx);

        //        Debug.Log(string.Format("Client 1 from game {0} send {1}", m_id, number));
        //        number += 2;

        //        using (var writer = new DataStreamWriter(4, Allocator.Temp))
        //        {
        //            writer.Write(number);
        //            m_driver.Send(NetworkPipeline.Null, con, writer);
        //        }
        //    }
        //    else if (cmd == NetworkEvent.Type.Disconnect)
        //    {
        //        Debug.Log(string.Format("Client 1 of game {0} disconnected from server", m_id));
        //        con = default(NetworkConnection);
        //    }
        //}
    }

    private void CheckGameActualityProcess(ref NetworkConnection con, ref ActivGamePlayerState playerState, bool playerContinue, bool opponentContinue)
    {
        if (playerContinue && opponentContinue)
            playerState = ActivGamePlayerState.NewGameStart;
        else
        {
            IsActive = false;
            con = default(NetworkConnection);
        }
    }

    private void WaitingOpponentContinue(ref ActivGamePlayerState playerState, ActivGamePlayerState opponentState)
    {
        if (playerState <= opponentState)
            playerState = ActivGamePlayerState.CheckGameActuality;
    }

    private void WaitingContinueProcess(NetworkConnection con, ref ActivGamePlayerState playerState, ref bool playerContinue)
    {
        DataStreamReader reader;
        NetworkEvent.Type cmd;
        if ((cmd = m_driver.PopEventForConnection(con, out reader)) !=
                NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Data)
            {
                var readerCtx = default(DataStreamReader.Context);
                int number = reader.ReadInt(ref readerCtx);
                playerContinue = number == 0 ? false : true;
                Debug.Log(string.Format("Client 1 from game {0} send {1}", m_id, number));
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log(string.Format("Client 1 of game {0} disconnected from server", m_id));
                con = default(NetworkConnection);
            }
            playerState = ActivGamePlayerState.WaitingOpponentContinue;
        }
    }

    private void SendOpponentTurnProcess(
       NetworkConnection con, 
       ref ActivGamePlayerState playerState, 
       States opponentTurn)
    {
        using (var writer = new DataStreamWriter(4, Allocator.Temp))
        {
            writer.Write((int)opponentTurn);
            m_driver.Send(NetworkPipeline.Null, con, writer);
            Debug.Log("Send SendopponentTurnProcess");

        }
        playerState = ActivGamePlayerState.WaitingContinue;
    }

    private void WaitingOpponentTurn(ref ActivGamePlayerState playerState, ActivGamePlayerState oppState)
    {
        if (playerState <= oppState)
            playerState = ActivGamePlayerState.SendOpponentTurn;
    }
    // IsActive    
}
