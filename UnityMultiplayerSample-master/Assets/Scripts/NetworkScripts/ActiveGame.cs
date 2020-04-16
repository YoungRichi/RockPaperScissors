using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;

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

    public ActiveGame(ulong id, UdpNetworkDriver driver, NetworkConnection con1, NetworkConnection con2)
    {
        m_driver = driver;
        m_con1 = con1;
        m_con2 = con2;
    }

    // Process
    public void GameProcess()
    {
        PlayerProcess(m_con1);
        PlayerProcess(m_con2);
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

    private void PlayerProcess(NetworkConnection con)
    {
        DataStreamReader reader;
        NetworkEvent.Type cmd;
        while ((cmd = m_driver.PopEventForConnection(con, out reader)) !=
                NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Data)
            {
                var readerCtx = default(DataStreamReader.Context);
                uint number = reader.ReadUInt(ref readerCtx);

                Debug.Log(string.Format("Client 1 from game {0} send {1}", m_id, number));
                number += 2;

                using (var writer = new DataStreamWriter(4, Allocator.Temp))
                {
                    writer.Write(number);
                    m_driver.Send(NetworkPipeline.Null, con, writer);
                }
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log(string.Format("Client 1 of game {0} disconnected from server", m_id));
                con = default(NetworkConnection);
            }
        }
    }
    // IsActive    
}
