using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas;
using com.magicsoftware.unipaas.util;
using System.Drawing;
using com.magicsoftware.util;

namespace Gui.com.magicsoftware.unipaas.util
{
   /// <summary>
   /// compares controls and returns their relative tab-order
   /// </summary>
   class TabOrderComparer : IComparer<MgControlBase>
   {
      MgFormBase form;

      /// <summary>
      /// dictionary of the X,Y coordinates of controls - the coordinates are calculated once
      /// </summary>
      Dictionary<MgControlBase, Point> coordinatesDict;

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="form"></param>
      public TabOrderComparer(MgFormBase form)
      {
         this.form = form;
         BuildDict();
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="a"></param>
      /// <param name="b"></param>
      /// <returns></returns>
      int IntCompare(int a, int b)
      {
         return a < b ? -1 : (a > b ? 1 : 0);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="control1"></param>
      /// <param name="control2"></param>
      /// <returns></returns>
      bool ControlsHaveCommonAncestor(MgControlBase control1, MgControlBase control2)
      {
         List<MgControlBase> ancestorsList = new List<MgControlBase>();
         // build the ancestors list of one control
         MgControlBase parent = control1.getLinkedParent(false);
         while (parent != null)
         {
            ancestorsList.Add(parent);
            parent = parent.getLinkedParent(false);
         }

         // go over 2nd control parents, see if they exist in the 1st control's ancestors list
         parent = control2.getLinkedParent(false);
         while (parent != null)
         {
            if (ancestorsList.Contains(parent))
               return true;
            parent = parent.getLinkedParent(false);
         }

         return false;
      }

      /// <summary>
      /// compares controls and returns their relative tab-order, according to their coordinates
      /// </summary>
      /// <param name="control1"></param>
      /// <param name="control2"></param>
      /// <returns></returns>
      int GeoCompareTabOrder(MgControlBase control1, MgControlBase control2)
      {
         int idx1, idx2;
         int ReturnValue;
         int calc = (Manager.Environment.Language == 'H' ? -1 : 1);

         // get the coordinates from the pre-calculated dictionary
         int x1 = coordinatesDict[control1].X,
            x2 = coordinatesDict[control2].X,
            y1 = coordinatesDict[control1].Y,
            y2 = coordinatesDict[control2].Y;

         ReturnValue = IntCompare(y1, y2);
         if (ReturnValue != 0)
            return ReturnValue;
         
         ReturnValue = IntCompare(x1, x2);
         if (ReturnValue != 0)
            return ReturnValue * calc;

         idx1 = form.CtrlTab.getControlIdx(control1, true);
         idx2 = form.CtrlTab.getControlIdx(control2, true);
         
         return IntCompare(idx1, idx2) * calc;
      }

      /// <summary>
      /// built according to C++ function TabbingOrderDitsCompare
      /// </summary>
      /// <param name="control1"></param>
      /// <param name="control2"></param>
      /// <returns></returns>
      public int Compare(MgControlBase control1, MgControlBase control2)
      {
         int layer1 = control1.Layer;
         int layer2 = control2.Layer;
         int returnValue;
         int calc = Manager.Environment.Language == 'H' ? -1 : 1;

         MgControlBase parent1 = control1.getLinkedParent(false) as MgControlBase;
         MgControlBase parent2 = control2.getLinkedParent(false) as MgControlBase;

         if (parent1 == parent2)
         {
            if (parent1 != null && parent1.isTableControl())
               calc = (parent1.getProp(PropInterface.PROP_TYPE_HEBREW).getValueBoolean() ? -1 : 1);

            //if both dits belong to same table/tab control - compare their layers
            if (layer1 != 0 && layer2 != 0)
            {
               returnValue = IntCompare(layer1, layer2) * calc;
               if (returnValue != 0)
                  return returnValue;
            }
            //if they have same layer perform regular GeoCompare
         }
         else if (!ControlsHaveCommonAncestor(control1, control2))
         {
            //if one dit belongs to table and other does not - we compare table position
            //with position of dit          
            if (layer1 != 0)
            {
               if (control2 == parent1)
                  return 1;
               else
                  control1 = parent1;
            }
            if (layer2 != 0)
            {
               //Fixed bug #759200, when we compare son to father the son always will be a smaller .
               if (control1 == parent2)
                  return -1;
               else
                  control2 = parent2;
            }
         }
         return GeoCompareTabOrder(control1, control2);
      }

      /// <summary>
      /// build the dictionary of the controls' coordinates
      /// </summary>
      private void BuildDict()
      {
         coordinatesDict = new Dictionary<MgControlBase, Point>();
         // data from the runtime designer
         Dictionary<MgControlBase, Dictionary<string, object>> designerInfoDict = form.DesignerInfoDictionary;
         
         Point pt = new Point();

         // get the regular runtime coordinates
         for(int i = 0; i < form.CtrlTab.getSize(); i++)
         {
            MgControlBase item = form.CtrlTab.getCtrl(i);
            pt.X = item.getProp(PropInterface.PROP_TYPE_LEFT).CalcLeftValue(item);
            pt.Y = item.getProp(PropInterface.PROP_TYPE_TOP).CalcTopValue(item);
            coordinatesDict.Add(item, pt);
         }

         // override the regular data with the data from the runtime designer
         foreach (var controlData in designerInfoDict)
         {
            foreach (var item in controlData.Value)
	         {
		         switch(item.Key)
               {
                  case Constants.WinPropTop:
                     UpdateCoordinate(controlData.Key, (int)item.Value, Axis.Y);
                     break;
                  case Constants.WinPropLeft:
                     UpdateCoordinate(controlData.Key, (int)item.Value, Axis.X);
                     break;
                  case Constants.WinPropTop + Constants.TabOrderPropertyTermination:
                     UpdateCoordinate(controlData.Key, (int)item.Value, Axis.Y);
                     break;
                  case Constants.WinPropLeft + Constants.TabOrderPropertyTermination:
                     UpdateCoordinate(controlData.Key, (int)item.Value, Axis.X);
                     break;
               }
            }         
         }
      }

      void UpdateCoordinate(MgControlBase control, int value, Axis XY)
      {
         Point pt = (Point)coordinatesDict[control]; //make function
         if (XY == Axis.Y)
            pt.Y = value;
         else
            pt.X = value;
         coordinatesDict[control] = pt;

      }
   }
}
