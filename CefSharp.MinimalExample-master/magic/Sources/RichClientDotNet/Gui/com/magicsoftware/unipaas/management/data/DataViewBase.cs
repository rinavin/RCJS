using System;
using System.Collections.Generic;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas.management.tasks;

namespace com.magicsoftware.unipaas.management.data
{
   /// <summary>
   /// functionality required by the GUI namespace from the DataViewBase class.
   /// </summary>
   public abstract class DataViewBase
   {
      #region initiatedFromServer

      protected Dictionary<int, DcValues> _dcValsCollection = new Dictionary<int, DcValues>();
      private bool _emptyDataview; // Indicates the task is in Empty Dataview
      protected FieldsTable _fieldsTab; // <dvheader> ...</dvheader> tag
      protected TaskBase _task; // container task

      #endregion

      #region initiatedByClient

      // dummy DcValues for use with non-DC choice controls
      private readonly DcValues _emptyChoice = new DcValues(true, false);
      private readonly DcValues _emptyChoiceForVectors = new DcValues(true, true);

      #endregion

      /// <summary>
      ///   parse the DVHEADER tag
      /// </summary>
      public virtual void fillHeaderData()
      {
         _fieldsTab.fillData(this);
      }

      /// <summary>
      ///   get a field by its id
      /// </summary>
      /// <param name = "id">field id </param>
      public FieldDef getField(int id)
      {
         if (_fieldsTab != null)
            return _fieldsTab.getField(id);

         Events.WriteExceptionToLog("in DataView.getField(int): There is no fieldsTab object");
         return null;
      }

      /// <returns> the emptyDataview</returns>
      public bool isEmptyDataview()
      {
         return _emptyDataview;
      }

      /// <param name = "emptyDataview">the emptyDataview to set</param>
      public void setEmptyDataview(bool emptyDataview)
      {
         _emptyDataview = emptyDataview;
      }

      /// <summary>
      ///   return a reference to the task of this dataview
      /// </summary>
      public ITask getTask()
      {
         return _task;
      }

      /// <summary>
      ///   get dcValues object by its id
      /// </summary>
      /// <param name = "dcId">id of a data control values </param>
      public DcValues getDcValues(int dcId)
      {
         if (dcId == DcValues.EMPTY_DCREF)
            return getEmptyChoice();

         DcValues dcv;
         _dcValsCollection.TryGetValue(dcId, out dcv);
         return dcv;
      }

      /// <returns>dummy DcValues for use with non-DC choice controls</returns>
      internal DcValues getEmptyChoice()
      {
         return _emptyChoice;
      }

      internal DcValues getEmptyChoiceForVectors()
      {
         return _emptyChoiceForVectors;
      }

      /// <summary>
      ///   get fields table (<dvheader>) of the ((DataView)dataView)
      /// </summary>
      public virtual FieldsTable GetFieldsTab()
      {
         return _fieldsTab;
      }
   }
}
