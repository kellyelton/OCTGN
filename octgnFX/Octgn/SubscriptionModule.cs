/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */
using System.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using Octgn.Core;
using log4net;
using Octgn.Communication;

namespace Octgn
{
    public class SubscriptionModule
    {
        internal static ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region Singleton

        internal static SubscriptionModule SingletonContext { get; set; }

        private static readonly object SubscriptionModuleSingletonLocker = new object();

        public static SubscriptionModule Get()
        {
            lock (SubscriptionModuleSingletonLocker) return SingletonContext ?? (SingletonContext = new SubscriptionModule());
        }

        internal SubscriptionModule()
        {
            Log.Info("Creating");
            this.SubTypes = new List<SubType>();
            SubTypes.Add(new SubType { Description = "$3.00 per month", Name = "silver" });
            SubTypes.Add(new SubType { Description = "$33.00 per year", Name = "gold" });
            CheckTimer = new Timer(TimeSpan.FromMinutes(5).TotalMilliseconds);
            CheckTimer.Elapsed += CheckTimerOnElapsed;
            CheckTimer.Start();
            Log.Info("Created");
            Task.Factory.StartNew(() => CheckTimerOnElapsed(null, null)).ContinueWith(
                x =>
                { if (x.Exception != null) Log.Info("Get Is Subbed Failed", x.Exception); });
            Subscription.Client = Program.LobbyClient;
            Program.LobbyClient.Connected += LobbyClient_Connected;
            var sti = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/subscriberbenefits.txt"));
            var benifits = new List<string>();
            using (var sr = new StreamReader(sti.Stream))
            {
                var l = sr.ReadLine();
                while (l != null)
                {
                    benifits.Add(l);
                    l = sr.ReadLine();
                }
            }
            Benefits = benifits;
        }

        #endregion Singleton

        public List<string> Benefits { get; internal set; }

        public List<SubType> SubTypes { get; set; }

        public bool IsSubscribed => Subscription.IsActive;

        internal void UpdateIsSubbed()
        {
            Log.Info("Getting IsSubscribed");

            var previousValue = IsSubscribed;

            Subscription.Update();

            var newValue = IsSubscribed;

            if (previousValue != newValue) {
                OnIsSubbedChanged(newValue);
            }
        }

        public event Action<bool> IsSubbedChanged;

        public string GetSubscribeUrl(SubType type)
        {
            return AppConfig.WebsitePath;
        }

        protected virtual void OnIsSubbedChanged(bool obj)
        {
            this.IsSubbedChanged?.Invoke(obj);
        }

        internal Timer CheckTimer { get; set; }

        private void CheckTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            Log.Info("Check timer elapsed");
            this.UpdateIsSubbed();
        }

        private void LobbyClient_Connected(object sender, ConnectedEventArgs results)
        {
            this.UpdateIsSubbed();
        }

    }
    public class SubType
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public override string ToString()
        {
            return Description;
        }
    }
}