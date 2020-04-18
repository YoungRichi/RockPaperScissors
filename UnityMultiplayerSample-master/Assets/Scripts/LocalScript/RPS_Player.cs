using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class RPS_Player : NetworkBehaviour
{

    public bool debug_Default = false;

    private Text scoreText;
    private Text infoText;
    private Text handsText;

    [SyncVar] private RPS_GameState gameState;

    private RPS_PlayerData myPlayerData;
    private RPS_PlayerData otherPlayerData;

    private void Awake()
    {
        if (debug_Default)
            Debug.Log("# " + name + ": " + GetType() + ": Awake() [netId=" + netId.Value + "][isServer=" + isServer + "][isLocalPlayer=" + isLocalPlayer + "]");

        scoreText = GameObject.Find("ScoreText").GetComponent<Text>();
        infoText = GameObject.Find("InfoText").GetComponent<Text>();
        handsText = GameObject.Find("HandsText").GetComponent<Text>();

        // Disable debug gui by default
        if (handsText)
            handsText.enabled = false;
    }

    // Use this for initialization
    void Start()
    {
        gameState = RPS_GameState.Paused;
        Update_PlayerDisplay();
    }


    public override void OnStartClient()
    {
        if (debug_Default)
            Debug.Log("# " + name + ": " + GetType() + ": OnStartClient() [netId=" + netId.Value + "][isServer=" + isServer + "][isLocalPlayer=" + isLocalPlayer + "]");

        gameObject.name = "Player " + netId.Value;

    }


    // Update is called once per frame
    private void Update()
    {
        if (isLocalPlayer) // && !isServer)
        {
            if (gameState == RPS_GameState.Play)
            {
                GetPlayerInput();
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                if (debug_Default)
                {
                    Debug.Log("Update: Got KeyCode.D");
                    handsText.enabled = !handsText.enabled;
                }
            }
        }
    }

    private void GetPlayerInput()
    {
        if (isLocalPlayer)
        {
            RPS_GameHand hand = RPS_GameHand.None;

            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
                hand = RPS_GameHand.Rock;

            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
                hand = RPS_GameHand.Paper;

            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
                hand = RPS_GameHand.Scissors;

            if (hand != RPS_GameHand.None)
            {
                CmdSetPlayerInput(hand);
            }
        }
    }

    [Command] // Run this method on the instance of this object that is on the server machine
    private void CmdSetPlayerInput(RPS_GameHand handPlayed)
    {
        if (debug_Default)
            Debug.Log("# " + name + ": " + GetType() + ": CmdSetPlayerInput(handPlayed=" + handPlayed + ") [netId=" + netId.Value + "][isServer=" + isServer + "][isLocalPlayer=" + isLocalPlayer + "]");

        // if running on server machine...
        if (isServer)
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

            // then step though player objects until...
            foreach (GameObject player in players)
            {
                RPS_Server serverComponent = player.GetComponent<RPS_Server>();

                // the object running the server is found then...
                if (serverComponent.isLocalPlayer)
                {
                    if (debug_Default)
                        Debug.Log(" ... " + name + ": " + GetType() + ": CmdSetPlayerInput(handPlayed=" + handPlayed + "): ServerObject='" + player.name + "' [netId=" + netId.Value + "][isServer=" + isServer + "][isLocalPlayer=" + isLocalPlayer + "]");

                    // send hand played to the server component which is handling the gameplay logic
                    player.GetComponent<RPS_Server>().CmdSetPlayerHand(netId.Value, handPlayed);
                }
            }
        }
    }

    [ClientRpc] // Run this method on both (all) instances of this object 
    public void RpcSetGameState(RPS_GameState state)
    {
        if (debug_Default)
            Debug.Log("# " + name + ": " + GetType() + ": RpcSetGameState(state=" + state + ") [netId=" + netId.Value + "][isServer=" + isServer + "][isLocalPlayer=" + isLocalPlayer + "]");

        gameState = state;

        if (isLocalPlayer)
        {
            Update_PlayerDisplay();
        }
    }

    [ClientRpc] // Run this method on both (all) instances of this object 
    public void RpcSetGameData(RPS_PlayerData data)
    {
        if (debug_Default)
            Debug.Log("# " + name + ": " + GetType() + ": RpcSetGameData() [netId=" + netId.Value + "][isServer=" + isServer + "][isLocalPlayer=" + isLocalPlayer + "]");

        if (isLocalPlayer)
        {
            if (data.playerId == netId.Value)
                myPlayerData = data;
            else
                otherPlayerData = data;

            Update_PlayerDisplay();
        }
    }

    [ClientRpc] // Run this method on both (all) instances of this object 
    public void RpcSetOutcomeMessage(string message)
    {
        if (debug_Default)
            Debug.Log("# " + name + ": " + GetType() + ": RpcSetOutcomeMessage(message='" + message + "') [netId=" + netId.Value + "][isServer=" + isServer + "][isLocalPlayer=" + isLocalPlayer + "]");

        if (isLocalPlayer)
        {
            outcomeMessage = message;
            Update_PlayerDisplay();
        }
    }


    private string outcomeMessage;

    private void Update_PlayerDisplay()
    {
        if (debug_Default)
            Debug.Log("# " + name + ": " + GetType() + ": Update_PlayerDisplay() [netId=" + netId.Value + "][isServer=" + isServer + "][isLocalPlayer=" + isLocalPlayer + "]");

        if (isLocalPlayer)
        {
            scoreText.text = " Score: You=" + myPlayerData.score + " vs Other=" + otherPlayerData.score;

            string gamePlayInfo = "";
            switch (gameState)
            {
                case RPS_GameState.Paused:
                    gamePlayInfo = "Waiting for Other Player To Connect...";
                    break;

                case RPS_GameState.Play:
                    if (myPlayerData.hand != RPS_GameHand.None)
                        gamePlayInfo = "You threw " + myPlayerData.hand + "! \nWaiting for Other Player...";
                    else
                        gamePlayInfo = "Make Your Move.";
                    break;

                case RPS_GameState.Waiting:
                    gamePlayInfo = outcomeMessage;
                    break;
            }

            infoText.text = gamePlayInfo;

            handsText.text = "DEBUG (Hands: You=" + myPlayerData.hand + " vs Other=" + otherPlayerData.hand + ")";
        }
    }

}