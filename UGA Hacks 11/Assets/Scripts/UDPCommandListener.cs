using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class UDPCommandListener : MonoBehaviour
{
    [Header("Network Intel")]
    [SerializeField] private int listenPort = 5555;

    [Header("Tactical Output")]
    [SerializeField] private LineDrawer lineDrawer;
    [SerializeField] private SpellSequencer spellSequencer;

    private UdpClient _udp;
    private Thread _recvThread;
    private volatile bool _running;

    private string[] classes = { "error", "circle", "triangle", "square", "star", "slash" };
    private readonly ConcurrentQueue<Action> _mainThreadQueue = new ConcurrentQueue<Action>();

    // Tracking the state of the incoming stream
    private short _activeShapeId = -1;
    private bool _isCurrentlyDrawing = false;

    // STATE LOCK: Remember the last action packet ID to prevent duplicate runes
    private short _lastProcessedActionId = -1;

    private void OnEnable()
    {
        if (lineDrawer == null)
            Debug.LogError("[UDPCommandListener] lineDrawer is not assigned!");
        StartReceiver();
    }

    private void OnDisable() => StopReceiver();

    private void Update()
    {
        while (_mainThreadQueue.TryDequeue(out var action))
        {
            try { action.Invoke(); }
            catch (Exception e) { Debug.LogError($"[UDPCommandListener] queued action failed: {e}"); }
        }
    }

    private void StartReceiver()
    {
        if (_running) return;
        try
        {
            _udp = new UdpClient();
            _udp.ExclusiveAddressUse = false;
            _udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udp.Client.Bind(new IPEndPoint(IPAddress.Any, listenPort));

            _running = true;
            _recvThread = new Thread(ReceiveLoop) { IsBackground = true };
            _recvThread.Start();
            Debug.Log($"<color=green>[WAR ROOM]</color> UDP Listener secured on port {listenPort}.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[WAR ROOM] Failed to bind: {e}");
            _running = false;
        }
    }

    private void StopReceiver()
    {
        _running = false;
        try { _udp?.Close(); } catch { }
        _udp = null;
        if (_recvThread != null && _recvThread.IsAlive)
        {
            try { _recvThread.Join(50); } catch { }
        }
        _recvThread = null;
    }

    private void ReceiveLoop()
    {
        IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
        while (_running)
        {
            try
            {
                byte[] data = _udp.Receive(ref remote);
                if (data != null && data.Length > 0)
                    ProcessPacketData(data);
            }
            catch (SocketException) { if (!_running) break; }
            catch (ObjectDisposedException) { break; }
            catch (Exception ex) { Debug.LogWarning($"[NET] Receive error: {ex.Message}"); }
        }
    }

    private void ProcessPacketData(byte[] data)
    {
        byte type = data[0];

        // TYPE 0: STREAM PACKET
        if (type == 0 && data.Length >= 16)
        {
            short shapeId = BitConverter.ToInt16(data, 1);
            float x = BitConverter.ToSingle(data, 7);
            float y = BitConverter.ToSingle(data, 11);
            bool isDrawingPacket = data[15] != 0;
            Vector2 norm = new Vector2(x, y);

            _mainThreadQueue.Enqueue(() =>
            {
                if (lineDrawer == null) return;
                lineDrawer.UpdateCursorNormalized(norm);

                if (isDrawingPacket)
                {
                    if (!_isCurrentlyDrawing || _activeShapeId != shapeId)
                    {
                        _isCurrentlyDrawing = true;
                        _activeShapeId = shapeId;
                        lineDrawer.StartNewLine();
                    }
                    lineDrawer.AddPointNormalized(norm);
                }
                else if (_isCurrentlyDrawing && _activeShapeId == shapeId)
                {
                    _isCurrentlyDrawing = false;
                    lineDrawer.InitiateFade();
                }
            });
        }
        // TYPE 1: ACTION PACKET
        else if (type == 1 && data.Length >= 5)
        {
            short shapeId = BitConverter.ToInt16(data, 1);
            byte classId = data[3];
            byte laneId = data[4];
            Debug.Log("Class ID: " + classId);
            // ONLY PROCESS IF THIS IS A NEW SHAPE ID
            if (shapeId != _lastProcessedActionId)
            {
                _lastProcessedActionId = shapeId;

                _mainThreadQueue.Enqueue(() =>
                {
                    if (lineDrawer != null && _isCurrentlyDrawing && _activeShapeId == shapeId)
                    {
                        _isCurrentlyDrawing = false;
                        lineDrawer.InitiateFade();
                    }

                    if (spellSequencer != null)
                    {
                        try
                        {
                            // If Class 5 is the slash, finalize the spell
                            if (classId == 5) 
                            {
                                spellSequencer.FinalizeSequence(laneId);
                                _lastProcessedActionId = -1; // Reset to allow same shapes in next spell
                            }
                            else 
                            {
                                spellSequencer.AddToStack(classId);
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"[SEQUENCER ERROR] Failed to process ClassID {classId}: {e.Message}");
                        }
                    }
                });
            }
        }
    }
}