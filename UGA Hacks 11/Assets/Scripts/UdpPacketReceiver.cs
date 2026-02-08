using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class UdpPacketReceiver : MonoBehaviour
{
    [SerializeField] private int listenPort = 5555;

    private UdpClient udp;
    private Thread recvThread;
    private volatile bool running;

    private readonly ConcurrentQueue<Action> mainThreadQueue = new ConcurrentQueue<Action>();

    private void OnEnable()
    {
        StartReceiver();
    }

    private void OnDisable()
    {
        StopReceiver();
    }

    private void Update()
    {
        while (mainThreadQueue.TryDequeue(out var action))
        {
            action.Invoke();
        }
    }

    private void StartReceiver()
    {
        if (running) return;

        udp = new UdpClient(listenPort);
        running = true;

        recvThread = new Thread(ReceiveLoop) { IsBackground = true };
        recvThread.Start();

        Debug.Log($"[NET] Listening on UDP {listenPort}");
    }

    private void StopReceiver()
    {
        running = false;
        try { udp?.Close(); } catch { }
        udp = null;
    }

    private void ReceiveLoop()
    {
        IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);

        while (running)
        {
            try
            {
                byte[] data = udp.Receive(ref remote);
                if (data == null || data.Length == 0) continue;

                // Parse on background thread, enqueue log to main thread
                var parsed = ParsePacket(data);
                mainThreadQueue.Enqueue(() =>
                {
                    Debug.Log($"[NET] {remote.Address}:{remote.Port} {parsed}");
                });
            }
            catch (SocketException)
            {
                // Expected when closing
                if (!running) break;
            }
            catch (Exception ex)
            {
                mainThreadQueue.Enqueue(() => Debug.LogWarning($"[NET] Receive error: {ex.Message}"));
            }
        }
    }

    private static string ParsePacket(byte[] data)
    {
        byte type = data[0];

        if (type == 0 && data.Length >= 16)
        {
            // Stream packet: [0][shapeId:int16][offset:int32][x:float][y:float][isDrawing:byte]
            short shapeId = BitConverter.ToInt16(data, 1);
            int offset = BitConverter.ToInt32(data, 3);
            float x = BitConverter.ToSingle(data, 7);
            float y = BitConverter.ToSingle(data, 11);
            byte isDrawing = data[15];

            return $"STREAM id={shapeId} offset={offset} x={x:F3} y={y:F3} drawing={isDrawing}";
        }

        if (type == 1 && data.Length >= 5)
        {
            // Action packet: [1][shapeId:int16][classId:byte][laneId:byte]
            short shapeId = BitConverter.ToInt16(data, 1);
            byte classId = data[3];
            byte laneId = data[4];

            return $"ACTION id={shapeId} class={classId} lane={laneId}";
        }

        return $"UNKNOWN type={type} bytes={data.Length}";
    }
}