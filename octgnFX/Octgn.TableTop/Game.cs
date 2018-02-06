using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Octgn.TableTop
{
    public class Game : StatefulObject
    {
        public IEnumerable<StatefulObject> Players => this.OfType<Player>();

        public Game(string key = null, string name = null) : base(key, name) {
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e) {
            var newPlayers = e.NewItems?.OfType<Player>().ToArray();
            var affectedPlayers = e.OldItems?.OfType<Player>().ToArray();

            base.OnCollectionChanged(e);

            if (newPlayers?.Length + affectedPlayers?.Length > 0) {
                Notify(newPlayers, null, nameof(Players), false);
            }
        }
    }
}
