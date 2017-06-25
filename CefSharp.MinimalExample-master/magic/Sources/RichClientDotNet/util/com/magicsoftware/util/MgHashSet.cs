using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.util
{
   /// <summary>
   /// TODO : remove this when upgrade to .NET 3.5
   /// Basic HashSet implementation that in missing in .NET 2.0
   /// </summary>
   /// <typeparam name="T"></typeparam>
   public class MgHashSet<T>
   {
      Dictionary<T, bool> dictionary = new Dictionary<T, bool>();

      /// <summary>
      /// return count
      /// </summary>
      public int Count
      {
         get
         {
            return dictionary.Count;
         }
      }

      /// <summary>
      /// Add item
      /// </summary>
      /// <param name="t"></param>
      public void Add(T t)
      {
         dictionary[t] = true;
      }

      /// <summary>
      /// remove item
      /// </summary>
      /// <param name="t"></param>
      public void Remove(T t)
      {
         dictionary.Remove(t);
      }

      /// <summary>
      /// returns true if contains item
      /// </summary>
      /// <param name="t"></param>
      /// <returns></returns>
      public bool Contains(T t)
      {
         return dictionary.ContainsKey(t);
      }

      /// <summary>
      /// clear all items
      /// </summary>
      public void Clear()
      {
         dictionary.Clear();
      }
   }
}
