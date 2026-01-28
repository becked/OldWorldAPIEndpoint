using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace OldWorldAPIEndpoint
{
    /// <summary>
    /// Minimal TCP server that broadcasts messages to all connected clients.
    /// Uses newline-delimited JSON (one JSON object per line).
    /// </summary>
    public class TcpBroadcastServer
    {
        private readonly int _port;
        private TcpListener _listener;
        private readonly ConcurrentDictionary<int, TcpClient> _clients = new ConcurrentDictionary<int, TcpClient>();
        private int _nextClientId = 0;
        private volatile bool _running;
        private Thread _acceptThread;

        public TcpBroadcastServer(int port)
        {
            _port = port;
        }

        /// <summary>
        /// Start the TCP server and begin accepting connections.
        /// </summary>
        public void Start()
        {
            if (_running) return;

            try
            {
                _listener = new TcpListener(IPAddress.Loopback, _port);
                _listener.Start();
                _running = true;

                _acceptThread = new Thread(AcceptLoop)
                {
                    IsBackground = true,
                    Name = "APIEndpoint-Accept"
                };
                _acceptThread.Start();

                Debug.Log($"[APIEndpoint] TCP server started on port {_port}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[APIEndpoint] Failed to start TCP server: {ex.Message}");
                _running = false;
            }
        }

        /// <summary>
        /// Stop the TCP server and disconnect all clients.
        /// </summary>
        public void Stop()
        {
            _running = false;

            try
            {
                _listener?.Stop();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TcpBroadcastServer] Error stopping listener: {ex.Message}");
            }

            foreach (var kvp in _clients)
            {
                try
                {
                    kvp.Value.Close();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[TcpBroadcastServer] Error closing client {kvp.Key}: {ex.Message}");
                }
            }
            _clients.Clear();

            Debug.Log("[APIEndpoint] TCP server stopped");
        }

        /// <summary>
        /// Broadcast a JSON message to all connected clients.
        /// Uses newline-delimited JSON format.
        /// </summary>
        public void Broadcast(string json)
        {
            int clientCount = _clients.Count;
            Debug.Log($"[APIEndpoint] Broadcast called, {clientCount} clients connected");

            if (_clients.IsEmpty) return;

            // Newline-delimited JSON - simple and easy to parse
            byte[] payload = Encoding.UTF8.GetBytes(json + "\n");

            foreach (var kvp in _clients)
            {
                try
                {
                    var stream = kvp.Value.GetStream();
                    stream.Write(payload, 0, payload.Length);
                    stream.Flush();
                    Debug.Log($"[APIEndpoint] Sent {payload.Length} bytes to client {kvp.Key}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[APIEndpoint] Error sending to client {kvp.Key}: {ex.Message}");
                    // Client disconnected or error - remove it
                    if (_clients.TryRemove(kvp.Key, out var client))
                    {
                        try { client.Close(); } catch { }
                        Debug.Log($"[APIEndpoint] Client {kvp.Key} disconnected");
                    }
                }
            }
        }

        /// <summary>
        /// Background thread that accepts incoming connections.
        /// </summary>
        private void AcceptLoop()
        {
            while (_running)
            {
                try
                {
                    var client = _listener.AcceptTcpClient();
                    var id = Interlocked.Increment(ref _nextClientId);
                    _clients[id] = client;
                    Debug.Log($"[APIEndpoint] Client {id} connected from {client.Client.RemoteEndPoint}");
                }
                catch (SocketException) when (!_running)
                {
                    // Server shutting down - expected
                    break;
                }
                catch (Exception ex)
                {
                    if (_running)
                    {
                        Debug.LogError($"[APIEndpoint] Accept error: {ex.Message}");
                    }
                }
            }
        }
    }
}
