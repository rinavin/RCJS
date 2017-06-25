using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using com.magicsoftware.controls.designers;
using com.magicsoftware.controls.utils;
using com.magicsoftware.util;
using com.magicsoftware.win32;
using Controls.com.magicsoftware.support;

#if !PocketPC
using System.ComponentModel.Design;
using System.Security.Permissions;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Text;
using System.Collections;
#else
using System.Runtime.InteropServices;
using TabAlignment = com.magicsoftware.mobilestubs.TabAlignment;
using Message = com.magicsoftware.mobilestubs.Message;
#endif

namespace com.magicsoftware.controls
{


#if !PocketPC
   [Designer(typeof(MgTabControlRuntimeDesigner))]
   [ToolboxBitmap(typeof(TabControl))]
   public class MgTabControl : TabControl, IMultilineProperty, IRightToLeftProperty, IGradientColorProperty, ICanParent, IMgContainer, IChoiceControl, IFontOrientation
#else
   public class MgTabControl : TabControl, IMultilineProperty, IGradientColorProperty
#endif
   {
      private const int DEFAULT_TAB_TEXT_OFFSET = 10;
      private const int DEFAULT_TAB_IMAGE_OFFSET = 5;

      private int tabTextOffset = DEFAULT_TAB_TEXT_OFFSET;
      private int tabImageOffset = DEFAULT_TAB_IMAGE_OFFSET;

      public Rectangle InitialDisplayRectangle { get; set; }

      private static int SystemDefaultPadding;

      /// <summary>
      /// Delegate for MnemonicKeyPressedEvent that accepts tabControl & selectedTabIndex as an arguments.
      /// </summary>
      /// <param name="tabControl"></param>
      /// <param name="selectedTabIndex"></param>
      public delegate void MnemonicKeyPressedEventHandler(object sender, MnemonicKeyPressedEventArgs e);

      /// <summary>
      /// The event is raised when Mnemonic key is pressed on Tab control.
      /// </summary>
      public event MnemonicKeyPressedEventHandler MnemonicKeyPressed;

      static MgTabControl()
      {
         TabControl tabControl = new TabControl();

         SystemDefaultPadding = tabControl.Padding.X;

         tabControl.Dispose();
      }

#if !PocketPC

      #region IRightToLeftProperty Members

      public override RightToLeft RightToLeft
      {
         get
         {
            return base.RightToLeft;
         }
         set
         {
            base.RightToLeft = value;

#if  !PocketPC
            UpdateStyles();
#endif
         }
      }

      public override bool RightToLeftLayout
      {
         get
         {
            // When Right to left is set, the tabs must be painted from Right in case of Top and Bottom .
            // But for tab control side if Left/Right  the tabs must not be inverted .
            // So set the Layout only when RTL is true and tab side is top or Bottom.
            return (RightToLeft == RightToLeft.Yes && Alignment <= TabAlignment.Bottom) ? true : false;
         }
         set
         {
            // should not set RightToLeftLayout directly
         }
      }

      #endregion


      #region IMultilineProperty Members

      private bool multiline;
      public new bool Multiline
      {
         get
         {
            return base.Multiline;
         }
         set
         {
            if (multiline != value)
            {
               multiline = value;
               UpdateMultiLine();
            }
         }
      }

      #endregion

      public new TabAlignment Alignment
      {
         get 
         {
            return base.Alignment;
         }
         set 
         {
            if (base.Alignment != value)
            {
               base.Alignment = value;
               UpdateMultiLine();
            }
         }
      }
#endif

      #region IGradientColorProperty Members

      public GradientColor GradientColor { get; set; }
      public GradientStyle GradientStyle { get; set; }

      #endregion

      private int MaxTextWidth { get; set; }
      public bool IsBasePaint { get; set; }

      private int iconWidth_DO_NOT_USE_DIRECTLY;
      public int IconWidth
      {
         get { return iconWidth_DO_NOT_USE_DIRECTLY; }
         set
         {
            if (iconWidth_DO_NOT_USE_DIRECTLY != value)
            {
               iconWidth_DO_NOT_USE_DIRECTLY = value;
               RefreshItemSize();
            }
         }
      }

      public override Font Font
      {
         get
         {
            return base.Font;
         }
         set
         {
            if (base.Font != value)
            {
               base.Font = value;
               UpdateMaxTextWidth();
               if (this.IsHandleCreated)
                  SetNativeFont();

#if PocketPC
               // If the font changed, we need to recalculate the new tab offset
               CalcOffset();
#endif
            }
         }
      }

      private Color pageColor;
#if !PocketPC
      [Category("Appearance")]
#endif
      public Color PageColor
      {
         get
         {
            return pageColor;
         }
         set
         {
            if (pageColor != value)
            {
               pageColor = value;
               if (SelectedIndex != -1)
                  TabPages[SelectedIndex].Invalidate();

               Invalidate(true);
            }
         }
      }

      public bool IsDragging { get; set; }

      /// <summary>
      /// Denote the TabsWidth of Tab Control 
      /// Will have - Fit to text , Fixed, Fixed in line , Fill in line options
      /// This is internally set SizeMode of Tab control
      /// </summary>
      private TabControlTabsWidth tabsWidth;
      public TabControlTabsWidth TabsWidth
      {
         get
         {
            return tabsWidth;
         }
         set
         {
            if (tabsWidth != value)
            {
               tabsWidth = value;
               UpdateMultiLine();
               SetTabSizeMode();
            }
         }
      }

#if !PocketPC
      #region Titlecolor related members

      #region properties

      internal TabStyleProvider DisplayStyleProvider
      {
         get;
         private set;
      }

      protected override CreateParams CreateParams
      {
         [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
         get
         {
            CreateParams cp = base.CreateParams;
            if (this.RightToLeftLayout)
               cp.ExStyle = cp.ExStyle | NativeWindowCommon.WS_EX_LAYOUTRTL | NativeWindowCommon.WS_EX_NOINHERITLAYOUT;
            return cp;
         }
      }


      private Color titleColor = Color.Empty;
      public Color TitleColor
      {
         get { return titleColor; }
         set
         {
            if (titleColor != value)
            {
               titleColor = value;

               this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, titleColor != Color.Empty);


               if (titleColor.IsEmpty)
                  fUpDown.ReleaseHandle();
               else if (!titleColor.IsEmpty )
               {
                  RecreateBuffers();

                  if (fUpDown.Handle == IntPtr.Zero)
                     fUpDown.AssignHandle(upDownHandle);
               }

               UpdateHotTrack(); // Update the hot track property  
               Invalidate();
            }
         }
      }

      private Color titleFgColor = Color.Empty;
      public Color TitleFgColor
      {
         get { return titleFgColor; }
         set
         {
            if (titleFgColor != value)
            {
               titleFgColor = value;
               Invalidate();
            }
         }
      }

      private Color hotTrackColor = Color.Empty;
      public Color HotTrackColor
      {
         get { return hotTrackColor; }
         set
         {
            if (hotTrackColor != value)
            {
               hotTrackColor = value;
               Invalidate();
            }
         }
      }

      private Color hotTrackFgColor = Color.Empty;
      public Color HotTrackFgColor
      {
         get { return hotTrackFgColor; }
         set
         {
            if (hotTrackFgColor != value)
            {
               hotTrackFgColor = value;
               Invalidate();
            }
         }
      }

      /// <summary>
      /// Denotes the color of selected tab
      /// </summary>
      private Color selectTabColor = Color.Empty;
      public Color SelectedTabColor
      {
         get { return selectTabColor; }
         set
         {
            if (selectTabColor != value)
            {
               selectTabColor = value;
               Invalidate();
            }
         }
      }

      /// <summary>
      /// Denotes the foreground color of selected tab
      /// </summary>
      private Color selectTabFgColor = Color.Empty;
      public Color SelectedTabFgColor
      {
         get { return selectTabFgColor; }
         set
         {
            if (selectTabFgColor != value)
            {
               selectTabFgColor = value;
               Invalidate();
            }
         }
      }


      /// <summary>
      /// Determines if tabs should be hot tracked
      /// </summary>
      private bool hotTrack;
      public new bool HotTrack
      {
         get
         {
            return base.HotTrack;
         }
         set
         {
            if (hotTrack != value)
            {
               hotTrack = value;
               UpdateHotTrack();
            }
         }
      }

      public int ActiveIndex
      {
         get
         {
            Point mousePosition = this.PointToClient(Control.MousePosition);

            // If the  mouse is on scroll of tab , don't show hot track
            // Don't show hottrack while opening context menu
            if (!TabUpDownRect.IsEmpty && TabUpDownRect.Contains(mousePosition) || contextMenuOpening)
               return -1;

            NativeWindowCommon.TCHITTESTINFO hitTestInfo = new NativeWindowCommon.TCHITTESTINFO(mousePosition);
            int index = NativeWindowCommon.SendMessage(this.Handle, NativeWindowCommon.TCM_HITTEST, 0, ref hitTestInfo);
            return index;
         }
      }

      public new Point MousePosition
      {
         get
         {
            if (titleColor != Color.Empty)
            {
               Point loc = this.PointToClient(Control.MousePosition);

               if (this.RightToLeftLayout)
                  loc.X = (this.Width - loc.X);

               return loc;
            }
            else
               return Control.MousePosition;
         }
      }

      private int titlePadding;
      public int TitlePadding
      {
         get
         {
            return titlePadding;
         }

         set
         {
            if (titlePadding != value)
            {
               titlePadding = value;
               UpdatePadding();
            }
         }
      }

      public int FontOrientation { get; set; }

      #endregion

      #region methods

      /// <summary>
      /// Raise an event when Mnemonic key pressed.
      /// </summary>
      /// <param name="selectedtabIndex"></param>
      internal void OnMnemonicKeyPressed(MnemonicKeyPressedEventArgs mnemonicEventArgs)
      {
         if (MnemonicKeyPressed != null)
            MnemonicKeyPressed(this, mnemonicEventArgs);
      }

      public void UpdateMultiLine()
      {
         //for Left and Right tabs, the multiline needs to be true.
         //for Left and Right tabs, the multiline needs to be true.
         if (Alignment == TabAlignment.Left || Alignment == TabAlignment.Right)
            base.Multiline = true;
         else
            // For top and botton when tabsWidth is FixedInLine multiline must be false to avoid flickering.
            base.Multiline = (TabsWidth == TabControlTabsWidth.FixedInLine) ? false : multiline;
      }

      /// <summary>
      /// update the hot track property of Control
      /// </summary>
      private void UpdateHotTrack()
      {
         // when title color is not empty, hot track must always be true for proper painting
         base.HotTrack = TitleColor != Color.Empty ? true : hotTrack;
      }

      internal void PaintTransparentBackground(Graphics graphics)
      {
         if ((Parent != null))
         {
            //	Set the cliprect to be relative to the parent
            ClientRectangle.Offset(Location);

            //	Save the current state before we do anything.
            GraphicsState state = graphics.Save();

            //	Set the graphicsobject to be relative to the parent
            graphics.TranslateTransform((float)-Location.X, (float)-Location.Y);
            graphics.SmoothingMode = SmoothingMode.HighSpeed;

            //	Paint the parent
            PaintEventArgs e = new PaintEventArgs(graphics, ClientRectangle);
            try
            {
               InvokePaintBackground(Parent, e);
               InvokePaint(Parent, e);
            }
            finally
            {
               //	Restore the graphics state and the clipRect to their original locations
               graphics.Restore(state);
               ClientRectangle.Offset(-Location.X, -Location.Y);
            }
         }
      }

      #endregion

      #endregion
#endif

      /// <summary>
      /// return the designer host
      /// </summary>
      /// <returns></returns>
#if !PocketPC
      public IDesignerHost GetDesignerHost()
      {
         return (IDesignerHost)GetService(typeof(IDesignerHost));
      }
#endif

      /// <summary>
      /// Update Control's Padding based on TitlePadding
      /// </summary>
      private void UpdatePadding()
      {
         if (TitlePadding > 0)
         {
            this.Padding = new Point(TitlePadding, Padding.Y);
            this.tabTextOffset = TitlePadding * 2;
            this.tabImageOffset = TitlePadding;
         }
         else
         {
            this.Padding = new Point(SystemDefaultPadding, Padding.Y);
            this.tabTextOffset = DEFAULT_TAB_TEXT_OFFSET;
            this.tabImageOffset = DEFAULT_TAB_IMAGE_OFFSET;
         }

         RefreshItemSize();
      }

      /// <summary>
      /// Set the SizeMode of control from tabsWidth
      /// </summary>
      private void SetTabSizeMode()
      {
         TabSizeMode tabSizeMode = TabSizeMode.Normal;
         switch (TabsWidth)
         {
            case TabControlTabsWidth.FitToText:
               tabSizeMode = TabSizeMode.Normal;
               break;
            case TabControlTabsWidth.Fixed:
            case TabControlTabsWidth.FixedInLine:
               tabSizeMode = TabSizeMode.Fixed;
               break;
            case TabControlTabsWidth.FillToRight:
               tabSizeMode = TabSizeMode.FillToRight;
               break;
            default:
               Debug.Assert(false);
               break;
         }
         SizeMode = tabSizeMode;
      }

      /// <summary></summary>
      public void UpdateMaxTextWidth()
      {
         int maxTextWidth = 0;

         //find the max text on tab page
         foreach (TabPage tabPage in TabPages)
         {
            Size textExt = Utils.GetTextExt(Font, tabPage.Text, tabPage);
            maxTextWidth = Math.Max(textExt.Width, maxTextWidth);
         }

         MaxTextWidth = maxTextWidth;
         RefreshItemSize();
      }

      /// <summary></summary>
      private void RefreshItemSize()
      {
         if (tabsWidth == TabControlTabsWidth.FixedInLine)
         {
            int widthofTab = 0;
            if (TabPages.Count > 0)
            {
               // When TabWidth== FixedInLine  as the tabs are equally distributed ,width of tab is TabControl.Width / no of tabs .
               // But ,the tab starts from X= 2 . So we need to adjust this in width otherwise tab scroll appears .
               // Also we need to leave space of 1 pixel more because if tabWidth is exactly same as TabControl.Width still scrollbar appears .
               // So we subtract 3 from Width .

               // For left and right we need to leave space for 2 pixels from up (as y=2) and 3 pixels from bottom otherwise tab are disaplyed in multiline
               // So we need to subtract 5 from height .

               if (Alignment == TabAlignment.Top || Alignment == TabAlignment.Bottom)
                  widthofTab = Math.Max((Width - 3) / TabPages.Count, 0);
               else
                  widthofTab = Math.Max((Height - 5) / TabPages.Count, 0);

               ItemSize = new Size(widthofTab, ItemSize.Height);
            }
         }
         else
         {
            int width = MaxTextWidth + tabTextOffset;

            if (IconWidth > 0)
               width += IconWidth + tabImageOffset;

            ItemSize = new Size(width, ItemSize.Height);
         }
      }

      /// <summary>
      /// Creates/Modifies TabPages.
      /// </summary>
      /// <param name="items"></param>
      public void SetTabPages(String[] items)
      {
         int oldCount = TabPages.Count;
         int newCount = items.Length;


         if (newCount > oldCount)
         {
            for (int i = oldCount; i < newCount; i++)
            {
               MgTabPage page = new MgTabPage();
               TabPages.Add(page);
            }
         }
         else
         {
            for (int i = oldCount - newCount - 1; i >= 0; i--)
            {
               TabPage page = TabPages[TabPages.Count - 1];
               TabPages.Remove(page);
               page.Dispose();
            }
         }

         TabPage tabPage;
         for (int i = 0; i < newCount; i++)
         {
            tabPage = TabPages[i];

            String text = items[i];
#if PocketPC
            // On mobile, if there's an '&' in the tab text, the text is not painted
            text = text.Replace("&", "");
#endif

            tabPage.Text = text;
            tabPage.TabIndex = i;
         }

         UpdateMaxTextWidth();

         if (InitialDisplayRectangle.IsEmpty)
            InitialDisplayRectangle = new Rectangle(DisplayRectangle.Location, DisplayRectangle.Size);

#if PocketPC
         // If we didn't have pages before, we need to calculate the tab offset
         CalcOffset();
#endif
      }

#if !PocketPC
      /// <summary>
      /// register to events of the Tab Page
      /// </summary>
      /// <param name="page"></param>
      void RegisterEvents(MgTabPage page)
      {
         page.ControlAdded += new ControlEventHandler(page_ControlAdded);
         page.ControlRemoved += new ControlEventHandler(page_ControlRemoved);
      }

      /// <summary>
      /// Unregister to events of the Tab Page
      /// </summary>
      /// <param name="page"></param>
      void UnRegisterEvents(MgTabPage page)
      {
         page.ControlAdded -= new ControlEventHandler(page_ControlAdded);
         page.ControlRemoved -= new ControlEventHandler(page_ControlRemoved);
      }

      void page_ControlRemoved(object sender, ControlEventArgs e)
      {
         this.OnControlRemoved(e);
      }
      void page_ControlAdded(object sender, ControlEventArgs e)
      {
         this.OnControlAdded(e);
      }
#endif

      IntPtr upDownHandle;
      private NativeUpDown fUpDown;
      public Rectangle TabUpDownRect { get { return fUpDown.Rect; } }
      bool contextMenuOpening { get; set; }

      /// <summary>
      /// buffer used for painting tab control when title color > 0
      /// </summary>
      BufferedGraphics tabBuffer;

      public MgTabControl()
      {
#if PocketPC
         // Default dock style on mobile is different
         Dock = DockStyle.None;

         subclassTabControl();
#else
         this.ControlAdded += OnControlAdded;
         this.ControlRemoved += OnControlRemoved;
         this.SizeChanged += MgTabControl_SizeChanged;
         this.ContextMenuStripChanged += MgTabControl_ContextMenuStripChanged;
       
         this.GradientStyle = GradientStyle.None;
         pageColor = base.BackColor;

         this.SetStyle(ControlStyles.Opaque | ControlStyles.ResizeRedraw, true);
         this.DisplayStyleProvider = TabStyleProvider.CreateProvider(this, TabStyle.Default);

         fUpDown = new NativeUpDown(this);
#endif
      }

      private void RecreateBuffers()
      {
         if (!TitleColor.IsEmpty)
         {
            BufferedGraphicsManager.Current.MaximumBuffer = new Size(Math.Max(this.Width, 1), Math.Max(this.Height, 1));

            // Dispose of old backbufferGraphics (if one has been created already)
            if (tabBuffer != null)
               tabBuffer.Dispose();

            // Create new backbufferGrpahics that matches the current size of buffer.
            tabBuffer = BufferedGraphicsManager.Current.Allocate(this.CreateGraphics(),
                new Rectangle(0, 0, Math.Max(this.Width, 1), Math.Max(this.Height, 1)));

            // Invalidate the control so a repaint gets called somewhere down the line.
            this.Invalidate();
         }
      }

#if !PocketPC

      /// <summary>
      /// Process the Mnemonic key pressed to select the tab page whose text is associated with pressed mnemonic key.
      /// </summary>
      /// <param name="inputChar"></param>
      /// <returns></returns>
      protected override bool ProcessMnemonic(char inputChar)
      {
         if (CanSelect)
         {
            for (int tabIndex = 0; tabIndex < TabPages.Count; tabIndex++)
            {
               MgTabPage tabPage = (MgTabPage)TabPages[tabIndex];
               
               if (tabPage.IsMnemonicChar(inputChar))
               {
                  if (SelectedIndex != tabIndex)
                  {
                     MnemonicKeyPressedEventArgs mnemonicEventArgs = new MnemonicKeyPressedEventArgs(tabIndex);
                     OnMnemonicKeyPressed(mnemonicEventArgs);
                  }
                  return true;
               }
            }
         }

         return base.ProcessMnemonic(inputChar);
      }

#endif

      void MgTabControl_SizeChanged(object sender, EventArgs e)
      {
         this.SizeChanged -= MgTabControl_SizeChanged;
         RefreshItemSize();
         RecreateBuffers();
         this.SizeChanged += MgTabControl_SizeChanged;
      }

      protected override void OnSelectedIndexChanged(EventArgs e)
      {
         base.OnSelectedIndexChanged(e);
         Invalidate();
      }


      void MgTabControl_ContextMenuStripChanged(object sender, EventArgs e)
      {
         this.ContextMenuStrip.Opening += ContextMenuStrip_Opening;
         this.ContextMenuStrip.Closed += ContextMenuStrip_Closed;
         this.ContextMenuStrip.Disposed += ContextMenuStrip_Disposed;
      }

      private void ContextMenuStrip_Closed(object sender, ToolStripDropDownClosedEventArgs e)
      {
         contextMenuOpening = false;
      }

      void ContextMenuStrip_Opening(object sender, CancelEventArgs e)
      {
         contextMenuOpening = !e.Cancel;
         Invalidate();
      }

      void ContextMenuStrip_Disposed(object sender, EventArgs e)
      {
         this.ContextMenuStrip.Opening -= ContextMenuStrip_Opening;
         this.ContextMenuStrip.Closed -= ContextMenuStrip_Closed;
         this.ContextMenuStrip.Disposed -= ContextMenuStrip_Disposed;
      }

#if !PocketPC
      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void OnControlAdded(object sender, ControlEventArgs e)
      {
         if (e.Control is MgTabPage)
         {
            RegisterEvents((MgTabPage)e.Control);
         }
      }

      void OnControlRemoved(object sender, ControlEventArgs e)
      {
         if (e.Control is MgTabPage)
         {
            UnRegisterEvents((MgTabPage)e.Control);
         }
      }

      protected override void CreateHandle()
      {
         base.CreateHandle();
         SetNativeFont();
      }
      
      /// <summary>
      /// Set the native font for owner drawn tab control 
      /// </summary>
      public void SetNativeFont()
      {
         if (TitleColor != Color.Empty)
         {
            IntPtr hFont = this.Font.ToHfont();

            // We need to send following message , otherwise we don't get correct tab sizes 
            // in studio -  for FitToText .
            // in runtime - Qcr431069 / defect 69080 (no of tabs in case of multiline are not correct)
            NativeWindowCommon.SendMessage(this.Handle, NativeWindowCommon.WM_SETFONT, hFont.ToInt32(), (-1));
            NativeWindowCommon.SendMessage(this.Handle, NativeWindowCommon.WM_FONTCHANGE, 0, 0);

            this.UpdateStyles(); // This is required otherwise hottrack is not seen for left and right tabs (Defect 130744)

            Invalidate(); // If font was changed in runtime using expression tab is not painted correctly, so explicitly call invalidate.

            // TODO: Find a correct place to releasing the pointer. Cannot do it here as it is still being used 
            //NativeWindowCommon.DeleteObject(hFont);
         }
      }

#endif

#if PocketPC
      public Size ItemSize { get; set; }
      public bool Multiline { get; set; }
      public ImageList ImageList { get; set; }
      public int TabCount { get { return TabPages.Count; } }
      private TabAlignment alignment;
      public TabAlignment Alignment 
      {
         get
         {
            return alignment;
         }
         set
         {
            // If allignment changed, we need to recalculate the offset
            if (alignment != value)
            {
               alignment = value;
               CalcOffset();
            }
         }
      }
      public int Offset { get; set; }
      public void CalcOffset()
      {
         // CF does not support different alignments. If original alignment was not bottom,
         // we need to shift the child controls. the offset here is the height the controls will shift.
         int old = Offset;
         if (alignment != TabAlignment.Bottom &&  Controls.Count > 0)
         {
            Offset = Height - Controls[0].Height;
         }
         else
            Offset = 0;
         if (old != Offset)
         {
            // If offset changed, we need to go over all controls and change their location
            foreach (Control tabPage in Controls)
            {
               foreach (Control panel in tabPage.Controls)
               {
                  foreach (Control control in panel.Controls)
                  {
#if PocketPC
                     if (panel is MgPanel && control == ((MgPanel)panel).dummy)
                        continue;
#endif
                     control.Top -= Offset - old;
                  }

               }
            }
         }
      }
#endif

#if !PocketPC

      public bool SuspendPaint { get; set; }
      protected override void WndProc(ref Message m)
      {
         if (m.Msg == NativeWindowCommon.WM_PAINT)
         {
            //Ignore the paint message if SuspendPaint=true.
            if (SuspendPaint)
               return;

            if (Parent is ISupportsTransparentChildRendering)
               ((ISupportsTransparentChildRendering)Parent).TransparentChild = this;
         }

         if ((m.Msg == NativeWindowCommon.WM_KEYDOWN) && (m.WParam == (IntPtr)Keys.Tab) && ((Control.ModifierKeys & Keys.Control) != 0))
         {
            //QCR #727777, some keyboard events on the tab are sent to its parent, if tabs parent happends to be tab control also.
            //It happends during PreviewKeyDown so we can not control it. We need to prevent default .Net behavior and instead use magic
            // events to handle events like Ctrl+Tab and Ctrl+Shift+Tab on tab Control.


            Keys key = Keys.Tab | Control.ModifierKeys;
            KeyEventArgs ke = new KeyEventArgs(key);

            OnKeyDown(ke);
            return;
         }

         base.WndProc(ref m);

         if (m.Msg == NativeWindowCommon.WM_PAINT)
         {
            if (Parent is ISupportsTransparentChildRendering)
               ((ISupportsTransparentChildRendering)Parent).TransparentChild = null;
         }

         if (m.Msg == NativeWindowCommon.WM_NCHITTEST)
         {
            //There is an area between bottom of tab header and top of tab page that does not receive any mouse events. 
            //Area's height is 2 pixels. Any clicks, mouseover and etc events are received by control that is UNDER tabControl. 
            //Problem exists only in .NET - not in win32. 
            //http://groups.google.com/group/microsoft.public.dotnet.framework.windowsforms/browse_thread/thread/86fabbaae76cefd4#

            if (m.Result.ToInt32() == (int)NativeWindowCommon.HitTestValues.HTTRANSPARENT)
            {
               // If you want to enable clicks on the area behind the tabs instead of 
               // passing through then just return HTCLIENT instead of the following. 
               Point pt = PointToClient(new Point(m.LParam.ToInt32()));
               Rectangle rc = DisplayRectangle;
               int offset = (Alignment == TabAlignment.Top ? 2 : 3);
               rc.Inflate(3, 3);
               if (rc.Contains(pt))
                  m.Result = new IntPtr((int)NativeWindowCommon.HitTestValues.HTCLIENT);
            }
         }
         else if (!titleColor.IsEmpty && m.Msg == NativeWindowCommon.WM_PARENTNOTIFY && (m.WParam.ToInt32() & 0xffff) == NativeWindowCommon.WM_CREATE)
         {
            /* Tab scroller has been created (there are too many tabs to display and the control is not multiline), so
             * let's attach our hook to it.*/
            StringBuilder className = new StringBuilder(16);
            if (NativeWindowCommon.RealGetWindowClass(m.LParam, className, 16) > 0 && className.ToString() == "msctls_updown32")
            {
               fUpDown.ReleaseHandle();
               fUpDown.AssignHandle(m.LParam);
               upDownHandle = m.LParam;
            }
         }
         if (m.Msg == NativeWindowCommon.WM_HSCROLL)
         {
            Invalidate();
         }
      }

      protected override void OnResize(EventArgs e)
      {
         base.OnResize(e);
         Invalidate();
      }

      /// <summary>
      /// true, when we are starting drag of this control
      /// </summary>
      bool IsStartingDragOfThisControl
      {
         get
         {
            if (this.DesignMode && (Control.MouseButtons & MouseButtons.Left) != 0 && Cursor.Current == Cursors.SizeAll)
            {
               ISelectionService selectionService = (ISelectionService)this.GetService(typeof(ISelectionService));
               ICollection collection = selectionService.GetSelectedComponents();
               foreach (Control item in collection)
               {
                  if (item == this)
                     return true;
               }
            }
            return false;

         }
      }

      protected override void OnPaint(PaintEventArgs e)
      {
         //If this.TitleColor != Color.Empty, UserPaint is set to false. So, OnPaint() shouldn't get called.
         Debug.Assert(TitleColor != Color.Empty);

         // identify the case we are in the drag state in studio
         // in this case do not use double buffering - use usual painting because frameworks creates an image while dragging and this cannot handle double buffering hence tabs are not seen while dragging .
         if (IsStartingDragOfThisControl)
         {         
            MgTabControlRenderer.CustomPaint(this, e.Graphics);
         }
         else
         {
            // use double buffering 
            MgTabControlRenderer.CustomPaint(this, tabBuffer.Graphics);
            tabBuffer.Render(e.Graphics);
         }
      }

#endif

#if PocketPC
      // Using the native window proc
      // A delegate for our wndproc
      public delegate int MobileWndProc(IntPtr hwnd, uint msg, uint wParam, int lParam);

      // Original wndproc
      private IntPtr OrigWndProc;
      // object's delegate
      MobileWndProc proc;

      //This is usually where the control handle is created - subclass
      void subclassTabControl()
      {
         proc = new MobileWndProc(WindowProc);
         OrigWndProc = (IntPtr)NativeWindowCommon.SetWindowLong(Handle, NativeWindowCommon.GWL_WNDPROC,
                         Marshal.GetFunctionPointerForDelegate(proc).ToInt32());
      }

      // Our wndproc
      private int WindowProc(IntPtr hwnd, uint msg, uint wParam, int lParam)
      {
         // We don't get the LBUTTONDOWN here - only the notify about the click. It should be good enough
         if (msg == NativeWindowCommon.WM_NOTIFY)
         {
            NativeWindowCommon.NMHDR nmhdr =
                     (NativeWindowCommon.NMHDR)Marshal.PtrToStructure((IntPtr)lParam, typeof(NativeWindowCommon.NMHDR));
            if (nmhdr.code == NativeWindowCommon.NM_CLICK)
            {
               MouseEventArgs mouseEvents = new MouseEventArgs(MouseButtons.Left, 0, 0, 0, 0);
               this.OnMouseDown(mouseEvents);
            }
         }
         return NativeWindowCommon.CallWindowProc(OrigWndProc, hwnd, msg, wParam, lParam);
      }
      public void CallKeyDown(KeyEventArgs e)
      {
         OnKeyDown(e);
      }
#endif


      public event CanParentDelegate CanParentEvent;

      public bool CanParent(CanParentArgs allowDragDropArgs)
      {
         if (CanParentEvent != null)
            return CanParentEvent(this, allowDragDropArgs);
         return true;
      }

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

      /// <summary>
      /// remove the tab page when the desiger action "TabControlRemoveTab" is invoked
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="eevent"></param>
#if !PocketPC
      public virtual void OnDesignerRemoveTabPage(object sender, EventArgs eevent)
      {
         TabPage selectedTab = this.SelectedTab;
         IDesignerHost service = (IDesignerHost)this.GetService(typeof(IDesignerHost));
         if (service != null)
         {
            DesignerTransaction transaction = null;
            try
            {
               try
               {
                  MemberDescriptor member = TypeDescriptor.GetProperties(this)["Controls"];

                  transaction = service.CreateTransaction("TabControlRemoveTab" + selectedTab.Site.Name + Site.Name);
                  IComponentChangeService componentChangeService = (IComponentChangeService)GetService(typeof(IComponentChangeService));
                  componentChangeService.OnComponentChanging(this, null);

                  service.DestroyComponent(selectedTab);
                  componentChangeService.OnComponentChanged(this, member, null, null);
               }
               catch (CheckoutException exception)
               {
                  if (exception != CheckoutException.Canceled)
                  {
                     throw exception;
                  }
                  return;
               }

            }
            finally
            {
               if (transaction != null)
               {
                  transaction.Commit();
               }
            }
         }
      }

      /// <summary>
      /// Get the tab count excluding dummy tab.
      /// </summary>
      /// <returns></returns>
      public virtual int TabCountExcludingDummyTab()
      {
         return 0;
      }
      
#endif

#if !PocketPC
      public virtual void OnDesignerAddTabPage(bool addingOnInitialize)
      {
         IDesignerHost service = (IDesignerHost)this.GetService(typeof(IDesignerHost));
         if (service != null)
         {
            DesignerTransaction transaction = null;
            try
            {
               try
               {
                  transaction = service.CreateTransaction("TabControlAddTab" + this.Site.Name);
               }
               catch (CheckoutException exception)
               {
                  if (exception != CheckoutException.Canceled)
                  {
                     throw exception;
                  }
                  return;
               }
               MemberDescriptor member = TypeDescriptor.GetProperties(this)["Controls"];
               MgTabPage page = new MgTabPage();
               IComponentChangeService componentChangeService = (IComponentChangeService)GetService(typeof(IComponentChangeService));

               if (!addingOnInitialize)
               {
                  componentChangeService.OnComponentChanging(this, member);
               }
               page.Padding = new Padding(3);
               string str = null;
               PropertyDescriptor descriptor2 = TypeDescriptor.GetProperties(page)["Name"];
               if ((descriptor2 != null) && (descriptor2.PropertyType == typeof(string)))
               {
                  str = GetNewPageText();
               }
               if (str != null)
               {
                  PropertyDescriptor descriptor3 = TypeDescriptor.GetProperties(page)["Text"];
                  if (descriptor3 != null)
                  {
                     descriptor3.SetValue(page, str);
                  }
               }
               PropertyDescriptor descriptor4 = TypeDescriptor.GetProperties(page)["UseVisualStyleBackColor"];
               if (((descriptor4 != null) && (descriptor4.PropertyType == typeof(bool))) && (!descriptor4.IsReadOnly && descriptor4.IsBrowsable))
               {
                  descriptor4.SetValue(page, true);
               }
               this.TabPages.Add(page);
               this.SelectedIndex = this.TabCount - 1;
               if (!addingOnInitialize)
               {
                  componentChangeService.OnComponentChanged(this, member, null, null);
               }
            }
            finally
            {
               if (transaction != null)
               {
                  transaction.Commit();
               }
            }
         }
      }

      /// <summary>
      /// calculate the new TabPage text. 
      /// The Text calculation is done based on the tab pages in this tab control only.
      /// The change is based on the text and not on the name because the name is used by the designer site, and it does not allow duplicate names.
      /// </summary>
      /// <returns></returns>
      string GetNewPageText()
      {
         int maxVal = 0;
         foreach (TabPage item in Controls)
         {
            if (item.Text.StartsWith("TabPage"))
            {
               try
               {
                  int value = Int32.Parse(item.Text.Substring("TabPage".Length));
                  if (value > maxVal)
                     maxVal = value;
               }
               catch
               { }
            }
         }

        return "TabPage" + (maxVal+1).ToString();
      }

      protected override void Dispose(bool disposing)
      {
         if (disposing)
         {
            if (tabBuffer != null)
               tabBuffer.Dispose();
         }
         base.Dispose(disposing);
      }
     

#endif
   }


#if !PocketPC
   [Designer(typeof(TabPageDesigner))]
#endif
   public class MgTabPage : TabPage, IGetClassInfo, ICanParent, ISupportsTransparentChildRendering
   {
      public MgTabPage()
      {
#if !PocketPC
         UseVisualStyleBackColor = true;
         DoubleBuffered = true;
#else
         BackColor = SystemColors.Control;
#endif
      }

      /// <summary>
      /// 
      /// </summary>
      public override string Text
      {
         get
         {
            return base.Text;
         }
         set
         {
            if (OriginalText != value)
            {
               OriginalText = value;
               string textWithoutAccelerator = GetDisplayTextAndSetAcceleratorKey(new StringBuilder(OriginalText), false);

               if (base.Text != textWithoutAccelerator)
                  base.Text = Name = textWithoutAccelerator;

               TextWithAccelerator = GetDisplayTextAndSetAcceleratorKey(new StringBuilder(OriginalText), true);
            }
         }
      }

      /// <summary>
      /// Get or set the original text assigned to the tab page.
      /// </summary>
      private string OriginalText { get; set; }

      /// <summary>
      /// Get or set the accelerator key associated with the text assigned to the Tab page.
      /// </summary>
      private char Accelerator { get; set; }

      /// <summary>
      /// Get the text which contains '&' to show the character after '&' as an accelerator.
      /// </summary>
      internal string TextWithAccelerator { get; private set; }

      /// <summary>
      /// Remove the accelerator characters from given option string and set the accelerator key.
      /// If character contains "&&", allow to display only the single '&' character.
      /// </summary>
      /// <param name="optionsStr"></param>
      /// <returns></returns>
      private String GetDisplayTextAndSetAcceleratorKey(StringBuilder optionsStr, bool shouldDisplayTextWithAccelerator)
      { 
            Accelerator = '\0';
            if (optionsStr != null)
            {
               for (int optionsStrIdx = 0; optionsStrIdx < optionsStr.Length;)
               {
                  if (optionsStr[optionsStrIdx] == '&')
                  {
                     bool removeChar = true;
                     if (optionsStrIdx < optionsStr.Length - 1)
                     {
                        //character contains "&&", allow to display only the single '&' character.
                        if (optionsStr[optionsStrIdx + 1] == ('&'))
                        {
                           optionsStrIdx += 1;
                           if (shouldDisplayTextWithAccelerator)
                              removeChar = false;
                        }
                        else
                        {
                           //Set the character after '&' as an accelerator key only if accelerator key is not set already.
                           if (Accelerator == '\0')
                           {
                              Accelerator = char.ToUpper(optionsStr[optionsStrIdx + 1]);
                              if (shouldDisplayTextWithAccelerator)
                                 removeChar = false;
                           }
                        }
                     }

                     if (removeChar)
                        optionsStr = optionsStr.Remove(optionsStrIdx, 1);
                     else
                        optionsStrIdx++;
                  }
                  else
                     optionsStrIdx++;
               }
            }

         return (optionsStr != null ? optionsStr.ToString() : null);
      }

      /// <summary>
      /// Check if the character is mnemonic or not.
      /// </summary>
      /// <param name="inputChar"></param>
      /// <returns></returns>
      internal bool IsMnemonicChar(char inputChar)
      {
         return Accelerator == char.ToUpper(inputChar);
      }

      /// <summary> </summary>
      /// <param name="e"></param>
      protected override void OnPaint(PaintEventArgs e)
      {
         MgTabControl parentMgTabControl = this.Parent as MgTabControl;

         if (parentMgTabControl == null || parentMgTabControl.IsBasePaint)
            base.OnPaint(e);
         else
            ControlRenderer.FillRectAccordingToGradientStyle(e.Graphics, ClientRectangle, parentMgTabControl.PageColor, Color.Empty, ControlStyle.NoBorder,
                                                             false, parentMgTabControl.GradientColor, parentMgTabControl.GradientStyle);

         if (TransparentChild != null && DesignMode)
            ControlRenderer.RenderOwnerDrawnControlsOfParent(e.Graphics, TransparentChild);
      }

      /// <summary>
      /// return the class name to be displayed in the new studio document outline
      /// </summary>
      /// <returns></returns>
      public string GetClassName(InfoType infoType)
      {
         switch (infoType)
         {
            case InfoType.Component:
               return Text;
            case InfoType.ComponentAndClass:
               return "TabPage";
         }
         return null;
      }

      #region ICanParent
      public event CanParentDelegate CanParentEvent;

      /// <summary>
      /// 
      /// </summary>
      /// <param name="allowDragDropArgs"></param>
      /// <returns></returns>
      public bool CanParent(CanParentArgs allowDragDropArgs)
      {
         
        if (CanParentEvent != null) //runtime designer
            return CanParentEvent(this, allowDragDropArgs);
        else   if (this.Parent is ICanParent) //studio
              return ((ICanParent)this.Parent).CanParent(allowDragDropArgs);
         return true;
      }

      #endregion 

      #region ISupportsTransparentChildRendering

      public Control TransparentChild { get; set; }

      #endregion
   }

   /// <summary>
   /// Subclass for updown button to save X positon
   /// https://code.google.com/p/sharpfile/source/browse/trunk/UI/TabControl.cs?r=446
   /// </summary>
   class NativeUpDown : NativeWindow
   {
      public NativeUpDown(MgTabControl ctrl) { parentControl = ctrl; }

      private MgTabControl parentControl;

      /// <summary>
      /// Reports about current bounds of tab scroller.
      /// </summary>
      public Rectangle Rect { get; private set; }

      protected override void WndProc(ref Message m)
      {
        // if native updown is destroyed we need release our hook
         if (m.Msg == NativeWindowCommon.WM_DESTROY || m.Msg == NativeWindowCommon.WM_NCDESTROY)
             this.ReleaseHandle();
         if (m.Msg == NativeWindowCommon.WM_WINDOWPOSCHANGING)
         {
            //When a scroller position is changed we should remember that new position.
            NativeWindowCommon.WINDOWPOS wp = (NativeWindowCommon.WINDOWPOS)m.GetLParam(typeof(NativeWindowCommon.WINDOWPOS));
            Rect = new Rectangle(wp.x, wp.y, wp.cx, wp.cy);
         }
         if (m.Msg == NativeWindowCommon.WS_SHOWWINDOW)
         {
            // (Defect 130515 ) - When the scroll bar hides itself  we get this message . 
            // Since the scrollbar is hidden , we can paint in the area below scrollbar, so we should not set rect in this case
            Rect = Rectangle.Empty;
         }
         base.WndProc(ref m);
      }
   }

   /// <summary>
   /// Subclass to save selectedTabIndex which is used for selecting the tab page when mnemonic key is pressed. 
   /// </summary>
   public class MnemonicKeyPressedEventArgs : EventArgs
   {
      /// <summary>
      /// Index of the tab page to be selected.
      /// </summary>
      public int SelectedTabIndex { get; private set; }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="selectedTabIndex"></param>
      public MnemonicKeyPressedEventArgs(int selectedTabIndex)
      {
         SelectedTabIndex = selectedTabIndex;
      }
   }
}
