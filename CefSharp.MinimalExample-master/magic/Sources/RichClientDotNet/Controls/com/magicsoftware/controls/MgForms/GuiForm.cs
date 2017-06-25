using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using com.magicsoftware.win32;
using System.Runtime.InteropServices;
using com.magicsoftware.controls.utils;
using com.magicsoftware.util;
#if !PocketPC
using System.ComponentModel;
using System.ComponentModel.Design;
#endif
using com.magicsoftware.controls.designers;
using System.Drawing;
using System.Diagnostics;
using System.Globalization;
using com.magicsoftware.support;

namespace com.magicsoftware.controls
{


    /// <summary>
    /// The form can not be moved
    /// used for FitToMdi forms
    /// </summary>
    /// 
#if !PocketPC
    [Designer(typeof(FormDocumentDesigner))]
    [ToolboxBitmap(typeof(Form))]
    public class GuiForm : Form, IRightToLeftProperty, IGradientColorProperty, ITextProperty, ICanParent, IMgContainer, IShowGridProperty, ISupportsTransparentChildRendering
#else
   public class GuiForm : Form, ITextProperty
#endif
    {
        public bool AllowMove { get; set; }

        /// <summary>
        /// true, for blocking batch forms that need to prevent focus ontheir ancestors
        /// </summary>
        public bool ShouldBlockAnsestorActivation { get; set; }

        /// <summary>
        /// form has started closing
        /// </summary>
        public bool IsClosing { get; set; }


#if !PocketPC
        #region IGradientColorProperty Members

        public GradientColor GradientColor { get; set; }
        public GradientStyle GradientStyle { get; set; }

        #endregion

        #region IShowGridProperty Members

        public virtual bool ShowGrid { get { return true; } }

        #endregion

        public delegate void SizingDelegate(Object sender, SizingEventArgs e);

        public event SizingDelegate Sizing;

        public delegate void NCActivateDelegate (Object sender, EventArgs e);

        public event NCActivateDelegate NCActivate;

        /// <summary>
        /// the event is raised on left mouse down on the non client area
        /// </summary>
        public event NCMouseEventHandler NCMouseDown;


        public delegate void CanRepositionDelegate(Object sender, RepositionEventArgs e);
        /// <summary>
        /// check if we can allow repozition of a window
        /// </summary>
        public event CanRepositionDelegate CanReposition;

#if !PocketPC
        public delegate void CopyDataDelegate(Object sender, CopyDataEventArgs e);

        /// <summary>
        /// raised when wm_activate message is recieved
        /// </summary>
        public event WMActivateDelegate WMActivate;


        public delegate void WMActivateDelegate(Object sender, ActivateArgs e);


        /// <summary>
        /// Event called when WM_COPYDATA is received to the form
        /// </summary>
        public event CopyDataDelegate CopyData;
#endif

        DateTime activateAppTime;
        public System.DateTime ActivateAppTime
        {
            get { return activateAppTime; }
        }

        DateTime mouseActivateAppTime;
        public System.DateTime MouseActivateAppTime
        {
            get { return mouseActivateAppTime; }
        }
        public GuiForm()
           : base()
        {
            this.DoubleBuffered = true;
            AllowMove = true;
            GradientStyle = GradientStyle.None;

            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (BackColor == Color.Transparent || BackColor.A < 255)
            {
                if (Parent is ISupportsTransparentChildRendering)
                    ((ISupportsTransparentChildRendering)Parent).TransparentChild = this;

                int hScrollHeight = (this.HScroll ? SystemInformation.HorizontalScrollBarHeight : 0);
                int vScrollWidth = (this.VScroll ? SystemInformation.VerticalScrollBarWidth : 0);

                int borderWidth = ((this.Width - this.ClientSize.Width) - vScrollWidth) / 2;
                int titleBarHeight = this.Height - this.ClientSize.Height - (2 * borderWidth) - hScrollHeight;

                System.Drawing.Drawing2D.GraphicsContainer g = e.Graphics.BeginContainer();
                Rectangle translateRect = this.Bounds;
                e.Graphics.TranslateTransform(-(Left + borderWidth), -(Top + titleBarHeight + borderWidth));
                PaintEventArgs pe = new PaintEventArgs(e.Graphics, translateRect);
                this.InvokePaintBackground(Parent, pe);
                this.InvokePaint(Parent, pe);
                e.Graphics.ResetTransform();
                e.Graphics.EndContainer(g);
                pe.Dispose();

                if (Parent is ISupportsTransparentChildRendering)
                    ((ISupportsTransparentChildRendering)Parent).TransparentChild = null;
            }

            ControlRenderer.PaintBackgoundColorAndImage(this, e.Graphics, true, DisplayRectangle);

            if (TransparentChild != null && DesignMode)
                ControlRenderer.RenderOwnerDrawnControlsOfParent(e.Graphics, TransparentChild);
        }


        /// <summary>
        /// raise event on non client mouse down
        /// </summary>
        protected virtual void OnNCMouseDown(Message m)
        {
            if (NCMouseDown != null)
                NCMouseDown(this, new NCMouseEventArgs(m));
        }

        Message? postponedMessage = null;
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            // Defect# 109658: When an MDI child is closed, it throws exception for Icon, as it seems it is garbage collected.
            // This is Windows problem. This is not related to Print Preview, but happens when any other window is opened and
            // another MDI child window is closed in background. 
            // Refer: https://social.msdn.microsoft.com/Forums/windows/en-US/2de9797a-d71c-4846-92ad-5371a541b439/cannot-access-a-disposed-objectobject-name-icon?forum=winformsapplications
            if (!e.Cancel && MdiParent != null && WindowState == FormWindowState.Maximized) // Defect# 124330: This creates problem for Modal window. So apply fix only for problematic case i.e. MDI child window
                Hide();
        }

        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            base.SetBoundsCore(x, y, width, height, specified);

            // Defect# 115626: when we attempt to set dimensions of form to be more than desktop bounds (resolution), winforms does not set this size. 
            // It sets max size of window to be dimension + 12 pixels. This is what that happens. This can even be reproduced with dual monitor when dimension is increased accordingly.
            // Some links:
            // https://msdn.microsoft.com/en-us/library/25w4thew(v=vs.110).aspx
            // http://bytes.com/topic/c-sharp/answers/252405-increase-maximum-size-windows-form-designer
            // http://www.codeproject.com/Questions/273139/I-need-to-increase-max-size-length-in-my-windows-F
            // Below mentioned link has suggestion which fixes the problem. The call to MoveWindow when bounds are set, helps.
            // http://stackoverflow.com/questions/6651115/is-the-size-of-a-form-in-visual-studio-designer-limited-to-screen-resolution
            // Restrict this fix to Child window since we have problem for it for now.

            if (Parent != null && (width > SystemInformation.MaxWindowTrackSize.Width || height > SystemInformation.MaxWindowTrackSize.Height))
                NativeWindowCommon.MoveWindow(Handle, x, y, width, height, true);
        }

        /// <summary>
        /// execute mnemonic method 
        /// </summary>
        /// <param name="control"></param>
        /// <param name="charCode"></param>
        /// <returns></returns>
        private bool InvokeProcessMnemonic(Control control, char charCode)
        {
            bool wasHandled = false;

            System.Reflection.BindingFlags bindingAttrs = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            System.Reflection.MemberInfo[] memberinfos = control.GetType().GetMember("ProcessMnemonic", bindingAttrs);

            if (memberinfos != null)
            {
                System.Reflection.MethodInfo mi = (System.Reflection.MethodInfo)memberinfos[0];
                object[] a = new object[] { charCode };

                wasHandled = (bool)mi.Invoke(control, a);

            }
            return wasHandled;
        }


        /// <summary>
        /// process Mnemonic on control and his children without subforms
        /// </summary>
        /// <param name="control"></param>
        /// <param name="charCode"></param>
        /// <returns></returns>
        private bool MGProcessMnemonicOnControlAndChildrensWithoutSubforms(Control control, char charCode)
        {
            bool wasHandled = false;
            for (int i = 0; i < control.Controls.Count && !wasHandled; i++)
            {
                Control ctrl = control.Controls[i];

                // don't execute executeMnemonic for subform control 
                bool isSubformControl = ControlUtils.IsSubFormControl(ctrl);
                if (!isSubformControl)
                {
                    wasHandled = InvokeProcessMnemonic(ctrl, charCode);

                    // if not handled then execute on children of this control
                    if (!wasHandled)
                        wasHandled = MGProcessMnemonicOnControlAndChildrensWithoutSubforms(ctrl, charCode);
                }
            }

            return wasHandled;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentCntrol"></param>
        /// <param name="charCode"></param>
        /// <returns></returns>
        private bool MgProcessMnemonic(Control currentCntrol, char charCode)
        {
            bool wasHandled = false;

            if (currentCntrol != null)
            {
                // try to execute mnemonic on me 
                wasHandled = InvokeProcessMnemonic(currentCntrol, charCode);

                if (!wasHandled)
                {
                    //get the first sub form control that control place on it (if any) 
                    Control continerControl = ControlUtils.GetSubFormControl(currentCntrol);

                    // if there is not subform, get the form
                    if (continerControl == null)
                        continerControl = this;

                    // process Mnemonic on control and his children not include subforms 
                    wasHandled = MGProcessMnemonicOnControlAndChildrensWithoutSubforms(continerControl, charCode);
                }
            }


            return wasHandled;
        }

        /// <summary>
        /// Process mnemonic on form and handled the hebrew char also 
        /// </summary>
        /// <param name="wasHandled"></param>
        /// <param name="charCode"></param>
        /// <returns></returns>
        private bool ProcessMnemonicOnForm(bool wasHandled, char charCode)
        {
            // if not found mnemonic then call the basic ProcessMnemonic
            if (!wasHandled)
            {
                wasHandled = base.ProcessMnemonic(charCode);

                if (!wasHandled && MnemonicHelper.NeedToHandelMnemonicForHebChar)
                {
                    char hebChartoHandel = MnemonicHelper.GetHebCharFromEngChar(charCode);
                    if (hebChartoHandel != ' ')
                    {
                        wasHandled = base.ProcessMnemonic(hebChartoHandel);
                        // fixed defect #:142416, while user is select menu that is heb menu
                        // then update the lang to heb 
                        if (wasHandled)
                            Application.CurrentInputLanguage = InputLanguage.FromCulture(new CultureInfo("he-IL"));

                    }
                }

            }
            return wasHandled;
        }

        /// <summary>
        /// Process mnemonic related to focused control
        /// handel the eng char and if not handled then check for hebrew char 
        /// </summary>
        /// <param name="charCode"></param>
        /// <returns></returns>
        private bool ProcessMnemonicRelatedToFocusedControl(char charCode)
        {
            bool wasHandled = false;

            Control checkControl = ControlUtils.GetFocusedControl();

            if (checkControl != null)
            {
                wasHandled = MgProcessMnemonic(checkControl, charCode);

                // handle the hebrew char
                if (!wasHandled && MnemonicHelper.NeedToHandelMnemonicForHebChar)
                {
                    char hebChartoHandel = MnemonicHelper.GetHebCharFromEngChar(charCode);
                    if (hebChartoHandel != ' ')
                    {
                        wasHandled = MgProcessMnemonic(checkControl, hebChartoHandel);
                        // fixed defect #:142416, while user is select menu that is heb menu
                        // then update the lang to heb 
                        if (wasHandled)
                            Application.CurrentInputLanguage = InputLanguage.FromCulture(new CultureInfo("he-IL"));
                    }

                }
            }
            return wasHandled;
        }

        /// <summary>
        /// Fixed defect #139067:
        /// return true if the the code is in ProcessMnemonic to avoid loop
        /// </summary>
        public bool InProcessMnemonic { get; set; }

        /// <summary>
        /// handle the mnemonic , for hebrew version check also the accelerator of the heb
        /// </summary>
        /// <param name="charCode"></param>
        /// <returns></returns>
        protected override bool ProcessMnemonic(char charCode)
        {
            bool wasHandled = false;

            if (!InProcessMnemonic)
            {
                InProcessMnemonic = true;
                wasHandled = ProcessMnemonicRelatedToFocusedControl(charCode);

                // if not handled then process mnemonic on form
                if (!wasHandled)
                    wasHandled = ProcessMnemonicOnForm(wasHandled, charCode);

                InProcessMnemonic = false;
            }
            return wasHandled;

        }


        protected override void WndProc(ref Message m)
        {

            switch (m.Msg)
            {
#if !PocketPC
                case NativeWindowCommon.WM_COPYDATA:
                    CopyData(this, new CopyDataEventArgs(m.LParam));
                    break;

                case NativeWindowCommon.WM_WINDOWPOSCHANGING:
                    WindowReposition(m);
                    break;
#endif

                case NativeWindowCommon.WM_SIZING:
                    if (Sizing != null)
                    {
                        SizingEventArgs sizingEventArgs = new SizingEventArgs(
                           (NativeWindowCommon.SizingEdges)m.WParam,
                           (NativeWindowCommon.RECT)Marshal.PtrToStructure(m.LParam, typeof(NativeWindowCommon.RECT))
                           );
                        Sizing(this, sizingEventArgs);
                        Marshal.StructureToPtr(sizingEventArgs.DragRect, m.LParam, true);
                    }
                    break;

                case NativeWindowCommon.WM_SYSCOMMAND:
                    if (!AllowMove && m.WParam.ToInt32() == NativeWindowCommon.SC_MOVE)
                        return;
                    else
                    {
                        //Defect #125874. It is .Net behavior (or bug?) that if MaximizeBox = false and the form is being maximized, it covers 
                        //the whole screen --- even expanding below the task bar.
                        //So, temporarily set MaximizeBox = true.
                        bool isRestoring = (m.WParam.ToInt32().Equals(NativeWindowCommon.SC_RESTORE));
                        bool orgMaxBox = this.MaximizeBox;
                        if (isRestoring)
                            this.MaximizeBox = true;

                        //Defect 81609: simulating behavior in unipass 1.9:
                        //the ACT_HIT message added to the queue by WM_NCLBUTTONDOWN, was not extracted untill whole WM_SYSCOMMAND was not exectuted.
                        //WM_SYSCOMMAND exits only after mouse is up and all needed windows are activated
                        //if we will handle ACT_HIT before it, WM_SYSCOMMAND will override our handling and activate clicked window disregarding magic logic.
                        base.WndProc(ref m);

                        if (isRestoring)
                            this.MaximizeBox = orgMaxBox;

                        RaiseMouseDownIfNeeded();
                        return;
                    }

                case NativeWindowCommon.WM_NCLBUTTONDOWN:
                case NativeWindowCommon.WM_NCRBUTTONDOWN:
                case NativeWindowCommon.WM_NCLBUTTONDBLCLK:
                    if (!AllowMove && m.WParam.ToInt32() == NativeWindowCommon.HTCAPTION && m.Msg == NativeWindowCommon.WM_NCLBUTTONDOWN)
                    {
                        OnNCMouseDown(m);
                        return;
                    }
                    else
                    {
                        int param = m.WParam.ToInt32();
                        Debug.Write("WM_NCLBUTTONDOWN Param=" + param);
                        Debug.Flush();
                        postponedMessage = m;
                        // Activate();
                    }
                    break;

                case NativeWindowCommon.WM_CHILDACTIVATE:
                    NativeWindowCommon.SendMessage(this.Handle, NativeWindowCommon.WM_NCACTIVATE, 1, 0);
                    break;

                case NativeWindowCommon.WM_NCACTIVATE:
                    if (NCActivate != null && m.WParam.ToInt32() == 1)
                        NCActivate(this, new EventArgs());
                    break;
                case NativeWindowCommon.WM_ACTIVATE:
                    if (WMActivate != null)
                    {
                        ActivateArgs args = new ActivateArgs();
                        args.WmParam = (int)m.WParam;
                        WMActivate(this, args);
                        if (args.StopActivation)  //restore focus to last form
                            return;
                    }
                    break;

                case NativeWindowCommon.WM_ACTIVATEAPP:
                    if (m.WParam.ToInt32() == 1) // application is activated
                        activateAppTime = DateTime.Now;

                    break;
                case NativeWindowCommon.WM_MOUSEACTIVATE:
                    mouseActivateAppTime = DateTime.Now; ;
                    break;


            }

            base.WndProc(ref m);
        }

        /// <summary>
        /// window reposition
        /// </summary>
        /// <param name="m"></param>
        private void WindowReposition(Message m)
        {
            RepositionEventArgs repositionEventArgs = new RepositionEventArgs();
            if (CanReposition != null)
            {
                NativeWindowCommon.WINDOWPOS wp = new NativeWindowCommon.WINDOWPOS();
                wp = (NativeWindowCommon.WINDOWPOS)m.GetLParam(typeof(NativeWindowCommon.WINDOWPOS));

                repositionEventArgs.HwndInsertAfter = wp.hwndInsertAfter;

                CanReposition(this, repositionEventArgs);

                if (!repositionEventArgs.CanReposition)
                {
                    wp.flags |= NativeWindowCommon.SWP_NOZORDER;
                    Marshal.StructureToPtr(wp, m.LParam, true);
                }

            }
        }

        public void RaiseMouseDownIfNeeded()
        {
            if (postponedMessage != null)
            {
                //  Activate();
                OnNCMouseDown((Message)postponedMessage);
                postponedMessage = null;
            }
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

        #region ISupportsTransparentChildRendering

        public Control TransparentChild { get; set; }

        #endregion
    }

    public class SizingEventArgs : EventArgs
    {
        public NativeWindowCommon.SizingEdges SizingEdge { get; private set; }
        public NativeWindowCommon.RECT DragRect { get; set; }
        public SizingEventArgs(NativeWindowCommon.SizingEdges sizingEdge, NativeWindowCommon.RECT dragRect)
           : base()
        {
            SizingEdge = sizingEdge;
            DragRect = dragRect;
        }
    }

#if !PocketPC
    /// <summary>
    /// Contains COPYDATASTRUCT information recieved when WM_COPYDATA is received to form
    /// </summary>
    public class CopyDataEventArgs : EventArgs
    {
        /// <summary>
        /// COPYDATASTRUCT structure
        /// </summary>
        public IntPtr Copydata { get; private set; }

        #region Ctors

        public CopyDataEventArgs(IntPtr copydata)
        {
            Copydata = copydata;
        }

        #endregion
    }

    public class RepositionEventArgs : EventArgs
    {
        /// <summary>
        /// output!!
        /// </summary>
        public bool CanReposition { get; set; }
        public IntPtr HwndInsertAfter { get; set; }

        public RepositionEventArgs()
           : base()
        {
            CanReposition = true;
        }
    }

    public class ActivateArgs : EventArgs
    {
        public bool StopActivation { get; set; }
        public int WmParam { get; set; }
    }
#endif
}
