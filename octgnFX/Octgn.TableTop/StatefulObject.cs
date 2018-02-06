using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Octgn.TableTop
{
    public abstract class StatefulObject : ObservableCollection<StatefulObject>
    {
        public String Id { get; }
        public String Key { get; }

        public string Name {
            get => Get<string>();
            set => Set(value);
        }

        private readonly Dictionary<string, object> _properties = new Dictionary<string, object>();

        protected StatefulObject(string key = null, string name = null) {
            Id = Guid.NewGuid().ToString();

            Key = key;
            if (string.IsNullOrWhiteSpace(key)) {
                key = Id;
            }
            Key = key;
            Set(name, nameof(Name), false, false);
        }

        public void SetFromServer(string propertyName, object value) {
            Set(value, propertyName, false, true);
        }

        public event EventHandler<StatefulObjectPropertyChangedEventArgs> StatefulObjectPropertyChanged;

        /// <summary>
        /// Set a <see cref="StatefulObject"/>'s property.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="newValue"></param>
        /// <param name="propertyName"></param>
        /// <param name="broadcast">Broadcast the change to the server</param>
        /// <param name="fireChangeNotification"></param>
        /// <returns></returns>
        protected virtual bool Set<T>(T newValue, [CallerMemberName]string propertyName = null, bool broadcast = true, bool fireChangeNotification = true) {
            if(_properties.TryGetValue(propertyName, out var oldValue)
                && object.Equals(oldValue, newValue)) {
                // The property exists, and it equals the value we're
                // trying to set it to already.

                // There's no point in doing anything, it's already correct
                return false;
            }

            _properties[propertyName] = newValue;

            if(fireChangeNotification)
                Notify(newValue, oldValue, propertyName, broadcast);

            return true;
        }

        /// <summary>
        /// Notify listeners of <see cref="PropertyChanged"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="newValue"></param>
        /// <param name="oldValue"></param>
        /// <param name="propertyName"></param>
        /// <param name="broadcast">Broadcast the change to the server</param>
        protected virtual void Notify<T>(T newValue, T oldValue, [CallerMemberName]string propertyName = null, bool broadcast = true) {
            var args = new StatefulObjectPropertyChangedEventArgs(propertyName) {
                Object = this,
                NewValue = newValue,
                OldValue = oldValue,
                Broadcast = broadcast
            };

            base.OnPropertyChanged(args);
        }

        internal virtual T Get<T>([CallerMemberName]string propertyName = null) {
            return (T)_properties[propertyName];
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e) {
            if (e.NewItems != null) {
                foreach (var obj in e.NewItems.Cast<StatefulObject>()) {
                    obj.PropertyChanged += Child_PropertyChanged;
                }
            }
            if (e.OldItems != null) {
                foreach (var obj in e.OldItems?.Cast<StatefulObject>()) {
                    obj.PropertyChanged -= Child_PropertyChanged;
                }
            }

            base.OnCollectionChanged(e);
        }

        private void Child_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (sender == null) throw new ArgumentNullException(nameof(sender));
            if (e == null) throw new ArgumentNullException(nameof(e));
            var obj = ((StatefulObject)sender) ?? throw new ArgumentException($"{nameof(sender)} can't be type {sender.GetType()}", nameof(sender));

            var args = (StatefulObjectPropertyChangedEventArgs)e
                ?? throw new ArgumentException($"{nameof(e)} can't be type {e.GetType()}.");

            StatefulObjectPropertyChanged?.Invoke(sender, args);
        }
    }

    public class StatefulObjectPropertyChangedEventArgs : PropertyChangedEventArgs
    {
        public StatefulObjectPropertyChangedEventArgs(string propertyName) : base(propertyName) {
        }

        public StatefulObject Object { get; set; }
        public object NewValue { get; set; }
        public object OldValue { get; set; }

        /// <summary>
        /// Should we broadcast this change to other clients/the server
        /// </summary>
        public bool Broadcast { get; set; }
    }
}
