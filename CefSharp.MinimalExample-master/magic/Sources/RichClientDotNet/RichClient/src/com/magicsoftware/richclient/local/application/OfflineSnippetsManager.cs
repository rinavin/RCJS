using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Collections.Specialized;

using com.magicsoftware.richclient;
using com.magicsoftware.util.Xml;
using com.magicsoftware.richclient.sources;
using com.magicsoftware.richclient.util;
using util.com.magicsoftware.util;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.util;

namespace com.magicsoftware.richclient.local.application
{
   internal class OfflineSnippetsManager
   {
      /// <summary>parsing of the assemblies xml</summary>
      class AssembliesSaxHandler : MgSAXHandlerInterface
      {
         /// <summary>
         /// 
         /// </summary>
         /// <param name="xmlSerializedTaskDefinitionsTable"></param>
         public void parse(byte[] xmlSerializedSnippetAssemblies)
         {
            MgSAXHandler mgSAXHandler = new MgSAXHandler(this);
            mgSAXHandler.parse(xmlSerializedSnippetAssemblies);
         }

         public void endElement(String elementName, String elementValue, NameValueCollection attributes)
         {
            if (elementName.Equals(XMLConstants.MG_TAG_ASSEMBLY))
            {
               String path = attributes[XMLConstants.MG_ATTR_ASSEMBLY_PATH];
               String isGuiThreadExecutionStr = attributes[XMLConstants.MG_ATTR_IS_GUI_THREAD_EXECUTION];
               bool isGuiThreadExecution = isGuiThreadExecutionStr != null && isGuiThreadExecutionStr.Equals("1", StringComparison.CurrentCultureIgnoreCase);

               path = XmlParser.unescape(path);

               // Add assembly to the collection
               ReflectionServices.AddAssembly(path, false, path, isGuiThreadExecution);

               String strEncrypted = "&ENCRYPTED=N";
               path = path.Remove(path.Length - strEncrypted.Length, strEncrypted.Length);
               ApplicationSourcesManager.GetInstance().ReadSource(path, false);
            }
         }
      }

      internal void FillFromUrl(String tagName)
      {
         XmlParser parser = ClientManager.Instance.RuntimeCtx.Parser;
         String XMLdata = parser.getXMLdata();
         int endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_TERM, parser.getCurrIndex());
         if (endContext != -1 && endContext < XMLdata.Length)
         {
            // find last position of its tag
            String tagAndAttributes = parser.getXMLsubstring(endContext);
            parser.add2CurrIndex(tagAndAttributes.IndexOf(tagName) + tagName.Length);

            List<String> tokensVector = XmlParser.getTokens(parser.getXMLsubstring(endContext), XMLConstants.XML_ATTR_DELIM);
            Debug.Assert((tokensVector[0]).Equals(XMLConstants.MG_ATTR_VALUE));
            String offlineSnippetsURL = (tokensVector[1]);

            byte[] content = ApplicationSourcesManager.GetInstance().ReadSource(offlineSnippetsURL, true);

            ClientManager.Instance.LocalManager.ApplicationDefinitions.OfflineSnippetsManager.FillFrom(content);
            endContext = XMLdata.IndexOf(XMLConstants.TAG_OPEN, endContext);
            if (endContext != -1)
               parser.setCurrIndex(endContext);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="fontxml"></param>
      internal void FillFrom(byte[] assemblyUrlxml)
      {
         if (assemblyUrlxml != null)
         {
            AssembliesSaxHandler handler = new AssembliesSaxHandler();
            handler.parse(assemblyUrlxml);
         }
      }
   }
}
