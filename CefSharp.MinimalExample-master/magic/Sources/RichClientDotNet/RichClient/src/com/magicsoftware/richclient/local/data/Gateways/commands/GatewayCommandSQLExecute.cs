using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.util;

namespace com.magicsoftware.richclient.local.data.gateways.commands
{
   class GatewayCommandSQLExecute : GatewayCommandBase
   {
      public string sqlStatement;
      public StorageAttribute[] storageAttributes;
      public object[] statementReturnedValues;
      public DBField[] dbFields;

      /// <summary>
      /// Execute SQL command.
      /// </summary>
      /// <returns></returns>
      internal override GatewayResult Execute()
      {
         GatewayResult result = new GatewayResult();

         try
         {
            if (DbDefinition != null)
            {
               DatabaseDefinition dbDefinition = (DatabaseDefinition)DbDefinition.Clone();
               UpdateDataBaseLocation(dbDefinition);
               result.ErrorCode = GatewayAdapter.Gateway.SQLExecute(dbDefinition, sqlStatement, storageAttributes, out statementReturnedValues, ref dbFields);

               for (int i = 0; i < statementReturnedValues.Length; i++)
               {
                  if (statementReturnedValues[i] != null)
                  {                      
                     statementReturnedValues[i] = GatewayAdapter.StorageConvertor.ConvertGatewayToRuntimeField(dbFields[i], statementReturnedValues[i]);
                  }
               }
            }
            else
            {
               result.ErrorCode = GatewayErrorCode.DatasourceNotExist;
            }
         }
         catch
         {
            throw new NotImplementedException();
         }

         SetErrorDetails(result);

         return result;
      }

      protected override void SetErrorDetails(GatewayResult result)
      {
         base.SetErrorDetails(result);
      } 
   }
}
