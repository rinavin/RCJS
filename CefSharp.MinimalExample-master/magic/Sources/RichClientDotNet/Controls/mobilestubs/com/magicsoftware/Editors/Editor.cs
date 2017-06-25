using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

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
            get { return null; }
            set { }
        }

        public bool IsDisposed
        {
            get { return false; }
        }


        public Editor(Control parentControl)
        {
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
        }

        /// <summary>
        /// dispose editor
        /// </summary>

        public virtual void Dispose()
        {
        }

        /// <summary>
        /// Hide editor
        /// </summary>

        public virtual void Hide()
        {
        }


    }
}
