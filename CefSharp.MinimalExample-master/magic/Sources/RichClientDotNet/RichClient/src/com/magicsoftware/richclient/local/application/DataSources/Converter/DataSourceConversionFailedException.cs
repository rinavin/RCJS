using System;

namespace com.magicsoftware.richclient.local.application.datasources.converter
{
   /// <summary>
   /// Exception for DataSource Conversion failure.
   /// </summary>
   internal class DataSourceConversionFailedException : ApplicationException
   {
      internal String DataSourceName { get; private set; }

      internal DataSourceConversionFailedException(String dataSourceName, String message)
                     : base(message)
      {
         DataSourceName = dataSourceName;
      }

      internal String GetUserError()
      {
         return (String.Format("Failed to convert data source : {0}", DataSourceName));
      }
   }
}
