using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Data;
using com.magicsoftware.unipaas.dotnet;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>
   /// This class implements handlers for DataView Control.
   /// </summary>
   class DataViewControlHandler
   {
      private static DataViewControlHandler _instance;
      internal static DataViewControlHandler getInstance()
      {
         if (_instance == null)
            _instance = new DataViewControlHandler();
         return _instance;
      }

      /// <summary>
      /// This method will register handlers for DataView Control.
      /// </summary>
      /// <param name="mgCtrl"></param>
      /// <param name="dataTable"></param>
      public void AddHandler(GuiMgControl mgCtrl, DataTable dataTable)
      {
         Object obj = ControlsMap.getInstance().object2Widget(mgCtrl);
         Control ctrl = (Control)obj;

         dataTable.ColumnChanged += ColumnChangedHandler;
         //get BindingManagerBase object and registering handler for it
         BindingManagerBase dm = ctrl.BindingContext[dataTable];
         dm.CurrentChanged += CurrentChangedHandler;
      }


      /// <summary>
      /// This method will unregister handlers for DataView Control.
      /// </summary>
      /// <param name="mgCtrl"></param>
      /// <param name="dataTable"></param>
      public void RemoveHandler(GuiMgControl mgCtrl, DataTable dataTable)
      {
         Object obj = ControlsMap.getInstance().object2Widget(mgCtrl);
         Control ctrl = (Control)obj;

         dataTable.ColumnChanged -= ColumnChangedHandler;

         //get BindingManagerBase object to unregister handler
         BindingManagerBase dm = ctrl.BindingContext[dataTable];
         dm.CurrentChanged -= CurrentChangedHandler;

      }

      /// <summary>
      /// This will be invoked on Column value change.
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void ColumnChangedHandler(object sender, DataColumnChangeEventArgs e)
      {
         //Save updated value ( e.ProposedValue) in DNObjectsCollection
         DNObjectsCollection dnObjectsCollection = DNManager.getInstance().DNObjectsCollection;
         Type type = (e.ProposedValue != null) ? e.ProposedValue.GetType() : typeof(DBNull);
         int dnKey = dnObjectsCollection.CreateEntry(type);
         dnObjectsCollection.Update(dnKey, e.ProposedValue);

         //invoke event which will update variable at core and will cause recompute & variable change handler
         int columnID = e.Column.Ordinal;
         Events.OnDVControlColumnValueChangedEvent(sender, columnID, dnKey);
      }

      /// <summary>
      /// This will be invoked when current row changes in DataView Control.
      /// </summary>
      /// <param name="sender">Destination entry key</param>
      /// <param name="e">Destination entry key</param>
      void CurrentChangedHandler(object sender, EventArgs e)
      {
         if (((CurrencyManager)sender).Position == -1)
            return;
         DataRow row = (DataRow)((DataRowView)((((CurrencyManager)sender)).Current)).Row;
         Object dataTable = row.Table;
         int rowId = ((CurrencyManager)sender).Position;
         int posIsn = -1;

         //Get the posIsn only if it is valid row in dataTable.
         if (rowId < ((DataTable)dataTable).Rows.Count)
            posIsn = (int)(row[0]);

         //invoke event which will change the current record at core and will cause refresh and  
         //recompute  
         Events.OnDVControlRowChangedEvent(dataTable, rowId, posIsn);
      }
   }
}
