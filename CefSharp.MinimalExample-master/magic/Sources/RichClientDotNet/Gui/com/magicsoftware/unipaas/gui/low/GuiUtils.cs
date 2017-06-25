using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.IO;
using System.Globalization;
using com.magicsoftware.controls;
using com.magicsoftware.editors;
using com.magicsoftware.win32;
using Controls.com.magicsoftware.support;

using com.magicsoftware.unipaas.util;
using com.magicsoftware.unipaas.dotnet;
using com.magicsoftware.util;
using com.magicsoftware.controls.utils;
using com.magicsoftware.support;

#if !PocketPC
using VisualStyles = System.Windows.Forms.VisualStyles;
using System.Drawing.Drawing2D;
using System.Reflection;
using Controls.com.magicsoftware.controls.MgLine;
using System.Runtime.InteropServices;
using System.Text;
#else
using TextBox = com.magicsoftware.controls.MgTextBox;
using Panel = com.magicsoftware.controls.MgPanel;
using RightToLeft = com.magicsoftware.mobilestubs.RightToLeft;
using RichTextBox = com.magicsoftware.mobilestubs.RichTextBox;
using FlatStyle = com.magicsoftware.mobilestubs.FlatStyle;
using ImageLayout = com.magicsoftware.mobilestubs.ImageLayout;
using ColorTranslator = OpenNETCF.Drawing.ColorTranslator;
using ButtonBase = OpenNETCF.Windows.Forms.ButtonBase2;
using ContentAlignment = OpenNETCF.Drawing.ContentAlignment2;
using SystemPens = com.magicsoftware.richclient.mobile.gui.SystemPens;
using TextFormatFlags = com.magicsoftware.controls.utils.TextFormatFlags;
using Appearance = com.magicsoftware.mobilestubs.Appearance;
using MainMenu = com.magicsoftware.controls.MgMenu.MgMainMenu;
using MenuItem = com.magicsoftware.controls.MgMenu.MgMenuItem;
using ContextMenu = com.magicsoftware.controls.MgMenu.MgContextMenu;
using LayoutEventArgs = com.magicsoftware.mobilestubs.LayoutEventArgs;
using PointF = com.magicsoftware.mobilestubs.PointF;
#endif

namespace com.magicsoftware.unipaas.gui.low
{
#if !PocketPC
   internal sealed class GuiUtils : GuiUtilsBase
   {
      private static ToolTip _tooltip = new ToolTip();


      /// <summary></summary>
      static GuiUtils()
      {
         // set the properties of the tooltip 
         _tooltip.InitialDelay = 10;
         _tooltip.ReshowDelay = 10;
         _tooltip.OwnerDraw = true;
         _tooltip.Draw += tooltip_Draw;

         ControlUtils.GetSubFormControl += ControlUtils_GetSubFormControl;
         ControlUtils.IsSubFormControl += ControlUtils_IsSubFormControl;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <returns></returns>
      private static bool ControlUtils_IsSubFormControl(object sender)
      {
         bool isSubFormControl = false;
         Control senderAsControl = sender as Control;

         if (senderAsControl != null)
         {
            ControlsMap controlsMap = ControlsMap.getInstance();
            MapData mapDataControl = controlsMap.getMapData(senderAsControl);

            isSubFormControl = (mapDataControl != null && mapDataControl.getControl() != null && 
                               (mapDataControl.getControl().isSubform() || mapDataControl.getControl().isFrameSet()));
         }
         return isSubFormControl;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <returns></returns>
      private static Control ControlUtils_GetSubFormControl(object sender)
      {
         Control returnControl = null;
         Control senderAsControl = sender as Control;

         if (senderAsControl != null)
         {
            if (ControlUtils_IsSubFormControl(senderAsControl))
               return senderAsControl;
            else
               return ControlUtils_GetSubFormControl(senderAsControl.Parent);

         }
         return returnControl;
      }

      /// <summary>
      /// Checks if textbox is empty and has hint
      /// </summary>
      /// <param name="textBoxCtrl"></param>
      /// <returns></returns>
      internal static bool IsEmptyHintTextBox(Control textBoxCtrl)
      {
         return textBoxCtrl is MgTextBox && string.IsNullOrEmpty(((MgTextBox)textBoxCtrl).Text) && !string.IsNullOrEmpty(((MgTextBox)textBoxCtrl).TextBoxStrategy.HintText);
      }

      /// <summary>
      /// sets timeout for tooltip
      /// </summary>
      /// <param name="timeout"></param>
      internal static void setTooltipTimeout(int timeout)
      {
         _tooltip.AutoPopDelay = (timeout < 32 ? timeout : 32) * 1000;
      }

      /// <summary>
      /// Draws the tooltip surface according to the value of the RightToLeft property
      /// of the form to which the associated control belongs.
      /// </summary>
      /// <remarks>QCR #777767: Drawing the tooltip surface was added in order to support right
      /// aligned tooltips</remarks>
      /// <param name="sender">The source of the event.</param>
      /// <param name="e">A DrawToolTipEventArgs that contains the event data.</param>
      static void tooltip_Draw(object sender, DrawToolTipEventArgs e)
      {
         e.Graphics.FillRectangle(SystemBrushes.Info, e.Bounds);
         e.DrawBorder();

         if (e.AssociatedControl.FindForm().RightToLeft != RightToLeft.Yes)
            e.DrawText(TextFormatFlags.Left);
         else
            e.DrawText(TextFormatFlags.RightToLeft | TextFormatFlags.Right);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal static Control createGroupBox()
      {
         GroupBox groupBox = new MgGroupBox();
         new BasicControlsManager(groupBox);
         GroupHandler.getInstance().addHandler(groupBox);
         return (Control)groupBox;
      }

      /// <summary> creates statusBar
      /// 
      /// </summary>
      /// <param name="guiCommand"></param>
      internal static Control createStatusBar(Control control)
      {
         StatusStrip statusBar;
         Form form = getForm(control);
         statusBar = GuiUtils.AccessTest ? new StatusStrip() :  new MgStatusStrip();
         // Fixed bug #:249427, The StatusBar must be create with "AutoSize = true". (his SBPanel must be create with AutoSize=false)
         statusBar.AutoSize = true;
         statusBar.LayoutStyle = ToolStripLayoutStyle.Table;
         statusBar.ShowItemToolTips = true;
         statusBar.Height = (int)((float)statusBar.Height * Utils.GetDpiScaleRatioY(statusBar));
         StatusHandler.getInstance().addHandler(statusBar);
         ((TagData)form.Tag).StatusBarControl = statusBar;
         form.Controls.Add(statusBar);
         return statusBar;
      }

      /// <summary>
      /// Create statusBar Pane
      /// 
      /// </summary>
      /// <param name="statusBar"></param>
      internal static Object createSBPane(StatusStrip statusBar)
      {
         ToolStripStatusLabel statusBarPane = null;
         if (statusBar != null)
         {
            statusBarPane = new ToolStripStatusLabel();
            statusBarPane.AutoSize = false;
            statusBarPane.BackColor = SystemColors.Control;
            statusBarPane.TextAlign = ContentAlignment.MiddleLeft;
            statusBarPane.ImageScaling = ToolStripItemImageScaling.None;
            CreateTagData(statusBarPane);
            StatusPaneHandler.getInstance().addHandler(statusBarPane);
            statusBar.Items.Add(statusBarPane);
         }
         return statusBarPane;
      }

      /// <summary>
      /// create table control
      /// </summary>
      /// <param name="mgControl"></param>
      /// <param name="list"></param>
      /// <param name="columnCount"></param>
      /// <returns></returns>
      internal static Control createTree(GuiMgControl guiMgControl)
      {
         MgTreeView tree = new MgTreeView();
         TreeHandler.getInstance().addHandler(tree);
         new TreeManager(tree, guiMgControl);
         return tree;
      }

      /// <summary> create Rich Edit Control</summary>
      internal static Control createRichEditControl()
      {
         MgRichTextBox richEditCtrl = new MgRichTextBox();
         // QCR# 929805. Set 'DetectUrls' property of the RichEdit control to 
         // false so that URLs will not be converted/formatted to Link.
         richEditCtrl.DetectUrls = false;
         RichEditHandler.getInstance().addHandler(richEditCtrl);
         return (Control)richEditCtrl;
      }

      /// <summary> create Rich Text (static) Control</summary>
      internal static Control createRichTextControl()
      {
         MgRichTextBox richEditCtrl = new MgRichTextBox();
         // QCR# 929805. Set 'DetectUrls' property of the RichEdit control to 
         // false so that URLs will not be converted/formatted to Link.
         richEditCtrl.DetectUrls = false;
         RichEditHandler.getInstance().addHandler(richEditCtrl);
         return (Control)richEditCtrl;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="commandType"></param>
      /// <param name="parent"></param>
      /// <param name="guiMgControl"></param>
      /// <returns></returns>
      internal static Object createSimpleControlForEditor(CommandType commandType, Control parent, GuiMgControl guiMgControl)
      {
         return createControl(commandType, 0, parent, guiMgControl, null, null, 0, false, true, null, 0, null, true, DockingStyle.NONE);
      }

      /// <summary> creates widget according to type</summary>
      /// <param name="commandType"></param>
      /// <param name="style"></param>
      /// <param name="parent"></param>
      /// <param name="mgControl"></param>
      /// <param name="list"></param>
      /// <param name="isFrameSet"></param>
      /// <param name="dockingStyle">docking style of the control.</param>
      /// <pEolumnCount></pEolumnCount>
      /// <returns>control</returns>
      internal static Object createControl(CommandType commandType, int style, Control parent, GuiMgControl guiMgControl, List<String> stringList, List<GuiMgControl> ctrlList, int columnCount, bool isFrameSet, bool forceWindowControl, Type type, int num2, Object tableBehaviour, bool addHandlers, DockingStyle dockingStyle)
      {
         Object obj = null;
         Boolean setParent = true;
         Boolean setTagData = true;
         if (isOwnerDrawControl(guiMgControl) && !forceWindowControl)
         {
            obj = LogicalControl.createControl(commandType, guiMgControl, parent, 0, 0);
            setTagData = false;
            setParent = false;
         }
         else
         {
            switch (commandType)
            {
               case CommandType.CREATE_LABEL:
                  obj = createLabelControl();
                  break;
               case CommandType.CREATE_EDIT:
                  obj = createTextControl(addHandlers, dockingStyle);
                  break;
               case CommandType.CREATE_BUTTON:
                  obj = createButton(guiMgControl.ButtonStyle);
                  break;
               case CommandType.CREATE_CHECK_BOX:
                  obj = createCheckBox();
                  break;
               case CommandType.CREATE_RADIO_CONTAINER:
                  obj = createRadioContainer(guiMgControl);
                  break;

               case CommandType.CREATE_COMBO_BOX:
                  obj = createComboBox();
                  break;
               case CommandType.CREATE_LIST_BOX:
                  obj = createListBox();
                  break;
               case CommandType.CREATE_SUB_FORM:
                  obj = createSubFormControl(parent, isFrameSet);
                  setTagData = false;
                  break;
               case CommandType.CREATE_IMAGE:
                  obj = createImageControl();
                  break;
               case CommandType.CREATE_TABLE:
                  obj = createTableControl(guiMgControl, ctrlList, columnCount, style, tableBehaviour);
                  Form form = FindForm(parent);
                  ((TagData)form.Tag).TableControls.Add((TableControl)obj);
                  setTagData = false;
                  break;
               case CommandType.CREATE_TREE:
                  obj = createTree(guiMgControl);
                  setTagData = false;
                  break;
               case CommandType.CREATE_COLUMN:
                  obj = getTableManager((TableControl)parent).createColumn(guiMgControl, guiMgControl.Layer - 1);
                  setTagData = false;
                  setParent = false;
                  break;
               case CommandType.CREATE_GROUP:
                  obj = createGroupBox();
                  setTagData = false;
                  break;
               case CommandType.CREATE_STATUS_BAR:
                  obj = createStatusBar(parent);
                  setParent = false;
                  break;
               case CommandType.CREATE_FRAME_SET:
                  obj = createMgSplitContainer(parent, style);
                  setTagData = false;
                  break;
               case CommandType.CREATE_FRAME_FORM:
                  obj = createFrameFormControl(parent);
                  setTagData = false;
                  break;
               case CommandType.CREATE_CONTAINER:
                  obj = createContainerControl(parent);
                  setTagData = false;
                  break;
               case CommandType.CREATE_TAB:
                  obj = createTab();
                  setTagData = false;
                  break;
               case CommandType.CREATE_BROWSER:
                  obj = createBrowserControl();
                  break;
               case CommandType.CREATE_RICH_EDIT:
                  obj = createRichEditControl();
                  break;
               case CommandType.CREATE_RICH_TEXT:
                  obj = createRichTextControl();
                  break;
               case CommandType.CREATE_DOTNET:
                  obj = createDotNetControl(type, stringList, num2);
                  setTagData = false;
                  break;
#if !PocketPC
               case CommandType.CREATE_LINE:
                  obj = createLineControl();
                  ((MgLine)obj).CreateAsTableHeaderChild = guiMgControl.IsTableHeaderChild;
                  break;
#endif
               default:
                  throw new ApplicationException("in GuiUtils.createControl(): command type not handled: " + commandType);
            }
         }
         Debug.Assert(obj != null);

         if (setTagData)
            CreateTagData((Control)obj);

         if (setParent && !(parent is TabControl))
         {
            //TODO: Ronak.  Remove the temporary fix (setting size to 0,0 before attaching the control to the parent.
            // I have reverted the change following change because Mercury tests fails for RC Table control. Few mercury
            // tests were updated and few were not updated. After release of 2.1 mercury tests will be modified.
            // Immediately remove the following code and apply the proper fix after release of 2.1.

            //((Control)obj).Hide();

            // Defect 138306: Z-oeder of table header controls is changed when Hide() is called before adding control to parent (TableControl).
            // So, instead just set Size as (0,0) for Table Header controls.

            if (AccessTest || guiMgControl.IsTableHeaderChild)
               ((Control)obj).Size = new Size(0, 0);
            else
               ((Control)obj).Hide();

            parent.Controls.Add((Control)obj);
         }

         if (obj is Control)
         {
            Control control = (Control)obj;
            if (addHandlers)
               setContextMenu(control, null);

            //for ContainerManager save the form on the ContainerManager class
            ContainerManager containerManager = ((TagData)control.Tag).ContainerManager;
            if (containerManager != null)
               containerManager.Form = FindForm(control);

            if (!((TagData)control.Tag).IsDotNetControl && !(control is MgWebBrowser))
               control.AllowDrop = true;
         }
         return obj;
      }

      /// <summary> create the MgSplitContainer
      /// 
      /// </summary>
      /// <param name="parent"></param>
      /// <param name="swtStyle"></param>
      /// <returns></returns>
      private static Control createMgSplitContainer(Control parent, int style)
      {
         MgSplitContainer mgSplitContainer = new MgSplitContainer();
         mgSplitContainer.SetSplitterStyle(style);
         //// create default MinSizeInfo for a each MgSplitContainer
         MinSizeInfo minimumSizeInfo = new MinSizeInfo(mgSplitContainer.getOrientation());
         ((TagData)mgSplitContainer.Tag).MinSizeInfo = minimumSizeInfo;
#pragma warning disable 642
         if (parent is Form)
         {
         }
         //create a default layout data
         else
            setMgSplitContainerData((Control)mgSplitContainer);
#pragma warning restore 642
         //TODO for DSI
         if (parent is Form || parent is Panel)
            mgSplitContainer.Dock = DockStyle.Fill;
         // for the out most MgSplitContainer we need to change the client rect, not to include the offset
         if (parent is Form) //we can't use (GuiUtils.isOutmostMgSplitContainer(this))
            MgSplitContainerHandler.getInstance().addHandler(mgSplitContainer);
         return mgSplitContainer;
      }

      /// <summary> create frame form control</summary>
      /// <param name="parent"></param>
      /// <param name="swtStyle"></param>
      /// <returns></returns>
      private static Panel createFrameFormControl(Control parent)
      {
         Panel panel = new Panel();
         CreateTagData(panel);
         if (parent is MgSplitContainer)
            setMgSplitContainerData(panel);
         else
            Debug.Assert(false);
         return panel;
      }

      /// <summary>create container control</summary>
      /// <param name="parent"></param>
      /// <param name="swtStyle"></param>
      /// <returns></returns>
      private static Panel createContainerControl(Control parent)
      {
         Panel panel = createInnerPanel(parent, false);
         panel.Dock = DockStyle.Fill;//temporary until the placement will work
         panel.SuspendLayout();
         SubformPanelHandler.getInstance().addHandler(panel);
         return panel;
      }

      /// <summary> get the active shell</summary>
      internal static Form getActiveForm()
      {
         Form form = Form.ActiveForm;
         if (form != null && form.IsMdiContainer)
         {
            Form activeMDIChild = GetActiveMDIChild(form);
            if (activeMDIChild != null)
               form = activeMDIChild;
         }
         if (form == null)
         {
            FormCollection OpenForms = Application.OpenForms;
            if (OpenForms.Count > 0)
               form = OpenForms[0];
         }
         return form;
      }

      /// <summary>
      /// Returns the active MDI child
      /// </summary>
      /// <param name="mdiParent"></param>
      /// <returns></returns>
      internal static Form GetActiveMDIChild(Form mdiParent)
      {
         Form activeMDIChild = null;

         Debug.Assert(mdiParent.IsMdiContainer);

         activeMDIChild = mdiParent.ActiveMdiChild;

         if (activeMDIChild != null && (!ControlsMap.isMagicWidget(activeMDIChild) || ((TagData)activeMDIChild.Tag).IsMDIClientForm))
            activeMDIChild = null;

         return activeMDIChild;
      }

      internal static void setEnabled(ToolStripMenuItem menuItem, bool enabled)
      {
         menuItem.Enabled = enabled;
      }
      internal static void setEnabled(ToolStripButton toolItem, bool enabled)
      {
         toolItem.Enabled = enabled;
      }
      internal static void setEnabled(ToolStrip menu, bool enabled)
      {
         menu.Enabled = enabled;
      }

      /// <summary>Set Visible property of ToolStripMenuItem</summary>
      /// <param name="menuItem"></param>
      /// <param name="visible"></param>
      internal static void setVisible(ToolStripMenuItem menuItem, bool visible)
      {
         menuItem.Visible = visible;
      }

      /// <summary>Set Visible property of ToolStripButton</summary>
      /// <param name="toolItem"></param>
      /// <param name="visible"></param>
      internal static void setVisible(ToolStripButton toolItem, bool visible)
      {
         toolItem.Visible = visible;
      }

      /// <summary>Set Visible property of ToolStripSeparator</summary>
      /// <param name="toolStrip"></param>
      /// <param name="visible"></param>
      internal static void setVisible(ToolStripSeparator toolStrip, bool visible)
      {
         toolStrip.Visible = visible;
      }


      /// <summary>sets the tooltip on the control</summary>
      /// <param name="control"></param>
      /// <param name="tooltipStr"></param>
      internal static void setTooltip(Control control, String tooltipStr)
      {
         if (control != null)
         {
            String prevTooltipStr = getTooltip(control);
            String tooltipStrMLS = null;
            if (tooltipStr == null || tooltipStr == "")
               tooltipStrMLS = "";
            else
            {
               tooltipStrMLS = Events.Translate(tooltipStr);

               // QCR#297266: Tootip should not show accelerator, hence use 2 ampersands
               tooltipStrMLS = tooltipStrMLS.Replace("&", "&&");
            }

            // prevTooltipStr can never be null. If from a ctrl with a tooltip never set,
            // it is empty str. And, Magic never sets it to null.
            if (!prevTooltipStr.Equals(tooltipStrMLS))
               _tooltip.SetToolTip(control, tooltipStrMLS);
         }
      }

      /// <summary>sets the tooltip on the control</summary>
      /// <param name="toolStripLabel"></param>
      /// <param name="tooltipStr"></param>
      internal static void setTooltip(ToolStripLabel toolStripLabel, String tooltipStr)
      {
         if (toolStripLabel != null)
         {
            if (tooltipStr == null)
               tooltipStr = "";
            toolStripLabel.ToolTipText = tooltipStr;
         }
      }

      /// <summary>return the tooltip attached to the control</summary>
      /// <param name="control"></param>
      /// <returns></returns>
      internal static String getTooltip(Control control)
      {
         String tooltipStr = "";
         if (control != null)
            tooltipStr = _tooltip.GetToolTip(control);
         return tooltipStr;
      }

      /// <summary> </summary>
      /// <param name="control"></param>
      /// <param name="text"></param>
      internal static void setToolstripText(ToolStripItem control, String text)
      {
         if (control != null)
         {
            // QCR #916542: ampersands are translated to underscores by the ToolStripItem therfore we should
            // first double them
            text = text.Replace("&", "&&");
            control.Text = text;
         }
         else
            throw new ApplicationException("in GuiUtils.setText()");
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="richEdit"></param>
      /// <param name="readOnly"></param>
      internal static void setReadOnly(RichTextBox richEdit, bool readOnly)
      {
         richEdit.ReadOnly = readOnly;
      }

      /// <summary>
      /// Set the contextMenu of the control to null
      /// </summary>
      /// <param name="control"></param>
      internal static void clearContextMenu(Control control)
      {
         // dispose the context menu.
         if (control.ContextMenuStrip != null)
         {
            control.ContextMenuStrip.Dispose();
            control.ContextMenuStrip = null;
         }
      }

      /// <summary>
      /// create Tag data to ToolStripItem
      /// </summary>
      /// <param name="control"></param>
      internal static void CreateTagData(ToolStripItem toolStripItem)
      {
         if (toolStripItem.Tag == null)
            toolStripItem.Tag = new TagData();
         else
            Debug.Assert(false);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="alignmentTypeHori"></param>
      /// <returns></returns>
      internal static FormStartPosition WindowPosition2StartupPosition(WindowPosition mgWindowPosition)
      {
         FormStartPosition formStartPosition = FormStartPosition.Manual;
         switch (mgWindowPosition)
         {
            case WindowPosition.CenteredToMagic:
            case WindowPosition.CenteredToParent://The form is centered within the bounds of its parent form.
            //formStartPosition = FormStartPosition.CenterParent;
            //not need break;
            case WindowPosition.CenteredToDesktop: // Calculation for location is done in startupPosition()
            case WindowPosition.Customized://The position of the form is determined by the Location property.
               formStartPosition = FormStartPosition.Manual;
               break;
            case WindowPosition.DefaultBounds:
               //The form opens with the default location and size given by the operating system. 
               //The Top, Left, Width, and Height properties are ignored.
               formStartPosition = FormStartPosition.WindowsDefaultBounds;
               break;
            case WindowPosition.DefaultLocation:
               // The form will be opened at a default location 
               // The width and height of form will be set determined by width and height properties.
               formStartPosition = FormStartPosition.WindowsDefaultLocation;
               break;
            default:
               Debug.Assert(false);
               break;
         }
         return formStartPosition;
      }

      /// <summary>
      /// Change the cursor of the control and its children to ensure a consistent cursor for 
      /// all the controls because the .NET default behavior is to allow some child controls to
      /// have different cursor than their parent control.
      /// </summary>
      /// <param name="control"></param>
      internal static void SetCursor(Control control, Cursor cursor)
      {
         foreach (Control child in control.Controls)
            SetCursor(child, cursor);
         try
         {
            control.Cursor = cursor;
         }
         catch (Exception)
         {
            //Do nothing.
            //The WebBrowser control does not support the Cursor property.
            //If we try to set cursor on WebBrowser control, exception is thrown.
            //We should ignore this exception.
         }
      }

      /// <summary> Mask character for password edit controls </summary>
      /// <returns> The echo character is the character that is displayed when the user enters text or 
      /// the text is changed by the programmer.
      /// </returns>
      internal static char getCharPass()
      {
         if (_charPass == null)
         {
            // for password edit only:
            // The echo character is the character that is displayed when the user enters text or the
            // text is changed by the programmer.
            using (TextBox text = new TextBox())
            {
               text.UseSystemPasswordChar = true;
               _charPass = text.PasswordChar;
            }
         }
         return (char)_charPass;
      }

      /// <summary>
      /// change color for rich text box
      /// </summary>
      /// <param name="rtb"></param>
      internal static void changeColor(RichTextBox rtb)
      {
         ColorDialog cd = new ColorDialog();
         cd.Color = rtb.SelectionColor;
         cd.AllowFullOpen = true;   // allow custom colors          
         cd.FullOpen = true;   // shows custom colors automatically
         if (cd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            rtb.SelectionColor = cd.Color;
      }

      /// <summary>
      /// change font for rich text box
      /// </summary>
      /// <param name="rtb"></param>
      internal static void changeFont(RichTextBox rtb)
      {
         FontDialog fd = new FontDialog();
         fd.Font = rtb.SelectionFont;
         fd.ShowColor = false;
         fd.ShowApply = false;
         if (fd.ShowDialog() != System.Windows.Forms.DialogResult.Cancel)
            rtb.SelectionFont = fd.Font;
      }

      /// <summary>
      /// set  MDI frame
      /// </summary>
      /// <param name="form"></param>
      internal static Control SetMDIFrame(Form form)
      {
         form.IsMdiContainer = true;
         new MDIClientNativeWindow(form);
         MdiClient mdiClient = (MdiClient)form.Controls[0];
         SetBoubleBufferStyle(mdiClient);
         //set like in inner panel
         new BasicControlsManager(mdiClient);
         ((TagData)mdiClient.Tag).IsInnerPanel = true;
         mdiClient.BackgroundImageLayout = ImageLayout.None;
         GuiUtils.setContextMenu(mdiClient, null);
         return mdiClient;
      }

      /// <summary>
      /// sets DoubleBuffer style using reflection
      /// </summary>
      /// <param name="mdiClient"></param>
      private static void SetBoubleBufferStyle(MdiClient mdiClient)
      {
         try
         {
            // Prevent flickering, only if our assembly
            // has reflection permission.
            Type mdiType = typeof(MdiClient);
            ControlStyles styles = ControlStyles.DoubleBuffer;
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            MethodInfo method = mdiType.GetMethod("SetStyle", flags);
            object[] param = { styles, true };
            method.Invoke(mdiClient, param);
         }
         catch (System.Security.SecurityException)
         {
            /*Don't do anything!!! This code is running under 
            partially trusted context*/
         }
      }

      /// <summary>
      /// return true if form is maximized in MDI frame, in this case it 
      /// affects minimum size of the mdi frame itself
      /// </summary>
      /// <param name="form"></param>
      /// <returns></returns>
      static internal bool isMaximizedInMDIFrame(Form form)
      {
         TagData tagData = form.Tag as TagData;

         // tagData is null for forms created from .NET snippets
         if (tagData == null)
            return false;

         WindowType WindowType = ((TagData)form.Tag).WindowType;
         return (WindowType == WindowType.FitToMdi || //fit to mdi form
         (WindowType == WindowType.MdiChild && form.MdiParent.ActiveMdiChild == form &&
         form.WindowState == FormWindowState.Maximized)); //active maximized mdi child
      }

      /// <summary>
      /// return true if form is minimized
      /// considers mdi children that are minimized along with the mdi container
      /// </summary>
      /// <param name="form"></param>
      /// <returns></returns>
      internal static bool IsFormMinimized(Form form)
      {
         if (form.WindowState == FormWindowState.Minimized)
            return true;
         if (form.IsMdiChild)
         {
            if (isMaximizedInMDIFrame(form) && form.MdiParent.WindowState == FormWindowState.Minimized)
               return true;
         }
         return false;
      }

      /// <summary>
      /// calculate minimum width according to grag rectangle
      /// origin:  on GUI::CalcWindowMinWidth
      /// </summary>
      /// <param name="form"></param>
      /// <param name="dragRect"></param>
      /// <returns></returns>
      internal static int calcWindowMinWidth(Form form, NativeWindowCommon.RECT dragRect)
      {
         MinSizeInfo msi = getMinSizeInfo(form);
         Point pt = msi.getMinSize(true);
         int minWidth = form.Size.Width - form.ClientSize.Width + pt.X;
         NativeWindowCommon.RECT wRect;
         IntPtr handle = isMaximizedInMDIFrame(form) ? form.MdiParent.Handle : form.Handle;
         NativeWindowCommon.GetWindowRect(handle, out wRect);
         if (dragRect.right - dragRect.left < minWidth)
            minWidth = Math.Min(wRect.right - wRect.left, minWidth);
         return minWidth;
      }

      /// calculate minimum height according to grag rectangle
      /// origin:  on GUI::CalcWindowMinHeight
      internal static int calcWindowMinHeight(Form form, NativeWindowCommon.RECT dragRect)
      {
         MinSizeInfo msi = getMinSizeInfo(form);
         Point pt = msi.getMinSize(true);
         Size clientAreaMinSize = computeClientSize(form, new Size(0, pt.Y));
         int NonClientAreaHeight = form.Size.Height - form.ClientSize.Height;
         int minHeight = clientAreaMinSize.Height + NonClientAreaHeight;
         NativeWindowCommon.RECT wRect;
         IntPtr handle = isMaximizedInMDIFrame(form) ? form.MdiParent.Handle : form.Handle;
         NativeWindowCommon.GetWindowRect(handle, out wRect);
         if (dragRect.bottom - dragRect.top < minHeight)
            minHeight = Math.Min(wRect.bottom - wRect.top, minHeight);
         return minHeight;
      }

      /// <summary>
      /// restrict window according to minimum width and height
      /// origin : gui.cpp RestrictWindowSizingRect
      /// </summary>
      /// <param name="SizingEdge"></param>
      /// <param name="MinWidth"></param>
      /// <param name="MinHeight"></param>
      /// <param name="DragRect"></param>
      internal static void RestrictWindowSizingRect(NativeWindowCommon.SizingEdges SizingEdge, int MinWidth, int MinHeight, ref NativeWindowCommon.RECT DragRect)
      {
         switch (SizingEdge)
         {
            case NativeWindowCommon.SizingEdges.WMSZ_RIGHT:
            case NativeWindowCommon.SizingEdges.WMSZ_TOPRIGHT:
            case NativeWindowCommon.SizingEdges.WMSZ_BOTTOMRIGHT:
               DragRect.right = Math.Max(DragRect.right, DragRect.left + MinWidth);
               break;
            case NativeWindowCommon.SizingEdges.WMSZ_LEFT:
            case NativeWindowCommon.SizingEdges.WMSZ_TOPLEFT:
            case NativeWindowCommon.SizingEdges.WMSZ_BOTTOMLEFT:
               DragRect.left = Math.Min(DragRect.left, DragRect.right - MinWidth);
               break;
         }
         // Take care of window height
         switch (SizingEdge)
         {
            case NativeWindowCommon.SizingEdges.WMSZ_BOTTOM:
            case NativeWindowCommon.SizingEdges.WMSZ_BOTTOMLEFT:
            case NativeWindowCommon.SizingEdges.WMSZ_BOTTOMRIGHT:
               DragRect.bottom = Math.Max(DragRect.bottom, DragRect.top + MinHeight);
               break;
            case NativeWindowCommon.SizingEdges.WMSZ_TOP:
            case NativeWindowCommon.SizingEdges.WMSZ_TOPLEFT:
            case NativeWindowCommon.SizingEdges.WMSZ_TOPRIGHT:
               DragRect.top = Math.Min(DragRect.top, DragRect.bottom - MinHeight);
               break;
         }
      }

      /// <summary>
      /// Update drag rectangle by minimum width/height
      /// </summary>
      /// <param name="sizingEdge"></param>
      /// <param name="dragRect"></param>
      /// <param name="form"></param>
      internal static void UpdateByMinimun(NativeWindowCommon.SizingEdges sizingEdge, ref NativeWindowCommon.RECT dragRect, Form form)
      {
         int minHeight = calcWindowMinHeight(form, dragRect);
         int minWidth = calcWindowMinWidth(form, dragRect);
         RestrictWindowSizingRect(sizingEdge, minWidth, minHeight, ref dragRect);
      }

      /// <summary>
      /// Sets the selection mode of list box.
      /// </summary>
      /// <param name="listBox">List box object</param>
      /// <param name="mode">selection mode(one\multi extended)</param>
      internal static void SetSelectionMode(ListBox listBox, SelectionMode mode)
      {
         listBox.SelectionMode = mode;
      }


      /// <summary> sets the image of a ToolStripItem. </summary>
      /// <param name="toolStripItem"></param>
      /// <param name="image"></param>
      /// <param name="imageStyle"></param>
      internal static void SetImage(ToolStripItem toolStripItem, Image image, CtrlImageStyle imageStyle)
      {
         //Dispose the existing image before setting the new one.
         if (toolStripItem.Image != null)
            toolStripItem.Image.Dispose();

         toolStripItem.Image = ImageUtils.GetImageByStyle(image, imageStyle, toolStripItem.Size, false);
      }

      /// <summary>
      /// Gets logical and physical control and returns if new physical control should be created.
      /// </summary>
      /// <param name="control"></param>
      /// <param name="lg"></param>
      /// <returns></returns>
      internal static bool ShouldCreateControl(Control control, LogicalControl lg)
      {
         bool oldHasHint = (control is MgTextBox) && (control as MgTextBox).TextBoxStrategy.IsHintTextHasValue;
         bool newHasHint = (lg is LgText) && (lg as LgText).IsHintEnabled ;
         return oldHasHint || newHasHint;
      }
   }
#else //PocketPC
   internal sealed class GuiUtils : GuiUtilsBase
   {
      /// <summary>
      /// 
      /// </summary>
      /// <param name="commandType"></param>
      /// <param name="parent"></param>
      /// <param name="mgControl"></param>
      /// <returns></returns>
      internal static Object createSimpleControlForEditor(CommandType commandType, Control parent, GuiMgControl guiMgControl)
      {
         return createControl(commandType, 0, parent, guiMgControl, null, null, 0, false, true, null, 0, null, true, DockingStyle.NONE);
      }

      /// <summary> creates widget according to type</summary>
      /// <param name="commandType"></param>
      /// <param name="style"></param>
      /// <param name="parent"></param>
      /// <param name="mgControl"></param>
      /// <param name="list"></param>
      /// <param name="isFrameSet"></param>
      /// <param name="addDefaultHandlers">Flag indicating to attach default event handlers or not.</param>
      /// <pEolumnCount></pEolumnCount>
      /// <param name="dockingStyle">docking style of the control.</param>
      /// <returns>control</returns>
      internal static Object createControl(CommandType commandType, int style, Control parent, GuiMgControl guiMgControl, List<String> stringList, List<GuiMgControl> ctrlList, int columnCount, bool isFrameSet, bool forceWindowControl, Type type, int num2, Object tableBehaviour, bool addDefaultHandlers, DockingStyle dockingStyle)
      {
         Object obj = null;
         Boolean setParent = true;
         Boolean setTagData = true;

         if (isOwnerDrawControl(guiMgControl) && !forceWindowControl)
         {
            obj = LogicalControl.createControl(commandType, guiMgControl, parent, 0, 0);
            setTagData = false;
            setParent = false;
         }
         else
         {
            switch (commandType)
            {
               case CommandType.CREATE_LABEL:
                  obj = createLabelControl();
                  break;

               case CommandType.CREATE_EDIT:
                  obj = createTextControl(addDefaultHandlers, dockingStyle);
                  break;

               case CommandType.CREATE_BUTTON:
                  obj = createButton(guiMgControl.ButtonStyle);
                  break;

               case CommandType.CREATE_CHECK_BOX:
                  obj = createCheckBox();
                  break;

               case CommandType.CREATE_RADIO_CONTAINER:
                  obj = createRadioContainer(guiMgControl);
                  break;

               case CommandType.CREATE_COMBO_BOX:
                  obj = createComboBox();
                  break;

               case CommandType.CREATE_LIST_BOX:
                  obj = createListBox();
                  break;

               case CommandType.CREATE_SUB_FORM:
                  obj = createSubFormControl(parent, isFrameSet);
                  setTagData = false;
                  break;

               case CommandType.CREATE_IMAGE:
                  obj = createImageControl();
                  break;

               case CommandType.CREATE_TABLE:
                  obj = createTableControl(guiMgControl, ctrlList, columnCount, style, tableBehaviour);
                  setTagData = false;
                  break;

#if !PocketPC //tmp
                  case CommandType.CREATE_TREE:
                  obj = createTree(guiMgControl);
                  setTagData = false;
                  break;
#endif
               case CommandType.CREATE_COLUMN:
                  obj = getTableManager((TableControl)parent).createColumn(guiMgControl, guiMgControl.Layer - 1);
                  setTagData = false;
                  setParent = false;
                  break;

#if !PocketPC //tmp
                  case CommandType.CREATE_GROUP:
                  obj = createGroupBox();
                  setTagData = false;
                  break;

                  case CommandType.CREATE_STATUS_BAR:
                  obj = createStatusBar(parent);
                  setParent = false;
                  break;

                  case CommandType.CREATE_FRAME_SET:
                  obj = createMgSplitContainer((Control)parent, style);
                  setTagData = false;
                  break;

                  case CommandType.CREATE_FRAME_FORM:
                  obj = createFrameFormControl((Control)parent);
                  setTagData = false;
                  break;

                  case CommandType.CREATE_CONTAINER:
                  obj = createContainerControl((Control)parent);
                  setTagData = false;
                  break;
#endif
               case CommandType.CREATE_TAB:
                  obj = createTab();
                  setTagData = false;
                  break;

               case CommandType.CREATE_BROWSER:
                  obj = createBrowserControl();
                  break;

#if !PocketPC //tmp
                  case CommandType.CREATE_RICH_EDIT:
                  obj = createRichEditControl();
                  break;

                  case CommandType.CREATE_RICH_TEXT:
                  obj = createRichTextControl();
                  break;
#endif
               case CommandType.CREATE_DOTNET:
                  obj = createDotNetControl(type, stringList, num2);
                  setTagData = false;
                  break;

               default:
                  throw new ApplicationException("in GuiUtils.createControl(): command type not handled: " + commandType);
            }
         }
         Debug.Assert(obj != null);
         if (setParent && !(parent is TabControl))
         {
            // Whenever we add a control to a parent, it is painted at the default location, 
            // irrespective of whether parent's layout is suspended or not (I tested this in 
            // a sample .Net application as well).
            // Now, when we refresh the control's location based on the properties, it is 
            // visually seen that a control was first placed at a different location and 
            // then moved somewhere else.
            // So, hide all the controls before adding them to their parents and make them 
            // visible (if Visible property is TRUE) only after re-locating them as per their 
            // navigation properties.
            //((Control)obj).Hide();

            // If we hide the control, we need to show them. And because of this, #310473 was fixed 
            // in ContainerManaer.toControl -> control.Show(). This fix (#310473) should be applied 
            // to TreeManager.showTmpEditor().
            //
            // As of now we cannot relocate the fix of #310473 to TreeManager, as Mercury Tests for 
            // table control was developed based on this fix. If we relocated the fix from ContainerManager
            // to TreeManager, then it Mercury tests fails. (#713922)
            //
            // As we can't relocate the fix, it creates a problem for controls(Radio, Image, Button) which are 
            // on table control, when nos of records are less than the table control can accommodate. (#778264, 729792).
            // i.e.  : Consider that we have a table control having edit and radio control. Now if nos of 
            //         records are 5 but table can accommodate 10 records, then remaining 5 radio controls 
            //         are displayed at 0,0 location of table control.
            //
            // Hence considering all scenarios, the fix will be, to reset the size of the created control 
            // before setting its parent.

            ((Control)obj).Size = new Size();
            parent.Controls.Add((Control)obj);
         }
         if (setTagData)
         {
            CreateTagData((Control)obj);
         }
         if (obj is Control)
         {
            Control control = (Control)obj;
            if (addDefaultHandlers == true)
               setContextMenu(control, null);
            //for ContainerManager save the form on the ContainerManager  class
            ContainerManager containerManager = ((TagData)control.Tag).ContainerManager;
            if (containerManager != null)
               containerManager.Form = FindForm(control);
         }
#if PocketPC
         // Add the dummy menu also for logical edit/.net controls and for columns - otherwise the 
         // creation of the tmpEditor causes resize and redraw of the form
         if (commandType == CommandType.CREATE_EDIT || commandType == CommandType.CREATE_DOTNET ||
             commandType == CommandType.CREATE_COLUMN)
         {
            //#981405 Add dummy menu to mobile form. This is require to show the keyboard icon on menu
            // Later in case if mobile form has a menu then this menu will be replaced by real menu.
            Form form = FindForm(parent);
            if (form.Menu == null)
               form.Menu = new MainMenu();
         }
#endif
         return obj;
      }

      /// <summary> IsXPStylesActive: for mobile, just say XP style is active for now
      /// </summary>
      /// <returns></returns>
      internal static bool IsXPStylesActive()
      {
         return true;
      }

      /// <summary> get the active shell</summary>
      internal static Form getActiveForm()
      {
         throw new NotImplementedException("getActiveForm");
      }

      /// <summary>
      /// Set the contextMenu of the control to null
      /// </summary>
      /// <param name="control"></param>
      internal static void clearContextMenu(Control control)
      {
         // dispose dummy context menu.
         if (control.ContextMenu != null &&
             ((com.magicsoftware.controls.MgMenu.MgContextMenu)control.ContextMenu).Name == "Dummy")
         {
            control.ContextMenu.Dispose();
            control.ContextMenu = null;
         }
      }
       
      /// <summary> Mask character for password edit controls </summary>
      /// <returns> The echo character is the character that is displayed when the user enters text or 
      /// the text is changed by the programmer.
      /// </returns>
      internal static char getCharPass()
      {
         if (_charPass == null)
         {
            // for password edit only:
            // The echo character is the character that is displayed when the user enters text or the
            // text is changed by the programmer.
            _charPass = '*';
         }
         return (char)_charPass;
      }
   }

#endif

   /// <summary>utils that are required & can be built on compact framework</summary>
   internal class GuiUtilsBase
   {
#if !PocketPC
      internal static DroppedData DroppedData { get; set; }
      internal static DraggedData DraggedData { get; set; }
#endif

      internal static bool AccessTest { get; set; }
      protected static char? _charPass;
      internal static Control ControlToDispose { get; set; }

      private enum ButtonState
      {
         FOCUS = 0,
         SELECTED = 1, // MOUSE_DOWN
         DISABLE = 2,
         NORMAL = 3,
         HOTTRACK = 4,
         DEFAULT_BUTTON = 5
      }

      ///<summary>
      /// Set RightToLeft prop of a control
      ///</summary>
      ///<param name="control">!!.</param>
      ///<param name="rightToLeftval">!!.</param>
      ///<returns>!!.</returns>
      internal static void setRightToLeft(Control control, bool rightToLeftval)
      {
         if ((control is Panel || ControlUtils.IsMdiClient(control)) && ((TagData)control.Tag).IsClientPanel)
            control = getForm(control);

         if (!ControlUtils.SetRightToLeft(control, rightToLeftval))
            Events.WriteDevToLog("RightToLeft is not supported for control " + control.GetType().Name);
      }

      /// <summary></summary>
      /// <param name="control"></param>
      /// <param name="mgFont"></param>
      /// <returns></returns>
      internal static PointF GetFontMetrics(Control control, MgFont mgFont)
      {
         PointF retPoint = Utils.GetFontMetricsByMgFont(control, mgFont);

         return retPoint;
      }


      /// <summary> Gets the resolution of the control. </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      internal static Point GetResolution(Control control)
      {
         return Utils.GetResolution(control);
      }

      /// <summary>return full coordinates of the controlstakes in consideration corner of parent</summary>
      /// <param name="control"></param>
      /// <returns></returns>
      internal static Rectangle getBounds(Control control)
      {
         Point offset = revertOffset(parentCornerOffset(control));
         Rectangle rect = control.Bounds;
         rect.Offset(offset.X, offset.Y);
         return rect;
      }

      /// <summary>revert offset</summary>
      /// <param name="offset"></param>
      /// <returns></returns>
      internal static Point revertOffset(Point offset)
      {
         return new Point(-offset.X, -offset.Y);
      }

      /// <summary>return corner offset of parent - relevant only when parent is client panel</summary>
      /// <param name="control"></param>
      /// <returns></returns>
      internal static Point parentCornerOffset(Control control)
      {
         Point offset = new Point();
         Control parent = control.Parent;
         if (parent != null && parent is Panel && parent.Tag is TagData && ((TagData)parent.Tag).IsInnerPanel)
            offset = ((Panel)parent).AutoScrollPosition;
         return offset;
      }

      /// <summary>get rect related to</summary>
      /// <param name="rect"></param>
      /// <param name="parentCtrl"></param>
      /// <param name="relativeCtrlObject"></param> relative window, if null - it is desctop 
      internal static void getRectRelatedTo(ref Rectangle rect, Control parentCtrl, Object relativeCtrlObject)
      {
         Point pointOnParent = new Point(rect.X, rect.Y);
         Point resultPoint = parentCtrl.PointToScreen(pointOnParent);
         if (relativeCtrlObject != null) // if relativeCtrlObject == null we need desctop coordinates
            resultPoint = ((Control)relativeCtrlObject).PointToClient(resultPoint);

         rect.X = resultPoint.X;
         rect.Y = resultPoint.Y;
      }

      /// <summary> return saved rectangle</summary>
      /// <param name="control"></param>
      /// <returns></returns>
      internal static Rectangle? getSavedBounds(Control control)
      {
         TagData td = (TagData)control.Tag;
         return td.Bounds;
      }

      /// <summary></summary>
      /// <param name="ctrl"></param>
      /// <param name="radioSuggestedValue"></param>
      static internal void setSuggestedValueOfChoiceControlOnTagData(Control ctrl, String suggestedValue)
      {
         ((TagData)(ctrl.Tag)).SuggestedValueOfChoiceControl = suggestedValue;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="ctrl"></param>
      /// <param name="getRadioSuggestedValue"></param>
      static internal void setGetSuggestedValueOfChoiceControlOnTagData(Control ctrl, bool getSuggestedValueOfChoiceControl)
      {
         ((TagData)(ctrl.Tag)).GetSuggestedValueOfChoiceControl = getSuggestedValueOfChoiceControl;
      }

      /// <summary> </summary>
      /// <param name="obj"></param>
      /// <returns></returns>
      static internal String getValue(Object obj)
      {
         String str = "";

         if (obj != null)
         {
            if (obj is TextBox || obj is Label || obj is MgLinkLabel)
               str = ((Control)obj).Text;
            else if (obj is MgCheckBox)
               str = getCheckBoxValue(((MgCheckBox)obj).CheckState);
            else if (obj is RadioButton)
            {
               RadioButton button = ((RadioButton)obj);
               ControlsMap controlsMap = ControlsMap.getInstance();
               // fixed bug #:938734: new Definition by Eyal.r(diffrent from Online).
               // Once you select radio option,it is possible to de-select it.
               TagData tgParent = (TagData)button.Parent.Tag;

               if ((tgParent.GetSuggestedValueOfChoiceControl))
                  str = tgParent.SuggestedValueOfChoiceControl;
               else if (button.Checked)
                  str = GetRadioButtonIndex(button);
               else
                  str = "";
            }
            else if (obj is MgRadioPanel)
            {
               str = "" + GuiConstants.DEFAULT_LIST_VALUE;
               MgRadioPanel mgr = (MgRadioPanel)obj;
               TagData tg = (TagData)mgr.Tag;

               if (tg.GetSuggestedValueOfChoiceControl)
                  str = tg.SuggestedValueOfChoiceControl;
               else
               {
                  Object selectedButton = getSelectedRadioControl((MgRadioPanel)obj);
                  if (selectedButton != null)
                     str = getValue(selectedButton);
               }
            }
            else if (obj is TabControl)
            {
               Control ctrl = (Control)obj;
               TagData tg = (TagData)ctrl.Tag;

               if (tg.GetSuggestedValueOfChoiceControl)
                  str = tg.SuggestedValueOfChoiceControl;
               else
                  str = Convert.ToString(((TabControl)obj).SelectedIndex);
            }
            else if (obj is MgWebBrowser)
            {
               if (((MgWebBrowser)obj).Url != null)
                  str = ((MgWebBrowser)obj).Url.ToString();
            }
            else if (obj is ListControl /*|| (obj is TabFolder)*/)
            {
               int val = GuiConstants.DEFAULT_VALUE_INT;

               Control ctrl = (Control)obj;
               TagData tg = (TagData)ctrl.Tag;
#if !PocketPC
               // If ListBox has selection mode set to multiple then get all selected indices.
               if (((ctrl is ListBox) && ((ListBox)ctrl).SelectionMode != SelectionMode.One))
                  str = GetListBoxSelectedIndice((ListBox)ctrl);
               else
#endif
                  if (tg.GetSuggestedValueOfChoiceControl)
                  str = tg.SuggestedValueOfChoiceControl;
               else
               {
                  if (obj is ListControl)
                     val = ((ListControl)obj).SelectedIndex;

                  val = (val < 0 ? GuiConstants.DEFAULT_VALUE_INT : val);
                  str = System.Convert.ToString(val);
               }
            }
#if !PocketPC  //tmp
            else if (obj is GroupBox)
               str = ((GroupBox)obj).Text;
#endif
            else if (obj is LogicalControl)
               str = ((LogicalControl)obj).getValue();
            else if (obj is RichTextBox)
               str = ((RichTextBox)obj).Rtf;
            else if (obj is DateTimePicker)
            {
               DateTimePicker datePicker = (DateTimePicker)obj;
               str = String.Format("{0:" + datePicker.CustomFormat + "}", datePicker.Value);
            }
            else
            {
               //set the assert only when it is not DOTNET control
               if (obj is Control)
               {
                  Control ctrl = (Control)obj;
                  if (!((TagData)ctrl.Tag).IsDotNetControl && !(ctrl is MgButtonBase))
                     Debug.Assert(false);
               }
               else
                  Debug.Assert(false);
            }
         }
         return str;
      }

      /// <summary>return the check Box value</summary>
      /// <param name="checkBox"></param>
      /// <returns></returns>
      static internal String getCheckBoxValue(CheckState checkState)
      {
         String str = "";
         if (checkState == CheckState.Checked)
            str = "1";//"" + MgCheckState.CHECKED;
         else if (checkState == CheckState.Unchecked)
            str = "0";//"" + MgCheckState.UNCHECKED;
         else if (checkState == CheckState.Indeterminate)
            str = "";// "" + MgCheckState.INDETERMINATE;

         return str;
      }

      /// <summary> returns control of the radio that is selected</summary>
      /// <param name="control"></param>
      /// <returns></returns>
      internal static Control getSelectedRadioControl(MgRadioPanel mgRadioPanel)
      {
         Control retControl = null;

         foreach (Control child in mgRadioPanel.Controls)
         {
            if (((RadioButton)child).Checked)
            {
               retControl = child;
               break;
            }
         }
         return retControl;
      }

      /// <summary>RC mobile: 'System.Windows.Forms.Control' and 'System.Windows.Forms.Form' don't support 'FindForm'.
      /// Required change: to replace the method Control.FindForm() with a call to a new method 
      /// GuiUtils.FindForm() that will climb up the parents till it find the forms.</summary>
      /// <param name="obj"></param>
      /// <returns></returns>
      internal static Form FindForm(Control control)
      {
         //if the send obj is control climb up the parents till it find the form. 
         while (control != null && !(control is Form))
         {
            if (control.Parent != null)
               control = control.Parent;
            else
            {
               TagData tg = (TagData)control.Tag;
               if (control.Parent == null && tg != null && tg.ContainerTabControl != null)
                  control = tg.ContainerTabControl;
               else
                  control = control.Parent;
            }
         }

#if !PocketPC
         if (control != null && ControlsMap.isMagicWidget(control) && ((TagData)control.Tag).IsMDIClientForm)
            control = ((Form)control).MdiParent;
#endif

         return (Form)control;
      }

      /// <summary>Return the form related to the given object. If the object is a Form then return it.
      /// If the object is an SDI client area panel then return its form. All other cases
      /// are faults.</summary>
      /// <param name="obj"></param>
      /// <returns></returns>
      internal static Form getForm(Object obj)
      {
         if (obj is Form)
            return (Form)obj;
         else if ((obj is Panel || ControlUtils.IsMdiClient(obj)) && ((TagData)((Control)obj).Tag).IsClientPanel)
         {
            Form form = (Form)((Control)obj).Parent;

#if !PocketPC
            if (((TagData)form.Tag).IsMDIClientForm)
               form = form.MdiParent;
#endif

            return form;
         }

         return null;
      }

#if !PocketPC
      /// <summary>
      /// find top level form 
      /// if we have a child form - find its parent form
      /// </summary>
      /// <param name="form"></param>
      /// <returns></returns>
      internal static Form FindTopLevelForm(Form form)
      {
         while (!form.TopLevel && !form.IsMdiChild)
            form = FindForm(form.Parent);
         return form;
      }
#endif

      /// <summary>
      /// find top level parent form, when the <form>'s windowType is childwindow.
      /// </summary>
      /// <param name="form"></param>
      /// <returns></returns>
      internal static Form getTopLevelFormForChildWindow(Form form)
      {
         Form topMostForm = form;

         if (((TagData)form.Tag).WindowType == WindowType.ChildWindow)
         {
            topMostForm = form.ParentForm;
            while (((TagData)topMostForm.Tag).WindowType == WindowType.ChildWindow)
               topMostForm = topMostForm.ParentForm;
         }

         return topMostForm;
      }

      /// <summary> get focused control usig data saved on form
      /// </summary>
      /// <returns></returns>
      internal static Control getFocusedControl()
      {
         Form form = GuiUtils.getActiveForm();
         return getFocusedControl(form);
      }

      /// <summary>
      /// get focused control of the current form
      /// </summary>
      /// <param name="form"></param>
      /// <returns></returns>
      internal static Control getFocusedControl(Form form)
      {
         Control control = null;
         if (form != null && form.Tag is TagData)
            control = ((TagData)(form.Tag)).LastFocusedControl;

         return control;
      }

      /// <summary>create Tag data to control</summary>
      /// <param name="control"></param>
      internal static void CreateTagData(Control control)
      {
         if (control.Tag == null)
            control.Tag = new TagData();
         else
            Debug.Assert(false);
      }

      /// <summary>create Tag data to control</summary>
      /// <param name="control"></param>
      internal static void CreateTagData(ToolBarButton control)
      {
         if (control.Tag == null)
            control.Tag = new TagData();
         else
            Debug.Assert(false);
      }

      /// <summary>Create Tag data to MainMenu</summary>
      /// <param name="menu"></param>
      internal static void CreateTagData(MainMenu menu)
      {
         if (menu.Tag == null)
            menu.Tag = new TagData();
         else
            Debug.Assert(false);
      }

      /// <summary>Create Tag data for MenuItem</summary>
      /// <param name="menuItem"></param>
      internal static void CreateTagData(MenuItem menuItem)
      {
         if (menuItem.Tag == null)
            menuItem.Tag = new TagData();
         else
            Debug.Assert(false);
      }

      /// <summary>Create Tag data for ContextMenu</summary>
      /// <param name="menu"></param>
      internal static void CreateTagData(ContextMenu menu)
      {
         if (menu.Tag == null)
            menu.Tag = new TagData();
         else
            Debug.Assert(false);
      }

      /// <summary> Gets the inner control of a Control. 
      /// If the control is TabControl, it returns the TabControlPanel</summary>
      /// <param name="obj">the control</param>
      /// <returns></returns>
      internal static Control getInnerControl(Control control)
      {
         Control inner = control;
         if (control is TabControl)
         {
            TabControl tabControl = (TabControl)control;
            inner = ((TagData)tabControl.Tag).TabControlPanel;
         }

         return inner;
      }

      /// <summary>create a Label control</summary>
      /// <returns>Control</returns>
      internal static Control createLabelControl()
      {
         MgLabel label = new MgLabel();
         label.AutoSize = false;
         LabelHandler.getInstance().addHandler(label);
         return label;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal static Control createLineControl()
      {
#if !PocketPC
         return new MgLine();
#else
         return null;
#endif
      }

      /// <summary>create a dotnet Control </summary>
      /// <param name="type">Type of DN object</param>
      /// <param name="dnEventsNames">comma-delimited string of events that should be raised for the object</param>
      /// <param name="key">key of the DNObjectsCollection, where the DN control created will be kept.</param>
      /// <returns></returns>
      internal static Control createDotNetControl(Type type, List<String> dnEventsNames, int key)
      {
         Control ctrl = null;

         if (type != null)
         {
            //create instance of the type
            Object instance = ReflectionServices.CreateInstance(type, null, null);

            if (instance != null)
            {
               //In case, if there is no DN reference variable attached, the key will be 0 => no need to update collection.
               if (key > 0)
                  DNManager.getInstance().DNObjectsCollection.Update(key, instance);

               ctrl = (Control)instance;
#if !PocketPC
               //automatically set AutoSize = false to support magic bounds
               PropertyInfo autoSizeProp = ReflectionServices.GetMemeberInfo(ctrl.GetType(), "AutoSize", false, null) as PropertyInfo;
               if (autoSizeProp != null)
                  ReflectionServices.SetPropertyValue(autoSizeProp, ctrl, null, false);
#endif
               CreateTagData(ctrl);
               ((TagData)ctrl.Tag).IsDotNetControl = true;
               ((TagData)ctrl.Tag).ApplicationHookedDNeventsNames = dnEventsNames;

               // add handlers
               DotNetHandler.getInstance().addHandler(ctrl, dnEventsNames);
            }
         }

         return ctrl;
      }

      /// <summary>
      /// Update the dot net object only (no events) in the DNObjectsCollection for the given key.
      /// </summary>
      /// <param name="key"></param>
      /// <param name="obj"></param>
      internal static void AttachDNKeyToObject(int key, Object obj)
      {
         DNManager.getInstance().DNObjectsCollection.Update(key, obj);
      }

      /// <summary> create Text Control</summary>
      /// <param name="addDefaultHandlers">flag indicating whether to attach default event handlers or not.</param>
      /// <param name="dockingStyle">docking style of the control.</param>
      /// <returns>Control</returns>
      internal static Control createTextControl(bool addDefaultHandlers, DockingStyle dockingStyle)
      {
         MgTextBox textBox = new MgTextBox();
         textBox.RightToLeft = RightToLeft.No;

         //Add the common handler(s).These handlers are be attached to control always. 
         TextBoxHandler.getInstance().addCommonHandlers(textBox);

         if (addDefaultHandlers)
            TextBoxHandler.getInstance().addHandler(textBox);

         //Add docking style for control.
         switch (dockingStyle)
         {
            case DockingStyle.BOTTOM:
               textBox.Dock = DockStyle.Bottom;
               break;
            case DockingStyle.FILL:
               textBox.Dock = DockStyle.Fill;
               break;
            case DockingStyle.LEFT:
               textBox.Dock = DockStyle.Left;
               break;
            case DockingStyle.NONE:
               textBox.Dock = DockStyle.None;
               break;
            case DockingStyle.RIGHT:
               textBox.Dock = DockStyle.Right;
               break;
            case DockingStyle.TOP:
               textBox.Dock = DockStyle.Top;
               break;
         }

         return (Control)textBox;
      }

      /// <summary> create MgButton by parent</summary>
      internal static Control createButton(CtrlButtonTypeGui buttonStyle)
      {
         Control control = null;

         if (buttonStyle == CtrlButtonTypeGui.Hypertext)
            control = createLinkLabelControl();
         else
         {
            MgButtonBase button;
            if (buttonStyle == CtrlButtonTypeGui.Push)
               button = new MgPushButton();
            else
               button = new MgImageButton();

            button.IsBasePaint = AccessTest;

            control = button;
            ButtonHandler.getInstance().addHandler(button);
         }

         return control;
      }

      /// <summary> create Checkbox by parent</summary>
      internal static Control createCheckBox()
      {
         MgCheckBox checkBox = new MgCheckBox();
         checkBox.FlatStyle = FlatStyle.System;
         ButtonHandler.getInstance().addHandler(checkBox);
         return (Control)checkBox;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal static Control createImageControl()
      {
         PictureBox pictureBox = new MgPictureBox();
         PictureBoxHandler.getInstance().addHandler(pictureBox);
         return (Control)pictureBox;
      }

      /// <summary> create Link Label control(button style = HyperText)</summary>
      internal static Control createLinkLabelControl()
      {
         MgLinkLabel linkLabel = new MgLinkLabel();
         LabelHandler.getInstance().addHandler(linkLabel);
         linkLabel.BackColor = SystemColors.Control;
         linkLabel.FlatStyle = FlatStyle.System;
         linkLabel.UseMnemonic = true;

         ControlUtils.SetContentAlignment(linkLabel, ContentAlignment.MiddleCenter);

         return (Control)linkLabel;
      }

      /// <summary> create Browser Control</summary>
      internal static Control createBrowserControl()
      {
         MgWebBrowser webBrowser = new MgWebBrowser();
         BrowserHandler.getInstance().addHandler(webBrowser);
         return (Control)webBrowser;
      }

      /// <summary> create a combo box on panel
      /// </summary>
      /// <returns></returns>
      internal static Control createComboBox()
      {
         MgComboBox comboBox = new MgComboBox();

         ComboHandler.getInstance().addHandler(comboBox);
         return (Control)comboBox;
      }

      /// <summary>
      /// create a list box on panel
      /// </summary>
      /// <returns></returns>
      internal static Control createListBox()
      {
         MgListBox listBox = new MgListBox();
         ListHandler.getInstance().addHandler(listBox);
         return (Control)listBox;
      }

      /// <summary>
      /// create table control
      /// </summary>
      /// <param name="mgControl"></param>
      /// <param name="list"></param>
      /// <param name="columnCount"></param>
      /// <param name="style"></param>
      /// <param name="tableBehaviour">the behaviour of the table control</param>
      /// <returns></returns>
      internal static Control createTableControl(GuiMgControl mgControl, List<GuiMgControl> list, int columnCount, int style, Object tableBehaviour)
      {
         TableControl tableControl = null;

         if (((TableBehaviour)tableBehaviour) == TableBehaviour.UnlimitedItems)
         {
            tableControl = new TableControlUnlimitedItems();
            new TableManagerUnlimitedItems(tableControl, mgControl, list, columnCount, style);
         }
         else
         {
            tableControl = new TableControlLimitedItems();
            new TableManagerLimitedItems(tableControl, mgControl, list, columnCount, style);
         }

         tableControl.ShowContextMenu = Events.IsContextMenuAllowed(mgControl);
         TableHandler.getInstance().addHandler(tableControl);
         return tableControl;
      }

      /// <summary>
      /// create the radio container
      /// </summary>
      /// <returns></returns>
      internal static Control createRadioContainer(GuiMgControl guiMgControl)
      {
         MgRadioPanel mgRadioPanel = new MgRadioPanel();
         mgRadioPanel.SuspendLayout();
         RadioPanelHandler.getInstance().addHandler(mgRadioPanel);
         return mgRadioPanel;
      }

      /// <summary>
      /// create a radio button on panel
      /// </summary>
      /// <param name="mgControl"></param>
      /// <returns></returns>
      internal static Control createRadioButton(MgRadioPanel mgRadioPanel)
      {
         Debug.Assert(mgRadioPanel != null);

         MgRadioButton button = new MgRadioButton();
         button.AutoCheck = false;
         button.IsBasePaint = AccessTest;
         mgRadioPanel.Controls.Add(button);
         RadioButtonHandler.getInstance().addHandler(button);
#if PocketPC
         // No layout event on mobile - perform the radio panel layout
         mgRadioPanel.LayoutEngine.Layout(mgRadioPanel, new LayoutEventArgs());
#endif

         button.Tag = new TagData();

#if !PocketPC
         button.AllowDrop = true;
#endif

         return button;
      }

      ///<summary>
      ///  create Tab
      ///</summary>
      ///<returns></returns>
      internal static Control createTab()
      {
         MgTabControl tab = new MgTabControl();
         tab.IsBasePaint = true; //TODO: Kaushal. Should be set to false once Runtime eliminates the use of Panel.
         CreateTagData(tab);
         TabHandler.getInstance().addHandler(tab);

         Panel panel = new MgPanel();
#if PocketPC
         // On Mobile, the background color is not initialized, because Control.DefaultBackColor 
         // does not exist
         panel.BackColor = SystemColors.Control;
#endif
         panel.Dock = DockStyle.Fill;
         new BasicControlsManager(panel);
         TabControlPanelHandler.getInstance().addHandler(panel);

         ((TagData)panel.Tag).ContainerTabControl = tab;
         ((TagData)tab.Tag).TabControlPanel = panel;

         // set dummy context to the panel in order to catch menu opening event.
         setContextMenu(panel, null);

#if !PocketPC
         // Always set it true, because for Drag & Drop, we need a DragOver Event to
         // retrieve the MapData and control. We will use control.TagData's / LogicalControl's
         // AllowDrop to check whether the drop is allowed on control / LogicalControl.
         panel.AllowDrop = true;
#endif

         return (Control)tab;
      }


      /// <summary>
      /// reset the focus color
      /// </summary>
      /// <param name="textCtrl"></param>
      internal static void ResetFocusColor(TextBox textCtrl)
      {
         if (GuiUtils.AccessTest)
         {
            TagData tag = (TagData)textCtrl.Tag;
            if (tag.OrgFocusBGColor != null && tag.OrgFocusFGColor != null)
            {
               textCtrl.BackColor = (Color)tag.OrgFocusBGColor;
               tag.OrgFocusBGColor = null;

               textCtrl.ForeColor = (Color)tag.OrgFocusFGColor;
               tag.OrgFocusFGColor = null;
            }
         }
      }


      /// <summary>
      /// set the focus color
      /// </summary>
      /// <param name="textCtrl"></param>
      internal static void SetFocusColor(TextBox textCtrl)
      {
         if (GuiUtils.AccessTest)
         {
            ResetFocusColor(textCtrl);

            TagData tag = (TagData)textCtrl.Tag;
            if (tag.FocusBGColor != null && tag.FocusFGColor != null)
            {
               tag.OrgFocusBGColor = textCtrl.BackColor;
               textCtrl.BackColor = (Color)tag.FocusBGColor;

               tag.OrgFocusFGColor = textCtrl.ForeColor;
               textCtrl.ForeColor = (Color)tag.FocusFGColor;
            }
         }
      }

      /// <summary> </summary>
      /// <param name="control"></param>
      /// <param name="text"></param>
      internal static void setText(Control control, String text)
      {
         if (control is Form)
         {
            ((TagData)control.Tag).TextToDisplay = text;

            //when showTitleBar= true the text should be at lest one ' '
            if (((TagData)control.Tag).ShowTitleBar && String.IsNullOrEmpty(text))
               text = " ";
            //when showTitleBar= false the text must be empty 
            //#435503: To hide the title bar but show text in task bar do not set form's text to blank string 
            // when border style = No Border and title bar = No.
            else if (!((TagData)control.Tag).ShowTitleBar
                     && ((Form)control).FormBorderStyle != FormBorderStyle.None && !String.IsNullOrEmpty(text))
               text = String.Empty;
         }

         if (control is RichTextBox)
         {
            RichTextBox richEditCtrl = (RichTextBox)control;
#if !PocketPC
            ControlUtils.SetTextFotRichTextBox(richEditCtrl, text);
#endif
            ((TagData)richEditCtrl.Tag).RtfValueBeforeEnteringControl = richEditCtrl.Rtf;
         }
         else
            ControlUtils.SetText(control, text);
      }

      /// <summary> check if the paste action needs to be enabled or disabled.</summary>
      /// <param name="ctrl">never send null , it might cause problems.</param>
      /// <param name="checkClipContent"></param>
      static internal void checkPasteEnable(GuiMgControl guiMgCtrl, bool checkClipContent)
      {
         bool enable = false;

         // enable only for text ctrl that is also modifiable.
         if (guiMgCtrl != null)
         {
            // should we check the content or assume it exists ?
            if (checkClipContent)
            {
               try
               {
                  enable = ClipboardDataExists(NativeWindowCommon.CF_TEXT);
               }
               catch (System.Runtime.InteropServices.ExternalException e)
               {
                  Events.WriteExceptionToLog("checkPasteEnable: " + e.Message);
               }
            }
            else
               enable = true;
            Events.OnEnablePaste(guiMgCtrl, enable);
         }
      }



      /// <summary>
      /// Gets the data on the clipboard in the format specified by the selected item of the specified listbox.
      /// </summary>
      internal static string GetClipboardData(uint format)
      {
         String result = null;
         if (NativeWindowCommon.OpenClipboard(IntPtr.Zero))
         {
            //Get handle of clipboard data in the selected format
            IntPtr hClip = NativeWindowCommon.GetClipboardData(format);
            if (hClip != IntPtr.Zero)
            {

               IntPtr gLock = NativeWindowCommon.GlobalLock(hClip);
               UIntPtr length = NativeWindowCommon.GlobalSize(hClip);

               //Init a buffer which will contain the clipboard data
               byte[] buffer = new byte[(int)length];

               //Copy clipboard data to buffer
               Marshal.Copy(gLock, buffer, 0, (int)length);
               if (format == NativeWindowCommon.CF_UNICODETEXT)
                  result = Encoding.Unicode.GetString(buffer);
               else
                  result = Encoding.Default.GetString(buffer);

               int index = result.IndexOf('\0');
               if (index > -1)
                  result = result.Substring(0, index);

               NativeWindowCommon.GlobalUnlock(hClip);
            }
            NativeWindowCommon.CloseClipboard();
         }

         return result;
      }

      /// <summary>
      /// check if clipboard contains data of the format
      /// </summary>
      /// <param name="format"></param>
      /// <returns></returns>
      internal static bool ClipboardDataExists(uint format)
      {
         bool result = false;

         if (NativeWindowCommon.OpenClipboard(IntPtr.Zero))
         {
            //Get handle of clipboard data in the selected format
            IntPtr hClip = NativeWindowCommon.GetClipboardData(format);
            result = hClip != IntPtr.Zero;
            NativeWindowCommon.CloseClipboard();
         }

         return result;
      }

      /// <summary> Disable paste, check not needed.</summary>
      /// <param name="ctrl"></param>
      static internal void disablePaste(GuiMgControl ctrl)
      {
         Events.OnEnablePaste(ctrl, false);
      }

      /// <summary> check if any acts should be enabled\disabled due to gui activity.</summary>
      /// <param name="control"></param>
      /// <param name="editControl">Called only from gui thread.</param>
      static internal void enableDisableEvents(Object control, GuiMgControl editControl)
      {
         if (control is TextBoxBase)
         {
            // disable/enable cut/copy
            bool enableCutCopy = (((TextBoxBase)control).SelectionLength != 0);

            // Copy/Cut should not be allowed for Password edit controls.
            if (control is TextBox && ((TextBox)control).UseSystemPasswordChar)
               enableCutCopy = false;

            Events.OnEnableCutCopy(editControl, enableCutCopy);
         }
      }

      /// <summary> check if the control needs auto wide after press any chars, or after set any text</summary>
      /// <param name="editControl"></param>
      /// <param name="textBox"></param>
      /// <param name="text"></param>
      internal static void checkAutoWide(GuiMgControl editControl, TextBoxBase textBox, String text)
      {
         bool checkAutoWide;
         // check if we need to ignore check auto wide
         checkAutoWide = ((TagData)textBox.Tag).CheckAutoWide;

         if (checkAutoWide)
         {
            Size textExt = Utils.GetTextExt(textBox.Font, text, textBox);
            Rectangle rect = textBox.ClientRectangle;

            if (textExt.Width > rect.Width)
               Events.OnWide(editControl);
         }
      }

      /// <summary>
      /// checks and closes tree editor if needed. performed on click
      /// </summary>
      /// <param name="clickedControl">clicked tree control</param>
      internal static void checkAndCloseTreeEditorOnClick(Control clickedControl)
      {
         ControlsMap controlsMap = ControlsMap.getInstance();

         Editor editor = GetTmpEditorFromTagData(FindForm(clickedControl));
         if (editor != null)
         {
            Control control = editor.Control;
            if (isTreeEditor(control))
            {
               // click on tree editor should not close editor
               if (control != clickedControl)
               {
                  MapData mapData = controlsMap.getMapData(control);
                  if (mapData != null)
                     Events.OnEditNodeExit(mapData.getControl(), mapData.getIdx());
               }
            }
         }
      }

      /// <summary>return true if control is tree editor</summary>
      /// <param name="control"></param>
      /// <returns></returns>
      internal static bool isTreeEditor(Control control)
      {
         if (control != null && !isDisposed(control) && control.Parent is TreeView)
            return ((TagData)control.Tag).IsEditor;
         return false;
      }

      /// <summary></summary>
      /// <param name="control"></param>
      /// <returns></returns>
      internal static bool isDisposed(Control control)
      {
#if !PocketPC
         return control != null ? control.IsDisposed : true;
#else
         return false;
#endif
      }

      /// <summary></summary>
      /// <param name="control"></param>
      /// <returns></returns>
      internal static void performLayout(Control control)
      {
#if !PocketPC
         control.PerformLayout(control, "");
#endif
      }

      internal static void resumeLayout(Control control, bool value)
      {
         control.ResumeLayout(value);
      }

      /// <summary> Translate WinForms.Keys.Modifier into magic modifiers</summary>
      /// <param name="modifiers">event's modifiers</param>
      /// <returns> Magic Modifier</returns>
      internal static Modifiers getModifier(Keys modifiers)
      {
         if ((modifiers & Keys.Control) != 0)
         {
            if ((modifiers & Keys.Shift) != 0)
               return Modifiers.MODIFIER_SHIFT_CTRL;
            else if ((modifiers & Keys.Alt) != 0)
               return Modifiers.MODIFIER_ALT_CTRL;
            else
               return Modifiers.MODIFIER_CTRL;
         }
         else if ((modifiers & Keys.Alt) != 0)
         {
            if ((modifiers & Keys.Shift) != 0)
               return Modifiers.MODIFIER_ALT_SHIFT;
            else
               return Modifiers.MODIFIER_ALT;
         }
         else if ((modifiers & Keys.Shift) != 0)
            return Modifiers.MODIFIER_SHIFT;
         else
            return Modifiers.MODIFIER_NONE;
      }

      /// <summary> perform setBounds and save the rectangle on the control</summary>
      /// <param name="control"></param>
      /// <param name="rect"></param>
      internal static void setBounds(Control control, Rectangle rect)
      {
         Rectangle orgRect = rect;

         if (control is TableControl)
         {
            TableControl tableControl = (TableControl)control;
            TableManager tableManager = getTableManager((TableControl)control);

            if (tableManager.BottomPositionInterval == BottomPositionInterval.RowHeight &&
                rect.Height > tableControl.TitleHeight)
            {
               // if bottom position interval is rowHeight, reduce the height of the table to ignore the partial row.

               int tableHeight = Math.Max(0, (rect.Height - (tableControl.TitleHeight + (tableControl.BorderHeight * 2))));

               int rowsInPage = 0;
               int rowHeight = 0;

               // If table has row placement, rows in page remains same and row height changes.
               // else, row height remains same and rows in page changes.
               if (tableControl.HasRowPlacement)
               {
                  rowsInPage = tableControl.RowsInPage;
                  if (rowsInPage > 0)
                     rowHeight = tableHeight / rowsInPage;
               }
               else
               {
                  rowHeight = tableControl.RowHeight;
                  if (rowHeight > 0)
                     rowsInPage = tableHeight / rowHeight;
               }

               rect.Height = tableControl.TitleHeight + (rowsInPage * rowHeight) + (tableControl.BorderHeight * 2);
            }
         }

         if (((TagData)control.Tag).IsClientPanel)
            control = getForm(control);

         if (control is Form)
         {
            Point offset = parentCornerOffset(control);
            rect.Offset(offset.X, offset.Y);
            control.Location = rect.Location;
            control.ClientSize = rect.Size;
         }
         else
         {
            Point parentOffset = parentCornerOffset(control);
            rect.Offset(parentOffset.X, parentOffset.Y);
            control.Bounds = rect;

            if (control is MgComboBox)
               ControlUtils.SetComboBoxItemHeight(control, rect.Height);

#if PocketPC
            if (control is MgComboBox)
               ((MgComboBox)control).DropDownWidth = rect.Width;
            else if (control is MgRadioPanel)
               // No layout event on mobile - perform the radio panel layout
               ((MgRadioPanel)control).LayoutEngine.Layout(control, new LayoutEventArgs());
#endif
         }

         saveBounds(control, orgRect);
      }

      /// <summary>save bounds of form
      /// when we save form bounds we save its client area size instead of bounds size
      /// location we save as it is</summary>
      /// <param name="form"></param>
      internal static void saveFormBounds(Form form)
      {
         Rectangle rect = form.Bounds;
         rect.Size = form.ClientSize;
         if (form.WindowState == FormWindowState.Normal)
         {
            saveBounds(form, rect);
            saveDesktopBounds(form);
         }
         setLastWindowState(form);
      }



      /// <summary> save the last window state on form's tag
      /// 
      /// </summary>
      /// <param name="form"></param>
      internal static void setLastWindowState(Form form)
      {
         TagData td = (TagData)form.Tag;

#if !PocketPC
         if (form.WindowState != FormWindowState.Minimized)
#endif
            td.LastWindowState = form.WindowState;
      }


      /// <summary> save the rectangle on the control</summary>
      /// <param name="control"></param>
      /// <param name="rect"></param>
      internal static void saveBounds(Control control, Rectangle rect)
      {
         TagData td = (TagData)control.Tag;
         td.Bounds = rect;
      }

      /// <summary> return true if this control is the direct child of the out most MgSplitContainer</summary>
      /// <param name="control"></param>
      /// <returns></returns>
      internal static bool isDirectChildOfOutmostMgSplitContainer(Control control)
      {
         bool isDirectChild = false;
         Control parent = control.Parent;

         if (parent != null && isOutmostMgSplitContainer(parent))
            isDirectChild = true;

         return isDirectChild;
      }

      /// <summary> return true if this control is out most MgSplitContainer</summary>
      /// <param name="control"></param>
      /// <returns></returns>
      internal static bool isOutmostMgSplitContainer(Control control)
      {
         return (control is MgSplitContainer && control.Parent is Form);
      }

      /// <summary> </summary>
      /// <param name="pt"></param>
      /// <param name="decrease">TODO</param>
      /// <param name="setWidth"></param>
      internal static void updateFrameSize(ref Point pt, bool decrease)
      {
         int deltaWidth = MgSplitContainer.SPLITTER_WIDTH;
         int deltaHeight = MgSplitContainer.SPLITTER_WIDTH;

         pt.X = Math.Max(0, (pt.X == GuiConstants.DEFAULT_VALUE_INT ? pt.X : pt.X + (decrease ? -deltaWidth : deltaWidth)));
         pt.Y = Math.Max(0, (pt.Y == GuiConstants.DEFAULT_VALUE_INT ? pt.Y : pt.Y + (decrease ? -deltaHeight : deltaHeight)));
      }

      internal static MinSizeInfo getMinSizeInfo(Control control)
      {
         return getMinSizeInfo(control, true);
      }

      /// <summary> </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      internal static MinSizeInfo getMinSizeInfo(Control control, bool createIfNeed)
      {
         MinSizeInfo msi = ((TagData)control.Tag).MinSizeInfo;
         if (createIfNeed && msi == null)
            msi = createMinSizeInfo(control);
         return msi;
      }

      /// <summary> </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      internal static MinSizeInfo createMinSizeInfo(Control control)
      {
         MinSizeInfo msi = new MinSizeInfo(0);
         ((TagData)control.Tag).MinSizeInfo = msi;

         return msi;
      }

      /// <summary> create a copy of rectangle </summary>
      /// <param name="rect"></param>
      /// <returns></returns>
      internal static Rectangle copyRect(Rectangle rect)
      {
         return new Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
      }

      /// <summary> return text control of the object
      /// 
      /// </summary>
      /// <param name="object">
      /// </param>
      /// <returns>
      /// </returns>
      internal static TextBoxBase getTextCtrl(Object obj)
      {
         TextBoxBase textCtrl = null;
         if (obj is TextBoxBase)
            textCtrl = (TextBoxBase)obj;
         else if (obj is LogicalControl)
            textCtrl = (TextBoxBase)((LogicalControl)obj).getEditorControl();

         return textCtrl;
      }



      /// <summary> set focus on control</summary>
      /// <param name="control"></param>
      /// <returns>True, if the focus was shifted to the control.</returns>
      internal static bool setFocus(Control control, bool activateForm, bool setLogicalFocus)
      {
         bool focusChanged = false;
         Control controlToFocus = null;
         Form form = GuiUtils.FindForm(control);

         // if Print Preview is in focus and when it is attempted to focus on any control on Runtime window,
         // neither set focus on it nor activate its parent form. Instead, set that control as active in that form.
         // This prevents runtime window not to get activated.
         // Condition GuiUtils.IsFormMinimized(form) is added for Defect #133234.
         // The problem is that if the form is minimized, framework neither set the focus on the control on which we call Focus() nor sets the form.ActiveControl with it.
         // So, when the form is restored back, the focus remains on the last ActiveControl.
         // As a workaround, if the form is minimized then instead of calling Focus() (which actually does nothing), we will just set the form's ActiveControl.
         // This will ensure that when the form is restored, the focus will be on the desired control.
         setLogicalFocus = setLogicalFocus || PrintPreviewFocusManager.GetInstance().ShouldPrintPreviewBeFocused || GuiUtils.IsFormMinimized(form);

         if (control is MgRadioPanel)
         {
            MgRadioPanel mgRadioPanel = (MgRadioPanel)control;
            Control mouseDownControl = ((TagData)mgRadioPanel.Tag).MouseDownOnControl;
            //fixed bug#:435168, set focus to the mouse down control
            if (mouseDownControl != null)
            {
               controlToFocus = mouseDownControl;
               ((TagData)mgRadioPanel.Tag).MouseDownOnControl = null;
            }
            else
            {
               Control selectedRadioButton = getSelectedRadioControl(mgRadioPanel);
               if (selectedRadioButton != null)
                  controlToFocus = selectedRadioButton;
               else
               {
                  if (mgRadioPanel.Controls.Count > 0)
                  {
                     // we have unselected radio
                     Control firstControl = mgRadioPanel.Controls[0];
                     if (((TagData)firstControl.Tag).IgnoreClick == false &&
                         (getValue(firstControl) == "" + GuiConstants.DEFAULT_VALUE_INT ||
                          getValue(firstControl) == ""))
                     {
                        // QCR #914227, performing focus on unselected radio SWT & DOTNet always selects first radio
                        // this is unwanted behaviour in magic and needs to be undone
                        ((TagData)firstControl.Tag).IgnoreClick = true;
                     }
                     controlToFocus = firstControl;
                  }
               }
            }
         }
         else
         {
#if !PocketPC
            Control acceptButton = (Control)(form.AcceptButton);
            if (acceptButton != null && (TagData)acceptButton.Tag != null)
            {
               TagData tgAcceptButton = (TagData)acceptButton.Tag;
               bool savePrevValueDrawAsDefaultButton = tgAcceptButton.DrawAsDefaultButton;
               bool moveToButtonControl = control is MgButtonBase || control is MgLinkLabel;
               tgAcceptButton.DrawAsDefaultButton = !moveToButtonControl;
               if (savePrevValueDrawAsDefaultButton != tgAcceptButton.DrawAsDefaultButton)
                  GuiUtils.RefreshButtonImage(acceptButton);
            }
#endif
            controlToFocus = control;
         }

         if (AllowFocusOnControl(controlToFocus))
         {
            bool allowFormActivate = true;
            if (controlToFocus is Panel)
            {
               Form activeForm = GuiUtils.getActiveForm();
               allowFormActivate = (activeForm == null || form == activeForm);
            }

            if (activateForm && !setLogicalFocus && allowFormActivate)
               form.Activate();

            if (setLogicalFocus)
               form.ActiveControl = controlToFocus;
            else
               controlToFocus.Focus();

            focusChanged = true;
         }

         return focusChanged;
      }

      /// <summary>
      /// allow focusing on control
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      static bool AllowFocusOnControl(Control control)
      {
         bool result = true;
         if (control == null)
            result = false;
         else if (control.Focused)
            result = false;
         else if (IsDotNetControl(control))
         {
#if !PocketPC
            Control controlInFocus = ControlUtils.GetFocusedControl();
            //if we currently in focus on child of NET control - do not move the focus to .NET control
            //itself - if will interfear with the .NET control 
            //QCR #779290
            if (IsAncestor(control, controlInFocus))
               result = false;
#endif

         }
         return result;
      }

      /// <summary>
      /// returns true if control is magic Dot Net control
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      static bool IsDotNetControl(Control control)
      {
         TagData tagData = (TagData)control.Tag;
         return (tagData != null && tagData.IsDotNetControl);
      }

      /// <summary>
      /// returns true if "ancestor" is ancestor of the "control"
      /// </summary>
      /// <param name="control"></param>
      /// <param name="ancestor"></param>
      /// <returns></returns>
      static bool IsAncestor(Control ancestor, Control control)
      {
         while (control != null)
         {
            if (control == ancestor)
               return true;
            else
               control = control.Parent;
         }
         return false;
      }


      /// <summary> return focus to last focuses control of the form </summary>
      /// <param name="form"></param>
      internal static void restoreFocus(Form form)
      {
         Control control = null;
         if ((TagData)form.Tag != null)
            control = ((TagData)form.Tag).LastFocusedControl;
         if (control != null && !GuiUtils.isDisposed(control))
            setFocus(control, false, false);
      }

      /// <summary> checks if focusing control is control described in mapdata </summary>
      /// <param name="form">form </param>
      /// <param name="mapData"></param>
      /// <returns></returns>
      internal static bool isFocusingControl(Form form, MapData mapData)
      {
         bool ret = false;
         MapData focusMapData = ((TagData)form.Tag).InFocusControl;
         if (focusMapData != null)
            ret = focusMapData.Equals(mapData);
         return ret;
      }

      /// <summary> removes focusing control form the form </summary>
      /// <param name="form"></param>
      internal static void removeFocusingControl(Form form)
      {
         ((TagData)form.Tag).InFocusControl = null;
      }

      /// <summary> save control for whic focus is executing</summary>
      /// <param name="form">form</param>
      /// <param name="mapData">mapdata of focusing control</param>
      internal static void saveFocusingControl(Form form, MapData mapData)
      {
         ((TagData)form.Tag).InFocusControl = mapData;
      }

      /// <summary></summary>
      /// <param name="control"></param>
      /// <param name="Font"></param>
      internal static void setFont(Control control, Font font)
      {
         if (font != null)
            ControlUtils.SetFont(control, font);
#if !PocketPC
         if (control is ComboBox)
            setVisibleLines((ComboBox)control, ((TagData)((ComboBox)control).Tag).VisibleLines, control.Font);
#endif
      }

      /// <summary> </summary>
      /// <param name="combo"></param>
      /// <param name="visibleLines"></param>
      internal static void setVisibleLines(ComboBox combo, int visibleLines, Font font)
      {
#if !PocketPC
         int itemHeight;
         Utils.SetDropDownHeight(combo, visibleLines, font, out itemHeight);
         ((TagData)combo.Tag).VisibleLines = combo.DropDownHeight / itemHeight;
         if (combo is MgComboBox)
            (combo as MgComboBox).VisibleLines = visibleLines;
#else
         Events.WriteDevToLog("setVisibleLines - Not supported in the CF");
#endif
      }



      /// <summary></summary>
      /// <param name="control"></param>
      /// <returns></returns>
      internal static void RefreshButtonImage(Control control)
      {
         if (control is MgImageButton)
         {
            MgImageButton button = (MgImageButton)control;
            if (button.ImageList == null || button.ImageList.Images.Count == 0)
               button.BackgroundImage = null;
            else
            {
               ButtonState Index = ButtonState.NORMAL;
               TagData tg = (TagData)control.Tag;
               bool support6Images = button.Supports6Images();

               if (tg.OnMouseDown)
                  Index = ButtonState.SELECTED;
               else if (support6Images && tg.OnHovering)
                  Index = ButtonState.HOTTRACK;
               else if (!tg.Enabled)
                  Index = ButtonState.DISABLE;
               else if (button.Focused)
                  Index = ButtonState.FOCUS;
               else
               {
                  if (support6Images && tg.DrawAsDefaultButton)
                     Index = ButtonState.DEFAULT_BUTTON;
                  else
                     Index = ButtonState.NORMAL;
               }

               //TODO: Kaushal. Instead of scaling the image here, it should be scaled while setting the ImageList and while resizing he control.
               //This will save some unnecessary scaling.
               button.BackgroundImage = ImageUtils.GetImageByStyle(button.ImageList.Images[(int)Index], CtrlImageStyle.Distorted, control.Size, false);
            }
         }
      }

      /// <summary>
      /// get tab index that is going to be selected after click on the point
      /// </summary>
      /// <param name="tab"></param>
      /// <param name="ptClick"></param>
      /// <returns></returns>
      static int getTabSelectingIndex(TabControl tab, Point ptClick)
      {
#if !PocketPC
         for (int index = 0; index < tab.TabPages.Count; index++)
         {
            // Get the TabPage that contains the click coordinates
            if (tab.GetTabRect(index).Contains(ptClick))
            {
               return index;
            }
         }
#endif
         return tab.SelectedIndex;
      }

      /// <summary> save click coordinates</summary>
      /// <param name="event"></param>
      internal static void SaveLastClickInfo(MapData mapData, Control sender, Point location)
      {
         ControlsMap controlsMap = ControlsMap.getInstance();
         Point ptOrg = location;
         Point client = new Point(1, 1); // default client coordinates
         Point offset = new Point(0, 0); // default window coordinates
         bool LastClickCoordinatesAreInPixels = true; // only for a tab control client coordinates are not in pixels
         Rectangle retRect;
         GuiMgControl guiMgCtrl = mapData.getControl();
         GuiMgForm guiMgForm = mapData.getForm();
         bool isSubform = (guiMgCtrl != null && guiMgCtrl.isSubform());
         String controlName = guiMgCtrl == null ? "" : guiMgCtrl.Name;
         Rectangle rect;

         // For Radio don't evaluate this. 
         // Previously it is not getting evaluated because newObj == sender (same RadioButton).
         if (guiMgCtrl != null && !guiMgCtrl.isRadio())
         {
            Object newObj = controlsMap.object2Widget(guiMgCtrl, mapData.getIdx());
            if (newObj != sender) //in tab for example we have panel for sender but controls object is TabControl
            {
               if (newObj is Control)
                  CalcLastClickInfoForRealControl(newObj, ref location, sender);
               else if (newObj is LogicalControl)
               {
                  LogicalControl l = newObj as LogicalControl;
                  Control editorControl = l.getEditorControl();
                  if (editorControl is Control)
                     CalcLastClickInfoForRealControl(editorControl, ref location, sender);
#if !PocketPC //temp
                  else if (!(newObj is TreeChild)) // tree child is treated as tree control
#endif
                  {
                     LogicalControl logicalControl = (LogicalControl)newObj;
                     rect = logicalControl.getRectangle(); //get logical's control rectangle
                     location.X -= rect.X;
                     location.Y -= rect.Y;

                     // Fixed defect#:136772, handle the scroll bar
                     if (sender is MgPanel)
                     {
                        MgPanel mgpanel = (MgPanel)sender;

                        if (mgpanel.AutoScrollPosition.X < 0)
                           location.X -= mgpanel.AutoScrollPosition.X;

                        if (mgpanel.AutoScrollPosition.Y < 0)
                           location.Y -= mgpanel.AutoScrollPosition.Y;
                     }
                  }
               }
            }
         }
         if (guiMgForm == null && !isSubform)
         {
            guiMgForm = guiMgCtrl.GuiMgForm;
            if (sender is MgTabControl)
            {
               MgTabControl mgTabFolder = (MgTabControl)sender;
               client.X = getTabSelectingIndex(mgTabFolder, location) + 1;
               client.Y = -1;
               LastClickCoordinatesAreInPixels = false;
            }
            else
            {
               if (sender is MgRadioButton)
               {
                  Control radio = sender;
                  retRect = new Rectangle(location.X, location.Y, 0, 0);
                  getRectRelatedTo(ref retRect, sender, radio.Parent);
                  location.X = retRect.X;
                  location.Y = retRect.Y;
               }

               client.X = location.X;
               client.Y = location.Y;
            }

            Panel formComposite = GuiUtils.getSubformPanel(sender);
            if (formComposite == null)
               formComposite = (Panel)controlsMap.object2Widget(guiMgCtrl.GuiMgForm);

            retRect = new Rectangle(ptOrg.X, ptOrg.Y, 0, 0);
            getRectRelatedTo(ref retRect, sender, formComposite);
            offset.X = retRect.X;
            offset.Y = retRect.Y;
         }
         else
         {
            offset = ptOrg;
            if (isSubform)
               guiMgForm = guiMgCtrl.GuiMgForm;
         }

         Events.SaveLastClickInfo(guiMgForm, controlName, client.X, client.Y, offset.X, offset.Y, LastClickCoordinatesAreInPixels);
      }

      /// <summary>
      /// calc last click for real control 
      /// </summary>
      /// <param name="newObj"></param>
      /// <param name="location"></param>
      /// <param name="sender"></param>
      private static void CalcLastClickInfoForRealControl(Object newObj, ref Point location, Control sender)
      {
         Control realControl = (Control)newObj;
         Rectangle retRect = new Rectangle(location.X, location.Y, 0, 0);
         getRectRelatedTo(ref retRect, sender, realControl);
         location.X = retRect.X;
         location.Y = retRect.Y;
      }
      /// <summary> return true for control/form that can have transparent value
      /// 
      /// </summary>
      /// <param name="object">SWT widget
      /// </param>
      /// <returns>
      /// </returns>
      internal static bool supportTransparency(Object obj)
      {
         if (obj is Control)
            return ControlUtils.SupportsTransparency((Control)obj);
         else if (obj is LogicalControl)
         {
            if (obj is LgColumn)
               return false;
            //We don't support transparency for controls on table header except Label control.
            else if ((((LogicalControl)obj).GuiMgControl).IsTableHeaderChild && !(obj is LgLabel))
               return false;
            else if (transparentOnlyInOwnerDraw(((LogicalControl)obj).GuiMgControl))
               return false;
         }
         return true;
      }

      /// <summary>return true if control can be transparent only when owner - drawen</summary>
      /// <param name="ctrl"></param>
      /// <returns></returns>
      internal static bool transparentOnlyInOwnerDraw(GuiMgControl ctrl)
      {
         //TODO: owner draw text support transparency
         return (ctrl.isTextControl() || ctrl.isComboBox());
      }

      /// <summary> set IME mode (JPN: IME support)
      /// </summary>
      /// <param name="control">
      /// <param name="mode">
      /// </param>
      internal static void setImeMode(TextBox control, int mode)
      {
         ((TagData)control.Tag).ImeMode = mode;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="control"></param>
      /// <param name="fileName"></param>
      internal static void setImageInfoOnTagData(Object obj, Image image, CtrlImageStyle imageStyle)
      {
         TagData td = null;
         Control ctrl = obj as Control;

         if (ctrl != null)
            td = (TagData)ctrl.Tag;

         if (td != null)
         {
            td.Image = image;
            td.ImageStyle = imageStyle;
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="ctrl"></param>
      /// <param name="image"></param>
      internal static void setImageList(Control control, ImageList imageList)
      {
         if (control is TreeView)
            ((TreeView)control).ImageList = imageList;
         else if (control is MgTabControl)
            ((MgTabControl)control).ImageList = imageList;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="mgImageButton"></param>
      /// <param name="imageList"></param>
      internal static void setImageList(MgImageButton mgImageButton, MgImageList imageList)
      {
         mgImageButton.ImageList = imageList;
         RefreshButtonImage(mgImageButton);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="control"></param>
      /// <param name="filename"></param>
      internal static void SetImageList(Control control, String filename)
      {
         Debug.Assert(control is MgImageButton || control is TreeView || control is TabControl);

         if (control is MgImageButton)
         {
            MgImageButton mgButton = control as MgImageButton;
            MgImageList mgImageList = getImageListItemForButtonControl(filename, mgButton.PBImagesNumber);
            setImageList(mgButton, mgImageList);
         }
         else if (control is TreeView || control is TabControl)
         {
            ImageList imageList = ImageListCache.GetInstance().Get(filename);
            setImageList(control, imageList);
         }
      }

      /// <summary> Returns the image list from the url string
      /// </summary>
      /// <param name="urlString"></param>
      /// <returns></returns>
      internal static MgImageList getImageListItemForButtonControl(String urlString, int PBImagesNumber)
      {
         MgImageListCacheKey imageListCacheKey = new MgImageListCacheKey(urlString, PBImagesNumber);
         MgImageList imageList = MgImageListCache.GetInstance().Get(imageListCacheKey);
         return imageList;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="obj"></param>
      internal static void setBackgroundImage(Control ctrl)
      {
         Size size = ctrl.Size;

         // If the control is a panel and it has a scrollbar, the actual size can be bigger.
         // Image should be resized based on the actual size not the display size.
         if (ctrl is Panel)
         {
            Size logSize = ((MgPanel)ctrl).AutoScrollMinSize;

            if (logSize.Width > size.Width)
               size.Width = logSize.Width;
            if (logSize.Height > size.Height)
               size.Height = logSize.Height;
         }

         TagData td = (TagData)ctrl.Tag;
         Image image = ImageUtils.GetImageByStyle(td.Image, td.ImageStyle, size, false);

         ControlUtils.SetImage(ctrl, image);
      }


      /// <summary>
      /// get an image from a file.
      /// </summary>
      /// <param name="fileName"></param>
      /// <returns></returns>
      internal static Image getImageFromFile(String fileName)
      {
         Image image = null;

         String trimmedFileName = (fileName != null ? fileName.Trim() : null);
         if (!string.IsNullOrEmpty(trimmedFileName))
            image = ImageLoader.GetImage(trimmedFileName);

         return image;
      }

      /// <summary>
      /// get GrayScaleColor
      /// </summary>
      /// <param name="srcColor"></param>
      /// <returns></returns>
      internal static Color getGrayScaleColor(Color srcColor)
      {
         int RGB = (srcColor.R + srcColor.G + srcColor.B) / 3;
         Color OutputColor = Color.FromArgb(RGB, RGB, RGB);
         return OutputColor;
      }

      /// <summary>
      /// load an image from a byte array.
      /// </summary>
      /// <param name="imageData">content. can be null.</param>
      /// <returns>an iamge, or null if the content is null or empty.</returns>
      internal static Image GetImageFromBinary(byte[] imageData)
      {
         Image image = null;

         try
         {
            if (imageData != null && imageData.Length > 0)
            {
               MemoryStream stream = new MemoryStream(imageData);
               image = ImageUtils.FromStream(stream);
            }
         }
         catch (Exception ex)
         {
            Events.WriteExceptionToLog(ex);
         }

         return image;
      }

      /// <summary></summary>
      /// <param name="ctrl"></param>
      /// <param name="OnClick"></param>
      internal static void SetOnClickOnTagData(Control ctrl, Boolean OnClick)
      {
         ((TagData)(ctrl.Tag)).OnMouseDown = OnClick;
      }

      #region DRAG And DROP
#if !PocketPC
      /// <summary>
      /// handle Drag for MouseDown.
      /// this method is responsible to check whether the drag can be performed on a control or not 
      /// </summary>
      /// <param name="control"></param>
      /// <param name="evtArgs"></param>
      /// <param name="mapData"></param>
      internal static void AssessDrag(Control control, MouseEventArgs evtArgs, MapData mapData)
      {
         bool allowDrag = false;
         bool clickIsOnTableControl = false;
         GuiMgControl mgControl = mapData.getControl();
         Object ctrl = ControlsMap.getInstance().object2Widget(mgControl, mapData.getIdx());

         // Check AllowDrag on TableControl when: 1) Rows are MultiMarked  or 
         //                                       2) Click on table control where there isn't any control.
         if (control is TableControl)
         {
            TableManager tableManager = getTableManager((TableControl)control);
            if (tableManager.IsInMultimark || ((TagData)control.Tag).ContainerManager.HitTest(new Point(((MouseEventArgs)evtArgs).X, ((MouseEventArgs)evtArgs).Y), true, true) == null)
               clickIsOnTableControl = true;
         }

         if ((control is TableControl && clickIsOnTableControl) || control is MgTreeView)
            allowDrag = ((TagData)control.Tag).AllowDrag;
         else if (ctrl is LogicalControl)
            allowDrag = ((LogicalControl)ctrl).AllowDrag;
         else if (ctrl is Control)
            allowDrag = ((TagData)((Control)ctrl).Tag).AllowDrag;

         // If drag can be performed,
         //    - Save the mousedown location / caret position. ( *** TextBox only )
         //    - Marked the selection.                         ( *** TextBox only )
         //    - Initiate DragBoxFromMouseDown.
         if (allowDrag)
         {
            if (control is MgTextBox)
            {
               MgTextBox mgTxtbox = (MgTextBox)control;
               if (mgTxtbox.SaveSelectionLength > 0)
               {
                  mgTxtbox.MouseDownLocation = mgTxtbox.SelectionStart;
                  mgTxtbox.Select(mgTxtbox.SaveSelectionStart, mgTxtbox.SaveSelectionLength);
               }
               else
                  allowDrag = false;   // Don't start Drag when text is not selected in a textbox.
            }

            if (allowDrag)
            {
               Form Form = GuiUtils.FindForm(control);

               // Create a rectangle using the DragSize, with the mouse position being
               // at the center of the rectangle.
               Size dragSize = SystemInformation.DragSize;
               ((TagData)Form.Tag).DragBoxFromMouseDown = new Rectangle(new Point(evtArgs.X - (dragSize.Width / 2),
                                                                                   evtArgs.Y - (dragSize.Height / 2)),
                                                                         dragSize);
            }
         }
      }

      /// <summary>
      /// Check whether we can perform begin drag or not. It will check whether
      /// the LeftMouse button is down and dragBoxFromMouseDown is not empty.
      /// </summary>
      /// <param name="evtArgs"></param>
      /// <param name="control"></param>
      /// <returns></returns>
      internal static bool ShouldPerformBeginDrag(Control control, MouseEventArgs evtArgs)
      {
         bool beginDrag = false;
         Form Form = GuiUtils.FindForm(control);
         Rectangle dragBoxFromMouseDown = ((TagData)Form.Tag).DragBoxFromMouseDown;

         if (!dragBoxFromMouseDown.IsEmpty && evtArgs.Button == MouseButtons.Left)
            beginDrag = true;

         return beginDrag;
      }

      /// <summary>
      /// Initiate a drag operation. Put MG_ACT_BEGIN_DRAG to Runtime thread.
      /// </summary>
      /// <param name="control"></param>
      /// <param name="evtArgs"></param>
      /// <param name="mapData"></param>
      internal static void BeginDrag(Control control, MouseEventArgs evtArgs, MapData mapData)
      {
         GuiMgControl mgControl = mapData.getControl();
         Form Form = GuiUtils.FindForm(control);
         Rectangle dragBoxFromMouseDown = ((TagData)Form.Tag).DragBoxFromMouseDown;

         // If we can perform DragBegin :
         //    - We want to initiate DragBegin only once, hence set DragBoxFromMouseDown = EMPTY
         //    - Mark the text as selected & Reset the Manager.draggedData
         //    - Put MG_ACT_BEGIN_DRAG.
         if (mgControl != null && !dragBoxFromMouseDown.Contains(evtArgs.X, evtArgs.Y))
         {
            // Allow drag on Table with multimarked rows.
            if (control is TableControl && getTableManager((TableControl)control).IsInMultimark)
            {
               mapData = ((TagData)control.Tag).MapData;
               mgControl = mapData.getControl();
            }

            if (control is MgTextBox)
            {
               MgTextBox mgTxtbox = (MgTextBox)control;
               mgTxtbox.Select(mgTxtbox.SaveSelectionStart, mgTxtbox.SaveSelectionLength);
            }

            // #129244. The value of a listbox is updated when Selection Change event occurs.
            // Selection change event is not raised when dragging the item from a listbox and hence
            // in Drag Begin, the value of the listbox control will be updated so that in Drop event
            // one can use listbox variable to get the dragged data.
            if (control is MgListBox)
            {
               GuiUtils.setSuggestedValueOfChoiceControlOnTagData(control, GuiUtils.getValue(control));
               Events.OnSelection(GuiUtils.getValue(control), mgControl, mapData.getIdx(), false);
            }

            ((TagData)Form.Tag).DragBoxFromMouseDown = Rectangle.Empty;

            GuiUtils.DraggedData.Clean();
            GuiUtils.DraggedData.IsBeginDrag = true;
            Events.OnBeginDrag(mgControl, mapData.getIdx());
         }
      }

      /// <summary>
      /// Check whether drop is allowed on a control or not.
      /// </summary>
      /// <param name="control"></param>
      /// <param name="guiMgControl"></param>
      /// <param name="rowIdx">row numer in case of table control</param>
      /// <returns></returns>
      internal static bool handleDragOver(Control control, GuiMgControl guiMgControl, int rowIdx)
      {
         bool DropAllowed = false;

         // First check whether drop is allowed on form or not by checking Panel's TagData.
         // If we found a control on a form then check whether drop is allowed on control or not.
         if ((control is Panel || control is Form) && ((TagData)control.Tag).AllowDrop == true)
            DropAllowed = true;

         if (guiMgControl != null)
         {
            Object ctrl = ControlsMap.getInstance().object2Widget(guiMgControl, rowIdx);

            if (control is MgTreeView)
               DropAllowed = ((TagData)control.Tag).AllowDrop == true;
            else if (guiMgControl.IsDotNetControl() && ((Control)ctrl).Enabled)
               DropAllowed = ((Control)ctrl).AllowDrop;
            else if (ctrl is LogicalControl)
            {
               DropAllowed = false;
               if (((LogicalControl)ctrl).AllowDrop && ((LogicalControl)ctrl).Enabled)
                  DropAllowed = true;
            }
            else if (ctrl is Control && ((Control)ctrl).Enabled)
               DropAllowed = ((TagData)((Control)ctrl).Tag).AllowDrop;
         }

         return DropAllowed;
      }

      /// <summary>
      /// handles a drop event.
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="evtArgs"></param>
      /// <param name="mapData"></param>
      internal static void handleDragDrop(Control sender, EventArgs evtArgs, MapData mapData)
      {
         Control control = sender;
         GuiMgControl mgControl = mapData.getControl();

         // Save the received data into the DroppedData.
         GuiUtils.DroppedData.SetDroppedData(((DragEventArgs)evtArgs).Data);

         // Convert the coordinates from screen to form and store it.
         Point screenPt = new Point(((DragEventArgs)evtArgs).X, ((DragEventArgs)evtArgs).Y);
         Form form = GuiUtils.FindForm(control);
         Point formPt = form.PointToClient(screenPt);
         GuiUtils.DroppedData.X = formPt.X;
         GuiUtils.DroppedData.Y = formPt.Y;

         // Set the selection of a control, as we won't be able to retrieve it later.
         if (mgControl != null && mgControl.isTextControl())
         {
            // When control is TextBox : 
            //    - Replace the selected text/insert text, if we are currently parked on it.    OR
            //    - Replace the whole text (not parked still the actual control - **AllowTesting = Y)
            //
            // When control is lgText : 
            //          - Replace the whole text (not parked on control -  **AllowTesting = N)
            if (control is TextBox)
            {
               GuiUtils.DroppedData.SelectionEnd = GuiConstants.REPLACE_ALL_TEXT;
               if (control.Focused)
               {
                  GuiUtils.DroppedData.SelectionStart = ((TextBox)control).SelectionStart;
                  GuiUtils.DroppedData.SelectionEnd = GuiUtils.DroppedData.SelectionStart + ((TextBox)control).SelectionLength;
               }
            }
            else
            {
               Object obj = ControlsMap.getInstance().object2Widget(mgControl, mapData.getIdx());
               if (obj is LgText)
                  GuiUtils.DroppedData.SelectionEnd = -1;
            }
         }

         // Put MG_ACT_CTRL_HIT & MG_ACT_BEGIN_DROP
         Events.OnBeginDrop(mapData.getForm(), mgControl, mapData.getIdx());
      }

      /// <summary>
      /// Resets the properties set for Drag operation.
      /// </summary>
      /// <param name="control"></param>
      internal static void ResetDragInfo(Control control)
      {
         Form form = GuiUtils.FindForm(control);
         Rectangle dragBoxFromMouseDown = ((TagData)form.Tag).DragBoxFromMouseDown;
         if (control is TextBox && !dragBoxFromMouseDown.IsEmpty)
         {
            // Reset properties, that were set in WM_LBUTTONDOWN and MouseDown.
            ((MgTextBox)control).ResetSavedProperties();
         }
         ((TagData)form.Tag).DragBoxFromMouseDown = Rectangle.Empty;
      }

      /// <summary>
      /// Set the cursor for drag operation, if the cursor is created.
      /// </summary>
      /// <param name="evtArgs"></param>
      internal static void SetDragCursor(GiveFeedbackEventArgs evtArgs)
      {
         Cursor tmpCursor = null;

         if (evtArgs.Effect == DragDropEffects.Copy)
            tmpCursor = GuiUtils.DraggedData.CursorCopy;
         else if (evtArgs.Effect == DragDropEffects.None)
            tmpCursor = GuiUtils.DraggedData.CursorNone;

         if (tmpCursor != null)
         {
            evtArgs.UseDefaultCursors = false;
            Cursor.Current = tmpCursor;
         }
      }

#endif
      #endregion

      /// <summary>constructor for GuiUtilsBase
      /// </summary>
      static GuiUtilsBase()
      {
         _charPass = null;

#if !PocketPC
         DraggedData = new DraggedData();
         DroppedData = new DroppedData();
#endif

      }

      /// <summary>
      ///  get table's manager 
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      internal static ContainerManager getContainerManager(Control control)
      {
         Debug.Assert(IsContainerManager(control));

         TagData td = (TagData)control.Tag;
         return td.ContainerManager;
      }

      internal static bool IsContainerManager(Control control)
      {
         if (control is TabPage)
            return false;
         return control is TableControl || control is TreeView || control is Panel ||
#if !PocketPC
 control is GroupBox ||
#endif
 ControlUtils.IsMdiClient(control);
      }

      /// <summary> set the control visiblity 
      /// </summary>
      /// <param name="control"></param>
      /// <param name="visible"></param>
      internal static void setVisible(Control control, bool visible)
      {
         if (((TagData)control.Tag) != null)
            ((TagData)control.Tag).Visible = visible;

         // Fixed bug#: 932969
         // Overview: when we Scroll\PageDown\up we send SuspendPaint to the parent of the table.
         //           the parent of the table in this case is CTRL_TYPE_CONTAINER.
         //           we refresh the 'Visible' property of the CTRL_TYPE_CONTAINER
         //           => the statement "control.Visible = visible" releases the SuspendPaint of the container 
         //           => we get TableControl.paint and we see flickering.
         // Solution: don't set the visibility of a container (unless its visibility changed).
         if (!(control is MgPanel) || control.Visible != visible)
            control.Visible = visible;

         RefreshButtonImage(control);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="menuItem"></param>
      /// <param name="enabled"></param>
      internal static void setEnabled(MenuItem menuItem, bool enabled)
      {
         menuItem.Enabled = enabled;
      }

      /// <summary>
      /// set control enabled/disabled 
      /// </summary>
      /// <param name="control"></param>
      /// <param name="enable"></param>
      internal static void setEnabled(Control control, bool enabled)
      {
         ((TagData)control.Tag).Enabled = enabled;
         control.Enabled = enabled;
         RefreshButtonImage(control);
      }

      /// <summary>
      /// set the control's readonly property 
      /// </summary>
      /// <param name="text"></param>
      /// <param name="readOnly"></param>
      internal static void setReadOnly(TextBox text, bool readOnly)
      {
         text.ReadOnly = readOnly;
      }

      /// <summary>
      /// setTextLimit 
      /// </summary>
      /// <param name="text"></param>
      /// <param name="limit"></param>
      internal static void setTextLimit(TextBox textBox, int limit)
      {
         textBox.MaxLength = limit;
      }

      /// <summary>
      /// Sets AcceptsReturn property of Text box. 
      /// </summary>
      /// <param name="textBox"></param>
      /// <param name="acceptsReturn"></param>
      internal static void setMultilineAllowCR(TextBox textBox, bool acceptsReturn)
      {
         textBox.AcceptsReturn = acceptsReturn;
      }

      /// <summary> Set Multiline WordWrap Scroll of a TextBox 
      /// </summary>
      internal static void setMultilineWordWrapScroll(TextBox textBox, MultilineHorizontalScrollBar horizontalScrollBar)
      {
         switch (horizontalScrollBar)
         {
            case MultilineHorizontalScrollBar.Yes:
               textBox.ScrollBars |= ScrollBars.Horizontal;
               textBox.WordWrap = false;
               break;

            case MultilineHorizontalScrollBar.No:
               textBox.WordWrap = false;
               break;

            case MultilineHorizontalScrollBar.WordWrap:
               textBox.WordWrap = true;
               break;
         }
      }

      /// <summary> Set Multiline Vertical Scroll of a TextBox 
      /// </summary>
      internal static void setMultilineVerticalScroll(TextBox textBox, bool showVerticalScroll)
      {
         if (showVerticalScroll)
            textBox.ScrollBars |= ScrollBars.Vertical;
         else
            textBox.ScrollBars &= ~ScrollBars.Vertical;
      }

      /// <summary> controls that are displayed on table as text
      /// 
      /// </summary>
      /// <param name="ctrl"></param>
      /// <returns></returns>
      internal static bool isOwnerDrawControl(GuiMgControl ctrl)
      {
         if (ctrl.IsTableHeaderChild)
            return false;

         //to support testing tools - create actual controls and not use owner draw for all controls
         if (!AccessTest)
         {
            if (ctrl.Type == MgControlType.CTRL_TYPE_TEXT ||
                ctrl.Type == MgControlType.CTRL_TYPE_LABEL ||
                ctrl.Type == MgControlType.CTRL_TYPE_IMAGE)
               return (true);
            if (ctrl.Type == MgControlType.CTRL_TYPE_COMBO && ctrl.IsRepeatable)
               return (true);
         }
         if (ctrl.Type == MgControlType.CTRL_TYPE_LINE)
            return (true);

         return (false);
      }

      /// <summary> Set the selected item in a combobox 
      /// </summary>
      /// <param name="combo"></param>
      /// <param name="select"></param>
      internal static void setSelect(MgComboBox combo, int select)
      {
         //remove SelectedIndexChanged handler from combo before we change the value. put it back after.
         //we want the event to pop only when the user makes the change. 
         combo.SelectedIndexChanged -= ComboHandler.getInstance().SelectedIndexChangedHandler;

         if (select < 0 || select >= combo.Items.Count)
            combo.SelectedIndex = -1;
         else
            combo.SelectedIndex = select;

         setListControlOriginalValue(combo, combo.SelectedIndex.ToString());
         combo.SelectedIndexChanged += ComboHandler.getInstance().SelectedIndexChangedHandler;
      }

      /// <summary>
      /// set the ListControlOriginalValue on tag data
      /// </summary>
      /// <param name="control"></param>
      /// <param name="selectedIndex"></param>
      internal static void setListControlOriginalValue(Control control, string selectedIndex)
      {
         TagData tagData = (TagData)(control.Tag);
         tagData.ListControlOriginalValue = "" + selectedIndex;
      }

      /// <summary>
      /// Add items to control's list
      /// </summary>
      /// <param name="button"></param>
      /// <param name="itemsList"></param>
      internal static void setItemsList(Control control, String[] itemsList)
      {
         if (control is ListControl)
            ControlUtils.SetItemsList((ListControl)control, itemsList);
         else if (control is MgTabControl)
         {
            MgTabControl tab = (MgTabControl)control;
            int OrgSelectionIndex = tab.SelectedIndex;
#if !PocketPC //tmp
            Control ctrlFocus = FindForm(tab).ActiveControl; // get the control that has focus
#endif

            //QCR #779348, prevent flickering : instead of rectreating tab pages use existing ones
            //add/delete pages only when needed
            //perform selection and text width  update only when needed
            tab.SuspendLayout();
#if !PocketPC
            tab.SuspendPaint = true;
            ((MgPanel)((TagData)tab.Tag).TabControlPanel).SuspendPaint = true;
#endif

            //if the current selected TabPage is going to be removed, detach the Panel from the 
            //TabPage because framework doesn't do it.
            if (OrgSelectionIndex > itemsList.Length - 1)
               ((MgPanel)((TagData)tab.Tag).TabControlPanel).Parent = null;

            tab.SetTabPages(itemsList);

            if (tab.SelectedIndex != OrgSelectionIndex || tab.SelectedIndex < 0)
               setSelectionForTabControl(OrgSelectionIndex, tab);
#if !PocketPC
            ((MgPanel)((TagData)tab.Tag).TabControlPanel).SuspendPaint = false;
            tab.SuspendPaint = false;
            tab.Update();
#endif
            tab.ResumeLayout(false);

            // If tab in Recompute, then focus is switched to tab. The focus must be restored.
#if !PocketPC //tmp
            if (ctrlFocus != null && ctrlFocus != tab && !ctrlFocus.Focused)
               ctrlFocus.Focus();
#endif
         }
         else if (control is MgRadioPanel)
         {
            RemoveRadioButtons((MgRadioPanel)control);

            // Add new radioButtons to RadioPanel
            for (int lineInRadio = 0; lineInRadio < itemsList.Length; lineInRadio++)
            {
               Control radioButton = GuiUtils.createRadioButton((MgRadioPanel)control);
               GuiUtils.setText(radioButton, itemsList[lineInRadio]);
            }
         }
         else
            throw new ApplicationException("in GuiCommandQueue.setItemsList()");
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="CheckState"></param>
      /// <returns></returns>
      internal static CheckState checkBoxCheckStates2CheckBoxCheckState(MgCheckState CheckState)
      {
         System.Windows.Forms.CheckState RetCheckState = System.Windows.Forms.CheckState.Checked;
         switch (CheckState)
         {
            case MgCheckState.UNCHECKED:
               RetCheckState = System.Windows.Forms.CheckState.Unchecked;
               break;
            case MgCheckState.CHECKED:
               RetCheckState = System.Windows.Forms.CheckState.Checked;
               break;
            case MgCheckState.INDETERMINATE:
               RetCheckState = System.Windows.Forms.CheckState.Indeterminate;
               break;
            default:
               break;
         }

         return RetCheckState;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="checkBox"></param>
      /// <param name="CheckState"></param>
      internal static void setCheckBoxCheckState(MgCheckBox checkBox, CheckState CheckState)
      {
         if (checkBox.CheckState != CheckState)
         {
            checkBox.CheckStateChanged -= ButtonHandler.getInstance().CheckedStateChangedHandler;
            checkBox.CheckState = CheckState;
            checkBox.CheckStateChanged += ButtonHandler.getInstance().CheckedStateChangedHandler;
         }
      }

      /// <summary>
      /// set selection for tab control
      /// </summary>
      /// <param name="tabPageIndex"></param>
      /// <param name="obj"></param>
      internal static void setSelectionForTabControl(int tabPageIndex, MgTabControl tab)
      {
         if (tabPageIndex < 0 || tabPageIndex >= tab.Controls.Count)
         {
            if (tab.Controls.Count > 0)
               tabPageIndex = 0;
            else
               tabPageIndex = -1;
         }

#if !PocketPC
         tab.SuspendPaint = true;
         ((MgPanel)((TagData)tab.Tag).TabControlPanel).SuspendPaint = true;
#endif

         if (tab.SelectedIndex != tabPageIndex)
         {
#if !PocketPC
            tab.Selecting -= TabHandler.getInstance().SelectingHandler;
#endif
            tab.SelectedIndex = tabPageIndex;
#if !PocketPC
            tab.Selecting += TabHandler.getInstance().SelectingHandler;
#endif
         }

         if (tabPageIndex > -1)
         {
            TabPage selectedTabPage = tab.TabPages[tabPageIndex];
            Panel panel = ((TagData)tab.Tag).TabControlPanel;
            selectedTabPage.Controls.Add(panel);
         }

#if !PocketPC
         ((MgPanel)((TagData)tab.Tag).TabControlPanel).SuspendPaint = false;
         tab.SuspendPaint = false;
         tab.Update();
#endif

      }

      /// <summary>
      /// calculate sorts distance between line and point
      /// </summary>
      /// <param name="point"></param>
      /// <param name="lineStart"></param>
      /// <param name="lineEnd"></param>
      /// <returns></returns>
      static internal double ShortestDistance(Point point, Point lineStart, Point lineEnd)
      {
         float A = point.X - lineStart.X;
         float B = point.Y - lineStart.Y;
         float C = lineEnd.X - lineStart.X;
         float D = lineEnd.Y - lineStart.Y;

         float dot = A * C + B * D;
         float len_sq = C * C + D * D;
         float param = dot / len_sq;

         float xx, yy;

         if (param < 0)
         {
            xx = lineStart.X;
            yy = lineStart.Y;
         }
         else if (param > 1)
         {
            xx = lineEnd.X;
            yy = lineEnd.Y;
         }
         else
         {
            xx = lineStart.X + param * C;
            yy = lineStart.Y + param * D;
         }

         return distance(point.X, point.Y, xx, yy);//your distance function
      }

      /// <summary> cald distance between 2 points
      /// </summary>
      /// <param name="x1"></param>
      /// <param name="y1"></param>
      /// <param name="x2"></param>
      /// <param name="y2"></param>
      /// <returns></returns>
      static internal double distance(float x1, float y1, float x2, float y2)
      {
         return Math.Sqrt(((x1 - x2) * (x1 - x2)) + ((y1 - y2) * (y1 - y2)));
      }

      /// <summary>
      /// reverse horizontal alignment
      /// </summary>
      /// <param name="alignment"></param>
      /// <returns></returns>
      internal static AlignmentTypeHori reverseHorizontalAlignment(AlignmentTypeHori alignment)
      {
         if (alignment == AlignmentTypeHori.Left)
            alignment = AlignmentTypeHori.Right;
         else if (alignment == AlignmentTypeHori.Right)
            alignment = AlignmentTypeHori.Left;
         return alignment;
      }

      /// <summary>
      /// create a dummy form.
      /// </summary>
      /// <returns></returns>
      internal static Form createDummyForm()
      {
         Form form = new Form();
         form.Location = new Point(-50000, -50000);
         form.Size = new Size(0, 0);
#if !PocketPC //tmp
         form.StartPosition = FormStartPosition.Manual;
#endif
         form.Visible = true;
         form.Activate();

         return form;
      }

      /// <summary>
      /// returns distance between point and rectangle
      /// </summary>
      /// <param name="rect"></param>
      /// <param name="pt"></param>
      /// <returns></returns>
      internal static int getDistance(Rectangle rect, Point pt)
      {
         int dx, dy;
         int right = rect.X + rect.Width;
         int bottom = rect.Y + rect.Height;

         if (pt.X < rect.X)
            dx = rect.X - pt.X;
         else if (pt.X > right)
            dx = pt.X - right;
         else
            dx = 0;

         if (pt.Y < rect.Y)
            dy = rect.Y - pt.Y;
         else if (pt.Y > bottom)
            dy = pt.Y - bottom;
         else
            dy = 0;
         return (dx * dx + dy * dy);
      }

      /// <summary>
      /// return client panel of form to which the control belongs
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      internal static Panel getParentPanel(Control control)
      {
         while (control.Parent != null)
         {
            control = control.Parent;
            if (control is Panel && control.Tag is TagData && ((TagData)(control.Tag)).IsInnerPanel)
               return (Panel)control;
         }
         return null;
      }

      /// <summary>
      /// retuns container panel of control
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      internal static Panel getContainerPanel(Control control)
      {
         while (control.Parent != null)
         {
            control = control.Parent;
            if (control is Panel && control.Tag is TagData && ((TagData)(control.Tag)).ContainerManager != null)
               return (Panel)control;
         }
         return null;
      }

      /// <summary>
      /// sets the System.Windows.Forms.CheckBox.ThreeStates 
      /// </summary>
      /// <param name="alignmentTypeHori"></param>
      /// <returns></returns>
      internal static void SetThreeStates(MgCheckBox checkBox, Boolean IsThreeStates)
      {
         checkBox.ThreeState = IsThreeStates;
      }

      /// <summary>
      /// get table's manager
      /// </summary>
      /// <param name="table"></param>
      /// <returns></returns>
      internal static TableManager getTableManager(TableControl table)
      {
         TagData td = (TagData)table.Tag;
         return (TableManager)td.ContainerManager;
      }

      /// <summary>
      /// get table's manager
      /// </summary>
      /// <param name="table"></param>
      /// <returns></returns>
      internal static ItemsManager getItemsManager(Control control)
      {
         Debug.Assert(control is TableControl || control is TreeView);
         TagData td = (TagData)control.Tag;
         return (ItemsManager)td.ContainerManager;
      }

      /// <summary> perform necessary scrolls to show control
      /// Move the corner point of a window so the rectangle 'bounds' is visible (specified in pixels)
      /// see gui.cpp method :_gui_rect_visible()
      /// </summary>
      /// <param name="control">control to show</param>
      /// <param name="child">tableChild of control if it is a table's son</param>
      internal static void scrollToControl(Control control, LogicalControl lg)
      {
         // if control is table son, first try to scroll the table
         if (lg != null && lg.Coordinator is TableCoordinator)
            ((TableCoordinator)lg.Coordinator).scrollToColumn();
#if !PocketPC //tmp
         else if (control is TreeView)
         {
            TreeManager treeManager = getTreeManager((TreeView)control);
            if (treeManager != null) //can be null for .NET control
               treeManager.showSelection();
         }
#endif

         //Form form = control.FindForm();
         //form.ScrollControlIntoView(control);

         // now scroll control's form
         Panel parentPanel = getParentPanel(control);
         // The set focus command was send to the queue and the composite was replace to mgSplitContainer
         if (parentPanel == null)
            return;

         Point pixCorner = parentPanel.AutoScrollPosition; // this is "corner"
         pixCorner.X *= -1;
         pixCorner.Y *= -1;

         Rectangle vsbl = getBounds(control);

         if (control.Parent != parentPanel)
         {
            /* Control.RectangleToScreen() gives the location based on the display rectangle */
            /* and not the actual rectangle (actual rectangle can be different in case the   */
            /* scrollbars are present). So, adjust the location based on the current scroll  */
            /* position. Further, we need to get the relative rectangle only if the current  */
            /* control is contained in a control other than the parentPanel.                 */

            // translate coordinates to forms coordinates
            vsbl = control.Parent.RectangleToScreen(vsbl);
            vsbl = parentPanel.RectangleToClient(vsbl);

            vsbl.Offset(pixCorner.X, pixCorner.Y);
         }

         // compute new "corner"
         Rectangle area = parentPanel.ClientRectangle;

         int xCrnr = pixCorner.X;
         int yCrnr = pixCorner.Y;
         int dxRect = area.Width;
         int dyRect = area.Height;
         int dxVsbl = vsbl.Width;
         int dyVsbl = vsbl.Height;

         int Vsblbottom = vsbl.Y + vsbl.Height;
         int VsblRight = vsbl.X + vsbl.Width;

         if (vsbl.Y < yCrnr || (Vsblbottom > yCrnr + dyRect && dyVsbl <= dyRect))
            yCrnr = Math.Max(0, vsbl.Y - Math.Max(5, ((dyRect - dyVsbl) / 2)));

         if (vsbl.X < xCrnr || VsblRight > xCrnr + dxRect)
            xCrnr = Math.Max(0, vsbl.X - Math.Max(5, ((dxRect - dxVsbl) / 2)));

         if (xCrnr != pixCorner.X || yCrnr != pixCorner.Y)
            cornerMove(parentPanel, pixCorner, xCrnr - pixCorner.X, yCrnr - pixCorner.Y);
      }

      /// <summary> Move the corner point of a window                                            
      /// see gui.cpp method: GUI_CTX::CornerMove ()
      /// </summary>
      /// <param name="sc"></param>
      /// <param name="pixCorner"></param>
      /// <param name="pixDx"></param>
      /// <param name="pixDy"></param>
      /// <returns> TRUE if corner really changed. This function accept movement in pixels</returns>
      internal static bool cornerMove(Panel clientPanel, Point pixCorner, int pixDx, int pixDy)
      {
         Rectangle Rect = clientPanel.ClientRectangle;
         int xCrnr = 0;
         int yCrnr = 0;
         int pixVis = 0;

         Point logSize = new Point(clientPanel.DisplayRectangle.Size.Width, clientPanel.DisplayRectangle.Size.Height);
         if (pixDx != 0)
         {
            xCrnr = pixCorner.X + pixDx;
            if (xCrnr <= 0)
               xCrnr = 0;
            else
            {
               pixVis = Rect.Width;
               if (xCrnr > logSize.X - pixVis)
                  xCrnr = logSize.X - pixVis;
            }
         }
         else
            xCrnr = pixCorner.X;

         /* ---------------------------------------------------------------- */
         /* Move Y corner and verify it does not over/under flows the window */
         /* ---------------------------------------------------------------- */
         if (pixDy != 0)
         {
            yCrnr = pixCorner.Y + pixDy;
            if (yCrnr <= 0)
               yCrnr = 0;
            else
            {
               pixVis = Rect.Height;
               if (yCrnr > logSize.Y - pixVis)
                  yCrnr = logSize.Y - pixVis;
            }
         }
         else
            yCrnr = pixCorner.Y;

         /* Compute difference from current corner. If none, bail out */
         /* --------------------------------------------------------- */
         pixDx = xCrnr - pixCorner.X;
         pixDy = yCrnr - pixCorner.Y;
         if (pixDx == 0 && pixDy == 0)
            return (false);

         pixCorner.X += pixDx;
         pixCorner.Y += pixDy;

         clientPanel.AutoScrollPosition = pixCorner;
         return true;
      }

#if !PocketPC //tmp
      /// <summary>
      /// get the tree manager
      /// </summary>
      /// <param name="tree"></param>
      /// <returns></returns>
      internal static TreeManager getTreeManager(TreeView tree)
      {
         TagData td = (TagData)tree.Tag;
         return (TreeManager)td.ContainerManager;
      }

#endif

      /// <summary>
      /// Sometimes, mouse button down should not produce click. 
      /// return true when the mouse down can produce click, or false if the click needs to be skipped at that point.
      /// </summary>
      /// <param name="control"></param>
      /// <param name="eventArgs"></param>
      /// <returns></returns>
      internal static bool canMouseDownProduceClick(Control control, MouseEventArgs eventArgs)
      {
         bool canProduceClick = true;
#if !PocketPC
         // RIA and Online behave differently here.
         // if click can be skipped, check if it should :
         // for tree, if a node was clicked and the node is different than the current selected one
         // then we know the next event of the tree will be either node change or expand/collapse.
         // 
         if (control is TreeView && Events.CanTreeClickBeSkippedOnMouseDown())
         {
            TreeView tree = (TreeView)control;
            TreeViewHitTestInfo hitTestInfo = tree.HitTest(eventArgs.X, eventArgs.Y);
            // we try to asses if this mouse down will be followed by another action : before select\expand\collapse
            // if so, we want that action to add click event instead of now.
            if ((hitTestInfo.Node != null && hitTestInfo.Node != tree.SelectedNode) &&
                (hitTestInfo.Location == TreeViewHitTestLocations.Label ||
                 hitTestInfo.Location == TreeViewHitTestLocations.PlusMinus ||
                 hitTestInfo.Location == TreeViewHitTestLocations.Image))
               canProduceClick = false;
         }
#endif
         return canProduceClick;
      }
      /// <summary>
      /// check if dot net control can be focused
      /// </summary>
      /// <param name="mgcontrol"></param>
      /// <returns></returns>
      internal static bool canFocus(GuiMgControl guiMgControl)
      {
         bool parkable = true;
         Debug.Assert(guiMgControl.IsDotNetControl());

#if !PocketPC
         Control c = ControlsMap.getInstance().object2Widget(guiMgControl) as Control;
         if (c != null)
            parkable = CanFocus(c);
#endif

         return parkable;
      }

#if !PocketPC
      /// <summary> returns if the control or one of its children can be focused. </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      private static bool CanFocus(Control control)
      {
         bool canFocus = false;

         //If the control itself is either hidden or disabled, we cannot park on it or its child.
         if (control.Visible && control.Enabled)
         {
            canFocus = control.CanSelect;

            //If the control cannot be focused, check if any of its children can be focused.
            if (!canFocus)
            {
               foreach (Control child in control.Controls)
               {
                  canFocus = CanFocus(child);

                  if (canFocus)
                     break;
               }
            }
         }

         return canFocus;
      }
#endif

      /// <summary>
      /// will return the composite of the control if it is radio button
      /// </summary>
      /// <param name="object"></param>
      /// <returns></returns>
      internal static MgRadioPanel getRadiobuttonPanel(object ctrlObj)
      {
         MgRadioPanel RadioButtonPanel = null;

         object obj = ControlsMap.getInstance().object2Widget(ctrlObj, 0);
         if (obj is MgRadioPanel)
            RadioButtonPanel = (MgRadioPanel)obj;

         return RadioButtonPanel;
      }

      /// <summary> </summary>
      /// <param name="object"></param>
      /// <param name="checked"></param>
      internal static void setChecked(Object obj, bool value)
      {
         if (obj is CheckBox)
            Debug.Assert(false);//use GuiUtils.setCheckBoxCheckState()
         else if (obj is RadioButton)
            Debug.Assert(false); // use GuiUtils.setChecked(MgRadioPanel, int)
#if !PocketPC
         else if (obj is ToolStripMenuItem)
            ((ToolStripMenuItem)obj).Checked = value;
         else if (obj is ToolStripButton)
            ((ToolStripButton)obj).Checked = value;
#endif
         else
            Debug.Assert(false);
      }

      /// <summary> create subform control, can be scroll composite or Composite(for frame set)</summary>
      /// <param name="parent"></param>
      /// <param name="isFrameSet"></param>
      /// <returns></returns>
      internal static Panel createSubFormControl(Control parent, bool isFrameSet)
      {
         Panel retPanel = null;

         if (isFrameSet)
         {
            retPanel = new MgPanel();
            new BasicControlsManager(retPanel);
            //    create the default layout data for the composite
            setMgSplitContainerData(retPanel);
            SubformPanelHandler.getInstance().addHandler(retPanel);
         }
         //if it is sub form need to create scroll composite and inner composite
         else
         {
            retPanel = createSubformPanel(parent);
         }

         ((TagData)retPanel.Tag).ContainsSubFormOrFrame = true;

         //add the control to the array control for splitter container or add it to the parent 
         if (parent is MgSplitContainer)
         {
            ((MgSplitContainer)parent).addChild(retPanel);
         }

         return retPanel;
      }

      /// <summary> create MgSplitContainer data for control</summary>
      /// <param name="conrtol"></param>
      /// <returns></returns>
      internal static MgSplitContainerData setMgSplitContainerData(Control conrtol)
      {
         TagData tagData = ((TagData)conrtol.Tag);

         if (tagData.MgSplitContainerData == null)
            tagData.MgSplitContainerData = new MgSplitContainerData();
         else
            Debug.Assert(false);

         return tagData.MgSplitContainerData;
      }

      /// <summary> Creates inner composite under the scrolled composite
      /// was :createInnerComposite
      /// </summary>
      /// <param name="parent"></param>
      /// <returns></returns>
      internal static Panel createSubformPanel(Control parent)
      {
         Panel retPanel = createInnerPanel(parent, false);
         retPanel.SuspendLayout();
         SubformPanelHandler.getInstance().addHandler(retPanel);

         return retPanel;
      }

      /// <summary></summary>
      /// <param name="parent"></param>
      /// <param name="swtStyle"></param>
      /// <param name="isHelpWindow"></param>
      /// <returns></returns>
      internal static Panel createInnerPanel(Control parent, bool isHelpWindow)
      {
         Panel retPanel = new MgPanel();
         new BasicControlsManager(retPanel);
         ((TagData)retPanel.Tag).IsInnerPanel = true;

         retPanel.AutoScroll = true;
         retPanel.BackgroundImageLayout = ImageLayout.None;

#if !PocketPC
         retPanel.BackgroundImageLayout = ImageLayout.None;

         // Always set it true, because for Drag & Drop, we need a DragOver Event to
         // retrieve the MapData and control. We will use control.TagData's / LogicalControl's
         // AllowDrop to check whether the drop is allowed on control / LogicalControl.
         retPanel.AllowDrop = true;
#endif
         if (parent is MgSplitContainer)
            setMgSplitContainerData((Control)retPanel);

         //Do not set context menu for help window.
         if (!isHelpWindow)
            setContextMenu(retPanel, null);

         return retPanel;
      }

#if !PocketPC
      /// <summary>
      /// 
      /// </summary>
      /// <param name="control"></param>
      /// <param name="menu"></param>
      internal static void setContextMenu(Control control, ToolStrip menu)
      {
         if (menu != null)
         {
            //Fixed bug#:259246 Context menu:RighToLeft property will be set according to form.RighToLeft property(also for subform) 
            ContextMenuStrip contextMenu = (ContextMenuStrip)menu;
            contextMenu.RightToLeft = GuiUtils.FindForm(control).RightToLeft;
            control.ContextMenuStrip = (ContextMenuStrip)menu;
         }
         else
         {
            ContextMenuStrip dummyMenu = new ContextMenuStrip();
            dummyMenu.Name = "Dummy";
            // we must listen to dummy menu, in order to get to the OPENING event.
            MgMenuHandler.getInstance().addHandler(dummyMenu);
            control.ContextMenuStrip = dummyMenu;
            CreateTagData(dummyMenu);
            ((TagData)dummyMenu.Tag).ContextCanOpen = false;
         }
      }

#else

      /// <summary>Set Context menu for form or control</summary>
      /// <param name="control"></param>
      /// <param name="menu"></param>
      internal static void setContextMenu(Control control, Menu menu)
      {
         try
         {
            if (menu != null)
               control.ContextMenu = (ContextMenu)menu;
            else
            {
               ContextMenu dummyMenu = new ContextMenu();
               dummyMenu.Name = "Dummy";
               control.ContextMenu = dummyMenu;
               // we must listen to dummy menu, in order to get to the OPENING event.
               MgMenuHandler.getInstance().addHandler(dummyMenu);
               CreateTagData(dummyMenu);
               ((TagData)dummyMenu.Tag).ContextCanOpen = false;
            }
         }
         catch (Exception e)
         {
            Events.WriteErrorToLog("setContextMenu - " + e.Message);
         }
      }

#endif

      /// <summary>
      /// 
      /// </summary>
      /// <param name="control"></param>
      /// <param name="x"></param>
      /// <param name="y"></param>
      /// <param name="width"></param>
      /// <param name="height"></param>
      internal static void controlSetBounds(Control control, int x, int y, int width, int height)
      {
#if !PocketPC
         control.SetBounds(x, y, width, height);
#else
         control.Bounds = new Rectangle(x, y, width, height);
#endif
      }

      /// <summary> get the MgSplitContainerData for the control
      /// 
      /// </summary>
      /// <param name="conrtol"></param>
      /// <returns></returns>
      internal static MgSplitContainerData getMgSplitContainerData(Control conrtol)
      {
         MgSplitContainerData reMgSplitContainerData = null;
         TagData tagData = ((TagData)conrtol.Tag);

         if ((tagData.MgSplitContainerData is MgSplitContainerData))
            reMgSplitContainerData = tagData.MgSplitContainerData;
         else
            Debug.Assert(false);

         return reMgSplitContainerData;
      }

      /// <summary> Compute client size of a Form</summary>
      /// <param name="form"></param>
      /// <param name="size"></param>
      /// <returns></returns>
      internal static Size computeClientSize(Form form, Size size)
      {
         Size clientSize = new Size(size.Width, size.Height);

#if !PocketPC //temp
         TagData td = (TagData)form.Tag;
         Control statusBar = td.StatusBarControl;

         if (td.toolBar != null && td.toolBar.Items.Count > 0)
            clientSize.Height += td.toolBar.Height;
         if (form.MainMenuStrip != null && (TagData)form.MainMenuStrip.Tag != null && ((TagData)form.MainMenuStrip.Tag).Visible)
            clientSize.Height += form.MainMenuStrip.Height;
         if (statusBar != null)
            clientSize.Height += statusBar.Height;
#endif

         return clientSize;
      }

      /// <summary> set widths to columns</summary>
      /// <param name="tableManager"></param>
      /// <param name="columnsData"></param>
      internal static void setWidthColumns(TableManager tableManager, int[] widthArray, int[] widthForFillTablePlacementArray)
      {
         LgColumn lgColumn = null;
         int width = 0;

         for (int idx = 0; idx < widthArray.Length; idx++)
         {
            width = widthArray[idx];

            lgColumn = tableManager.getColumn(idx);

            // save the width on columnManager even if this column is not visible.
            lgColumn.setWidth(width, false, false);
            lgColumn.WidthForFillTablePlacement = widthForFillTablePlacementArray[idx];

            // set width on table column
            if (lgColumn.Visible)
               lgColumn.TableColumn.Width = width;
         }
      }

      /// <summary> reoder column manager</summary>
      /// <param name="tableManager"></param>
      /// <param name="columnsData"></param>
      internal static void reorderColumnManager(TableManager tableManager, int[] orderArray)
      {
         LgColumn lgColumn = null, newLgColumn = null;
         int layer = 0;

         // 1. set layer and reorder tablemanager
         for (int idx = 0; idx < orderArray.Length; idx++)
         {
            // set the layer
            layer = orderArray[idx];

            lgColumn = tableManager.getColumn(idx);
            newLgColumn = (LgColumn)tableManager.ColumnsManager.getLgColumnByMagicIdx(layer);

            // If already not at the correct place, move the newLgColumn to position 'idx'
            if (lgColumn.MgColumnIdx != layer)
               tableManager.reorder(newLgColumn, lgColumn);
         }
      }

      /// <summary>
      /// get order to sort table control, based on tableManager
      /// </summary>
      /// <param name="tableManager"></param>
      /// <returns></returns>
      internal static int[] getSortOrder(TableManager tableManager)
      {
         List<int> orderlist = new List<int>();
         foreach (LgColumn columnMgr in tableManager.getColumns())
         {
            if (columnMgr.Visible)
               orderlist.Add(columnMgr.GuiColumnIdx);
         }

         // convert orderlist to int[] array
         int[] orderArray = orderlist.ToArray();

         return orderArray;
      }

      /// <summary>
      /// get order to restore native header of table control
      /// </summary>
      /// <param name="columnCount"></param>
      /// <returns></returns>
      internal static int[] getRestoreNativeSortOrder(int columnCount)
      {
         int[] orderArray = new int[columnCount];

         // set order to 0,1,2
         for (int index = 0; index < columnCount; index++)
            orderArray[index] = index;

         return orderArray;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="form"></param>
      internal static void saveDesktopBounds(Form form)
      {
         Point offset = new Point();
         TagData td = (TagData)form.Tag;

#if !PocketPC
         // We maintained two bounds in TagData : 
         //    1) Bounds         :-  Form's position on the screen
         //    2) DeskTopBounds  :-  Form's position on the screen with scroll offsets. 
         // We save DeskTopBounds in formstate, so when we apply the formstate, it also restore the scroll 
         // positions (show scroll bar with appropriate scrolling).
         if (form.IsMdiChild)
         {
            // get scrollbar position for mdi frame
            MdiClient mdiClient = (MdiClient)form.Parent;
            offset.X = Utils.ScrollInfo(mdiClient, NativeScroll.SB_HORZ).nPos;
            offset.Y = Utils.ScrollInfo(mdiClient, NativeScroll.SB_VERT).nPos;
         }
#endif
         td.DeskTopBounds = new Rectangle(form.Left + offset.X, form.Top + offset.Y, form.ClientSize.Width, form.ClientSize.Height);
      }

      /// <summary>
      /// get the panel on the subform
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      internal static Panel getSubformPanel(Control control)
      {
         Panel panel = null;
         while (control != null && !(control is Form))
         {
            if (control is Panel)
            {
               TagData td = ((Panel)control).Tag as TagData;
               if (td != null && td.ContainsSubFormOrFrame)
               {
                  panel = (Panel)control;
                  break;
               }
            }
            control = control.Parent;
         }

         return panel;
      }

      /// <summary>
      /// Common functionality : Moved from GuiCommandsQueue.
      /// </summary>
      /// <param name = "image"></param>
      /// <param name = "control"></param>
      /// <param name = "imageStyle"></param>
      internal static void SetImageToControl(Image image, Control control, CtrlImageStyle imageStyle)
      {
         GuiUtils.setImageInfoOnTagData(control, image, imageStyle);

         GuiUtils.setBackgroundImage(control);
      }

      /// <summary>
      /// Set image for each radio button in the radioPanel.
      /// </summary>
      /// <param name="image">image</param>
      /// <param name="imageFileName">Image filename</param>
      /// <param name="obj">object to which we should set image</param>
      /// <param name="imageStyle">Image Style</param>
      internal static void SetImageToRadio(Image image, String imageFileName, Object obj, CtrlImageStyle imageStyle)
      {
         bool isAnimatedImage = false;

#if !PocketPC
         isAnimatedImage = ImageAnimator.CanAnimate(image);
#endif

         foreach (RadioButton radioButton in ((MgRadioPanel)obj).Controls)
         {
            //#998825. If an image is loaded and attached to multiple controls, 
            //CLR throws an "Unhandled exception in GDI+" when trying to set the ActiveFrame 
            //from UpdtaeFrame(). Prior to the fix of #779789, the image was not animated 
            //and so, there was no crash.
            //Solution: if an animated image is attached to a radio control, load the image 
            //for each radio option.
            if (isAnimatedImage)
               image = GuiUtils.getImageFromFile(imageFileName);
            GuiUtils.SetImageToControl(image, radioButton, imageStyle);
         }
      }

      /// <summary>
      /// Set Appearance for Radio control.
      /// </summary>
      /// <param name="obj">object</param>
      /// <param name="appearance">appearance for the radio : Button/Normal</param>
      internal static void SetRadioAppearance(Object obj, Appearance appearance)
      {
         if (obj is MgRadioPanel)
            ((MgRadioPanel)obj).Appearance = appearance;
         else if (obj is LgRadioContainer)
            ((LgRadioContainer)obj).Appearance = appearance;
         else
            Debug.Assert(false);
      }

      /// <summary>
      /// remove all controls that belong to the RadioPanel.
      /// </summary>
      /// <param name="radioPanel">parent control of radio buttons</param>
      internal static void RemoveRadioButtons(MgRadioPanel radioPanel)
      {
         //"foreach loop" can not be used to add or remove items from the source collection to avoid unpredictable side effects.
         //Please refer: http://msdn.microsoft.com/en-us/library/ttw7t8t6.aspx
         //Previously, foreach loop was not disposing all the radio buttons.
         //e.g. When 2 radio buttons are present in radio panel, it comes to dispose the first radio button,
         //for second radio button it doesn't come in loop & doesn't dispose the second radio button.
         //So replacing "foreach loop" with "while loop" will ensure all the controls are disposed from existing controls list of Panel.
         while (radioPanel.Controls.Count > 0)
         {
            Control child = radioPanel.Controls[0];
#if PocketPC
            // On Mobile, if we get to the dispose handler and try to access the object, we get an 
            // exception, so we call the dispose cleanup handler before the actual dispose of the object.
            EventArgs e = new EventArgs();
            RadioButtonHandler.getInstance().handleEvent(HandlerBase.EventType.DISPOSED, child, e);
            child.Disposed -= RadioButtonHandler.getInstance().DisposedHandler;
#endif
            //Calling the Clear method does not remove control handles from memory. You must explicitly call the Dispose method to avoid memory leaks.
            //Please refer: http://msdn.microsoft.com/en-us/library/system.windows.forms.control.controlcollection.clear(v=vs.110).aspx
            child.Dispose();
         }

         radioPanel.Controls.Clear();
      }

#if !PocketPC
      /// <summary>
      /// Mark the radio button to checked whose index is passed and set 
      /// </summary>
      /// <param name="radioPanel"></param>
      /// <param name="lineInRadio">index of the radio button inside the radio panel</param>
      internal static void SetChecked(MgRadioPanel radioPanel, int lineInRadio)
      {
         radioPanel.SelectedIndex = lineInRadio;
      }
#else
      /// <summary>
      /// Mark the radio button to checked whose index is passed and set 
      /// </summary>
      /// <param name="radioPanel"></param>
      /// <param name="lineInRadio">index of the radio button inside the radio panel</param>
      internal static void SetChecked(MgRadioPanel radioPanel, int lineInRadio)
      {
         for (int i = 0; i < radioPanel.Controls.Count; i++)
         {
            MgRadioButton radioButton = (MgRadioButton)radioPanel.Controls[i];
            radioButton.Checked = (lineInRadio == i) ? true : false;
         }
      }
#endif
      /// <summary>
      /// Set nos of columns for a radio control
      /// </summary>
      /// <param name="obj">object : MgRadioPanel/LgRadioContainer</param>
      /// <param name="numCol">nos of columns on the panel</param>
      internal static void SetLayoutNumColumns(Object obj, int numCol)
      {
         if (obj is MgRadioPanel)
         {
            MgRadioPanel panel = ((MgRadioPanel)obj);
            panel.NumColumns = numCol;
         }
         else if (obj is LgRadioContainer)
            obj = ((LgRadioContainer)obj).ColumnsInRadio = numCol;
         else
            Debug.Assert(false);
      }

      /// <summary> Returns whether the specified MgPanel is a dubform or not. </summary>
      /// <param name="mgPanel"></param>
      /// <returns></returns>
      internal static bool IsSubformPanel(MgPanel mgPanel)
      {
         TagData tagData = (TagData)mgPanel.Tag;
         return (tagData.IsInnerPanel && !tagData.IsClientPanel);
      }

      /// <summary> Sets the control's name. </summary>
      /// <param name="control"></param>
      /// <param name="name"></param>
      internal static void SetControlName(Control control, String name)
      {
         if (control.Name != name)
            control.Name = name;
      }

      /// <summary>
      /// Get the index of radio button in the Radio panel.
      /// </summary>
      /// <param name="radioButton">radio button whose index we want</param>
      /// <returns>index of a radio button inside the radio panel</returns>
      internal static String GetRadioButtonIndex(RadioButton radioButton)
      {
         MgRadioPanel radioPanel = (MgRadioPanel)radioButton.Parent;
         return Convert.ToString(radioPanel.Controls.GetChildIndex(radioButton));
      }

      /// <summary>
      /// Gets the comma separated string for selected indice of list box.
      /// </summary>
      /// <param name="objListBox"></param>
      /// <returns>comma separated string for selected indice else -1</returns>
      internal static string GetListBoxSelectedIndice(ListBox listBox)
      {
         string selectedIndice = string.Empty;

#if !PocketPC
         if (listBox.SelectedIndices.Count > 0)
         {
            int[] indice = GetSelectedIndices(listBox);

            // Although we got the indices from listBox, but listbox.SeletedItems may not be 
            // updated properly. Hence explicitly select the selected entires of a listBox.
            listBox.SelectedIndexChanged -= ListHandler.getInstance().SelectedIndexChangedHandler;
            for (int i = 0; i < selectedIndice.Length; i++)
               listBox.SetSelected(i, true);
            listBox.SelectedIndexChanged += ListHandler.getInstance().SelectedIndexChangedHandler;

            selectedIndice = Misc.GetCommaSeperatedString(indice);
         }
         else
            selectedIndice = "-1";
#else
         selectedIndice = listBox.SelectedIndex.ToString();
#endif

         return selectedIndice;
      }

#if !PocketPC
      /// <summary>
      ///  Get the selected indices from the listbox.
      /// </summary>
      /// <param name="listBox"></param>
      /// <returns>array of selected indices</returns>
      internal static int[] GetSelectedIndices(ListBox listBox)
      {
         int[] indice = new int[listBox.SelectedIndices.Count];

         // We should use listBox.GetSelected(<index>) to get the list of selected indices.
         // This is because there is a problem/feature in a framework with multiselect listbox where
         // listBox.SelectedIndices.Count may indicate more items then it has in its vector/array. 
         // Hence when you try to copy all indices from listBox.SelectedIndices, it will throw an 
         // ArrayIndexOutofBounds exception. (This was producible with the D&D + multiselection listbox)

         // When we select a multiple items in the listbox without releasing a mouse button then we see
         // multiple entries as marked but it is not selected internally (i.e. listBox.SelectedItems is not
         // getting updated. It is updated only when we release mouse button and we get SelectionIndexChange event)

         if (listBox.SelectedIndices.Count > 0)
         {
            for (int i = 0, j = 0; i < listBox.Items.Count; i++)
               if (listBox.GetSelected(i) == true)
                  indice[j++] = i;
         }

         return indice;
      }

      /// <summary>
      /// Delete WindowMenu Item.
      /// </summary>
      /// <param name="menuReference"></param>
      internal static void DeleteMenuItem(MenuReference menuReference)
      {
         Object obj = ControlsMap.getInstance().object2Widget(menuReference);
         if (obj is ToolStripItem)
         {
            ToolStripItem menuItem = (ToolStripItem)obj;
            menuItem.Dispose();
         }
      }
#endif

      /// <summary>
      /// 
      /// </summary>
      /// <param name="obj"></param>
      /// <param name="x"></param>
      /// <param name="y"></param>
      /// <param name="width"></param>
      /// <param name="height"></param>
      /// <param name="isIgnorePlacement"></param>
      internal static void setBounds(Object obj, int x, int y, int width, int height, bool isIgnorePlacement)
      {
         if (obj is LgColumn)
         {
            if (width != GuiConstants.DEFAULT_VALUE_INT)
               ((LgColumn)obj).setWidth(width, true, true);
         }
#if !PocketPC
         //temp
         else if (obj is ToolStripItem)
         {
            // ToolStripItem is not a Control. So instead of handling it here, its Width is separately handled.
            return;
         }
#endif
         else if (obj is Line)
         {
            Line line = ((Line)obj);
            if (height != GuiConstants.DEFAULT_VALUE_INT)
               line.Y2 = transformTabYCoord(height, line.GetContainerControl());
            else if (width != GuiConstants.DEFAULT_VALUE_INT)
               line.X2 = transformTabXCoord(width, line.GetContainerControl());
            if (x != GuiConstants.DEFAULT_VALUE_INT)
               line.X1 = transformTabXCoord(x, line.GetContainerControl());
            else if (y != GuiConstants.DEFAULT_VALUE_INT)
               line.Y1 = transformTabYCoord(y, line.GetContainerControl());
         }
         else if (obj is LogicalControl)
         {
            LogicalControl logicalControl = ((LogicalControl)obj);
            if (height != GuiConstants.DEFAULT_VALUE_INT)
               logicalControl.Height = height;
            else if (width != GuiConstants.DEFAULT_VALUE_INT)
               logicalControl.Width = width;
            if (x != GuiConstants.DEFAULT_VALUE_INT)
               logicalControl.X = transformTabXCoord(x, logicalControl.GetContainerControl());
            else if (y != GuiConstants.DEFAULT_VALUE_INT)
               logicalControl.Y = transformTabYCoord(y, logicalControl.GetContainerControl());
         }
         else
         {
            Control control = (Control)obj;

            Form form = getForm(control);
            if (form != null)
               control = form;

            Rectangle? saveRect = getSavedBounds(control);
            if (saveRect == null)
               saveRect = getBounds(control);

            y = transformTabYCoord(y, control.Parent);
            x = transformTabXCoord(x, control.Parent);

            Rectangle rect = (Rectangle)saveRect;
            rect.X = (x == GuiConstants.DEFAULT_VALUE_INT
                         ? rect.X
                         : x);
            rect.Y = (y == GuiConstants.DEFAULT_VALUE_INT
                         ? rect.Y
                         : y);
            rect.Width = (width == GuiConstants.DEFAULT_VALUE_INT
                             ? rect.Width
                             : width);
            rect.Height = (height == GuiConstants.DEFAULT_VALUE_INT
                              ? rect.Height
                              : height);

            // for Shells ensure that the client area fits the required width and height,
            // i.e., add the size of the non-client area to the width and height of the Shell.
            if (control is Form)
            {
               if (width != GuiConstants.DEFAULT_VALUE_INT ||
                   height != GuiConstants.DEFAULT_VALUE_INT)
               {
                  Size clientSize = computeClientSize((Form)control, rect.Size);

                  if (width != GuiConstants.DEFAULT_VALUE_INT)
                     rect.Width = clientSize.Width;
                  else if (height != GuiConstants.DEFAULT_VALUE_INT)
                     rect.Height = clientSize.Height;
               }
            }
            else
            {
               resizeFrame(x, y, control, rect);

               // control is regular control on the form
               rect = applyPreviousPlacement(control, rect,
                                             new Rectangle(x, y, width, height), isIgnorePlacement);
            }

            setBounds(control, rect);
            if (control is Form)
               saveDesktopBounds((Form)control);
         }
      }

      /// <summary>
      ///   transform coordinates of tab children relatively to panel of the tab control and not to tab itself
      /// </summary>
      /// <param name = "y"></param>
      /// <param name = "control"></param>
      /// <returns></returns>
      private static int transformTabYCoord(int y, Control parentControl)
      {
         if (y != GuiConstants.DEFAULT_VALUE_INT)
         {
            if (parentControl is Panel)
            {
               Panel panel = (Panel)(parentControl);
               if (panel.Parent is TabPage)
               {
                  TabPage page = (TabPage)panel.Parent;
                  MgTabControl tab = (MgTabControl)page.Parent;

#if !PocketPC
                  //temp
                  // At design time, the Y was set according the TabControl.
                  // So, while placing it on a TabPage, adjust the TabControl Header height.
                  y -= tab.DisplayRectangle.Y;
#else
                  // On Mobile, use calculated tab offset, which includes the adjustment to bottom alignment
                  y -= tab.Offset;
#endif
               }
            }
         }

         return y;
      }

      /// <summary>
      ///   transform tab X coordinate
      /// </summary>
      /// <param name = "x"></param>
      /// <param name = "control"></param>
      /// <returns></returns>
      private static int transformTabXCoord(int x, Control parentControl)
      {
#if !PocketPC
         //temp
         if (x != GuiConstants.DEFAULT_VALUE_INT)
         {
            if (parentControl is Panel)
            {
               Panel panel = (Panel)(parentControl);
               if (panel.Parent is TabPage)
               {
                  TabPage page = (TabPage)panel.Parent;
                  TabControl tab = (TabControl)page.Parent;
                  x -= tab.DisplayRectangle.X;
               }
            }
         }
#endif

         return x;
      }

      /// <summary>
      ///   Due to the structure of the frames in the form editor we must resize and move frames according to the
      ///   following rules: (1) All the direct children of the outmost MgSplitContainer will have an offset of 2 pixels
      ///   from its border. (2) All the frames and MgSplitContainers (except the outmost) will be resized by 4 pixels:
      ///   width -= 4, height -= 4
      /// </summary>
      /// <returns></returns>
      private static void resizeFrame(int x, int y, Control control, Rectangle rect)
      {
         // All the direct children of the outmost MgSplitContainer will have an offset of 2 pixels from its border.
         if (isDirectChildOfOutmostMgSplitContainer(control))
         {
            rect.X = (x == GuiConstants.DEFAULT_VALUE_INT
                         ? rect.X
                         : x + MgSplitContainer.SPLITTER_WIDTH / 2);
            rect.Y = (y == GuiConstants.DEFAULT_VALUE_INT
                         ? rect.Y
                         : y + MgSplitContainer.SPLITTER_WIDTH / 2);
         }

         // All the frames and MgSplitContainers (except the outmost) will be resized by 4 pixels: width -= 4, height -= 4
         if (control.Parent is MgSplitContainer)
         {
            Point pt = new Point(rect.Width, rect.Y);
            updateFrameSize(ref pt, true);
            rect.Width = pt.X;
            rect.Height = pt.Y;
         }
      }

      /// <summary>
      ///   apply changes in bounds that were caused by previous placements
      /// </summary>
      /// <param name = "control"></param>
      /// <param name = "rect">a new rectangle ready for setBounds</param>
      /// <param name = "rect">requested by guiCommand rectangle, includes DEFAULT_VALUE_INT for unset coordinates</param>
      /// <returns> rectangle with previous placement changes</returns>
      private static Rectangle applyPreviousPlacement(Control control, Rectangle rect, Rectangle requestedRect, bool isIgnorePlacement)
      {
         Rectangle prevRect = getBounds(control);
         Rectangle newRect = new Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
         Rectangle savedRect = newRect;
         Object rectObject = ((TagData)control.Tag).LastBounds;
         if (!isIgnorePlacement && (rectObject != null && rectObject is Rectangle))
         {
            savedRect = (Rectangle)rectObject;
            // if parent has placement layout installed this means that initial setbounds
            // was performed for all children and savedRect has correct information
            // add placement caused movement to the rectangle
            if (requestedRect.X != GuiConstants.DEFAULT_VALUE_INT)
            {
               if (savedRect.X != GuiConstants.DEFAULT_VALUE_INT)
                  newRect.X += prevRect.X - savedRect.X;
               savedRect.X = requestedRect.X;
            }
            if (requestedRect.Y != GuiConstants.DEFAULT_VALUE_INT)
            {
               if (savedRect.Y != GuiConstants.DEFAULT_VALUE_INT)
                  newRect.Y += prevRect.Y - savedRect.Y;
               savedRect.Y = requestedRect.Y;
            }
            if (requestedRect.Width != GuiConstants.DEFAULT_VALUE_INT)
            {
               if (savedRect.Width != GuiConstants.DEFAULT_VALUE_INT)
                  newRect.Width += prevRect.Width - savedRect.Width;
               savedRect.Width = requestedRect.Width;
            }
            if (requestedRect.Height != GuiConstants.DEFAULT_VALUE_INT)
            {
               if (savedRect.Height != GuiConstants.DEFAULT_VALUE_INT)
                  newRect.Height += prevRect.Height - savedRect.Height;
               savedRect.Height = requestedRect.Height;
            }
         }
         // this is first time
         else
            savedRect = copyRect(requestedRect);
         // save for the next time
         ((TagData)control.Tag).LastBounds = savedRect;

         //// save the orgSize for the split container
         updateMgSplitContainerOrgSize(savedRect.Width, savedRect.Height, control);

         return newRect;
      }

      /// <summary>
      ///   update the orgWidth\Height for all the child of the MgSplitContainer
      /// </summary>
      internal static void updateMgSplitContainerOrgSize(int width, int height, Control control)
      {
         if (shouldHaveMgSplitContainerData(control))
         {
            MgSplitContainerData mgSplitContainerData = null;
            mgSplitContainerData = getMgSplitContainerData(control);
            if (width != GuiConstants.DEFAULT_VALUE_INT)
               mgSplitContainerData.orgWidth = width;
            if (height != GuiConstants.DEFAULT_VALUE_INT)
               mgSplitContainerData.orgHeight = height;
         }
      }

      /// <summary>
      ///   return true if the control should have mgSplitContainerData.
      /// </summary>
      /// <param name = "control"></param>
      /// <returns></returns>
      internal static bool shouldHaveMgSplitContainerData(Control control)
      {
         bool update = false;

         if (control.Parent is MgSplitContainer)
            update = true;

         return update;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="obj"></param>
      /// <param name="font"></param>
      internal static void setFont(Object obj, Font font, int orientation, int mgFontIndex)
      {
         if (obj is LogicalControl)
         {
            ((LogicalControl)obj).Font = font;
            ((LogicalControl)obj).MgFontIndex = mgFontIndex;
         }
         else
            setFont((Control)obj, font);

         if (obj is IFontOrientation)
         {
            ((IFontOrientation)obj).FontOrientation = orientation;
            if (AccessTest && obj is MgLabel)
               ((MgLabel)obj).Invalidate();
         }
      }

      /// <summary>
      /// Set the Border color of the control according to the foreground color.
      /// </summary>
      /// <param name="obj"></param>
      /// <param name="foregroundColor"></param>
      internal static void setBorderColor(Object obj, Color foregroundColor)
      {
         if (obj is MgGroupBox)
            ((MgGroupBox)obj).BorderColor = foregroundColor;
      }

      /// <summary>
      /// </summary>
      internal static void setBackgroundColor(Object obj, Color backgroundColor, MgColor mgColor, bool isTransparent, bool isTransparentForLogiclControl, int mgColorIndex)
      {

         if (obj is TabControl)
         {
            TabControl tabControl = (TabControl)obj;
            ControlUtils.SetBGColor(tabControl, backgroundColor);
            obj = ((TagData)tabControl.Tag).TabControlPanel;
         }


         if (obj is TableControl)
            getTableManager((TableControl)obj).setMgBgColor(mgColor);
         else if (obj is LogicalControl)
         {
            ((LogicalControl)obj).BgColor = backgroundColor;
            if ((obj is LogicalControl) &&
                transparentOnlyInOwnerDraw(((LogicalControl)obj).GuiMgControl))
               ((LogicalControl)obj).IsTransparentInOwnerDraw = isTransparentForLogiclControl;
            ((LogicalControl)obj).MgColorIndex = mgColorIndex;

            if (obj is LgLabel)
               ((LgLabel)obj).IsTransparentWhenOnHeader = isTransparentForLogiclControl;
         }
         else if (obj is Control)
         {
#if !PocketPC
            if (obj is MgTextBox)
               ((MgTextBox)obj).IsTransparent = isTransparent;
            if (obj is MgLabel)
               ((MgLabel)obj).IsTransparentWhenOnHeader = isTransparent;
#endif
            ControlUtils.SetBGColor(((Control)obj), backgroundColor);

            if ((isTransparent || backgroundColor.A < 255) && obj is Panel)
            {
               //If this is our internal clientpanel, set its parent form's color as well.
               //Note: getForm() returns parent form only if this is the client panel.
               Form form = getForm(obj);
               if (form != null)
               {
                  TagData tagData = (TagData)form.Tag;

                  if (tagData.WindowType == WindowType.ChildWindow)
                  {
                     ControlUtils.SetBGColor(form, Color.Transparent);
                  }
               }
            }
         }
         else
            Debug.Assert(false);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="obj"></param>
      /// <param name="foregroundColor"></param>
      internal static void setForegroundColor(Object obj, Color foregroundColor)
      {
         if (obj is Control)
            ControlUtils.SetFGColor(((Control)obj), foregroundColor);
         else if (obj is LogicalControl)
            ((LogicalControl)obj).FgColor = foregroundColor;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="obj"></param>
      /// <param name="foregroundColor"></param>
      internal static void setGradientStyle(Object obj, GradientStyle gradientStyle)
      {
         if (obj is Control)
         {
            if (obj is TabControl)
            {
               TabControl tabControl = (TabControl)obj;
               obj = ((TagData)tabControl.Tag).TabControlPanel;
            }
            ControlUtils.SetGradientStyle((Control)obj, gradientStyle);
         }
         else if (obj is LogicalControl)
            ((LogicalControl)obj).GradientStyle = gradientStyle;
         else
            Debug.Assert(false);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="obj"></param>
      /// <param name="str"></param>
      internal static void setText(Object obj, string str)
      {
         Form form = getForm(obj);
         if (form != null)
            obj = form;

         if (obj is LogicalControl)
            ((LogicalControl)obj).Text = str;
         else if (obj is Control)
            setText((Control)obj, str);
#if !PocketPC
         //temp
         else if (obj is ToolStripItem)
            GuiUtils.setToolstripText((ToolStripItem)obj, str);
#endif
      }

      /// <summary>
      /// Gets the current TmpEditor in the hierarchy of forms (if there are child forms)
      /// </summary>
      /// <param name="form"></param>
      /// <returns></returns>
      internal static Editor GetTmpEditorFromTagData(Form form)
      {
         Form topmostForm = getTopLevelFormForChildWindow(form);

         return ((TagData)topmostForm.Tag).TmpEditor;
      }

      /// <summary>
      /// Sets the TmpEditor for the hierarchy of forms (if there are child forms)
      /// </summary>
      /// <param name="form"></param>
      /// <param name="editor"></param>
      internal static void SetTmpEditorOnTagData(Form form, Editor editor)
      {
         Form topmostForm = getTopLevelFormForChildWindow(form);

         ((TagData)topmostForm.Tag).TmpEditor = editor;
      }

   }
}
