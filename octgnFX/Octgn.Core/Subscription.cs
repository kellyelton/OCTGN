using Octgn.Library.Communication;
using Octgn.Site.Api;
using Octgn.Site.Api.Models;
using System;

namespace Octgn.Core
{
    public static class Subscription
    {
        private static log4net.ILog Log { get; } = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static Client Client { get; set; }

        public static bool IsActive {
            get => _isActive;
            private set {
                if (value == _isActive) return;

                _isActive = value;

                OnSubscriptionChanged(value);
            }
        }

        private static bool _isActive;

        private static bool _isUpdating;
        private static readonly object UpdateLock = new object();

        public static void Update() {
            Log.Info("Updating");
            lock (UpdateLock) {
                if(_isUpdating) {
                    Log.Info("Already updating, skipping");
                    return;
                }
                _isUpdating = true;
            }

            try {
                bool? ret = null;

                var username = Prefs.Username;
                var password = Prefs.Password.Decrypt();

                if (Client?.IsConnected ?? false) {
                    var client = new ApiClient();
                    var res = IsSubbedResult.UnknownError;

                    if (!String.IsNullOrWhiteSpace(password)) {
                        res = client.IsSubbed(username, password);
                    } else
                        res = client.IsSubbed(username, password);
                    switch (res) {
                        case IsSubbedResult.Ok:
                            ret = true;
                            break;
                        case IsSubbedResult.AuthenticationError:
                        case IsSubbedResult.NoSubscription:
                        case IsSubbedResult.SubscriptionExpired:
                            ret = false;
                            break;
                    }
                } else {
                    if (string.IsNullOrWhiteSpace(password)) ret = false;
                    else {
                        var client = new ApiClient();
                        var res = client.IsSubbed(username, password);
                        switch (res) {
                            case IsSubbedResult.Ok:
                                ret = true;
                                break;
                            case IsSubbedResult.AuthenticationError:
                            case IsSubbedResult.NoSubscription:
                            case IsSubbedResult.SubscriptionExpired:
                                ret = false;
                                break;
                        }
                    }
                }

                Log.InfoFormat("Is Subscribed = {0}", ret == null ? "Unknown" : ret.ToString());

                // We weren't able to check, just keep whatever value we already had.
                if (ret == null) return;

                IsActive = ret.Value;
            } catch (Exception e) {
                Log.Warn("ce", e);
            } finally {
                _isUpdating = false;
            }
        }

        public static event EventHandler<bool?> SubscriptionChanged;

        private static void OnSubscriptionChanged(bool? isActive) {
            SubscriptionChanged?.Invoke(null, isActive);
        }

    }
}
