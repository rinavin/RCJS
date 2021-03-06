using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using Task = com.magicsoftware.richclient.tasks.Task;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;
using com.magicsoftware.richclient.sources;

namespace com.magicsoftware.richclient.env
{
   internal class LanguageData
   {
      private const int MLS_EOF_CHARS_TO_READ = 10;
      private static readonly Object _mutex = new Object();
      private Hashtable _constMessages;
      private String _constMessagesContent;
      private String _constMessagesUrl;
      private String _mlsContent;
      private String _mlsFileUrl;
      private Hashtable _mlsStrings;
      
      /// <summary>
      ///   CTOR
      /// </summary>
      internal LanguageData()
      {
      }

      /// <summary>
      ///   To retrieve the content of the 'language' element <language>.....</language>
      /// </summary>
      internal void fillData()
      {
         XmlParser parser = ClientManager.Instance.RuntimeCtx.Parser;
         while (initInnerObjects(parser, parser.getNextTag()))
         {
         }

         InitConstMessagesTable();
      }

      /// <summary>
      ///   To extract the url of the const messages from the xml from <language><constmessages>***</constmessages></language>
      /// </summary>
      /// <param name = "foundTagName">possible tag name, name of object, which need be allocated</param>
      private bool initInnerObjects(XmlParser parser, String foundTagName)
      {
         if (foundTagName == null)
            return false;

         switch (foundTagName)
         {
            case ConstInterface.MG_TAG_LANGUAGE:
               parser.setCurrIndex(parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex()) + 1);
               break;

            case ConstInterface.MG_TAG_CONSTMESSAGES:
               {
                  int endUrlValue;

                  parser.setCurrIndex(parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex()) + 1);
                  endUrlValue = parser.getXMLdata().IndexOf(XMLConstants.TAG_OPEN, parser.getCurrIndex());
                  _constMessagesUrl = parser.getXMLsubstring(endUrlValue);
                  parser.setCurrIndex(endUrlValue);
                  break;
               }

            case ConstInterface.MG_TAG_CONSTMESSAGESCONTENT:
               {
                  int endUrlValue;

                  parser.setCurrIndex(parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex()) + 1);
                  endUrlValue = parser.getXMLdata().IndexOf(ConstInterface.MG_TAG_CONSTMESSAGESCONTENT, parser.getCurrIndex()) - 2;
                  _constMessagesContent = parser.getXMLsubstring(endUrlValue);
                  parser.setCurrIndex(endUrlValue);
                  break;
               }

            case ConstInterface.MG_TAG_MLS_CONTENT:
               {
                  int endUrlValue;

                  parser.setCurrIndex(parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex()) + 1);
                  endUrlValue = parser.getXMLdata().IndexOf(ConstInterface.MG_TAG_MLS_CONTENT, parser.getCurrIndex()) - 2;
                  _mlsContent = parser.getXMLsubstring(endUrlValue);
                  parser.setCurrIndex(endUrlValue);

                  // A new MLS has arrived, if mlsStrings has values, clean it.
                  if (!String.IsNullOrEmpty(_mlsContent))
                  {
                     // clear the existing mls (if it exists)
                     if (_mlsStrings != null)
                     {
                        _mlsStrings.Clear();
                        _mlsStrings = null;
                     }
                     // we got a new content. refresh the menues.
                     RefreshMenusText();
                  }
                  // If new MLS is an empty string, clean the MLS data
                  if (String.IsNullOrEmpty(_mlsContent) && _mlsContent != null)
                  {
                     _mlsContent = null;
                     if (_mlsStrings != null)
                     {
                        _mlsStrings.Clear();
                        _mlsStrings = null;
                        RefreshMenusText();
                     }
                  }

                  break;
               }

            case ConstInterface.MG_TAG_MLS_FILE_URL:
               {
                  int endUrlValue;

                  parser.setCurrIndex(parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex()) + 1);
                  endUrlValue = parser.getXMLdata().IndexOf(XMLConstants.TAG_OPEN, parser.getCurrIndex());
                  _mlsFileUrl = parser.getXMLsubstring(endUrlValue).Trim();
                  // A new MLS has arrived, if mlsStrings has values, clean it.
                  if (!String.IsNullOrEmpty(_mlsFileUrl))
                  {
                     // clear the existing mls (if it exists)
                     if (_mlsStrings != null)
                     {
                        _mlsStrings.Clear();
                        _mlsStrings = null;
                     }
                     // we got a new file. refresh the menus.
                     RefreshMenusText();
                  } 
                     // If new MLS is an empty string, clean the MLS data
                  else if (String.IsNullOrEmpty(_mlsFileUrl) && _mlsFileUrl != null)
                  {
                     _mlsFileUrl = null;
                     if (_mlsStrings != null)
                     {
                        _mlsStrings.Clear();
                        _mlsStrings = null;
                        RefreshMenusText();
                     }
                  }
                  parser.setCurrIndex(endUrlValue);

                  break;
               }

            case ConstInterface.MG_TAG_CONSTMESSAGES_END:
            case ConstInterface.MG_TAG_MLS_FILE_URL_END:
            case ConstInterface.MG_TAG_CONSTMESSAGESCONTENT_END:
            case ConstInterface.MG_TAG_MLS_CONTENT_END:
               parser.setCurrIndex2EndOfTag();
               break;

            case ConstInterface.MG_TAG_LANGUAGE_END:
               parser.setCurrIndex2EndOfTag();
               return false;

            default:
               Logger.Instance.WriteExceptionToLog("There is no such tag in LanguageData.initInnerObjects(): " + foundTagName);
               return false;
         }
         return true;
      }

      /// <summary> Returns the const msg by msgId. </summary>
      /// <param name="msgId"></param>
      /// <returns></returns>
      internal String getConstMessage(string msgId)
      {
         //Map default message ID to corresponding message string.
         if (_constMessages == null)
         {
            _constMessages = new Hashtable();
            for (int iCtr = 0; iCtr <= MsgInterface.DefaultMessages.Length - 1; iCtr++)
            {
               _constMessages.Add(MsgInterface.DefaultMessages[iCtr].MsgID, MsgInterface.DefaultMessages[iCtr].MsgString);
            }
         }

         return ((String)_constMessages[msgId]);
      }

      /// <summary> Initialize the const msg table from either the URL or the XML contents. </summary>
      private void InitConstMessagesTable()
      {
         try
         {
            String msgsInCommentString = null;
            if (_constMessagesUrl != null || _constMessagesContent != null)
            {
               if (_constMessagesUrl != null)
               {
                  try
                  {
                     // get the content from the server
                     byte[] msgsInComment = CommandsProcessorManager.GetContent(_constMessagesUrl, false);

                     // get the content from the server.
                     // Since the file is 'UTF-8' , we must specify the charset for the byteArray that
                     // returns from the get content. (otherwise hebrew or other utf8 chars will cause an
                     // exception
                     // in the parse).
                     msgsInCommentString = Encoding.UTF8.GetString(msgsInComment, 0, msgsInComment.Length);
                  }
                  catch (Exception)
                  {
                     Logger.Instance.WriteExceptionToLog(String.Format("Unknown message file: \"{0}\"", _constMessagesUrl));
                  }
               }
               else
               {
                  // No cache - we have all the data here
                  msgsInCommentString = _constMessagesContent;
               }

               if (msgsInCommentString != null)
               {
                  // ignore the comment wrapper
                  const String START_TAG = "<ErrorCodes>";
                  const String END_TAG = "</ErrorCodes>";
                  int startData = msgsInCommentString.IndexOf(START_TAG);
                  int endData = msgsInCommentString.IndexOf(END_TAG) + END_TAG.Length;

                  // again, when the String turns to byteArray, we must specify the UTF-8.
                  byte[] retrievedConstMessages =
                     Encoding.UTF8.GetBytes(msgsInCommentString.Substring(startData, (endData) - (startData)));

                  // parse 'constMessages' into 'languageMessages'
                  fillConstMessagesTable(retrievedConstMessages);
               }
            }
            else
            {
               Logger.Instance.WriteDevToLog(
                  "'constMessagesUrl' and 'constMessagesContent' were not initialized.");
            }
         }
         catch (Exception ex)
         {
            Logger.Instance.WriteExceptionToLog(ex);
         }
      }

      /// <summary>
      ///   Read the const msgs from the xml and fill the const msg table.
      /// </summary>
      /// <param name = "constMessages"></param>
      private void fillConstMessagesTable(byte[] constMessages)
      {
         try
         {
            if (constMessages != null)
            {
               _constMessages = new Hashtable();
               new ConstMsgSaxHandler(this, constMessages);
            }
         }
         catch (Exception ex)
         {
            Logger.Instance.WriteExceptionToLog(ex);
         }
      }

      /// <summary>
      ///   Return the translation on the fromString. If the mlsStrings_ table is empty, read it from the file a and fill it.
      /// </summary>
      /// <param name = "fromString"></param>
      internal String translate(String fromString)
      {
         String toString = "";

         // no mls file exists.
         if (_mlsFileUrl == null && _mlsContent == null)
            return fromString;

         try
         {
            if (_mlsStrings == null)
            {
               lock (_mutex)
               {
                  // double check lock pattern
                  if (_mlsStrings == null)
                  {
                     fillMlsMessagesTable();
                  }
               }
            }
            // get the msg from the table
            if (_mlsStrings != null)
               toString = ((String) _mlsStrings[fromString]);

            // when not found, returns the fromString.
            if (toString == null)
               toString = fromString;
         }
         catch (Exception e)
         {
            Misc.WriteStackTrace(e, Console.Error);
         }
         return toString;
      }

      /// <summary>
      ///   Fill the mlsStrings_ table. Get the file, read the number of translation from its location at the last
      ///   line of the file. Then loop on the file from the beginning and fill the table.
      /// </summary>
      internal void fillMlsMessagesTable()
      {
         try
         {
            if (_mlsContent != null)
            {
               // No cache - we have all the data here
               if (_mlsContent.Length > 0)
               {
                  long contentLen = _mlsContent.Length;
                  int linePairs = 0; // How many paired lines are in the file.
                  String srcLine = _mlsContent + 3; //skip BOM

                  _mlsStrings = new Hashtable();

                  // read the last line
                  String linesStr = _mlsContent.Substring(MLS_EOF_CHARS_TO_READ); //br.ReadLine();
                  // the number of pairs is in hex, so convert it to int.
                  linePairs = Convert.ToInt32(linesStr, 16);
                  String[] tokens = StrUtil.tokenize(srcLine, "\n");

                  for (int line = 0; line < linePairs*2; line += 2)
                     _mlsStrings[tokens[line]] = tokens[line + 1];
               }
            }
            else
            {
               // get the content from the server
               byte[] content = CommandsProcessorManager.GetContent(_mlsFileUrl, false);
               Stream stream = new MemoryStream(content);

               if (stream != null && stream.Length > 0)
               {
                  //QCR # 930249: Passing the encoding scheme as unicode.This required for the proper reading and parsing of the contents
                  //of the MLS file.
                  StreamReader br = new StreamReader(stream, Encoding.Unicode);
                  long contentLen = stream.Length;
                  int linePairs = 0; // How many paired lines are in the file.
                  String srcLine;
                  String transLine;

                  _mlsStrings = new Hashtable();

                  // jump to the last line of the file to read the number of pairs
                  br.BaseStream.Seek(contentLen - (MLS_EOF_CHARS_TO_READ*2), SeekOrigin.Begin);

                  // read the last line
                  String linesStr = br.ReadLine();
                  // the number of pairs is in hex, so convert it to int.
                  linePairs = Convert.ToInt32(linesStr, 16);
                  // now from the begining (skipping the UTF BOM bytes), loop on the pairs and fill the hash map
                  br.BaseStream.Seek(2, SeekOrigin.Begin);

                  for (int pairNum = 0; pairNum < linePairs; pairNum++)
                  {
                     srcLine = br.ReadLine();
                     transLine = br.ReadLine();
                     _mlsStrings[srcLine] = transLine;
                  }
               }
            }
         }
         catch (Exception e)
         {
            Misc.WriteStackTrace(e, Console.Error);
         }
      }

      /// <summary>
      ///   The menu texts needs to be refreshed due to a change in the language.
      /// </summary>
      private void RefreshMenusText()
      {
         int ctlIdx = 0;
         Task mainProg = (Task) MGDataCollection.Instance.GetMainProgByCtlIdx(ctlIdx);

         while (mainProg != null)
         {
            ApplicationMenus menus = Manager.MenuManager.getApplicationMenus(mainProg);
            // call refreshMenuesTextMls for each application menu of components, starting with the main.
            if (menus != null)
               menus.refreshMenuesTextMls();

            ctlIdx = mainProg.getCtlIdx();
            mainProg = (Task)mainProg.getMGData().getNextMainProg(ctlIdx);
         }
      }

      #region Nested type: ConstMsgSaxHandler

      /// <summary>
      ///   This class responsible to the sax parsing (handler) of the const msg information from the xml buffer.
      /// </summary>
      /// <author>  eran
      /// </author>
      internal class ConstMsgSaxHandler : MgSAXHandlerInterface
      {
         private readonly LanguageData _enclosingInstance;

         internal ConstMsgSaxHandler(LanguageData enclosingInstance, byte[] constMessages)
         {
            _enclosingInstance = enclosingInstance;
            MgSAXHandler mgSAXHandler = new MgSAXHandler(this);
            mgSAXHandler.parse(constMessages);
         }

         internal LanguageData Enclosing_Instance
         {
            get { return _enclosingInstance; }
         }

         /*
         * (non-Javadoc)
         * @see MgSAXHandlerInterface#endElement(java.lang.String, java.lang.String, java.lang.String)
         */

         #region MgSAXHandlerInterface Members

         public void endElement(String elementName, String elementValue, NameValueCollection attributes)
         {
            if (elementName.Equals("ErrorMessage"))
               Enclosing_Instance._constMessages[attributes["id"]] = elementValue;
         }

         #endregion
      }

      #endregion
   }
}
