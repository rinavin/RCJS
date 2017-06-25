using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;
using com.magicsoftware.richclient.util;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.unipaas.management.data;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   /// <summary>
   /// 
   /// </summary>
   class UpdateDataViewToDataSourceEventCommand : EventCommand
   {
      internal string TaskVarList { get; set; }
      internal string DestColumnList { get; set; }
      internal int    DestDataSource { get; set; }
      internal string DestDataSourceName { get; set; }
      internal string DataViewContent { get; set; }
      internal List<DBField> DestinationDataSourceFieldsList { get; set; }
      internal List<FieldDef> SourceVarList { get; set; }


      /// <summary>
      /// CTOR
      /// </summary>
      public UpdateDataViewToDataSourceEventCommand() : base(InternalInterface.MG_ACT_UPDATE_DATAVIEW_TO_DATASOURCE)
      {
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="hasChildElements"></param>
      /// <returns></returns>
      protected override string SerializeCommandData(ref bool hasChildElements)
      {
         CommandSerializationHelper helper = new CommandSerializationHelper();

         helper.SerializeTaskTag(TaskTag);
         helper.SerializeMagicEvent(MagicEvent);
         helper.SerializeAttribute(ConstInterface.MG_ATTR_TASKVARLIST, TaskVarList);
         helper.SerializeAttribute(ConstInterface.MG_ATTR_DESTINATION_DATASOURCE, DestDataSource);
         if (!string.IsNullOrEmpty(DestDataSourceName))
         {
            helper.SerializeAttribute(ConstInterface.MG_ATTR_DESTINATION_DATASOURCE_NAME, DestDataSourceName);
         }
         helper.SerializeAttribute(ConstInterface.MG_ATTR_DESTINATION_COLUMNLIST, DestColumnList);
         helper.SerializeAttribute(ConstInterface.MG_ATTR_SOURCE_DATAVIEW, DataViewContent);
         
         return helper.GetString();
      }
   }
}
