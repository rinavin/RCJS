using System.Windows.Forms;
using com.magicsoftware.win32;
using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
#if !PocketPC
using System.ComponentModel;
using com.magicsoftware.controls.designers;
using System.Drawing;
using Controls.com.magicsoftware.controls.PropertyInterfaces;
using com.magicsoftware.controls.utils;
#else
using ComboBox = OpenNETCF.Windows.Forms.ComboBox2;
using FlatStyle = com.magicsoftware.mobilestubs.FlatStyle;
using Message = com.magicsoftware.mobilestubs.Message;
#endif

namespace com.magicsoftware.controls
{
#if !PocketPC
   [Designer(typeof(ListControlDesigner))]
   [ToolboxBitmap(typeof(ComboBox))]
   public class MgComboBox : ComboBox, IRightToLeftProperty, IChoiceControl, ISetSpecificControlPropertiesForFormDesigner, IItemsCollection
#else
   public class MgComboBox : ComboBox
#endif
   {
#if !PocketPC
      // information about the list box control inside the combo box
      IntPtr HwndDropDownList { get; set; }
      IntPtr OriginalDropDownWindowProc { get; set; }
      ControlWndProc CustomDropDownWindowProc { get; set; }
      public bool Is3DResizableComboBox { get; set; }
      public int VisibleLines { get; set; } //Saves visible lines property

      public const int BOTTOM_PADDING = 6;
      
      // event that raise when mouse down on drop down list
      public event EventHandler MouseDownOnDropDownList;
#endif

      public MgComboBox()
         : base()
      {
         this.DropDownStyle = ComboBoxStyle.DropDownList;
         
         this.DrawItem += comboBox_DrawItem;
#if !PocketPC
         ResetDropDownInfo();
         base.RightToLeft = RightToLeft.No;
#endif

#if PocketPC
         subclassCombobox();
#endif
      }

      public void SetDrawMode(DrawMode drawMode)
      {
         DrawMode = drawMode;
      }

      /// <summary>
      /// Sets item height for owner draw combobox
      /// </summary>
      /// <param name="rect"></param>
      public void SetItemHeight(int height)
      {
         int itemHeight = ((height - MgComboBox.BOTTOM_PADDING) > 0) ? height - MgComboBox.BOTTOM_PADDING : ItemHeight;
         ItemHeight = itemHeight;
         int count = VisibleLines > 0 ? Math.Min(Items.Count, VisibleLines) : Items.Count;
         Utils.SetDropDownHeight(this, count, Font, out itemHeight);
      }

#if !PocketPC
      /// <summary>
      /// 
      /// </summary>
      public void ResetDropDownInfo()
      {
         HwndDropDownList = IntPtr.Zero;
         OriginalDropDownWindowProc = IntPtr.Zero;
         CustomDropDownWindowProc = null;
      }

      public void AddRange(Object[] items)
      {
         Items.AddRange(items);
      }
#else
      public void AddRange(Object[] items)
      {
         foreach (Object item in items)
            Items.Add(item);
      }

      public FlatStyle FlatStyle { get; set; }
      public new bool DroppedDown 
      { 
         get { return base.DroppedDown; }
         set
         {
            NativeWindowCommon.SendMessage(this.Handle, NativeWindowCommon.CB_SHOWDROPDOWN, value ? 1 : 0, 0);
         }

      }

      public new int DropDownWidth { set {base.DropDownWidth = value; } }

#endif

#if !PocketPC
      /// <summary>
      ///  WindowProc of the drop down list box.
      /// </summary>
      /// <param name="hwnd"></param>
      /// <param name="msg"></param>
      /// <param name="wParam"></param>
      /// <param name="lParam"></param>
      /// <returns></returns>
      protected int DropDownWindowProc(IntPtr hwnd, uint msg, uint wParam, int lParam)
      {
         switch (msg)
         {
            case NativeWindowCommon.WM_LBUTTONDOWN:
               // when the user click on the drop down arise the event 
               if (MouseDownOnDropDownList != null)
                  MouseDownOnDropDownList(this, new EventArgs());

               break;
         }

         return NativeWindowCommon.CallWindowProc(OriginalDropDownWindowProc, hwnd, (uint)msg, wParam, lParam);
      }

      public delegate int ControlWndProc(IntPtr hwnd, uint msg, uint wParam, int lParam);

      protected override void WndProc(ref Message m)
#else
      protected int WndProc(ref Message m)
#endif
      {
         MouseEventArgs mouseEvents;
         switch (m.Msg)
         {
#if !PocketPC
            case NativeWindowCommon.CBN_DROPDOWN:
               {
                  if (OriginalDropDownWindowProc == IntPtr.Zero)
                  {
                     NativeWindowCommon.COMBOBOXINFO comboBoxInfo = new NativeWindowCommon.COMBOBOXINFO();
                     // alloc ptrComboBoxInfo 
                     IntPtr ptrComboBoxInfo = Marshal.AllocHGlobal(Marshal.SizeOf(comboBoxInfo));

                     try
                     {
                        //get combo Box Info 
                        comboBoxInfo.cbSize = (IntPtr)Marshal.SizeOf(comboBoxInfo);
                        Marshal.StructureToPtr(comboBoxInfo, ptrComboBoxInfo, true);
                        IntPtr intptr = NativeHeader.SendMessage(this.Handle, NativeWindowCommon.CB_GETCOMBOBOXINFO, 0, ptrComboBoxInfo);

                        //CB_GETCOMBOBOXINFO : If the send message succeeds, the return value is nonzero.
                        int returnValue = intptr.ToInt32();
                        if (returnValue > 0)
                        {
                           // get the info from ptrComboBoxInfo to comboBoxInfo
                           comboBoxInfo = (NativeWindowCommon.COMBOBOXINFO)Marshal.PtrToStructure(ptrComboBoxInfo, typeof(NativeWindowCommon.COMBOBOXINFO));
                           
                           // save hwnd drop down list 
                           HwndDropDownList = comboBoxInfo.hwndList;                           

                           //replace the window proc of the drop down list
                           CustomDropDownWindowProc = new ControlWndProc(DropDownWindowProc);
                           
                           // save the original drop down list (most be member in the class so the garbig collection will not free it)
                           OriginalDropDownWindowProc = (IntPtr)NativeWindowCommon.SetWindowLong(HwndDropDownList,
                              NativeWindowCommon.GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(CustomDropDownWindowProc).ToInt32());

                        }
                        else
                           Debug.Assert(false);
                     }
                     finally
                     {
                        // free the ptrComboBoxInfo
                        Marshal.FreeHGlobal(ptrComboBoxInfo);
                     }
                  }
               }
               break;
#endif
            case NativeWindowCommon.WM_LBUTTONDOWN:
            case NativeWindowCommon.WM_LBUTTONDBLCLK:
               if (!this.DroppedDown)
               {
                  // prevent drop down on combo - to prevent combo openning on non parkable controls
                  // - it must be handled manually 
                  mouseEvents = new MouseEventArgs(MouseButtons.Left, 0,
                     NativeWindowCommon.LoWord((int)m.LParam), NativeWindowCommon.HiWord((int)m.LParam), 0);
                  this.OnMouseDown(mouseEvents);

                  // provide doubleclick event
#if !PocketPC
                  if (m.Msg == NativeWindowCommon.WM_LBUTTONDBLCLK)
                     this.OnMouseDoubleClick(mouseEvents);
                  return;
#else
                  return 0;
#endif
               }
               break;
         }

#if !PocketPC
         base.WndProc(ref m);
#else
         return NativeWindowCommon.CallWindowProc(OrigWndProc, m.HWnd, (uint)m.Msg, (uint)m.WParam.ToInt32(), m.LParam.ToInt32());
#endif
      }
#if PocketPC
      // A delegate for our wndproc
      public delegate int MobileWndProc(IntPtr hwnd, uint msg, uint wParam, int lParam);
      // Original wndproc
      private IntPtr OrigWndProc;
      // object's delegate
      MobileWndProc proc;

      /// <summary> subclass the combobox - replace it's window proce
      /// </summary>
      void subclassCombobox()
      {
         proc = new MobileWndProc(WindowProc);
         OrigWndProc = (IntPtr)NativeWindowCommon.SetWindowLong(Handle, NativeWindowCommon.GWL_WNDPROC,
                         Marshal.GetFunctionPointerForDelegate(proc).ToInt32());
      }

      // Our wndproc
      private int WindowProc(IntPtr hwnd, uint msg, uint wParam, int lParam)
      {
         Message message = new Message();
         message.HWnd = hwnd;
         message.Msg = (int)msg;
         // Need this casting, for flags that are passed as large unsigned values 
         message.WParam = (IntPtr)(int)(((UIntPtr)wParam).ToUInt32());
         message.LParam = (IntPtr)lParam;

         // Call our usual code
         return WndProc(ref message);
      }

      public void CallKeyDown(KeyEventArgs e)
      {
         OnKeyDown(e);
      }
#endif

#if !PocketPC
      public void setSpecificControlPropertiesForFormDesigner(Control fromControl)
      {
         ControlUtils.SetSpecificControlPropertiesForFormDesignerForListControl((MgComboBox)fromControl, this);      
      }

      public System.Collections.IList ItemsCollection
      {
         get
         {
            return Items;
         }        
      }
#endif
      /// <summary>
      /// Draw combobox item
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void comboBox_DrawItem(object sender, DrawItemEventArgs e)
      {
         if (e.Index >= 0) // a dropdownlist may initially have no item selected, so skip the highlighting
         {
            e.DrawBackground();

            //Defect 131114 - Enlarge bounds where to write the text for bigger fonts.
            Rectangle bounds = e.Bounds;
            bounds.Width = Bounds.Width;
            bounds.X = 1;

            StringFormat stringFormat;
            if (RightToLeft == RightToLeft.Yes)
            {
               bounds.Width = bounds.Width - 5;
               stringFormat = new StringFormat(StringFormatFlags.DirectionRightToLeft);
               if (DropDownHeight < ItemHeight * Items.Count && DroppedDown) //If there is scrollbar
                  bounds.Width -= SystemInformation.VerticalScrollBarWidth;
            }
            else
               stringFormat = StringFormat.GenericDefault;
            
            stringFormat.LineAlignment = StringAlignment.Center;
            stringFormat.FormatFlags |= StringFormatFlags.NoWrap; //Defect 132171 - word wrapped in some cases.

            Color stringColor = ForeColor;
            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected) //For selected, the text should be white.
               stringColor = SystemColors.HighlightText;
            using (SolidBrush brush = new SolidBrush(stringColor))
            {
               e.Graphics.DrawString(Items[e.Index].ToString(), Font, brush, bounds, stringFormat);
            }
         }

         e.DrawFocusRectangle();
      }
   }
}
