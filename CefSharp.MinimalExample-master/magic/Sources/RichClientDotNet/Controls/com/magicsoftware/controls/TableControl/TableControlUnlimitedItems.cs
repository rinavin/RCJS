using System;
using com.magicsoftware.win32;
using System.Windows.Forms;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// TableControl for unlimited items
   /// </summary>
   public class TableControlUnlimitedItems : TableControl
   {

#if PocketPC
      bool DesignMode = false;
#endif
      /// <summary>
      /// update vertical scroll bar
      /// </summary>
      protected override void updateVScroll(bool calculateRowsInPage)
      {
         bool visibleChanged = false;
         bool hScrollBarExisted;
         bool hScrollBarExists;

         if (calculateRowsInPage)
            ComputeAndSetRowsInPage(false);

         hScrollBarExisted = isHscrollShown();

         if (isVscrollShown())
         {
            if (isVerticalScrollBarVisible != isVscrollShown())
            {
               NativeScroll.ShowScrollBar(this.Handle, NativeScroll.SB_VERT, true);
               isVerticalScrollBarVisible = true;
               visibleChanged = true;
            }

            NativeScroll.SCROLLINFO sc = ScrollInfo(NativeScroll.SB_VERT);

            if (this.DesignMode)
            {
               if (sc.nMax != 1)
               {
                  sc.nMax = 1;
                  NativeScroll.SetScrollInfo(this.Handle, NativeScroll.SB_VERT, ref sc, true);
               }
            }
            else if ((_virtualItemsCount != 0 && sc.nMax != _virtualItemsCount - 1) ||
                  (RowsInPage != 0 && sc.nPage != RowsInPage))
            {
               sc.nMax = _virtualItemsCount - 1;
               sc.nPage = RowsInPage;
               sc.nPos = _topIndex + RecordsBeforeCurrentView;
               sc.fMask = NativeScroll.SIF_PAGE | NativeScroll.SIF_RANGE | NativeScroll.SIF_POS;

               NativeScroll.SetScrollInfo(this.Handle, NativeScroll.SB_VERT, ref sc, true);
            }
         }
         else
         {
            // If the scrollbar was shown earlier (isVerticalScrollBarVisible is true) and is to be removed 
            // now (isVscrollShown() is false), handle scrollbar animation caused due to bug in Windows theme
            if (isVerticalScrollBarVisible)
            {
               HideVerticalScrollbarAnimationForWindowsThemes();
               visibleChanged = true;
            }
         }

         hScrollBarExists = isHscrollShown();

         if (visibleChanged)
            OnVScrollBarVisibleChanged(isVscrollShown(), hScrollBarExisted, hScrollBarExists);
      }

      /// <summary>
      /// is vertical scroll shown
      /// </summary>
      /// <returns></returns>
      protected override bool isVscrollShown()
      {
         return VerticalScrollBar && ((_virtualItemsCount > RowsInPage) || this.DesignMode);
      }

      /// <summary>
      /// gets the previous scroll value
      /// </summary>
      /// <param name="thumbPos">the new thumb position</param>
      /// <returns></returns>
      protected override int GetPrevScrollVal(int thumbPos)
      {
         return thumbPos;
      }

      /// <summary>
      /// gets the next thumb track value based on 'value'
      /// </summary>
      /// <param name="thumbTrackVal">thumb track value</param>
      /// <returns></returns>
      protected override int GetThumbTrackVal(int thumbTrackVal)
      {
         return thumbTrackVal;
      }

      /// <summary>
      /// vertical table scroll
      /// </summary>
      /// <param name="scrollEventType">Scroll Event Type</param>
      /// <param name="newPos">the new position</param>
      /// <param name="oldPos">the old position</param>
      /// <param name="scrollWindow">true, if we need to scroll the window right away</param>
      protected override void ScrollVertically(ScrollEventType scrollEventType, int newPos, int oldPos, bool scrollWindow)
      {
         NativeScroll.SCROLLINFO sc = ScrollInfo(NativeScroll.SB_VERT);
         int maxScroll = sc.nMax - sc.nPage + 1;
         if (newPos > maxScroll)
            newPos = maxScroll;
         if (newPos < 0)
            newPos = 0;

         NativeScroll.SetScrollPos(this.Handle, NativeScroll.SB_VERT, newPos, true);

         _topIndex = newPos - RecordsBeforeCurrentView;

         if (scrollWindow)
         {
            NativeWindowCommon.RECT rc = new NativeWindowCommon.RECT();
            rc.top = ClientRectangle.Top + _header.Height;
            rc.left = ClientRectangle.Left;
            rc.right = ClientRectangle.Right;
            rc.bottom = ClientRectangle.Bottom;
            NativeScroll.ScrollWindowEx(this.Handle, 0, (oldPos - newPos) * RowHeight, ref rc, (IntPtr)null, (IntPtr)null, (IntPtr)null, 0);
         }
         base.ScrollVertically(scrollEventType, newPos, oldPos, scrollWindow);
      }

      /// <summary>
      /// set the virtual list size
      /// </summary>
      /// <param name="size"></param>
      public override void SetVirtualItemsCount(int size)
      {
         if (_virtualItemsCount != size)
         {
            while (_virtualItemsCount > size && size >= 0)
            {
               _items[_virtualItemsCount - 1].Dispose();
               _items.RemoveAt(--_virtualItemsCount);
            }
            while (_virtualItemsCount < size)
            {
               _items.Add(new TableItem(_virtualItemsCount, this));
               _virtualItemsCount++;
            }
            _virtualItemsCount = size;
         }

         // In RC when table is invalidated it always get setitemscount(0) and then setItemsCount(realcount). So, after request with (size == 0)
         // there will always follow real request with correct number of rows (we always have at least one row in RC). So, when size == 0 we are 
         // in temporary state and should not update scroll. The problem with updating vscroll when size==0 is that now (after implementing the
         // topic for hiding scrollbar), table control layout is invoked that in turn calls updateHScroll.
         if (size != 0)
            updateVScroll(true);
      }

      /// <summary>
      /// set the items count
      /// </summary>
      /// <param name="size"></param>
      public override void SetItemsCount(int size)
      {
         SetVirtualItemsCount(size);
      }

      /// <summary>
      /// insert Item at position idx.
      /// </summary>
      /// <param name="idx"></param>
      public override void InsertItem(int idx)
      {
         base.InsertItem(idx);

         _virtualItemsCount++;
      }

      /// <summary>
      ///  remove Item at position idx.
      /// </summary>
      /// <param name="idx"></param>
      public override void RemoveItem(int idx)
      {
         base.RemoveItem(idx);

         _virtualItemsCount--;
      }

      /// <summary>
      /// scroll vertical scroll by rows 'rowsToScroll'
      /// </summary>
      /// <param name="rowsToScroll">number of rows to scroll</param>
      protected override void ScrollVerticallyMouseWheel(int rowsToScroll)
      {
         TopIndex += rowsToScroll;
      }

      /// <summary>
      /// sets Top Index of table
      /// </summary>
      /// <param name="value">new top index</param>
      /// <param name="scrollWindow">if true, table is scrolled. Otherwise, it is user's responsibility
      ///  to refresh the table
      /// </param>
      public override bool SetTopIndex(int value, bool scrollWindow)
      {
         bool changed = false;
         if (value != _topIndex)
         {
            if (isVscrollShown())
               ScrollVertically(ScrollEventType.ThumbPosition, value + RecordsBeforeCurrentView, _topIndex + RecordsBeforeCurrentView, scrollWindow);
            else
            {
               //check max
               int oldValue = _topIndex;
               _topIndex = Math.Min(value, _virtualItemsCount - RowsInPage + 1);
               _topIndex = Math.Max(_topIndex, 0);
               if (scrollWindow)
               {
                  ScrollEventArgs arg = new ScrollEventArgs(ScrollEventType.ThumbPosition, oldValue, _topIndex, ScrollOrientation.VerticalScroll);
                  this.OnScroll(arg);
               }
               _corner.Y = _topIndex;
            }
            changed = true;
         }
         return changed;
      }
      
      /// <summary>
      /// Returns the actual topIndex for the table by reducing the recordsBeforeCurrentView value.
      /// </summary>
      /// <param> name="scrollbarPosition"> scrollbarPosition</param>
      public override int GetTopIndexFromScrollbarPosition(int scrollbarPosition)
      {
         return scrollbarPosition - RecordsBeforeCurrentView;
      }
   }
}
