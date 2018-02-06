namespace Octgn.TableTop
{
    public class Image : TableElement
    {
        public string Path {
            get => Get<string>();
            set => Set(value);
        }

        public double Width {
            get => Get<double>();
            set => Set(value);
        }

        public double Height {
            get => Get<double>();
            set => Set(value);
        }

        protected Image(long id, string name) : base(id, name) {
            Name = name;
        }
    }
}
