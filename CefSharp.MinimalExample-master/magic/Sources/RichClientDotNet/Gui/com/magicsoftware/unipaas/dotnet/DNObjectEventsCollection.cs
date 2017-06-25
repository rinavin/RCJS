using System;
using System.Collections.Generic;
using com.magicsoftware.unipaas.util;

namespace com.magicsoftware.unipaas.dotnet
{
   /// <summary>
   ///   This class will contain structures for events management in reflection
   /// </summary>
   public class DNObjectEventsCollection
   {
      /// <summary>
      ///   This hashTable is used for: 
      /// 
      ///   1. Finding object by its hashcode. When event is raised we have hashcode of 
      ///   the object that raised the event, we use this table to find the real object. 
      ///   We can not use the clientManager DNObjectsCollection since the key from 
      ///   DNObjectsCollection which was used to register may not exist any more when event
      ///   is raised. It is possible when original task is already closed.
      /// 
      ///   2. Managing list of events to which the object subscribed. Reflection does not 
      ///   give us a tool to get list of delegates that are subscribed for an event, 
      ///   since event can be implemented by the user. So we manage list of events name 
      ///   and delegates for any event we subscribed on and for every object. 
      ///   This allows preventing double registration for an event. When object is removed 
      ///   from DNObjectsCollection we remove the handler from the object to prevent a memory 
      ///   leak. This is also useful when we want to register to the object again, we will 
      ///   not add the handler second time.
      /// </summary>
      private readonly Dictionary<int, ObjectEvents> _objectEventsCollection = new Dictionary<int, ObjectEvents>();

      /// <summary>
      ///   this class contains information needed for event handling of the object
      /// </summary>
      internal class ObjectEvents
      {
         internal Object Object { get; set; }

         //Delegate for control value changed event.
         internal Delegate DNControlValueChangedDelegate { get; set; }

         //DN control value changed event name.
         internal string DNControlValueChangedEventName { get; set; }

         // list of events to which the object subscribed
         private Dictionary<string, Delegate> _delegates = new Dictionary<string, Delegate>();
         internal Dictionary<string, Delegate> Delegates
         {
            get
            {
               return _delegates;
            }
            set
            {
               _delegates = value;
            }
         }

         internal int VarRefCount { get; set; }

         /// <summary>
         ///   check if delegate exist for an event on the object
         /// </summary>
         /// <param name = "name"></param>
         /// <returns></returns>
         internal bool delegateExist(string name)
         {
            return _delegates.ContainsKey(name);
         }

         /// <summary>
         ///   add a delegete for an event on the object
         /// </summary>
         /// <param name = "name"></param>
         /// <param name = "d"></param>
         internal void add(String name, Delegate d)
         {
            _delegates.Add(name, d);
         }
      }

      /// <summary>
      ///   check if an entry exist in ObjectEventsCollection, if not - create it
      /// </summary>
      /// <param name = "o"></param>
      /// <returns></returns>
      internal ObjectEvents checkAndCreateObjectEvents(Object o)
      {
         ObjectEvents objectEvents = getObjectEvents(o);
         if (objectEvents == null)
         {
            objectEvents = new ObjectEvents();
            objectEvents.Object = o;
            _objectEventsCollection.Add(o.GetHashCode(), objectEvents);
         }

         // increase the var ref count by one.
         objectEvents.VarRefCount++;

         return objectEvents;
      }

      /// <summary>
      ///   remove an entry from ObjectEventsCollection
      /// </summary>
      /// <param name = "o"></param>
      private void Remove(Object o)
      {
         ObjectEvents objectEvents = getObjectEvents(o);

         if (objectEvents != null)
         {
            foreach (var item in objectEvents.Delegates)
            {
               //remove handler
               ReflectionServices.removeHandler(o, item.Key, item.Value);
            }
            objectEvents.Delegates.Clear();

            if (objectEvents.DNControlValueChangedDelegate != null)
               ReflectionServices.removeHandler(o, objectEvents.DNControlValueChangedEventName, objectEvents.DNControlValueChangedDelegate);
         }

         if (_objectEventsCollection.ContainsKey(o.GetHashCode()))
            _objectEventsCollection.Remove(o.GetHashCode());
      }

      /// <summary>
      ///   get object by hashCode
      /// </summary>
      /// <param name = "hashCode"></param>
      /// <returns></returns>
      internal ObjectEvents getObjectEvents(object obj)
      {
         ObjectEvents retEvents;
         _objectEventsCollection.TryGetValue(obj.GetHashCode(), out retEvents);
         return retEvents;
      }

      /// <summary>
      ///   get ObjectEvents for the Obj instance
      /// </summary>
      /// <param name = "obj"></param>
      /// <returns></returns>
      internal Object getObject(int hashCode)
      {
         ObjectEvents objectEvents;
         _objectEventsCollection.TryGetValue(hashCode, out objectEvents);
         return (objectEvents != null
                 ? objectEvents.Object
                 : null);
      }

      /// <summary> adds events to 'obj'</summary>
      /// <param name="obj"></param>
      /// <param name="dnEventsNames">comma-delimited string of events that should be raised for the object</param>
      public void addEvents(object obj, String[] dnEventsNames)
      {
         if (obj != null) 
         {
            // Even if there is no event to this object, add an entry into ObjectsEventsHashTable.
            // So that we know the references to this object, when we do add events to this obj
            ObjectEvents objectEvents = checkAndCreateObjectEvents(obj);

            if (dnEventsNames != null)
               foreach (string item in dnEventsNames)
                  ReflectionServices.addHandler(item, obj, objectEvents, true);
         }
      }

      /// <summary> remove events to 'obj'</summary>
      /// <param name="obj"></param>
      public void removeEvents(object obj)
      {
         if (obj != null)
         {
            // get the objectEvent in the ObjectsEventsHashTable
            ObjectEvents objectEvents = getObjectEvents(obj);

            if (objectEvents != null)
            {
               // decrease ref count by one
               objectEvents.VarRefCount--;

               if (objectEvents.VarRefCount <= 0)
                  Remove(obj);
            }
         }
      }

   }
}
