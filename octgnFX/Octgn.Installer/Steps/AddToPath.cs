using System;
using System.Threading.Tasks;

namespace Octgn.Installer.Steps
{
    public class AddToPath : Step
    {
        public string Path { get; }

        public Scope Scope { get; }

        private readonly EnvironmentVariableTarget _target;

        public AddToPath(string path, Scope scope) {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));

            Path = path;

            Scope = scope;

            switch (Scope) {
                case Scope.User:
                    _target = EnvironmentVariableTarget.User;
                    break;
                case Scope.Machine:
                    _target = EnvironmentVariableTarget.Machine;
                    break;
                default: throw new InvalidOperationException($"{nameof(Scope)} {Scope} not implemented.");
            }
        }

        public override Task Execute(Context context) {
            var pathString = Environment.GetEnvironmentVariable("PATH", _target);

            if (!pathString.Contains(Path)) {
                pathString += ";" + Path;

                Environment.SetEnvironmentVariable("PATH", pathString, _target);
            }

            return Task.Delay(0);
        }
    }
}
