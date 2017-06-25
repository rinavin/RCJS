using System.Windows.Forms;
using System.Diagnostics;
using com.magicsoftware.util;

namespace com.magicsoftware.unipaas.gui.low
{

   /// <summary> 
   /// base class of TableManager and TreeManager
   /// </summary>
   /// <author>  rinav</author>
   internal abstract class ItemsManager : ContainerManager
   {
      protected internal int _rowsInPage; // number of rows in page
      //protected internal Display display;
      /// <summary>
      /// hashset of marked items
      /// </summary>
      MgHashSet<int> markedItems = new MgHashSet<int>();

      internal ItemsManager(Control mainControl)
         : base(mainControl)
      {
      }

      /// <summary> 
      /// select row
      /// </summary>
      /// <param name="number">row number</param>
      internal abstract void setSelectionIndex(int number);

      internal abstract int getTopIndex();

      /// <summary> 
      /// return control of tableChild
      /// </summary>
      /// <param name="child"></param>
      /// <returns></returns>
      internal Control getEditorControl(LogicalControl child)
      {
         return child.getEditorControl();
      }

      /// <summary> get Rows In page</summary>
      /// <returns></returns>
      internal int getRowsInPage()
      {
         return _rowsInPage;
      }

      /// <summary>
      /// return true if item is marked
      /// </summary>
      /// <param name="index"></param>
      /// <returns></returns>
      public bool IsItemMarked(int index)
      {
         return markedItems.Contains(index);
      }

      /// <summary>
      /// return true if we are in multimaek state - any item is marked
      /// </summary>
      internal bool IsInMultimark
      {
         get
         {
            return markedItems.Count > 0;
         }
      }

      /// <summary>
      /// make row
      /// </summary>
      /// <param name="index"></param>
      public virtual void MarkRow(int index)
      {
         markedItems.Add(index);
      }

      public virtual void UnMarkRow(int index)
      {
         markedItems.Remove(index);
      }

      /// <summary>
      /// un mark all rows
      /// </summary>
      public virtual void UnMarkAll()
      {
         markedItems.Clear();
      }


      public bool ShouldPaintRowAsMarked(int mgRow, bool isSelected)
      {
         bool isRowMarked = IsItemMarked(mgRow);
         if ((isSelected && !IsInMultimark) || isRowMarked)
            return true;
         return false;
      }

   }     

}