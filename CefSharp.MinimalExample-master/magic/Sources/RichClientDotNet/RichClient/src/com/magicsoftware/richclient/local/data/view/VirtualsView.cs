using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.local.data.view;
using com.magicsoftware.richclient.local.data.view.fields;

namespace com.magicsoftware.richclient.local.data.view
{
   /// <summary>
   /// view for virtuals
   /// </summary>
   internal class VirtualsView : RuntimeViewBase
   {
      bool hasLocate = false;
      public override bool HasLocate
      {
         get
         {
            return (hasLocate || LocalDataviewManager.HasUserLocates);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="field"></param>
      /// <param name="indexInRecordView"></param>
      internal override void MapFieldDefinition(fields.IFieldView field, int indexInRecordView)
      {
         base.MapFieldDefinition(field, indexInRecordView);

         if (field.Locate != null && field.DataviewHeaderId == MAIN_VIEW_ID)
            hasLocate = true;
      }

      /// <summary>
      /// link has locate
      /// </summary>
      /// <param name="dataviewHeaderId"></param>
      /// <returns></returns>
      internal bool LinkHasLocate(int dataviewHeaderId)
      {
         foreach (var indexInRecord in this.fieldIndexInRecordByIndexInView.Values)
         {
            IFieldView fieldView = LocalDataviewManager.TaskViews.Fields[indexInRecord];
            if (fieldView.Id == dataviewHeaderId && fieldView.Locate != null) //check dataviewHeaderId!!!
               return true;
         }
         return false;
      }
   }
}
