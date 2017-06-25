using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.tasks;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   /// <summary>
   /// 
   /// </summary>
   class ExecOperCommand : ClientOriginatedCommand, ICommandTaskTag
   {
      public String TaskTag { get; set; }
      internal String HandlerId { get; set; }
      internal int OperIdx { get; set; }
      internal int DitIdx { get; set; }
      internal String Val { get; set; }
      internal Task MprgCreator { get; set; }
      internal Operation Operation { get; set; }
      internal bool CheckOnly { get; set; }

      ExecutionStack ExecutionStack; 

      protected override string CommandTypeAttribute
      {
         get { return ConstInterface.MG_ATTR_VAL_EXEC_OPER; }
      }

      /// <summary>
      /// CTOR
      /// </summary>
      public ExecOperCommand()
      {
         DitIdx = Int32.MinValue;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="hasChildElements"></param>
      /// <returns></returns>
      protected override string SerializeCommandData(ref bool hasChildElements)
      {
         CommandSerializationHelper helper = new CommandSerializationHelper();

         bool execStackExists = ExecutionStack != null && !ExecutionStack.empty();

         helper.SerializeTaskTag(TaskTag);
         
         if (HandlerId != null && !execStackExists) 
            helper.SerializeAttribute(ConstInterface.MG_ATTR_HANDLERID, HandlerId);
            
         if (OperIdx > Int32.MinValue && !execStackExists)
            helper.SerializeAttribute(ConstInterface.MG_ATTR_OPER_IDX, OperIdx);
         
         helper.SerializeDitIdx(DitIdx);
         if (Val != null)
            helper.SerializeAttribute(XMLConstants.MG_ATTR_VALUE, XmlParser.escape(Val));

         if (CheckOnly)
            helper.SerializeAttribute(ConstInterface.MG_ATTR_CHECK_ONLY, "1");

         return helper.GetString();

      }
      
      /// <summary>
      ///   sets the execstack of the current command to be sent to the server
      /// </summary>
      /// <param name = "execStack">- current execution stack of raise event operations from rt </param>
      internal void SetExecutionStack(ExecutionStack execStack)
      {
         ExecutionStack = new ExecutionStack();
         ExecutionStack.push(MGDataCollection.Instance.getTaskIdById(TaskTag), HandlerId, OperIdx);
         ExecutionStack.pushUpSideDown(execStack);
      }

      /// <summary>
      /// extra data - add serialization of the execution stack
      /// </summary>
      /// <returns></returns>
      protected override string SerializeDataAfterCommand()
      {
         bool execStackExists = ExecutionStack != null && !ExecutionStack.empty();

         if (execStackExists)
         {
            StringBuilder message = new StringBuilder();
            ExecutionStack.buildXML(message);
            return message.ToString();
         }
         return null;
      }

   }
}
