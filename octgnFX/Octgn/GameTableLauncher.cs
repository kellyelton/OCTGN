using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Octgn
{
    public class GameTableLauncher
    {
        private readonly bool _isDebug;

        public GameTableLauncher() {
#if DEBUG
            _isDebug = true;
#endif
        }

        public void Join(Guid gameId, string nickname, string host, int port, string password = null, bool spectator = false, bool devMode = false) {
            if (String.IsNullOrWhiteSpace(nickname)) throw new ArgumentNullException(nameof(nickname));
            if (String.IsNullOrWhiteSpace(host)) throw new ArgumentNullException(nameof(host));

            var arguments = new List<string>();

            arguments.Add("join-game");
            arguments.Add($"{host}:{port}");
            arguments.Add(gameId.ToString());
            arguments.Add(nickname);
            if (!string.IsNullOrWhiteSpace(password)) {
                arguments.Add(BuildArgument("password", password));
            }
            if (spectator) {
                arguments.Add(BuildArgument("spectator"));
            }
            if (devMode) {
                arguments.Add(BuildArgument("devmode"));
            }

            LaunchTabletopProcess(arguments);
        }

        public void Host() {
            throw new NotImplementedException();
        }

        public void Replay() {
            throw new NotImplementedException();
        }

        private string BuildArgument(string name) {
            return $"-{name}";
        }

        private string BuildArgument(string name, string value) {
            return $"-{name}=\"{value}\"";
        }

        private void LaunchTabletopProcess(IEnumerable<string> arguments) {
            if (arguments == null) throw new ArgumentNullException(nameof(arguments));

            string path = null;

            if (_isDebug) {
                path = Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\Octgn.Play.JodsTabletop\bin\Debug\Octgn.Play.JodsTabletop.exe");
                path = Path.GetFullPath(path);
            } else {
                path = Directory.GetCurrentDirectory() + "\\TableTop\\Octgn.Play.JodsTabletop.exe";
            }

            var process = new Process();
            process.StartInfo.Arguments = String.Join(" ", arguments);
            process.StartInfo.FileName = path;

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
        }
    }
}
