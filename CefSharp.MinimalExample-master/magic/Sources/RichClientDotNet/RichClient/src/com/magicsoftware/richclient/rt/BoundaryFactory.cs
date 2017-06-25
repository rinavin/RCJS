using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.local.data;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.richclient.exp;
using com.magicsoftware.util;
using com.magicsoftware.richclient.data;

namespace com.magicsoftware.richclient.rt
{
   /// <summary>
   /// Class for creating Boundary objects.
   /// </summary>
   internal class BoundaryFactory
   {
      public int MinExpressionId { get; set; }
      public int MaxExpressionId { get; set; }
      public int BoundaryFieldIndex { get; set; }

      internal Boundary CreateBoundary(Task task, DataSourceReference dataSourceReference)
      {
         DBField field = dataSourceReference.DataSourceDefinition.Fields[BoundaryFieldIndex - 1];

         Boundary b = new Boundary(task, MinExpressionId, MaxExpressionId, (StorageAttribute)field.Attr, field.StorageFldSize());
         return b;
      }
   }

}
