using System.Drawing;
using System.ComponentModel;
using System.Collections.Generic;
using com.magicsoftware.win32;
using System;
using System.Windows.Forms;
using com.magicsoftware.util;
using Controls.com.magicsoftware.controls.Renderer;

#if PocketPC
using System.Runtime.InteropServices;
using Message = com.magicsoftware.mobilestubs.Message;

#else
using System.ComponentModel.Design;

#endif

namespace com.magicsoftware.controls
{
   /// <summary>
   /// implements table control : properties
   /// </summary>
   public abstract partial class TableControl
   {
      protected Header _header;
      private TableColumns _columns;

      private int _prevWidth = 0;
      private int _prevLogWidth = 0;
      protected List<TableItem> _items;

      /// <summary>
      /// style of table (2d , 3d raised , windows 3d)
      /// </summary>
      ControlStyle controlStyle = ControlStyle.Windows;
      public ControlStyle ControlStyle
      {
         get
         {
            return controlStyle;
         }

         set
         {
            if (controlStyle != value)
            {
               controlStyle = value;

               UpdateRenderers();

               // for windows 3d and 3D-raised style, borderstyle should always be Fixed3D
               BorderStyle = BorderStyle.None;
               AdjustHeaderRectangle();
               Invalidate();
            }
         }
      }

      #region IBorderTypeProperty members

      /// <summary>
      /// type of border (thick / thin / no border)
      /// </summary>
      private BorderType borderType;
      public BorderType BorderType
      {
         get { return borderType; }
         set
         {
            if (borderType != value)
            {
               borderType = value;
               AdjustHeaderRectangle();
               Invalidate();
            }
         }
      }

      #endregion

      /// <summary>
      ///  Denotes the factor 
      ///  TODO - use code from get_resolution_factor()
      /// </summary>
      public static int Factor
      {
         get
         {
            return 1;
         }
      }

      public bool PaintDidvidersInEnd { get; set; } = false;

#if !PocketPC

      [
         Category("Behavior"),
         Description("Columns"),
         MergableProperty(false),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Content)
      ]
#endif
      public TableColumns Columns
      {
         get { return _columns; }
      }

      /// <summary> This event will be raised whenever row height changes. </summary>
      public event EventHandler RowHeightChanged;
      public event EventHandler TitleHeightChanged;

      private int rowHeight;
#if !PocketPC
      [
       Category("Appearance"),
       Description("Specifies row height.")
      ]
#endif
      public int RowHeight
      {
         get { return rowHeight; }
         set
         {
            if (rowHeight != value)
            {
               rowHeight = value;
               if (RowHeightChanged != null)
                  RowHeightChanged(this, new EventArgs());
               else
                  Invalidate();
            }
         }
      }

      public int OriginalRowHeight { get; set; }

      public bool HasRowPlacement { get; set; }

      public int BorderHeight
      {
         get
         {
            return tableStyleRenderer.GetBorderSize();
         }
      }

#if !PocketPC
      [
          Category("Appearance"),
          Description("Specifies title height.")
      ]
#endif
      bool titleHeightIsSet;
      private int titleHeight;
      public int TitleHeight
      {
         get { return titleHeight; }
         set
         {
            if (value != titleHeight || !titleHeightIsSet)
            {
               titleHeight = value;
               titleHeightIsSet = true;
               AdjustHeaderRectangle();
               _header.SetDividerHeight();
               if (this.TitleHeightChanged != null)
                  TitleHeightChanged(this, new EventArgs());
               _header.Invalidate();
            }
         }
      }


      private bool showLineDividers;

#if !PocketPC
      [
         Category("Appearance"),
          Description("Shows line dividers")
      ]
#endif
      public bool ShowLineDividers
      {
         get { return showLineDividers; }
         set
         {
            showLineDividers = value;
            _header.TableLineDivider = value;
            Invalidate();
         }
      }

      private bool showColumnDividers;

#if !PocketPC
      [
         Category("Appearance"),
         Description("Shows column dividers")
      ]
#endif
      public bool ShowColumnDividers
      {
         get { return showColumnDividers; }
         set
         {
            showColumnDividers = value;
            _header.TableColumnDivider = value;
            Invalidate();
         }
      }

#if !PocketPC
      [
         Category("Appearance"),
         Description("Last divider")
      ]
#endif

      /// <summary>
      /// indicates whether last divider is visible or not
      /// </summary>
      public bool ShowLastDivider
      {
         get
         {
            return _header.ShowLastDivider;
         }
         set
         {
            if (_header.ShowLastDivider != value)
            {
               _header.ShowLastDivider = value;
               Invalidate();
            }
         }
      }


#if !PocketPC
      [
         Category("Appearance"),
         Description("Show Vertical scrollBar if needed.")
      ]
#endif
      private bool verticalScrollBar_DO_NOT_USE_DIRECTLY;
      public bool VerticalScrollBar 
      {
         get { return verticalScrollBar_DO_NOT_USE_DIRECTLY; }
         set 
         {
            if (verticalScrollBar_DO_NOT_USE_DIRECTLY != value)
            {
               verticalScrollBar_DO_NOT_USE_DIRECTLY = value;
               updateVScroll(false);
            }
         }
      }

      private bool horizontalScrollBar_DO_NOT_USE_DIRECTLY = true;

      /// <summary>
      ///  indicates if table should have horizontal scroll bar 
      /// </summary>
      public bool HorizontalScrollBar
      {
         get { return horizontalScrollBar_DO_NOT_USE_DIRECTLY; }
         set
         {
            if (horizontalScrollBar_DO_NOT_USE_DIRECTLY != value)
            {
               horizontalScrollBar_DO_NOT_USE_DIRECTLY = value;
               updateHScroll();
            }
         }
      }

      private Color magicBackgorundColor = Color.Empty; // background color
#if !PocketPC
      [
         Category("Appearance"),
         Description("Table's Background Color")
      ]
#endif

      public Color MagicBgColor
      {
         get { return magicBackgorundColor; }
         set
         {
            if (magicBackgorundColor != value)
            {
               magicBackgorundColor = value;
               UpdateBgColorAndTransparency();
               _header.UpdateTitleBrush();
            }
         }
      }
      private bool isBackGroundColorTransparent;

#if !PocketPC
      [
         Category("Appearance"),
         Description("Table's Background Color is Transparent")
      ]
#endif

      public bool IsMagicBackGroundColorTransparent
      {
         get { return isBackGroundColorTransparent; }
         set
         {
            if (isBackGroundColorTransparent != value)
            {
               isBackGroundColorTransparent = value;
               UpdateBgColorAndTransparency();
            }
         }
      }

      private Color bgColor = Color.Empty; // background color
      public Color BgColor
      {
         get { return bgColor; }
         set
         {
            if (bgColor != value)
            {
               bgColor = value;
            }
         }
      }


      private Color alternateColor = Color.Empty; // alternating color

 #if !PocketPC
      [
         Category("Appearance"),
         Description("Table's Alternating Color")
      ]
#endif
      public Color AlternateColor
      {
         get { return alternateColor; }
         set
         {
            if (alternateColor != value)
            {
               alternateColor = value;
               UpdateBgColorAndTransparency();
            }
         }
      }
      private readonly Color defaultColor; // default table's color

      private TableColorBy colorBy = TableColorBy.Column; // color by property of table
#if !PocketPC
      [
         Category("Appearance"),
         Description("Color by")
      ]
#endif
      public TableColorBy ColorBy
      {
         get { return colorBy; }
         set
         {
            if (colorBy != value)
            {
               colorBy = value;
               UpdateBgColorAndTransparency();
            }
         }
      }
 

      private bool rightToLeftLayout = false;

#if !PocketPC
      [
         Category("Appearance"),
         Description("Right to left layout.")
      ]
#endif
      public bool RightToLeftLayout
      {
         get { return rightToLeftLayout; }
         set
         {
            if (value != rightToLeftLayout)
            {
               rightToLeftLayout = value;
               isVerticalScrollBarVisible = false;
               _header.RightToLeftLayout = value;
#if !PocketPC //tmp
               RecreateHandle();
               updateVScroll(false);
#endif
               Invalidate();
            }
         }
      }

#if !PocketPC
      [
         Category("Appearance"),
         Description("Table is transparent.")
      ]
#endif
      private bool isTransparent;

      public bool IsTransparent
      {
         get { return isTransparent; }
         set
         {
            if (value != isTransparent)
               isTransparent = value;
         }
      }


#if !PocketPC
      [
         Category("Behavior"),
         Description("Allows column resize.")
      ]
#endif
      public bool AllowColumnResize
      {
         get { return _header.AllowColumnsResize; }
         set { _header.AllowColumnsResize = value; }
      }

#if !PocketPC
      [
         Category("Behavior"),
         Description("Allows column reorder.")
      ]
#endif
      public bool AllowColumnReorder
      {
         get { return _header.AllowDragSections; }
         set { _header.AllowDragSections = value; }
      }
 #if !PocketPC
      [
         Category("Appearance"),
         Description("Title bar Color ")
      ]
#endif


      public Color TitleColor
      {
         get { return _header.TitleColor; }
         set { _header.TitleColor = value; }
      }

      private Color dividerColor = SystemColors.ControlLight;
#if !PocketPC
      [
         Category("Appearance"),
         Description("Column divider Color ")
      ]
#endif

      public Color DividerColor
      {
         get { return dividerColor; }
         set 
         {
            if (dividerColor != value)
            {
               if (value != Color.Empty)
                  dividerColor = value;                 
               else
                  dividerColor = SystemColors.ControlLight;

               _header.DividerColor = value;
               _header.UpdateDividerPen();
            }
         }
      }

      public override Color ForeColor
      {
         get
         {
            return base.ForeColor;
         }

         set
         {
            base.ForeColor = value;
            _header.UpdateDividerPen();
         }
      }

      public bool AddEndEllipsesFlag
      {
         get
         {
            return _header.AddEndEllipsesFlag;
         }
         set
         {
            _header.AddEndEllipsesFlag = value;
         }
      }

      // size of a list of rows that the table control is aware to (but doesn't necessarily display)
      protected int _virtualItemsCount = 0;
#if !PocketPC
      [
         Category("Behavior"),
         Description("Set virtual table size.")
      ]
#endif
      public int VirtualItemsCount
      {
         get { return _virtualItemsCount; }
         set { SetVirtualItemsCount(value); }
      }

      protected int _topIndex;
#if !PocketPC
      [
         Category("Behavior"),
         Description("Set table Top Index")
      ]
#endif
      public int TopIndex
      {
         get { return _topIndex; }
         set { SetTopIndex(value, true); }
      }

      public int RecordsBeforeCurrentView { get; set; }

      public int RowsInPage { get; protected set; }

      public delegate void TableItemDisposeEventHandler(
             object sender, TableItemDisposeArgs ea);

#if !PocketPC
      [
         Description("Occurs when table row is disposed"),
         Category("Behavior")

      ]
#endif
      public event TableItemDisposeEventHandler ItemDisposed;

      public delegate void TableDrawRowHandler(
             object sender, TablePaintRowArgs ea);
#if !PocketPC
      [
          Description("Occurs when table row is disposed"),
          Category("Behavior")

      ]
#endif
      public event TableDrawRowHandler EraseItem;
      public event TableDrawRowHandler PaintItem;

      public event EventHandler BeforeSectionDrag;

      public delegate void TableReorderHandler(
      object sender, TableReorderArgs ea);

      public event TableReorderHandler Reorder;
      public event EventHandler ReorderEnded;

      /// <summary> 
      /// Required designer variable.
      /// </summary>
      private System.ComponentModel.IContainer components = null;
      /// <summary> 
      /// Clean up any resources being used.
      /// </summary>
      /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
      protected override void Dispose(bool disposing)
      {
         if (disposing && (components != null))
         {
            components.Dispose();
         }
         base.Dispose(disposing);
      }

      #region Component Designer generated code

      /// <summary> 
      /// Required method for Designer support - do not modify 
      /// the contents of this method with the code editor.
      /// </summary>
      private void InitializeComponent()
      {
         _header = GetHeader();
         _header.UpdateRenderer(this);
         this.SuspendLayout();
         // 
         // header
         // 
         _header.Clickable = true;
         _header.FullDragSections = true;
         _header.HotTrack = true;
         _header.Location = new System.Drawing.Point(0, 0);
         _header.Name = "header";
         _header.Size = new System.Drawing.Size(213, 30);
         _header.TabIndex = 0;
         // 
         // TableControl
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.BackColor = System.Drawing.Color.White;
         this.Controls.Add(_header);
         this.Name = "TableControl";
         this.Size = new System.Drawing.Size(213, 190);
#if !PocketPC
         this.Load += new System.EventHandler(this.TableControl_Load);
         this.Layout += new System.Windows.Forms.LayoutEventHandler(this.TableControl_Layout);
         this.Scroll += new System.Windows.Forms.ScrollEventHandler(this.TableControl_Scroll);
#else
            _header.AfterSectionTrack += new HeaderSectionWidthEventHandler(header_AfterSectionTrack);
            this.HandleCreated += new EventHandler(TableControl_HandleCreated);
#endif
         this.Resize += new System.EventHandler(this.TableControl_Resize);
         this.ResumeLayout(false);

      }
      #endregion

      protected virtual Header GetHeader()
      {
         return new com.magicsoftware.controls.Header();
      }

 #if !PocketPC
     public IDesignerHost GetDesignerHost()
      {
         return (IDesignerHost)GetService(typeof(IDesignerHost));
      }
#endif
#if PocketPC
        // Using the native window proc
        // A delegate for our wndproc
        public delegate void MobileWndProc(IntPtr hwnd, uint msg, uint wParam, int lParam);

        // Original wndproc
        private IntPtr OrigWndProc;
        // object's delegate
        MobileWndProc proc;

        //This is usually where the control handle is created - subclass
        void TableControl_HandleCreated(object sender, EventArgs e)
        {
           proc = new MobileWndProc(WindowProc);
           OrigWndProc = (IntPtr)NativeWindowCommon.SetWindowLong(Handle, NativeWindowCommon.GWL_WNDPROC,
                           Marshal.GetFunctionPointerForDelegate(proc).ToInt32());
        }

        // Our wndproc
        private void WindowProc(IntPtr hwnd, uint msg, uint wParam, int lParam)
        {
           Message message = new Message();
           message.HWnd = hwnd;
           message.Msg = (int)msg;
           // Need this casting, for flags that are passed as large unsigned values 
           message.WParam = (IntPtr)(int)(((UIntPtr)wParam).ToUInt32());
           message.LParam = (IntPtr)lParam;

           // Call our usual code
           WndProc(ref message);
        }
#endif

      /// <summary>
      /// set the bound of header 
      /// </summary>
      private void AdjustHeaderRectangle()
      {
         // set the location and size of header
         _header.Location = tableStyleRenderer.GetHeaderRectangle(_header).Location;
         _header.Size = tableStyleRenderer.GetHeaderRectangle(_header).Size;
      }
   }
}
