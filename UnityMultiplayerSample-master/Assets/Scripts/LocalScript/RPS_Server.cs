using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


public enum RPS_GameState { Setup, Paused, Play, Waiting }
public enum RPS_GameHand { None, Rock, Paper, Scissors }

public struct RPS_PlayerData
{
    public uint playerId;
    public int score;
    public RPS_GameHand hand;
};

public class RPS_Server : NetworkBehaviour
{

    public bool debug_Default = false;

    private RPS_GameState gameState;

    private List<RPS_PlayerData> playerDataList;

    private void Awake()
    {
        if (debug_Default)
            Debug.Log("# " + name + ": " + GetType() + ": Awake() [netId=" + netId.Value + "][isServer=" + isServer + "][isLocalPlayer=" + isLocalPlayer + "]");

        gameState = RPS_GameState.Paused;
    }

    void Start()
    {
        playerDataList = new List<RPS_PlayerData>();
    }

    public override void OnStartClient()
    {
        if (debug_Default)
            Debug.Log("# " + name + ": " + GetType() + ": OnStartClient() [netId=" + netId.Value + "][isServer=" + isServer + "][isLocalPlayer=" + isLocalPlayer + "]");

        StartCoroutine(WaitForTwoPlayers());
    }


    private void ProcessPlayerMove()
    {
        uint playerId = netId.Value;
        uint otherPlayerId = GetOtherPlayerId(netId.Value);

        RPS_GameHand playerHand = GetPlayerData(playerId).hand;
        RPS_GameHand otherPlayerHand = GetPlayerData(otherPlayerId).hand;

        if (debug_Default)
            Debug.Log("### " + name + ": " + GetType() + ": ProcessPlayerMove(): [(playerHand=" + playerHand + ", otherPlayerHand=" + otherPlayerHand + ")] [netId=" + netId.Value + "][isLocalPlayer=" + isLocalPlayer + "][isServer=" + isServer + "] Starting...", this);

        if (playerHand != RPS_GameHand.None && otherPlayerHand != RPS_GameHand.None && gameState == RPS_GameState.Play)
        {
            uint winningId = uint.MaxValue;

            gameState = RPS_GameState.Waiting;


            // Rock > Scissors
            if (playerHand == RPS_GameHand.Rock && otherPlayerHand == RPS_GameHand.Scissors)
            {
                winningId = playerId;
            }
            // Paper > Rock
            if (playerHand == RPS_GameHand.Paper && otherPlayerHand == RPS_GameHand.Rock)
            {
                winningId = playerId;
            }
            // Scissors > Paper
            if (playerHand == RPS_GameHand.Scissors && otherPlayerHand == RPS_GameHand.Paper)
            {
                winningId = playerId;
            }


            // Rock > Scissors
            if (otherPlayerHand == RPS_GameHand.Rock && playerHand == RPS_GameHand.Scissors)
            {
                winningId = otherPlayerId;
            }
            // Paper > Rock
            if (otherPlayerHand == RPS_GameHand.Paper && playerHand == RPS_GameHand.Rock)
            {
                winningId = otherPlayerId;
            }
            // Scissors > Paper
            if (otherPlayerHand == RPS_GameHand.Scissors && playerHand == RPS_GameHand.Paper)
            {
                winningId = otherPlayerId;
            }


            // give points and build results message
            if (winningId == playerId)
            {
                CmdAddPlayerScore(playerId, 1);
                ShowOutcome(playerId, playerHand, otherPlayerId, otherPlayerHand);
            }
            if (winningId == otherPlayerId)
            {
                CmdAddPlayerScore(otherPlayerId, 1);
                ShowOutcome(otherPlayerId, otherPlayerHand, playerId, playerHand);
            }
            if (winningId == uint.MaxValue)
            {
                ShowOutcome(uint.MaxValue, otherPlayerHand, uint.MaxValue, playerHand);
            }


            // pause so players can see round results
            StartCoroutine(PauseThenNextRound());
        }
    }

    private IEnumerator PauseThenNextRound()
    {
        if (debug_Default)
            Debug.Log("# " + name + ": " + GetType() + ": PauseThenNextRound() [netId=" + netId.Value + "][isServer=" + isServer + "][isLocalPlayer=" + isLocalPlayer + "]");

        gameState = RPS_GameState.Waiting;
        BroadcastGameState();

        yield return new WaitForSeconds(1);

        CmdSetPlayerHand(netId.Value, RPS_GameHand.None);
        CmdSetPlayerHand(GetOtherPlayerId(netId.Value), RPS_GameHand.None);

        yield return new WaitForSeconds(5);

        gameState = RPS_GameState.Play;
        BroadcastGameState();
    }

    private void ShowOutcome(uint winnerId, RPS_GameHand winningHand, uint losingId, RPS_GameHand losingHand)
    {
        uint playerId;
        string results;

        playerId = netId.Value;
        results = BuildResultsMessage(playerId, winnerId, winningHand, losingId, losingHand);
        BroadcastOutcomeMessageToPlayer(playerId, results);

        playerId = GetOtherPlayerId(netId.Value);
        results = BuildResultsMessage(playerId, winnerId, winningHand, losingId, losingHand);
        BroadcastOutcomeMessageToPlayer(playerId, results);
    }

    private string BuildResultsMessage(uint playerId, uint winnerId, RPS_GameHand winningHand, uint losingId, RPS_GameHand losingHand)
    {
        string outcomeMessage;
        outcomeMessage = "--- Tie Round ---";
        outcomeMessage += "\n< " + winningHand + " vs " + losingHand + " >";

        if (playerId == winnerId)
        {
            outcomeMessage = "!!! You WIN !!!";
            outcomeMessage += "\n< " + winningHand + " vs " + losingHand + " >";
        }
        if (playerId == losingId)
        {
            outcomeMessage = "... You LOSE ...";
            outcomeMessage += "\n< " + losingHand + " vs " + winningHand + " >";
        }

        return outcomeMessage;
    }



    [Command]
    public void CmdSetPlayerHand(uint playerId, RPS_GameHand hand)
    {
        if (debug_Default)
            Debug.Log("# " + name + ": " + GetType() + ": CmdSetPlayerHand(playerId=" + playerId + ", hand=" + hand + ") [netId=" + netId.Value + "][isServer=" + isServer + "][isLocalPlayer=" + isLocalPlayer + "]");

        if (isServer && isLocalPlayer)
        {
            RPS_PlayerData data = GetPlayerData(playerId);

            if (data.hand == RPS_GameHand.None || hand == RPS_GameHand.None)
            {
                data.hand = hand;
                UpdatePlayerData(data);

                BroadcastPlayerData();

                ProcessPlayerMove();
            }
        }
    }

    [Command]
    public void CmdAddPlayerScore(uint playerId, int score)
    {
        if (debug_Default)
            Debug.Log("# " + name + ": " + GetType() + ": CmdAddPlayerScore(playerId=" + playerId + ", score=" + score + ") [netId=" + netId.Value + "][isServer=" + isServer + "][isLocalPlayer=" + isLocalPlayer + "]");

        if (isServer && isLocalPlayer)
        {
            RPS_PlayerData data = GetPlayerData(playerId);
            data.score += score;
            UpdatePlayerData(data);

            BroadcastPlayerData();
        }
    }

    private void UpdatePlayerData(RPS_PlayerData data)
    {
        if (debug_Default)
            Debug.Log("# " + name + ": " + GetType() + ": UpdatePlayerData() [netId=" + netId.Value + "][isServer=" + isServer + "][isLocalPlayer=" + isLocalPlayer + "]");

        for (int i = 0; i < playerDataList.Count; i++)
        {
            if (playerDataList[i].playerId == data.playerId)
            {
                playerDataList[i] = data;
                return;
            }
        }

        playerDataList.Add(data);
    }

    private uint GetOtherPlayerId(uint playerId)
    {
        if (debug_Default)
            Debug.Log("# " + name + ": " + GetType() + ": GetOtherPlayerId(playerId=" + playerId + ") [netId=" + netId.Value + "][isServer=" + isServer + "][isLocalPlayer=" + isLocalPlayer + "]");

        foreach (RPS_PlayerData player in playerDataList)
        {
            if (player.playerId != playerId)
                return player.playerId;
        }

        return uint.MaxValue;
    }

    private RPS_PlayerData GetPlayerData(uint playerId, bool getThisPlayerId = true)
    {
        if (debug_Default)
            Debug.Log("# " + name + ": " + GetType() + ": GetPlayerData() [netId=" + netId.Value + "][isServer=" + isServer + "][isLocalPlayer=" + isLocalPlayer + "]");

        foreach (RPS_PlayerData player in playerDataList)
        {
            if (player.playerId == playerId)
                return player;
        }

        RPS_PlayerData newPlayerData = new RPS_PlayerData();
        newPlayerData.playerId = playerId;
        newPlayerData.score = 0;
        newPlayerData.hand = RPS_GameHand.None;

        return newPlayerData;
    }


    private void BroadcastPlayerData()
    {
        if (debug_Default)
            Debug.Log("# " + name + ": " + GetType() + ": BroadcastPlayerData(): [isLocalPlayer=" + isLocalPlayer + "] Starting...");

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject player in players)
        {
            RPS_Player playerComponent = player.GetComponent<RPS_Player>();

            foreach (RPS_PlayerData data in playerDataList)
            {
                playerComponent.RpcSetGameData(data);
            }
        }
    }

    private void BroadcastGameState()
    {
        if (debug_Default)
            Debug.Log("# " + name + ": " + GetType() + ": BroadcastGameState(): [isLocalPlayer=" + isLocalPlayer + "] Starting...");

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject player in players)
        {
            RPS_Player playerComponent = player.GetComponent<RPS_Player>();

            playerComponent.RpcSetGameState(gameState);
        }
    }

    private void BroadcastOutcomeMessageToPlayer(uint id, string message)
    {
        if (debug_Default)
            Debug.Log("# " + name + ": " + GetType() + ": BroadcastOutcomeMessageToPlayer(): [isLocalPlayer=" + isLocalPlayer + "] Starting...");

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject player in players)
        {
            RPS_Player playerComponent = player.GetComponent<RPS_Player>();
            uint pId = playerComponent.netId.Value;

            if (pId == id)
                playerComponent.RpcSetOutcomeMessage(message);
        }
    }

    private IEnumerator WaitForTwoPlayers()
    {
        if (debug_Default)
            Debug.Log("# " + name + ": " + GetType() + ": WaitForTwoPlayer() [netId=" + netId.Value + "][isServer=" + isServer + "][isLocalPlayer=" + isLocalPlayer + "]");

        yield return new WaitForSeconds(1);

        if (debug_Default)
            Debug.Log(" ... " + name + ": " + GetType() + ": WaitForTwoPlayer(): Pre-Valid Test [netId=" + netId.Value + "][isServer=" + isServer + "][isLocalPlayer=" + isLocalPlayer + "]");

        if (isServer && isLocalPlayer)
        {
            while (CountObjectsWithPlayerTag() < 2)
            {
                if (debug_Default)
                    Debug.Log(" ... " + name + ": " + GetType() + ": WaitForTwoPlayer(): Counting... [netId=" + netId.Value + "][isServer=" + isServer + "][isLocalPlayer=" + isLocalPlayer + "]");

                yield return new WaitForSeconds(1);
            }

            // start game
            gameState = RPS_GameState.Play;
            BroadcastGameState();
        }
    }

    private int CountObjectsWithPlayerTag()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        if (debug_Default)
            Debug.Log("# " + name + ": " + GetType() + ": GetPlayerTagedObjectCount() count=" + players.Length + " [isServer=" + isServer + "][isLocalPlayer=" + isLocalPlayer + "]");

        return players.Length;
    }

}
