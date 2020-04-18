using UnityEngine;
using UnityEngine.Networking;

public class NewGui : MonoBehaviour
{
    public Texture buttonTexture;
    //是否开启网络功能（是否连接网络）  
    bool isHaveNetworkRole = false;
    GUIStyle fontStyle;
    private void OnEnable()
    {
        fontStyle = new GUIStyle();
        fontStyle.alignment = TextAnchor.MiddleCenter;
        fontStyle.fontSize = 15;
        fontStyle.normal.textColor = Color.red;
        fontStyle.normal.background = (Texture2D)buttonTexture;
       
    }

    void Start()
    {
        isHaveNetworkRole = false;
        
    }

    private void OnDisconnected()
    {
        isHaveNetworkRole = false;

    }

    void OnGUI()
    {
        
        if (isHaveNetworkRole)
        {
            if (GUI.Button(new Rect(Screen.width / 2 - 80, Screen.height / 2 - 12, 160, 50), "Stop", fontStyle))
            {
                
                NetworkManager.singleton.StopServer();
                NetworkManager.singleton.StopClient();
                NetworkManager.singleton.StopHost();
                OnDisconnected();
            }
            return;
        }
        if (GUI.Button(new Rect(Screen.width / 2f - 80, Screen.height / 2 - 48, 160, 50), "Start Host", fontStyle))
        {
            var client = NetworkManager.singleton.StartHost();
            isHaveNetworkRole = true;

        }
        if (GUI.Button(new Rect(Screen.width / 2f - 80, Screen.height / 2 - 12, 160, 50), "Start Server", fontStyle))
        {
            isHaveNetworkRole = NetworkManager.singleton.StartServer();
        }

        if (GUI.Button(new Rect(Screen.width / 2f - 80, Screen.height / 2 + 24, 160, 50), "Start Client", fontStyle))
        {
            var client = NetworkManager.singleton.StartClient();
            isHaveNetworkRole = true;
        }
    }
}
