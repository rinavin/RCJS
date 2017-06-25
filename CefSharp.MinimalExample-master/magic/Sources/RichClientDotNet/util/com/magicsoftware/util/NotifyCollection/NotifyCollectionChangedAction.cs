using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.util.notifyCollection
{
   // Summary:
   //     Describes the action that caused a System.Collections.Specialized.INotifyCollectionChanged.CollectionChanged
   //     event.
   public enum NotifyCollectionChangedAction
   {
      // Summary:
      //     One or more items were added to the collection.
      Add = 0,
      //
      // Summary:
      //     One or more items were removed from the collection.
      Remove = 1,
      //
      // Summary:
      //     One or more items were replaced in the collection.
      Replace = 2,
      //
      // Summary:
      //     One or more items were moved within the collection.
      Move = 3,
      //
      // Summary:
      //     The content of the collection changed dramatically.
      Reset = 4,
   }
}
