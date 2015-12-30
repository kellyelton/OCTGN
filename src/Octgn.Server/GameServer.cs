using System;
using System.Net.Sockets;

namespace Octgn.Server
{
    public class GameServer : IDisposable
    {
        private GameServerListener _listener;
        public GameServer(int port)
        {
            _listener = new GameServerListener(port, OnSocket);
        }

        private void OnSocket(GameServerSocket sock)
        {

        }

        public void Dispose()
        {
            _listener.Dispose();
        }
    }

    internal class GameServerListener : IDisposable
    {
        private bool _disposed;
        private TcpListener _listener;
        private Action<GameServerSocket> _onSocket;
        public GameServerListener(int port, Action<GameServerSocket> onSocket)
        {
            _listener = new TcpListener(System.Net.IPAddress.Any, port);
            _onSocket = onSocket;
            Run();
        }

        private async void Run()
        {
            var socket = await _listener.AcceptSocketAsync();
            if(socket != null)
            {
                // Handle the socket
                _onSocket(new GameServerSocket(socket));
            }
            lock (this)
            {
                if (_disposed) return;
                Run();
            }
        }
        public void Dispose()
        {
            lock (this)
            {
                if (_disposed) return;
                _disposed = true;
            }
        }
    }

    internal class GameServerSocket
    {
        private Socket _socket;
        public GameServerSocket(Socket sock)
        {
            _socket = sock;
        }
    }
}
