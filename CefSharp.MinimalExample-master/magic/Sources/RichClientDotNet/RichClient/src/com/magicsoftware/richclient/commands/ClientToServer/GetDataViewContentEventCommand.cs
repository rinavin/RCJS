using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;
using com.magicsoftware.richclient.util;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   /// <summary>
   /// 
   /// </summary>
   class GetDataViewContentEventCommand : EventCommand
   {
      internal string Generation { get; set; }
      internal string TaskVarList { get; set; }
      internal DataViewOutputType OutputType { get; set; }
      internal int DestinationDataSourceNumber { get; set; }
      internal string ListOfIndexesOfSelectedDestinationFields { get; set; }

      /// <summary>
      /// CTOR
      /// </summary>
      public GetDataViewContentEventCommand() : base(InternalInterface.MG_ACT_GET_DATAVIEW_CONTENT)
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
         helper.SerializeAttribute(ConstInterface.MG_ATTR_GENERATION, Generation);
         helper.SerializeAttribute(ConstInterface.MG_ATTR_TASKVARLIST, TaskVarList);
         helper.SerializeAttribute(ConstInterface.MG_ATTR_OUTPUTTYPE, (char)OutputType);
         helper.SerializeAttribute(ConstInterface.MG_ATTR_DESTINATION_DATASOURCE, DestinationDataSourceNumber);
         helper.SerializeAttribute(ConstInterface.MG_ATTR_LIST_OF_INDEXES_OF_SELECTED_DESTINATION_FIELDS, ListOfIndexesOfSelectedDestinationFields);

         return helper.GetString();
      }
   }
}
