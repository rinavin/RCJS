using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using com.magicsoftware.richclient.util;
using com.magicsoftware.util;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.unipaas.util;

namespace com.magicsoftware.richclient.local.data
{
   /// <summary>
   /// Defines a reference to a data source.
   /// </summary>
   public class DataSourceReference
   {
      #region Properties
      internal int NameExpression { get; private set; }
      internal Access Access { get; private set; }
      internal DataSourceDefinition DataSourceDefinition { get; private set; }
      internal bool IsLocal { get { return DataSourceDefinition != null; } }
      #endregion

      #region Constructors

      public DataSourceReference()
      {

      }

      public DataSourceReference(DataSourceDefinition dataSource, Access access)
      {
         this.DataSourceDefinition = dataSource;
         this.Access = access;
      }

      #endregion

      #region Methods
      /// <summary>
      ///  Sets the attributes of the task table
      /// </summary>
      /// <param name="attributes"></param>
      /// <returns></returns>
      public void SetAttributes(NameValueCollection attributes)
      {
         IEnumerator enumerator = attributes.GetEnumerator();
         while (enumerator.MoveNext())
         {
            String attr = (String)enumerator.Current;
            setAttribute(attr, attributes[attr]);
         }
      }

      /// <summary>
      ///  Sets the value of specific attribute (according the data from parser)
      /// </summary>
      /// <param name="attribute"></param>
      /// <param name="valueStr">value of attribute</param>
      /// <returns></returns>
      protected virtual bool setAttribute(string attribute, string valueStr)
      {
         int num;
         switch (attribute)
         {
            case ConstInterface.MG_ATTR_TASK_TABLE_NAME_EXP:
               IntUtil.TryParse(valueStr, out num);
               NameExpression = num;
               break;
            case ConstInterface.MG_ATTR_TASK_TABLE_ACCESS:
               Access = (Access)valueStr[0];
               break;
            case ConstInterface.MG_ATTR_TASK_TABLE_IDENTIFIER:
               SetDataSourceDefinitionAttribute(valueStr);
               break;
            default:
               return false;
         }
         return true;
      }
      #endregion

      #region Helper Methods

      /// <summary>
      ///  Sets the Data Source Definition
      /// </summary>
      /// <param name="valueStr"></param>
      /// <returns></returns>
      private void SetDataSourceDefinitionAttribute(string valueStr)
      {
         int ctlIdx;
         int tableIsn;
         String[] data = StrUtil.tokenize(valueStr, ",");
         Debug.Assert(data.Length > 1);
         IntUtil.TryParse(data[0], out ctlIdx);
         IntUtil.TryParse(data[1], out tableIsn);
         DataSourceId id = new DataSourceId();
         id.CtlIdx = ctlIdx;
         id.Isn = tableIsn;
         DataSourceDefinition = ClientManager.Instance.LocalManager.ApplicationDefinitions.DataSourceDefinitionManager.GetDataSourceDefinition(id);
      }

      #endregion


   }
}
