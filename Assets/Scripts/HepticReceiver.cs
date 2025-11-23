using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class HapticReceiver : MonoBehaviour
{
    private UdpClient udpClient;
    private IPEndPoint remoteEP;
    public int port = 9876;

    void Start()
    {
        udpClient = new UdpClient(port);
        remoteEP = new IPEndPoint(IPAddress.Any, port);
        udpClient.BeginReceive(ReceiveData, null);
    }

    void ReceiveData(IAsyncResult ar)
    {
        byte[] receivedBytes = udpClient.EndReceive(ar, ref remoteEP);
        string receivedData = Encoding.UTF8.GetString(receivedBytes);
        Debug.Log($"Received Haptic Data: {receivedData}");

        // Process and apply haptic feedback
        ApplyHapticFeedback(receivedData);

        udpClient.BeginReceive(ReceiveData, null);
    }

    void ApplyHapticFeedback(string data)
    {
        // Extract force & position values and use them in Unity for VR interactions
        Debug.Log($"Applying Haptic Feedback: {data}");
    }
}
