using System;
using System.Collections.Generic;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.tasks;
using com.magicsoftware.unipaas.management.data;
using System.Diagnostics;

namespace com.magicsoftware.unipaas.management.gui
{
   /// <summary>
   ///   This class provides the implementation of the status bar.
   /// </summary>
   /// <author>  Kaushal Sanghavi</author>
   public class MgStatusBar : MgControlBase
   {
      //Collection for storing pane objects added to status bar.
      private Dictionary<int, MgControlBase> _paneObjectsCollection;

      public int PanesCount
      {
         get { return _paneObjectsCollection.Count; }
      }

      /// <summary>
      ///   Constructor
      /// </summary>
      public MgStatusBar(TaskBase task)
         : base(MgControlType.CTRL_TYPE_STATUS_BAR, task.getForm(), -1)
      {
         //initialize collection.
         _paneObjectsCollection = new Dictionary<int, MgControlBase>();
      }

      /// <summary>
      ///   sort the panes in order of their layers
      /// </summary>
      internal void sortLinkedControls()
      {
         try
         {
            getLinkedControls().Sort(new CompareLayers());
         }
         catch (Exception)
         {
            // TODO : (SUSHANT) alert developer  
         }
      }

     /// <summary>Gets the pane for the specified pane index.</summary>
     /// <param name="paneIdx">Index of the pane object</param>
     /// <returns>pane object</returns>
      public MgControlBase getPane(int paneIdx)
      {
         MgControlBase pane = null;

         if (_paneObjectsCollection.ContainsKey(paneIdx))
            pane = _paneObjectsCollection[paneIdx];

         return pane;
      }

      /// <summary>Adds the pane to the status bar/// </summary>
      /// <param name="statusBarIdx">Status bar index</param>
      /// <param name="paneType">Pane type: label or image</param>
      /// <param name="paneIdx">Pane index</param>
      /// <param name="paneWidth">Pane width</param>
      /// <param name="showPaneBorder">Show border or not</param>
      public void AddPane(MgControlType paneType, int paneIdx, int paneWidth, bool showPaneBorder)
      {
         var num = new NUM_TYPE();

         //Get status bar index.
         int statusBarIdx = getForm().getControlIdx(this);
         
         // Create pane object
         MgControlBase paneObj = getForm().ConstructMgControl(paneType, getForm().getTask(), statusBarIdx);

         if (paneObj != null)
         {
            // Set width of pane object.     
            num.NUM_4_LONG(paneWidth);
            paneObj.setProp(PropInterface.PROP_TYPE_WIDTH, num.toXMLrecord());

            // Set Idx of pane object.     
            num.NUM_4_LONG(paneIdx);
            paneObj.setProp(PropInterface.PROP_TYPE_LAYER, num.toXMLrecord());

            // Set border of pane object.     
            if (showPaneBorder)
               paneObj.setProp(PropInterface.PROP_TYPE_BORDER, "1");

            //Properties specific to image pane only.
            if (paneObj.Type == MgControlType.CTRL_TYPE_SB_IMAGE)
            {
               paneObj.DataType = StorageAttribute.ALPHA;
               paneObj.setPIC("1000");

               num.NUM_4_LONG((int)CtrlImageStyle.Copied);
               paneObj.setProp(PropInterface.PROP_TYPE_IMAGE_STYLE, num.toXMLrecord());
            }

            //Add to control table collection.
            getForm().CtrlTab.addControl(paneObj);

            //Add to collection.
            Debug.Assert(!_paneObjectsCollection.ContainsKey(paneIdx));
            _paneObjectsCollection[paneIdx] = paneObj;
         }
      }

      /// <summary>Writes the data on the pane with the specified index.</summary>
      /// <param name="paneIdx">Pane index</param>
      /// <param name="info">Info to to shown on status bar</param>
      public void UpdatePaneContent(int paneIdx, string info)
      {
         if (_paneObjectsCollection.ContainsKey(paneIdx))
         {
            //get the pane object.
            MgControlBase paneObj = _paneObjectsCollection[paneIdx];

            if (paneObj != null)
            {
               switch (paneObj.Type)
               {
                  case MgControlType.CTRL_TYPE_SB_IMAGE:
                     //Show icon in image pane
                     paneObj.SetAndRefreshDisplayValue(info, info == null, false);
                     break;

                  case MgControlType.CTRL_TYPE_SB_LABEL:
                     //write the message to pane
                     paneObj.setProp(PropInterface.PROP_TYPE_TEXT, info);
                     paneObj.getProp(PropInterface.PROP_TYPE_TEXT).RefreshDisplay(true, 0);
                     break;

                  default:
                     Debug.Assert(false);
                     break;
               }
               Commands.beginInvoke();
            }
         }
      }

      /// <summary>
      /// Writes tool tip to the specified pane.
      /// </summary>
      /// <param name="paneIdx">Pane idx</param>
      /// <param name="toolTipText">tool tip text</param>
      public void UpdatePaneToolTip(int paneIdx, string toolTipText)
      {
         MgControlBase pane = getPane(paneIdx);
         if (pane != null)
         {
            pane.setProp(PropInterface.PROP_TYPE_TOOLTIP, toolTipText);
            pane.getProp(PropInterface.PROP_TYPE_TOOLTIP).RefreshDisplay(false);
         }
      }

      /// <summary>
      /// Refresh the text property of all the StatusPanes.
      /// </summary>
      public void RefreshPanes()
      {
         foreach(KeyValuePair<int, MgControlBase> obj in _paneObjectsCollection)
         {
            MgControlBase paneControl = obj.Value;
            switch (paneControl.Type)
            {
               case MgControlType.CTRL_TYPE_SB_LABEL:
                  paneControl.getProp(PropInterface.PROP_TYPE_TEXT).RefreshDisplay(true, 0);
                  break;

               default:
                  Debug.Assert(false);
                  break;
            }
         }
         Commands.beginInvoke();
      }

      #region Nested type: CompareLayers

      /// <summary>
      ///   supporting class for Collections.sort (used for sorting linked controls)
      /// </summary>
      private class CompareLayers : IComparer<MgControlBase>
      {
         #region IComparer<MgControl> Members

         /// <summary>
         /// </summary>
         public int Compare(MgControlBase pane1, MgControlBase pane2)
         {
            int pane1Layer = 0;
            int pane2Layer = 0;

            if (!pane1.Equals(pane2))
            {
               pane1Layer = pane1.getProp(PropInterface.PROP_TYPE_LAYER).getValueInt();
               pane2Layer = pane2.getProp(PropInterface.PROP_TYPE_LAYER).getValueInt();

               if (pane1Layer == pane2Layer)
               {
                  throw new ApplicationException("Duplicate Layers");
               }
            }

            return pane1Layer - pane2Layer;
         }

         #endregion
      }

      #endregion
   }
}