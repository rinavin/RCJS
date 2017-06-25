using System;
using System.Collections.Generic;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.richclient.data;
using com.magicsoftware.richclient.local.data;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.util;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;

namespace com.magicsoftware.richclient.rt
{

   /// <summary>
   /// representation of the local link
   /// </summary>
   internal class LocalDataviewHeader : DataviewHeaderBase, IDataviewHeader
   {

      List<DBField> dbFields;
      DBKey dbKey;
      /// <summary>
      /// index of table in the task tables
      /// </summary>
      int TableIndex { get; set; }

      public DataSourceReference TaskDataSource { get { return _task.DataSourceReferences[TableIndex]; } }

      /// <summary>
      /// DataSourceDefinition
      /// </summary>
      DataSourceDefinition DataSourceDefinition
      {
         get
         {
            return TaskDataSource.DataSourceDefinition;
         }
      }

      /// <summary>
      /// get database fields of this link
      /// </summary>
      public List<DBField> DbFields
      {
         get
         {
            if (dbFields == null)
            {
               dbFields = new List<DBField>();
               foreach (Field field in Fields)
               {
                  // filter virtual fields
                  if (!field.IsVirtual)
                  {
                     DBField dbField = DataSourceDefinition.Fields[field.IndexInTable];
                     dbFields.Add(dbField);
                  }
               }
            }

            return dbFields;
         }
      }

      /// <summary>
      /// get key
      /// </summary>
      public DBKey DbKey
      {
         get
         {
            if (SortKey != null)
               return SortKey;
            long keyIndex = _keyIdx;
            if (KeyExpression > 0)
            {
               if (Task.EvaluateExpressionAsLong(KeyExpression, out keyIndex))
                  keyIndex--;
               else
                  keyIndex = _keyIdx;
            }

            // If there's no expression - use the constant key index.
            if (dbKey == null || keyIndex != _keyIdx)
               dbKey = DataSourceDefinition.TryGetKey((int)keyIndex);
            return dbKey;
         }
      }

      /// <summary>
      /// sort key
      /// </summary>
      public DBKey SortKey
      {
         get; internal set; 
      }

      /// <summary>
      /// link direction
      /// </summary>
      public Order RecordsOrder
      {
         get
         {
            return (Order)_dir;
         }
      }

      public bool CanInsert
      {
         get
         {
            return Mode == LnkMode.Create || Mode == LnkMode.Write;
         }
      }

      public bool CanDelete
      {
         get
         {
            return Mode == LnkMode.Create || Mode == LnkMode.Write;
         }
      }

      internal LocalDataviewHeader(Task task, int tableIndex)
         : base(task)
      {
         this.TableIndex = tableIndex;
      }

      protected override void setAttribute(string attribute, string valueStr)
      {
         base.setAttribute(attribute, valueStr);
      }

      /// <summary>
      /// get linked record
      /// </summary>
      /// <param name="curRec"></param>
      /// <returns></returns>
      internal override bool getLinkedRecord(Record curRec)
      {
         IClientCommand dataViewCommand = CommandFactory.CreateRecomputeUnitDataViewCommand(_task.getTaskTag(), RecomputeIdFactory.GetRecomputeId(this), curRec.getId());
         ReturnResult result = _task.DataviewManager.Execute(dataViewCommand);
         return result.Success;
      }
   }
}
