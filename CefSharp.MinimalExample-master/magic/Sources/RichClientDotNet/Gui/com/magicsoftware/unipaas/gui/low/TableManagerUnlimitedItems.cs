using System;
using System.Collections.Generic;
using com.magicsoftware.controls;
using System.Windows.Forms;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>
   /// manages TableControl for unlimited items
   /// </summary>
   internal class TableManagerUnlimitedItems : TableManager
   {
      protected internal bool _includesFirst; // true, if table includes it's real first element
      protected internal bool _includesLast; // true, if table includes it's real last element

      protected bool _realIncludesFirst; // this is includeFirst that was sent from
      // the  server includefirst member may be updated here, because of listview limitations

      /// <summary>
      /// ctor
      /// </summary>
      /// <param name="tableControl"></param>
      /// <param name="mgControl"></param>
      /// <param name="children"></param>
      /// <param name="columnsCount"></param>
      /// <param name="style"></param>
      internal TableManagerUnlimitedItems(TableControl tableControl, GuiMgControl mgControl, List<GuiMgControl> children,
                      int columnsCount, int style)
         : base(tableControl, mgControl, children, columnsCount, style)
      {
         _realIncludesFirst = _includesFirst = false;
         _includesLast = false;
      }

      /// <summary> returns true if table includes it's real first line
      /// </summary>
      /// <returns></returns>
      internal bool isIncludesFirst()
      {
         return _includesFirst;
      }

      /// <summary> sets includesFirst member
      /// </summary>
      /// <param name="includesFirst"></param>
      internal void setIncludesFirst(bool includesFirst)
      {
         _realIncludesFirst = includesFirst;
         // if not realinclude first that includesFirst is computed in updateIncludeFirst
         if (_realIncludesFirst)
            _includesFirst = includesFirst;
         _tableControl.StartTablePaintWithAlternateColor = !_includesFirst;
      }

      /// <summary> If number of rows in table is less then rows in page we can not create hidden dummy record in the
      /// beginning of the table. So, we must update includesFirst according to number rows in table and number
      /// rows in page
      /// </summary>
      /// <returns> true is includesFirst was changed</returns>
      protected bool updateIncludeFirst()
      {
         bool prev = _includesFirst;
         _includesFirst = _realIncludesFirst;
         _tableControl.StartTablePaintWithAlternateColor = !_includesFirst;
         return prev != _includesFirst;
      }

      /// <summary> </summary>
      /// <returns> true if table includes it's real first line</returns>
      internal bool isIncludesLast()
      {
         return _includesLast;
      }

      /// <summary> returns true if table includes it's real last line
      /// </summary>
      /// <returns></returns>
      internal void setIncludesLast(bool includesLast)
      {
         _includesLast = includesLast;
      }

      /// <summary>
      /// gets last value of the control that was previously in focus
      /// is working only for text controls 
      /// used to restore control's edited value after current record is replaced by different chunk
      /// </summary>
      /// <returns></returns>
      private LastFocusedVal GetLastEditedFocusedVal()
      {
         Form form = GuiUtilsBase.FindForm(_tableControl);
         LastFocusedVal lastFocusedVal = null;
         MapData mapData = ((TagData)form.Tag).LastFocusedMapData;

         if (mapData != null && mapData.getControl() != null)
         {
            Object obj = controlsMap.object2Widget(mapData.getControl(), mapData.getIdx());
            var lg = obj as LogicalControl;
            if (lg != null && lg.ContainerManager == this)
            {
               GuiMgControl mgControl = mapData.getControl();
               if (mgControl.isTextControl())
                  lastFocusedVal = new LastFocusedVal(mgControl, mapData.getIdx(), lg.Text);
            }
         }
         return lastFocusedVal;
      }


      /// <summary> return true if any row needs to be refreshed
      /// </summary>
      /// <param name="top"> </param>
      /// <returns> </returns>
      internal bool needToGetRows(int top)
      {
         int last = top + _rowsInPage + 1;
         if (_rowsInPage == 0)
            last++;
         if (_includesLast)
            last = Math.Min(_mgCount, last);
         for (int i = top; i < last; i++)
         {
            if (isValidIndex(i))
            {
               TableItem item = _tableControl.getItem(i);
               // row is not created or it is invalide
               if (item == null ||
                   item.Controls == null || !item.IsValid)
                  return true;
            }
            else
               return true;
         }
         return false;
      }

      /// <summary> 
      /// vertical scroll
      /// </summary>
      /// <param name="ea"></param>
      internal override void ScrollVertically(ScrollEventArgs ea)
      {
         if (ea.Type == ScrollEventType.EndScroll)
            return;

         int newPos = ea.NewValue - _tableControl.RecordsBeforeCurrentView;

         if (ea.Type == ScrollEventType.First && _includesFirst == false)
         {
            newPos = 1;
            SetGuiTopIndex(newPos);
            _tableControl.Refresh();
         }
         else if (ea.Type == ScrollEventType.Last && _includesLast == false)
         {
            newPos = Math.Max(_tableControl.VirtualItemsCount - _rowsInPage - 3, 0);
            SetGuiTopIndex(newPos);
         }
         else if (_includesFirst && newPos < 0)
         {
            newPos = 0;
         }
         else if (_includesLast && newPos > _tableControl.VirtualItemsCount - _rowsInPage)
         {
            newPos = Math.Max(_tableControl.VirtualItemsCount - _rowsInPage, 0);
            SetGuiTopIndex(newPos);
         }

         if (needToGetRows(newPos))
         {
            LastFocusedVal lastFocusedVal = GetLastEditedFocusedVal();
            int desiredTopIndex = getMagicIndex(newPos);

            Events.OnGetRowsData(_mgControl, desiredTopIndex, false, lastFocusedVal);
         }
         else
            refreshPage();
      }

      /// <summary>
      /// gets the items count of table control
      /// </summary>
      /// <returns></returns>
      internal int GetTableControlItemsCount()
      {
         return _tableControl.VirtualItemsCount;
      }

      /// <summary>
      /// sets the items count of table control
      /// </summary>
      /// <param name="newCount"></param>
      internal void SetTableControlItemsCount(int newCount)
      {
         SetTableVirtualItemsCount(newCount);
      }

      /// <summary>
      /// sets the virtual items count of table control
      /// </summary>
      /// <param name="count">the count to set</param>
      internal override void SetTableVirtualItemsCount(int count)
      {
#if !PocketPC
         //prevent unnecessary resizes
         _tableControl.Layout -= TableHandler.getInstance().LayoutHandler;
#endif

         _tableControl.VirtualItemsCount = count;

#if !PocketPC
         //prevent unnecessary resizes
         _tableControl.Layout += TableHandler.getInstance().LayoutHandler;
#endif
      }

      /// <summary>
      /// set and save last top index
      /// </summary>
      /// <param name="guiTopIndex"> </param>
      internal override void SetGuiTopIndex(int guiTopIndex)
      {
         _guiTopIndex = guiTopIndex;
         _tableControl.Scroll -= TableHandler.getInstance().ScrollHandler;
         _tableControl.SetTopIndex(guiTopIndex, false);
         _tableControl.Scroll += TableHandler.getInstance().ScrollHandler;
      }

      /// <summary>
      /// set recordsBeforeCurrentView for tableControl.
      /// </summary>
      /// <param name="value"> </param>
      internal void SetRecordsBeforeCurrentView(int value)
      {
         _tableControl.RecordsBeforeCurrentView = value;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="newRowsInPage"></param>
      internal override bool resize()
      {
         int orgNumberOfRowsInPage = _rowsInPage;
         bool success = base.resize();

         if (success && orgNumberOfRowsInPage != _rowsInPage)
         {
            _inResize = true;

            if (!_realIncludesFirst)
            {
               if (updateIncludeFirst())
               {
                  if (_includesFirst && _tableControl.VirtualItemsCount > 0)
                     _tableControl.RemoveItem(0);
                  else
                  {
                     _tableControl.InsertItem(0);
                     _prevTopGuiIndex = -1;
                     SetTopIndex(0);
                  }
               }
            }

            if (IsSmallerThanScreen(_mgCount))
            //QCR #997566, need to show scrollbar to let user know there are records before current record
            //create dummy records for this
            {
               SetTableItemsCount(_rowsInPage + 1);
               SetGuiTopIndex(_guiTopIndex);
            }

            Events.OnTableResize(_mgControl, _rowsInPage);
            int top = getMagicIndex(_tableControl.TopIndex);

            // TODO: merge OnGetRowsData into Events.OnResize.
            // 1.it is possible that in the beginning before we updated top index to 1, resize calls
            // this method and we receive negative top index. It must be corrected to 1.
            //2.QCR #987247, like in online during placement all records should be refreshed, not only new ones
            Events.OnGetRowsData(_mgControl, Math.Max(top, 0), true, null);

            _inResize = false;
         }

         return success;
      }

      /// <summary>
      /// table has more rows then records , but includeFirst is false
      /// </summary>
      /// <param name="newCount"></param>
      /// <returns></returns>
      protected bool IsSmallerThanScreen(int newCount)
      {
         return (!_includesFirst /*&& includesLast */&& newCount < _rowsInPage + 1);
      }

      /// <summary> translates magic row number into SWT line number
      /// </summary>
      /// <param name="mgIndex"></param>
      /// <returns></returns>
      internal override int getGuiRowIndex(int mgIndex)
      {
         int guiIndex = base.getGuiRowIndex(mgIndex);
         if (!_includesFirst)
            guiIndex++;
         return guiIndex;
      }

      /// <summary> translates gui line number into magic row number
      /// </summary>
      /// <param name="guiIndex"></param>
      /// <returns></returns>
      internal override int getMagicIndex(int guiIndex)
      {
         int mgIndex = base.getMagicIndex(guiIndex);
         if (!_includesFirst)
            mgIndex--;
         // assert (mgIndex >= 0);
         return mgIndex;
      }

      /// <summary>
      /// sets Table Items Count
      /// </summary>
      /// <param name="newCount">the count to set</param>
      internal override void SetTableItemsCount(int newCount)
      {
          if (_mgCount != newCount)
              RefreshPageNeeded = true;

         _mgCount = newCount;
         int oldCount = GetTableControlItemsCount();
         bool updated = false;
         if (newCount != 0)
         {
            // update includesFirst according to number rows in table, if number of rows is too small
            // creating dummy record is impossible
            updated = updateIncludeFirst();
            if (!_includesFirst)
            {
               newCount++;
               if (updated)
               // when chunks are added and size becomes bigger than page,
               // create dummy first line
               {
                  //TODO new TableItem(table, 0, 0);
                  _prevTopGuiIndex = -1;
                  //SetItemsCount may cause resize, in this case we need mgCount to be updated with correct value
                  if (_tableControl.VirtualItemsCount < 2)
                  {
                     //setItemscount 1 will not work if we have less then 2 items. QCR #434423
                     SetTableControlItemsCount(newCount);
                  }
               }
            }
            if (!_includesLast)
               newCount++;
         }

         if (IsSmallerThanScreen(newCount))
            //QCR #997566, need to show scrollbar to let user know there are records before current record
            newCount = _rowsInPage + 1; //create dummy records for this

         if (oldCount != newCount || updated)
            RefreshPageNeeded = true;

         // In RC when table is invalidated it always get setitemscount(0) and then setItemsCount(realcount). So, after request with (newCount == 0)
         // there will always follow real request with correct number of rows (we always have at least one row in RC). So, when newCount == 0 we are 
         // in temporary state and should not do any “resize”. 
         if (newCount == 0)
            _tableControl.HorizontalScrollVisibilityChanged -= TableHandler.getInstance().HorizontalScrollVisibilityChangedHandler; 

         SetTableControlItemsCount(newCount);

         if (newCount == 0)
            _tableControl.HorizontalScrollVisibilityChanged += TableHandler.getInstance().HorizontalScrollVisibilityChangedHandler;

         if (!_includesFirst && updated)
            SetTopIndex(0);
      }

      /// <summary>
      /// gets the number of rows to refresh
      /// </summary>
      /// <returns></returns>
      protected override int GetNumberOfRowsToRefresh()
      {
         int count = _tableControl.getItemsCount();
         if (IsSmallerThanScreen(_mgCount))
            count = _mgCount + 1;
         return count;
      }

      /// <summary>
      /// Indicates whether _rowsInPage includes partial rows or not.
      /// </summary>
      /// <returns></returns>
      protected override bool IsPartialRowIncludedInRowsInPage()
      {
         return false;
      }
   }
}
