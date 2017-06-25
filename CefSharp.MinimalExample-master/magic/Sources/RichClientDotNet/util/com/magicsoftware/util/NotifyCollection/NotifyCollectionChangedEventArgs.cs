using System;
using System.Text;
using System.Diagnostics;
using System.Collections;

namespace com.magicsoftware.util.notifyCollection
{
   // Summary:
   //     Provides data for the System.Collections.Specialized.INotifyCollectionChanged.CollectionChanged
   //     event.
   public class NotifyCollectionChangedEventArgs : EventArgs
   {
      // Fields
      private NotifyCollectionChangedAction _action;
      private IList _newItems;
      private int _newStartingIndex;
      private IList _oldItems;
      private int _oldStartingIndex;

      // Methods
      public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action)
      {
         _newStartingIndex = -1;
         _oldStartingIndex = -1;
         if (action != NotifyCollectionChangedAction.Reset)
         {
            throw new ArgumentException("WrongActionForCtor", "action");
         }
         InitializeAdd(action, null, -1);
      }

      public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList changedItems)
      {
         _newStartingIndex = -1;
         _oldStartingIndex = -1;
         if (((action != NotifyCollectionChangedAction.Add) && (action != NotifyCollectionChangedAction.Remove)) && (action != NotifyCollectionChangedAction.Reset))
         {
            throw new ArgumentException("MustBeResetAddOrRemoveActionForCtor", "action");
         }
         if (action == NotifyCollectionChangedAction.Reset)
         {
            if (changedItems != null)
            {
               throw new ArgumentException("ResetActionRequiresNullItem", "action");
            }
            InitializeAdd(action, null, -1);
         }
         else
         {
            if (changedItems == null)
            {
               throw new ArgumentNullException("changedItems");
            }
            InitializeAddOrRemove(action, changedItems, -1);
         }
      }

      public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object changedItem)
      {
         _newStartingIndex = -1;
         _oldStartingIndex = -1;
         if (((action != NotifyCollectionChangedAction.Add) && (action != NotifyCollectionChangedAction.Remove)) && (action != NotifyCollectionChangedAction.Reset))
         {
            throw new ArgumentException("MustBeResetAddOrRemoveActionForCtor", "action");
         }
         if (action == NotifyCollectionChangedAction.Reset)
         {
            if (changedItem != null)
            {
               throw new ArgumentException("ResetActionRequiresNullItem", "action");
            }
            InitializeAdd(action, null, -1);
         }
         else
         {
            InitializeAddOrRemove(action, new object[] { changedItem }, -1);
         }
      }

      public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList newItems, IList oldItems)
      {
         _newStartingIndex = -1;
         _oldStartingIndex = -1;
         if (action != NotifyCollectionChangedAction.Replace)
         {
            throw new ArgumentException("WrongActionForCtor", "action");
         }
         if (newItems == null)
         {
            throw new ArgumentNullException("newItems");
         }
         if (oldItems == null)
         {
            throw new ArgumentNullException("oldItems");
         }
         InitializeMoveOrReplace(action, newItems, oldItems, -1, -1);
      }

      public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList changedItems, int startingIndex)
      {
         _newStartingIndex = -1;
         _oldStartingIndex = -1;
         if (((action != NotifyCollectionChangedAction.Add) && (action != NotifyCollectionChangedAction.Remove)) && (action != NotifyCollectionChangedAction.Reset))
         {
            throw new ArgumentException("MustBeResetAddOrRemoveActionForCtor", "action");
         }
         if (action == NotifyCollectionChangedAction.Reset)
         {
            if (changedItems != null)
            {
               throw new ArgumentException("ResetActionRequiresNullItem", "action");
            }
            if (startingIndex != -1)
            {
               throw new ArgumentException("ResetActionRequiresIndexMinus1", "action");
            }
            InitializeAdd(action, null, -1);
         }
         else
         {
            if (changedItems == null)
            {
               throw new ArgumentNullException("changedItems");
            }
            if (startingIndex < -1)
            {
               throw new ArgumentException("IndexCannotBeNegative", "startingIndex");
            }
            InitializeAddOrRemove(action, changedItems, startingIndex);
         }
      }

      public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object changedItem, int index)
      {
         _newStartingIndex = -1;
         _oldStartingIndex = -1;
         if (((action != NotifyCollectionChangedAction.Add) && (action != NotifyCollectionChangedAction.Remove)) && (action != NotifyCollectionChangedAction.Reset))
         {
            throw new ArgumentException("MustBeResetAddOrRemoveActionForCtor", "action");
         }
         if (action == NotifyCollectionChangedAction.Reset)
         {
            if (changedItem != null)
            {
               throw new ArgumentException("ResetActionRequiresNullItem", "action");
            }
            if (index != -1)
            {
               throw new ArgumentException("ResetActionRequiresIndexMinus1", "action");
            }
            InitializeAdd(action, null, -1);
         }
         else
         {
            InitializeAddOrRemove(action, new object[] { changedItem }, index);
         }
      }

      public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object newItem, object oldItem)
      {
         _newStartingIndex = -1;
         _oldStartingIndex = -1;
         if (action != NotifyCollectionChangedAction.Replace)
         {
            throw new ArgumentException("WrongActionForCtor", "action");
         }
         InitializeMoveOrReplace(action, new object[] { newItem }, new object[] { oldItem }, -1, -1);
      }

      public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList newItems, IList oldItems, int startingIndex)
      {
         _newStartingIndex = -1;
         _oldStartingIndex = -1;
         if (action != NotifyCollectionChangedAction.Replace)
         {
            throw new ArgumentException("WrongActionForCtor", "action");
         }
         if (newItems == null)
         {
            throw new ArgumentNullException("newItems");
         }
         if (oldItems == null)
         {
            throw new ArgumentNullException("oldItems");
         }
         InitializeMoveOrReplace(action, newItems, oldItems, startingIndex, startingIndex);
      }

      public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList changedItems, int index, int oldIndex)
      {
         _newStartingIndex = -1;
         _oldStartingIndex = -1;
         if (action != NotifyCollectionChangedAction.Move)
         {
            throw new ArgumentException("WrongActionForCtor", "action");
         }
         if (index < 0)
         {
            throw new ArgumentException("IndexCannotBeNegative", "index");
         }
         InitializeMoveOrReplace(action, changedItems, changedItems, index, oldIndex);
      }

      public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object changedItem, int index, int oldIndex)
      {
         _newStartingIndex = -1;
         _oldStartingIndex = -1;
         if (action != NotifyCollectionChangedAction.Move)
         {
            throw new ArgumentException("WrongActionForCtor", "action");
         }
         if (index < 0)
         {
            throw new ArgumentException("IndexCannotBeNegative", "index");
         }
         object[] newItems = new object[] { changedItem };
         InitializeMoveOrReplace(action, newItems, newItems, index, oldIndex);
      }

      public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object newItem, object oldItem, int index)
      {
         _newStartingIndex = -1;
         _oldStartingIndex = -1;
         if (action != NotifyCollectionChangedAction.Replace)
         {
            throw new ArgumentException("WrongActionForCtor", "action");
         }
         InitializeMoveOrReplace(action, new object[] { newItem }, new object[] { oldItem }, index, index);
      }

      private void InitializeAdd(NotifyCollectionChangedAction action, IList newItems, int newStartingIndex)
      {
         _action = action;
         _newItems = (newItems == null) ? null : new ArrayList(newItems);
         _newStartingIndex = newStartingIndex;
      }

      private void InitializeAddOrRemove(NotifyCollectionChangedAction action, IList changedItems, int startingIndex)
      {
         if (action == NotifyCollectionChangedAction.Add)
         {
            InitializeAdd(action, changedItems, startingIndex);
         }
         else if (action == NotifyCollectionChangedAction.Remove)
         {
            InitializeRemove(action, changedItems, startingIndex);
         }
         else
         {
            Debug.Assert(false);//, "Unsupported action: {0}".Arg(action));
         }
      }

      private void InitializeMoveOrReplace(NotifyCollectionChangedAction action, IList newItems, IList oldItems, int startingIndex, int oldStartingIndex)
      {
         InitializeAdd(action, newItems, startingIndex);
         InitializeRemove(action, oldItems, oldStartingIndex);
      }

      private void InitializeRemove(NotifyCollectionChangedAction action, IList oldItems, int oldStartingIndex)
      {
         _action = action;
         _oldItems = (oldItems == null) ? null : new ArrayList(oldItems);
         _oldStartingIndex = oldStartingIndex;
      }

      // Properties
      public NotifyCollectionChangedAction Action
      {
         get
         {
            return _action;
         }
      }

      public IList NewItems
      {
         get
         {
            return _newItems;
         }
      }

      public int NewStartingIndex
      {
         get
         {
            return _newStartingIndex;
         }
      }

      public IList OldItems
      {
         get
         {
            return _oldItems;
         }
      }

      public int OldStartingIndex
      {
         get
         {
            return _oldStartingIndex;
         }
      }
   }




   public delegate void NotifyCollectionChangedEventHandler(object sender, NotifyCollectionChangedEventArgs e);
}
