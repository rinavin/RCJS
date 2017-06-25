using System;
using System.Collections.Generic;

namespace com.magicsoftware.util
{
   /// <summary> An abstract base class for caching system resources</summary>
   /// <author>  ehudm</author>
   public abstract class ResourcesCache<TKey, TValue>
      where TValue : class
   {
      private readonly Dictionary<TKey, TValue> _resources;

      /// <summary> CTOR</summary>
      protected internal ResourcesCache()
      {
         _resources = new Dictionary<TKey, TValue>();
      }

      /// <summary>
      /// clear all resources.
      /// </summary>
      public void Clear()
      {
         _resources.Clear();
      }

      /// <summary> Returns the resource according to the key. If the resource is not in the cache then first it creates the
      /// resource and then adds it to the cache</summary>
      /// <param name="key">The key of the requested resource</param>
      /// <returns> VALUE</returns>
      public virtual TValue Get(TKey key)
      {
         TValue value = null;

         //ms-help://MS.VSCC.v90/MS.MSDNQTR.v90.en/fxref_mscorlib/html/589059d3-d7f8-e09b-705d-91a461971cc2.htm
         // When a program often has to try keys that turn out not to be in the dictionary, 
         // TryGetValue can be a more efficient way to retrieve values.
         _resources.TryGetValue(key, out value);
         if (value == null)
         {
            value = CreateInstance(key);
            _resources[key] = value;
         }
         return value;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="key"></param>
      /// <param name="value"></param>
      public virtual void Put(TKey key, TValue value)
      {
         _resources[key] = value;
      }

      /// <summary> 
      /// Returns the number of items in the cache
      /// </summary>
      /// <returns></returns>
      protected internal int Count()
      {
         return _resources.Count;
      }

      /// <summary> 
      /// Removes an item form the cache.
      /// </summary>
      /// <param name="key"></param>
      protected internal void Remove(TKey key)
      {
         _resources.Remove(key);
      }

      /// <summary> Creates an instance of the VALUE type according to the key</summary>
      /// <param name="key">the key by which to create the value</param>
      /// <returns> VALUE</returns>
      protected abstract TValue CreateInstance(TKey key);
   }
}