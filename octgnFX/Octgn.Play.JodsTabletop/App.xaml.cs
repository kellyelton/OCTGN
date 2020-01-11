/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using log4net;
using Octgn.Communication;
using Octgn.Core;
using Octgn.Core.DataManagers;
using Octgn.Library;
using Octgn.Play;
using Octgn.Scripting;
using Octgn.Utils;
using Octgn.Wpf.Windows;
using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Octgn
{
    public partial class App : Application
    {
        private readonly static ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static bool IsReleaseTest { get; private set; }

        public static bool IsDeveloperMode { get; private set; }

        public static PlayWindow PlayWindow { get; private set; }

        public static Guid GameDefinitionId { get; private set; }

        protected override void OnStartup(StartupEventArgs e) {
            //TODO: Get from command args
            log4net.GlobalContext.Properties["gameid"] = "12345";

            LoggerFactory.DefaultMethod = (con)=> new Log4NetLogger(con.Name);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;

            IsReleaseTest = false;

            string nickname = null;
            string password = string.Empty;
            bool spectator = false;
            IPEndPoint host = null;

            var os = new Mono.Options.OptionSet()
            {
                    {"g|game=", x => GameDefinitionId = Guid.Parse(x)},
                    {"n|nickname=", x => nickname = x},
                    {"p|password=", x => password = x},
                    {"s|spectator", x => spectator = true},
                    {"h|host=", x => host = ParseHost(x)},
                    {"x|devmode", x => IsDeveloperMode = true}
                };

            os.Parse(e.Args);

            Log.Info("Creating Config class");
            try {
                Config.Instance = new Config();
            } catch (Exception ex) {
                //TODO: Find a better user experience for dealing with this error. Like a wizard to fix the problem or something.
                Log.Fatal("Error loading config", ex);

                MessageBox.Show($"Error loading Config:{Environment.NewLine}{ex}", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);

                Shutdown(1);

                return;
            }

            Environment.SetEnvironmentVariable("OCTGN_DATA", Config.Instance.DataDirectoryFull, EnvironmentVariableTarget.Process);

            var isTestRelease = false;
            try {
                isTestRelease = File.Exists(Path.Combine(Config.Instance.Paths.ConfigDirectory, "TEST"));
            } catch (Exception ex) {
                Log.Warn("Error checking for test mode", ex);
            }

            Diagnostics.IsTestRelease = isTestRelease;

            UpdateManager.Current = new UpdateManager(IsReleaseTest);

            Log.Info("Setting api path");
            Octgn.Site.Api.ApiClient.DefaultUrl = new Uri(AppConfig.WebsitePath);
            try {
                Log.Debug("Setting rendering mode.");
                RenderOptions.ProcessRenderMode = Prefs.UseHardwareRendering ? RenderMode.Default : RenderMode.SoftwareOnly;
            } catch (Exception) {
                // if the system gets mad, best to leave it alone.
            }

            Versioned.Setup(IsDeveloperMode);
            /* This section is automatically generated from the file Scripting/ApiVersions.xml. So, if you enjoy not getting pissed off, don't modify it.*/
            //START_REPLACE_API_VERSION
			Versioned.RegisterVersion(Version.Parse("3.1.0.0"),DateTime.Parse("2014-1-12"),ReleaseMode.Live );
			Versioned.RegisterVersion(Version.Parse("3.1.0.1"),DateTime.Parse("2014-1-22"),ReleaseMode.Live );
			Versioned.RegisterVersion(Version.Parse("3.1.0.2"),DateTime.Parse("2015-8-26"),ReleaseMode.Live );
			Versioned.RegisterFile("PythonApi", "pack://application:,,,/Scripting/Versions/3.1.0.0.py", Version.Parse("3.1.0.0"));
			Versioned.RegisterFile("PythonApi", "pack://application:,,,/Scripting/Versions/3.1.0.1.py", Version.Parse("3.1.0.1"));
			Versioned.RegisterFile("PythonApi", "pack://application:,,,/Scripting/Versions/3.1.0.2.py", Version.Parse("3.1.0.2"));
			//END_REPLACE_API_VERSION
            Versioned.Register<ScriptApi>();


            var game = GameManager.Get().GetById(GameDefinitionId);
            User octgnUser = null;

            Task.Run(() => {
                var gameEngine = GameEngine.Join(Dispatcher, game, octgnUser, nickname, password, spectator, host.Address, host.Port, IsDeveloperMode).Result;

                Dispatcher.InvokeAsync(() => {
                    //new LoadingWindow().Show();

                    App.PlayWindow = new PlayWindow(gameEngine);
                    App.PlayWindow.Show();
                    App.PlayWindow.Activate();
                });
            });
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
            var exception = (Exception)e.ExceptionObject;

            OnFatalError(exception);
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e) {
            OnFatalError(e.Exception);
        }

        private void OnFatalError(Exception exception) {
            Log.Fatal("Unhandled Exception", exception);

            var message = "There was an unexpected error and OCTGN will now close." + Environment.NewLine + Environment.NewLine + exception.Message;

            MessageBox.Show(message, "Unexpected Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private IPEndPoint ParseHost(string host) {
            var parts = host.Split(new char[1] { ':' }, 2);
            if (parts.Length != 2) throw new FormatException($"{host} is not a valid host");

            var ip = IPAddress.Parse(parts[0]);

            var port = int.Parse(parts[1]);

            if (port <= 0) throw new FormatException($"port {port} is invalid");

            return new IPEndPoint(ip, port);
        }
    }
}
