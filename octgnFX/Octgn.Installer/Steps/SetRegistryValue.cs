using Microsoft.Win32;
using System;
using System.Threading.Tasks;

namespace Octgn.Installer.Steps
{
    public class SetRegistryValue : Step
    {
        public string SubKey { get; }

        public string Name { get; }

        public object Value { get; }

        public RegistryValueKind ValueKind { get; }

        public RegistryKey Root { get; }

        public SetRegistryValue(RegistryKey root, string subKey, string name, string value) {
            Root = root ?? throw new ArgumentNullException(nameof(root));
            if (string.IsNullOrWhiteSpace(subKey)) throw new ArgumentNullException(nameof(subKey));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));

            SubKey = subKey;
            Name = name;
            Value = value;
            ValueKind = RegistryValueKind.String;
        }

        public override Task Execute(Context context) {
            return Task.Run(() => {
                RegistryKey subkey = null;
                try {
                    subkey = Root.OpenSubKey(SubKey, true);

                    if (subkey == null) {
                        subkey = Root.CreateSubKey(SubKey, RegistryKeyPermissionCheck.ReadWriteSubTree);
                    }

                    subkey.SetValue(Name, Value, ValueKind);
                } finally {
                    subkey?.Dispose();
                }
            });
        }
    }
}
