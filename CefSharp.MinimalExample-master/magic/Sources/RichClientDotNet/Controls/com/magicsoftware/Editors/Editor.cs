using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
#if PocketPC
using MgComboBox = com.magicsoftware.controls.MgComboBox;
using NativeWindowCommon = com.magicsoftware.win32.NativeWindowCommon;
#endif

namespace com.magicsoftware.editors
{
    /// <summary>
    /// abstract editor class
    /// </summary>
    /// 
    public abstract class Editor
    {
        protected Control parentControl; //main control - tree or table
        protected Control control;   //editor control
        public Control Control
        {
            get { return control; }
            set { control = value; }
        }

        private bool isDisposed;   //true if control is disposed
        public bool IsDisposed
        {
            get { return isDisposed; }
        }


        public Editor(Control parentControl)
        {
            this.parentControl = parentControl;
        }

        /// <summary>
        /// calculate controls bounds
        /// </summary>
        /// <returns></returns>
        public abstract Rectangle Bounds();


        /// <summary>
        /// return true if control should be hidden
        /// </summary>
        /// <returns></returns>
        public abstract bool isHidden();



        /// <summary>
        /// layout editor
        /// </summary>
        public void Layout()
        {
            if (Control != null && !IsDisposed)
            {
                if (isHidden())
                    Control.Size = new Size();
                else
                {
                    parentControl.SuspendLayout();
                    Rectangle bounds = Bounds();
#if PocketPC
                    // Avoid changing bounds if they are the same - Mobile calculation may result in different 
                    // height which we will have to fix later
                    if(Control.Bounds != bounds)
#endif
                   Control.Bounds = bounds;
                   UpdateControlInfo(Control, bounds.Height); 
                

#if PocketPC
                    // For tmpEditor, the control's height does not always change as it should. Force it's 
                    // height to the right value using native call
                    if (Control.Height != bounds.Height)
                    {
                       NativeWindowCommon.SetWindowPos(Control.Handle, IntPtr.Zero,
                          0, 0, bounds.Width, bounds.Height, 
                          NativeWindowCommon.SWP_NOMOVE | NativeWindowCommon.SWP_NOZORDER);
                    }

                    // Set the combobox dropdown width to the width of the control.
                    if (Control is MgComboBox)
                       ((MgComboBox)Control).DropDownWidth = Control.Width;
#endif
                    parentControl.ResumeLayout(false);

                }

            }

        }

        protected virtual void UpdateControlInfo(Control control, int height)
        {
        }

        /// <summary>
        /// dispose editor
        /// </summary>

        public virtual void Dispose()
        {
            if (control != null)
            {
                control.Dispose();
                control = null;
            }
            isDisposed = true;

        }

        /// <summary>
        /// Hide editor
        /// </summary>

        public virtual void Hide()
        {
#if !PocketPC
           if (control != null && !control.IsDisposed)
                 control.Size = new System.Drawing.Size(0, 0);
#else
           // On Mobile we don't know if the control is disposed...
           if (control != null)
           {
              try
              {
                 control.Size = new System.Drawing.Size(0, 0);
              }
              catch
              {
              }
           }
#endif
        }


    }
}
