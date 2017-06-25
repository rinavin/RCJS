using System;
namespace com.magicsoftware.unipaas.gui.low
{

   /// <summary> save information about the sub form , child of MgSplitContainer</summary>
   /// <author>  rinat
   /// 
   /// </author>
   class MgSplitContainerData
   {
      internal int orgWidth; // The original Width of the sub form
      internal int orgHeight; // The original Height of the sub form
      internal bool allowHorPlacement; // for MgSplitContainer with style SWT.HORIZONTAL, if the placement is allowed
      internal bool allowVerPlacement; // for MgSplitContainer with style SWT.VERTICAL, if the placement is allowed 

      /// <summary> </summary>
      internal MgSplitContainerData()
      {
         orgWidth = 0;
         orgHeight = 0;
         allowHorPlacement = true;
         allowVerPlacement = true;
      }
   }
}