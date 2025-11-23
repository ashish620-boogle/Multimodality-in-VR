using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;

public class SendPosition : MonoBehaviour
{
    NetworkDriver m_Driver;
    NativeList<NetworkConnection> m_Connections;

    public Transform stylus;
    public Transform hapticCollider;
    //public Transform stylus_collider;
    public ushort serverPort = 5000;
    private float ctime;
    private float lastReceiveTime = -1f;
    private float estimatedRTT = 0f;

    void Start()
    {
        m_Driver = NetworkDriver.Create();
        m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);

        var endpoint = NetworkEndpoint.AnyIpv4.WithPort(serverPort);
        if (m_Driver.Bind(endpoint) != 0)
        {
            Debug.LogError($"Failed to bind to port {serverPort}");
            return;
        }
        m_Driver.Listen();
        Debug.Log($"Server started at {GetLocalIPAddress()}:{serverPort}");
    }

    void OnDestroy()
    {
        if (m_Driver.IsCreated)
        {
            m_Driver.Dispose();
            m_Connections.Dispose();
        }
    }

    void Update()
    {
        m_Driver.ScheduleUpdate().Complete();

        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated)
            {
                m_Connections.RemoveAtSwapBack(i);
                i--;
            }
        }

        NetworkConnection c;
        while ((c = m_Driver.Accept()) != default)
        {
            m_Connections.Add(c);
            Debug.Log("Client connected.");
        }

        foreach (var connection in m_Connections)
        {
            if (!connection.IsCreated) continue;

            m_Driver.BeginSend(NetworkPipeline.Null, connection, out var writer);

             //Send stylus position and rotation
            writer.WriteFloat(stylus.position.x);
            writer.WriteFloat(stylus.position.y);
            writer.WriteFloat(stylus.position.z);
            writer.WriteFloat(stylus.rotation.eulerAngles.x);
            writer.WriteFloat(stylus.rotation.eulerAngles.y);
            writer.WriteFloat(stylus.rotation.eulerAngles.z);
            writer.WriteFloat(hapticCollider.position.x);
            writer.WriteFloat(hapticCollider.position.y);
            writer.WriteFloat(hapticCollider.position.z);
            writer.WriteFloat(hapticCollider.rotation.eulerAngles.x);
            writer.WriteFloat(hapticCollider.rotation.eulerAngles.y);
            writer.WriteFloat(hapticCollider.rotation.eulerAngles.z);


            m_Driver.EndSend(writer);

            //Debug.Log($"[SERVER] Sent Stylus Data - Position: ({stylus.position}) Rotation: ({stylus.rotation.eulerAngles})");
            //Debug.Log($"[SERVER] Sent HC Data - Position: ({hapticCollider.position}) Rotation: ({hapticCollider.rotation.eulerAngles})");
        }

        for (int i = 0; i < m_Connections.Length; i++)
        {
            DataStreamReader stream;
            NetworkEvent.Type cmd;
            while ((cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Data)
                {
                    ctime = stream.ReadFloat();
                    float now = Time.time;
                    if (lastReceiveTime > 0)
                    {
                        estimatedRTT = (now - lastReceiveTime) / 2f;
                        Debug.Log($"[SERVER] Estimated Round Trip Time: {estimatedRTT:F4} seconds");
                    }
                    lastReceiveTime = now;

                    //Debug.Log($"[SERVER] Got this from client - {ctime}");
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client disconnected.");
                    m_Connections[i] = default;
                    break;
                }
            }
        }
    }


    string GetLocalIPAddress()
    {
        foreach (var ip in System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return "Unavailable";
    }
}