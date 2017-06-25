using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.local.data.view.fields;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.richclient.exp;

namespace com.magicsoftware.richclient.local.data.view.RecordCompute
{

   /// <summary>
   /// this class is responsible for building records computer
   /// </summary>
   internal class RecordComputerBuilder
   {
      internal LocalDataviewManager LocalDataviewManager { get; set; }
      internal TaskViewsCollection TaskViews { get; set; }

      Dictionary<RecomputeId, ComputeLinkStrategy> linkComputers = new Dictionary<RecomputeId, ComputeLinkStrategy>();

      /// <summary>
      /// builds record computer
      /// </summary>
      /// <returns></returns>
      internal virtual RecordComputer Build()
      {
         RecordComputer recordComputer = new RecordComputer();
         recordComputer.DataviewSynchronizer = LocalDataviewManager.DataviewSynchronizer;

         int nextViewDataViewHeaderId = -1;
         int? nextLinkStartAfterField = GetNextLinkStartField(ref nextViewDataViewHeaderId);

         for (int i = 0; i < TaskViews.Fields.Count; i++)
         {
            if (i == nextLinkStartAfterField)
            {
               ComputeLinkStrategy computeStrategy = new ComputeLinkStrategy() { View = TaskViews.LinkViews[nextViewDataViewHeaderId] };
               RecomputeId unitId = RecomputeIdFactory.GetRecomputeId(computeStrategy.View.DataviewHeader);
               //add computer for fetch of the links
               recordComputer.Add(unitId, computeStrategy);
               linkComputers.Add(unitId, computeStrategy);
               nextLinkStartAfterField = GetNextLinkStartField(ref nextViewDataViewHeaderId);
            }

            //add computer for fields
            AddComputeFieldStrategy(recordComputer, TaskViews.Fields[i]);
         }

         // Add the range expression computer - must be after all other computation is taken care of.
         AddBoundaryExpressionComputeStrategy(recordComputer, PropInterface.PROP_TYPE_TASK_PROPERTIES_RANGE, UnitComputeErrorCode.RangeFailed);

         return recordComputer;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="recordComputer"></param>
      /// <param name="field"></param>
      private void AddComputeFieldStrategy(RecordComputer recordComputer, IFieldView field)
      {
         bool checkLocate = ShouldCheckLocateOnField(field);
         if (field.IsLink)
         {
            RecomputeId ownerLinkComputeId = RecomputeIdFactory.GetDataviewHeaderComputeId(field.DataviewHeaderId);
            var ownerLinkComputer = linkComputers[ownerLinkComputeId];
            ownerLinkComputer.Add(CreateComputeFieldStrategy(field, checkLocate));
         }
         else
            recordComputer.Add(CreateComputeFieldStrategy(field, checkLocate));
      }

      protected virtual bool ShouldCheckLocateOnField(IFieldView field)
      {
         return (field.Locate != null && field.IsVirtual && field.IsLink);
      }

      protected ComputeFieldStrategy CreateComputeFieldStrategy(IFieldView field, bool checkLocate)
      {
         return new ComputeFieldStrategy()
         {
            Field = field,
            CheckLocate = checkLocate,
            ShouldComputeOnFetch = field.IsVirtual && !field.AsReal && field.HasInitExpression
         };
      }

      /// <summary>
      /// returns index of the first field of next link or null if it does not exists
      /// </summary>
      /// <param name="nextViewDataViewHeaderId"> id of next dataview header (link)</param>
      /// <returns></returns>
      private int? GetNextLinkStartField(ref int nextViewDataViewHeaderId)
      {
         int? nextLinkStartAfterField = null;
         nextViewDataViewHeaderId++;
         if (TaskViews.LinkViews.ContainsKey(nextViewDataViewHeaderId))
         {
            nextLinkStartAfterField = TaskViews.LinkViews[nextViewDataViewHeaderId].DataviewHeader.LinkStartAfterField;
         }
         return nextLinkStartAfterField;
      }

      protected void AddBoundaryExpressionComputeStrategy(RecordComputer recordComputer, int boundaryPropertyId, UnitComputeErrorCode computeFailureCode)
      {
         var task = TaskViews.Task;
         if (task.checkIfExistProp(boundaryPropertyId))
         {
            Property prop = task.getProp(boundaryPropertyId);
            int expId = prop.GetExpressionId();
            var expression = task.getExpById(expId);
            if (expression != null)
               recordComputer.Add(new ComputeBoundaryExpressionStrategy(expression) { FailureCode = computeFailureCode });
         }
      }
   }
}
