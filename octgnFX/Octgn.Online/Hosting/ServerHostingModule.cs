using System.Threading.Tasks;
using Octgn.Communication;
using System;
using Octgn.Communication.Packets;
using System.Collections.Generic;

namespace Octgn.Online.Hosting
{
    public class ServerHostingModule : Module
    {
        private readonly Server _server;
        private readonly string _gameServerUserId;

        public ServerHostingModule(Server server, string gameServerUserId) {
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _gameServerUserId = gameServerUserId ?? throw new ArgumentNullException(nameof(gameServerUserId));

            Attach(new RequestHandler());
        }

        public override void Initialize() {
            var requestHandler = GetModule<RequestHandler>();

            requestHandler.Register(nameof(IClientHostingRPC.HostGame), OnHostGame);

            base.Initialize();
        }

        public override IEnumerable<Type> IncludedTypes => _includedTypes;
        private static Type[] _includedTypes = new Type[] { typeof(HostedGame) };

        private async Task<ProcessResult> OnHostGame(RequestPacket request) {
            var sendRequest = new RequestPacket(request);
            var gsResp = await _server.Request(sendRequest, _gameServerUserId);

            return new ProcessResult(gsResp.Data);
        }
    }
}