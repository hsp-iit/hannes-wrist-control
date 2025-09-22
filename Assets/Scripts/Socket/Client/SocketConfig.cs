using System.Collections.Generic;
using YamlDotNet.Serialization;

public class SocketConfig
{
    public Socket SOCKET;
    public int fps { get; set; }
    public float sampleTime { get; set; }
    public Communication COMMUNICATION;
}

public class Socket
{
    public string ip_server { get; set; }
    public string ip_client { get; set; }
    public int port_server { get; set; }
    public int port_client { get; set; }
    public int block_size { get; set; }
    public List<string> headers { get; set; }
    public int header_num_bytes { get; set; }
    public int payloadLength_num_bytes { get; set; }
}

public class Communication
{
    public List<string> server_elements_to_receive { get; set; }
    public List<string> client_elements_to_receive { get; set; }
}
