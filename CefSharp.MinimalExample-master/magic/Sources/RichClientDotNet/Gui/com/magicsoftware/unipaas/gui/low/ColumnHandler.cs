using System;
using com.magicsoftware.controls;
using System.Drawing;
using System.Windows.Forms;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>
   ///  Handler for column control
   /// </summary>
   /// <author>  rinat</author>
   class ColumnHandler : HandlerBase
   {
      private static ColumnHandler _instance;
      internal static ColumnHandler getInstance()
      {
         if (_instance == null)
            _instance = new ColumnHandler();
         return _instance;
      }

      private ColumnHandler()
      {
      }


      /// <summary>
      ///  adds handler for Column control
      /// </summary>
      /// <param name="column"> </param>
      internal void addHandler(TableColumn column)
      {
         column.AfterTrackHandler += AfterColumnTrackHandler;
         column.ClickHandler += ColumnClick;
         column.ClickFilterHandler += ColumnFilterClick;
      }

      /// <summary> </summary>
      internal override void handleEvent(EventType type, Object sender, EventArgs e)
      {
         TableColumn column = (TableColumn)sender;
         TagData td = (TagData)column.Tag;
         LgColumn columnManager = td.ColumnManager;
         int direction = -1;
         String columnHeaderString;

         if (columnManager == null)
            return;

         var contextIDGuard = new Manager.ContextIDGuard(Manager.GetContextID(columnManager.GuiMgControl));
         try
         {
            switch (type)
            {
               case EventType.AFTER_COLUMN_TRACK:
                  columnManager.updateWidth();
                  break;

               case EventType.COLUMN_CLICK:
                  direction = columnManager.getSortDirection();

                  //As event is raise by click action, then the argument for the event will be 
                  //column title if its not null else it will be in form of Column:XXX
                  //XXX - column number
                  columnHeaderString = (!String.IsNullOrEmpty(column.Text)
                                          ? column.Text
                                          : "Column: " + (columnManager.MgColumnIdx + 1));

                  Events.OnColumnClick(columnManager.GuiMgControl, direction, columnHeaderString);
                  break;

               case EventType.COLUMN_FILTER_CLICK:
                  columnHeaderString = (!String.IsNullOrEmpty(column.Text)
                        ? column.Text
                        : "Column: " + (columnManager.MgColumnIdx + 1));
                  int index = column.Index; 

                  //Calculate column top left point
                  Header columnHeader = ((HeaderSectionEventArgs)e).Item.Header; 
                  Panel panel = GuiUtils.getParentPanel(columnHeader.Parent);
                  Point panelStart = panel.PointToScreen(new Point());
                  panelStart.Offset(panel.AutoScrollPosition);
                  Point columnStart = columnHeader.PointToScreen(new Point(columnHeader.GetHeaderSectionStartPos(((HeaderSectionEventArgs)e).Item.Index), 0));

                  int x = columnStart.X - panelStart.X;
                  int y = columnStart.Y - panelStart.Y;
                  int width = ((HeaderSectionEventArgs)e).Item.Width;
                  int height = ((HeaderSectionEventArgs)e).Item.Header.Height;

                  Events.OnColumnFilter(columnManager.GuiMgControl, columnHeaderString, x, y, width, height);
                  break;
            }
         }
         finally 
         {
            contextIDGuard.Dispose();
         }
      }
   }
}