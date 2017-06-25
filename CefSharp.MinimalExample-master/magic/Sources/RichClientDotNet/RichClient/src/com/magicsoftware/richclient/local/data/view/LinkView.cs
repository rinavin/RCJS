using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.local.data.view.RecordCompute;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.unipaas.management.data;

namespace com.magicsoftware.richclient.local.data.view
{
   /// <summary>
   /// View of link
   /// </summary>
   class LinkView : RuntimeRealView
   {
      public IDataviewHeader DataviewHeader { get { return (IDataviewHeader)DataSourceViewDefinition; } }

      LinkConditionCalculator LinkConditionCalculator = new LinkConditionCalculator();
     
      /// <summary>
      /// prepare link
      /// </summary>
      /// <returns></returns>
      internal override gateways.GatewayResult Prepare()
      {
         LinkConditionCalculator.LinkHeader = DataviewHeader;
         LinkConditionCalculator.CalculateTaskCondition();
         if (!HasLocateOnVirtuals)
            CurrentCursor.CursorDefinition.LimitTo = 1;
         return base.Prepare();
      }

      /// <summary>
      /// true if link should be performed
      /// </summary>
      internal bool ShouldPerformLink
      {
         get
         {
            return LinkConditionCalculator.ShouldPerformLink;
         }
      }

      /// <summary>
      /// has Locate on Virtuals
      /// </summary>
      bool HasLocateOnVirtuals
      {
         get
         {
            return LocalDataviewManager.TaskViews.VirtualView.LinkHasLocate(Id);
         }
       }

      /// <summary>
      /// fetch link into record
      /// </summary>
      /// <param name="record"></param>
      /// <returns></returns>
      internal override GatewayResult Fetch(IRecord record)
      {
         GatewayResult result = CursorFetch();

         if (result.Success)
         {
             PositionCache.Set(GetPositionId(record), CurrentPosition);
             CopyValues(record);

         }
         if (!result.Success)
             PositionCache.Remove(GetPositionId(record));

         return result;
      }

      internal GatewayResult ModifyRecord(IRecord record)
      {
         GatewayResult result = new GatewayResult();
         if (GetPosition(record) != null) //record exists
            result = base.UpdateRecord(record);
         else if (DataSourceViewDefinition.CanInsert && ShouldPerformLink)
            result = base.InsertRecord(record);
         return result;
      }

      /// <summary>
      /// activate on record's delete
      /// </summary>
      /// <param name="record"></param>
      /// <returns></returns>
      protected override GatewayResult OnDelete(IRecord record)
      {
         GatewayResult result = new GatewayResult();
         ClearRecord(record);
         return result;
      }

      /// <summary>
      /// activated on record's update
      /// </summary>
      /// <param name="record"></param>
      /// <returns></returns>
      protected override GatewayResult OnInsert(IRecord record)
      {
         return ModifyRecord(record);
      }

      /// <summary>
      /// activated on recod's insert
      /// </summary>
      /// <param name="record"></param>
      /// <returns></returns>
      protected override GatewayResult OnUpdate(IRecord record)
      {
         return ModifyRecord(record);
      }
   }  

}
