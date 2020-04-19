using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class oPlayer : MonoBehaviour
{
    Rigidbody rb;
    oClient client;
   

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        client = FindObjectOfType<oClient>();
    }

    // Update is called once per frame
    void Update()
    {
        //Send input to server
        float xMov = Input.GetAxis("Horizontal");
        float yMov = Input.GetAxis("Vertical");
        transform.Translate(xMov, 0, yMov);

        if (xMov != 0 || yMov !=0)
        {
            string msg = "MOVE|" + xMov.ToString() + "|" + yMov.ToString();
            client.sendMessage(msg);
        }

    }
}
