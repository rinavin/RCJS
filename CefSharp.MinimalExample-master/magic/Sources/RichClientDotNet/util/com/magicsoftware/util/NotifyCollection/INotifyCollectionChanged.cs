using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.util.notifyCollection
{
   public interface INotifyCollectionChanged
   {
      // Summary:
      //     Occurs when the collection changes.
      event NotifyCollectionChangedEventHandler CollectionChanged;
   }
}
