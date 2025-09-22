using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using Diagnostics = System.Diagnostics;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;


public static class TransformExtensions
{
    public static Vector3 TransformPointUnscaled(this Transform transform, Vector3 position)
    {
        var localToWorldMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        return localToWorldMatrix.MultiplyPoint3x4(position);
    }

    public static Vector3 InverseTransformPointUnscaled(this Transform transform, Vector3 position)
    {
        var worldToLocalMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one).inverse;
        return worldToLocalMatrix.MultiplyPoint3x4(position);
    }
}


public class SocketClient : MonoBehaviour
{
    private SocketConfig socket_config;

    // Camera from which to take the RGB image.
    public GameObject wristCamera;
    // External camera to have a view both on the object and the hand. 
    // Used for visualization only
    public GameObject externalCamera;
    // Target object, we need it because we will send to the client the 
    // camera to object pose
    public GameObject targetObject;

    // The wrist object for reading/writing ps and fe.
    public GameObject wrist;
    private float newPsEulDeg;
    private float newFeEulDeg;

    private bool requestSent = false;

    // Create necessary TcpClient objects
    private TcpClient client;
    // It will contain the tcp or udp client
    private IDisposable generalClient;

    private Thread receiveThread;

    // To get the rgb image 
    private RenderTexture renderTexture;

    private List<double> meanFPS = new List<double>();
    private int COUNTER_MAX_VALUE = 50;

    private Diagnostics.Stopwatch responseToResponse = null;
    private List<float> respToRespMeanFPS = new List<float>();
    private Diagnostics.Stopwatch requestToRequest = null;
    private List<float> reqToReqMeanFPS = new List<float>();


    private Texture2D CaptureUnityView(Camera camera)
    {
        camera.targetTexture = renderTexture;
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = renderTexture;
        camera.Render();

        Texture2D mainCameraTexture = new Texture2D(
            renderTexture.width,
            renderTexture.height
        );
        mainCameraTexture.ReadPixels(
            new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0
        );
        mainCameraTexture.Apply();

        RenderTexture.active = currentRT;
        camera.targetTexture = null;

        return mainCameraTexture;
    }

    private void ReceiveData()
    {
        NetworkStream stream = client.GetStream();

        int countElementsReceived = 0;
        List<object> elementsReceived = new List<object>();

        int bytesReadHeader = 0;
        int bytesReadPayloadLength = 0;
        int bytesReadPayload = 0;

        int payloadLength = -1;

        byte[] rawHeader = new byte[socket_config.SOCKET.header_num_bytes];
        byte[] rawPayloadLength = new byte[socket_config.SOCKET.payloadLength_num_bytes];
        byte[] rawPayload = null;

        string header = null;
        object payload = null;
        int remainingAttempts = 10000;
        while (true)
        {
            try
            {
                // Buffer to store the response bytes.
                byte[] data = null;
                Int32 curBytesRead = 0;
                data = new Byte[socket_config.SOCKET.block_size];
                // Read the current batch of the python module response bytes.
                curBytesRead = stream.Read(data, 0, data.Length);
                if (curBytesRead == 0)
                    remainingAttempts -= 1;
                else
                    remainingAttempts = 10000;
                if (remainingAttempts == 0)
                    throw new Exception(
                        "Not receiving data from the server, maybe an error " + 
                        "on the server side? Closing connection"
                    );

                for (int i = 0; i < curBytesRead; i++)
                {
                    if (bytesReadHeader < socket_config.SOCKET.header_num_bytes)
                    {
                        rawHeader[bytesReadHeader] = data[i];
                        bytesReadHeader++;
                    }
                    else if (bytesReadPayloadLength < socket_config.SOCKET.payloadLength_num_bytes)
                    {
                        rawPayloadLength[bytesReadPayloadLength] = data[i];
                        bytesReadPayloadLength++;

                        if (bytesReadPayloadLength == socket_config.SOCKET.payloadLength_num_bytes)
                        {
                            // Convert payload length bytes to int
                            if (BitConverter.IsLittleEndian)
                                Array.Reverse(rawPayloadLength);
                            payloadLength = BitConverter.ToInt32(rawPayloadLength, 0);

                            rawPayload = new byte[payloadLength];
                        }
                    }
                    else if (bytesReadPayload < payloadLength)
                    {
                        rawPayload[bytesReadPayload] = data[i];

                        bytesReadPayload++;

                        // When all payload is read, convert it from raw bytes
                        // to C# type according to the header specified
                        if (bytesReadPayload == payloadLength)
                        {
                            // Convert header bytes to string
                            header = Encoding.UTF8.GetString(rawHeader).Trim();

                            // Convert payload bytes to the type defined in the header

                            if (header == "string")
                            {
                                payload = Encoding.UTF8.GetString(rawPayload);
                            }
                            else if (header == "image")
                            {
                                throw new Exception(
                                    "Currently not implemented: there is no need " +
                                    "to receive an image back from the python module"
                                );
                            }
                            else if (header == "vector3")
                            {
                                float x, y, z;
                                if (BitConverter.IsLittleEndian)
                                {
                                    Array.Reverse(rawPayload);
                                    x = BitConverter.ToSingle(rawPayload, 8);
                                    y = BitConverter.ToSingle(rawPayload, 4);
                                    z = BitConverter.ToSingle(rawPayload, 0);
                                }
                                else
                                {
                                    x = BitConverter.ToSingle(rawPayload, 0);
                                    y = BitConverter.ToSingle(rawPayload, 4);
                                    z = BitConverter.ToSingle(rawPayload, 8);
                                }

                                payload = new Vector3(x, y, z);
                            }
                            else if (header == "float")
                            {
                                if (BitConverter.IsLittleEndian)
                                    Array.Reverse(rawPayload);
                                payload = BitConverter.ToSingle(rawPayload, 0);
                            }
                            else
                            {
                                throw new Exception("Header does not exist: " + header);
                            }

                            elementsReceived.Add(payload);
                            countElementsReceived += 1;

                            // Reset stuff about packet reading
                            bytesReadHeader = 0;
                            bytesReadPayloadLength = 0;
                            bytesReadPayload = 0;

                            // When all the elements are received, store them in proper 
                            // variables and they will be used to update ps and fe angles
                            // in the next Update() function call
                            if (countElementsReceived == socket_config.COMMUNICATION.client_elements_to_receive.Count)
                            {
                                if (responseToResponse == null)
                                {
                                    // Enter here only once, since that viariable
                                    // is initialized to null only at the beginning of
                                    // the script
                                    responseToResponse = new Diagnostics.Stopwatch();
                                    responseToResponse.Start();
                                }
                                else
                                {
                                    responseToResponse.Stop();
                                    respToRespMeanFPS.Add(
                                        1.0f / (responseToResponse.ElapsedMilliseconds / 1000.0f)
                                    );
                                    if (respToRespMeanFPS.Count == COUNTER_MAX_VALUE)
                                    {
                                        Debug.Log("response to response FPS: "
                                                  + respToRespMeanFPS.Sum() / respToRespMeanFPS.Count);
                                        respToRespMeanFPS.Clear();
                                    }
                                    responseToResponse.Restart();
                                }

                                float psThetaDotRadS = (float)elementsReceived[0];
                                float feThetaDotRadS = (float)elementsReceived[1];

                                // revert the sign since it is the opposite
                                // between unity ref frame and the one defined 
                                // in our robot model
                                psThetaDotRadS *= -1;

                                float deltaPsThetaDeg = psThetaDotRadS * socket_config.sampleTime * Mathf.Rad2Deg;
                                float deltaFeThetaDeg = feThetaDotRadS * socket_config.sampleTime * Mathf.Rad2Deg;

                                newPsEulDeg += deltaPsThetaDeg;
                                newFeEulDeg += deltaFeThetaDeg;

                                requestSent = false;

                                countElementsReceived = 0;
                                elementsReceived.Clear();
                            }

                        }
                    }
                    else
                    {
                        throw new Exception(
                            "Something went wrong while reading the data sent " +
                            "by the server"
                        );
                    }
                }

            }
            catch (Exception err)
            {
                Debug.LogError(err.ToString());
                Debug.LogError(
                    "Exception occurred while receiving the data. " + 
                    "Please stop the execution from the Unity Editor"
                );
                stream.Close(0);
                break;
            }
        }
    }

    void Awake()
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance)
            .Build();
        socket_config = deserializer.Deserialize<SocketConfig>(
            File.ReadAllText("Python/src/socket/socket_config.yaml")
        );

        // Keep fps fixed
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = socket_config.fps;

        // Create local client
        client = new TcpClient(socket_config.SOCKET.ip_server, socket_config.SOCKET.port_server);

        generalClient = client;

        //client.NoDelay = true;

        // Create a new thread for receiving incoming messages
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    void Start()
    {
        Camera cameraToStream = wristCamera.GetComponent<Camera>();
        renderTexture = new RenderTexture(
            cameraToStream.pixelWidth,
            cameraToStream.pixelHeight,
            24,
            UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB
        );
        renderTexture.Create();

        newPsEulDeg = wrist.transform.localEulerAngles.y;
        newFeEulDeg = wrist.transform.localEulerAngles.x;
    }

    void Update()
    {
        if (requestSent)
        {
            return;
        }

        requestSent = true;

        if (requestToRequest == null)
        {
            // enter here only the first time since it is initialized to null
            requestToRequest = new Diagnostics.Stopwatch();
            requestToRequest.Start();
        }
        else
        {
            requestToRequest.Stop();
            reqToReqMeanFPS.Add(1.0f / (requestToRequest.ElapsedMilliseconds / 1000.0f));
            if (reqToReqMeanFPS.Count == COUNTER_MAX_VALUE)
            {
                //Debug.Log("request to request FPS: " + reqToReqMeanFPS.Sum() / reqToReqMeanFPS.Count);
                reqToReqMeanFPS.Clear();
            }
            requestToRequest.Restart();
        }

        wrist.transform.localEulerAngles = new Vector3(newFeEulDeg, newPsEulDeg, 0);

        // SEND CAMERA TO OBJECT POSE
        Vector3 Tc2o = TransformExtensions.InverseTransformPointUnscaled(
            wristCamera.transform, targetObject.transform.position
        );
        Sender.SendVector3(Tc2o, generalClient, socket_config.SOCKET.header_num_bytes);
        Quaternion Rc2o = Quaternion.Inverse(wristCamera.transform.rotation) * targetObject.transform.rotation;
        Sender.SendVector3(Rc2o.eulerAngles, generalClient, socket_config.SOCKET.header_num_bytes);

        // SEND CURRENT PS AND FE ANGLES
        // the ps sign is reverted in order to convert the unity ref frame to 
        // the one used by out model
        Sender.SendFloat(
            wrist.transform.localEulerAngles.y * -1, generalClient, socket_config.SOCKET.header_num_bytes
        );
        Sender.SendFloat(
            wrist.transform.localEulerAngles.x, generalClient, socket_config.SOCKET.header_num_bytes
        );

        // SEND WRIST CAMERA RGB IMAGE 
        Texture2D wristView = CaptureUnityView(wristCamera.GetComponent<Camera>());
        Sender.SendImage(wristView, generalClient, socket_config.SOCKET.block_size, socket_config.SOCKET.header_num_bytes);

        // SEND EXTERNAL CAMERA RGB IMAGE 
        Texture2D externalView = CaptureUnityView(externalCamera.GetComponent<Camera>());
        Sender.SendImage(externalView, generalClient, socket_config.SOCKET.block_size, socket_config.SOCKET.header_num_bytes);
    }


    void OnDisable()
    {
        if (receiveThread != null)
            receiveThread.Abort();

        if (generalClient.GetType() == typeof(TcpClient))
        {
            ((TcpClient)generalClient).Close();
        }
        else
        {
            throw new NotImplementedException();
        }
    }

}