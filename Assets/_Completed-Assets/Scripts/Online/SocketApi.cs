using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class SocketAPI
{
    public EndPoint ComputeEndPoint(string host, int port)
    {
        IPAddress ipAddress = IPAddress.None;
        if (host.Length == 0)
            host = Dns.GetHostName();

        bool found = false;
        try
        {
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            foreach (var ip in ipHost.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    ipAddress = ip;
                    found = true;
                    break;
                }
            }
        }
        catch (Exception ) { }
        //fallback
        if(!found) 
            ipAddress = IPAddress.Parse(host);

        Log( "Adress resolved : " +  ipAddress );
        return  new IPEndPoint(ipAddress , port);
    }

    private class Client
    {
        public Socket m_socket = null;
        public bool m_isListening = false;
        public State m_state = State.DISCONNECTED;
    }
    
    private List<Client> m_sockets = new List<Client>();
    public delegate void LogDelegate (string txt);
    public LogDelegate Log;

    private enum State
    {
        DISCONNECTED,
        CONNECTING,
        CONNECTED,
    }

    private List<Client> m_removingClients = new List<Client>();
    private List<Client> m_newClients = new List<Client>();
        
    public bool IsConnected()
        {
            foreach (var s in m_sockets)
            {
                if (s.m_state != State.CONNECTED)
                    return false;
            }
            return true;
        }
    public int GetNumConnectedClient()
    {
        int i = 1;
        foreach (var s in m_sockets)
        {
            if (!s.m_isListening && s.m_state == State.CONNECTED)
                i++;
        }

        return i;
    }
    public EndPoint GetLocalEndPoint()
    {
        if (!IsConnected())
            return new IPEndPoint(IPAddress.Any, 0);
        return m_sockets[0].m_socket.LocalEndPoint;
    }
    public void FetchClients(List<EndPoint> _clients)
    {
        if (!IsConnected())
            return;
        foreach (var client in m_sockets)
        {
            if (client.m_isListening)
                _clients.Add(client.m_socket.LocalEndPoint);
            else
                _clients.Add(client.m_socket.RemoteEndPoint);
        }

    }
    public void Process()
    {
        
        foreach (Client client in m_sockets)
        {
            switch (client.m_state)
            {
                case State.CONNECTING:
                {
                    if (client.m_socket.Poll(0, SelectMode.SelectError))
                    {
                        Log("Can't connect");
                        client.m_state = State.DISCONNECTED;
                        m_removingClients.Add(client);
                    }
                    else if (client.m_socket.Poll(0, SelectMode.SelectWrite))
                    {
                        Log("Connected");
                        client.m_state = State.CONNECTED;
                    }
                    break;
                }
                case State.CONNECTED:
                {
                    //listen
                    if (client.m_isListening)
                    {
                        try
                        {
                            Socket joinerSocket = client.m_socket.Accept();
                            if (joinerSocket != null)
                            {
                                Log("Someone Joined");
                                var joiner = NewClient(joinerSocket);
                                joiner.m_state = State.CONNECTED;
                                m_newClients.Add(joiner);
                            }
                        }catch(SocketException ex)
                        {
                            if (ex.SocketErrorCode == SocketError.WouldBlock)
                            {
                                continue;
                            }
                            else
                            {
                                Log("Socket error " + ex.ToString());
                                m_removingClients.Add(client);
                            }
                        }
                    }
                    //receive
                    else
                    {if (!Receive(client))
                        {
                            Log("Removing client ");
                            m_removingClients.Add(client);
                        }


                    }
                    break;
                }
            }
        }

        m_sockets.AddRange(m_newClients);
        m_newClients.Clear();
        m_sockets.RemoveAll((client) => { return m_removingClients.Contains(client); });
        m_removingClients.Clear();

    }
    // Start is called before the first frame update
    private Client NewClient(Socket _socket = null)
    {
        Log("Initialize");
        Client client = new Client();
        if (_socket == null)
            client.m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        else
            client.m_socket = _socket;
        client.m_socket.Blocking = false;
        return client;
    }

    public void Host(string host, int port)
    {
        var client = NewClient();
        Log("Host");
        client.m_isListening = true;
        var EP = ComputeEndPoint(host, port);
        try
        {
            client.m_socket.Bind(EP); 
            Log("Socket Binded");
            client.m_socket.Listen(100);
            Log("Socket Listening");
            m_sockets.Add(client);
            client.m_state = State.CONNECTED;
        } catch (Exception ex) { Log(ex.ToString()); }
    }

    public void Join(string host, int port)
    {
        var client = NewClient();
        m_sockets.Add(client);
        Log("Join");
        var EP = ComputeEndPoint(host, port);
        try
        {
            Log("Connecting...");
            client.m_state = State.CONNECTING;
            client.m_socket.Connect(EP);
        }
        catch (SocketException ex)
        {
            if (ex.SocketErrorCode != SocketError.WouldBlock)
            {
                Log(ex.ToString());
            }
        }
        catch (Exception ex) { Log(ex.ToString()); }
    }
    public void Send(byte[] _msg)
    {
        try
        {
            foreach (var client in m_sockets)
            {
                if (!client.m_isListening)
                    client.m_socket.Send(_msg);
            }
        } catch (Exception ex) { Log(ex.ToString()); }
    }
    
    
    public delegate int MessageCallback(byte[] msg);

    public MessageCallback OnMessage;
    private bool Receive(Client client )
    {
        const int bufferSize = 32 * 1024 * 1024;
        byte[] buffer = new byte[bufferSize];

        try
        {
            if (client.m_isListening)
                return false;

            if (client.m_socket.Available > 0)
            {
                client.m_socket.ReceiveTimeout = 100;
                int nbBytes = client.m_socket.Receive(buffer);

                if (nbBytes == bufferSize)
                {
                    Log("error : buffer size exceeded");
                    //should handle this
                    return true;
                }

                if (nbBytes > 0)
                {
                    Log("msg received with size " + nbBytes + " from " +
                        client.m_socket.RemoteEndPoint.ToString());
                    var msg = new byte[nbBytes];
                    Array.Copy(buffer, msg, nbBytes);
                    OnMessage(msg);
                }
            }

        }
        catch (SocketException sex)
        {
            if (sex.SocketErrorCode != SocketError.TimedOut)
            {
                Log(sex.ToString());
                return false;
            }

        }
        catch (Exception ex)
        {
            Log(ex.ToString());
            return false;
        }

        return true;

    }


}