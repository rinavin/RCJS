using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;
using System.Collections;

namespace com.magicsoftware.unipaas.management.data
{
   public class ObjectReferencesCollection : IDisposable, IEnumerable
   {
      MgArrayList _refs = new MgArrayList();

      public ObjectReferenceBase this[int i]
      {
         get { return (ObjectReferenceBase)_refs[i]; }
      }

      public void Add(ObjectReferenceBase objRef)
      {
         _refs.Add(objRef);
      }

      public void Dispose()
      {
         for (int i = 0; i < _refs.Count; i++)
         {
            ((IDisposable)_refs[i]).Dispose();
         }
      }

      public ObjectReferencesCollection Clone()
      {
         ObjectReferencesCollection dcRefsCopy = new ObjectReferencesCollection();
         for (int i = 0; i < _refs.Count; i++)
         {
            ObjectReferenceBase refCopy = this[i].Clone();
            dcRefsCopy.Add(refCopy);
         }
         return dcRefsCopy;
      }

      public IEnumerator GetEnumerator()
      {
         return _refs.GetEnumerator();
      }
   }
}
