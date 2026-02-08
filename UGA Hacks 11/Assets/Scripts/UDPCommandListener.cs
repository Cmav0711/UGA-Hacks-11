using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class UDPCommandListener : MonoBehaviour
{
    [Header("Network Config")]
    [SerializeField] private int port = 5555;
    
    [Header("Intelligence Link")]
    [SerializeField] private LineDrawer lineDrawer;

    private UdpClient _udpClient;
    private Thread _receiveThread;
    private bool _isListening = true;

    // Buffer for thread-to-main-thread communication
    private struct PacketInfo {
        public byte type;
        public ushort id;
        public Vector2 pos;
        public bool isDrawing;
        public byte shapeClass;
        public byte direction;
    }

    private PacketInfo _latestPacket;
    private bool _hasNewData = false;
    private readonly object _lock = new object();

    void Start()
    {
        _receiveThread = new Thread(ListenForPackets);
        _receiveThread.IsBackground = true;
        _receiveThread.Start();
    }

    private void ListenForPackets()
    {
        _udpClient = new UdpClient(port);
        IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);

        while (_isListening)
        {
            try
            {
                byte[] data = _udpClient.Receive(ref remoteIpEndPoint);
                if (data.Length >= 1) ParsePacket(data);
            }
            catch (Exception e) { Debug.LogWarning($"UDP Error: {e}"); }
        }
    }

    private void ParsePacket(byte[] data)
    {
        byte type = data[0];
        lock (_lock)
        {
            _latestPacket.type = type;

            if (type == 1 && data.Length >= 16) // Position
            {
                _latestPacket.id = BitConverter.ToUInt16(data, 1);
                _latestPacket.pos = new Vector2(BitConverter.ToSingle(data, 7), BitConverter.ToSingle(data, 11));
                _latestPacket.isDrawing = data[15] != 0;
            }
            else if (type == 2 && data.Length >= 5) // Termination
            {
                _latestPacket.id = BitConverter.ToUInt16(data, 1);
                _latestPacket.shapeClass = data[3];
                _latestPacket.direction = data[4];
                _latestPacket.isDrawing = false; // Force stop
            }
            _hasNewData = true;
        }
    }

    [Header("Conflict Resolution")]
[SerializeField] private bool useUDPAuthority = true;
[SerializeField] private KeyedInputProvider keyedProvider; // Reference your old script

void Update()
{
    // If UDP Authority is active, we disable the manual override
    if (keyedProvider != null)
    {
        keyedProvider.enabled = !useUDPAuthority;
    }

    if (!useUDPAuthority) return;

    lock (_lock)
    {
        if (_hasNewData)
        {
            ExecuteTacticalCommand(_latestPacket);
            _hasNewData = false;
        }
    }
}

    private ushort _activeLineID;
    private bool _isCurrentlyDrawing = false;
    public SpellSequencer spellSequencer;
    private void ExecuteTacticalCommand(PacketInfo info)
    {
        if (info.type == 1)
        {
            // ... (Keep existing Type 1 Drawing Logic) ...
        }
        else if (info.type == 2 && _isCurrentlyDrawing && _activeLineID == info.id)
        {
            _isCurrentlyDrawing = false;
            lineDrawer.InitiateFade();

            if (info.direction == 0) 
            {
                // Just add to the sequence
                spellSequencer.AddToStack(info.shapeClass);
            }
            else 
            {
                // Directional input received: Finalize and Fire!
                spellSequencer.AddToStack(info.shapeClass); // Add final shape
                spellSequencer.FinalizeSequence(info.direction);
            }
        }
    }

    private void OnApplicationQuit()
    {
        _isListening = false;
        _udpClient?.Close();
    }
}