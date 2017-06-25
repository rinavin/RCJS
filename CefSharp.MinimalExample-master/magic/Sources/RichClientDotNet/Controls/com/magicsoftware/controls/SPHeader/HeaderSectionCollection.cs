using System;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;

#if PocketPC
using RightToLeft = com.magicsoftware.mobilestubs.RightToLeft;
using LeftRightAlignment = com.magicsoftware.mobilestubs.LeftRightAlignment;
using HandleRef = com.magicsoftware.mobilestubs.HandleRef;
using ArgumentOutOfRangeException = com.magicsoftware.mobilestubs.MgArgumentOutOfRangeException;
using ContentAlignment = OpenNETCF.Drawing.ContentAlignment2;
using ColorTranslator = OpenNETCF.Drawing.ColorTranslator;
using Control = OpenNETCF.Windows.Forms.Control2;
using CreateParams = OpenNETCF.Windows.Forms.CreateParams;
using Message = Microsoft.WindowsCE.Forms.Message;
#endif

namespace com.magicsoftware.controls
{
   /// <summary>
   /// HeaderSectionCollection class.
   /// </summary>

   //  [Serializable]
   public class HeaderSectionCollection : IList
   {
      /// <summary>
      /// Data fields
      /// </summary>
      private Header owner = null;
      public Header Header
      {
         get { return this.owner; }
      }

      private List<HeaderSection> alSectionsByOrder = null;
      private List<HeaderSection> alSectionsByRawIndex = null;

      public int Count
      {
         get { return this.alSectionsByOrder.Count; }
      }

      public HeaderSection this[int index]
      {
         get { return (HeaderSection)this.alSectionsByOrder[index]; }
         set
         {
            if (index < 0 || index >= this.alSectionsByOrder.Count)
               throw new ArgumentOutOfRangeException("index", index,
                                            ErrMsg.IndexOutOfRange());

            _SetSection(index, (HeaderSection)value);
         }
      }

      /// <summary>
      /// Construction
      /// </summary>
      internal HeaderSectionCollection(Header owner)
      {
         this.owner = owner;
         this.alSectionsByOrder = new List<HeaderSection>();
         this.alSectionsByRawIndex = new List<HeaderSection>();
      }

      /// <summary>
      /// Helpers
      /// </summary>
      private void BindSection(HeaderSection item)
      {
         if (item == null)
         {
            throw new ArgumentNullException("item", ErrMsg.NullVal());
         }

         if (item.Collection != null)
         {
            throw new ArgumentException(ErrMsg.SectionIsAlreadyAttached(item.Text), "item");
         }

         item.Collection = this;
      }

      private void UnbindSection(HeaderSection item)
      {
         if (item == null)
         {
            throw new ArgumentNullException("item", ErrMsg.NullVal());
         }

         if (item.Collection != this)
         {
            throw new ArgumentException(ErrMsg.SectionDoesNotExist(item.Text), "item");
         }

         item.Collection = null;
      }

      /// <summary>
      /// Operations
      /// </summary>
      internal int _FindSectionRawIndex(HeaderSection item)
      {
         return this.alSectionsByRawIndex.IndexOf(item);
      }

      internal HeaderSection _GetSectionByRawIndex(int iSection)
      {
         return (HeaderSection)this.alSectionsByRawIndex[iSection];
      }

      internal void _Move(int iFrom, int iTo)
      {
         Debug.Assert(iFrom >= 0 || iFrom < this.alSectionsByOrder.Count);
         Debug.Assert(iTo >= 0 || iTo < this.alSectionsByOrder.Count);
         Debug.Assert(this.alSectionsByOrder.Count == this.alSectionsByRawIndex.Count);

         HeaderSection item = (HeaderSection)this.alSectionsByOrder[iFrom];
         this.alSectionsByOrder.RemoveAt(iFrom);
         this.alSectionsByOrder.Insert(iTo, item);
      }

      internal void _SetSection(int index, HeaderSection item)
      {
         Debug.Assert(index >= 0 || index < this.alSectionsByOrder.Count);
         Debug.Assert(this.alSectionsByOrder.Count == this.alSectionsByRawIndex.Count);

         // Bind item to the collection
         BindSection(item);

         HeaderSection itemOld = (HeaderSection)this.alSectionsByOrder[index];
         int iSection = this.alSectionsByRawIndex.IndexOf(itemOld);

         try
         {
            this.alSectionsByOrder[index] = item;
            this.alSectionsByRawIndex[iSection] = item;

            UnbindSection(itemOld);

            // Notify owner
            if (this.owner != null)
               this.owner._OnSectionChanged(iSection, item);
         }
         catch
         {
            if (itemOld.Collection == null)
               BindSection(itemOld);

            this.alSectionsByOrder[index] = itemOld;
            this.alSectionsByRawIndex[iSection] = itemOld;

            UnbindSection(item);
            throw;
         }
      }

      public void Insert(int index, HeaderSection item)
      {
         Debug.Assert(this.alSectionsByOrder.Count == this.alSectionsByRawIndex.Count);

         if (index < 0 || index > this.alSectionsByOrder.Count)
            throw new ArgumentOutOfRangeException("index", index, ErrMsg.IndexOutOfRange());

         // Bind item to the collection
         BindSection(item);

         try
         {
            this.alSectionsByOrder.Insert(index, item);
            this.alSectionsByRawIndex.Insert(index, item);

            try
            {
               // Notify owner
               if (this.owner != null)
                  this.owner._OnSectionInserted(index, item);
            }
            catch
            {
               this.alSectionsByOrder.Remove(item);
               this.alSectionsByRawIndex.Remove(item);
               throw;
            }
         }
         catch
         {
            if (this.alSectionsByOrder.Count > this.alSectionsByRawIndex.Count)
               this.alSectionsByOrder.RemoveAt(index);

            UnbindSection(item);
            throw;
         }
      }

      public int Add(HeaderSection item)
      {
         int index = this.alSectionsByOrder.Count;

         Insert(index, item);

         return index;
      }

      public void RemoveAt(int index)
      {
         Debug.Assert(this.alSectionsByOrder.Count == this.alSectionsByRawIndex.Count);

         if (index < 0 || index >= this.alSectionsByOrder.Count)
            throw new ArgumentOutOfRangeException("index", index, ErrMsg.IndexOutOfRange());

         HeaderSection item = (HeaderSection)this.alSectionsByOrder[index];
         int iSectionRemoved = this.alSectionsByRawIndex.IndexOf(item);
         Debug.Assert(iSectionRemoved >= 0);

         UnbindSection(item);

         this.alSectionsByOrder.RemoveAt(index);
         this.alSectionsByRawIndex.RemoveAt(iSectionRemoved);

         if (this.owner != null)
            this.owner._OnSectionRemoved(iSectionRemoved, item);
      }

      public void Remove(HeaderSection item)
      {
         int index = this.alSectionsByOrder.IndexOf(item);

         if (index != -1)
            RemoveAt(index);
      }

      public void Move(int iFrom, int iTo)
      {
         if (iFrom < 0 || iFrom >= this.alSectionsByOrder.Count)
            throw new ArgumentOutOfRangeException("iFrom", iFrom, ErrMsg.IndexOutOfRange());

         if (iTo < 0 || iTo >= this.alSectionsByOrder.Count)
            throw new ArgumentOutOfRangeException("iTo", iTo, ErrMsg.IndexOutOfRange());

         _Move(iFrom, iTo);
      }

      public void Clear()
      {
         Debug.Assert(this.alSectionsByOrder.Count == this.alSectionsByRawIndex.Count);

         foreach (HeaderSection item in this.alSectionsByOrder)
         {
            UnbindSection(item);
         }

         this.alSectionsByOrder.Clear();
         this.alSectionsByRawIndex.Clear();

         if (this.owner != null)
            this.owner._OnAllSectionsRemoved();
      }

      internal void Clear(bool bDisposeItems)
      {
         Debug.Assert(this.alSectionsByOrder.Count == this.alSectionsByRawIndex.Count);

         foreach (HeaderSection item in this.alSectionsByOrder)
         {
            UnbindSection(item);

            if (bDisposeItems)
               item.Dispose();
         }

         this.alSectionsByOrder.Clear();
         this.alSectionsByRawIndex.Clear();

         if (this.owner != null)
            this.owner._OnAllSectionsRemoved();
      }


      public int IndexOf(HeaderSection item)
      {
         return this.alSectionsByOrder.IndexOf(item);
      }

      public bool Contains(HeaderSection item)
      {
         return this.alSectionsByOrder.IndexOf(item) != -1;
      }

      public void CopyTo(Array aDest, int index)
      {
         // Need to implement it for List<headerSection>
         //this.alSectionsByOrder.CopyTo(aDest, index);
      }

      /// <summary>
      /// Implementation: IEnumerable
      /// </summary>
      IEnumerator IEnumerable.GetEnumerator()
      {
         return this.alSectionsByOrder.GetEnumerator();
      }

      /// <summary>
      /// Implementation: ICollection
      /// </summary>
      bool ICollection.IsSynchronized
      {
         get { return true; }
      }

      object ICollection.SyncRoot
      {
         get { return this; }
      }


      /// <summary>
      /// Implementation: IList
      /// </summary>
      bool IList.IsFixedSize
      {
         get { return false; }
      }

      bool IList.IsReadOnly
      {
         get { return false; }
      }

      object IList.this[int index]
      {
         get { return this.alSectionsByOrder[index]; }
         set
         {
            _SetSection(index, (HeaderSection)value);
         }
      }

      void IList.Insert(int index, object value)
      {
         Insert(index, (HeaderSection)value);
      }

      int IList.Add(object value)
      {
         return Add((HeaderSection)value);
      }

      void IList.Remove(object value)
      {
         Remove((HeaderSection)value);
      }

      bool IList.Contains(object value)
      {
         return this.alSectionsByOrder.Contains((HeaderSection)value);
      }

      int IList.IndexOf(object value)
      {
         return this.alSectionsByOrder.IndexOf((HeaderSection)value);
      }

      internal void sort(int[] order)
      {
         List<HeaderSection> tempSectionsByOrder = new List<HeaderSection>(alSectionsByOrder.Count);

         for (int i = 0; i < order.Length; i++)
            tempSectionsByOrder.Insert(i, alSectionsByOrder[order[i]]);

         alSectionsByOrder.Clear();
         alSectionsByOrder = tempSectionsByOrder;
      }
   } // HeaderSectionCollection class
}