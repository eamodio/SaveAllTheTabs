using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace SaveAllTheTabs.Polyfills
{
    public sealed class TrulyObservableCollection<T> : ObservableCollection<T>
        where T : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler CollectionItemChanged;

        public TrulyObservableCollection()
            : base()
        {
            CollectionChanged += OnObservableCollectionCollectionChanged;
        }

        public TrulyObservableCollection(List<T> list)
            : base(list)
        {
            CollectionChanged += OnObservableCollectionCollectionChanged;

            AddSubscriptions(Items);
        }

        private void AddSubscriptions(IEnumerable<T> items)
        {
            if (items == null)
            {
                return;
            }

            foreach (var item in items)
            {
                item.PropertyChanged += OnItemPropertyChanged;
            }
        }

        private void RemoveSubscriptions(IEnumerable<T> items)
        {
            if (items == null)
            {
                return;
            }

            foreach (var item in items)
            {
                item.PropertyChanged -= OnItemPropertyChanged;
            }
        }

        private void OnObservableCollectionCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            AddSubscriptions(e.NewItems?.Cast<T>());
            RemoveSubscriptions(e.OldItems?.Cast<T>());
        }

        private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            CollectionItemChanged?.Invoke(sender, e);
            //var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, sender, sender, IndexOf((T)sender));
            //OnCollectionChanged(args);
        }
    }
}