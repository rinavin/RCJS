using System;
using System.Diagnostics;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas.management.tasks;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.dotnet;
using System.Collections.Generic;

namespace com.magicsoftware.unipaas.management.data
{
   public abstract class Field : FieldDef
   {
      #region initiatedByClient

      public MgControlBase ControlToFocus { get; internal set; }
      protected ControlTable _controls;
      protected DataViewBase _dataview; // reference to the dataview in order to access record data (updating, etc...)

      #endregion

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="id"> idx in FieldsTable </param>
      protected Field(int id) : base(id)
      {
      }

      /// <summary>
      /// get the task which the field belongs to
      /// </summary>
      public ITask getTask()
      {
         return _dataview.getTask();
      }

      /// <summary>
      ///   update the display
      /// </summary>
      public void updateDisplay(String displayValue, bool isNull, bool calledFromEditSet)
      {
         MgControlBase ctrl;
         MgControlBase ctrlValue = null;
         String defaultValue = "" + GuiConstants.DEFAULT_VALUE_INT;
         String savedValue = null;
         String savePrevValue = null;

         bool savedIsNull = false;
         bool savePrevIsNull = false;

         if (_controls != null)
         {
            MgControlBase firstControlValue = null;
            bool foundControlValue = false;
            MgControlBase savedControlToFocus = ControlToFocus;
            for (int i = 0;
                 i < _controls.getSize();
                 i++)
            {
               ctrl = _controls.getCtrl(i);
               if (calledFromEditSet)
               {
                  savedValue = ctrl.Value;
                  savePrevValue = ctrl.getPrevValueInArray(ctrl.getDisplayLine(true));

                  savedIsNull = ctrl.IsNull;
                  savePrevIsNull = ctrl.getPrevIsNullsInArray();
               }
               if (!ctrl.getForm().inRefreshDisplay())
               {
                  ctrl.resetPrevVal(); // force update of the display
                  ctrl.SetAndRefreshDisplayValue(displayValue, isNull, false);

                  // Even if the control that contains the correct value is found we don't break the loop because:
                  // (1) we need to refresh all the controls
                  // (2) the last control that was focus with the correct value is the one that should be checked                  
                  if (!ctrl.isRadio())
                  {
                     if (ctrlValue == null ||
                         ctrl.Value != null && !ctrl.Value.Equals(defaultValue) &&
                         (ctrl == savedControlToFocus))
                        ctrlValue = ctrl;
                  }
                  else
                  {
                     //Fixed bug#:780359, for radio control select the correct control
                     //if not found any ctrlvalue(the control that was in focuse)
                     //select the first control with the correct value if not exist select the first control.

                     //a. save the first control (with or without the correct value)
                     if (firstControlValue == null)
                        firstControlValue = ctrl;
                     if (ctrl.Value != null && !ctrl.Value.Equals(defaultValue))
                     {
                        //b. save the first control with the correct value
                        if (!foundControlValue)
                        {
                           firstControlValue = ctrl;
                           foundControlValue = true;
                        }
                        //c.save the control that belong to the focus control
                        if (ctrl == savedControlToFocus)
                           ctrlValue = ctrl;
                        else if (ctrlValue == null)
                           ctrlValue = firstControlValue;
                     }
                  }
               }

               if (calledFromEditSet)
                  ctrl.setValueForEditSet(savedValue, savePrevValue, savedIsNull, savePrevIsNull);
            }

            // if there was a control that had the correct value and this field is linked to more than one
            // control then it means that the control that contained the correct value might have been reset by
            // one of its siblings so there is a need to refresh its value again.
            if (ctrlValue != null)
            {
               if (calledFromEditSet)
               {
                  savedValue = ctrlValue.Value;
                  savePrevValue = ctrlValue.getPrevValueInArray(ctrlValue.getDisplayLine(true));

                  savedIsNull = ctrlValue.IsNull;
                  savePrevIsNull = ctrlValue.getPrevIsNullsInArray();
               }

               //save the control that belong to the value on the field.

               ControlToFocus = ctrlValue;
               if (_controls.getSize() > 1)
               {
                  ctrlValue.resetPrevVal(); // force update of the display
                  ctrlValue.SetAndRefreshDisplayValue(displayValue, isNull, false);
               }

               if (calledFromEditSet)
               {
                  ctrlValue.setValueForEditSet(savedValue, savePrevValue, savedIsNull, savePrevIsNull);
                  ctrlValue.getForm().getTask().setLastParkedCtrl(ctrlValue);
                  Manager.SetFocus(ctrlValue, -1);
               }

               //Fixed bug#:465616, when the control is the current focus control then refresh his focus control
               if (ctrlValue.isRadio() && ctrlValue == ctrlValue.getForm().getTask().getLastParkedCtrl())
                  Manager.SetFocus(ctrlValue, -1);
            }
         }
      }

      /// <summary>
      ///   set a reference to a control attached to this field
      /// </summary>
      /// <param name = "ctrl">the control which is attached to this field</param>
      public virtual void SetControl(MgControlBase ctrl)
      {
         if (_controls == null)
            _controls = new ControlTable();
         if (!_controls.contains(ctrl))
         {
            _controls.addControl(ctrl);

            //save the first ctrl
            if (_controls.getSize() == 1)
               ControlToFocus = ctrl;
         }
      }

      /// <summary>
      ///   removes a reference to a control attached to this field
      /// </summary>
      /// <param name = "ctrl">the control which is attached to this field</param>
      internal void RemoveControl(MgControlBase ctrl)
      {
         _controls.Remove(ctrl);
      }

      /// <summary>
      /// gets the default DotNetType for 'magicType'.
      /// </summary>
      public Type GetDefaultDotNetTypeForMagicType()
      {
         Type dotNetType = null;

         switch (getType())
         {
            case StorageAttribute.BLOB :
               {
                  if (getContentType() == BlobType.CONTENT_TYPE_BINARY)
                     dotNetType = typeof(Byte[]);
                  else
                     dotNetType = DNConvert.getDefaultDotNetTypeForMagicType(null, getType());
               }
               break;

            case StorageAttribute.NUMERIC :
               {
                  PIC pic = new PIC(_picture, StorageAttribute.NUMERIC, getTask().getCompIdx());
                  if (pic.getDec () > 0)
                     dotNetType = typeof(Double);
                  else
                     dotNetType = typeof(long);
               }
               break;
            default :
               dotNetType = DNConvert.getDefaultDotNetTypeForMagicType(null, getType());
               break;
         }

         return dotNetType;

      }

      /// <summary>
      /// Returns the list of radio controls which are attached to the same field and resides on the same task.
      /// </summary>
      internal List<MgControlBase> GetRadioCtrls()
      {
         List<MgControlBase> radioCtrls = new List<MgControlBase>();

         if (_controls != null)
         {
            for (int i = 0; i < _controls.getSize(); i++)
            {
               MgControlBase ctrl = (MgControlBase)_controls.getCtrl(i);

               if (ctrl.isRadio())
                  radioCtrls.Add(ctrl);
            }
         }

         return radioCtrls;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      public override string ToString()
      {
         return String.Format("(Field {0}-{1}) in task {2}", _id, _varName, getTask());
      }
   }
}
