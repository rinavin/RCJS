using System;
using System.Collections;
using com.magicsoftware.unipaas.management.data;

namespace com.magicsoftware.unipaas.dotnet
{
   /// <summary>
   /// This class holds info about DotNet Objects in DNObjectCollection and its corrosponding field.
   /// </summary>
   public class DNObjectFieldCollection
   {
      private Hashtable _objTbl = new Hashtable();

      private static Object _fldsLock = new Object();

      /// <summary>
      /// create an entry into the ObjectFieldTable
      /// </summary>
      /// <returns></returns>
      public void createEntry(int key, Field fld)
      {
         lock (_fldsLock)
         {
            if (!_objTbl.ContainsKey(key))
               _objTbl[key] = fld;
         }
      }

      /// <summary>
      /// gets the field at 'key'
      /// </summary>
      /// <param name="key"></param>
      /// <returns></returns>
      public Field get(int key)
      {
         Field fld = null;

         lock (_fldsLock)
         {
            if (_objTbl.ContainsKey(key))
               fld = (Field)_objTbl[key];
         }
         return fld;
      }

      /// <summary>
      /// remove the entry with 'key'
      /// </summary>
      /// <param name="key"></param>
      public void remove(int key)
      {
         try
         {
            lock (_fldsLock)
            {
               _objTbl.Remove(key);
            }
         }
         catch (Exception) { }
      }
   }
}

