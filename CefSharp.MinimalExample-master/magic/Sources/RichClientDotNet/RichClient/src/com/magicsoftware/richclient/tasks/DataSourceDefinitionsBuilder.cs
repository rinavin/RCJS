using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.local.data.view;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.richclient.local.application.datasources;
using com.magicsoftware.richclient.local;
using com.magicsoftware.richclient.local.data;
using com.magicsoftware.util;
using com.magicsoftware.richclient.gui;
using com.magicsoftware.richclient.local.data.view.RangeDataBuilder;
using com.magicsoftware.richclient.local.data.view.Boundaries;

namespace com.magicsoftware.richclient.tasks
{
   class DataSourceDefinitionsBuilder
   {
      /// <summary>
      /// Build source definitions list for data controls contained in the form of the given task.
      /// </summary>
      /// <param name="task">The task whose form should be searched for data controls.</param>
      /// <returns>A list of data source definitions that correspond to the data controls. If the
      /// task does not have a form, the method will return an empty list.</returns>
      public static List<IDataSourceViewDefinition> BuildDataControlSourceDefinitions(Task task)
      {
         List<IDataSourceViewDefinition> viewDefs = new List<IDataSourceViewDefinition>();
         MgFormBase form = task.getForm();
         if (form != null)
         {
            foreach (var control in form.CtrlTab.GetControls(ControlTable.SelectDataControlPredicate))
            {
               DataSourceId sourceId = new DataSourceId();
               sourceId.CtlIdx = control.SourceTableReference.CtlIndex;
               sourceId.Isn = control.SourceTableReference.ObjectISN;

               var dsDef = ClientManager.Instance.LocalManager.ApplicationDefinitions.DataSourceDefinitionManager.GetDataSourceDefinition(sourceId);
               if (dsDef != null)
               {
                  DataSourceReference dataSourceRef = new DataSourceReference(dsDef, Access.Read);
                  Property indexProperty = control.getProp(PropInterface.PROP_TYPE_INDEX);
                  int keyIndex = indexProperty.getValueInt() - 1;

                  Property valueFieldProperty = control.getProp(PropInterface.PROP_TYPE_LINK_FIELD);
                  int valueFieldIndex = valueFieldProperty.getValueInt() - 1;
                  Property displayFieldProperty = control.getProp(PropInterface.PROP_TYPE_DISPLAY_FIELD);
                  int displayFieldIndex = displayFieldProperty.getValueInt() - 1;

                  var viewDef = new DataControlSourceViewDefinition(dataSourceRef, keyIndex, valueFieldIndex, displayFieldIndex, control.getDitIdx());

                  viewDef.RangeDataBuilder = CreateRangeDataBuilder(((MgControl)control).RangeBoundaryFactories, task, viewDef);
                  viewDefs.Add(viewDef);
               }
            }
         }
         return viewDefs;
      }

      private static IRangeDataBuilder CreateRangeDataBuilder(IEnumerable<BoundaryFactory> boundaryFactories, Task task, DataControlSourceViewDefinition sourceView)
      {
         DataSourceDefinition dsDefinition = sourceView.TaskDataSource.DataSourceDefinition;
         var fieldBoundaries = new List<FieldBoundaries>();

         if (boundaryFactories != null)
            foreach (var factory in boundaryFactories)
            {
               FieldBoundaries fb = new FieldBoundaries();
               fb.DBField = dsDefinition.Fields[factory.BoundaryFieldIndex - 1];
               fb.IndexInView = sourceView.FindOrAddField(fb.DBField);
               fb.Range = factory.CreateBoundary(task, sourceView.TaskDataSource);
               fb.IsLink = false;
               fieldBoundaries.Add(fb);
            }

         DataControlRangeDataBuilder rangeDataBuilder = new DataControlRangeDataBuilder(fieldBoundaries);
         return rangeDataBuilder;
      }

   }


}
