using System;
using System.Collections;
using System.Collections.Specialized;
using com.magicsoftware.richclient.util;
using com.magicsoftware.util;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.richclient.cache;

namespace com.magicsoftware.richclient.local.application.Databases
{
   /// <summary>
   /// Sax parser for database definitions buffer
   /// </summary>
   class DatabaseDefinitionsSaxParser : MgSAXHandlerInterface
   {
      DatabaseDefinitionsManager databaseDefinitionsManager;

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="xmlData"></param>
      /// <param name="ddm"></param>
      public DatabaseDefinitionsSaxParser(byte[] xmlData, DatabaseDefinitionsManager databaseDefinitionsManager)
      {
         this.databaseDefinitionsManager = databaseDefinitionsManager;
         MgSAXHandler mgSAXHandler = new MgSAXHandler(this);
         mgSAXHandler.parse(xmlData);

      }

      #region MgSAXHandlerInterface
      public void endElement(string elementName, string elementValue, NameValueCollection attributes)
      {
         if (elementName.Equals(ConstInterface.MG_TAG_DATABASE_INFO))
         {
            DatabaseDefinition databaseDefinition = new DatabaseDefinition();
            setAttributes(databaseDefinition, attributes);

            databaseDefinitionsManager.Add(databaseDefinition.Name.ToUpper(), databaseDefinition);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="attributes"></param>
      private void setAttributes(DatabaseDefinition databaseDefinition, NameValueCollection attributes)
      {
         IEnumerator enumerator = attributes.GetEnumerator();
         while (enumerator.MoveNext())
         {
            String attr = (String)enumerator.Current;
            setAttribute(databaseDefinition, attr, attributes[attr]);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="attribute"></param>
      /// <param name="valueStr"></param>
      /// <returns></returns>
      protected virtual bool setAttribute(DatabaseDefinition databaseDefinition, string attribute, string valueStr)
      {
         int tmp;
         switch (attribute)
         {
            case ConstInterface.MG_ATTR_DATABASE_NAME:
               databaseDefinition.Name = valueStr;
               break;
            case ConstInterface.MG_ATTR_DATABASE_LOCATION:
               {
                  string localDatabaseLocation = String.Empty;
                  databaseDefinition.Location = valueStr;
               }
               break;
            case ConstInterface.MG_ATTR_DATABASE_TYPE:
               IntUtil.TryParse(valueStr, out tmp);
               databaseDefinition.DatabaseType = tmp;
               break;
            case ConstInterface.MG_ATTR_DATABASE_USER_PASSWORD:
               {
                   databaseDefinition.UserPassword = valueStr;
               }
               break;
            default:
               return false;
         }
         return true;
      }

      #endregion
   }
}
