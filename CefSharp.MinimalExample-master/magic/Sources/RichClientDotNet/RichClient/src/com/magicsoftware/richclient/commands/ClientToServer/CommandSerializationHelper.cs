using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.tasks;
using util.com.magicsoftware.util;
using com.magicsoftware.richclient.util;
using com.magicsoftware.util.Xml;
using com.magicsoftware.util;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   /// <summary>
   /// helper class - supplies methods for serialization of commands data.
   /// </summary>
   class CommandSerializationHelper
   {
      StringBuilder message = new StringBuilder();

      internal string GetString()
      {
         return message.ToString();
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="taskTag"></param>
      /// <message.Append(s></message.Append(s>
      internal void SerializeTaskTag(string taskTag)
      {
         // QCR #980454 - the task id may change during the task's lifetime, so taskId might
         // be holding the old one - find the task and re-fetch its current ID.
         String id = MGDataCollection.Instance.getTaskIdById(taskTag);

         SerializeAttribute(XMLConstants.MG_ATTR_TASKID, id);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="fldId"></param>
      /// <message.Append(s></message.Append(s>
      internal void SerializeFldId(int fldId)
      {
         SerializeAttribute(ConstInterface.MG_ATTR_FIELDID, fldId);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="ditIdx"></param>
      /// <message.Append(s></message.Append(s>
      internal void SerializeDitIdx(int ditIdx)
      {
         if (ditIdx > Int32.MinValue)
            SerializeAttribute(XMLConstants.MG_ATTR_DITIDX, ditIdx);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="collectedOfflineRequiredMetadata"></param>
      /// <param name="hasChildElements"></param>
      /// <param name="isAccessingServerUsingHTTPS">true if web requests from the client are made using the HTTPS protocol.</param>
      /// <message.Append(s></message.Append(s>
      internal void SerializeAccessedCacheFiles(Dictionary<string, string> collectedOfflineRequiredMetadata, ref bool hasChildElements, bool isAccessingServerUsingHTTPS)
      {
         if (isAccessingServerUsingHTTPS)
            message.AppendFormat(" {0}=\"Y\"", ConstInterface.MG_ATTR_IS_ACCESSING_SERVER_USING_HTTPS);

         if (collectedOfflineRequiredMetadata != null && collectedOfflineRequiredMetadata.Count > 0)
         {
            hasChildElements = true;

            message.Append(" " + ConstInterface.MG_ATTR_SHOULD_PRE_PROCESS + "=\"Y\"");
            message.Append(XMLConstants.TAG_CLOSE + "\n");

            foreach (KeyValuePair<string, string> cachedFileInfo in collectedOfflineRequiredMetadata)
            {
               message.Append(XMLConstants.XML_TAB + XMLConstants.XML_TAB + XMLConstants.TAG_OPEN);
               message.Append(ConstInterface.MG_TAG_CACHE_FILE_INFO + " ");
               message.Append(ConstInterface.MG_ATTR_SERVER_PATH + "=\"" + cachedFileInfo.Key + "\" ");
               message.Append(ConstInterface.MG_ATTR_TIMESTAMP + "=\"" + cachedFileInfo.Value + "\"");
               message.Append(XMLConstants.TAG_TERM + "\n");
            }

            message.Append(XMLConstants.XML_TAB + XMLConstants.END_TAG + ConstInterface.MG_TAG_COMMAND);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="refreshMode"></param>
      /// <message.Append(s></message.Append(s>
      internal void SerializeRefreshMode(ViewRefreshMode refreshMode)
      {
         if (refreshMode != ViewRefreshMode.None)
            SerializeAttribute(ConstInterface.MG_ATTR_REALREFRESH, (int)refreshMode);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="magicEvent"></param>
      /// <message.Append(s></message.Append(s>
      internal void SerializeMagicEvent(int magicEvent)
      {
         SerializeAttribute(ConstInterface.MG_ATTR_MAGICEVENT, magicEvent);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="closeSubformOnly"></param>
      /// <message.Append(s></message.Append(s>
      internal void SerializeCloseSubformOnly(bool closeSubformOnly)
      {
         if (closeSubformOnly)
            SerializeAttribute(ConstInterface.MG_ATTR_CLOSE_SUBFORM_ONLY, "1");
      }

      /// 
      /// </summary>
      /// <param name="keyIndex"></param>
      /// <message.Append(s></message.Append(s>
      internal void SerializeKeyIndex(int keyIndex)
      {
         SerializeAttribute(ConstInterface.MG_ATTR_KEY, keyIndex);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="attribute"></param>
      /// <param name="value"></param>
      /// <message.Append(s></message.Append(s>
      internal void SerializeAttribute(string attribute, string value)
      {
         message.Append(" " + attribute + "=\"" + value + "\"");
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="attribute"></param>
      /// <param name="value"></param>
      internal void SerializeAttribute(string attribute, int value)
      {
         message.Append(" " + attribute + "=\"" + value + "\"");
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="attribute"></param>
      /// <param name="value"></param>
      internal void SerializeAttribute(string attribute, char value)
      {
         message.Append(" " + attribute + "=\"" + value + "\"");
      }

   }
}
