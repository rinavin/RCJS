using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util.Xml;
using util.com.magicsoftware.util;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.util;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.commands.ClientToServer;
using System.Diagnostics;

namespace com.magicsoftware.richclient.commands.ServerToClient
{
   class XMLBasedCommandBuilder
   {
      String newTaskXML;

      /// <summary>
      ///   Need part input String to relevant for the class data
      /// </summary>
      internal IClientCommand fillData()
      {
         XmlParser parser = ClientManager.Instance.RuntimeCtx.Parser;

         int endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_TERM, parser.getCurrIndex());

         if (endContext != -1 && endContext < parser.getXMLdata().Length)
         {
            // last position of its tag
            String tag = parser.getXMLsubstring(endContext);
            parser.add2CurrIndex(tag.IndexOf(ConstInterface.MG_TAG_COMMAND) + ConstInterface.MG_TAG_COMMAND.Length);

            List<String> tokensVector = XmlParser.getTokens(parser.getXMLsubstring(endContext),
                                                            XMLConstants.XML_ATTR_DELIM);
            IClientTargetedCommand command = initElements(tokensVector);
            parser.setCurrIndex(endContext + XMLConstants.TAG_TERM.Length); // to delete ">" too

            var openUrlCommand = command as OpenURLCommand;
            if (openUrlCommand != null)
            {
               getTaskXML(parser);
               openUrlCommand.NewTaskXML = newTaskXML;
            }
            
            return command;
         }
         Logger.Instance.WriteDevToLog("in Command.FillData() out of string bounds");
         return null;
      }

      /// <summary>
      ///   parse elements
      /// </summary>
      /// <param name = "tokensVector">the tokens vector</param>
      private IClientTargetedCommand initElements(List<String> tokensVector)
      {
         Debug.Assert(tokensVector[0] == XMLConstants.MG_ATTR_TYPE, "The first attribute of a <command> element must be the command type.");

         var commandType = getType(tokensVector[1].ToLower());
         var command = CreateCommand(commandType);

         for (int j = 2;
              j < tokensVector.Count;
              j += 2)
         {
            string attribute = tokensVector[j];
            string valueStr = tokensVector[j + 1];

            command.HandleAttribute(attribute, valueStr);
         }
         return command;
      }

      private IClientTargetedCommand CreateCommand(ClientTargetedCommandType commandType)
      {
         switch (commandType)
         {
            case ClientTargetedCommandType.Abort:
               return new AbortCommand();

            case ClientTargetedCommandType.Verify:
               return new VerifyCommand();

            case ClientTargetedCommandType.EnhancedVerify:
               return new EnhancedVerifyCommand();

            case ClientTargetedCommandType.OpenURL:
               return new OpenURLCommand();

            case ClientTargetedCommandType.Result:
               return new ResultCommand();

            case ClientTargetedCommandType.AddRange:
               return new AddRangeCommand();

            case ClientTargetedCommandType.AddLocate:
               return new AddLocateCommand();

            case ClientTargetedCommandType.AddSort:
               return new AddSortCommand();

            case ClientTargetedCommandType.ResetRange:
               return new ResetRangeCommand();

            case ClientTargetedCommandType.ResetLocate:
               return new ResetLocateCommand();

            case ClientTargetedCommandType.ResetSort:
               return new ResetSortCommand();

            case ClientTargetedCommandType.ClientRefresh:
               return new ClientRefreshCommand();

            default:
               throw new InvalidOperationException("Unknown client targeted command type " + commandType);
         }
      }

      /// <summary>
      ///   Get XML for if new task
      /// </summary>
      /// <param name = "foundTagName">XML tag</param>
      private void getTaskXML(XmlParser parser)
      {
         if (!parser.getNextTag().Equals(ConstInterface.MG_TAG_TASK_XML))
            throw new ApplicationException("Task's XML is missing");

         newTaskXML = parser.ReadContentOfCurrentElement();
      }

      /// <summary>
      ///   get type of command by value of 'type' attribute
      ///   ONLY Server to Client commands
      /// </summary>
      private ClientTargetedCommandType getType(String valueAttr)
      {
         ClientTargetedCommandType type;

         switch (valueAttr)
         {
            case ConstInterface.MG_ATTR_VAL_ABORT:
               type = ClientTargetedCommandType.Abort;
               break;
            case ConstInterface.MG_ATTR_VAL_VERIFY:
               type = ClientTargetedCommandType.Verify;
               break;
            case ConstInterface.MG_ATTR_VAL_ENHANCED_VERIFY:
               type = ClientTargetedCommandType.EnhancedVerify;
               break;
            case ConstInterface.MG_ATTR_VAL_RESULT:
               type = ClientTargetedCommandType.Result;
               break;
            case ConstInterface.MG_ATTR_VAL_OPENURL:
               type = ClientTargetedCommandType.OpenURL;
               break;
            case ConstInterface.MG_ATTR_VAL_ADD_RANGE:
               type = ClientTargetedCommandType.AddRange;
               break;
            case ConstInterface.MG_ATTR_VAL_ADD_LOCATE:
               type = ClientTargetedCommandType.AddLocate;
               break;
            case ConstInterface.MG_ATTR_VAL_ADD_SORT:
               type = ClientTargetedCommandType.AddSort;
               break;
            case ConstInterface.MG_ATTR_VAL_RESET_RANGE:
               type = ClientTargetedCommandType.ResetRange;
               break;
            case ConstInterface.MG_ATTR_VAL_RESET_LOCATE:
               type = ClientTargetedCommandType.ResetLocate;
               break;
            case ConstInterface.MG_ATTR_VAL_RESET_SORT:
               type = ClientTargetedCommandType.ResetSort;
               break;
            case ConstInterface.MG_ATTR_VAL_CLIENT_REFRESH:
               type = ClientTargetedCommandType.ClientRefresh;
               break;

            default:
               type = (ClientTargetedCommandType)valueAttr[0];
               Logger.Instance.WriteExceptionToLog(
                  "Command.getType there is no such SERVER to CLIENT command : " + valueAttr);
               break;
         }

         return type;
      }
   }
}
