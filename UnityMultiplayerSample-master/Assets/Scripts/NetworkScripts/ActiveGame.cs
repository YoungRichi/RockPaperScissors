using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;

public class ActiveGame
{
    private UdpNetworkDriver m_driver;
    private NetworkConnection m_con1;
    private NetworkConnection m_con2;

    public ActiveGame(UdpNetworkDriver driver, NetworkConnection con1, NetworkConnection con2)
    {
        m_driver = driver;
        m_con1 = con1;
        m_con2 = con2;
    }

    // Process
    public void GameProcess() {

    }
    // IsActive    
}
