using System;
using System.Diagnostics;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.dotnet;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.unipaas.management
{
   public class RuntimeContextBase
   {
      public Int64 ContextID { get; set; }

      public XmlParser Parser { get; set; }           // xml parser
      public MgFormBase FrameForm { get; set; }       // form of the MDI/SDI frame
      public String LastClickedCtrlName { get; set; }
      public int LastClickedMenuUid { get; set; }     // program menu uid if executed from menu

      // CurrentClickedCtrl - always points to the current clickedCtrl. Initially null, but once clicked on any
      // control, it always points to this clicked ctrl. And, is reset back to null once we have successfully
      // parked on the parkable control.
      public MgControlBase CurrentClickedCtrl { get; set; }
      public MgControlBase CurrentClickedRadio { get; set; }
      internal String DefaultStatusMsg { get; set; }
      private bool _insertMode;

      private readonly int[] _lastCoordinates = new[] { 0, 0, 0, 0 };
      internal bool LastClickCoordinatesAreInPixels { get; private set; }

      private readonly DNException _dnException = new DNException(); //active DotNet Exception object
      public DNException DNException { get { return _dnException; } }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="contextID"></param>
      public RuntimeContextBase (Int64 contextID)
      {
         ContextID = contextID;
         LastClickedCtrlName = "";
         _insertMode = true;
         Manager.SetCurrentContextID(contextID);
      }

      /// <summary>
      /// </summary>
      /// <param name = "controlName"></param>
      /// <param name = "clientX"></param>
      /// <param name = "clientY"></param>
      /// <param name = "offsetX"></param>
      /// <param name = "offsetY"></param>
      /// <param name="isInPixels"></param>
      public void SaveLastClickInfo(String controlName, int clientX, int clientY, int offsetX, int offsetY, bool isInPixels)
      {
         _lastCoordinates[0] = offsetX; // Gives the X location of the last click, relative to the window
         _lastCoordinates[1] = offsetY; // Gives the Y location of the last click, relative to the window
         _lastCoordinates[2] = clientX; // Gives the X location of the last click, relative to the control within which the click occurred
         _lastCoordinates[3] = clientY; // Gives the Y location of the last click, relative to the control within which the click occurred

         LastClickCoordinatesAreInPixels = isInPixels;

         LastClickedCtrlName = ""; // is empty string
         if (!string.IsNullOrEmpty(controlName))
            LastClickedCtrlName = controlName;
      }

      /// <summary>
      /// </summary>
      /// <param name = "index"></param>
      /// <returns></returns>
      internal int GetClickProp(int index)
      {
         if (index >= 0 && index < 4)
            return _lastCoordinates[index];

         throw new ApplicationException("in RuntimeContextBase.getClickProp() illegal index: " + index);
      }

      /// <summary> return the InsertMode </summary>
      public bool IsInsertMode()
      {
         return _insertMode;
      }

      /// <summary> toggle the _insertMode (between insert and overwrite). </summary>
      public void ToggleInsertMode()
      {
         _insertMode = !_insertMode;
      }
   }
}
