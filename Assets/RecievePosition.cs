using UnityEngine;
using System.Collections;
using Unity.Networking.Transport;
using Unity.Collections;
using UnityEngine.Audio;

public class RecievePosition : MonoBehaviour
{
    NetworkDriver m_Driver;
    NetworkConnection m_Connection;

    public Transform stylus;
    public Transform HapticStylys;

    public string serverIP = "192.168.236.200";
    public ushort serverPort = 5000;

    Vector3 force;

    void Start()
    { 
        m_Driver = NetworkDriver.Create();
        var endpoint = NetworkEndpoint.Parse(serverIP, serverPort);
        m_Connection = m_Driver.Connect(endpoint);
        Debug.Log("[CLIENT] Attempting to connect to server...");


    }

    void OnDestroy()
    {
        m_Driver.Dispose();
    }


    void Update()
    {
        //Networking
        m_Driver.ScheduleUpdate().Complete();

        if (!m_Connection.IsCreated) return;

        DataStreamReader stream;
        NetworkEvent.Type cmd;

        while ((cmd = m_Connection.PopEvent(m_Driver, out stream)) != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                Debug.Log("[CLIENT] Connected to server.");
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                float posX = stream.ReadFloat();
                float posY = stream.ReadFloat();
                float posZ = stream.ReadFloat();
                float rotX = stream.ReadFloat();
                float rotY = stream.ReadFloat();
                float rotZ = stream.ReadFloat();

                stylus.transform.position = Vector3.Lerp(stylus.transform.position, new Vector3(posX, posY, posZ), Time.deltaTime * 10);
                stylus.transform.rotation = Quaternion.Slerp(stylus.transform.rotation, Quaternion.Euler(rotX, rotY, rotZ), Time.deltaTime * 10);

                posX = stream.ReadFloat();
                posY = stream.ReadFloat();
                posZ = stream.ReadFloat();
                rotX = stream.ReadFloat();
                rotY = stream.ReadFloat();
                rotZ = stream.ReadFloat();

                HapticStylys.transform.position = Vector3.Lerp(HapticStylys.transform.position, new Vector3(posX, posY, posZ), Time.deltaTime * 10);
                HapticStylys.transform.rotation = Quaternion.Slerp(HapticStylys.transform.rotation, Quaternion.Euler(rotX, rotY, rotZ), Time.deltaTime * 10);

                Debug.Log("[CLIENT] Position updated");

                SendForceFeedback();
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log("[CLIENT] Disconnected from server.");
                m_Connection = default;
            }
        }

    }

    void SendForceFeedback()
    {
        m_Driver.BeginSend(NetworkPipeline.Null, m_Connection, out var writer);
        writer.WriteFloat(1f);
        m_Driver.EndSend(writer);

        Debug.Log($"[CLIENT] Sent acknolwdgement");
    }

}