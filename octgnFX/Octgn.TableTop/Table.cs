namespace Octgn.TableTop
{
    /// <summary>
    /// The main surface that holds elements
    /// </summary>
    public class Table : StatefulObject
    {
        public double Width {
            get => Get<double>();
            set => Set(value);
        }

        public double Height{
            get => Get<double>();
            set => Set(value);
        }

        public Table(string key = null, string name = null) : base(key, name) {
        }
    }
}
