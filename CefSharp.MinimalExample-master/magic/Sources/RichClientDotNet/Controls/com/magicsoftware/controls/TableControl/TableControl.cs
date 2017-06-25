using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using com.magicsoftware.win32;
using System.Diagnostics;
using com.magicsoftware.controls.utils;
using com.magicsoftware.util.notifyCollection;
using com.magicsoftware.util;
using com.magicsoftware.controls.designers;
using Controls.com.magicsoftware.controls.Renderer;
using Controls.com.magicsoftware.controls.PropertyInterfaces;

#if !PocketPC
using System.Windows.Forms.VisualStyles;
using System.ComponentModel;
using System.ComponentModel.Design;



#else
using Message = com.magicsoftware.mobilestubs.Message;
using LayoutEventArgs = com.magicsoftware.mobilestubs.LayoutEventArgs;
using RightToLeft = com.magicsoftware.mobilestubs.RightToLeft;
#endif

namespace com.magicsoftware.controls
{
   /// <summary>
   /// implements table control : main table class
   /// </summary>

#if !PocketPC
   [Designer(typeof(TableControlDesigner))]
   //ToolboxBitmap is used  to find the image icon of TableControl, TableControl.bmp is the image for TableControl.
   [ToolboxBitmap(typeof(ResourceFinder), "Controls.Resources.TableControl.bmp")]
   public abstract partial class TableControl : UserControl, INotifyCollectionChanged, IRightToLeftProperty, IBorderStyleProperty, ICanParent, IMgContainer, IBorderTypeProperty
#else
   public abstract partial class TableControl : UserControl, INotifyCollectionChanged, IRightToLeftProperty, IBorderStyleProperty, ICanParent
#endif
   {
      #region IRightToLeftProperty Members

#if !PocketPC
      public override RightToLeft RightToLeft
      {
         get
         {
            return base.RightToLeft;
         }
         set
         {
            base.RightToLeft = value;
            RightToLeftLayout = (value == RightToLeft.Yes ? true : false);
         }
      }
#else
      public RightToLeft RightToLeft
      {
         get
         {
            return (RightToLeftLayout ? RightToLeft.Yes : RightToLeft.No);
         }
         set
         {
            RightToLeftLayout = (value == RightToLeft.Yes ? true : false);
         }
      }
#endif

      #endregion

#if !PocketPC
      private readonly VisualStyleRenderer _renderer = null;  //render is used for themed border painting
      private readonly VisualStyleElement _element = VisualStyleElement.TextBox.TextEdit.Normal;
#endif
      public const int TAIL_COLUMN_IDX = int.MaxValue;

      static Color defaultHeaderBackColor = Color.FromArgb(252, 252, 252);

      public bool AllowPaint { set; get; } //paint will be executed only if value is true, for performance improvements
      private int _displayWidth;
      public bool ShowContextMenu { set; get; } //This will decide if ContextMenu should be invoked on table control

      protected Point _corner; //save here corner of the table, we need it since getXCorner can be executed only in
                             //GUI thread. But to support animated gif we must perform it in separate thread.

      protected bool _isThumbDrag = false; // indicates scroll-thumb is dragging.
      private bool _startTablePaintWithAlternateColor = false;

      private Pen dividerPen;

      /// <summary>
      /// Strategy for Table Multi Column Display
      /// </summary>
      public TableMultiColumnStrategyBase TableMultiColumnStrategy { get; set; }

      /// <summary>
      /// in designer drag
      /// </summary>
      public bool IsDragging { get; private set; }
      
      /// <summary>
      /// Indicates OnDragDrop() for Table control designer is in process
      /// </summary>
      public bool InDragDrop { get; set; }

      public bool StartTablePaintWithAlternateColor
      {
         get { return _startTablePaintWithAlternateColor; }
         set { _startTablePaintWithAlternateColor = value; }
      }
      // columns count
      public int ColumnCount
      {
         get { return _columns.Count; }
      }

      public bool HasTableHeaderChild { get; set; }

      public bool IsColoredByRow { get { return colorBy == TableColorBy.Row; } }

      public const int STUDIO_BORDER_WIDTH = 2;

      /// <summary>
      /// non client left mouse down
      /// </summary>
      public event NCMouseEventHandler NCMouseDown;
      public event EventHandler HorizontalScrollVisibilityChanged;
      Timer repaintHeaderTimer = new Timer() { Interval = 300 };
      public delegate void BeginTrackHandler(object sender, TableColumnArgs e);


#if !PocketPC
      public event DragEventHandler DesignerDragEnter;
      public event EventHandler DesignerDragStop;
      public event DragEventHandler DesignerDragOver;
      /// <summary>
      /// raised when begining track of the header
      /// </summary>
      public event BeginTrackHandler BeginTrack;


#endif

      protected TableStyleRendererBase tableStyleRenderer;

      /// <summary>
      /// ctor
      /// </summary>
      protected TableControl()
      {
         InitializeComponent();
         UpdateRenderers();

#if !PocketPC
         SetStyle(ControlStyles.SupportsTransparentBackColor, true);
         DoubleBuffered = true;
         SetStyle(ControlStyles.UserPaint, true);
         SetStyle(ControlStyles.AllPaintingInWmPaint, true);
         SetStyle(ControlStyles.DoubleBuffer, true);
         SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
#endif
         RowsInPage = 0;
         AllowPaint = true;
         AllowColumnResize = true;
         AllowColumnReorder = true;
         ShowContextMenu = true;
         _columns = new TableColumns(this);
         _items = new List<TableItem>();
         this.Disposed += new EventHandler(TableControl_Disposed);
         this.BorderStyle = BorderStyle.Fixed3D;
         defaultColor = BackColor;
         BackColor = Color.Transparent;//we do not want .NET to paint the Table so its color is always transparent

#if !PocketPC
         this.MouseWheel += new MouseEventHandler(TableControl_MouseWheel);
#endif
         _header.BeforeSectionTrack += new HeaderSectionWidthConformableEventHandler(header_BeforeSectionTrack);
         _header.SectionClick += new HeaderSectionEventHandler(header_SectionClick);
         _header.FilterClick += new HeaderSectionEventHandler(header_FilterClick);
         _header.AfterSectionDrag += new HeaderSectionOrderConformableEventHandler(header_AfterSectionDrag);
         _header.SectionDragEnded += new HeaderSectionOrderConformableEventHandler(header_SectionDragEnded);
         _header.BeforeSectionDrag += new HeaderSectionOrderConformableEventHandler(header_BeforeSectionDrag);
#if !PocketPC
         repaintHeaderTimer.Tick += repaintHeaderTimer_Tick ;


         Columns.CollectionChanged += Columns_CollectionChanged;
         if (Application.RenderWithVisualStyles && VisualStyleRenderer.IsElementDefined(_element))
            _renderer = new VisualStyleRenderer(_element);
#endif
         dividerPen = new Pen(DividerColor);
         initHScroll();
         TableMultiColumnStrategy = new TableMultiColumnDisabledStrategy(this);
      }

      /// <summary>
      /// update the renderers
      /// </summary>
      private void UpdateRenderers()
      {
         if (tableStyleRenderer != null)
            tableStyleRenderer.Dispose();
         tableStyleRenderer = TableRendererFactory.GetRenderer(this);
         _header.UpdateRenderer(this);
      }

#if !PocketPC
      protected override void OnControlAdded(ControlEventArgs e)
      {
         base.OnControlAdded(e);
         OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, e.Control));
      }

      protected override void OnControlRemoved(ControlEventArgs e)
      {
         base.OnControlRemoved(e);
         OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, e.Control));
      }

      protected override void OnMouseCaptureChanged(EventArgs e)
      {
         base.OnMouseCaptureChanged(e);
         //activate timer to clear the upper/left corner of scrollbar painted by scrollbar animation
         if (!this.Capture && Application.RenderWithVisualStyles)
            repaintHeaderTimer.Start();
           
      }


      void repaintHeaderTimer_Tick(object sender, EventArgs e)
      {
         if (!IsDisposed && !isVerticalScrollBarVisible && Application.RenderWithVisualStyles)
         {
            NativeWindowCommon.RedrawWindow(this.Handle, IntPtr.Zero, IntPtr.Zero, NativeWindowCommon.RedrawWindowFlags.RDW_FRAME |
                                           NativeWindowCommon.RedrawWindowFlags.RDW_INVALIDATE);
            this._header.Refresh(); //to clear the upper/left corner 
         }
         repaintHeaderTimer.Stop();

      }
#endif

      public void EnableMultiColumnDisplayStrategy()
      {
         TableMultiColumnStrategy = new TableMultiColumnEnabledStrategy(this);
      }

      public void DisableMultiColumnDisplayStrategy()
      {
         TableMultiColumnStrategy = new TableMultiColumnDisabledStrategy(this);
      }

      void Columns_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
      {
         OnCollectionChanged( e);
      }

      void header_BeforeSectionDrag(object sender, HeaderSectionOrderConformableEventArgs ea)
      {
         OnBeforeSectionDrag();
      }

      protected void OnBeforeSectionDrag()
      {
         if (BeforeSectionDrag != null)
            BeforeSectionDrag(this, new EventArgs());
      }

#if !PocketPC
      protected override CreateParams CreateParams
      {
         get
         {
            CreateParams createParams = base.CreateParams;

            if (rightToLeftLayout)
               createParams.ExStyle |= NativeWindowCommon.WS_EX_LEFTSCROLLBAR;
            return createParams;
         }
      }
#endif

      /// <summary>
      /// handle section drag end
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="ea"></param>
      void header_SectionDragEnded(object sender, HeaderSectionOrderConformableEventArgs ea)
      {
         if (ReorderEnded != null)
            ReorderEnded(this, new EventArgs());
      }

      /// <summary>
      /// handle section drag 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="ea"></param>
      void header_AfterSectionDrag(object sender, HeaderSectionOrderConformableEventArgs ea)
      {
         TableColumn column = getColumnByHeaderSection(ea.Item);
         int columnIndex = Columns.IndexOf(column);

         TableColumn newColumn = _columns[ea.Order];         
         int newColumnIndex = Columns.IndexOf(newColumn);
        _columns.Move(columnIndex, newColumnIndex);
       
         TableReorderArgs e = new TableReorderArgs(column, newColumn);
         if (Reorder != null)
            Reorder(this, e);
         ea.Accepted = false;
      }

      ///// <summary> 
      ///// sort the headers in a order
      ///// </summary>
      ///// <param name="order"></param>
      public void sort(int[] order)
      {
         this._columns.Sort(order);
         sortNativeHeader(order);
      }

      public void sortHeader(int[] order)
      {
         _header.Sections.sort(order);
      }
      /// <summary> 
      /// sort the native headers in a order
      /// </summary>
      /// <param name="order"></param>
      public void sortNativeHeader(int[] order)
      {
         _header.sortOrder(order);
      }

      /// <summary>
      /// create event on click
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="ea"></param>
      void header_SectionClick(object sender, HeaderSectionEventArgs ea)
      {
         if (ea.Button == MouseButtons.Left)
         {
            TableColumn column = getHeaderColumn(ea.Item);
            column.OnColumnClick();
         }
      }

      /// <summary>
      /// Handler for column filter click event
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="ea"></param>
      void header_FilterClick(object sender, HeaderSectionEventArgs ea)
      {
         if (ea.Button == MouseButtons.Left)
         {
            TableColumn column = getHeaderColumn(ea.Item);
            column.OnFilterClick(ea);
         }
      }

      /// <summary>
      /// insert header section
      /// </summary>
      /// <param name="index"></param>
      /// <param name="section"></param>
      internal void InsertHeaderSection(int index, HeaderSection section)
      {
         _header.Sections.Insert(index, section);
      }

      /// <summary>
      /// remove header section
      /// </summary>
      /// <param name="index"></param>
      internal void RemoveHeaderSection(int index)
      {
         _header.Sections.RemoveAt(index);
      }


      /// <summary>
      /// remove column at index
      /// </summary>
      /// <param name="index"></param>
      internal void RemoveColumnAt(int index)
      {

         this._columns.RemoveAt(index);
         this.PerformLayout();
      }

      /// <summary>
      /// handle table disposing
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void TableControl_Disposed(object sender, EventArgs e)
      {
         foreach (TableItem item in _items)
         {
            item.Dispose();
         }
         repaintHeaderTimer.Dispose();
         if (dividerPen != null)
            dividerPen.Dispose();
      }

      /// <summary>
      /// raises the item dispose event
      /// </summary>
      /// <param name="item"></param>
      internal void onItemDispose(TableItem item)
      {
         TableItemDisposeArgs ea = new TableItemDisposeArgs(item);
         if (ItemDisposed != null)
            ItemDisposed(this, ea);
      }      

      /// <summary> calculate rows in Page depending on the row height, if 
      /// table doesn't have row placement.
      /// </summary>
      /// <param name="forceCompute">true to forcefully compute row in page. </param>
      public virtual void ComputeAndSetRowsInPage(bool forceCompute)
      {
         // In general, if HasRowPlacement is yes, RowsInPage should remain unchanged.
         // In this case, it is RowHeight, which should change.
         // But there is an exception to this law.
         // We get the initial row height from the worker thread and first time, we should 
         // calculate the rows in page depending on this row height (ofcourse, from then 
         // onwards, rows in page will remain unchanged for RowPlacement=Yes).
         // So, when the row height is changed from the worker thread (and not because of 
         // table resize), we should compute RowsInPage forcefully.
         if (RowHeight != 0 && (!HasRowPlacement || forceCompute))
         {
            int tableHeight = Math.Max(0, (ClientRectangle.Height - TitleHeight));
            RowsInPage = tableHeight / RowHeight;
         }
      }

      /// <summary> Calculate and set the RowHeight depending on the no. of rows in page </summary>
      public void ComputeAndSetRowHeight()
      {
         // row height can change only if table HasRowPlacement.
         if (RowsInPage != 0 && HasRowPlacement)
         {
            int tableHeight = Math.Max(0, (ClientRectangle.Height - TitleHeight));
            RowHeight = tableHeight / RowsInPage;
         }
      }

      /// <summary>
      /// get column width from section width
      /// </summary>
      /// <param name="i"></param>
      /// <returns></returns>
      public int GetColumnWidthFromSectionWidth(int i)
      {
         return tableStyleRenderer.GetColumnWidthFromSectionWidth(i, _header);
      }

      /// <summary>
      /// get section width from column width
      /// </summary>
      /// <param name="headerSection"></param>
      /// <param name="index"></param>
      /// <param name="width"></param>
      /// <returns></returns>
      public int GetHeaderSectionWidthByColumnWidth(int index, int width)
      {
         return tableStyleRenderer.GetHeaderSectionWidthFromColumnWidth(index, width);
      }

      /// <summary>
      /// adjust the corner 
      /// </summary>
      /// <param name="rect"></param>
      public void AdjustXCorner(ref Rectangle rect)
      {
         rect.Offset(-_corner.X, 0);
      }

      protected virtual Pen GetLineDividerPen()
      {
         dividerPen.Color = tableStyleRenderer.GetDividerColor();
         return dividerPen;
      }

      public virtual int GetTotalRowDividerHeight()
      {
         return ClientRectangle.Height;
      }

      /// <summary>
      /// returns true if we need to paint themed border
      /// </summary>
      /// <returns></returns>
      private bool HasThemeBorder()
      {
#if !PocketPC //tmp
         return (BorderStyle != BorderStyle.None && _renderer != null && Application.RenderWithVisualStyles && !DesignMode);
#else
         return false;
#endif
      }

      /// <summary>
      /// draw non client area for themed control
      /// </summary>
      /// <param name="msg"></param>
      private void DrawStyledNCArea(ref Message msg)
      {
         IntPtr hdc = NativeWindowCommon.GetWindowDC(msg.HWnd);

         if (hdc != IntPtr.Zero)
         {
            using (Graphics g = Graphics.FromHdc(hdc))
            {
               using (Region r = createBorderRegion())
               {
                  DrawNCArea(ref msg);
                  g.Clip = r;
                  Rectangle bounds = new Rectangle(0, 0, this.Width, this.Height);
#if !PocketPC //tmp
                  if (_renderer != null)
                     _renderer.DrawBackground(g, bounds);
#endif
               }
            }

            NativeWindowCommon.ReleaseDC(msg.HWnd, hdc);
         }
      }

      /// <summary>
      /// create region for border area
      /// </summary>
      /// <returns></returns>
      private Region createBorderRegion()
      {
         Rectangle bounds = new Rectangle(0, 0, this.Width, this.Height);
         Region r = new Region(bounds);
#if !PocketPC
         bounds.Inflate(-SystemInformation.Border3DSize.Width, -SystemInformation.Border3DSize.Height);
#else
         bounds.Inflate(-SystemInformation.BorderSize.Width, -SystemInformation.BorderSize.Height);
#endif
         r.Exclude(bounds);
         return r;
      }

      /// <summary>
      /// translate rectangle to NativeWindowCommon.RECT 
      /// </summary>
      /// <param name="rectangle"></param>
      /// <returns></returns>
      static NativeWindowCommon.RECT toRECT(Rectangle rectangle)
      {
         NativeWindowCommon.RECT rect = new NativeWindowCommon.RECT();
         rect.left = rectangle.Left;
         rect.right = rectangle.Right;
         rect.top = rectangle.Top;
         rect.bottom = rectangle.Bottom;
         return rect;
      }

      /// <summary>
      /// draw NC area that does not include border
      /// </summary>
      /// <param name="msg"></param>
      private void DrawNCArea(ref Message msg)
      {
         Rectangle wRect = this.Parent.RectangleToScreen(this.Bounds);
         NativeWindowCommon.RECT rect = toRECT(wRect);
         Rectangle BorderlessRect = wRect;
#if !PocketPC
         BorderlessRect.Inflate(-SystemInformation.Border3DSize.Width, -SystemInformation.Border3DSize.Height);
#else
         BorderlessRect.Inflate(-SystemInformation.BorderSize.Width, -SystemInformation.BorderSize.Height);
#endif
         IntPtr hRgn = NativeWindowCommon.CreateRectRgnIndirect(ref rect);   // Window Rgn
         rect = toRECT(BorderlessRect);

         IntPtr BorderlessRgn = NativeWindowCommon.CreateRectRgnIndirect(ref rect);   // Borderless window Rgn
         IntPtr hUpdRgn = msg.WParam;

         if (hUpdRgn == (IntPtr)1)   // The entire RGN needs to be painted
            NativeWindowCommon.CombineRgn(hRgn, hRgn, BorderlessRgn, NativeWindowCommon.RGN_AND);
         else
            NativeWindowCommon.CombineRgn(hRgn, hUpdRgn, BorderlessRgn, NativeWindowCommon.RGN_AND);

         //paint the NC area
         msg.LParam = IntPtr.Zero;
         msg.WParam = hRgn;
#if !PocketPC //tmp
         base.WndProc(ref msg);
#endif

         //release
         NativeWindowCommon.DeleteObject(hRgn);
         NativeWindowCommon.DeleteObject(BorderlessRgn);

      }

      /// <summary>
      /// on table paint - send events for cell foreground paint
      /// </summary>
      /// <param name="e"></param>
      protected override void OnPaint(PaintEventArgs e)
      {
         if (!AllowPaint)
            return;


         Brush brush = null;
         Color headerBackColor = Color.Empty;
         if (TitleColor == Color.Empty)
         {
            headerBackColor = defaultHeaderBackColor;
         }
         else
         {
            headerBackColor = Utils.GetNearestColor(e.Graphics, TitleColor);
         }

         brush = SolidBrushCache.GetInstance().Get(headerBackColor);
         // Button control border is painted semi-transparently on its border in XP theme. So ensure, header background is painted
         // and then button is painted, specially for button control on header
         e.Graphics.FillRectangle(brush, new Rectangle(0, 0, Width, TitleHeight));

#if !PocketPC
         ImageAnimator.UpdateFrames();
#endif
         onCellPaintEvents(e, this.PaintItem, false);

         // if dividers must be painted in end , paint them
         if (PaintDidvidersInEnd)
         {
            tableStyleRenderer.DrawBorder(e.Graphics, GetLineDividerPen());
            tableStyleRenderer.PaintDividers(_header, e.Graphics, GetLineDividerPen());
         }
      }

      /// <summary>
      /// table background paint
      /// </summary>
      /// <param name="e"></param>
      protected override void OnPaintBackground(PaintEventArgs e)
      {
         if (!AllowPaint)
            return;

         //Simulate Transparency
         if (IsTransparent || BackColor.A < 255)
         {
#if !PocketPC
            System.Drawing.Drawing2D.GraphicsContainer g = e.Graphics.BeginContainer();
            Rectangle translateRect = this.Bounds;
            e.Graphics.TranslateTransform(-Left, -Top);
            PaintEventArgs pe = new PaintEventArgs(e.Graphics, translateRect);
            this.InvokePaintBackground(Parent, pe);
            this.InvokePaint(Parent, pe);
            e.Graphics.ResetTransform();
            e.Graphics.EndContainer(g);
            pe.Dispose();
#endif
         }

         // If divider must not be painted at end , paint them, now
         if (!PaintDidvidersInEnd)
         {
            tableStyleRenderer.DrawBorder(e.Graphics, GetLineDividerPen());
            tableStyleRenderer.PaintDividers(_header, e.Graphics, GetLineDividerPen());
         }
      }

      /// <summary>
      /// returns row color when altrenating color is active
      /// </summary>
      /// <param name="row"></param>
      /// <returns></returns>
      public Color GetColorbyRow(int row)
      {

         if (IsRowAlternate(row))
            return alternateColor;
         else if (IsColoredByRow && row < _items.Count && _items[row].RowBGColor != Color.Empty && _items[row].IsVisibe)
            return _items[row].RowBGColor;
         else
            return bgColor;

      }

      /// <summary>
      /// paint cell in table
      /// </summary>
      /// <param name="ea"></param>
      /// <param name="paintHandler"></param>
      void PaintCell(Graphics gr, int row, CellData cellData )
      {
         Color color = tableStyleRenderer.GetCellColor(row,cellData.ColumnIdx);

         if (!color.IsEmpty && color != Color.Transparent)
         {
            Brush brush = SolidBrushCache.GetInstance().Get(Utils.GetNearestColor(gr, color));
            gr.FillRectangle(brush, cellData.Rect);
         }

      }

      public virtual bool ShouldCheckAlternateOrColumnColor(int row)
      {
         return true;
      }

      public virtual bool HaveLineDivider(int row)
      {
         return ShowLineDividers;
      }

      /// <summary>
      /// sends paint cell events
      /// </summary>
      /// <param name="e"></param> paint arguments
      /// <param name="paintHandler"></param>  handler to invoke on the cell
      /// <param name="paintTail"></param> if true, treat tail as cell
      /// <returns></returns>
      private void onCellPaintEvents(PaintEventArgs e, TableDrawRowHandler paintHandler, bool paintTail)
      {
            int firstColumn;
            int lastColumn;
            Rectangle clipRect;

#if !PocketPC
            clipRect = new Rectangle((int)e.Graphics.VisibleClipBounds.Left,
                                           (int)e.Graphics.VisibleClipBounds.Top,
                                           (int)e.Graphics.VisibleClipBounds.Right - (int)e.Graphics.VisibleClipBounds.Left,
                                           (int)e.Graphics.VisibleClipBounds.Bottom - (int)e.Graphics.VisibleClipBounds.Top);
#else
            clipRect = e.ClipRectangle;
#endif
            if (rightToLeftLayout)
            {
               firstColumn = findColumnByX(clipRect.Right + _corner.X);
               lastColumn = findColumnByX(clipRect.Left + _corner.X);
            }
            else
            {
               firstColumn = findColumnByX(clipRect.Left + _corner.X);
               lastColumn = findColumnByX(clipRect.Right + _corner.X);
            }

            int firstRow = findRowByY(clipRect.Top);
            int lastRow =  GetLastRow(clipRect.Bottom);

            if (lastColumn == TAIL_COLUMN_IDX)
            {
               paintTail = (_corner.X == 0);
               lastColumn = _columns.Count - 1;
               if (firstColumn == -1)
                  firstColumn = lastColumn;
            }
            else
               paintTail = false; //tail is not in cliiped area

            firstRow = Math.Max(0, firstRow);
            firstColumn = Math.Max(0, firstColumn);
            for (int row = firstRow; row <= lastRow; row++)
            {
               TableItem item = null;
               if (row < _items.Count)
                  item = _items[row];
               TablePaintRowArgs ea = new TablePaintRowArgs(e.Graphics,  row, item, GetRowRect(row));
               for (int col = firstColumn; col <= lastColumn; col++)
               {
                  //create arguments and send event for table cells
                  TableColumn column = _columns[col];
                  Rectangle rect = getCellRect(col, row);
                  CellData c = new CellData(column.Index, rect);
                  PaintCell(e.Graphics, row, c);
                  ea.addCellData(c);
               }
               if (paintTail)
               {
                  //create arguments and send event for table tail cells
                  Rectangle rect = getCellRect(TAIL_COLUMN_IDX, row);
                  CellData c = new CellData(TAIL_COLUMN_IDX, rect);
                  PaintCell(e.Graphics, row, c);
                  ea.addCellData(c);
               }

               if (paintHandler != null)
                  paintHandler(this, ea);
              
            }
      }

      protected virtual int GetLastRow(int y)
      {
         return findRowByY(y);
      }

      void header_BeforeSectionTrack(object sender, HeaderSectionWidthConformableEventArgs ea)
      {
         ea.Accepted = AllowColumnResize;
#if !PocketPC
         if (ea.Accepted)
         {
            TableColumn column = getHeaderColumn(ea.Item);
            if (BeginTrack != null)
               BeginTrack(this, new TableColumnArgs(column));

         }
#endif
      }

      /// <summary>
      /// find column to which X coordinate belongs, return LAST_COLUMN if it is in tail
      /// </summary>
      /// <param name="x"></param>
      /// <returns></returns>
      public int findColumnByX(int x)
      {
         int offset;
         return findColumnByX(x, out offset);
      }
         
      public int findColumnByX(int x, out int offsetInColumn )
      {
         if (rightToLeftLayout)
            x = mirrorXcoordinate(x);
         if (x < 0)
            x = 0;
         int columnStart = 0;
         for (int i = 0; i < _columns.Count; i++)
         {
            if (x <= columnStart + _columns[i].Width)
            {
               offsetInColumn = x - columnStart ;
               return i;
            }
            columnStart += _columns[i].Width;
         }
         offsetInColumn = -1;
         return TAIL_COLUMN_IDX;
      }

      

      /// <summary>
      /// find row on screen to which Y coordinate belongs
      /// </summary>
      /// <param name="y"></param>
      /// <returns></returns>
      public int findRowByY(int y)
      {
         if (y < _header.Height)
            return -1; // click on header
         return (_topIndex + (RowHeight > 0 ? (y - _header.Height) / RowHeight : 0));
      }

      /// <summary>
      /// get row rectangle
      /// </summary>
      /// <param name="row"></param>
      /// <returns></returns>
      public Rectangle GetRowRect(int row)
      {
         Rectangle rect = new Rectangle();
         rect.X = 0;
         rect.Y = _header.Height + (row - _topIndex) * RowHeight;
         rect.Height = RowHeight;
         for (int i = 0; i < _columns.Count; i++)
            rect.Width += _columns[i].Width;
         rect.Width = Math.Max(rect.Width, ClientRectangle.Width);
         rect.Offset(-_corner.X, 0);

         return rect;

      }

      /// <summary>
      /// return column rectangle
      /// </summary>
      /// <param name="col"></param>
      /// <returns></returns>
      public Rectangle GetColumnRectangle(int col, bool forLineArea)
      {
         return tableStyleRenderer.GetColumnRectangle(_header, col, forLineArea);
      }
      /// <summary>
      /// Return Header rectangle
      /// </summary>
      /// <returns></returns>
      public Rectangle GetHeaderRectangle()
      {
#if PocketPC
         return _header.Bounds;
#else
         return _header.DisplayRectangle;
#endif
      }

      /// <summary>
      /// Show indicator on Header
      /// </summary>
      /// <param name="designIndicator"></param>
      public void ShowIndicatorOnHeader(Control designIndicator)
      {
         designIndicator.Parent = _header;
      }

      /// <summary>
      /// calculates screen rectangle
      /// </summary>
      /// <param name="col"></param> gui column, if LAST_COLUMN - calc cell tail rectangle
      /// <param name="row"></param> row on screen
      /// <returns></returns>
      public Rectangle getCellRect(int col, int row)
      {
         Rectangle rect = GetColumnRectangle(col, true);
         rect.Y = tableStyleRenderer.GetCellTop(_topIndex, row);
         rect.Height = RowHeight;
         if (HaveLineDivider(row))
         {
            if (col == TAIL_COLUMN_IDX || _columns[col].HeaderSection.TopBorder)
               rect.Height--;
         }
         return rect;
      }

      /// <summary>
      /// mirror X coord
      /// </summary>
      /// <param name="x"></param>
      /// <returns></returns>
      private int mirrorXcoordinate(int x)
      {
         return _displayWidth - x;
      }

      /// <summary>
      /// translate x coord
      /// </summary>
      /// <param name="x"></param>
      /// <returns></returns>
      private int tranlateXCoordinate(int x)
      {
         if (rightToLeftLayout)
            return _displayWidth - x;
         else
            return x;
      }

      /// <summary>
      /// get the x coordinate of column for painting
      /// </summary>
      /// <param name="columnPos"></param>
      /// <returns></returns>
      internal int GetColumnX(int columnPos)
      {
         return tranlateXCoordinate(columnPos) - _corner.X;
      }

      /// <summary>
      /// mirror rectangle
      /// </summary>
      /// <param name="rect"></param>
      public void mirrorRectangle(ref Rectangle rect)
      {
         rect.X = mirrorXcoordinate(rect.X + rect.Width);
      }

      /// <summary>
      /// return bounds of item
      /// </summary>
      /// <param name="item"></param>
      /// <returns></returns>
      internal Rectangle getItemBounds(TableItem item)
      {
         int row = item.Idx - _topIndex;
         Rectangle rect = new Rectangle(0, _header.Height + row * RowHeight, ClientRectangle.Width, RowHeight);
         if (HaveLineDivider(row))
            rect.Height--;
         return rect;

      }

      /// <summary>
      /// handle table layout
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void TableControl_Layout(object sender, LayoutEventArgs e)
      {
         if (!Visible)
            return;
         SuspendLayout();
         updateVScroll(true);
         adjustHeader();
         updateHScroll();
         NativeWindowCommon.RedrawWindow(this.Handle, IntPtr.Zero, IntPtr.Zero, NativeWindowCommon.RedrawWindowFlags.RDW_FRAME |
                                         NativeWindowCommon.RedrawWindowFlags.RDW_INVALIDATE);
         ResumeLayout(false);
      }

      private void TableControl_Scroll(object sender, ScrollEventArgs e)
      {
      }


#if !PocketPC
      protected override void WndProc(ref Message m)
#else
      protected void WndProc(ref Message m)
#endif
      {
         switch (m.Msg)
         {
            case NativeWindowCommon.WM_CONTEXTMENU:
                  if (!this.ShowContextMenu)
                     return;
                  break;
            case NativeWindowCommon.WM_VSCROLL:
               if (m.HWnd == this.Handle)
               {

                  OnVScroll(ref m);
                  return;
               }
               break;

            case NativeWindowCommon.WM_HSCROLL:
               if (m.HWnd == this.Handle)
               {
                  OnHScroll(ref m);
                  return;
               }
               break;
            case NativeWindowCommon.WM_NCPAINT:
               if (HasThemeBorder())
               {
                 
                  DrawStyledNCArea(ref m);
                  return;
               }
               break;

            case NativeWindowCommon.WM_NCLBUTTONDOWN:
            case NativeWindowCommon.WM_NCRBUTTONDOWN:
               OnNCMouseDown(m);
               break;
            case NativeWindowCommon.WM_PARENTNOTIFY:
               OnParentNotify(m);
               break;
              
         }
#if !PocketPC
         base.WndProc(ref m);
        

#else
         // Call original proc
         NativeWindowCommon.CallWindowProc(OrigWndProc, m.HWnd, (uint)m.Msg, (uint)m.WParam, m.LParam.ToInt32());
#endif
      }

      /// <summary>
      /// handle parent notify event
      /// </summary>
      /// <param name="m"></param>
      void OnParentNotify(Message m)
      {
         int wParam = (int)m.WParam ;
         if (wParam == NativeWindowCommon.WM_LBUTTONDOWN || wParam == NativeWindowCommon.WM_RBUTTONDOWN)
            CreateMouseDownEventOnHeaderClick(m);
      }

      /// <summary>
      /// when click is on header, raise mouse down event for table
      /// </summary>
      /// <param name="m"></param>
      private void CreateMouseDownEventOnHeaderClick(Message m)
      {
#if !PocketPC
         Point point = new Point(NativeWindowCommon.LoWord((int)m.LParam), NativeWindowCommon.HiWord((int)m.LParam));
         TableHitTestResult hitTestResult = HitTest(point);
         if (hitTestResult.Area == TableHitTestArea.OnHeader)
         {
            MouseButtons button = (int)m.WParam == NativeWindowCommon.WM_LBUTTONDOWN ? MouseButtons.Left : MouseButtons.Right;
            MouseEventArgs mouseEventArgs = new MouseEventArgs(button, 0, point.X, point.Y, 0);
            this.OnMouseDown(mouseEventArgs);
         }
#endif
      }
      
      private void TableControl_Resize(object sender, EventArgs e)
      {
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="idx"></param>
      /// <returns></returns>
      public TableItem getItem(int idx)
      {
         Debug.Assert(idx >= 0 && idx < _items.Count);
         return _items[idx];
      }

      /// <summary>
      /// Gets header background color
      /// </summary>
      /// <returns></returns>
      public Color GetHeaderBGColor()
      {
         return _header.TitleColor;
      }

      /// <summary>
      /// Checks if control is located on header area
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      public bool IsControlOnHeaderArea(Control control)
      {
         return control.Top - Top < TitleHeight;
      }


      /// <summary>
      /// Get background color to use
      /// </summary>
      /// <returns></returns>
      public Color GetBGColorToUse(Control control)
      {
         return IsColoredHeaderChild(control) && ((MgLabel)control).IsTransparentWhenOnHeader ? _header.TitleColor : control.BackColor;
      }

      /// <summary>
      /// Check if table header is colored
      /// </summary>
      /// <returns></returns>
      private bool IsColoredHeaderChild(Control control)
      {
         return IsControlOnHeaderArea(control) && (_header.TitleColor != Color.Empty);
      }

      /// <summary>
      /// Invalidates all label header controls
      /// </summary>
      public void RefreshAllHeaderLabelControls()
      {
         foreach (Control control in Controls)
         {
            if (IsControlOnHeaderArea(control) && (control is Label))
               control.Invalidate();
         }
      }

      /// <summary>
      /// gets items count
      /// </summary>
      /// <returns></returns>
      public int getItemsCount()
      {
         return _items.Count;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="idx"></param>
      /// <returns></returns>
      public TableColumn getColumn(int idx)
      {
         return _columns[idx];
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="item"></param>
      /// <returns></returns>
      TableColumn getHeaderColumn(HeaderSection item)
      {
         return _columns[item.Index];
      }

      /// <summary>
      /// find column by header section
      /// </summary>
      /// <param name="item"></param>
      /// <returns></returns>
      TableColumn getColumnByHeaderSection(HeaderSection item)
      {
         foreach (var column in _columns)
         {
            if (column.HeaderSection == item)
               return column;
         }
         return null;
      }

      /// <summary>
      /// on drag divider end
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="ea"></param>
      protected virtual void header_AfterSectionTrack(object sender, HeaderSectionWidthEventArgs ea)
      {
         SuspendLayout();
         AllowPaint = false;
         adjustHeader();
         updateHScroll();
         TableColumn column = getHeaderColumn(ea.Item);
         column.onAfterColumnTrack();
         if (rightToLeftLayout)
            MoveColumns(0);
         else
            MoveColumns(column.Index + 1);
         AllowPaint = true;
         ResumeLayout(true);
         this.Invalidate();
      }


      /// <summary>
      /// move all columns starting from startIndex
      /// </summary>
      /// <param name="startIndex"></param>
      public void MoveColumns(int startIndex)
      {
         for (int i = startIndex; i < _columns.Count; i++)
            _columns[i].Move();
      }

      /// <summary> set table's background color and transparency
      /// </summary>
      /// <param name="color"></param>
      private void UpdateBgColorAndTransparency()
      {
         //is color transparent
         //!!!
         if (IsMagicBackGroundColorTransparent && (alternateColor.IsEmpty || colorBy == TableColorBy.Column))
            BgColor = Color.Transparent;            
         else   
            BgColor = MagicBgColor;

         IsTransparent = (BgColor == Color.Transparent);
         Invalidate();
         
      }

      /// <summary> returns true if control has background color
      /// </summary>
      /// <returns></returns>
      public bool HasAlternateColor
      {
         get
         {
            return colorBy == TableColorBy.Table && !alternateColor.IsEmpty;
         }
         
      }

      /// <summary> returns true if this row should be painted with alternated color
      /// </summary>
      /// <param name="guiRow"></param>
      /// <returns></returns>
      internal bool IsRowAlternate(int guiRow)
      {
         bool ret = false;
         if (HasAlternateColor)
         {
            ret = _startTablePaintWithAlternateColor;
            if (guiRow % 2 == 1)
               ret = !ret;
         }
         return ret;
      }

      public void ToggleAlternateColorForFirstRow()
      {
         _startTablePaintWithAlternateColor = !_startTablePaintWithAlternateColor;
      }

     
      

      /// <summary>
      /// translate mousewheel to scroll
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void TableControl_MouseWheel(object sender, MouseEventArgs e)
      {
#if !PocketPC //tmp
         int delta = SystemInformation.MouseWheelScrollLines;
         if (e.Delta > 0)
            delta *= -1;
         ScrollVerticallyMouseWheel(delta);
#else
         throw new NotImplementedException();
#endif
      }

      #region abstract and virtual functions

      /// <summary>
      /// set the virtual list size
      /// </summary>
      /// <param name="size"></param>
      public abstract void SetVirtualItemsCount(int size);

      /// <summary>
      /// set the items count
      /// </summary>
      /// <param name="size"></param>
      public abstract void SetItemsCount(int size);

      /// <summary>
      /// gets the thumb track value
      /// </summary>
      /// <param name="thumbTrackVal"></param>
      /// <returns></returns>
      protected abstract int GetThumbTrackVal(int thumbTrackVal);

      /// <summary>
      /// scroll vertical scroll by rows 'rowsToScroll'
      /// </summary>
      /// <param name="rowsToScroll">number of rows to scroll</param>
      protected abstract void ScrollVerticallyMouseWheel(int rowsToScroll);

      /// <summary>
      /// gets the previous scroll value
      /// </summary>
      /// <param name="thumbPos">the new thumb position</param>
      /// <returns></returns>
      protected abstract int GetPrevScrollVal(int thumbPos);

      /// <summary>
      /// insert Item at position idx.
      /// </summary>
      /// <param name="idx"></param>
      public virtual void InsertItem(int idx)
      {
         TableItem item = new TableItem(idx, this);
         _items.Insert(idx, item);
         int count = _items.Count;
         for (int i = idx; i < count; i++)
            _items[i].Idx = i;
      }

      /// <summary>
      ///  remove Item at position idx.
      /// </summary>
      /// <param name="idx"></param>
      public virtual void RemoveItem(int idx)
      {
         _items.RemoveAt(idx);
         int count = _items.Count;
         for (int i = idx; i < count; i++)
            _items[i].Idx = i;
      }

      /// <summary>
      /// set the Vertical scroll thumb position
      /// </summary>
      /// <param name="pos">the position to set</param>
      public virtual void SetVScrollThumbPos(int pos)
      {
      }
      
      /// <summary>
      /// returns the top index using scroll bar position which also includes the recordsBeforeCurrentView
      /// </summary>
      public virtual int GetTopIndexFromScrollbarPosition(int scrollbarPosition)
      {
         return scrollbarPosition;
      }

      /// <summary>
      /// set the page size for Vertical scroll bar
      /// </summary>
      /// <param name="pageSize">the page size to set</param>
      public virtual void SetVScrollPageSize(int pageSize)
      {
      }

      /// <summary>
      /// sets the previous scroll value
      /// </summary>
      /// <param name="thumbPos">the new thumb position</param>
      protected virtual void SetPrevScrollVal(int thumbPos)
      {
      }

      /// <summary>
      /// called when the thumb dragging has ended
      /// </summary>
      protected virtual void StartThumbDrag()
      {
         _isThumbDrag = true;
      }

      /// <summary>
      /// called when the thumb dragging has ended
      /// </summary>
      protected virtual void EndThumbDrag()
      {
         _isThumbDrag = false;
      }

      /// <summary>
      /// vertical table scroll
      /// </summary>
      /// <param name="scrollEventType">Scroll Event Type</param>
      /// <param name="newPos">the new position</param>
      /// <param name="oldPos">the old position</param>
      /// <param name="scrollWindow">true, if we need to scroll the window right away</param>
      protected virtual void ScrollVertically(ScrollEventType scrollEventType, int newPos, int oldPos, bool scrollWindow)
      {
         if (scrollWindow)
         {
            ScrollEventArgs arg = new ScrollEventArgs(scrollEventType, oldPos, newPos, ScrollOrientation.VerticalScroll);
            this.OnScroll(arg);
         }
         _corner.Y = getYCorner();
      }

      /// <summary>
      /// raise event on non client mouse down
      /// </summary>
      
      protected virtual void OnNCMouseDown(Message m)
      {
         if (NCMouseDown != null)
            NCMouseDown(this, new NCMouseEventArgs(m));
      }
      /// <summary>
      /// sets Top Index of table
      /// </summary>
      /// <param name="value">new top index</param>
      /// <param name="scrollWindow">if true, table is scrolled. Otherwise, it is user's responsibility
      ///  to refresh the table
      /// </param>
      public virtual bool SetTopIndex(int value, bool scrollWindow)
      {
         return false;
      }

      /// <summary>
      /// gets the vertical scroll thumb position
      /// </summary>
      /// <returns></returns>
      public virtual int GetVScrollThumbPos()
      {
         Debug.Assert(false);
         return -1;
      }
    
#if !PocketPC
      /// <summary>
      /// returns location of the hit on the table
      /// </summary>
      /// <param name="pt"></param>
      /// <returns></returns>
      public TableHitTestResult HitTest(Point pt)
      {
         TableHitTestResult hitTestResult = new TableHitTestResult();
         if (this._header.DisplayRectangle.Contains(pt))
            hitTestResult.Area = TableHitTestArea.OnHeader;
         else if (!this.ClientRectangle.Contains(pt))
            hitTestResult.Area = TableHitTestArea.OnNonClientArea;
         else
            hitTestResult.Area = TableHitTestArea.OnColumn;
         //find column
         int offsetInColumn;
         int columnIndex = findColumnByX(getXCorner() + pt.X, out offsetInColumn);
         if (columnIndex != TAIL_COLUMN_IDX)
            hitTestResult.TableColumn = Columns[columnIndex];

         return hitTestResult;
      }

#endif
      #endregion

#if PocketPC
      // Create our own Scroll handler
      public delegate void ScrollEventHandler(Object sender, ScrollEventArgs e);

      public event ScrollEventHandler Scroll;

      protected void OnScroll(ScrollEventArgs se)
      {
         TableControl_Scroll(this, se);
         // Call the scroll handler, if exists
         if (Scroll != null)
            Scroll(this, se);
      }

      public void Invalidate(Rectangle rc, bool invalidateChildren)
      {
         Invalidate(rc);
      }

      public void PerformLayout(Control affectedControl, string affectedProperty)
      {
         PerformLayout();
      }

      public void PerformLayout()
      {
         LayoutEventArgs e = new LayoutEventArgs();
         TableControl_Layout(this, e);
      }

      class SystemPens
      {
         static SystemPens()
         {
            ControlLight = new Pen(SystemColors.InactiveBorder);
         }
         public static Pen ControlLight { get; set; }
      }
#endif

      #region INotifyCollectionChanged Members

      public event NotifyCollectionChangedEventHandler CollectionChanged;

      #endregion

      protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
      {
         if (this.CollectionChanged != null)
         {
            this.CollectionChanged(this, e);

         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="line"></param>
      /// <param name="color"></param>
      public void SetRowBGColor(int line, Color color)
      {
         if(line < _items.Count)
            _items[line].RowBGColor = color;
      }

#if !PocketPC
      /// <summary>
      /// this method is invoked from the designer verb "Add Column"
      /// it adds column to the table
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      internal protected virtual void OnDesignerAddColumn(object sender, System.EventArgs e)
      {
         TableColumn column;
         IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost));
         DesignerTransaction designerTransaction;
         IComponentChangeService componentChangeService = (IComponentChangeService)GetService(typeof(IComponentChangeService));

         // Add a new button to the collection
         designerTransaction = designerHost.CreateTransaction("Add Column");
         column = (TableColumn)designerHost.CreateComponent(typeof(TableColumn));
         componentChangeService.OnComponentChanging(this, null);
         this.Columns.Add(column);
         componentChangeService.OnComponentChanged(this, null, null, null);
         designerTransaction.Commit();
      }
      /// <summary>
      /// this method is invoked from the designer verb "Remove Column"\
      /// it removes column to the table
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      internal protected virtual void OnDesignerRemoveColumn(object sender, System.EventArgs e)
      {
         TableColumn column;
         if (Columns.Count > 0)
         {
            IDesignerHost designerHost = (IDesignerHost)GetService(typeof(IDesignerHost));
            DesignerTransaction designerTransaction;
            IComponentChangeService componentChangeService = (IComponentChangeService)GetService(typeof(IComponentChangeService));

            // Add a new button to the collection
            designerTransaction = designerHost.CreateTransaction("Remove Column");
            column = Columns[Columns.Count - 1];
            componentChangeService.OnComponentChanging(this, null);
            designerHost.DestroyComponent(column);
            Columns.Remove(column);
            componentChangeService.OnComponentChanged(this, null, null, null);
            designerTransaction.Commit();
         }
      }

      public void OnComponentChanged()
      {
         IComponentChangeService componentChangeService = (IComponentChangeService)GetService(typeof(IComponentChangeService));
         if (componentChangeService != null)
            componentChangeService.OnComponentChanged(this, null, null, null);
      }

      
      public void OnDesignerDragEnter(DragEventArgs drgevent)
      {
         IsDragging = true;
         if (DesignerDragEnter != null)
            DesignerDragEnter(this, drgevent);
      }


      public void OnDesignerDragOver(DragEventArgs drgevent)
      {
         if (DesignerDragOver != null)
            DesignerDragOver(this, drgevent);
      }

      public void OnDesignerDragStop()
      {
         if (DesignerDragStop != null)
            DesignerDragStop(this, new EventArgs());
         IsDragging = false;
      }
     
#endif
      #region ICanParent members
      public event CanParentDelegate CanParentEvent;

      public bool CanParent(CanParentArgs allowDragDropArgs)
      {
         if (CanParentEvent != null)
            return CanParentEvent(this, allowDragDropArgs);
         return true;
      }
      #endregion


#if !PocketPC

      #region IMgContainer
      public event ComponentDroppedDelegate ComponentDropped;

      public virtual void OnComponentDropped(ComponentDroppedArgs args)
      {
         if (ComponentDropped != null)
            ComponentDropped(this, args);
      }
      #endregion

#endif

   }

  

}

/// <summary>
/// This internal class is outside the namespace & toolbox image is inside the Controls.Resources namespace.
/// </summary>
internal class ResourceFinder
{

}
