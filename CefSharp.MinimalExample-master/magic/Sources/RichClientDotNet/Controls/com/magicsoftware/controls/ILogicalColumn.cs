using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// Logical control for Table column
   /// </summary>
   public interface ILogicalColumn
   {
      /// <summary>
      /// get Width of column
      /// </summary>
      /// <returns></returns>
      int getWidth();

      /// <summary>
      /// Set width of column
      /// </summary>
      /// <param name="width"></param>
      /// <param name="updateNow"></param>
      void setWidthOnPlacement(int width, bool updateNow);

      /// <summary>
      /// Set width of column
      /// </summary>
      /// <param name="width"></param>
      /// <param name="updateNow"></param>
      /// <param name="moveEditors"></param>
      void setWidth(int width, bool updateNow, bool moveEditors);

      /// <summary>
      /// Return whether this column has placement
      /// </summary>
      /// <returns></returns>
      bool HasPlacement();

      /// <summary>
      /// Is column visible
      /// </summary>
      bool Visible { get; set; }

      /// <summary>
      /// index of column in tableControl , -1 if the column is not in tableControl(possible if not visible)
      /// </summary>
      int GuiColumnIdx { get; }

      /// <summary>
      /// magic column number (layer - 1)
      /// </summary>
      int MgColumnIdx { get; }

      /// <summary>
      /// returns TableColumn
      /// </summary>
      TableColumn TableColumn { get; }

      /// <summary>
      /// get column dx - change of column width
      /// </summary>
      /// <returns></returns>
      int getDx();

      /// <summary>
      /// return column starting X position
      /// </summary>
      /// <returns></returns>
      int getStartXPos();
   }
}
