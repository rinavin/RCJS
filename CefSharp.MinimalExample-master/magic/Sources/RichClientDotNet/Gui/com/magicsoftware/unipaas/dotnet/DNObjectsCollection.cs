using System;
using System.Collections.Generic;
using com.magicsoftware.unipaas.util;
using System.Diagnostics;

namespace com.magicsoftware.unipaas.dotnet
{
   /// <summary>
   /// This class holds info about all actual DotNet Objects.
   /// the class returns 1-based keys (key 0 indicates no key).
   /// </summary>
   public class DNObjectsCollection
   {
      private List<DNObjectsCollectionEntry> _objects = new List<DNObjectsCollectionEntry>();

      private static Object _objectsLock = new Object();

      /// <summary>
      /// create a empty entry into the DNObjectsCollection 
      /// </summary>
      /// <param name="dnType">the type of the object that will be saved in the entry</param>
      /// <returns>created key</returns>
      public int CreateEntry(Type dnType)
      {
         return CreateEntry(dnType, null);
      }

      /// <summary>
      /// create a empty entry into the DNObjectsCollection 
      /// </summary>
      /// <param name="dnType">the type of the object that will be saved in the entry</param>
      /// <param name="currentTaskDNEventsNames">comma-delimited string of events that are handled for the .net variable in the task containing the variable</param>
      /// <returns>created key</returns>
      internal int CreateEntry(Type dnType, String currentTaskDNEventsNames)
      {
         int key = 0;
         DNObjectsCollectionEntry entry = new DNObjectsCollectionEntry(dnType, null, currentTaskDNEventsNames);

         lock (_objectsLock)
         {
            for (int i = 0; i < _objects.Count; i++)
               if (_objects[i] == null)
               {
                  _objects[i] = entry;
                  key = i + 1; //the class returns 1-based keys (key 0 indicates no key)
                  break;
               }
            if (key == 0)
            {
               _objects.Add(entry);
               key = _objects.Count;
            }

            Debug.Assert(key > 0 && key <= _objects.Count); //the class returns 1-based keys (key 0 indicates no key)
         }
         return key;
      }

      /// <summary>
      /// updates the existing object in DNObjectsCollection
      /// Note: this method shall not create a new entry if there exists no entry at 'key'.
      /// it will always replace the existing object entry at 'key'.
      /// </summary>
      /// <param name="key"></param>
      /// <param name="obj"></param>
      public void Update(int key, Object obj)
      {
         lock (_objectsLock)
         {
            Debug.Assert(key > 0 && key <= _objects.Count); //the class returns 1-based keys (key 0 indicates no key)

            // cast the given object into the type that was set to the entry when created, 
            // and save it in the entry.
            DNObjectsCollectionEntry entry = _objects[key - 1];
            if (obj != null && entry.DNType != null)
               obj = ReflectionServices.DynCast(obj, entry.DNType);
            entry.DNObj = obj;
         }
      }

      /// <summary>
      /// gets the object at 'key'
      /// </summary>
      /// <param name="key"></param>
      /// <returns></returns>
      public Object GetDNObj(int key)
      {
         lock (_objectsLock)
         {
            Debug.Assert(key > 0 && key <= _objects.Count); //the class returns 1-based keys (key 0 indicates no key)
            return _objects[key - 1].DNObj;
         }
      }

      /// <summary>
      /// gets the .Net type at 'key'
      /// </summary>
      /// <param name="key"></param>
      /// <returns></returns>
      public Type GetDNType(int key)
      {
         lock (_objectsLock)
         {
            Debug.Assert(key > 0 && key <= _objects.Count); //the class returns 1-based keys (key 0 indicates no key)
            return _objects[key - 1].DNType;
         }
      }

      /// <summary></summary>
      /// <param name="key"></param>
      /// <returns></returns>
      internal String[] GetCurrentTaskDNEventsNames(int key)
      {
         lock (_objectsLock)
         {
            Debug.Assert(key > 0 && key <= _objects.Count); //the class returns 1-based keys (key 0 indicates no key)
            return _objects[key - 1].CurrentTaskDNEventsNames;
         }
      }

      /// <summary>
      /// remove the DNObjectCollection entry with 'key'
      /// </summary>
      /// <param name="key"></param>
      public void Remove(int key)
      {
         lock (_objectsLock)
         {
            Debug.Assert(key > 0 && key <= _objects.Count); //the class returns 1-based keys (key 0 indicates no key)
            _objects[key - 1] = null;
         }
      }

      /// <summary>
      /// Check that the given DN key has an entry with a value in the objects collection.
      /// </summary>
      /// <param name="key"></param>
      public bool IsDNObjectKeyValid(int key)
      {
         lock (_objectsLock)
         {
            return (key <= _objects.Count && _objects[key - 1] != null);
         }
      }

      /// <summary></summary>
      private class DNObjectsCollectionEntry
      {
         internal Type DNType { get; private set; }
         internal String[] CurrentTaskDNEventsNames { get; private set; }
         internal Object DNObj { get; set; }

         /// <summary>CTOR</summary>
         /// <param name="dnType"></param>
         /// <param name="dnObj"></param>
         public DNObjectsCollectionEntry(Type dnType, Object dnObj)
            : this(dnType, dnObj, null)
         {
         }

         /// <summary>CTOR</summary>
         /// <param name="dnType"></param>
         /// <param name="dnObj"></param>
         /// <param name="currentTaskDNEventsNames">comma-delimited string of events that are handled for the .net variable in the task containing the variable</param>
         public DNObjectsCollectionEntry(Type dnType, Object dnObj, String currentTaskDNEventsNames)
         {
            DNType = dnType;
            CurrentTaskDNEventsNames = (currentTaskDNEventsNames != null && currentTaskDNEventsNames.Length > 0
                                           ? currentTaskDNEventsNames.Split(',')
                                           : null);
            DNObj = dnObj;
         }
      }
   }
}