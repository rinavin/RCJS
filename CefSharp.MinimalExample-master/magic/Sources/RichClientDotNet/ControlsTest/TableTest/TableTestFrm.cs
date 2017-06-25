using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using com.magicsoftware.controls;
using com.magicsoftware.editors;
#if PocketPC
using ContentAlignment = OpenNETCF.Drawing.ContentAlignment2;
using Brushes = com.magicsoftware.mobilestubs.Brushes;
#endif

namespace ContolsTest.TableTest
{
   public partial class TableTestFrm : Form
   {
       TableColumn[] columns = new TableColumn[4];
      public TableTestFrm()
      {
         InitializeComponent();
         this.tableControl1.ItemDisposed += new TableControl.TableItemDisposeEventHandler(tableControl1_TableItemDispose);
         this.tableControl1.EraseItem += new TableControl.TableDrawRowHandler(tableControl1_TableDrawBgCell);
         this.tableControl1.PaintItem += new TableControl.TableDrawRowHandler(tableControl1_TableDrawFgCell);
         tableControl1.ReorderEnded += new EventHandler(tableControl1_ReorderEnded);


         for (int i = 0; i < columns.Length; i++)
         {
            TableColumn column = columns[i] = new TableColumn();
            tableControl1.Columns.Add(column);
            column.Text = "Text \n" + i;
            column.Width = 50;
            column.FgColor = Color.Blue; ;
            column.ContentAlignment = ContentAlignment.TopRight;
            column.AfterTrackHandler += new TableColumn.SectionTrackHandler(column_AfterColumnTrack);

         }

         createTableEditor();

      }

      void tableControl1_ReorderEnded(object sender, EventArgs e)
      {
         tableControl1.Refresh();
      }

      class MyBoundsComputer : BoundsComputer 
      {

         #region BoundsComputer Members

         Rectangle BoundsComputer.computeEditorBounds(Rectangle cellRectangle, bool isHeaderEditor)
         {
            return new Rectangle(cellRectangle.X + 5, cellRectangle.Y + 2, cellRectangle.Width - 10, cellRectangle.Height - 2);
         }

         #endregion
      }
      void createTableEditor()
      {
         TextBox text = new TextBox();
         tableControl1.Controls.Add(text);
         text.Text = "aaa";
         TableEditor editor = new TableEditor(tableControl1, 0, columns[1]);
         editor.BoundsComputer = new MyBoundsComputer() ;
         editor.Control = text;
         editor.Layout();
      }

      void column_AfterColumnTrack(object sender, EventArgs ea)
      {
         TableColumn column = (TableColumn)sender;
         Console.WriteLine(column.Index);
      }

      void tableControl1_TableDrawBgCell(object sender, TablePaintRowArgs ea)
      {
         SolidBrush brush = new SolidBrush(Color.Blue);
         if (ea.Row % 2 == 1)
            brush = new SolidBrush(Color.Salmon);
         ea.Graphics.FillRectangle(brush, ea.Rect);

      }
      void tableControl1_TableDrawFgCell(object sender, TablePaintRowArgs ea)
      {
#if !PocketPC
          if (ea.Row < 15)
             ea.Graphics.DrawString ("row" + ea.Row, tableControl1.Font, Brushes.White, ea.Rect);
#endif
      }

      private void tableControl1_Load(object sender, EventArgs e)
      {
      }

      void tableControl1_TableItemDispose(object sender, TableItemDisposeArgs ea)
      {
         //Console.WriteLine("Disposed {0}", ea.Item.Idx);
      }

      private void tableControl1_MouseDoubleClick(object sender, MouseEventArgs e)
      {
         tableControl1.TopIndex = 2;
      }

      private void TableTestFrm_Load(object sender, EventArgs e)
      {

      }

 


   }
}
