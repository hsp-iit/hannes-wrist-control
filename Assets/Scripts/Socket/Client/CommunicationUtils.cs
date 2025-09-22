using UnityEngine;
using System.Collections;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.IO.Compression;


public static class ImageUtils
{
    public static byte[] ConvertTexture2DToByteArray(Texture2D texture)
    {
        byte[] imageBytes = texture.EncodeToJPG();
        return imageBytes;
    }
}


public static class Sender
{
    /* Send string to python module via tcp socket
     */
    private static void SendString(string message, TcpClient client, int headerNumBytes)  
    {
        // Create header 
        string header = "string".PadRight(headerNumBytes);  
        byte[] rawHeader = Encoding.UTF8.GetBytes(header);

        // Convert message to byte array
        byte[] rawPayload = Encoding.UTF8.GetBytes(message);

        // Create payload length
        int payloadLength = rawPayload.Length;
        byte[] rawPayloadLength = BitConverter.GetBytes(payloadLength);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(rawPayloadLength);

        // Send bytes
        Sender.SendBytes(rawHeader, client);
        Sender.SendBytes(rawPayloadLength, client);
        Sender.SendBytes(rawPayload, client);
    }

    public static void SendString(string message, IDisposable client, int headerNumBytes)
    {
        if (client.GetType() == typeof(TcpClient))
        {
            SendString(message, (TcpClient)client, headerNumBytes);
        }
        else
        {
            throw new NotImplementedException();
        }

    }

    /* Send image to python module via tcp socket
     */
    private static void SendImage(Texture2D image, TcpClient client, int headerNumBytes)
    {
        // Create header
        string header = "image".PadRight(headerNumBytes);
        byte[] rawHeader = Encoding.UTF8.GetBytes(header);

        // Convert to byte array
        byte[] rawPayload = ImageUtils.ConvertTexture2DToByteArray(image);

        // Create payload length
        int payloadLength = rawPayload.Length;
        byte[] rawPayloadLength = BitConverter.GetBytes(payloadLength);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(rawPayloadLength);

        // Send bytes
        Sender.SendBytes(rawHeader, client);
        Sender.SendBytes(rawPayloadLength, client);
        Sender.SendBytes(rawPayload, client);
    }

    public static void SendImage(Texture2D image, IDisposable client, int blockSize, int headerNumBytes)
    {
        if (client.GetType() == typeof(TcpClient))
        {
            SendImage(image, (TcpClient)client, headerNumBytes);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    /* Send Vector3 to python module via tcp socket
        */
    private static void SendVector3(Vector3 vector, TcpClient client, int headerNumBytes)
    {
        // Create header
        string header = "vector3".PadRight(headerNumBytes); 
        byte[] rawHeader = Encoding.UTF8.GetBytes(header);

        // Convert to byte array
        byte[] rawX = BitConverter.GetBytes(vector.x);
        byte[] rawY = BitConverter.GetBytes(vector.y);
        byte[] rawZ = BitConverter.GetBytes(vector.z);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(rawX);
            Array.Reverse(rawY);
            Array.Reverse(rawZ);
        }

        // Create payload length
        int payloadLength = rawX.Length + rawY.Length + rawZ.Length;
        byte[] rawPayloadLength = BitConverter.GetBytes(payloadLength);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(rawPayloadLength);

        // Send bytes
        Sender.SendBytes(rawHeader, client);
        Sender.SendBytes(rawPayloadLength, client);
        Sender.SendBytes(rawX, client);
        Sender.SendBytes(rawY, client);
        Sender.SendBytes(rawZ, client);
    }

    public static void SendVector3(Vector3 vector, IDisposable client, int headerNumBytes)
    {
        if (client.GetType() == typeof(TcpClient))
        {
            SendVector3(vector, (TcpClient)client, headerNumBytes);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    /* Send float value to python module via tcp socket
     */
    private static void SendFloat(float value, TcpClient client, int headerNumBytes)
    {
        // Create header
        string header = "float".PadRight(headerNumBytes); 
        byte[] rawHeader = Encoding.UTF8.GetBytes(header);

        // Convert to byte array
        byte[] rawPayload = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(rawPayload);

        // Create payload length
        int payloadLength = rawPayload.Length;
        byte[] rawPayloadLength = BitConverter.GetBytes(payloadLength);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(rawPayloadLength);

        // Send bytes
        Sender.SendBytes(rawHeader, client);
        Sender.SendBytes(rawPayloadLength, client);
        Sender.SendBytes(rawPayload, client);
    }

    public static void SendFloat(float value, IDisposable client, int headerNumBytes)
    {
        if (client.GetType() == typeof(TcpClient))
        {
            SendFloat(value, (TcpClient)client, headerNumBytes);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    /* Send bytes to python module via tcp socket
     */
    private static void SendBytes(byte[] rawPacket, TcpClient client)
    {
        try
        {
            NetworkStream streamer = client.GetStream();
            streamer.Write(rawPacket, 0, rawPacket.Length);
        }
        catch (Exception err)
        {
            Debug.LogError(err.ToString());
        }
    }
}