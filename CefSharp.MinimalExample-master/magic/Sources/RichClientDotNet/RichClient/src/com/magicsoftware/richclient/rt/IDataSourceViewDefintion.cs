using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.local.data;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.util;

namespace com.magicsoftware.richclient.rt
{
   /// <summary>
   /// Definition of a view on a data source. This information is used
   /// when creating the actual view.
   /// </summary>
   internal interface IDataSourceViewDefinition
   {
      /// <summary>
      /// The reference to the data source for which the view will be created.
      /// </summary>
      DataSourceReference TaskDataSource { get; }

      /// <summary>
      /// The fields that the view will use.
      /// </summary>
      List<DBField> DbFields { get; }

      /// <summary>
      /// Gets the key that will be used by the view when accessing the data source.
      /// </summary>
      DBKey DbKey { get; }

      /// <summary>
      /// Gets the order of retrieving records from the database.
      /// </summary>
      Order RecordsOrder { get; }

      /// <summary>
      /// Gets a value denoting whether the view will be able to insert new
      /// records to the database.
      /// </summary>
      bool CanInsert { get; }

      /// <summary>
      /// Gets a value denoting whether the view will be able to delete records
      /// from the database.
      /// </summary>
      bool CanDelete { get; }
   }
}
