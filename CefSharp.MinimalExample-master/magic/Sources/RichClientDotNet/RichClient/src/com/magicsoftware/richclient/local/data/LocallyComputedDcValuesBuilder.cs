using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.unipaas.management.data;
using util.com.magicsoftware.util;
using com.magicsoftware.util;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.richclient.local.data.view;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.unipaas.management.gui;

namespace com.magicsoftware.richclient.local.data
{
   /// <summary>
   /// Builder of DcValues objects from locally fetched data. The DcValues objects it creates
   /// have an identifier that marks them as local, using a Mask. This differentiates the locally
   /// created DcValues objects from the server created DcValues objects.
   /// </summary>
   class LocallyComputedDcValuesBuilder : DcValuesBuilderBase
   {
      const int LocalDcValuesMask = 0x10000000;
      static IdGenerator dcValuesIdGenerator = new IdGenerator(LocalDcValuesMask);

      private RuntimeReadOnlyView view;
      private DBField displayField;
      private DBField valueField;

      #region Build variables

      List<string> displayValues;
      List<string> linkValues;
      List<bool> nullValueFlags;
      StorageAttribute linkType;

      #endregion


      public LocallyComputedDcValuesBuilder(RuntimeReadOnlyView view)
      {
         this.view = view;
         displayField = view.DataSourceViewDefinition.DbFields[0];
         if (view.DataSourceViewDefinition.DbFields.Count > 1)
            valueField = view.DataSourceViewDefinition.DbFields[1];
         else
            valueField = displayField;
      }

      public override DcValues Build()
      {
         FetchValuesFromView();

         DcValues dcv = CreateDcValues(false);
         SetId(dcv, dcValuesIdGenerator.GenerateId());
         SetType(dcv, linkType);
         SetDisplayValues(dcv, displayValues.ToArray());
         SetLinkValues(dcv, linkValues.ToArray());
         SetNullFlags(dcv, nullValueFlags.ToArray());

         return dcv;
      }

      string ConvertToDisplayValue(FieldValue fieldValue)
      {
         string displayValue = "";

         if (fieldValue.IsNull)
            displayValue = displayField.NullDisplay;
         else
         {
            int ctlIndex = view.DataSourceViewDefinition.TaskDataSource.DataSourceDefinition.Id.CtlIdx;
            PIC fieldPicture = new PIC(displayField.Picture, (StorageAttribute)displayField.Attr, ctlIndex);
            displayValue = DisplayConvertor.Instance.mg2disp(fieldValue.Value.ToString(), "", fieldPicture, ctlIndex, false);
            displayValue = displayValue.Trim();
         }

         // Empty values should be displayed as a single blank character so that they will
         // be selectable by pressing the 'space key'.
         // This is consistent with the behavior on the server as defined in choiceinp.cpp/PrepareDisplayElement_U
         // and RT::vew_fetch_data_ctrl.
         if (String.IsNullOrEmpty(displayValue))
            displayValue = " ";

         return displayValue;
      }


      void FetchValuesFromView()
      {
         // Using a dictionary here, because I don't have a 'hash set' available.
         var usedLinkValues = new com.magicsoftware.util.MgHashSet<FieldValue>();

         displayValues = new List<string>();
         linkValues = new List<string>();
         nullValueFlags = new List<bool>();
         linkType = StorageAttribute.NONE;

         DbPos pos = new DbPos(true);
         view.OpenCursor(false, pos, BoudariesFlags.Range);
         while (view.CursorFetch().Success)
         {
            linkType = (StorageAttribute)valueField.Attr;
            FieldValue linkValue = view.GetFieldValue(valueField);

            // According to Online and RC-non-offline behavior, null link values should not
            // be added to the data control. The server does not send them at all.
            if (!linkValue.IsNull && !usedLinkValues.Contains(linkValue))
            {
               nullValueFlags.Add(false);
               linkValues.Add(linkValue.Value.ToString());
               displayValues.Add(ConvertToDisplayValue(view.GetFieldValue(displayField)));
               usedLinkValues.Add(linkValue.Clone());
            }
         }
         view.CloseCursor();
      }

   }
}
