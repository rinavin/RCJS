using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;
using System.Collections.Specialized;
using com.magicsoftware.richclient.util;

namespace com.magicsoftware.richclient.local.data
{
   /// <summary>
   /// XML Sax handler for parsing the Task Tables
   /// </summary>
   class TaskTablesSaxHandler : MgSAXHandlerInterface
   {
      private readonly List<DataSourceReference> dataSourceRefs;
 
      public TaskTablesSaxHandler(List<DataSourceReference> dataSourceRefs)
      {
         this.dataSourceRefs = dataSourceRefs;
      }

      #region MgSAXHandlerInterface
      public void endElement(string elementName, string elementValue, NameValueCollection attributes)
      {
         if (elementName.Equals(ConstInterface.MG_TAG_TASK_TABLE))
         {
            DataSourceReference dataSource = new DataSourceReference();
            dataSource.SetAttributes(attributes);
            dataSourceRefs.Add(dataSource);
         }
      }
      #endregion

   }
}
