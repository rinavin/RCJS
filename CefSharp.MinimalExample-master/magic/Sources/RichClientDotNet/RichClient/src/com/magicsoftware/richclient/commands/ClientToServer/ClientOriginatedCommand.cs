using System;
using System.Collections.Generic;
using System.Text;
using util.com.magicsoftware.util;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.gui;
using System.Diagnostics;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   /// <summary>
   /// base class for commands created in the client
   /// </summary>
   abstract class ClientOriginatedCommand : IClientCommand
   {
      /// <summary>
      /// attribute of command to be sent to the server
      /// </summary>
      protected abstract String CommandTypeAttribute { get; }
       
      /// <summary>
      /// used to tell which commands are handled locally and should not be serialized
      /// </summary>
      /// <returns></returns>
      protected virtual bool ShouldSerialize
      {
         get { return true; }
      }

      /// <summary>
      /// should the SerializeRecords method be called for this command
      /// </summary>
      protected virtual bool ShouldSerializeRecords { get { return true; } }

      /// <summary>
      /// general serialization for stuff common to all serialized commands
      /// </summary>
      /// <returns></returns>
      internal String Serialize()
      {
         if (!ShouldSerialize)
            return null;

         StringBuilder message = new StringBuilder();
         bool hasChildElements = false;

         message.Append(XMLConstants.START_TAG + ConstInterface.MG_TAG_COMMAND);
         message.Append(" " + XMLConstants.MG_ATTR_TYPE + "=\"" + CommandTypeAttribute + "\"");

         message.Append(SerializeCommandData(ref hasChildElements));

         if(ShouldSerializeRecords)
            message.Append(SerializeRecords());

         if (hasChildElements)
            message.Append(XMLConstants.TAG_CLOSE);
         else
            message.Append(XMLConstants.TAG_TERM);

         message.Append(SerializeDataAfterCommand());
         return message.ToString();
      }

      /// <summary>
      /// virtual method, to allow commands to serialize specific data
      /// </summary>
      /// <param name="hasChildElements"></param>
      /// <returns></returns>
      protected virtual String SerializeCommandData(ref bool hasChildElements) { return null; }

      /// <summary>
      /// should not be called for Query and for IniputForceWrite:
      /// </summary>
      /// <returns></returns>
      private String SerializeRecords()       
      {
         StringBuilder message = new StringBuilder();
         try
         {
            MGData currMGData = MGDataCollection.Instance.getCurrMGData();
            int length = currMGData.getTasksCount();
            bool titleExist = false;
            Task currFocusedTask = ClientManager.Instance.getLastFocusedTask();

            for (int i = 0;
                 i < length;
                 i++)
            {
               Task task = currMGData.getTask(i);
               var ctrl = (MgControl)task.getLastParkedCtrl();
               if (ctrl != null && task.KnownToServer && !task.IsOffline)
               {
                  if (!titleExist)
                  {
                     message.Append(" " + ConstInterface.MG_ATTR_FOCUSLIST + "=\"");
                     titleExist = true;
                  }
                  else
                     message.Append('$');
                  message.Append(task.getTaskTag() + ",");
                  message.Append((task.getLastParkedCtrl()).getDitIdx());
               }
            }

            if (titleExist)
               message.Append("\"");

            if (currFocusedTask != null && !currFocusedTask.IsOffline)
               message.Append(" " + ConstInterface.MG_ATTR_FOCUSTASK + "=\"" + currFocusedTask.getTaskTag() + "\"");
         }
         catch (Exception ex)
         {
            Logger.Instance.WriteExceptionToLog(ex);
         }

         return message.ToString();
      }

      /// <summary>
      /// enable commands to serialize extra data after the command serialization (e.g. execution stack)
      /// </summary>
      /// <returns></returns>
      protected virtual string SerializeDataAfterCommand()
      {
         return null;
      }
   }
}
