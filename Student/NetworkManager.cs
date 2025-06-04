using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Teacher.NetworkManager
{

    public class NetworkManager : IDisposable
    {
        public Action<string> OnMessageReceived;

        private TcpClient _client;
        private NetworkStream _stream;
        private StreamReader _reader;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private Task _listeningTask;
        private volatile bool _isConnected;
        private readonly object _listenLock = new object();
        private readonly object _connectLock = new object(); // Lock for connect/dispose operations

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
                // Use leaveOpen: true as if the _reader is disposed, the _stream still exists.
                _reader = new StreamReader(_stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
                _isConnected = true;
                Console.WriteLine("Successfully connected to server.");
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
                throw new InvalidOperationException("Not connected to server.");

            try
            {
                var json = JsonSerializer.Serialize(data);
                // Line-based protocols
                byte[] buffer = Encoding.UTF8.GetBytes(json + "\n");
                // Use ConfigureAwait(false)
                await _stream.WriteAsync(buffer, 0, buffer.Length, _cts.Token).ConfigureAwait(false);
                await _stream.FlushAsync(_cts.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException || ex is OperationCanceledException)
            {
                await HandleDisconnectAsync(ex);
                throw new InvalidOperationException("Connection lost while sending data.", ex);
            }
        }

        public async Task ListeningPassivelyForever()
        {
            if (!_isConnected)
                throw new InvalidOperationException("Not connected to server.");

            try
            {
                MessageBox.Show("Started passive listening loop.");

                while (!_cts.Token.IsCancellationRequested)
                {
                    string response = await _reader.ReadLineAsync();

                    if (response == null)
                    {
                        MessageBox.Show("Server closed the connection.");
                        break;
                    }
                    
                    try
                    {
                        var doc = JsonDocument.Parse(response);
                        string type_message = doc.RootElement.GetProperty("type").GetString();
                        if (type_message == "send_message_to_all")
                        {
                            var content = doc.RootElement.GetProperty("content").GetString();
                            var sender = doc.RootElement.GetProperty("sender").GetString();
                            string formatted = $"{sender} says: {content}";
                            MessageBox.Show(formatted);
     
                            OnMessageReceived?.Invoke(formatted);
                        }

                    }
                    catch (Exception parseEx)
                    {
                        Console.WriteLine(parseEx.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Passive Listener Error] {ex.Message}");
                await HandleDisconnectAsync(ex);
            }
            finally
            {
                Console.WriteLine("Passive listening loop exited.");
            }
        }

        public async Task<string> ProcessSendMessage<T>(T data)
        {
            if (!_isConnected)
                throw new InvalidOperationException("Not connected to server.");

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
            Console.WriteLine("Listener task started.");
            string line = "";
            try
            {
                if (!token.IsCancellationRequested)
                {
                    line = await _reader.ReadLineAsync().ConfigureAwait(false);
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
            finally
            {
                Console.WriteLine("Listener task finished.");
            }
            return line; 
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
            Task.Run(() => Disconnected?.Invoke(this, EventArgs.Empty));

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

