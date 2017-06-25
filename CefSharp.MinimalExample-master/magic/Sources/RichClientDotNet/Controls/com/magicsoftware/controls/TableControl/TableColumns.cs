using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.controls;
using System.Collections;
using System.ComponentModel;
using com.magicsoftware.util.notifyCollection;

namespace com.magicsoftware.controls
{
   //[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
   public class TableColumns : IList, IList<TableColumn>, INotifyCollectionChanged
   {
      readonly TableControl tableControl;
      public TableControl TableControl
      {
         get { return tableControl; }
      }

      /// <summary>
      /// .NET 2 does not have observable collection, use this implementation
      /// </summary>
      ObservableCollection<TableColumn> _columns;

      ObservableCollection<TableColumn> Columns
      {
         get { return _columns; }
         set
         {

            if (_columns != null)
               _columns.CollectionChanged -= _columns_CollectionChanged;
            _columns = value;
            if (_columns != null)
               _columns.CollectionChanged += _columns_CollectionChanged;
 
         }
      }

      void _columns_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
      {
         OnCollectionChanged(e);
         tableControl.OnComponentChanged();
      }

      protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
      {
         if (this.CollectionChanged != null)
         {
            this.CollectionChanged(this, e);

         }
       }

      #region IList<TableColumn> Members

      public TableColumns(TableControl tableControl)
      {
         this.tableControl = tableControl;
         Columns = new ObservableCollection<TableColumn>();
         
      }

      public int IndexOf(TableColumn item)
      {
         return _columns.IndexOf(item);
      }

      public void Insert(int index, TableColumn item)
      {
         item.TableControl = tableControl;
         tableControl.InsertHeaderSection(index, item.HeaderSection);
         _columns.Insert(index, item);

      }

      public void RemoveAt(int index)
      {
         tableControl.RemoveHeaderSection(index);
         _columns.RemoveAt(index);
      }

      public TableColumn this[int index]
      {
         get
         {
            return _columns[index];
         }
         set
         {
            _columns[index] = value;
         }
      }

      #endregion

      #region ICollection<TableColumn> Members

      public void Add(TableColumn item)
      {
         _columns.Add(item);
         item.TableControl = tableControl;
         tableControl.InsertHeaderSection(_columns.IndexOf(item), item.HeaderSection);
      }

      public void Clear()
      {
         for (int i = _columns.Count - 1; i >= 0; i--)
         {
            RemoveAt(i);
            
         }

         
      }



      public bool Contains(TableColumn item)
      {
         return _columns.Contains(item);
      }

      public void CopyTo(TableColumn[] array, int arrayIndex)
      {
         _columns.CopyTo(array, arrayIndex);
      }

      public int Count
      {
         get { return _columns.Count; }
      }

      public bool IsReadOnly
      {
         get { return false; }
      }

      public bool Remove(TableColumn item)
      {
         int index = IndexOf(item);
         if (index >= 0)
         {
            RemoveAt(index);
            return true;
         }
         return false;
      }

      #endregion

      #region IEnumerable<TableColumn> Members

      public IEnumerator<TableColumn> GetEnumerator()
      {
         return _columns.GetEnumerator();
      }

      #endregion

      #region IEnumerable Members

      System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
      {
         return _columns.GetEnumerator();
      }

      #endregion

      #region IList Members

      public int Add(object value)
      {
         Add((TableColumn) value);
         return IndexOf(value);
      }

      public bool Contains(object value)
      {
        return Contains((TableColumn) value);
      }

      public int IndexOf(object value)
      {
         return IndexOf((TableColumn)value);
      }

      public void Insert(int index, object value)
      {
         Insert(index, (TableColumn)value);
      }

      public bool IsFixedSize
      {
         get { return false; }
      }

      public void Remove(object value)
      {
         Remove((TableColumn)value);
      }

      object IList.this[int index]
      {
         get
         {
           return this[index];
         }
         set
         {
            this[index] = (TableColumn)value;
         }
      }

      #endregion

      #region ICollection Members

      public void CopyTo(Array array, int index)
      {
         CopyTo((TableColumn[])array, index);
      }

      public bool IsSynchronized
      {
         get { return false; }
      }

      public object SyncRoot
      {
         get { throw new NotImplementedException(); }
      }

      #endregion

      public void Sort(int[] order)
      {
         ObservableCollection<TableColumn> tempColumns = new ObservableCollection<TableColumn>();

         // 1. sort the columns
         for (int i = 0; i < order.Length; i++)
            tempColumns.Insert(i, _columns[order[i]]);
         _columns.Clear();
         _columns = tempColumns;
         tableControl.sortHeader(order);

      }

      public void Move(int oldIndex, int newIndex)
      {
         TableColumn column = this[oldIndex];
         //synchronize header after columns move
         tableControl.RemoveHeaderSection(oldIndex);
         tableControl.InsertHeaderSection(newIndex, column.HeaderSection);
         _columns.Move(oldIndex, newIndex);
     }
     
      #region INotifyCollectionChanged Members

      public event NotifyCollectionChangedEventHandler CollectionChanged;

      #endregion

      

      
   }
}
