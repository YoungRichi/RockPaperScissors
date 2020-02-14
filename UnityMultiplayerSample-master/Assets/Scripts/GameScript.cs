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

    [SerializeField] Transform player;
    [SerializeField] Transform ai;
 
    [SerializeField] GameObject playerWinTxt;
    [SerializeField] GameObject aiWinTxt;
    [SerializeField] GameObject playerLoseTxt;
    [SerializeField] GameObject aiLoseTxt;
    [SerializeField] GameObject playerDrawTxt;
    [SerializeField] GameObject aiDrawTxt;

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

    void GenerateAISelection()
    {
        int random = UnityEngine.Random.Range(1, 4);

        aiChoose = random;

        if (aiChoose == (int)States.Rock)
        {
             var choice = Instantiate(rock, ai);
             Destroy(choice, 2.0f);
        }
        else if (aiChoose == (int)States.Scissors)
        {
            var choice = Instantiate(scissors, ai);
            Destroy(choice, 2.0f);
        }
        else if (aiChoose == (int)States.Paper)
        {
            var choice = Instantiate(paper, ai);
            Destroy(choice, 2.0f);
        }
        
    }
    

    void ChooseWinner()
    {
        //Deactivates all the win/lost texts
        aiDrawTxt.SetActive(false);
        playerDrawTxt.SetActive(false);
        aiLoseTxt.SetActive(false);
        aiWinTxt.SetActive(false);
        playerLoseTxt.SetActive(false);
        playerWinTxt.SetActive(false);


        //Activates win/lose text based on win/lose condition
        if (playerChoose == aiChoose)
        {
            //draw
            aiDrawTxt.SetActive(true);
            playerDrawTxt.SetActive(true);
        }
        else if (playerChoose == (int)States.Paper && aiChoose == (int)States.Rock)
        {
            //player won
            aiLoseTxt.SetActive(true);
            playerWinTxt.SetActive(true);
        }
        else if (playerChoose == (int)States.Paper && aiChoose == (int)States.Scissors)
        {
            //ai won
            aiWinTxt.SetActive(true);
            playerLoseTxt.SetActive(true);
        }
        else if (playerChoose == (int)States.Rock && aiChoose == (int)States.Scissors)
        {
            //player won
            aiLoseTxt.SetActive(true);
            playerWinTxt.SetActive(true);
        }
        else if (playerChoose == (int)States.Rock && aiChoose == (int)States.Paper)
        {
            //ai won
            aiWinTxt.SetActive(true);
            playerLoseTxt.SetActive(true);
        }
        else if (playerChoose == (int)States.Scissors && aiChoose == (int)States.Rock)
        {
            //ai won
            aiWinTxt.SetActive(true);
            playerLoseTxt.SetActive(true);
        }
        else if (playerChoose == (int)States.Scissors && aiChoose == (int)States.Paper)
        {
            //player won
            aiLoseTxt.SetActive(true);
            playerWinTxt.SetActive(true);
        }
        playerChoose = 0;
        playersTurn = true;
    }

    //Calls every time one of the buttons is pressed
    public void ProcessPlayerSelection(int choose)
    {
        playerChoose = choose;
        playersTurn = false;
        if (playerChoose == (int)States.Rock)
        {
            var choice = Instantiate(rock, player);
            Destroy(choice, 2.0f);
        }

        else if (playerChoose == (int)States.Scissors)
        {
            var choice = Instantiate(scissors, player);
            Destroy(choice, 2.0f);
        }

        else if (playerChoose == (int)States.Paper)
        {
            var choice = Instantiate(paper, player);
            Destroy(choice, 2.0f);
        }

    }
}
