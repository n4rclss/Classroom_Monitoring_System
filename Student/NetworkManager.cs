using RDPCOMAPILib;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Student.NetworkManager 
{
    public class NetworkManager : IDisposable
    {
        public Action<string> OnMessageReceived;
        private string _invitationToken;
        private TcpClient _client;
        private NetworkStream _stream;
        private BinaryReader _reader; // Changed from StreamReader
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private Task _listeningTask;
        private volatile bool _isConnected;
        private readonly object _listenLock = new object();
        private readonly object _connectLock = new object(); // Lock for connect/dispose operations

        private RDPSession _rdpSession;
        private Thread _rdpThread;
        private volatile bool _rdpActive = false;
        private readonly object _rdpLock = new object();
        private ManualResetEventSlim _rdpCompletedEvent = new ManualResetEventSlim(false);


        // Updated to use load balancer address and port
        private const string DefaultServerAddress = "127.0.0.1";
        private const int DefaultServerPort = 8000;

        public event EventHandler Disconnected;
        public bool IsConnected => _isConnected;

        private void Incoming(object Guest)
        {
            IRDPSRAPIAttendee MyGuest = (IRDPSRAPIAttendee)Guest;
            MyGuest.ControlLevel = CTRL_LEVEL.CTRL_LEVEL_INTERACTIVE;
        }

        public async Task ConnectAsync(string host = DefaultServerAddress, int port = DefaultServerPort)
        {
            _client = new TcpClient();
            try
            {
                await _client.ConnectAsync(host, port).ConfigureAwait(false);
                _stream = _client.GetStream();
                // Use BinaryReader for raw data handling
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
                
                // Send the raw JSON data without length prefix or newline
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

        // This passive listening might need adjustments depending on how the server sends non-login messages
        // For now, focusing on the request-response pattern for login
        public async Task ListeningPassivelyForever()
        {
            if (!_isConnected)
                throw new InvalidOperationException("Not connected to server.");

            try
            {
                Console.WriteLine("Started passive listening loop.");


                while (!_cts.Token.IsCancellationRequested)
                {
                    // Read raw bytes, assuming server sends raw JSON for other messages too
                    byte[] buffer = new byte[4096];
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, _cts.Token).ConfigureAwait(false);

                    if (bytesRead == 0)
                    {
                        Console.WriteLine("Server closed the connection.");
                        break;
                    }

                    string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Passively received: {response}");
                    
                    try
                    {
                        var doc = JsonDocument.Parse(response);
                        string type_message = doc.RootElement.GetProperty("type").GetString();
                        if (type_message == "notification") // Example handling
                        {
                            var content = doc.RootElement.GetProperty("message").GetString();
                            var sender = doc.RootElement.GetProperty("sender_username").GetString();
                            string formatted = $"{sender} says: {content}";
                            Console.WriteLine(formatted); // Log instead of MessageBox
                            OnMessageReceived?.Invoke(formatted);
                        }

                        if (type_message == "start_streaming")
                        {
                            await HandleCaptureScreen(doc.RootElement.GetProperty("sender_client_id").GetString());
                        }
                    }
                    catch (Exception parseEx)
                    {
                        Console.WriteLine($"Error parsing passive message: {parseEx.Message}");
                    }
                }
            }
            catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException)
            {
                 Console.WriteLine($"Passive listener stopped due to connection error: {ex.Message}"); 
                 await HandleDisconnectAsync(ex);
            }
            catch (OperationCanceledException)
            {
                 Console.WriteLine("Passive listener cancelled."); 
                 await HandleDisconnectAsync(null);
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
                    else
                    {
                        Console.WriteLine("Received empty response or connection closed.");
                        await HandleDisconnectAsync(null); // Treat as disconnect
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


        private async Task HandleCaptureScreen(string sender_client_id)
        {
            Console.WriteLine("Received capture screen request from student.");
            MessageBox.Show("Capture screen message from student!");

            // Start RDP in fire-and-forget manner to avoid blocking the listening loop
            _ = Task.Run(async () =>
            {
                try
                {
                    await StartRDPSessionAsync(sender_client_id);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error starting RDP session: {ex.Message}");
                }
            });
        }

        private async Task StartRDPSessionAsync(string sender_client_id)
        {
            await Task.Run(() =>
            {
                lock (_rdpLock)
                {
                    if (_rdpActive)
                    {
                        Console.WriteLine("RDP session already active.");
                        return;
                    }
                    _rdpActive = true;
                }

                _rdpThread = new Thread(RDPThreadWorker)
                {
                    IsBackground = true,
                    Name = "RDPThread"
                };

                _rdpCompletedEvent.Reset();
                _rdpThread.Start();
            });

            // Wait for RDP setup to complete (with timeout)
            bool completed = _rdpCompletedEvent.Wait(TimeSpan.FromSeconds(10));
            if (!completed)
            {
                Console.WriteLine("RDP setup timed out.");
                return;
            }

            // Send the invitation token after RDP is set up
            if (!string.IsNullOrEmpty(_invitationToken))
            {
                var screenResponse = new Student.MessageModel.Screen_data
                {
                    image_data = _invitationToken,
                    sender_client_id = sender_client_id,
                };

                try
                {
                    await SendAsync(screenResponse).ConfigureAwait(false);
                    MessageBox.Show("Student send token successfully with token length: " + _invitationToken?.Length.ToString());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send RDP token: {ex.Message}");
                }
            }
        }

        private void RDPThreadWorker()
        {
            try
            {
                Console.WriteLine("RDP thread started.");

                // Initialize RDP session on this thread
                _rdpSession = new RDPSession();
                _rdpSession.OnAttendeeConnected += Incoming;

                // Open RDP session
                _rdpSession.Open();

                // Create invitation
                IRDPSRAPIInvitation invitation = _rdpSession.Invitations.CreateInvitation(
                    "Monitoring_class",
                    Globals.UsernameGlobal,
                    "",
                    2);

                _invitationToken = invitation.ConnectionString;

                Console.WriteLine($"RDP invitation created with token length: {_invitationToken?.Length}");

                // Signal that RDP setup is complete
                _rdpCompletedEvent.Set();

                // Keep the RDP session alive
                while (_rdpActive && !_cts.Token.IsCancellationRequested)
                {
                    Thread.Sleep(1000); // Check every second
                }

                Console.WriteLine("RDP thread ending.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RDP thread error: {ex.Message}");
            }
            finally
            {
                CleanupRDP();
                _rdpCompletedEvent.Set(); // Ensure waiting thread is released
            }
        }

        private void CleanupRDP()
        {
            lock (_rdpLock)
            {
                _rdpActive = false;
                try
                {
                    _rdpSession?.Close();
                    _rdpSession = null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error cleaning up RDP: {ex.Message}");
                }
            }
        }

        public void StopRDPSession()
        {
            lock (_rdpLock)
            {
                if (!_rdpActive) return;

                _rdpActive = false;
                Console.WriteLine("Stopping RDP session...");
            }

            // Wait for RDP thread to finish (with timeout)
            if (_rdpThread != null && _rdpThread.IsAlive)
            {
                if (!_rdpThread.Join(TimeSpan.FromSeconds(5)))
                {
                    Console.WriteLine("RDP thread did not stop gracefully, continuing cleanup.");
                }
            }
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
