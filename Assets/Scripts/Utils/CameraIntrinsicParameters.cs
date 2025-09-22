using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Attach this script to a Camera to get its intrinsics parameter

[RequireComponent(typeof(Camera))]
public class CameraInstrinsicParameters : MonoBehaviour
{
    private Camera cam;

    void getIntrinsicParameters()
    {
        float pixel_aspect_ratio = (float)cam.pixelWidth / (float)cam.pixelHeight;

        float alpha_u = cam.focalLength * ((float)cam.pixelWidth / cam.sensorSize.x);
        float alpha_v = cam.focalLength * pixel_aspect_ratio * ((float)cam.pixelHeight / cam.sensorSize.y);

        float u_0 = (float)cam.pixelWidth / 2;
        float v_0 = (float)cam.pixelHeight / 2;

        //IntrinsicMatrix in row major
        Debug.Log(alpha_u + "  " + 0f + "  " + u_0);
        Debug.Log(0f + "  " + alpha_v + "  " + v_0);
        Debug.Log(0f + "  " + 0f + "  " + 1f);
    }


    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>();

        getIntrinsicParameters();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
