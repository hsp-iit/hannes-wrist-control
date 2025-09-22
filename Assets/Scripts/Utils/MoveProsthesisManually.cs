using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveProsthesisManually : MonoBehaviour
{
    public float speedTranslation = 0.01f;
    public float speedRotation = 1.0f;

    // Start is called before the first frame update
    void Start()
    {
        
    }


    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey("up"))
        {
            transform.position += new Vector3(0.0f, speedTranslation, 0.0f);
        }
        else if (Input.GetKey("down"))
        {
            transform.position += new Vector3(0.0f, -speedTranslation, 0.0f);
        }
        else if (Input.GetKey("left"))
        {
            transform.position += new Vector3(-speedTranslation, 0.0f, 0.0f);
        }
        else if (Input.GetKey("right"))
        {
            transform.position += new Vector3(speedTranslation, 0.0f, 0.0f);
        }
        else if (Input.GetKey("w"))
        {
            transform.rotation *= Quaternion.Euler(speedRotation, 0.0f, 0.0f);
        }
        else if (Input.GetKey("a"))
        {
            transform.rotation *= Quaternion.Euler(0.0f, speedRotation, 0.0f);
        }
        else if (Input.GetKey("s"))
        {
            transform.rotation *= Quaternion.Euler(-speedRotation, 0.0f, 0.0f);
        }
        else if (Input.GetKey("d"))
        {
            transform.rotation *= Quaternion.Euler(0.0f, -speedRotation, 0.0f);
        }
        else if (Input.GetKey("i"))
        {
            transform.position += new Vector3(0.0f, 0.0f, speedTranslation);
        }
        else if (Input.GetKey("j"))
        {
            transform.rotation *= Quaternion.Euler(0.0f, 0.0f, speedRotation);
        }
        else if (Input.GetKey("k"))
        {
            transform.position += new Vector3(0.0f, 0.0f, -speedTranslation);
        }
        else if (Input.GetKey("l"))
        {
            transform.rotation *= Quaternion.Euler(0.0f, 0.0f, -speedRotation);
        }


    }
}
