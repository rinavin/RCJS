using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using com.magicsoftware.editors;

namespace com.magicsoftware.controls
{

    /// <summary>
    /// Class table editor - manages window table control
    /// </summary>
    /// 
    public class TableEditor : Editor
    {

        /// <summary>
        /// table row
        /// </summary>
        public int Row { get; set; }

        public BoundsComputer BoundsComputer { get; set; } //interface for computing editors bounds


        /// <summary>
        /// set table column
        /// </summary>
        public int ColumnIdx
        {
            get { return 0; }
            set
            {
            }

        }
        /// <summary>
        /// activated on any change 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="ea"></param>
        void columnChange(object sender, EventArgs ea)
        {
        }

        public TableEditor(TableControl table)
            : this(table, 0, TableControl.TAIL_COLUMN_IDX)
        {
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="table"></param>
        /// <param name="row"></param>
        /// <param name="columnIdx"></param>
        public TableEditor(TableControl table, int row, int columnIdx)
            : base(table)
        {
        }



        /// <summary>
        /// return true if comtrol is hidden
        /// </summary>
        /// <returns></returns>
        public override bool isHidden()
        {
            return false;
        }

        /// <summary>
        /// calculate editor bounds
        /// </summary>
        /// <returns></returns>
        public override Rectangle Bounds()
        {
            return new Rectangle();

        }

        /// <summary>
        /// dispose table editor
        /// </summary>
        public override void Dispose()
        {
        }

        /// <summary>
        /// hide editor
        /// </summary>
        public override void Hide()
        {
        }
    }
}
