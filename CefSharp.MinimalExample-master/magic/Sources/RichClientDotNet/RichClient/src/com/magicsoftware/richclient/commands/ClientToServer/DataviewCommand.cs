using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.local.data;
using System.Diagnostics;
using com.magicsoftware.richclient.tasks;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   /// <summary>
   /// general base class for dataview commands
   /// </summary>
   class DataviewCommand : ClientOriginatedCommand, ICommandTaskTag
   {
      internal DataViewCommandType CommandType { get; set; }
      public String TaskTag { get; set; }

      protected override string CommandTypeAttribute
      {
         get { throw new NotImplementedException(); }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="hasChildElements"></param>
      /// <returns></returns>
      protected override string SerializeCommandData(ref bool hasChildElements)
      {
         Debug.Assert(false, "Dataview commands need not be serialized");
         return null;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      protected override bool ShouldSerialize
      {
         get { return false; }
      }
   }
}
