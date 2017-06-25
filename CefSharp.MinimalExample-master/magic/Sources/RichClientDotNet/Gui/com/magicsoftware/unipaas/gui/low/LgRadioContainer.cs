using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using com.magicsoftware.controls;
using com.magicsoftware.util;
#if PocketPC
using Appearance = com.magicsoftware.mobilestubs.Appearance;
using ContentAlignment = OpenNETCF.Drawing.ContentAlignment2;
#endif

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>
   /// This is a logical control corrospond to an actual radiobutton control (in Table control).
   /// When we have a radiobutton on table control, we do create a logical control for it. And when 
   /// we associate an actual radio control to this logical control, all properties are set by this class.
   /// </summary>
   internal class LgRadioContainer : LogicalControl
   {
      private bool _refreshItemList; //if true - refresh items list in editor
      //   original image
      internal Image OrgImage { get; private set; }
      internal String ImageFileName { get; private set; }

      /// <summary>
      /// Item list to be displayed for a radiobutton.
      /// </summary>
      private String[] _itemList;
      internal String[] ItemList
      {
         get { return _itemList; }
         set 
         {
            _itemList = value;
            _refreshItemList = true;
            Refresh(true);
         }
      }

      /// <summary>
      /// Index of currently checked radio button.
      /// </summary>
      private int _selectionIdx;
      internal int SelectionIdx
      {
         set
         {
            _selectionIdx = value;
            Refresh(true);
         }
      }

      /// <summary>
      /// Nos of columns in the radio panel
      /// </summary>
      private int _columnsInRadio;
      internal int ColumnsInRadio
      {
         set
         {
            _columnsInRadio = value;
            Refresh(true);
         }
      }

      /// <summary>
      /// Appearance of the radio button
      /// </summary>
      private Appearance _appearance;
      internal Appearance Appearance
      {
         get { return _appearance; }
         set
         {
            _appearance = value;
            Refresh(true);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="guiMgControl"></param>
      /// <param name="containerControl"></param>
      internal LgRadioContainer(GuiMgControl guiMgControl, Control containerControl) :
         base(guiMgControl, containerControl)
      {
         _selectionIdx = -1;
      }

      internal void SetImage(Image image, String imageFileName)
      {
         OrgImage = image;
         ImageFileName = imageFileName;
         Refresh(true);
      }

      /// <summary> set refreshItemList</summary>
      /// <param name="val"></param>
      internal void setRefreshItemList(bool val)
      {
         _refreshItemList = val;
      }

      /// <summary>
      /// Set the control specific properties.
      /// </summary>
      /// <param name="control"></param>
      internal override void setSpecificControlProperties(Control control)
      {
         MgRadioPanel radioPanel = ((MgRadioPanel)control);

         if (_refreshItemList)
         {
            GuiUtils.SetLayoutNumColumns(radioPanel, _columnsInRadio);
            GuiUtils.setItemsList(control, _itemList);
            _refreshItemList = false;
         }

         ControlUtils.SetStyle3D(radioPanel, Style);
         ControlUtils.SetBorderStyle(radioPanel, ControlBorderType);
         GuiUtils.SetRadioAppearance(control, _appearance);
         GuiUtils.SetImageToRadio(OrgImage, ImageFileName, radioPanel, CtrlImageStyle.Copied);
         ControlUtils.SetContentAlignment(radioPanel, ContentAlignment);
         GuiUtils.SetChecked(radioPanel, _selectionIdx);
         ControlUtils.SetMultiLine(radioPanel, MultiLine);

         ((TagData)radioPanel.Tag).Tooltip = Tooltip;
                  
         GuiUtils.resumeLayout(control, true);
         GuiUtils.performLayout(control);
      }
   }
}
