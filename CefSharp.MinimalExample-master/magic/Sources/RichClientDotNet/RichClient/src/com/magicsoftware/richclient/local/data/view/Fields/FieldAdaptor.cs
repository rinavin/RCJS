using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.data;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.gui;

namespace com.magicsoftware.richclient.local.data.view.fields
{
   /// <summary>
   ///  Field adaptor will adapt field class for use in runtime views
   /// </summary>
   internal class FieldAdaptor : IFieldView
   {
      /// <summary>
      /// RC field
      /// </summary>
      internal Field Field { get; set; }


      public bool IsVirtual
      {
         get { return Field.IsVirtual; }
      }

      public bool IsLink
      {
         get { return Field.IsLinkField; }
      }

      public int DataviewHeaderId
      {
         get { return Field.getDataviewHeaderId(); }
      }

      public int Id
      {
         get { return Field.getId(); }
      }

      public Boundary Range
      {
         get { return Field.Range; }
      }

      public Boundary Locate
      {
         get { return Field.Locate; }
      }

      public StorageAttribute StorageAttribute
      {
         get { return Field.getType(); }
      }

      public string Picture
      {
         get { return Field.getPicture(); }
      }

      /// <summary>
      /// computte field
      /// </summary>
      public void Compute(bool recompute)
      {
         Field.compute(recompute);
      }

      /// <summary>
      /// returns fields value from current record
      /// </summary>
      public string ValueFromCurrentRecord
      {
         get { return Field.getValue(false); }
      }


       /// <summary>
       /// Is field null in current record
       /// </summary>
       public bool IsNullFromCurrentRecord
      {
          get { return Field.isNull(); }
      }
      /// <summary>
      /// true if we should check range for this field during compute
      /// </summary>
      public bool ShouldCheckRangeInCompute
      {
         get { return Field.ShouldCheckRangeInCompute; }
      }


      /// <summary>
      /// returns true if the field is real or "virtual as real"
      /// </summary>
      public bool AsReal
      {
         get { return Field.VirAsReal || !IsVirtual; }
      }

      /// <summary>
      /// True if field belongs to event handler
      /// </summary>
      public bool IsEventHandlerField
      {
         get { return Field.IsEventHandlerField; }
      }

      /// <summary>
      /// return the length of the field string
      /// </summary>
      public int Length
      {
         get 
         {
            return Field.getSize();
         }
      }

      /// <summary>
      /// return the index of the field in the table
      /// </summary>
      public int IndexInTable { get { return Field.IndexInTable; } }


      public void TakeValFromRec()
      {
         Field.takeValFromRec();
      }


      public bool HasInitExpression
      {
         get { return Field.hasInitExp(); }
      }
   }
}
