////////////////////////////////////////////////////////////////////////////////////
//  File:   Header.cs
//  Author: Sergei Pavlovsky
//
//  Copyright (c) 2004 by Sergei Pavlovsky (sergei_vp@hotmail.com, sergei_vp@ukr.net)
//
//	This file is provided "as is" with no expressed or implied warranty.
//	The author accepts no liability if it causes any damage whatsoever.
// 
//  This code is free and may be used in any way you desire. If the source code in 
//  this file is used in any commercial application then a simple email would be 
//	nice.
//
//  Revisions: 
//    06/24/2004 - Bugfix. Control was flickering on resizing.
//
////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Security.Permissions;
using System.Text;
using System.Windows.Forms;
using System.Collections.Generic;
using com.magicsoftware.win32;
using com.magicsoftware.controls.utils;
using com.magicsoftware.util;
#if PocketPC
using RightToLeft = com.magicsoftware.mobilestubs.RightToLeft;
using LeftRightAlignment = com.magicsoftware.mobilestubs.LeftRightAlignment;
using HandleRef = com.magicsoftware.mobilestubs.HandleRef;
using ArgumentOutOfRangeException = com.magicsoftware.mobilestubs.MgArgumentOutOfRangeException;
using ContentAlignment = OpenNETCF.Drawing.ContentAlignment2;
using ColorTranslator = OpenNETCF.Drawing.ColorTranslator;
using Control = OpenNETCF.Windows.Forms.Control2;
using CreateParams = OpenNETCF.Windows.Forms.CreateParams;
using Message = Microsoft.WindowsCE.Forms.Message;
#endif

namespace com.magicsoftware.controls
{
   #region Header control

   /// <summary>
   /// Header class.
   /// </summary>
#if !PocketPC
   [
      Description("SP Header Control"),
      DefaultProperty("Sections"),
      DefaultEvent("AfterSectionTrack"),
      Designer(typeof(HeaderDesigner)),
      SecurityPermission(SecurityAction.LinkDemand,
                     Flags = SecurityPermissionFlag.UnmanagedCode),
      //ToolboxBitmap(@"C:\gui-examples\header1\SpHeader\table2.ico")
      ToolboxBitmap("Header.bmp")


   ]
#endif
   public class Header : Control
   {
#if PocketPC
      public RightToLeft RightToLeft { get; set; }
      public bool IsHandleCreated { get { return !childHandle.Equals(IntPtr.Zero); } }
      public IntPtr HeaderHandle { get { return childHandle; } }
      // Control height changes do not change the height of the HWND - we need to set it right.
      public new int Height
      {
         get { return base.Height; }
         set
         {
            if (base.Height != value)
            {
               base.Height = value;
               NativeWindowCommon.RECT rc = new NativeWindowCommon.RECT();
               NativeWindowCommon.GetWindowRect(childHandle, out rc);
               NativeWindowCommon.SetWindowPos(childHandle, IntPtr.Zero, rc.left, rc.top, rc.right - rc.left, value,
                  NativeWindowCommon.SWP_NOMOVE | NativeWindowCommon.SWP_NOZORDER);
            }
         }
      }
      public new int Width
      {
         get { return base.Width; }
         set
         {
            if (base.Width != value)
            {
               base.Width = value;
               NativeWindowCommon.RECT rc = new NativeWindowCommon.RECT();
               NativeWindowCommon.GetWindowRect(childHandle, out rc);
               NativeWindowCommon.SetWindowPos(childHandle, IntPtr.Zero, rc.left, rc.top, value, rc.bottom - rc.top,
                  NativeWindowCommon.SWP_NOMOVE | NativeWindowCommon.SWP_NOZORDER);
            }
         }
      }
#else
      public IntPtr HeaderHandle { get { return Handle; } }
#endif
      private Color titleColor = Color.Empty;
 
      public Color TitleColor
      {
         get { return titleColor; }
         set
         {
            titleColor = value;
            if (HeaderRenderer != null)
               HeaderRenderer.UpdateTitleBrush();
            SetDividerHeight();
            SetFilterHeight();
            Refresh();

         }
      }



      /// <summary>
      /// Column divider color
      /// </summary>
      private Color dividerColor = Color.Empty;

      public Color DividerColor
      {
         get { return dividerColor; }
         set
         {
            dividerColor = value;

            SetDividerHeight();
            SetDividerHeight();
            Refresh();
         }
      }

      private bool tableColumnDivider;

      /// <summary>
      /// Holds if table has column divider or not
      /// </summary>
      public bool TableColumnDivider
      {
         get { return tableColumnDivider; }
         set
         {
            tableColumnDivider = value;
            SetDividerHeight();
         }
      }

      private bool tableLineDivider;
      /// <summary>
      /// Holds if table has line divider
      /// </summary>
      public bool TableLineDivider
      {
         get { return tableLineDivider; }
         set
         {
            tableLineDivider = value;
            SetFilterHeight();
         }
      }

      /// <summary>
      /// Types
      /// </summary>

      [Flags]
      public enum HitTestArea : int
      {
         NoWhere = NativeHeader.HHT_NOWHERE,
         OnHeader = NativeHeader.HHT_ONHEADER,
         OnDivider = NativeHeader.HHT_ONDIVIDER,
         OnDividerOpen = NativeHeader.HHT_ONDIVOPEN,
         OnFilter = NativeHeader.HHT_ONFILTER,
         OnFilterButton = NativeHeader.HHT_ONFILTERBUTTON,
         Above = NativeHeader.HHT_ABOVE,
         Below = NativeHeader.HHT_BELOW,
         ToLeft = NativeHeader.HHT_TOLEFT,
         ToRight = NativeHeader.HHT_TORIGHT
      }

      public struct HitTestInfo
      {
         public HitTestArea fArea;
         public HeaderSection section;
      }

      /// <summary>
      /// Data Fields
      /// </summary>

      private int fStyle = NativeHeader.WS_CHILD;

      // Clickable
      [
#if !PocketPC
Category("Behavior"),
Description("Determines if control will generate events " +
         "when user clicks on its column titles."),
#endif
 DefaultValue(false)
]
      public bool Clickable
      {
         get { return (this.fStyle & NativeHeader.HDS_BUTTONS) != 0; }
         set
         {
            bool bOldValue = (this.fStyle & NativeHeader.HDS_BUTTONS) != 0;

            if (value != bOldValue)
            {
               if (value)
                  this.fStyle |= NativeHeader.HDS_BUTTONS;
               else
                  this.fStyle &= (~NativeHeader.HDS_BUTTONS);

               if (this.IsHandleCreated)
               {
                  UpdateWndStyle();
               }
            }
         }
      }

      // HotTrack
      [
#if !PocketPC
Category("Behavior"),
Description("Enables or disables hot tracking."),
#endif
 DefaultValue(false)
]
      public bool HotTrack
      {
         get { return (this.fStyle & NativeHeader.HDS_HOTTRACK) != 0; }
         set
         {
            bool bOldValue = (this.fStyle & NativeHeader.HDS_HOTTRACK) != 0;

            if (value != bOldValue)
            {
               if (value)
                  this.fStyle |= NativeHeader.HDS_HOTTRACK;
               else
                  this.fStyle &= (~NativeHeader.HDS_HOTTRACK);

               if (this.IsHandleCreated)
               {
                  UpdateWndStyle();
               }
            }
         }
      }

      // Flat
      [
#if !PocketPC
Category("Appearance"),
Description("Causes the header control to be drawn flat when " +
         "Microsoft® Windows® XP is running in classic mode."),
#endif
 DefaultValue(false)
]
      public bool Flat
      {
         get { return (this.fStyle & NativeHeader.HDS_FLAT) != 0; }
         set
         {
            bool bOldValue = (this.fStyle & NativeHeader.HDS_FLAT) != 0;

            if (value != bOldValue)
            {
               if (value)
                  this.fStyle |= NativeHeader.HDS_FLAT;
               else
                  this.fStyle &= (~NativeHeader.HDS_FLAT);

               if (this.IsHandleCreated)
               {
                  UpdateWndStyle();
               }
            }
         }
      }

      // AllowDragSections
      [
#if !PocketPC
Category("Behavior"),
Description("Determines if user will be able to drag header column " +
         "on another position."),
#endif
 DefaultValue(false)
]
      public bool AllowDragSections
      {
         get { return (this.fStyle & NativeHeader.HDS_DRAGDROP) != 0; }
         set
         {
            bool bOldValue = (this.fStyle & NativeHeader.HDS_DRAGDROP) != 0;

            if (value != bOldValue)
            {
               if (value)
                  this.fStyle |= NativeHeader.HDS_DRAGDROP;
               else
                  this.fStyle &= (~NativeHeader.HDS_DRAGDROP);

               if (this.IsHandleCreated)
               {
                  UpdateWndStyle();
               }
            }
         }
      }



      [
#if !PocketPC
Category("Behavior"),
Description("Determines if user will be able to resize header column "),
#endif
 DefaultValue(false)
]
      public bool AllowColumnsResize
      {
         get { return allowColumnsResize; }
         set { allowColumnsResize = value; }
      }

      bool allowColumnsResize = true;



      //public override RightToLeft RightToLeft
      //{
      //    get
      //    {
      //        return base.RightToLeft;
      //    }
      //    set
      //    {
      //        if (base.RightToLeft != value)
      //        {
      //            int fStyle = NativeHeader.GetWindowLong(Handle, NativeHeader.GWL_EXSTYLE);
      //            if (value == RightToLeft.Yes)
      //                fStyle |= NativeHeader.WS_EX_LAYOUTRTL | NativeHeader.WS_EX_NOINHERITLAYOUT;
      //            else
      //                fStyle &= ~(NativeHeader.WS_EX_LAYOUTRTL | NativeHeader.WS_EX_NOINHERITLAYOUT);

      //            NativeHeader.SetWindowLong(Handle, NativeHeader.GWL_EXSTYLE, fStyle);
      //        }
      //        base.RightToLeft = value;

      //    }
      //}

      // FullDragSections
      [
#if !PocketPC
Category("Behavior"),
Description("Causes the header control to display column contents " +
         "even while the user resizes a column."),
#endif
 DefaultValue(false)
]
      public bool FullDragSections
      {
         get { return (this.fStyle & NativeHeader.HDS_FULLDRAG) != 0; }
         set
         {
            bool bOldValue = (this.fStyle & NativeHeader.HDS_FULLDRAG) != 0;

            if (value != bOldValue)
            {
               if (value)
                  this.fStyle |= NativeHeader.HDS_FULLDRAG;
               else
                  this.fStyle &= (~NativeHeader.HDS_FULLDRAG);

               if (this.IsHandleCreated)
               {
                  UpdateWndStyle();
               }
            }
         }
      }

      // Sections
      private HeaderSectionCollection colSections = null;

#if !PocketPC
      [
         Category("Data"),
         Description("Sections of the header."),
         MergableProperty(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Content),
         Localizable(true)
      ]
#endif
      public HeaderSectionCollection Sections
      {
         get { return this.colSections; }
      }

      private ImageList imageList = null;
      [
#if !PocketPC
Category("Data"),
Description("Images for header's sections."),
#endif
 DefaultValue(null)
]
      public ImageList ImageList
      {
         get { return this.imageList; }
         set
         {
            if (this.imageList != value)
            {
               EventHandler ehRecreateHandle =
                  new EventHandler(this.OnImageListRecreateHandle);

               EventHandler ehDetachImageList =
                  new EventHandler(this.OnDetachImageList);

               if (this.imageList != null)
               {
#if !PocketPC
                  this.imageList.RecreateHandle -= ehRecreateHandle;
#endif
                  this.imageList.Disposed -= ehDetachImageList;
               }

               this.imageList = value;

               if (this.imageList != null)
               {
#if !PocketPC
                  this.imageList.RecreateHandle += ehRecreateHandle;
#endif
                  this.imageList.Disposed += ehDetachImageList;
               }

               if (IsHandleCreated)
               {
                  HandleRef hrThis = new HandleRef(this, this.HeaderHandle);

                  UpdateWndImageList(ref hrThis, this.imageList);
               }
            }
         }
      }

      bool showDividers = true;

      /// <summary>
      /// true, if we want to show deviders on the header
      /// False in case we have no line dividers and the header background is not system color. Is this case, the bottom line will be colored and the background color.
      /// </summary>
      public bool ShowDividers
      {
         get { return showDividers; }
         set { showDividers = value; }
      }

      private bool showBottomBorder = true;
      /// <summary>
      /// true, if we want to show bottom line of the header
      /// </summary>
      public bool ShowBottomBorder
      {
         get
         {
            return TitleColor != Color.Empty || DividerColor != Color.Empty;
         }
         set { showBottomBorder = value; }
      }

#if !PocketPC
      private int cxBitmapMargin = SystemInformation.Border3DSize.Width;
#else
      private int cxBitmapMargin = SystemInformation.BorderSize.Width;
#endif
      [
#if !PocketPC
Category("Appearance"),
Description("Width of the margin that surrounds a bitmap " +
         "within an existing header control."),
#endif
 DefaultValue(2)
]
      public int BitmapMargin
      {
         get { return this.cxBitmapMargin; }
         set
         {
            if (this.cxBitmapMargin != value)
            {
               if (value < 0)
                  throw new ArgumentOutOfRangeException("value", value, ErrMsg.NegVal());

               this.cxBitmapMargin = value;

               if (IsHandleCreated)
               {
                  HandleRef hrThis = new HandleRef(this, this.HeaderHandle);

                  UpdateWndBitmapMargin(ref hrThis, this.cxBitmapMargin);
               }
            }
         }
      }
      internal IntPtr SortPen;
      internal IntPtr WhitePen;
      internal IntPtr ButtonShadowPen;
      internal IntPtr SortBrush;
      internal IntPtr HeaderItemBorderPen;
      internal IntPtr FilterBrush;
      internal IntPtr FilterPen;
      internal IntPtr HighlightBrush;
      internal const int SORT_ICON_LEFT_RIGHT_MARGIN = 3;
      private bool rightToLeftLayout = false; //RTL control

      internal HeaderRenderer HeaderRenderer { get; private set; }

      /// <summary>
      /// RTL support
      /// </summary>
      public bool RightToLeftLayout
      {
         get { return rightToLeftLayout; }
         set
         {
            rightToLeftLayout = value;
#if !PocketPC
            RecreateHandle();
#endif
            Invalidate();
         }
      }

      public bool AddEndEllipsesFlag { get; set; }

      protected bool SupportMultilineText { get; set; }

      public bool ShowLastDivider { get; set; } = true;

      /// <summary>
      /// Construction & finalization
      /// </summary>
      public Header()
         : base()
      {
#if !PocketPC
         this.SetStyle(ControlStyles.UserPaint, false);
         //this.SetStyle(ControlStyles.UserMouse, false); // ???
         this.SetStyle(ControlStyles.StandardClick, false);
         this.SetStyle(ControlStyles.StandardDoubleClick, false);
         this.SetStyle(ControlStyles.Opaque, true);
         this.SetStyle(ControlStyles.ResizeRedraw, this.DesignMode);
         this.SetStyle(ControlStyles.Selectable, false);
#endif

         //this.SetStyle(ControlStyles.AllPaintingInWmPaint, false); // ???

         this.colSections = new HeaderSectionCollection(this);

         SortBrush = NativeWindowCommon.CreateSolidBrush(ColorTranslator.ToWin32(SystemColors.ControlDark));
         SortPen = NativeWindowCommon.CreatePen(NativeWindowCommon.PS_SOLID, 1, ColorTranslator.ToWin32(SystemColors.ControlDark));
         HeaderItemBorderPen = NativeWindowCommon.CreatePen(NativeWindowCommon.PS_SOLID, 1, ColorTranslator.ToWin32(SystemColors.ControlLight));
         ButtonShadowPen = NativeWindowCommon.CreatePen(NativeWindowCommon.PS_SOLID, 1, ColorTranslator.ToWin32(Color.White));
         WhitePen = NativeWindowCommon.CreatePen(NativeWindowCommon.PS_SOLID, 1, ColorTranslator.ToWin32(SystemColors.ControlDark));
         FilterPen = NativeWindowCommon.CreatePen(NativeWindowCommon.PS_SOLID, 1, ColorTranslator.ToWin32(Color.Black));
         FilterBrush = NativeWindowCommon.CreateSolidBrush(ColorTranslator.ToWin32(Color.Black));
         UpdateFlags();
      }

      /// <summary>
      /// update the renderer
      /// </summary>
      /// <param name="tableControl"></param>
      public void UpdateRenderer(TableControl tableControl)
      {
         if (HeaderRenderer != null)
            HeaderRenderer.Dispose();
         HeaderRenderer = HeaderRendererFactory.GetRenderer(tableControl, this);
         SetDividerHeight();
         UpdateDividerPen();
         UpdateTitleBrush();
      }
      
      /// <summary>
      /// update divider pen
      /// </summary>
      public void UpdateDividerPen()
      {
         HeaderRenderer.UpdateDividerPen();
         Invalidate();
      }

      /// <summary>
      /// update divider pen
      /// </summary>
      public void UpdateTitleBrush()
      {
         HeaderRenderer.UpdateTitleBrush();
         Invalidate();
      }

      public void Init()
      {
         // Code from OnHandleCreate
         HandleRef hrThis = new HandleRef(this, this.HeaderHandle);

         // Set Window Style
         UpdateWndStyle(ref hrThis, this.fStyle);

         // Set Bitmap Margin
         UpdateWndBitmapMargin(ref hrThis, this.cxBitmapMargin);

         // Set ImageList
         UpdateWndImageList(ref hrThis, this.imageList);


         // Add items
         for (int i = 0; i < this.colSections.Count; i++)
         {
            HeaderSection item = colSections[i];

            NativeHeader.HDITEM hdi;
            item.ComposeNativeData(i, out hdi);

            int nResult = NativeHeader.InsertItem(this.HeaderHandle, i, ref hdi);
            Debug.Assert(nResult >= 0);
            if (nResult < 0)
               throw new InvalidOperationException(ErrMsg.FailedToInsertItem(),
                                          new Win32Exception());
         }
      }

      protected virtual void UpdateFlags()
      {
         SupportMultilineText = true;
         AddEndEllipsesFlag = Environment.OSVersion.Platform != PlatformID.WinCE;
      }

      /// <summary> set order
      /// 
      /// </summary>
      /// <param name="order"></param>
      public void sortOrder(int[] order)
      {
         bool bResult = NativeHeader.SetOrderArray(this.HeaderHandle, order);

         Debug.Assert(bResult);
         if (!bResult)
            throw new InvalidOperationException(ErrMsg.FailedToInsertItem(),
                                       new Win32Exception());
      }

      /// <summary>
      /// Clean up any resources being used.
      /// </summary>

      protected override void Dispose(bool disposing)
      {
         if (disposing)
         {
            if (this.imageList != null)
            {
#if !PocketPC
               this.imageList.RecreateHandle -=
                  new EventHandler(this.OnImageListRecreateHandle);
#endif

               this.imageList.Disposed -=
                  new EventHandler(this.OnDetachImageList);

               this.imageList = null;
            }

            this.colSections.Clear(true);
         }

         if (HeaderRenderer != null)
            HeaderRenderer.Dispose();

         NativeWindowCommon.DeleteObject(SortBrush);
         NativeWindowCommon.DeleteObject(HeaderItemBorderPen);
         NativeWindowCommon.DeleteObject(SortPen);
         NativeWindowCommon.DeleteObject(WhitePen);
         NativeWindowCommon.DeleteObject(ButtonShadowPen);
         NativeWindowCommon.DeleteObject(FilterPen);
         NativeWindowCommon.DeleteObject(FilterBrush);

         base.Dispose(disposing);
      }

      /// <summary>
      /// Helpers
      /// </summary>

      private int ExtractIndexFromNMHEADER(ref NativeHeader.NMHEADER nmh)
      {
         return nmh.iItem;
      }

      private MouseButtons ExtractMouseButtonFromNMHEADER(ref NativeHeader.NMHEADER nmh)
      {
         switch (nmh.iButton)
         {
            case 0:
               return MouseButtons.Left;

            case 1:
               return MouseButtons.Right;

            case 2:
               return MouseButtons.Middle;
         }

         return MouseButtons.None;
      }

      private void ExtractSectionDataFromNMHEADER(ref NativeHeader.NMHEADER nmh,
                                       ref NativeHeader.HDITEM2 item)
      {
         if (nmh.pitem != IntPtr.Zero)
         {
            item = (NativeHeader.HDITEM2)Marshal.PtrToStructure(
                                    nmh.pitem, typeof(NativeHeader.HDITEM2));
         }
      }

      private static void UpdateWndImageList(ref HandleRef hrThis, ImageList imageList)
      {
#if !PocketPC
         Debug.Assert(hrThis.Handle != IntPtr.Zero);

         IntPtr hIL = (imageList != null) ? imageList.Handle : IntPtr.Zero;

         NativeHeader.SetImageList(hrThis.Handle, new HandleRef(imageList, hIL).Handle);
#else
         if(imageList != null)
            throw new NotImplementedException();
#endif
      }

      private static void UpdateWndBitmapMargin(ref HandleRef hrThis, int cxMargin)
      {
         Debug.Assert(hrThis.Handle != IntPtr.Zero);

         NativeHeader.SetBitmapMargin(hrThis.Handle, cxMargin);
      }

      private static void UpdateWndStyle(ref HandleRef hrThis, int fNewStyle)
      {
         Debug.Assert(hrThis.Handle != IntPtr.Zero);

         const int fOptions = NativeHeader.SWP_NOSIZE |
                         NativeHeader.SWP_NOMOVE |
                         NativeHeader.SWP_NOZORDER |
                         NativeHeader.SWP_NOACTIVATE |
                         NativeHeader.SWP_FRAMECHANGED;

         int fStyle = NativeHeader.GetWindowLong(hrThis.Handle, NativeHeader.GWL_STYLE);
         fStyle &= ~(NativeHeader.HDS_BUTTONS |
                  NativeHeader.HDS_HOTTRACK |
                  NativeHeader.HDS_FLAT |
                  NativeHeader.HDS_DRAGDROP |
                  NativeHeader.HDS_FULLDRAG);

         fStyle |= fNewStyle;

         NativeHeader.SetWindowLong(hrThis.Handle, NativeHeader.GWL_STYLE, fStyle);

         NativeHeader.SetWindowPos(hrThis.Handle, IntPtr.Zero, 0, 0, 0, 0, fOptions);
      }

      private void UpdateWndStyle()
      {
         HandleRef hrThis = new HandleRef(this, this.HeaderHandle);

         UpdateWndStyle(ref hrThis, this.fStyle);
      }

      /// <summary>
      /// Internal notifications
      /// </summary>

#if !PocketPC
      protected override void OnHandleCreated(EventArgs ea)
      {
         Init();
         base.OnHandleCreated(ea);
      }
#endif

      protected override void OnHandleDestroyed(EventArgs ea)
      {
         // Collect item parameters from native window

         base.OnHandleDestroyed(ea);
      }

      protected override void OnEnabledChanged(EventArgs ea)
      {
         base.OnEnabledChanged(ea);
      }

#if !PocketPC
      protected override void OnFontChanged(EventArgs ea)
      {
         base.OnFontChanged(ea);
      }
#endif

      internal void _OnSectionInserted(int index, HeaderSection item)
      {
         if (this.IsHandleCreated)
         {
            NativeHeader.HDITEM hdi;
            hdi.lpszText = null;

            item.ComposeNativeData(index, out hdi);

            int iResult = NativeHeader.InsertItem(new HandleRef(this, this.HeaderHandle).Handle,
                                         index, ref hdi);
            Debug.Assert(iResult == index);
            if (iResult < 0)
               throw new InvalidOperationException(ErrMsg.FailedToInsertItem(),
                                          new Win32Exception());
         }
      }

      internal void _OnSectionRemoved(int iRawIndex, HeaderSection item)
      {
         if (this.IsHandleCreated)
         {
            HandleRef hrThis = new HandleRef(this, this.HeaderHandle);

            bool bResult = NativeHeader.DeleteItem(hrThis.Handle, iRawIndex);
            Debug.Assert(bResult);
            if (!bResult)
            {
               throw new InvalidOperationException(ErrMsg.FailedToRemoveItem(),
                                          new Win32Exception());
            }
         }
      }

      internal void _OnAllSectionsRemoved()
      {
         if (this.IsHandleCreated)
         {
            BeginUpdate();

            try
            {
               HandleRef hrThis = new HandleRef(this, this.HeaderHandle);

               while (NativeHeader.GetItemCount(this.HeaderHandle) != 0)
               {
                  bool bResult = NativeHeader.DeleteItem(hrThis.Handle, 0);
                  Debug.Assert(bResult);
                  if (!bResult)
                  {
                     throw new InvalidOperationException(ErrMsg.FailedToRemoveItem(),
                                                new Win32Exception());
                  }

               }
            }
            finally
            {
               EndUpdate();
            }
         }
      }

      internal void _OnSectionChanged(int iRawIndex, HeaderSection item)
      {
         if (this.IsHandleCreated)
         {
            NativeHeader.HDITEM hdi;
            item.ComposeNativeData(-1, out hdi);

            HandleRef hrThis = new HandleRef(this, this.HeaderHandle);

            bool bResult = NativeHeader.SetItem(hrThis.Handle, iRawIndex, ref hdi);
            Debug.Assert(bResult);
            if (!bResult)
            {
               throw new InvalidOperationException(ErrMsg.FailedToChangeItem(),
                                          new Win32Exception());
            }
         }
      }

      internal void _OnSectionWidthChanged(HeaderSection item)
      {
         if (this.IsHandleCreated)
         {
            int iSection = this.colSections._FindSectionRawIndex(item);
            Debug.Assert(iSection >= 0);

            NativeHeader.HDITEM hdi = new NativeHeader.HDITEM();
            hdi.mask = NativeHeader.HDI_WIDTH;
            hdi.cxy = item.Width;

            HandleRef hrThis = new HandleRef(this, this.HeaderHandle);

            bool bResult = NativeHeader.SetItem(hrThis.Handle, iSection, ref hdi);
            Debug.Assert(bResult);
            if (!bResult)
            {
               throw new InvalidOperationException(ErrMsg.FailedToChangeItem(),
                                          new Win32Exception());
            }
         }
      }

      internal void _OnSectionTextChanged(HeaderSection item)
      {
         if (this.IsHandleCreated)
         {
            /*int iSection = this.colSections._FindSectionRawIndex(item);
            Debug.Assert(iSection >= 0);

            NativeHeader.HDITEM hdi = new NativeHeader.HDITEM();
            hdi.mask = NativeHeader.HDI_FORMAT | NativeHeader.HDI_TEXT;
            hdi.fmt = item.Format;
            hdi.lpszText = item.Text;

            HandleRef hrThis = new HandleRef(this, this.Handle);

            bool bResult = NativeHeader.SetItem(hrThis.Handle, iSection, ref hdi);
            Debug.Assert(bResult);
            if (!bResult)
            {
               throw new InvalidOperationException(ErrMsg.FailedToChangeItem(),
                                          new Win32Exception());
            }*/

         }
      }

      internal void _OnSectionImageIndexChanged(HeaderSection item)
      {
         if (this.IsHandleCreated)
         {
            int iSection = this.colSections._FindSectionRawIndex(item);
            Debug.Assert(iSection >= 0);

            NativeHeader.HDITEM hdi = new NativeHeader.HDITEM();
            hdi.mask = NativeHeader.HDI_FORMAT | NativeHeader.HDI_IMAGE;
            hdi.fmt = item.Format;
            hdi.iImage = item.ImageIndex;

            HandleRef hrThis = new HandleRef(this, this.HeaderHandle);

            bool bResult = NativeHeader.SetItem(hrThis.Handle, iSection, ref hdi);
            Debug.Assert(bResult);
            if (!bResult)
            {
               throw new InvalidOperationException(ErrMsg.FailedToChangeItem(),
                                          new Win32Exception());
            }
         }
      }

      internal void _OnSectionBitmapChanged(HeaderSection item)
      {
         if (this.IsHandleCreated)
         {
            int iSection = this.colSections._FindSectionRawIndex(item);
            Debug.Assert(iSection >= 0);

            NativeHeader.HDITEM hdi = new NativeHeader.HDITEM();
            hdi.mask = NativeHeader.HDI_FORMAT | NativeHeader.HDI_BITMAP;
            hdi.fmt = item.Format;
            hdi.hbm = item._GetHBitmap();

            HandleRef hrThis = new HandleRef(this, this.HeaderHandle);

            bool bResult = NativeHeader.SetItem(hrThis.Handle, iSection, ref hdi);
            Debug.Assert(bResult);
            if (!bResult)
            {
               throw new InvalidOperationException(ErrMsg.FailedToChangeItem(),
                                          new Win32Exception());
            }
         }
      }

      internal void _OnSectionRightToLeftChanged(HeaderSection item)
      {
         if (this.IsHandleCreated)
         {
            int iSection = this.colSections._FindSectionRawIndex(item);
            Debug.Assert(iSection >= 0);

            NativeHeader.HDITEM hdi = new NativeHeader.HDITEM();
            hdi.mask = NativeHeader.HDI_FORMAT;
            hdi.fmt = item.Format;

            HandleRef hrThis = new HandleRef(this, this.HeaderHandle);

            bool bResult = NativeHeader.SetItem(hrThis.Handle, iSection, ref hdi);
            Debug.Assert(bResult);
            if (!bResult)
            {
               throw new InvalidOperationException(ErrMsg.FailedToChangeItem(),
                                          new Win32Exception());
            }
         }
      }

      internal void _OnSectionContentAlignChanged(HeaderSection item)
      {
         if (this.IsHandleCreated)
         {
            int iSection = this.colSections._FindSectionRawIndex(item);
            Debug.Assert(iSection >= 0);

            NativeHeader.HDITEM hdi = new NativeHeader.HDITEM();
            hdi.mask = NativeHeader.HDI_FORMAT;
            hdi.fmt = item.Format;

            HandleRef hrThis = new HandleRef(this, this.HeaderHandle);

            bool bResult = NativeHeader.SetItem(hrThis.Handle, iSection, ref hdi);
            Debug.Assert(bResult);
            if (!bResult)
            {
               throw new InvalidOperationException(ErrMsg.FailedToChangeItem(),
                                          new Win32Exception());
            }
         }
      }

      internal void _OnSectionImageAlignChanged(HeaderSection item)
      {
         if (this.IsHandleCreated)
         {
            int iSection = this.colSections._FindSectionRawIndex(item);
            Debug.Assert(iSection >= 0);

            NativeHeader.HDITEM hdi = new NativeHeader.HDITEM();
            hdi.mask = NativeHeader.HDI_FORMAT;
            hdi.fmt = item.Format;

            HandleRef hrThis = new HandleRef(this, this.HeaderHandle);

            bool bResult = NativeHeader.SetItem(hrThis.Handle, iSection, ref hdi);
            Debug.Assert(bResult);
            if (!bResult)
            {
               throw new InvalidOperationException(ErrMsg.FailedToChangeItem(),
                                          new Win32Exception());
            }
         }
      }

      internal void _OnSectionSortMarkChanged(HeaderSection item)
      {
         if (this.IsHandleCreated)
         {
            int iSection = this.colSections._FindSectionRawIndex(item);
            Debug.Assert(iSection >= 0);

            NativeHeader.HDITEM hdi = new NativeHeader.HDITEM();
            hdi.mask = NativeHeader.HDI_FORMAT;
            hdi.fmt = item.Format;

            HandleRef hrThis = new HandleRef(this, this.HeaderHandle);

            bool bResult = NativeHeader.SetItem(hrThis.Handle, iSection, ref hdi);
            Debug.Assert(bResult);
            if (!bResult)
            {
               throw new InvalidOperationException(ErrMsg.FailedToChangeItem(),
                                          new Win32Exception());
            }
         }
      }

      private void OnImageListRecreateHandle(object sender, EventArgs ea)
      {
         if (IsHandleCreated)
         {
            HandleRef hrThis = new HandleRef(this, this.HeaderHandle);

            UpdateWndImageList(ref hrThis, this.imageList);
         }
      }

      private void OnDetachImageList(object sender, EventArgs ea)
      {
         if (sender == this.imageList)
         {
            this.imageList = null;

            if (IsHandleCreated)
            {
               HandleRef hrThis = new HandleRef(this, this.HeaderHandle);

               UpdateWndImageList(ref hrThis, this.imageList);
            }
         }
      }

      /// <summary>
      /// Events
      /// </summary>

#if !PocketPC
      [
         Description("Occurs when user clicks on the section.")
      ]
#endif
      public event HeaderSectionEventHandler SectionClick;
      protected void OnSectionClick(HeaderSectionEventArgs ea)
      {
         if (this.SectionClick != null)
            this.SectionClick(this, ea);
      }

#if !PocketPC
      [
         Description("Occurs when user clicks on the section filter.")
      ]
#endif
      public event HeaderSectionEventHandler FilterClick;
      protected void OnFilterClick(HeaderSectionEventArgs ea)
      {
         if (this.FilterClick != null)
         {
            this.FilterClick(this, ea);
         }
      }

#if !PocketPC
      [
         Description("Occurs when user performs double clicks on the section.")
      ]
#endif
      public event HeaderSectionEventHandler SectionDblClick;
      protected void OnSectionDblClick(HeaderSectionEventArgs ea)
      {
         if (this.SectionDblClick != null)
            this.SectionDblClick(this, ea);
      }

#if !PocketPC
      [
         Description("Occurs when user performs double click on section's divider.")
      ]
#endif
      public event HeaderSectionEventHandler DividerDblClick;
      protected void OnDividerDblClick(HeaderSectionEventArgs ea)
      {
         if (this.DividerDblClick != null)
            this.DividerDblClick(this, ea);
      }

      /// <summary>
      /// Indication that header section is in resize
      /// </summary>
      public bool OnSectionResize { get; set; }

#if !PocketPC
      [
         Description("Occurs when user is about to start resizing of the section.")
      ]
#endif
      public event HeaderSectionWidthConformableEventHandler BeforeSectionTrack;
      protected void OnBeforeSectionTrack(HeaderSectionWidthConformableEventArgs ea)
      {
         if (this.BeforeSectionTrack != null)
         {
            OnSectionResize = true;
            Delegate[] aHandlers = this.BeforeSectionTrack.GetInvocationList();

            foreach (HeaderSectionWidthConformableEventHandler handler in aHandlers)
            {
               try
               {
                  handler(this, ea);
               }
               catch (Exception)
               {
                  ea.Accepted = false;
               }

               if (!ea.Accepted)
                  break;
            }
         }
      }

#if !PocketPC
      [
         Description("Occurs when user is resizing the section.")
      ]
#endif
      public event HeaderSectionWidthConformableEventHandler SectionTracking;
      protected void OnSectionTracking(HeaderSectionWidthConformableEventArgs ea)
      {
         if (this.SectionTracking != null)
         {
            Delegate[] aHandlers = this.SectionTracking.GetInvocationList();

            foreach (HeaderSectionWidthConformableEventHandler handler in aHandlers)
            {
               try
               {
                  handler(this, ea);
               }
               catch (Exception)
               {
                  ea.Accepted = false;
               }

               if (!ea.Accepted)
                  break;
            }
         }
      }

#if !PocketPC
      [
         Description("Occurs when user has section resized.")
      ]
#endif
      public event HeaderSectionWidthEventHandler AfterSectionTrack;
      protected void OnAfterSectionTrack(HeaderSectionWidthEventArgs ea)
      {
         if (this.AfterSectionTrack != null)
         {
            OnSectionResize = false;
            this.AfterSectionTrack(this, ea);
         }
      }

#if !PocketPC
      [
         Description("Occurs when user is about to start dragging of the " +
                        "section to another position.")
      ]
#endif

      /// <summary>
      /// Indication that header section is in drag
      /// </summary>
      public bool OnDrag { get; set; }

      public event HeaderSectionOrderConformableEventHandler BeforeSectionDrag;
      protected void OnBeforeSectionDrag(HeaderSectionOrderConformableEventArgs ea)
      {
         if (this.BeforeSectionDrag != null)
         {
            OnDrag = true;
            Delegate[] aHandlers = this.BeforeSectionDrag.GetInvocationList();

            foreach (HeaderSectionOrderConformableEventHandler handler in aHandlers)
            {
               try
               {
                  handler(this, ea);
               }
               catch (Exception)
               {
                  ea.Accepted = false;
               }

               if (!ea.Accepted)
                  break;
            }
         }
      }

#if !PocketPC
      [
         Description("Occurs when user has drugged the section to another position")
      ]
#endif
      public event HeaderSectionOrderConformableEventHandler AfterSectionDrag;
      protected void OnAfterSectionDrag(HeaderSectionOrderConformableEventArgs ea)
      {
         if (this.AfterSectionDrag != null)
         {
            Delegate[] aHandlers = this.AfterSectionDrag.GetInvocationList();

            foreach (HeaderSectionOrderConformableEventHandler handler in aHandlers)
            {
               try
               {
                  handler(this, ea);
               }
               catch (Exception)
               {
                  ea.Accepted = false;
               }

               if (!ea.Accepted)
                  break;
            }
         }
      }

#if !PocketPC
      [
         Description("Occurs when header control finished handling section drug")
      ]
#endif
      public event HeaderSectionOrderConformableEventHandler SectionDragEnded;
      protected void OnSectionDragEnded(HeaderSectionOrderConformableEventArgs ea)
      {
         if (this.SectionDragEnded != null)
         {
            OnDrag = false;
            Delegate[] aHandlers = this.SectionDragEnded.GetInvocationList();

            foreach (HeaderSectionOrderConformableEventHandler handler in aHandlers)
            {
               try
               {
                  handler(this, ea);
               }
               catch (Exception)
               {
                  ea.Accepted = false;
               }

               if (!ea.Accepted)
                  break;
            }
         }
      }

      /// <summary>
      /// Operations
      /// </summary>
#if !PocketPC
      protected override Size DefaultSize
      {
         get { return new Size(168, 30); }
      }

      protected override void CreateHandle()
      {
         if (!this.RecreatingHandle)
         {
            InitCommonControlsHelper.Init(InitCommonControlsHelper.Classes.Header);
         }

         base.CreateHandle();

      }
#endif

      protected override CreateParams CreateParams
      {
         get
         {
            CreateParams createParams = base.CreateParams;

            createParams.ClassName = NativeHeader.WC_HEADER;
            createParams.Style &= ~(NativeHeader.HDS_BUTTONS |
                              NativeHeader.HDS_HOTTRACK |
                              NativeHeader.HDS_FLAT |
                              NativeHeader.HDS_DRAGDROP |
                              NativeHeader.HDS_FULLDRAG);

            createParams.Style |= this.fStyle;
            if (rightToLeftLayout)
               createParams.ExStyle = NativeHeader.WS_EX_LAYOUTRTL | NativeHeader.WS_EX_NOINHERITLAYOUT;

            return createParams;
         }
      }

      protected override void WndProc(ref Message msg)
      {
         switch (msg.Msg)
         {
            // Handle notifications
#if PocketPC
            case (NativeHeader.WM_NOTIFY):
               // Check this - do we get here with a full sample?
#endif
            case (NativeHeader.WM_NOTIFY + NativeHeader.OCM__BASE):
               {
                  NativeWindowCommon.NMHDR nmhdr =
                     (NativeWindowCommon.NMHDR)Marshal.PtrToStructure(msg.LParam, typeof(NativeWindowCommon.NMHDR));
                  if (nmhdr.code == NativeHeader.HDN_ITEMCHANGING)
                  {
                     NativeHeader.NMHEADER nmh =
                        (NativeHeader.NMHEADER)Marshal.PtrToStructure(msg.LParam, typeof(NativeHeader.NMHEADER));
                     int iSection = ExtractIndexFromNMHEADER(ref nmh);
                     HeaderSection item = this.colSections._GetSectionByRawIndex(iSection);
                     MouseButtons enButton = ExtractMouseButtonFromNMHEADER(ref nmh);
                     int cxWidth = 0;

                     NativeHeader.HDITEM2 hdi = new NativeHeader.HDITEM2();
                     hdi.mask = 0;

                     ExtractSectionDataFromNMHEADER(ref nmh, ref hdi);
                     if ((hdi.mask & NativeHeader.HDI_WIDTH) != 0 && this.FullDragSections)
                     {
                        cxWidth = hdi.cxy;

                        HeaderSectionWidthConformableEventArgs ea =
                           new HeaderSectionWidthConformableEventArgs(item, enButton, cxWidth);

                        OnSectionTracking(ea);

                        item._SetWidth(cxWidth);

                        if (!Utils.IsXPStylesActive())
                           Invalidate(GetSectionRect(item));


                        msg.Result = ea.Accepted ? (IntPtr)0 : (IntPtr)1;
                        return;
                     }
                  }
                  else if (nmhdr.code == NativeHeader.HDN_ITEMCHANGED)
                  {
                     NativeHeader.NMHEADER nmh =
                        (NativeHeader.NMHEADER)Marshal.PtrToStructure(msg.LParam, typeof(NativeHeader.NMHEADER));

                     int iSection = ExtractIndexFromNMHEADER(ref nmh);
                     HeaderSection item = this.colSections._GetSectionByRawIndex(iSection);
                     MouseButtons enButton = ExtractMouseButtonFromNMHEADER(ref nmh);
                     int cxWidth = 0;

                     NativeHeader.HDITEM2 hdi = new NativeHeader.HDITEM2();
                     hdi.mask = 0;

                     ExtractSectionDataFromNMHEADER(ref nmh, ref hdi);
                     if ((hdi.mask & NativeHeader.HDI_WIDTH) != 0 && this.FullDragSections)
                     {
                        cxWidth = hdi.cxy;

                        //HeaderSectionWidthEventArgs ea =
                        //   new HeaderSectionWidthEventArgs(item, enButton, cxWidth);

                        //item._SetWidth(cxWidth);

                        //OnAfterSectionTrack(ea);
                     }
                  }
                  else if (nmhdr.code == NativeHeader.HDN_ITEMCLICK)
                  {
                     NativeHeader.NMHEADER nmh =
                        (NativeHeader.NMHEADER)Marshal.PtrToStructure(msg.LParam, typeof(NativeHeader.NMHEADER));

                     int iSection = ExtractIndexFromNMHEADER(ref nmh);
                     MouseButtons enButton = ExtractMouseButtonFromNMHEADER(ref nmh);
                     HeaderSection item = this.colSections._GetSectionByRawIndex(iSection);

                     HeaderSectionEventArgs ea = new HeaderSectionEventArgs(item, enButton);
                     //check if the click is on the filter
                     int currentSectionStart = GetHeaderSectionStartPos(item.Index);
                     //check if in the filter area
                     if ((item.HasFilter) && (currentSectionStart + item.Width - HeaderSection.FILTER_WIDTH < ClickXCoordinate && ClickXCoordinate < currentSectionStart + item.Width))
                        OnFilterClick(ea);
                     else //When the click is not on the filter
                        OnSectionClick(ea);
                  }
                  else if (nmhdr.code == NativeHeader.HDN_ITEMDBLCLICK)
                  {
                     NativeHeader.NMHEADER nmh =
                        (NativeHeader.NMHEADER)Marshal.PtrToStructure(msg.LParam, typeof(NativeHeader.NMHEADER));

                     int iSection = ExtractIndexFromNMHEADER(ref nmh);
                     MouseButtons enButton = ExtractMouseButtonFromNMHEADER(ref nmh);
                     HeaderSection item = this.colSections._GetSectionByRawIndex(iSection);

                     HeaderSectionEventArgs ea = new HeaderSectionEventArgs(item, enButton);

                     OnSectionDblClick(ea);
                  }
                  else if (nmhdr.code == NativeHeader.HDN_DIVIDERDBLCLICK)
                  {
                     NativeHeader.NMHEADER nmh =
                        (NativeHeader.NMHEADER)Marshal.PtrToStructure(msg.LParam, typeof(NativeHeader.NMHEADER));

                     int iSection = ExtractIndexFromNMHEADER(ref nmh);
                     MouseButtons enButton = ExtractMouseButtonFromNMHEADER(ref nmh);
                     HeaderSection item = this.colSections._GetSectionByRawIndex(iSection);

                     HeaderSectionEventArgs ea = new HeaderSectionEventArgs(item, enButton);

                     OnDividerDblClick(ea);
                  }
                  else if (nmhdr.code == NativeHeader.HDN_BEGINTRACK)
                  {
                     NativeHeader.NMHEADER nmh =
                        (NativeHeader.NMHEADER)Marshal.PtrToStructure(msg.LParam, typeof(NativeHeader.NMHEADER));

                     int iSection = ExtractIndexFromNMHEADER(ref nmh);
                     HeaderSection item = this.colSections._GetSectionByRawIndex(iSection);
                     MouseButtons enButton = ExtractMouseButtonFromNMHEADER(ref nmh);
                     int cxWidth = 0;

                     NativeHeader.HDITEM2 hdi = new NativeHeader.HDITEM2();
                     hdi.mask = 0;

                     ExtractSectionDataFromNMHEADER(ref nmh, ref hdi);
                     if ((hdi.mask & NativeHeader.HDI_WIDTH) != 0)
                     {
                        cxWidth = hdi.cxy;
                     }

                     HeaderSectionWidthConformableEventArgs ea =
                        new HeaderSectionWidthConformableEventArgs(item, enButton, cxWidth);

                     OnBeforeSectionTrack(ea);

                     msg.Result = ea.Accepted ? (IntPtr)0 : (IntPtr)1;
                     return;
                  }
                  else if (nmhdr.code == NativeHeader.HDN_TRACK)
                  {
                     NativeHeader.NMHEADER nmh =
                        (NativeHeader.NMHEADER)Marshal.PtrToStructure(msg.LParam, typeof(NativeHeader.NMHEADER));

                     int iSection = ExtractIndexFromNMHEADER(ref nmh);
                     HeaderSection item = this.colSections._GetSectionByRawIndex(iSection);
                     MouseButtons enButton = ExtractMouseButtonFromNMHEADER(ref nmh);
                     int cxWidth = 0;

                     NativeHeader.HDITEM2 hdi = new NativeHeader.HDITEM2();
                     hdi.mask = 0;

                     ExtractSectionDataFromNMHEADER(ref nmh, ref hdi);
                     if ((hdi.mask & NativeHeader.HDI_WIDTH) != 0)
                     {
                        cxWidth = hdi.cxy;
                     }

                     HeaderSectionWidthConformableEventArgs ea =
                        new HeaderSectionWidthConformableEventArgs(item, enButton, cxWidth);

                     OnSectionTracking(ea);

                     msg.Result = ea.Accepted ? (IntPtr)0 : (IntPtr)1;
                     return;
                  }
                  else if (nmhdr.code == NativeHeader.HDN_ENDTRACK)
                  {
                     NativeHeader.NMHEADER nmh =
                        (NativeHeader.NMHEADER)Marshal.PtrToStructure(msg.LParam, typeof(NativeHeader.NMHEADER));

                     int iSection = ExtractIndexFromNMHEADER(ref nmh);
                     HeaderSection item = this.colSections._GetSectionByRawIndex(iSection);
                     MouseButtons enButton = ExtractMouseButtonFromNMHEADER(ref nmh);
                     int cxWidth = 0;

                     NativeHeader.HDITEM2 hdi = new NativeHeader.HDITEM2();
                     hdi.mask = 0;

                     ExtractSectionDataFromNMHEADER(ref nmh, ref hdi);
                     if ((hdi.mask & NativeHeader.HDI_WIDTH) != 0)
                     {
                        cxWidth = hdi.cxy;
                     }

                     HeaderSectionWidthEventArgs ea =
                        new HeaderSectionWidthEventArgs(item, enButton, cxWidth);

                     item._SetWidth(cxWidth);

                     OnAfterSectionTrack(ea);
                  }
                  else if (nmhdr.code == NativeHeader.HDN_BEGINDRAG)
                  {
                     NativeHeader.NMHEADER nmh =
                        (NativeHeader.NMHEADER)Marshal.PtrToStructure(msg.LParam, typeof(NativeHeader.NMHEADER));

                     int iSection = ExtractIndexFromNMHEADER(ref nmh);
                     if (iSection >= 0)
                     {
                        HeaderSection item = this.colSections._GetSectionByRawIndex(iSection);
                        MouseButtons enButton = MouseButtons.Left; // Microsoft bugfix
                        // MouseButtons enButton = ExtractMouseButtonFromNMHEADER(ref nmh);

                        int iOrder = this.colSections.IndexOf(item);

                        HeaderSectionOrderConformableEventArgs ea =
                           new HeaderSectionOrderConformableEventArgs(item, enButton, iOrder);

                        OnBeforeSectionDrag(ea);

                        msg.Result = ea.Accepted ? (IntPtr)0 : (IntPtr)1;
                     }
                     return;
                  }
                  else if (nmhdr.code == NativeHeader.HDN_ENDDRAG)
                  {
                     NativeHeader.NMHEADER nmh =
                        (NativeHeader.NMHEADER)Marshal.PtrToStructure(msg.LParam, typeof(NativeHeader.NMHEADER));

                     int iSection = ExtractIndexFromNMHEADER(ref nmh);
                     HeaderSection item = this.colSections._GetSectionByRawIndex(iSection);
                     MouseButtons enButton = ExtractMouseButtonFromNMHEADER(ref nmh);

                     NativeHeader.HDITEM2 hdi = new NativeHeader.HDITEM2();
                     hdi.mask = 0;

                     ExtractSectionDataFromNMHEADER(ref nmh, ref hdi);
                     Debug.Assert((hdi.mask & NativeHeader.HDI_ORDER) != 0);
                     int iNewOrder = hdi.iOrder;

                     HeaderSectionOrderConformableEventArgs ea =
                        new HeaderSectionOrderConformableEventArgs(item, enButton, iNewOrder);
                     if (iNewOrder >= 0)
                     {
                        OnAfterSectionDrag(ea);
                        // Update orders
                        if (ea.Accepted)
                        {
                           int iOldOrder = this.colSections.IndexOf(item);

                           this.colSections._Move(iOldOrder, iNewOrder);
                        }
                        OnSectionDragEnded(ea);
                     }
                     msg.Result = ea.Accepted ? (IntPtr)0 : (IntPtr)1;
                     return;
                  }
                  else if (nmhdr.code == NativeCustomDraw.NM_CUSTOMDRAW)
                  {
                     base.WndProc(ref msg);

                     NativeCustomDraw.NMCUSTOMDRAW pcust =
                        (NativeCustomDraw.NMCUSTOMDRAW)Marshal.PtrToStructure(msg.LParam, typeof(NativeCustomDraw.NMCUSTOMDRAW));
                     switch (pcust.dwDrawStage)
                     {
                        case NativeCustomDraw.CDDS_PREPAINT:

                           msg.Result = (IntPtr)(NativeCustomDraw.CDRF_NOTIFYITEMDRAW | NativeCustomDraw.CDRF_NOTIFYPOSTPAINT);

                           return;
                        case NativeCustomDraw.CDDS_ITEMPREPAINT:
                           msg.Result = (IntPtr)NativeCustomDraw.CDRF_NOTIFYPOSTPAINT;
                           return;

                        case NativeCustomDraw.CDDS_ITEMPOSTPAINT:
                           PaintItemBackground(pcust);
                           HeaderSection item = this.colSections._GetSectionByRawIndex(pcust.dwItemSpec.ToInt32());
                           item.DrawTitle(pcust.hdc, pcust.rc, pcust.dwItemSpec.ToInt32(), SupportMultilineText, AddEndEllipsesFlag, rightToLeftLayout);
                           break;

                        case NativeCustomDraw.CDDS_POSTPAINT:
                           PaintBackground(pcust);
                           break;
                     }
                  }

                  //		  else if ( nmhdr.code == NativeHeader.HDN_GETDISPINFO )
                  //		  {
                  //		  }
                  //		  else if ( nmhdr.code == NativeHeader.HDN_FILTERCHANGE )
                  //		  {
                  //		  }
                  //		  else if ( nmhdr.code == NativeHeader.HDN_FILTERBTNCLICK )
                  //		  {
                  //		  }
               }
               break;

            case NativeHeader.WM_SETCURSOR:
               if (AllowColumnsResize)
                  DefWndProc(ref msg);
               return;
         }

         base.WndProc(ref msg);
      }
#if !PocketPC
      /// <summary>
      /// Removes filter flag from all header sections when mouse goes outside the header
      /// </summary>
      /// <param name="e"></param>
      protected override void OnMouseLeave(EventArgs e)
      {
         foreach (HeaderSection hs in Sections)
         {
            if (hs.HasFilter)
            {
               hs.HasFilter = false;
               Invalidate(GetSectionRect(hs), !Application.RenderWithVisualStyles);
            }
         }

         base.OnMouseLeave(e);
      }
#endif


      private int ClickXCoordinate { get; set; }
#if !PocketPC
      /// <summary>
      /// Saves last click x coordinate for filter click event
      /// </summary>
      /// <param name="e"></param>
      protected override void OnMouseDown(MouseEventArgs e)
      {
         ClickXCoordinate = e.X;
         base.OnMouseDown(e);
      }
#endif
#if !PocketPC
      /// <summary>
      /// Updates color of current header section filter and sets if filter should be shown or not
      /// </summary>
      /// <param name="e"></param>
      protected override void OnMouseMove(MouseEventArgs e)
      {
         if (!OnDrag)
         {
            bool needInvalidate = false;
            HitTestInfo hti = HitTest(e.X, e.Y);
            int currentHSX = 0;

            HeaderSection currentHeaderSection = hti.section;
            if (currentHeaderSection != null  && currentHeaderSection.AllowFilter)
            {
               if (!currentHeaderSection.HasFilter)
               {
                  currentHeaderSection.HasFilter = true;
                  needInvalidate = true;
                  if (!Application.RenderWithVisualStyles)
                     Invalidate(GetSectionRect(currentHeaderSection), true);
               }
               //Highlight
               currentHSX = GetHeaderSectionStartPos(currentHeaderSection.Index);
               //check if in the filter area and set filter color to be little bit darker
               if (currentHSX + currentHeaderSection.Width - HeaderSection.FILTER_WIDTH < e.X && e.X < currentHSX + currentHeaderSection.Width)
               {
						int titleColorR = TitleColor.R - 20;
						int titleColorG = TitleColor.G - 20;
						int titleColorB = TitleColor.B - 20;
                  currentHeaderSection.FilterColor = Color.FromArgb(TitleColor.A, titleColorR > 0 ? titleColorR : TitleColor.R, titleColorG > 0 ? titleColorG : TitleColor.G, titleColorB > 0 ? titleColor.B : TitleColor.B);
                  needInvalidate = true;
               }
               else
               {
                  currentHeaderSection.FilterColor = Color.Empty;
                  needInvalidate = true;
               }

            }
            if (!OnSectionResize)
            {
               foreach (HeaderSection hs in Sections)
                  if (hs != currentHeaderSection)
                  {
                     hs.HasFilter = false;
                     hs.FilterColor = Color.Empty;
                  }
            }

            if (needInvalidate)
               Invalidate();
         }
         base.OnMouseMove(e);
      }
#endif


#if !PocketPC
      protected override void OnSizeChanged(EventArgs e)
      {
         SetFilterHeight();
         base.OnSizeChanged(e);
      }
#endif
      /// <summary>
      /// return current section start x point
      /// </summary>
      /// <param name="index"></param>
      /// <returns></returns>
      public int GetHeaderSectionStartPos(int index)
      {
         int res = 0;
         for (int i = 0; i < index; i++)
         {
            res += Sections[i].Width;
         }
         return res;
      }

      /// <summary>
      /// Holds header divider height in order to paint filter line in the same heght
      /// </summary>
      public int DividerHeight { get; set; }

      /// <summary>
      /// Holds filter height
      /// </summary>
      public int FilterHeight { get; set; }


      public void SetFilterHeight()
      {
         if (TitleColor != Color.Empty)
         {
            if (TableLineDivider)
               FilterHeight = Height - 1;
            else
               FilterHeight = Height;
         }
         else
            FilterHeight = Height - 2;

      }

      /// <summary>
      /// Paint background for HeaderSection
      /// </summary>
      /// <param name="pcust"></param>
      public void PaintItemBackground(NativeCustomDraw.NMCUSTOMDRAW pcust)
      {
         NativeWindowCommon.RECT rect = new NativeWindowCommon.RECT()
         {
            bottom = pcust.rc.bottom,
            top = pcust.rc.top,
            left = pcust.rc.left,
            right = pcust.rc.right
         };

         HeaderRenderer.PaintItemBackGround(pcust, rect);

      }

      /// <summary>
      /// Paint background for Header
      /// </summary>
      /// <param name="pcust"></param>
      public void PaintBackground(NativeCustomDraw.NMCUSTOMDRAW pcust)
      {
         NativeWindowCommon.RECT rect = new NativeWindowCommon.RECT()
         {
            bottom = Height,
            top = 0,
            left = GetTotalHeaderSectionsWidth(),
            right = Width
         };

        HeaderRenderer.PaintBackGround(pcust, rect);
      }

      /// <summary>
      /// Paint dividers in table header.
      /// Difference is for case where there is color on the header or not
      /// </summary>
      /// <param name="pcust"></param>
      /// <param name="rect"></param>
      /// <param name="difference"></param>
      //private void PaintDividers(NativeCustomDraw.NMCUSTOMDRAW pcust, ref NativeWindowCommon.RECT rect, int difference)
      //{
      //   // paint divider for last header only when "Show Last Divider " of table control is true 
      //   if (ShowDividers)
      //   {
      //      // Check if last divider is to be painted .
      //      // For last section , last divider should be painted when 'ShowLastDivider' property is set to true
      //      bool showlastDivider = (pcust.dwItemSpec.ToInt32() == Sections.Count - 1 ? ShowLastDivider : true);

      //      if (showlastDivider)
      //         HeaderRenderer.DrawDivider(pcust.hdc, pcust.dwItemSpec.ToInt32(), ref rect, DividerHeight);
      //   }
      //}

      /// <summary>
      /// Sets divider height
      /// </summary>
      public void SetDividerHeight()
      {
         if (HeaderRenderer != null)
            DividerHeight = HeaderRenderer.GetDividerHeight();
      }

      /// <summary>
      /// get header height
      /// </summary>
      /// <param name="height"></param>
      internal int GetHeight(int height)
      {
         return HeaderRenderer.GetHeaderHeight(height);
      }

      #region Helper methods

      /// <summary>
      /// Get Total of all Header section's width
      /// </summary>
      /// <returns></returns>
      int GetTotalHeaderSectionsWidth()
      {
         int width = 0;

         for (int index = 0; index < Sections.Count; index++)
         {
            HeaderSection headerSection = Sections[index];
            width += headerSection.Width;
         }

         return width;
      }

      /// <summary>
      /// Get Tail area width
      /// </summary>
      /// <returns></returns>
      int GetTailWidth()
      {
         return Width - GetTotalHeaderSectionsWidth();
      }

      #endregion

      /// <summary>
      /// Operations
      /// </summary>

      public void BeginUpdate()
      {
         if (this.IsHandleCreated)
         {
            HandleRef hrThis = new HandleRef(this, this.HeaderHandle);

            NativeWindowCommon.SendMessage(hrThis.Handle, NativeWindowCommon.WM_SETREDRAW,
                                    0, 0);
         }
      }

      public void EndUpdate()
      {
         if (this.IsHandleCreated)
         {
            HandleRef hrThis = new HandleRef(this, this.HeaderHandle);

            NativeWindowCommon.SendMessage(hrThis.Handle, NativeWindowCommon.WM_SETREDRAW,
                                    1, 0);
         }
      }

      public Rectangle GetSectionRect(HeaderSection item)
      {
         int iSection = this.colSections._FindSectionRawIndex(item);
         Debug.Assert(iSection >= 0);

         HandleRef hrThis = new HandleRef(this, this.HeaderHandle);

         NativeHeader.RECT rc;
         bool bResult = NativeHeader.GetItemRect(hrThis.Handle, iSection, out rc);
         Debug.Assert(bResult);
         if (!bResult)
            throw new Win32Exception();

         return Rectangle.FromLTRB(rc.left, rc.top, rc.right, rc.bottom);
      }

      public void CalculateLayout(Rectangle rectArea, out Rectangle rectPosition)
      {
         NativeHeader.HDLAYOUT hdl = new NativeHeader.HDLAYOUT();
         hdl.prc = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(NativeHeader.RECT)));
         hdl.pwpos = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(NativeHeader.WINDOWPOS)));

         try
         {
            HandleRef hrThis = new HandleRef(this, this.HeaderHandle);

            NativeHeader.RECT rc = new NativeHeader.RECT();
            rc.left = rectArea.Left;
            rc.top = rectArea.Top;
            rc.right = rectArea.Right;
            rc.bottom = rectArea.Bottom;

            Marshal.StructureToPtr(rc, hdl.prc, false);

            bool bResult = NativeHeader.Layout(hrThis.Handle, ref hdl);
            Debug.Assert(bResult);
            if (!bResult)
               throw new Win32Exception();

            NativeHeader.WINDOWPOS wp =
               (NativeHeader.WINDOWPOS)Marshal.PtrToStructure(hdl.pwpos,
                                          typeof(NativeHeader.WINDOWPOS));

            rectPosition = new Rectangle(wp.x, wp.y, wp.cx, wp.cy);
         }
         finally
         {
            if (hdl.prc != IntPtr.Zero)
               Marshal.FreeHGlobal(hdl.prc);

            if (hdl.pwpos != IntPtr.Zero)
               Marshal.FreeHGlobal(hdl.pwpos);
         }
      }

      public int SetHotDivider(int x, int y)
      {
         HandleRef hrThis = new HandleRef(this, this.HeaderHandle);

         return NativeHeader.SetHotDivider(hrThis.Handle, true, (y << 16) | x);
      }

      public int SetHotDivider(int iDevider)
      {
         HandleRef hrThis = new HandleRef(this, this.HeaderHandle);

         return NativeHeader.SetHotDivider(hrThis.Handle, false, iDevider);
      }

      public HitTestInfo HitTest(int x, int y)
      {
         return HitTest(new Point(x, y));
      }

      public HitTestInfo HitTest(Point point)
      {
         HandleRef hrThis = new HandleRef(this, this.HeaderHandle);

         NativeHeader.HDHITTESTINFO htiRaw = new NativeHeader.HDHITTESTINFO();
         htiRaw.pt.x = point.X;
         htiRaw.pt.y = point.Y;
         htiRaw.iItem = -1;
         htiRaw.flags = 0;

         NativeHeader.HitTest(hrThis.Handle, ref htiRaw);

         HitTestInfo hti = new HitTestInfo();
         hti.fArea = (HitTestArea)htiRaw.flags;

         if (htiRaw.iItem >= 0)
         {
            hti.section = this.colSections._GetSectionByRawIndex(htiRaw.iItem);
         }

         return hti;
      }

   }

   #endregion // Header control

   #region Common

   /// <summary>
   /// ErrMsg class
   /// </summary>
   internal abstract class ErrMsg
   {
      public static string NegVal()
      {
         return "Value cannot be negative.";
      }

      public static string NullVal()
      {
         return "Value cannot be null.";
      }

      public static string InvVal(string sValue)
      {
         return string.Format("Value of \"{0}\" is invalid.", sValue);
      }

      public static string IndexOutOfRange()
      {
         return "Index is out of range.";
      }

      public static string SectionIsAlreadyAttached(string sText)
      {
         return "Section \"" + sText + "\" is already added to the collection.";
      }

      public static string SectionDoesNotExist(string sText)
      {
         return "Section \"" + sText + "\" does not exist in the collection";
      }

      public static string FailedToInsertItem()
      {
         return "Failed to insert item.";
      }

      public static string FailedToRemoveItem()
      {
         return "Failed to remove item.";
      }

      public static string FailedToChangeItem()
      {
         return "Failed to change item.";
      }
   }

   #endregion Common
}
