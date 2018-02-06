namespace Octgn.TableTop
{
    public class Player : StatefulObject
    {
        public bool IsLocal {
            get => Get<bool>();
        }

        public Player(bool isLocal, string key = null, string name = null)  : base(key, name) {
            Set(isLocal, nameof(IsLocal), false, false);
        }
    }
}
