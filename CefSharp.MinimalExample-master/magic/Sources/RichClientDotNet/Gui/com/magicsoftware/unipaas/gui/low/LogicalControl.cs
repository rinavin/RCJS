using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using com.magicsoftware.controls;
using com.magicsoftware.editors;
using com.magicsoftware.util;
using Controls.com.magicsoftware;
using Controls.com.magicsoftware.controls.MgLine;
using Controls.com.magicsoftware.support;

#if PocketPC
using ContextMenuStrip = com.magicsoftware.mobilestubs.ContextMenuStrip;
using ContentAlignment = OpenNETCF.Drawing.ContentAlignment2;
using TextBox = com.magicsoftware.controls.MgTextBox;
using ContextMenu = com.magicsoftware.controls.MgMenu.MgContextMenu;
#endif

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>
   ///   base class for all controls that may NOT have real windows control for them
   /// </summary>
   internal abstract class LogicalControl : PlacementDrivenLogicalControl, IEditorProvider
   {
      internal GuiMgControl GuiMgControl { get; private set; }
      protected readonly Control _containerControl;

      // event is raised when style is changed
      internal event EventHandler StyleChanged;

      // event is raised when border style is changed
      internal event EventHandler BorderTypeChanged;

      // Allow Drag & Drop : will be used to check whether the drag/drop is allowed on control or not.
      internal bool AllowDrop { get; set; }
      internal bool AllowDrag { get; set; }

      internal String Name { get; set; }

      protected IEditorProvider _editorProvider;

      internal int MgColorIndex { get; set; }

      internal int MgFontIndex { get; set; }

      /// <summary>
      ///   paint the control
      /// </summary>
      /// <param name = "g"></param>
      internal void paint(Graphics g)
      {
         if (Visible)
            paint(g, getRectangle(), getBackgroundColor(true), FgColor, true);
      }

      /// <summary>
      ///   paint the control
      /// </summary>
      /// <param name = "g"></param>
      /// <param name = "rect"></param>
      /// <param name = "bgColor"></param>
      /// <param name = "fgColor"></param>
      /// <param name = "keepColor"></param>
      internal void paint(Graphics g, Rectangle rect, Color bgColor, Color fgColor, bool keepColor)
      {

         if (Visible && (g.VisibleClipBounds == null || rect.IntersectsWith(Rectangle.Round(g.VisibleClipBounds))))
         {
            paintBackground(g, rect, bgColor, fgColor, keepColor);
            paintForeground(g, rect, fgColor);
         }
      }

      /// <summary>
      ///   paint background
      /// </summary>
      /// <param name = "g"></param>
      /// <param name = "rect"></param>
      /// <param name = "bgColor"></param>
      /// <param name = "fgColor"></param>
      /// <param name = "keepColor"></param>
      internal virtual void paintBackground(Graphics g, Rectangle rect, Color bgColor, Color fgColor, bool keepColor)
      {
#if PocketPC
         offsetStaticControlRect(ref rect);
#endif
         if (GuiMgControl.SupportGradient() && keepColor)
            ControlRenderer.FillRectAccordingToGradientStyle(g, rect, BgColor, FgColor, Style,
                                                             false, GradientColor, GradientStyle);
         else
            ControlRenderer.PaintBackGround(g, rect, bgColor, true);
      }

      /// <summary>
      ///   paint foreground
      /// </summary>
      /// <param name = "g"></param>
      /// <param name = "rect"></param>
      /// <param name = "fgColor"></param>
      internal virtual void paintForeground(Graphics g, Rectangle rect, Color fgColor)
      {
         if (!String.IsNullOrEmpty(Text))
         {
#if PocketPC
            offsetStaticControlRect(ref rect);
#endif
            printText(g, rect, fgColor, Text);
         }
      }


      internal ContainerManager ContainerManager
      {
         get { return GuiUtilsBase.getContainerManager(_containerControl); }
      }

      /// <summary>
      ///   get editor control of child
      /// </summary>
      /// <returns></returns>
      public virtual Control getEditorControl()
      {
         if (_editorProvider != null)
            return _editorProvider.getEditorControl();
         else
            return null;
      }

      private String _tooltip;
      internal String Tooltip
      {
         get { return _tooltip; }
         set
         {
            bool changed = _tooltip != value;
            _tooltip = value;
            Refresh(changed);
         }
      }

      //   background color
      private Color _bgColor;
      internal virtual Color BgColor
      {
         get { return _bgColor; }
         set
         {
            bool changed = _bgColor != value;
            _bgColor = value;
            Refresh(changed);
         }
      }

      //   foreground color
      private Color _fgColor;
      internal virtual Color FgColor
      {
         get { return _fgColor; }
         set
         {
            bool changed = _fgColor != value;
            _fgColor = value;
            Refresh(changed);
         }
      }

      private Font _font;
      internal virtual Font Font
      {
         get { return _font; }
         set
         {
            bool changed = _font != value;
            _font = value;
            Refresh(changed);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="control"></param>
      /// <param name="isSelected"></param>
      /// <param name="recievingFocus"></param>
      internal void setProperties(Control control, bool isSelected, bool recievingFocus)
      {
         TableManager tableManager = ContainerManager as TableManager;
         Color bgColor;
         Color fgColor;
         bool isInFocus = false;

         if (tableManager != null && !GuiMgControl.IsTableHeaderChild)
         {
            bool keepColor;
            bool isRowmarked = tableManager.ShouldPaintRowAsMarked(_mgRow, isSelected);
            isInFocus = GuiUtils.getFocusedControl(tableManager.Form) == control || recievingFocus;
            bgColor = tableManager.computeControlBgColor(this, tableManager.ColumnsManager, control, isRowmarked, isInFocus,
                                       tableManager.getGuiRowIndex(_mgRow), false, out keepColor);
            fgColor = tableManager.computeControlFgColor(this, isSelected);
         }
         else
         {
            bgColor = getBackgroundColor(false);
            fgColor = _fgColor;
         }

         setSpecificControlProperties(control);

         setCommonProperties(control, bgColor, fgColor);

         SetFocusColor(control, bgColor, fgColor, isInFocus);

         SetMargin(control);
      }


      /// <summary>
      /// 
      /// </summary>
      /// <param name="control"></param>
      internal void setProperties(Control control)
      {
         _coordinator.RefreshNeeded = false;
         setProperties(control, true, true);
      }

      protected virtual void SetMargin(Control control)
      {    
      }

      /// <summary>
      /// while user defined focus color, set the new color to the control
      /// </summary>
      /// <param name="control"></param>
      /// <param name="bgColor"></param>
      /// <param name="fgColor"></param>
      protected virtual void SetFocusColor(Control control, Color bgColor, Color fgColor, bool isInFocus)
      {        
      }
      /// <summary>
      ///   TOBE overriten by controls that have editors, like StaticText, StaticImage
      /// </summary>
      /// <param name = "control"></param>
      internal virtual void setSpecificControlProperties(Control control)
      {
      }

      /// <summary>
      ///   TOBE overriten by controls that have editors, like StaticText, StaticImage
      /// </summary>
      /// <param name = "control"></param>
      internal virtual void setSpecificControlPropertiesForFormDesigner(Control control)
      {
      }

      ///   enabled control
      private bool _enabled;
      internal bool Enabled
      {
         get { return _enabled && _containerControl.Enabled; }
         set
         {
            bool changed = _enabled != value;
            _enabled = value;
            Refresh(changed);
         }
      }

      private bool _multiLine;
      internal bool MultiLine
      {
         get { return _multiLine; }
         set
         {
            bool changed = _multiLine != value;
            _multiLine = value;
            Refresh(changed);
         }
      }

      ///   multiline allow CR
      private bool _multiLineAllowCR;
      internal bool MultiLineAllowCR
      {
         get { return _multiLineAllowCR; }
         set
         {
            bool changed = _multiLineAllowCR != value;
            _multiLineAllowCR = value;
            Refresh(changed);
         }
      }

      ///   WordWrap control
      private bool _wordWrap;
      internal bool WordWrap
      {
         get { return _wordWrap; }
         set
         {
            bool changed = _wordWrap != value;
            _wordWrap = value;
            Refresh(changed);
         }
      }

      ///   gradientColor
      private GradientColor _gradientColor;
      internal GradientColor GradientColor
      {
         get { return _gradientColor; }
         set
         {
            bool changed = _gradientColor != value;
            _gradientColor = value;
            Refresh(changed);
         }
      }

      ///   gradientStyle
      private GradientStyle _gradientStyle = GradientStyle.None;
      internal GradientStyle GradientStyle
      {
         get { return _gradientStyle; }
         set
         {
            bool changed = _gradientStyle != value;
            _gradientStyle = value;
            Refresh(changed);
         }
      }

      ///   content Alignment
      private ContentAlignment _contentAlignment;
      internal virtual ContentAlignment ContentAlignment
      {
         get { return _contentAlignment; }
         set
         {
            bool changed = _contentAlignment != value;
            _contentAlignment = value;
            Refresh(changed);
         }
      }

      private String _text;
      internal virtual String Text
      {
         get { return _text; }
         set
         {
            bool changed = _text != value;
            _text = value;
            bool updated = false;

            if (GuiMgControl.isTextControl())
            {
               Control control = getEditorControl();
               if (control is TextBox)
               {
                  // Qcr #726829 : Since get value sometimes is activated immediatly after this, we need to update the value now.
                  GuiUtilsBase.setText(control, _text);
                  updated = true;
               }
            }
            if (!updated)
               Refresh(changed);
         }
      }

#if !PocketPC
      ///   context menu
      private ContextMenuStrip _contextMenu;
      internal ContextMenuStrip ContextMenu
      {
         get { return _contextMenu; }
         set
         {
            bool changed = _contextMenu != value;
            _contextMenu = value;
            Refresh(changed);
            if (_coordinator is TableCoordinator) //
               ContextMenu.Location = new Point(X, Y);
         }
      }
#else
      /// <summary>context menu</summary>
      private ContextMenu _contextMenu;
      internal ContextMenu ContextMenu
      {
         get { return _contextMenu; }
         set
         {
            bool changed = _contextMenu != value;
            _contextMenu = value;
            Refresh(changed);
         }
      }
#endif

      ///   text size limit
      private int _textSizeLimit;
      internal int TextSizeLimit
      {
         get { return _textSizeLimit; }
         set
         {
            bool changed = _textSizeLimit != value;
            _textSizeLimit = value;
            Refresh(changed);
         }
      }

      ///   has border
      private bool _showBorder;
      internal bool ShowBorder
      {
         get { return _showBorder; }
         set
         {
            bool changed = _showBorder != value;
            _showBorder = value;
            Refresh(changed);
         }
      }

      ///   style
      private ControlStyle _style;
      internal virtual ControlStyle Style
      {
         get { return _style; }
         set
         {
            bool changed = _style != value;
            _style = value;
            Refresh(changed);
            if (changed && StyleChanged != null)
               StyleChanged(this, new EventArgs());
         }
      }


      ///   style
      private BorderType _ControlBorderType;
      internal virtual BorderType ControlBorderType
      {
         get { return _ControlBorderType; }
         set
         {
            bool changed = _ControlBorderType != value;
            _ControlBorderType = value;
            Refresh(changed);
            if (changed && BorderTypeChanged != null)
               BorderTypeChanged(this, new EventArgs());
         }
      }


      ///   is password edit
      private bool _passwordEdit;
      internal bool PasswordEdit
      {
         get { return _passwordEdit; }
         set
         {
            bool changed = _passwordEdit != value;
            _passwordEdit = value;
            Refresh(changed);
         }
      }

      ///   show horizontal scrollbar
      private MultilineHorizontalScrollBar _horizontalScrollBar;
      internal MultilineHorizontalScrollBar HorizontalScrollBar
      {
         get { return _horizontalScrollBar; }
         set
         {
            bool changed = _horizontalScrollBar != value;
            _horizontalScrollBar = value;
            Refresh(changed);
         }
      }

      ///   show vertical scrollbar
      private bool _showVerticalScroll;
      internal bool ShowVerticalScroll
      {
         get { return _showVerticalScroll; }
         set
         {
            bool changed = _showVerticalScroll != value;
            _showVerticalScroll = value;
            Refresh(changed);
         }
      }

      private bool _modifable = true;
      internal bool Modifable
      {
         get { return _modifable; }
         set
         {
            bool changed = _modifable != value;
            _modifable = value;
            Refresh(changed);
         }
      }

      private bool _autoWide;
      internal bool AutoWide
      {
         get { return _autoWide; }
         set
         {
            bool changed = _autoWide != value;
            _autoWide = value;
            Refresh(changed);
         }
      }

      private bool _rightToLeft;
      internal bool RightToLeft
      {
         get { return _rightToLeft; }
         set
         {
            bool changed = _rightToLeft != value;

            _rightToLeft = value;
            if (changed && GuiMgControl.Type == MgControlType.CTRL_TYPE_COMBO)
               ContentAlignment = (_rightToLeft
                                      ? ContentAlignment.MiddleRight
                                      : ContentAlignment.MiddleLeft);
            Refresh(changed);
         }
      }

      private int _imeMode; // JPN: IME support
      internal int ImeMode
      {
         get { return _imeMode; }
         set { _imeMode = value; }
      }
       
      /// <summary>
      ///   invaldate control
      /// </summary>
      internal virtual void Invalidate(bool wholeParent)
      {
         if (ContainerManager is BasicControlsManager)
         {
            BasicControlsManager staticControlsManager = (BasicControlsManager) ContainerManager;
            if (staticControlsManager != null)
            {
               if (wholeParent)
               {
                  //invalidate all container control - to remove reminders of old line
                  _containerControl.Invalidate();
                  InvalidateSemiTransparentControls(_containerControl, null);
               }
               else
               {
                  Point offset = staticControlsManager.ContainerOffset();
                  Rectangle rect = getRectangle();
                  rect.Offset(offset.X, offset.Y);
                  _containerControl.Invalidate(rect );
                  InvalidateSemiTransparentControls(_containerControl, (Rectangle?)rect);
               }
               
            }
         }
         else if (ContainerManager is TableManager)
         {
            _containerControl.Invalidate(getRectangle());
         }
      }

      /// <summary>
      /// returns true if control is partially transparent
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      static bool IsSemiTransparentControl(Control control)
      {
         return control is Button;
      }

      /// <summary>
      /// invalidate partially transparent children
      /// </summary>
      /// <param name="container"></param>
      /// <param name="rect"></param>
      void InvalidateSemiTransparentControls(Control container, Rectangle? rect)
      {
         foreach (Control item in container.Controls)
         {
            if (IsSemiTransparentControl(item))
            {
               if (rect == null || item.Bounds.IntersectsWith((Rectangle)rect))
                  item.Invalidate();
            }
         }
      }


      // properties

      /// <summary>
      ///   true if the control' color should be transparent only in owner draw paint, but it is not allowed to be transparent for control's window
      /// </summary>
      internal bool IsTransparentInOwnerDraw { get; set; }

      internal Color getBackgroundColor(bool ownerDraw)
      {
         if (ownerDraw && IsTransparentInOwnerDraw && GuiUtilsBase.isOwnerDrawControl(GuiMgControl))
               return Color.Transparent;
         return BgColor;
      }

      /// <summary>
      ///   get value from child
      /// </summary>
      /// <returns>
      /// </returns>
      internal String getValue()
      {
         Control control = getEditorControl();
         if (control != null)
            return GuiUtilsBase.getValue(control);
         else
            return getSpecificControlValue();
      }

      internal virtual String getSpecificControlValue()
      {
         return Text;
      }

      /// <summary>
      /// </summary>
      /// <param name="control"></param>
      internal void setEditControlProperties(MgTextBox control)
      {
            //fixed defect #142195
            ContentAlignment contentAlignment = Manager.GetSpecialEditLeftAlign() ? ContentAlignment.TopLeft : ContentAlignment;
            ControlUtils.SetContentAlignment(control, contentAlignment);


            //For defect #77033, it is needed that setTextProperties will be called after setting the border (pl refer 
            //Fix Instructions for problem details).
            //But, this created problem for automated tests and defect #109374 was opened for the same (pl refer 
            //Fix Instructions for problem details).
            //So, applying the fix for #77033 only if AllowTesting=No.
            if (GuiUtils.AccessTest)
            setTextProperties(control, Text);

         if ((control).BorderStyle != (_showBorder
                                          ? BorderStyle.FixedSingle
                                          : BorderStyle.None))
            ControlUtils.SetBorder(control, ShowBorder);

         if (!GuiUtils.AccessTest)
            setTextProperties(control, Text);

         GuiUtilsBase.setImeMode(control, _imeMode);
      }


      internal LogicalControl(GuiMgControl guiMgControl, Control containerControl)
      {
         this.GuiMgControl = guiMgControl;
         ZOrder = guiMgControl.ControlZOrder;
         _containerControl = containerControl;
         //this.mgRow = mgRow;
         _imeMode = -1;
         PlacementData = new PlacementData(new Rectangle());
         InitializeContentAlignment();
         WordWrap = true; //for edit this property will update according to the Horizontal scroll bar property
         Enabled = true;
         _text = null;
         TextSizeLimit = -1; // -1 to later indicate that the limit was not set.
      }

      /// <summary>
      /// Initialize content alignment
      /// </summary>
      protected virtual void InitializeContentAlignment()
      {
         ContentAlignment = ContentAlignment.MiddleLeft;
      }

      /// <summary>
      ///   returns true if control can be hit
      /// </summary>
      /// <param name = "checkEnable">thue if we need to check if control is enabled
      /// </param>
      /// <returns>
      /// </returns>
      internal bool canHit(bool checkEnable)
      {
         if (!Visible)
            return false;
         if (checkEnable && !Enabled)
            return false;
         return true;
      }

      /// <summary>
      ///   print control's text
      /// </summary>
      /// <param name = "gc"></param>
      /// <param name = "rect"></param>
      /// <param name = "color"></param>
      /// <param name = "str"></param>
      internal virtual void printText(Graphics g, Rectangle rect, Color color, String str)
      {
         FontDescription font = new FontDescription(Font);
         ControlRenderer.PrintText(g, rect, color, font, str, MultiLine, ContentAlignment, Enabled, WordWrap, true, false,
                                   _rightToLeft);
      }

      internal bool isLastFocussedControl()
      {
         bool isLastFocussedCtrl = false;
         Editor editor = GuiUtils.GetTmpEditorFromTagData(GuiUtils.FindForm(_containerControl));

         if (editor != null && editor is Editor)
         {
            Control control = editor.Control;
            if (control != null && !GuiUtilsBase.isDisposed(control))
            {
               MapData mapData = ControlsMap.getInstance().getMapData(control);
               if (mapData != null && GuiMgControl == mapData.getControl() && _mgRow == mapData.getIdx())
                  isLastFocussedCtrl = true;
            }
         }

         return isLastFocussedCtrl;
      }

      /// <summary>
      ///   apply text properties to editor control
      /// </summary>
      /// <param name = "control">
      /// </param>
      /// <param name = "text">
      /// </param>
      protected internal void setTextProperties(TextBox textbox, String text)
      {
         if (textbox.Multiline != _multiLine)
            ControlUtils.SetMultiLine(textbox, MultiLine);

         if (_showVerticalScroll != ((textbox.ScrollBars & ScrollBars.Vertical) == ScrollBars.Vertical))
            GuiUtilsBase.setMultilineVerticalScroll(textbox, _showVerticalScroll);

         // Check if we need to call setMultilineWordWrapScroll - will it really change the textbox property
         bool shouldSetHS = false;
         switch (HorizontalScrollBar)
         {
            case MultilineHorizontalScrollBar.Yes:
               if (textbox.WordWrap)
                  shouldSetHS = true;
               break;

            case MultilineHorizontalScrollBar.No:
               if (textbox.WordWrap)
                  shouldSetHS = true;
               break;

            case MultilineHorizontalScrollBar.WordWrap:
               if (!textbox.WordWrap)
                  shouldSetHS = true;
               break;
         }
         if (shouldSetHS)
            GuiUtilsBase.setMultilineWordWrapScroll(textbox, HorizontalScrollBar);

         if (textbox.AcceptsReturn != _multiLineAllowCR)
            GuiUtilsBase.setMultilineAllowCR(textbox, MultiLineAllowCR);

         ((TagData) textbox.Tag).CheckAutoWide = AutoWide;
         if (text != null)
         {
            // 0 is illegal and will cause a crash.
            if (TextSizeLimit > 0 && textbox.MaxLength != _textSizeLimit)
               GuiUtilsBase.setTextLimit(textbox, TextSizeLimit);

            // QCR #922740, setText unselects the text, we should not perfrom it if not nessesary
            if (!textbox.Text.Equals(text))
               GuiUtilsBase.setText(textbox, text);
            if (textbox.ReadOnly == _modifable)
               GuiUtilsBase.setReadOnly(textbox, !_modifable);
         }
      }

      internal void setCommonProperties(Control control, Color bgColor, Color fgColor)
      {
         if (control.Enabled != _enabled || ((TagData) control.Tag).Enabled != _enabled)
            GuiUtilsBase.setEnabled(control, Enabled);

         if (control.Visible != Visible)
            GuiUtilsBase.setVisible(control, Visible);

         // fixed bug #:942320, the setFont is closed the combo box, need to do it only
         // if the font was changed
         if (Font == null || !Font.Equals(control.Font))
            ControlUtils.SetFont(control, Font);

         GuiUtils.setRightToLeft(control, RightToLeft);
         MgLine line = control as MgLine;
         if(!(line != null && line.CreateAsTableHeaderChild))
            SetColors(control, bgColor, fgColor);         
         GuiUtils.SetControlName(control, Name);
      }

      /// <summary>
      /// Checks if control is placed on table header
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns> 
      private bool ShouldSetHeaderColor(Control control)
      {
         TableControl table = control.Parent as TableControl;
         return table.IsControlOnHeaderArea(control) && (table.GetHeaderBGColor() != Color.Empty);
      }

      private Color GetHeaderBackColor(Control control)
      {
         TableControl table = control.Parent as TableControl;
         return table.GetHeaderBGColor();
      }

      /// <summary>
      /// Wrapped method for Set color with Focused color
      /// </summary>
      /// <param name="control"></param>
      /// <param name="bgColor"></param>
      /// <param name="fgColor"></param>
      protected void SetColors(Control control, Color bgColor, Color fgColor)
      {
         SetColors(control, bgColor, fgColor, false);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="control"></param>
      /// <param name="bgColor"></param>
      /// <param name="fgColor"></param>
      protected void SetColors(Control control, Color bgColor, Color fgColor, bool isFocusColor)
      {
         if ((control is MgLabel) && (control.Parent is TableControl) && ((MgLabel)control).IsTransparentWhenOnHeader && ShouldSetHeaderColor(control))
            bgColor = GetHeaderBackColor(control);
         if (control is MgTextBox && !isFocusColor && Modifable && IsTransparentInOwnerDraw && Manager.Environment.SpecialIgnoreBGinModify)
            bgColor = Color.White;

         ControlUtils.SetBGColor(control, bgColor);

         if (fgColor != null)
            ControlUtils.SetFGColor(control, fgColor);

         ControlUtils.SetGradientColor(control, GradientColor);
         ControlUtils.SetGradientStyle(control, GradientStyle);
      }
      /**
       *  Disposes the logical control.
       *  1. Removes the control from the ControlsMap.
       *  2. Stops the animation if it is on.
       */

      internal virtual void Dispose()
      {
         ControlsMap.getInstance().remove(GuiMgControl, _mgRow);
      }

      /// <summary>
      ///   creates controland coordinator according to the type
      /// </summary>
      /// <param name = "commandType"></param>
      /// <param name = "mgControl"></param>
      /// <param name = "containerControl"></param>
      /// <returns></returns>
      internal static LogicalControl createControl(CommandType commandType, GuiMgControl guiMgControl,
                                                   Control containerControl, int mgRow, int mgColumn)
      {
         LogicalControl lg = createControl(commandType, guiMgControl, containerControl);
         lg._mgRow = mgRow;
         lg._mgColumn = mgColumn;
         ContainerManager containerManager = GuiUtilsBase.getContainerManager(containerControl);
         if (containerManager is TableManager)
         {
            TableCoordinator tableCoordinator = new TableCoordinator((TableManager)containerManager, lg, mgColumn);
            lg._coordinator = tableCoordinator;
            lg._editorProvider = tableCoordinator;
         }
         else if (containerManager is BasicControlsManager)
         {
            EditorSupportingCoordinator editorSupportingCoordinator = new EditorSupportingCoordinator(lg);
            lg._coordinator = editorSupportingCoordinator;
            lg._editorProvider = editorSupportingCoordinator;
         }
         else // tree
            Debug.Assert(false);

         return lg;
      }

      /// <summary>
      ///   creates control according to the type
      /// </summary>
      /// <param name = "commandType"></param>
      /// <param name = "mgControl"></param>
      /// <param name = "containerControl"></param>
      /// <returns></returns>
      private static LogicalControl createControl(CommandType commandType, GuiMgControl guiMgControl,
                                                  Control containerControl)
      {
         switch (commandType)
         {
            case CommandType.CREATE_LABEL:
               return new LgLabel(guiMgControl, containerControl);
            case CommandType.CREATE_EDIT:
               return new LgText(guiMgControl, containerControl);
            case CommandType.CREATE_LINE:
               return new Line(guiMgControl, containerControl);
            case CommandType.CREATE_IMAGE:
               return new LgImage(guiMgControl, containerControl);
            case CommandType.CREATE_BUTTON:
               if (guiMgControl.IsHyperTextButton())
                  return new LgLinkLabel(guiMgControl, containerControl);
               else
                  return new LgButton(guiMgControl, containerControl);

            case CommandType.CREATE_CHECK_BOX:
               return new LgCheckBox(guiMgControl, containerControl);

            case CommandType.CREATE_RADIO_CONTAINER:
               return new LgRadioContainer(guiMgControl, containerControl);

            case CommandType.CREATE_COMBO_BOX:
               return new LgCombo(guiMgControl, containerControl);

            case CommandType.CREATE_RICH_EDIT:
            case CommandType.CREATE_RICH_TEXT:
               return new LgRich(guiMgControl, containerControl);

            default:
               Debug.Assert(false);
               return null;
         }
      }

      /// <summary>
      ///   ofset static controls according to container's offset
      /// </summary>
      /// <param name = "rect"></param>
      internal void offsetStaticControlRect(ref Rectangle rect)
      {
         BasicControlsManager staticControlsManager = ContainerManager as BasicControlsManager;
         if (staticControlsManager != null)
         {
            Point offset = staticControlsManager.ContainerOffset();
            rect.Offset(offset.X, offset.Y);
         }
      }

      /// <summary>
      ///   set the password to edit control
      /// </summary>
      /// <param name = "lg"></param>
      /// <param name = "control"></param>
      internal void setPasswordToControl(Control control)
      {
         MgTextBox textbox = control as MgTextBox;
         if (textbox != null)
            ControlUtils.SetPasswordEdit(textbox, PasswordEdit);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal Control GetContainerControl()
      {
         return _containerControl;
      }

      /// <summary>
      /// get the new colors after the color table changed
      /// </summary>
      /// <param name="refreshNow">should the real controls be refreshed now</param>
      internal virtual void RecalculateColors(bool refreshNow)
      {
         if (MgColorIndex != 0)
         {
            BgColor = ColorIndexToColor(MgColorIndex, true);
            FgColor = ColorIndexToColor(MgColorIndex, false);

            if (refreshNow)
            {
               Control c = getEditorControl();
               if (c != null)
                  SetColors(c, BgColor, FgColor);
            }
         }
      }

      /// <summary>
      /// get the new fonts after the font table changed
      /// </summary>
      internal void RecalculateFonts()
      {
         if (MgFontIndex != 0)
         {
            MgFont mgFont = Manager.GetFontsTable().getFont(MgFontIndex);
            Font = FontsCache.GetInstance().Get(mgFont);

            Control c = getEditorControl();
            if (c != null)
               ControlUtils.SetFont(c, Font);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="colorIndex"></param>
      /// <param name="isBackGround"></param>
      /// <returns></returns>
      protected Color ColorIndexToColor(int colorIndex, bool isBackGround)
      {
         MgColor mgColor = isBackGround ? Manager.GetColorsTable().getBGColor(colorIndex) : Manager.GetColorsTable().getFGColor(colorIndex);
         return ControlUtils.MgColor2Color(mgColor, isBackGround, isBackGround);
      }
   }
}
