using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.richclient.tasks.sort;
using com.magicsoftware.richclient.local.data.view.fields;
using com.magicsoftware.util;

namespace com.magicsoftware.richclient.local.data.view
{
   /// <summary>
   /// factory that creates sort key.
   /// </summary>
   internal class SortKeyBuilder
   {
      RuntimeRealView mainView;
      SortCollection RuntimeSorts;

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="view">mainView</param>
      /// <param name="runtimeSorts">sorts collection</param>
      public SortKeyBuilder(RuntimeRealView view, SortCollection runtimeSorts)
      {
         this.mainView = view;
         this.RuntimeSorts = runtimeSorts;
      }

      /// <summary>
      /// build sort key
      /// </summary>
      /// <returns>DbKey</returns>
      internal DBKey Build ()
      {
         DBKey sortKey = new DBKey();

         sortKey.SetMask(KeyMasks.KeySortMask);
         sortKey.SetMask(KeyMasks.KeyTypeVirtual);
         if (mainView.LocalDataviewManager.Task.UniqueSort == UniqueTskSort.Unique)
            sortKey.SetMask(KeyMasks.UniqueKeyModeMask);
         else
            sortKey.SetMask(KeyMasks.DuplicateKeyModeMask);
         for (int i = 0; i < RuntimeSorts.getSize(); i++)
         {
            int indexInView = RuntimeSorts.getSort(i).fldIdx;

            
            if (IsFieldSortable(indexInView))
            {
               Sort sort = RuntimeSorts.getSort(i);
               DBSegment seg = BuildSegment(sort.dir, sort.fldIdx);
               sortKey.Segments.Add(seg);
            }
         }

         if (sortKey.Segments.Count == 0)
            return null;
         
         return sortKey;
      }

      /// <summary>
      /// build segment for Sort Key
      /// </summary>
      /// <param name="dir"></param>
      /// <param name="fldIdx"></param>
      internal DBSegment BuildSegment(Boolean dir, int fldIdx)
      {
         DBSegment seg = new DBSegment();

         int realFldIdx = mainView.GetFieldIndexInViewByIndexInRecord(fldIdx - 1);
         seg.Field = mainView.GetDbField(realFldIdx);
         seg.SetMask(dir == true ? SegMasks.SegDirAscendingMask : SegMasks.SegDirDescendingMask);
         return seg;
      }

      /// <summary>
      /// Check if field is sortable or Not
      /// </summary>
      /// <param name="indexInView"> Index of field Of which need to check the sortable condition</param>
      /// <returns></returns>
      private bool IsFieldSortable(int indexInView)
      {
         bool isFieldSortable = true;
         IFieldView field = mainView.LocalDataviewManager.TaskViews.Fields[indexInView - 1];

         // Sort segment should be added only if fld is real and from Main Data Sourcei.e. MainView.
         if (!field.IsVirtual && field.DataviewHeaderId == RuntimeViewBase.MAIN_VIEW_ID)
         {
            int realFldIdx = mainView.GetFieldIndexInViewByIndexInRecord(indexInView - 1);
            DBField dbTimeField = mainView.GetDbField(realFldIdx);

            if (dbTimeField.Attr == (char)StorageAttributeType.Time && dbTimeField.PartOfDateTime > 0)
            {
               //If sort is applied only on time field of date time pair then sort is not allowed to time field.
               isFieldSortable = false;

               //If sort is applied on date and time field of date time pair then sort is not allowed to time field.
               for (int i = 0; i < RuntimeSorts.getSize(); i++)
               {
                  Sort sort = RuntimeSorts.getSort(i);

                  int dateIndex = mainView.GetFieldIndexInViewByIndexInRecord(sort.fldIdx - 1);
                  DBField dbDateField = mainView.GetDbField(dateIndex);

                  if (dbDateField.Isn == dbTimeField.PartOfDateTime)
                  {
                     isFieldSortable = true;
                     break;
                  }

               }
            }
         }
         else
         {
            isFieldSortable = false;
         }

         return isFieldSortable;
      }
   }
}
