using System;

namespace Octgn.Networking
{
    public class Mute : IDisposable
    {
        private readonly int _oldMuteId;
        private readonly IClient _client;

        public Mute(IClient client, int muteId)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));

            _oldMuteId = _client.Muted;

            _client.Muted = muteId;
        }

        public void Dispose() => _client.Muted = _oldMuteId;
    }
}