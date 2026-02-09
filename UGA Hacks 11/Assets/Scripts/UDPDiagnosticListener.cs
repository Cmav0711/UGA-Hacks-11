using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class UDPDiagnosticListener : MonoBehaviour
{
    [Header("Intelligence Settings")]
    [SerializeField] private int port = 5555;
    
    private UdpClient _diagnosticClient;
    private Thread _diagnosticThread;
    private bool _isRunning = true;

    void Start()
    {
        _diagnosticThread = new Thread(ListenForRawTraffic);
        _diagnosticThread.IsBackground = true;
        _diagnosticThread.Start();
        Debug.Log($"<color=cyan>DIAGNOSTIC MODE:</color> Listening for raw UDP on port {port}...");
    }

    private void ListenForRawTraffic()
    {
        try
        {
            // Bind to all interfaces to catch Tailscale traffic
            IPEndPoint localEp = new IPEndPoint(IPAddress.Any, port);
            _diagnosticClient = new UdpClient();
            _diagnosticClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _diagnosticClient.Client.Bind(localEp);

            IPEndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);

            while (_isRunning)
            {
                byte[] rawData = _diagnosticClient.Receive(ref remoteEp);
                
                // Thread-safe logging isn't strictly required for Debug.Log, 
                // but we use a string to capture the moment of impact.
                string report = $"<color=yellow>[PACKET RECEIVED]</color> " +
                               $"Source: {remoteEp.Address}:{remoteEp.Port} | " +
                               $"Size: {rawData.Length} bytes | " +
                               $"First Byte: {rawData[0]}";
                
                Debug.Log(report);
            }
        }
        catch (Exception e)
        {
            if (_isRunning) Debug.LogError($"Diagnostic Failure: {e.Message}");
        }
    }

    private void OnApplicationQuit()
    {
        _isRunning = false;
        _diagnosticClient?.Close();
        if (_diagnosticThread != null && _diagnosticThread.IsAlive)
            _diagnosticThread.Abort();
    }
}