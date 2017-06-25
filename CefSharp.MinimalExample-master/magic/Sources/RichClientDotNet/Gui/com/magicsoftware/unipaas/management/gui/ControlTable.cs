using System;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using System.Collections.Generic;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.unipaas.management.gui
{
   public class ControlTable
   {
      private readonly MgArrayList _controls;
      private MgFormBase _mgForm;

      /// <summary>CTOR</summary>
      public ControlTable()
      {
         _controls = new MgArrayList();
      }

      /// <summary>parse input string and fill inner data</summary>
      public void fillData(MgFormBase mgForm)
      {
         XmlParser parser = Manager.GetCurrentRuntimeContext().Parser;
         _mgForm = mgForm;

         while (initInnerObjects(parser.getNextTag()))
         {
         }
      }

      /// <summary>To allocate and fill inner objects of the class</summary>
      /// <param name = "foundTagName">name of tag, of object, which need be allocated</param>
      /// <returns> boolean if inner tags finished</returns>
      private bool initInnerObjects(String foundTagName)
      {
         if (foundTagName == null)
            return false;

         XmlParser parser = Manager.GetCurrentRuntimeContext().Parser;
         if (foundTagName.Equals(XMLConstants.MG_TAG_CONTROL))
         {
            MgControlBase control = _mgForm.ConstructMgControl();
            control.fillData(_mgForm, _controls.Count);
            _controls.Add(control);
         }
         else if (foundTagName.Equals('/' + XMLConstants.MG_TAG_CONTROL))
         {
            parser.setCurrIndex2EndOfTag();
            return false;
         }
         else
            return false;

         return true;
      }

      /// <summary>get the size of the table</summary>
      /// <returns> the size of the table</returns>
      public int getSize()
      {
         return _controls.Count;
      }

      /// <summary>
      /// Get a list of controls that match a selection criteria. The selection criteria is
      /// given in the form of a predicate that determines whether the control should be included
      /// in the generated list (returns true) or not (returns false).
      /// </summary>
      /// <param name="shouldSelectControlPredicate">A predicate delegate to determine 
      /// whether the MgControlBase should be included in the returned list.</param>
      /// <returns>A list of all the controls for whom the predicate returned true.</returns>
      public IList<MgControlBase> GetControls(Predicate<MgControlBase> shouldSelectControlPredicate)
      {
         List<MgControlBase> selectedControls = new List<MgControlBase>();
         foreach (MgControlBase ctrl in _controls)
         {
            if (shouldSelectControlPredicate(ctrl))
            {
               selectedControls.Add(ctrl);
            }
         }
         return selectedControls;
      }

      /// <summary>get control by its index</summary>
      /// <param name = "idx">is the index of the control in the table</param>
      /// <returns> the requested control object</returns>
      public MgControlBase getCtrl(int idx)
      {
         if (idx < getSize())
            return (MgControlBase)_controls[idx];
         return null;
      }

      /// <summary>get control by its name</summary>
      /// <param name = "ctrlName">is the requested controls name</param>
      /// <returns> the requested control object</returns>
      public MgControlBase getCtrl(String ctrlName)
      {
         if (!string.IsNullOrEmpty(ctrlName))
         {
            foreach (MgControlBase ctrl in _controls)
            {
               if (ctrlName.Equals(ctrl.getName(), StringComparison.InvariantCultureIgnoreCase) ||
                   ctrlName.Equals(ctrl.Name, StringComparison.InvariantCultureIgnoreCase))
                  return ctrl;
            }
         }
         return null;
      }

      /// <summary>get control (which is not the frame form) by its name</summary>
      /// <param name = "ctrlName">is the requested controls name</param>
      /// <param name="ctrlType"></param>
      /// <returns> the requested control object</returns>
      internal MgControlBase getCtrlByName(String ctrlName, MgControlType ctrlType)
      {
         if (!string.IsNullOrEmpty(ctrlName))
         {
            foreach (MgControlBase ctrl in _controls)
            {
               if (ctrl.Type == ctrlType)
               {
                  if (ctrlName.Equals(ctrl.getName(), StringComparison.InvariantCultureIgnoreCase) ||
                      ctrlName.Equals(ctrl.Name, StringComparison.InvariantCultureIgnoreCase))
                     return ctrl;
               }
            }
         }
         return null;
      }

      /// <summary>
      /// get a control by its ISN
      /// </summary>
      /// <param name="isn"></param>
      /// <returns></returns>
      public MgControlBase GetControlByIsn(int isn)
      {
         foreach (MgControlBase ctrl in _controls)
         {
            if (ctrl.ControlIsn == isn)
               return ctrl;
         }
         return null;
      }

      /// <summary>returns true if this table contains the given control</summary>
      /// <param name = "ctrl">the control to look for</param>
      internal bool contains(MgControlBase ctrl)
      {
         bool contains = false;

         if (ctrl != null)
            contains = _controls.Contains(ctrl);

         return contains;
      }

      /// <summary>add a control to the table</summary>
      /// <param name = "ctrl">is the control to be added</param>
      public void addControl(MgControlBase ctrl)
      {
         _controls.Add(ctrl);
      }

      /// <summary>set a control to a cell in the table</summary>
      /// <param name = "ctrl">is the control to be added</param>
      /// <param name = "index">where to set the control</param>
      public void setControlAt(MgControlBase ctrl, int index)
      {
         // ensure that the vector size is sufficient
         if (_controls.Count <= index)
            _controls.SetSize(index + 1);
         _controls[index] = ctrl;
      }

      /// <summary>replace a control in the control table with null</summary>
      /// <param name = "index">of control to be deleted</param>
      public void deleteControlAt(int index)
      {
         setControlAt(null, index);
      }

      /// <summary>removes a control from the table and returns true on success</summary>
      /// <param name = "ctrl">the control to remove</param>
      internal bool Remove(MgControlBase ctrl)
      {
         _controls.Remove(ctrl);
         return _controls.Contains(ctrl);
      }

      /// <summary>removes a control from the table by index.</summary>
      /// <param name = "idx">the idx of the control to be removed</param>
      public void Remove(int idx)
      {
         _controls.RemoveAt(idx);
      }

      /// <param name = "inCtrl">the control whose index we need</param>
      /// <param name="includeSubs"></param>
      /// <returns> idx of a control in the table, excluding subforms control</returns>
      internal int getControlIdx(MgControlBase inCtrl, bool includeSubs)
      {
         int counter = 0;

         foreach (MgControlBase ctrl in _controls)
         {
            if (inCtrl == ctrl)
               return counter;
            if (includeSubs || !ctrl.isSubform())
               counter++;
         }

         return -1;
      }


      /// <summary>
      /// An implementation of a predicate to select controls which are data controls, to be
      /// used as parameter for the 'GetControls' method.
      /// </summary>
      /// <param name="control">The control passed as parameter by the GetControl method.</param>
      /// <returns>The method returns true if the control is a data control. Otherwise it returns false.</returns>
      public static bool SelectDataControlPredicate(MgControlBase control)
      {
         return control.isDataCtrl();
      }
   }
}
