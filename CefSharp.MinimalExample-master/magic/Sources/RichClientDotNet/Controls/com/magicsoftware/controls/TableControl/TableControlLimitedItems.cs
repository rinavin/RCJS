using System;
using com.magicsoftware.win32;
using System.Windows.Forms;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// TableControl for limited items
   /// </summary>
   public class TableControlLimitedItems : TableControl
   {
      // _prevVScrollVal keeps track of last position on the VScroll. It is used
      // to simulate the dragging and mousewheel.
      private int _prevVScrollVal = 0;

      //the number of hidden rows (partially or fully) in table
      public int HiddenRowsInPage { get; private set; }

      private int _vScrollThumbPos;

      // SuspendPaint indicates if painting was explicitly suspended. This should not be confused with
      // Locking/Unlocking window update while dragging.
      public bool SuspendPaint { get; set; }

      //the number of rows in the table.
      public int PageSize { get; private set; }

      /// <summary>
      /// Ctor
      /// </summary>
      public TableControlLimitedItems()
      {
         _topIndex = 0;
         _prevVScrollVal = 0;
         _vScrollThumbPos = 0;
      }

      /// <summary>
      /// Update vertical scroll bar
      /// </summary>
      /// <param name="calculateRowsInPage">Calculate rows in page before updating scroll bar</param>
      protected override void updateVScroll(bool calculateRowsInPage)
      {
         bool visibleChanged = false;
         bool hScrollBarExisted;
         bool hScrollBarExists;

         if (SuspendPaint)
            return;

         if (calculateRowsInPage)
            ComputeAndSetRowsInPage(false);

         hScrollBarExisted = isHscrollShown();

         if (isVscrollShown())
         {
            if (isVerticalScrollBarVisible != isVscrollShown())
            {
               // In case of thumb drag window is already locked, no need to lock again
               if (RightToLeft == RightToLeft.Yes && !_isThumbDrag)
                  NativeWindowCommon.LockWindowUpdate(this.Handle);

               NativeScroll.ShowScrollBar(this.Handle, NativeScroll.SB_VERT, true);

               if (RightToLeft == RightToLeft.Yes && !_isThumbDrag)
                  NativeWindowCommon.LockWindowUpdate(IntPtr.Zero);

               visibleChanged = true;
            }

            // Unlock window and update
            if (UnlockWindowDrawOnThumbDrag())
               this.Update();

            NativeScroll.SCROLLINFO sc = ScrollInfo(NativeScroll.SB_VERT);
            sc.nPos = _vScrollThumbPos;
            sc.nMax = _virtualItemsCount;
            sc.nPage = PageSize;
            sc.fMask = NativeScroll.SIF_PAGE | NativeScroll.SIF_RANGE | NativeScroll.SIF_POS;
            NativeScroll.SetScrollInfo(this.Handle, NativeScroll.SB_VERT, ref sc, true);

            // lock drawing on the window
            LockWindowDrawOnThumbDrag();
         }
         else
         {
            // If the scrollbar was shown earlier (isVerticalScrollBarVisible is true) and is to be removed 
            // now (isVscrollShown() is false)... 
            if (isVerticalScrollBarVisible)
            {
               if (RightToLeft == RightToLeft.Yes && !_isThumbDrag)
                  NativeWindowCommon.LockWindowUpdate(this.Handle);

               // Also, handle scrollbar animation caused due to bug in Windows theme
               HideVerticalScrollbarAnimationForWindowsThemes();

               if (RightToLeft == RightToLeft.Yes && !_isThumbDrag)
                  NativeWindowCommon.LockWindowUpdate(IntPtr.Zero);

               //...end thumb drag.
               if (_isThumbDrag)
                  EndThumbDrag();

               visibleChanged = true;
            }
         }

         hScrollBarExists = isHscrollShown();

         if (visibleChanged)
            OnVScrollBarVisibleChanged(isVscrollShown(), hScrollBarExisted, hScrollBarExists);

         isVerticalScrollBarVisible = isVscrollShown();
      }

      /// <summary>
      /// is vertical scroll shown
      /// </summary>
      /// <returns></returns>
      protected override bool isVscrollShown()
      {
         return VerticalScrollBar && (_virtualItemsCount > PageSize);
      }

      /// <summary> calculate rows in Page depending on the row height, if 
      /// table doesn't have row placement. Assigns an extra entry for 
      /// partial row in case of limited items.
      /// </summary>
      /// <param name="forceCompute">true to forcefully compute row in page. </param>
      /// Calculate rows in page. Assigns an extra entry for partial row in case of limited items.
      /// </summary>
      public override void ComputeAndSetRowsInPage(bool forceCompute)
      {
         HiddenRowsInPage = 0;
         if (RowHeight != 0 && (!HasRowPlacement || forceCompute))
         {
            if (HasRowPlacement)
            {
               int tableHeight = Math.Max(1, (ClientRectangle.Height - TitleHeight));
               RowsInPage = tableHeight / RowHeight;
            }
            else
            {
               //algorithm for calculating HiddenRowsInPage is taken from CPP's RowsToAdd()
               bool hasPartialRows = false;
               bool temp = false;
               int visibleRows = CalculateNumberOfRowsInTable(false, ref hasPartialRows);
               RowsInPage = CalculateNumberOfRowsInTable(true, ref temp);

               if (hasPartialRows)
                  HiddenRowsInPage = 1;
               HiddenRowsInPage += (RowsInPage - visibleRows);
            }
         }
      }

      /// <summary> Calculates the no. of rows that can fit into the table.
      /// Also indicates if there is any partial row.
      /// The algorithm is taken from 1.9's CalculateNumberOfRowsInTable()
      /// </summary>
      /// <param name="scollbarAsClient"></param>
      /// <param name="partialRowExist"></param>
      /// <returns></returns>
      int CalculateNumberOfRowsInTable(bool scollbarAsClient, ref bool partialRowExist)
      {
         int noOfRows = 0;
         partialRowExist = false;

         int clientHeight = ClientSize.Height;

         noOfRows = Math.Max((clientHeight - TitleHeight) / RowHeight, 1);

         int scrollbarHeight = (Height - (BorderHeight * 2)) - clientHeight;

         if (scollbarAsClient)
            clientHeight += scrollbarHeight;

         noOfRows = Math.Max((clientHeight - TitleHeight) / RowHeight, 1);

         partialRowExist = ((clientHeight - TitleHeight) % RowHeight) > 0;

         if (partialRowExist)
            noOfRows++;

         return noOfRows;
      }

      /// <summary>
      /// gets the previous scroll value
      /// </summary>
      /// <param name="thumbTrackVal">the new thumb position</param>
      /// <returns></returns>
      protected override int GetThumbTrackVal(int thumbTrackVal)
      {
         int newThumbTrackVal = thumbTrackVal;

         // simulate OL dragging i.e fetch one record at a time
         if (thumbTrackVal < _prevVScrollVal)
            newThumbTrackVal = _prevVScrollVal - 1;   // move up by 1 row
         else if (thumbTrackVal > _prevVScrollVal)
            newThumbTrackVal = _prevVScrollVal + 1;   // move down by 1 row

         return newThumbTrackVal;
      }

      /// <summary>
      /// gets the previous scroll value
      /// </summary>
      /// <param name="thumbPos">the new thumb position</param>
      /// <returns></returns>
      protected override int GetPrevScrollVal(int thumbPos)
      {
         return _prevVScrollVal;
      }

      /// <summary>
      /// sets the previous scroll value
      /// </summary>
      /// <param name="thumbPos">the new thumb position</param>
      protected override void SetPrevScrollVal(int thumbPos)
      {
         _prevVScrollVal = thumbPos;
      }

      /// <summary>
      /// set the virtual list size
      /// </summary>
      /// <param name="size"></param>
      public override void SetVirtualItemsCount(int size)
      {
         _virtualItemsCount = size;
         updateVScroll(false);
      }

      /// <summary>
      /// set the items count
      /// </summary>
      /// <param name="size"></param>
      public override void SetItemsCount(int size)
      {
         int oldSize = _items.Count;
         if (oldSize != size)
         {
            while (oldSize > size && size >= 0)
            {
               _items[oldSize - 1].Dispose();
               _items.RemoveAt(--oldSize);
            }
            while (oldSize < size)
            {
               _items.Add(new TableItem(oldSize, this));
               oldSize++;
            }

            updateVScroll(true);
         }
      }

      /// <summary>
      /// set the vertical scroll thumb position
      /// </summary>
      /// <param name="pos">the position to set</param>
      public override void SetVScrollThumbPos(int pos)
      {
         _vScrollThumbPos = pos;
         _prevVScrollVal = pos;
         if (VerticalScrollBar)
            updateVScroll(false);
      }

      /// <summary>
      /// set the pageSize for vertical scrollbar
      /// </summary>
      /// <param name="pageSize">the pageSize to set</param>
      public override void SetVScrollPageSize(int pageSize)
      {
         PageSize = pageSize;
      }

      /// <summary>
      /// lock window draw if thumb drag in process
      /// </summary>
      public void LockWindowDrawOnThumbDrag()
      {
         if (_isThumbDrag)
            NativeWindowCommon.LockWindowUpdate(this.Handle);
      }

      /// <summary>
      /// unlock window draw if thumb drag in process or has ended
      /// </summary>
      /// <returns></returns>
      public bool UnlockWindowDrawOnThumbDrag()
      {
         if (_isThumbDrag)
         {
            NativeWindowCommon.LockWindowUpdate(IntPtr.Zero);
            return true;
         }
         else
            return false;
      }

      /// <summary>
      /// called when the thumb dragging is started or is in process
      /// </summary>
      protected override void StartThumbDrag()
      {
         base.StartThumbDrag();

         // lock drawing on the window (it will be unlocked in thumb drag is released).
         LockWindowDrawOnThumbDrag();
      }

      /// <summary>
      /// called when the thumb dragging has ended
      /// </summary>
      protected override void EndThumbDrag()
      {
         // unlock drawing on the window
         UnlockWindowDrawOnThumbDrag();

         base.EndThumbDrag();
      }

      /// <summary>
      /// scroll vertical scroll by rows 'rowsToScroll'
      /// </summary>
      /// <param name="rowsToScroll">number of rows to scroll</param>
      protected override void ScrollVerticallyMouseWheel(int rowsToScroll)
      {
         // Removed because mouse wheel will be handled from DefaultHandler.

         //int oldPos = _prevVScrollVal;
         //int newPos = _prevVScrollVal + rowsToScroll;

         //if (newPos < 0)
         //{
         //   oldPos = -newPos;
         //   newPos = 0;
         //}

         //// set the thumb on the vertical scroll bar
         //ScrollVertically(ScrollEventType.ThumbPosition, newPos, oldPos, true, true);
         //_prevVScrollVal = newPos;
      }

      /// <summary>
      /// gets the vertical scroll thumb position
      /// </summary>
      /// <returns></returns>
      public override int GetVScrollThumbPos()
      {
         return _vScrollThumbPos;
      }
   }
}
