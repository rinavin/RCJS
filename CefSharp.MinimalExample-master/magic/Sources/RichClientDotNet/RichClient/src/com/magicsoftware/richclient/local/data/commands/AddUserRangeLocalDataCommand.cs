using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.local.data.view.fields;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;
using System.Diagnostics;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// command to add the user range, from RangeAdd, to the local data view manager range collection
   /// </summary>
   class AddUserRangeLocalDataCommand : LocalDataViewCommandBase
   {
      protected int fieldIndex;
      protected UserRange userRange;

      protected virtual bool IsLocate
      {
         get 
         {
            return false;
         }
      }

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="command"></param>
      public AddUserRangeLocalDataCommand(AddUserRangeDataviewCommand command)
         : base(command)
      {
         fieldIndex = (int)command.Range.veeIdx - 1;
         userRange = command.Range;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal override ReturnResultBase Execute()
      {
         IFieldView fieldView = TaskViews.Fields[fieldIndex];

         if (fieldView.IsVirtual || fieldView.IsLink)
            LocalDataviewManager.UserRanges.Add(userRange);
         else
            LocalDataviewManager.UserGatewayRanges.Add(UserRangeToDataRange(userRange, fieldView));

         return new ReturnResult();
      }


      /// <summary>
      /// convert a UserRange object to A RangeData object
      /// </summary>
      /// <param name="userRange"></param>
      /// <param name="field"></param>
      /// <returns></returns>
      internal RangeData UserRangeToDataRange(UserRange userRange, IFieldView fieldView)
      {
         RangeData rangedata = new RangeData();

         rangedata.FieldIndex = TaskViews.ViewMain.GetFieldIndexInViewByIndexInRecord(fieldView.Id);

         InitBoundary(rangedata.Min, fieldView.StorageAttribute, userRange.discardMin, userRange.min, fieldView.Length, Char.MinValue);

         InitBoundary(rangedata.Max, fieldView.StorageAttribute, userRange.discardMax, userRange.max, fieldView.Length, Char.MaxValue);

         return rangedata;
      }

      /// <summary>
      /// init the boundary 
      /// </summary>
      /// <param name="boundaryValue"></param>
      /// <param name="storageAttribute"></param>
      /// <param name="discard"></param>
      /// <param name="value"></param>
      private void InitBoundary(BoundaryValue boundaryValue, StorageAttribute storageAttribute, bool discard, string value, int length, char filler)
      {
         if (discard)
         {
            boundaryValue.Type = RangeType.RangeNoVal;
            boundaryValue.Value.IsNull = true;
         }
         else
         {
            if (value == null)
            {
               boundaryValue.Value.IsNull = true;
            }
            else
            {
               if (storageAttribute == StorageAttribute.ALPHA ||
                  storageAttribute == StorageAttribute.UNICODE)
               {
                  if (IsLocate)
                  {
                     boundaryValue.Value.Value = DisplayConvertor.StringValueToMgValue(value, storageAttribute, filler, length);
                  }
                  else
                  {
                     boundaryValue.Value.Value = value;
                  }

                  boundaryValue.Value.Value = StrUtil.SearchAndReplaceWildChars((string)boundaryValue.Value.Value, length, filler);
               }
               else
               {
                  boundaryValue.Value.Value = DisplayConvertor.StringValueToMgValue(value, storageAttribute, filler, length);
               }
            }

            boundaryValue.Type = RangeType.RangeParam;
         }
      }

     
   }
}
