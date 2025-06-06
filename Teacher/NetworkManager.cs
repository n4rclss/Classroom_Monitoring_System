using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Teacher.NetworkManager
{
    public class NetworkManager : IDisposable
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private BinaryReader _reader;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private Task _listeningTask;
        private volatile bool _isConnected;
        private readonly object _listenLock = new object();
        private readonly object _connectLock = new object(); // Lock for connect/dispose operations

        // Updated to use load balancer address and port
        private const string DefaultServerAddress = "127.0.0.1";
        private const int DefaultServerPort = 8000;

        public event EventHandler Disconnected;
        public bool IsConnected => _isConnected;

        public async Task ConnectAsync(string host = DefaultServerAddress, int port = DefaultServerPort)
        {
            _client = new TcpClient();
            try
            {
                await _client.ConnectAsync(host, port).ConfigureAwait(false);
                _stream = _client.GetStream();
                _reader = new BinaryReader(_stream, Encoding.UTF8, leaveOpen: true);
                _isConnected = true;
                Console.WriteLine($"Successfully connected to load balancer at {host}:{port}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection failed: {ex.Message}");
                CleanupResources();
                _isConnected = false;
                throw new InvalidOperationException($"Failed to connect to {host}:{port}", ex);
            }
        }

        private async Task SendAsync<T>(T data)
        {
            if (!_isConnected)
                throw new InvalidOperationException("Not connected to load balancer.");

            try
            {
                var json = JsonSerializer.Serialize(data);
                byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
                
                // Send the raw JSON data without length prefix
                // The load balancer will handle the length prefixing when forwarding to servers
                await _stream.WriteAsync(jsonBytes, 0, jsonBytes.Length, _cts.Token).ConfigureAwait(false);
                await _stream.FlushAsync(_cts.Token).ConfigureAwait(false);
                
                Console.WriteLine($"Sent: {json}");
            }
            catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException || ex is OperationCanceledException)
            {
                await HandleDisconnectAsync(ex);
                throw new InvalidOperationException("Connection lost while sending data.", ex);
            }
        }

        public async Task<string> ProcessSendMessage<T>(T data)
        {
            if (!_isConnected)
                throw new InvalidOperationException("Not connected to load balancer.");

            string response = string.Empty;
            try
            {
                await SendAsync(data).ConfigureAwait(false);
                response = await ListenResponsesAsync(_cts.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while processing message: {ex.Message}");
            }
            return response;
        }

        private async Task<string> ListenResponsesAsync(CancellationToken token)
        {
            Console.WriteLine("Waiting for response from load balancer...");
            string response = "";
            
            try
            {
                if (!token.IsCancellationRequested)
                {
                    // Read response data (up to 4KB)
                    byte[] buffer = new byte[4096];
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);
                    
                    if (bytesRead > 0)
                    {
                        response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Console.WriteLine($"Received response: {response}");
                    }
                }
            }
            catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException)
            {
                Console.WriteLine($"Listener task stopped due to connection error: {ex.Message}"); 
                await HandleDisconnectAsync(ex);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Listener task cancelled."); 
                await HandleDisconnectAsync(null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error in listener task: {ex.Message}"); 
                await HandleDisconnectAsync(ex); 
            }
            
            return response; 
        }

        private async Task HandleDisconnectAsync(Exception reason)
        {
            if (!_isConnected) return;

            lock (_connectLock)
            {
                if (!_isConnected) return;
                _isConnected = false;
                Console.WriteLine("Handling disconnection...");
            }

            // Cancel any ongoing operations (like SendAsync)
            try { _cts?.Cancel(); } catch { }

            // Await the invocation of the Disconnected event to ensure proper handling
            if (Disconnected != null)
            {
                await Task.Run(() => Disconnected.Invoke(this, EventArgs.Empty));
            }
        }

        private void CleanupResources()
        {
            _reader?.Dispose();
            _stream?.Dispose();
            _client?.Close(); 
            _cts?.Dispose();

            _reader = null;
            _stream = null;
            _client = null;
            _cts = null;
        }

        public void Dispose()
        {
            lock (_connectLock) 
            {
                if (!_isConnected && _client == null) 
                {
                    return;
                }
                _isConnected = false;

                Console.WriteLine("Disposing NetworkManager...");

                // Cancel operations and signal listener to stop
                try { _cts?.Cancel(); } catch { /* Ignore */ }

                // Clean up resources
                CleanupResources();
            }
            GC.SuppressFinalize(this);
        }
    }
}
