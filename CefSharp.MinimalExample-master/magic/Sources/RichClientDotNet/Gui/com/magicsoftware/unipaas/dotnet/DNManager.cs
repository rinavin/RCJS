using System;
using System.Diagnostics;
using com.magicsoftware.unipaas.util;

namespace com.magicsoftware.unipaas.dotnet
{
   public class DNManager
   {
      private readonly DNObjectsCollection _DNobjectsCollection = new DNObjectsCollection(); //active Dotnet objects
      public DNObjectsCollection DNObjectsCollection { get { return _DNobjectsCollection; } }

      private readonly DNObjectEventsCollection _DNobjectEventsCollection = new DNObjectEventsCollection(); //(object-Events) collection
      public DNObjectEventsCollection DNObjectEventsCollection { get { return _DNobjectEventsCollection; } }

      private readonly DNObjectFieldCollection _DNobjectFieldCollection = new DNObjectFieldCollection(); //active Dotnet objects
      public DNObjectFieldCollection DNObjectFieldCollection { get { return _DNobjectFieldCollection; } }

      // singleton
      private static DNManager _instance;

      /// <summary>
      ///   Returns the single instance of the DNManager class.
      ///   In case this instance does not exist it creates that instance.
      /// </summary>
      /// <returns> DNManager instance</returns>
      public static DNManager getInstance()
      {
         if (_instance == null)
         {
            lock (typeof(DNManager))
            {
               if (_instance == null)
                  _instance = new DNManager();
            }
         }
         return _instance;
      }

      #region DNObjectsCollection Interactions

      /// <summary>creates a DNObjectsCollection entry and returns the new key</summary>
      /// <param name="assemblyHashCode"></param>
      /// <param name="typeName"></param>
      /// <param name="currentTaskDNEventsNames">comma-delimited string of events that are handled for the .net variable in the task containing the variable</param>
      /// <returns></returns>
      public int CreateDNObjectsCollectionEntry(int assemblyHashCode, String typeName, String currentTaskDNEventsNames)
      {
         int key = 0;
         Type dnType = null;

         // get the dotnet type         
         if (assemblyHashCode != 0 && typeName != null && typeName.Length != 0)
            dnType = ReflectionServices.GetType(assemblyHashCode, typeName);

         // create an entry into DNObjectsCollection
         key = DNObjectsCollection.CreateEntry(dnType, currentTaskDNEventsNames);

         return key;
      }

      /// <summary>
      /// Duplicates a DNObjectsCollection entry and returns the new key
      /// </summary>
      /// <param name="key"></param>
      /// <returns></returns>
      public int DuplicateDNObjectsCollectionEntry(int sourceKey)
      {
         String[] currentTaskDNEventsNames = DNObjectsCollection.GetCurrentTaskDNEventsNames(sourceKey);
         Object sourceObj = DNObjectsCollection.GetDNObj(sourceKey);
         Type sourceDNType = DNObjectsCollection.GetDNType(sourceKey);

         int key = DNObjectsCollection.CreateEntry(sourceDNType);
         DNObjectsCollection.Update(key, sourceObj);
         DNObjectEventsCollection.addEvents(sourceObj, currentTaskDNEventsNames);

         return key;
      }

      /// <summary>Updates specified entry in DNObjectCollection table</summary>
      /// <param name="destKey">Destination entry key</param>
      /// <param name="srcObj">Source .net object</param>
      /// <returns></returns>
      public void UpdateDNObject(int destKey, Object srcObj)
      {
         // for controls, events are queried from MgGui.dll when the control is created.
         String[] currentTaskDNEventsNames = DNObjectsCollection.GetCurrentTaskDNEventsNames(destKey);
         Object destObj = DNObjectsCollection.GetDNObj(destKey);
         Type destDNType = DNObjectsCollection.GetDNType(destKey);

         // perform a cast into Type 'destDNType'
         if (destDNType != null)
            srcObj = DNConvert.doCast(srcObj, destDNType);

         // check if the object has changed
         bool valsEqual = (srcObj == destObj || srcObj != null && destObj != null && srcObj.Equals(destObj));
         if (!valsEqual)
         {
            // update the object into DNObjectCollection
            DNObjectsCollection.Update(destKey, srcObj);
            DNObjectEventsCollection.removeEvents(destObj);
            DNObjectEventsCollection.addEvents(srcObj, currentTaskDNEventsNames);
         }
      }

      /// <summary>clear (set to null) objects stored in DNObjectsCollection</summary>
      /// <param name="keys">an array of keys to be cleared</param>
      /// <param name="removeEvent">if true, removes event on the objects</param>
      public void ClearDNObjectsCollectionEntries(int[] keys, bool removeEvent)
      {
         foreach (int key in keys)
         {
            if (removeEvent)
               RemoveEvents(key);
            DNObjectsCollection.Update(key, null);
         }
      }

      /// <summary>remove entry identified by idx from DNObjectsCollection</summary>
      /// <param name="key">key to be removed</param>
      /// <param name="removeEvent">if true, remove events for the respective object
      public void RemoveDNObjectsCollectionEntry(int key, bool removeEvents)
      {
         if (removeEvents)
            RemoveEvents(key);
         DNObjectsCollection.Remove(key);
      }

      /// <summary>remove entries from DNObjectsCollection</summary>
      /// <param name="keys">an array of keys to be removed</param>
      /// <param name="removeEvent">if true, remove events for the respective object
      public void RemoveDNObjectsCollectionEntries(int[] keys, bool removeEvents)
      {
         for (int idx = 0; idx < keys.Length; idx++)
            RemoveDNObjectsCollectionEntry(keys[idx], removeEvents);
      }

      /// <summary>remove entries from DNObjectsCollection</summary>
      /// <param name="keys">an array of keys to be removed</param>
      /// <param name="removeEvent">an array of flags, if true, remove events for the respective
      /// object in the keys array</param>
      public void RemoveDNObjectsCollectionEntries(int[] keys, bool[] removeEvents)
      {
         Debug.Assert(keys.Length == removeEvents.Length);

         for (int idx = 0; idx < keys.Length; idx++)
            RemoveDNObjectsCollectionEntry(keys[idx], removeEvents[idx]);
      }

      /// <summary>
      /// remove the events for the object in DnObjectsCollection
      /// </summary>
      /// <param name="dNObjectsCollectionkey"></param>
      private void RemoveEvents(int dNObjectsCollectionkey)
      {
         Object obj = DNObjectsCollection.GetDNObj(dNObjectsCollectionkey);
         if (obj != null)
            DNObjectEventsCollection.removeEvents(obj);
      }

      #endregion

      #region DNMemberInfoTable Interactions

      /// <summary>
      /// creates a DNMemberInfo for object at key 'obj', add into DNMemberInfoTable and returns key
      /// </summary>
      /// <param name="key">key to DNObjectsCollection</param>
      /// <returns></returns>
      public DNMemberInfo CreateDNMemberInfo(int key)
      {
         Type dnObjType = null;

         // get the dotnet object
         object dnObj = DNObjectsCollection.GetDNObj(key);

         // get the type
         if (dnObj != null)
            dnObjType = ReflectionServices.GetType(dnObj);

         // create a DNMemberInfo
         var dnMemberInfo = new DNMemberInfo(dnObjType, dnObj, null, key, null);

         return dnMemberInfo;
      }

      #endregion

   }
}
