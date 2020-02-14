using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum States
{
    Rock = 1,
    Scissors = 2,
    Paper = 3
}

public class GameScript : MonoBehaviour
{
    [SerializeField] GameObject rock;
    [SerializeField] GameObject scissors;
    [SerializeField] GameObject paper;

    [SerializeField] GameObject text;

    int playerChoose = 0;
    int aiChoose = 0;

    bool playersTurn = true;
    


    // Update is called once per frame
    void Update()
    {

        if (playersTurn && playerChoose <= 0)
            return;

        else
        {
            GenerateAISelection();
            ChooseWinner();
        }
    }

    public void ProcessPlayerSelection(int choose)
    {
        playerChoose = choose;
        playersTurn = false;
        if(playerChoose == 1)
            Debug.Log("Player: Rock");

        else if (playerChoose == 2)
            Debug.Log("Player: Scissors");

        else if (playerChoose == 3)
            Debug.Log("Player: Paper");

    }

    void GenerateAISelection()
    {
        int random = UnityEngine.Random.Range(1, 4);

        aiChoose = random;

        if (aiChoose == 1)
        {
             var choice = Instantiate(rock);
             Destroy(choice, 2.0f);
        }
        else if (aiChoose == 2)
        {
            var choice = Instantiate(scissors);
            Destroy(choice, 2.0f);
        }
        else if (aiChoose == 3)
        {
            var choice = Instantiate(paper);
            Destroy(choice, 2.0f);
        }
        
    }

    

    void ChooseWinner()
    {
        if (playerChoose == aiChoose)
        {
            //draw
        }
        else if (playerChoose == (int)States.Paper && aiChoose == (int)States.Rock)
        {
            //player won
        }
        else if (playerChoose == (int)States.Paper && aiChoose == (int)States.Scissors)
        {
            //ai won
        }
        else if (playerChoose == (int)States.Rock && aiChoose == (int)States.Scissors)
        {
            //player won
        }
        else if (playerChoose == (int)States.Rock && aiChoose == (int)States.Paper)
        {
            //ai won
        }
        else if (playerChoose == (int)States.Scissors && aiChoose == (int)States.Rock)
        {
            //ai won
        }
        else if (playerChoose == (int)States.Scissors && aiChoose == (int)States.Paper)
        {
            //player won
        }
        playerChoose = 0;
        playersTurn = true;
    }
}
