using System;
using System.Text;
using System.Collections.Generic;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace com.magicsoftware.util.notifyCollection
{

   [Serializable]
   public class ObservableCollection<T> : Collection<T>, INotifyCollectionChanged, INotifyPropertyChanged
   {
      // Fields
      private SimpleMonitor _monitor;
      private const string CountString = "Count";
      private const string IndexerName = "Item[]";

      // Events
      [field: NonSerialized]
      public virtual event NotifyCollectionChangedEventHandler CollectionChanged;

      [field: NonSerialized]
      protected event PropertyChangedEventHandler PropertyChanged;

      event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
      {
         add
         {
            PropertyChanged += value;
         }
         remove
         {
            PropertyChanged -= value;
         }
      }

      // Methods
      public ObservableCollection()
      {
         this._monitor = new SimpleMonitor();
      }

      public ObservableCollection(IEnumerable<T> collection)
      {
         this._monitor = new SimpleMonitor();
         if(collection == null)
         {
            throw new ArgumentNullException("collection");
         }
         this.CopyFrom(collection);
      }

      public ObservableCollection(List<T> list)
         : base((list != null) ? new List<T>(list.Count) : list)
      {
         this._monitor = new SimpleMonitor();
         this.CopyFrom(list);
      }

      protected IDisposable BlockReentrancy()
      {
         this._monitor.Enter();
         return this._monitor;
      }

      protected void CheckReentrancy()
      {
         if((this._monitor.Busy && (this.CollectionChanged != null)) && (this.CollectionChanged.GetInvocationList().Length > 1))
         {
            throw new InvalidOperationException("ObservableCollectionReentrancyNotAllowed");
         }
      }

      protected override void ClearItems()
      {
         this.CheckReentrancy();
         base.ClearItems();
         this.OnPropertyChanged(CountString);
         this.OnPropertyChanged(IndexerName);
         this.OnCollectionReset();
      }

      private void CopyFrom(IEnumerable<T> collection)
      {
         IList<T> items = base.Items;
         if((collection != null) && (items != null))
         {
            using(IEnumerator<T> enumerator = collection.GetEnumerator())
            {
               while(enumerator.MoveNext())
               {
                  items.Add(enumerator.Current);
               }
            }
         }
      }

      protected override void InsertItem(int index, T item)
      {
         this.CheckReentrancy();
         base.InsertItem(index, item);
         this.OnPropertyChanged(CountString);
         this.OnPropertyChanged(IndexerName);
         this.OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
      }

      public void Move(int oldIndex, int newIndex)
      {
         this.MoveItem(oldIndex, newIndex);
      }

      protected virtual void MoveItem(int oldIndex, int newIndex)
      {
         this.CheckReentrancy();
         T item = base[oldIndex];
         base.RemoveItem(oldIndex);
         base.InsertItem(newIndex, item);
         this.OnPropertyChanged(IndexerName);
         this.OnCollectionChanged(NotifyCollectionChangedAction.Move, item, newIndex, oldIndex);
      }

      protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
      {
         if(this.CollectionChanged != null)
         {
            using(this.BlockReentrancy())
            {
               this.CollectionChanged(this, e);
            }
         }
      }

      private void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index)
      {
         this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index));
      }

      private void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index, int oldIndex)
      {
         this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index, oldIndex));
      }

      private void OnCollectionChanged(NotifyCollectionChangedAction action, object oldItem, object newItem, int index)
      {
         this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, newItem, oldItem, index));
      }

      private void OnCollectionReset()
      {
         this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
      }

      protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
      {
         if(this.PropertyChanged != null)
         {
            this.PropertyChanged(this, e);
         }
      }

      private void OnPropertyChanged(string propertyName)
      {
         this.OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
      }

      protected override void RemoveItem(int index)
      {
         this.CheckReentrancy();
         T item = base[index];
         base.RemoveItem(index);
         this.OnPropertyChanged(CountString);
         this.OnPropertyChanged(IndexerName);
         this.OnCollectionChanged(NotifyCollectionChangedAction.Remove, item, index);
      }

      protected override void SetItem(int index, T item)
      {
         this.CheckReentrancy();
         T oldItem = base[index];
         base.SetItem(index, item);
         this.OnPropertyChanged(IndexerName);
         this.OnCollectionChanged(NotifyCollectionChangedAction.Replace, oldItem, item, index);
      }

      // Nested Types
      [Serializable]
      private class SimpleMonitor : IDisposable
      {
         // Fields
         private int _busyCount;

         // Methods
         public void Dispose()
         {
            this._busyCount--;
         }

         public void Enter()
         {
            this._busyCount++;
         }

         // Properties
         public bool Busy
         {
            get
            {
               return (this._busyCount > 0);
            }
         }
      }
   }
}
 
