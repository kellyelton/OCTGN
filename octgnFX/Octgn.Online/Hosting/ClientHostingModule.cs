using Octgn.Communication;
using Octgn.Communication.Packets;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Octgn.Online.Hosting
{
    public class ClientHostingModule : Module
    {
        public IClientHostingRPC RPC { get; set; }

        public event EventHandler<HostedGameEventArgs> HostedGameReady {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        public ClientHostingModule(Client client, Version octgnVersion) {
            RPC = new ClientHostingRPC(client, octgnVersion);
        }

        public override IEnumerable<Type> IncludedTypes => _includedTypes;
        private static Type[] _includedTypes = new Type[] { typeof(HostedGame) };
    }

    public class HostingModule : Module
    {
        public override void Initialize() {
            var requestHandler = GetModule<RequestHandler>();

            if(_clientRpc != null) {
                requestHandler.Register(nameof(IClientHostingRPC.HostGame), OnHostGameRequest);
            } else if(_server != null) {
                requestHandler.Register(nameof(IClientHostingRPC.HostGame), ForwardRequestToGameService);
            }

            base.Initialize();
        }

        #region Client Side

        private readonly Client _client;
        private readonly ClientHostingRPC _clientRpc;

        public EventHandler<HostedGameEventArgs> HostGameRequest;

        public HostingModule(Client client, Version octgnVersion) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _clientRpc = new ClientHostingRPC(_client, octgnVersion);

            Attach(new RequestHandler());
        }

        private Task<ProcessResult> OnHostGameRequest(RequestPacket request) {
            var game = HostedGame.GetFromPacket(request)
                ?? throw new InvalidOperationException($"game is null in {request}");

            var args = new HostedGameEventArgs {
                Client = _client,
                Game = game
            };

            HostGameRequest?.Invoke(this, args);

            return Task.CompletedTask;
        }

        #endregion Client Side

        #region Server Side

        private readonly Server _server;
        private readonly string _gameServerUserId;

        public HostingModule(Server server, string gameServerUserId) {
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _gameServerUserId = gameServerUserId ?? throw new ArgumentNullException(nameof(gameServerUserId));

            Attach(new RequestHandler());
        }

        private async Task<ProcessResult> ForwardRequestToGameService(RequestPacket request) {
            var sendRequest = new RequestPacket(request);
            var gsResp = await _server.Request(sendRequest, _gameServerUserId);

            return new ProcessResult(gsResp.Data);
        }

        #endregion Server Side

        public override IEnumerable<Type> IncludedTypes => _includedTypes;
        private static Type[] _includedTypes = new Type[] { typeof(HostedGame) };
    }
}
