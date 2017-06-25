using System;
using System.Drawing;
using System.Windows.Forms;
using com.magicsoftware.win32;
using com.magicsoftware.controls.utils;
using System.Diagnostics;

#if PocketPC
using Message = com.magicsoftware.mobilestubs.Message;
#else
using com.magicsoftware.controls.designers;
using System.ComponentModel.Design;
using System.Collections;
#endif

namespace com.magicsoftware.controls
{
   /// <summary>
   /// implements table control : responsible for table scrolling and resize adjustments
   /// </summary>
   public abstract partial class TableControl
   {
      public delegate void VScrollBarVisibleChangedDelegate(bool isVScrollBarVisible, bool hScrollBarExisted, bool hScrollBarExists);
      public event VScrollBarVisibleChangedDelegate VScrollBarVisibleChanged;
      protected void OnVScrollBarVisibleChanged(bool isVScrollBarVisible, bool hScrollBarExisted, bool hScrollBarExists)
      {
         if (VScrollBarVisibleChanged != null)
            VScrollBarVisibleChanged(isVScrollBarVisible, hScrollBarExisted, hScrollBarExists);
      }

      protected abstract void updateVScroll(bool calculateRowsInPage);
      protected abstract bool isVscrollShown();

      // This flag indicates if the scroll bar is visible and should be used to avoid the situations where some code is executed
      // to hide the scrollbar when it is already hidden
      protected bool isVerticalScrollBarVisible;
      private bool isHorizontalScrollBarVisible;

      /// <summary>
      /// returns true if horizontal sceollbar is visible (for studio use)
      /// </summary>
      public bool IsHorizontalScrollBarVisible
      {
         get { return isHorizontalScrollBarVisible; }
      }
      /// <summary>
      /// update horizontal scrollbar
      /// </summary>
      protected virtual void updateHScroll()
      {
         if (HorizontalScrollBar)
         {
            int logWidth = getLogWidth();
            int curWidth = ClientSize.Width;
            int Xcorner = getXCorner();

            SuspendLayout();
            //should be scrollbar
            if (logWidth > curWidth)
            {
               int maxRange = logWidth - curWidth;
               if (Xcorner > maxRange)
                  ScrollHorizontally(Xcorner, maxRange);

               NativeScroll.SCROLLINFO sc = ScrollInfo(NativeScroll.SB_HORZ);
               sc.nMax = logWidth;
               sc.nPage = curWidth;
               sc.fMask = NativeScroll.SIF_PAGE | NativeScroll.SIF_RANGE;
               NativeScroll.SetScrollInfo(this.Handle, NativeScroll.SB_HORZ, ref sc, false);

               if (rightToLeftLayout)
               {
                  int prevPos = 0, newPos;
                  prevPos = sc.nMax - sc.nPos;
                  newPos = sc.nMax - prevPos;
                  if (newPos != sc.nPos)
                     ScrollHorizontally(sc.nPos, newPos);
               }

               if (!isHorizontalScrollBarVisible)
               {
                  NativeScroll.ShowScrollBar(this.Handle, NativeScroll.SB_HORZ, true);
                  isHorizontalScrollBarVisible = true;
               }
            }
            //no scrollbar
            else // if (logWidth <= curWidth)
            {
               if (_prevLogWidth > _prevWidth)
               {
                  ScrollHorizontally(Xcorner, 0);
               }

               //It seems that horizontal scrollbar is shown by default on displaying vertical scroll. 
               // May be even the otherway is true and hence there is a need to refresh vertical scrollbar's visibility in following code
               // Anyway, hide it unconditionally. This fixes the issue of black area in table control
               //if (isHorizontalScrollBarVisible)
               {
                  NativeScroll.ShowScrollBar(this.Handle, NativeScroll.SB_HORZ, false);
                  NativeScroll.ShowScrollBar(this.Handle, NativeScroll.SB_VERT, isVscrollShown());
                  isHorizontalScrollBarVisible = false;
               }
            }

            if ((_prevLogWidth > _prevWidth && logWidth <= curWidth) ||
                (_prevLogWidth <= _prevWidth && logWidth > curWidth))
            {
               if (HorizontalScrollVisibilityChanged != null)
                  HorizontalScrollVisibilityChanged(this, new EventArgs());
            }

            _prevLogWidth = logWidth;
            _prevWidth = curWidth;
            _displayWidth = Math.Max(logWidth, curWidth);
            ResumeLayout(); 
         }
         else
         {
            // We need to update the display width even when there is no scrollbar so as to get correct cordinate in case of Rtl  
            UpdateDisplayWidth();
         }
      }

      /// <summary>
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void TableControl_Load(object sender, EventArgs e)
      {
         //needed for XP themes
#if !PocketPC //tmp
         this.AdjustFormScrollbars(true);
#endif
         _header.AfterSectionTrack -= new HeaderSectionWidthEventHandler(header_AfterSectionTrack);
         _header.AfterSectionTrack += new HeaderSectionWidthEventHandler(header_AfterSectionTrack);
      }

      /// <summary>
      /// returns corner
      /// </summary>
      /// <returns></returns>
      public Point GetCorner()
      {
         Point pt = new Point(getXCorner(), getYCorner());
         return pt;
      }

      /// <summary>
      /// returns X corner
      /// </summary>
      /// <returns></returns>
      public int getXCorner()
      {
         NativeScroll.SCROLLINFO si = ScrollInfo(NativeScroll.SB_HORZ);
         return si.nPos;
      }

      /// <summary>
      /// returns Y corner
      /// </summary>
      /// <returns></returns>
      public int getYCorner()
      {
         NativeScroll.SCROLLINFO si = ScrollInfo(NativeScroll.SB_VERT);
         return si.nPos;
      }

      /// <summary>
      /// update displaywidth
      /// </summary>
      public void UpdateDisplayWidth()
        {
            int logWidth = getLogWidth();
            int curWidth = ClientSize.Width;
            _displayWidth = Math.Max(logWidth, curWidth);
        }

      /// get table logical width
      public int getLogWidth()
      {
         int width = 0;
         foreach (HeaderSection section in _header.Sections)
         {
            width += section.Width;
         }
         return width;
      }

      /// <summary>
      /// adjust header size to table size
      /// </summary>
      private void adjustHeader()
      {
         _header.Width = tableStyleRenderer.GetHeaderRectangle(_header).Width;
      }

      /// <summary>
      /// handle vertical scroll
      /// </summary>
      /// <param name="m"></param>
      private void OnVScroll(ref Message m)
      {
         int newValue = Int32.MinValue;
         NativeScroll.SCROLLINFO sc = ScrollInfo(NativeScroll.SB_VERT);
         int value = sc.nPos;
         int nScrollCode = NativeWindowCommon.LoWord((int)m.WParam);

         switch (nScrollCode)
         {
            case NativeScroll.SB_TOP:
               newValue = 0;
               break;
            case NativeScroll.SB_BOTTOM:
               newValue = sc.nMax;
               break;
            case NativeScroll.SB_LINEUP:
               newValue = value - 1;
               break;
            case NativeScroll.SB_LINEDOWN:
               newValue = value + 1;
               break;
            case NativeScroll.SB_PAGEUP:
               newValue = value - sc.nPage;
               break;
            case NativeScroll.SB_PAGEDOWN:
               newValue = value + sc.nPage;
               break;
            case NativeScroll.SB_THUMBPOSITION:
               EndThumbDrag();
               break;
            case NativeScroll.SB_THUMBTRACK:
               StartThumbDrag();
               newValue = GetThumbTrackVal(sc.nTrackPos);
               value = GetPrevScrollVal(sc.nPos);
               break;
         }
         if (newValue != Int32.MinValue && newValue != value)
         {
            ScrollVertically((ScrollEventType)m.WParam, newValue, value, true);
         }
      }
#if !PocketPC
      /// <summary>
      /// retruns true if table's children are selected in the designer
      /// </summary>
      /// <returns></returns>
      private bool AreChildrenSelected()
      {
         ISelectionService selectionService = (ISelectionService)GetService(typeof(ISelectionService));
         bool childrenAreSelected = false;
         if (selectionService != null)
         {
            ICollection selectedComponents = selectionService.GetSelectedComponents();

            foreach (object obj in selectedComponents)
            {
               if (obj is Control && ((Control)obj).Parent == this)
               {
                  childrenAreSelected = true;
                  break;
               }
            }
         }
         return childrenAreSelected;
      }
      private void RefreshChildrensGlyphs()
      {

         bool childrenAreSelected = AreChildrenSelected();
         if (childrenAreSelected)
         {
            object selectionManager = this.GetService(ReflecionDesignHelper.GetType("System.Windows.Forms.Design.Behavior.SelectionManager", ReflecionDesignHelper.DesignAssembly));
            if (selectionManager != null)
               ReflecionDesignHelper.InvokeMethod(selectionManager, "Refresh");
         }
      }

#endif     

      /// <summary>
      /// handle horizontal scroll
      /// </summary>
      /// <param name="m"></param>
      private void OnHScroll(ref Message m)
      {
         int newValue = int.MinValue;
         NativeScroll.SCROLLINFO sc = new NativeScroll.SCROLLINFO();

         int nScrollCode = NativeWindowCommon.LoWord((int)m.WParam);
         sc = ScrollInfo(NativeScroll.SB_HORZ);
         int value = sc.nPos;
         int maxValue = sc.nMax;

         switch (nScrollCode)
         {
            case NativeScroll.SB_LEFT:
               newValue = 0;
               break;
            case NativeScroll.SB_RIGHT:
               newValue = maxValue;
               break;
            case NativeScroll.SB_LINELEFT:
               newValue = value - 1;
               break;
            case NativeScroll.SB_LINERIGHT:
               if (value + 1 <= maxValue)
                  newValue = value + 1;
               break;
            case NativeScroll.SB_PAGELEFT:
               newValue = value - sc.nPage;
               break;
            case NativeScroll.SB_PAGERIGHT:
               newValue = value + sc.nPage;
               break;
            case NativeScroll.SB_THUMBTRACK:
               newValue = sc.nTrackPos;
               break;
         }
         //Sometimes we get here even if no horizontal scrollbar exsits
         //happens during load of RTL table in new studio designer
         //we should not Scroll if we do not have horizontal scollbar
         if (newValue != int.MinValue && isHorizontalScrollBarVisible)
         {
            ScrollEventArgs arg = new ScrollEventArgs((ScrollEventType)m.WParam, value, newValue, ScrollOrientation.HorizontalScroll);
            this.OnScroll(arg);
            ScrollHorizontally(value, newValue);
            Invalidate();
#if !PocketPC
            if (this.DesignMode)
               RefreshChildrensGlyphs();
#endif

         }
         return;
      }

    
      /// <summary>
      /// perform horizontal scroll
      /// </summary>
      /// <param name="value"></param>
      /// <param name="newValue"></param>
      private void ScrollHorizontally(int value, int newValue)
      {
         NativeScroll.SCROLLINFO sc = ScrollInfo(NativeScroll.SB_HORZ);
         int maxValue = sc.nMax - sc.nPage;
         if (newValue < 0)
            newValue = 0;
         if (newValue > maxValue)
            newValue = maxValue;
         NativeScroll.SetScrollPos(this.Handle, NativeScroll.SB_HORZ, newValue, true);
         NativeWindowCommon.RECT rc = new NativeWindowCommon.RECT();
         NativeScroll.ScrollWindowEx(this.Handle, value - newValue, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                                     ref rc, NativeScroll.SW_SCROLLCHILDREN | NativeScroll.SW_INVALIDATE | NativeScroll.SW_ERASE);
         _corner.X = getXCorner();
      }

      /// <summary>
      /// is horizontal scroll shown
      /// </summary>
      /// <returns></returns>
      public bool isHscrollShown()
      {
         return _prevLogWidth > ClientSize.Width;
      }

      /// <summary>
      /// scroll info for scrollbar
      /// </summary>
      /// <param name="fnBar"></param>
      /// <returns></returns>
      protected NativeScroll.SCROLLINFO ScrollInfo(int fnBar)
      {
         return Utils.ScrollInfo(this, fnBar);
      }

      /// <summary>
      /// scroll column into view
      /// </summary>
      /// <param name="column"></param>
      public void showColumn(TableColumn column)
      {
         int colStart = 0;
         int dx = 0;
         for (int i = 0; i < column.Index; i++)
            colStart += _columns[i].Width;

         if (rightToLeftLayout)
            colStart = mirrorXcoordinate(colStart);

         int width = Math.Min(column.Width, ClientSize.Width);
         int colLeft = (rightToLeftLayout ? colStart - width : colStart);
         int colRight = (rightToLeftLayout ? colStart : colStart + width);

         NativeScroll.SCROLLINFO sc = ScrollInfo(NativeScroll.SB_HORZ);
         if (colLeft < sc.nPos)
            dx = colLeft - sc.nPos;
         else
         {
            if (colRight > sc.nPos + ClientSize.Width)
               dx = colRight - (sc.nPos + ClientSize.Width);
         }
         if (dx != 0)
            ScrollHorizontally(sc.nPos, sc.nPos + dx);
      }

      /// <summary>
      /// initialize horizontal scroll
      /// </summary>
      private void initHScroll()
      {
         NativeScroll.SCROLLINFO sc = ScrollInfo(NativeScroll.SB_HORZ);
         sc.nMax = sc.nMin = sc.nPos = 0;
         sc.nPage = 0;
         sc.fMask = NativeScroll.SIF_RANGE | NativeScroll.SIF_POS | NativeScroll.SIF_PAGE;
         NativeScroll.SetScrollInfo(this.Handle, NativeScroll.SB_HORZ, ref sc, false);
      }

      /// <summary>
      /// this causes paint for the table scrollbar area after scrollbar disappear while using Windows Themes
      /// </summary>
      protected void HideVerticalScrollbarAnimationForWindowsThemes()
      {
         if (NativeWindowCommon.IsThemeActive())
         {
            NativeScroll.SCROLLINFO sc = ScrollInfo(NativeScroll.SB_VERT); //force table to stop dragging when we hide scrollbar
            ScrollEventArgs arg = new ScrollEventArgs(ScrollEventType.EndScroll, sc.nPos, sc.nPos, ScrollOrientation.VerticalScroll);
            this.OnScroll(arg);

            SuspendLayout();
            sc.nPos = 0;
            sc.nMin = 0;
            //For some reason, we need the following code
            // a. While dragging thumb: As in case of Defect #115454. After the scrollbar is displayed, try dragging the thumbbar upwards. 
            //    It remains visible even after the scrollbar is hidden. The problem is fixed if nMax and nPage are set to 0.
            // b. While clicking on the scrollbar just above the thumb so that scroll will be hidden (as in case of defect #115331, RIA task)
            //    After hiding vertical scrollbar if column width is increased to display horizontal scrollbar and then the thumb on horizontal 
            //    scroll bar is clicked, some area above horizontal scroll bar is grayed. The problem is fixed if nMax and nPage are set to 1.
            if (_isThumbDrag)
               sc.nMax = sc.nPage = 0;
            else
               sc.nMax = sc.nPage = 1;
            sc.fMask = NativeScroll.SIF_ALL;
            NativeScroll.SetScrollInfo(this.Handle, NativeScroll.SB_VERT, ref sc, true);

            NativeScroll.ShowScrollBar(this.Handle, NativeScroll.SB_VERT, false);

            isVerticalScrollBarVisible = false;
            ResumeLayout();
            NativeWindowCommon.RedrawWindow(this.Handle, IntPtr.Zero, IntPtr.Zero, NativeWindowCommon.RedrawWindowFlags.RDW_FRAME |
                                          NativeWindowCommon.RedrawWindowFlags.RDW_INVALIDATE);
            this._header.Refresh();
         }
         else
            NativeScroll.ShowScrollBar(this.Handle, NativeScroll.SB_VERT, false);
      }
   }
}