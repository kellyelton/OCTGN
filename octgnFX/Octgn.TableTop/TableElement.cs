namespace Octgn.TableTop
{
    public abstract class TableElement : StatefulObject
    {
        public double X {
            get => Get<double>();
            set => Set(value);
        }

        public double Y {
            get => Get<double>();
            set => Set(value);
        }

        public double RelativeAngle {
            get => Get<double>();
            set => Set(value);
        }

        public double Scale {
            get => Get<double>();
            set => Set(value);
        }

        public bool IsVisible {
            get => Get<bool>();
            set => Set(value);
        }

        public int Layer {
            get => Get<int>();
            set => Set(value);
        }

        public Player Owner {
            get => Get<Player>();
            set => Set(value);
        }

        public Player Controller {
            get => Get<Player>();
            set => Set(value);
        }

        protected TableElement(string key = null, string name = null) : base(key, name) {
        }
    }
}
