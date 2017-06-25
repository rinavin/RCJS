using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace com.magicsoftware.unipaas.management.gui
{
   /// <summary>
   /// This class holds dataTable Objects and its corresponding DVControlManager.
   /// </summary>
   public class DVDataTableCollection
   {
      static private Dictionary<object, DVControlManager> _dvDataTableCollection = new Dictionary<object, DVControlManager>();

      private static Object _dvDataTableCollectionLock = new Object();

      /// <summary>
      /// Adds element to dictionary.
      /// </summary>
      /// <param name="dataTable"></param>
      /// <param name="dvControlManager"></param>
      static internal void Add(Object dataTable, DVControlManager dvControlManager)
      {
         lock (_dvDataTableCollectionLock)
         {
            _dvDataTableCollection.Add(dataTable, dvControlManager);
         }
      }

      /// <summary>
      /// Removes element from dictionary.
      /// </summary>
      /// <param name="dataTable"></param>
      static internal void Remove(object dataTable)
      {
         lock (_dvDataTableCollectionLock)
         {
            if (_dvDataTableCollection.ContainsKey(dataTable))
               _dvDataTableCollection.Remove(dataTable);
         }
      }

      /// <summary>
      /// Get DVControlManager.
      /// </summary>
      /// <param name="dataTable"></param>
      /// <returns></returns>
      static public DVControlManager GetDVControlManager(object dataTable)
      {
         lock (_dvDataTableCollectionLock)
         {
            if (_dvDataTableCollection.ContainsKey(dataTable))
               return _dvDataTableCollection[dataTable];
            else
               return null;
         }
      }
   }
}

