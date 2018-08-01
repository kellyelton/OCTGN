/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */
using System.Threading.Tasks;
using Octgn.Communication;
using System.Threading;
using Octgn.Communication.Packets;
using Octgn.Site.Api;
using System;
using System.Reflection;

namespace Octgn.Library.Communication
{
    public class SessionHandshaker : IHandshaker
    {
        private static ILogger Log = LoggerFactory.Create(MethodBase.GetCurrentMethod().DeclaringType);

        public string SessionKey { get; set; }
        public string UserId { get; set; }
        public string DeviceId { get; set; }

        public async Task<HandshakeResult> Handshake(IConnection connection, CancellationToken cancellationToken) {
            var req = new HandshakeRequestPacket("session");
            req["sessionKey"] = SessionKey;
            req["userId"] = UserId;
            req["deviceId"] = DeviceId;

            var result = await connection.Request(req, cancellationToken);

            return result.As<HandshakeResult>();
        }

        private ApiClient _apiClient = new ApiClient();

        public async Task<HandshakeResult> OnHandshakeRequest(HandshakeRequestPacket request, IConnection connection, CancellationToken cancellationToken) {
            if (request.HandshakeType != "session")
                throw new InvalidOperationException($"This authentication handler is a '{request.HandshakeType}' authentication type, not a 'session' authentication type.");

            var sessionKey = (string)request["sessionKey"];
            var userId = (string)request["userId"];
            var deviceId = (string)request["deviceId"];

            try {
                if (!await _apiClient.ValidateSession(userId, deviceId, sessionKey, cancellationToken)) {
                    return new HandshakeResult {
                        ErrorCode = "SessionInvalid",
                        Successful = false
                    };
                }

                cancellationToken.ThrowIfCancellationRequested();

                var apiUser = await _apiClient.UserFromUserId(userId, cancellationToken);

                var user = new User(userId, apiUser.UserName);

                return new HandshakeResult {
                    Successful = true,
                    User = user
                };
            } catch (TaskCanceledException ex) {
                Log.Warn($"{nameof(OnHandshakeRequest)}", ex);

                return new HandshakeResult {
                    ErrorCode = "Cancelled",
                    Successful = false
                };
            } catch (ApiClientException ex) {
                Log.Warn($"{nameof(OnHandshakeRequest)}", ex);

                return new HandshakeResult {
                    ErrorCode = "ApiClientError",
                    Successful = false
                };
            }
        }
    }
}
