using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.util;

namespace com.magicsoftware.richclient.local.data.view.fields
{
   /// <summary>
   /// Interface for fields used in runtime view
   /// </summary>
   internal interface IFieldView
   {
      /// <summary>
      /// is field virtual
      /// </summary>
      bool IsVirtual { get; }

      /// <summary>
      /// is field in a link
      /// </summary>
      bool IsLink { get; }

      /// <summary>
      /// dataview header id
      /// </summary>
      int DataviewHeaderId { get; }

      /// <summary>
      /// field ID
      /// </summary>
      int Id { get; }

      /// <summary>
      /// range
      /// </summary>
      Boundary Range { get; }

      /// <summary>
      /// locate
      /// </summary>
      Boundary Locate{ get; }

      /// <summary>
      /// storage attribue
      /// </summary>
      StorageAttribute StorageAttribute { get; }

      /// <summary>
      /// compute field
      /// </summary>
      void Compute(bool recompute);

      /// <summary>
      /// return value from current record
      /// </summary>
      String ValueFromCurrentRecord { get; }

      /// <summary>
      /// return value from current record
      /// </summary>
      bool IsNullFromCurrentRecord { get; }

      /// <summary>
      /// true, if range condition should be applied to the ield during compute
      /// </summary>
      bool ShouldCheckRangeInCompute { get; }

      bool AsReal { get; }

      /// <summary>
      /// True if field belongs to event handler
      /// </summary>
      bool IsEventHandlerField { get; }

      /// <summary>
      /// length of field data
      /// </summary>
      int Length { get; }

      /// <summary>
      /// return the index of the field in the table
      /// </summary>
      int IndexInTable { get; }

      /// <summary>
      /// return the Picture of the field in the table
      /// </summary>
      string Picture { get; }


      void TakeValFromRec();

      bool HasInitExpression { get; }
   }
}
