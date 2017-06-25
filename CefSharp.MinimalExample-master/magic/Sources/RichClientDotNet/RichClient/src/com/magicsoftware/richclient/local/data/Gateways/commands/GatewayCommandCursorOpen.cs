using System;
using System.Diagnostics;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.gatewaytypes.data;
using System.Xml.Serialization;

namespace com.magicsoftware.richclient.local.data.gateways.commands
{
   public class GatewayCommandCursorOpen : GatewayCommandBase
   {
      internal override GatewayResult Execute()
      {
         Record();
         GatewayResult result = new GatewayResult();

         GatewayAdapterCursor gatewayAdapterCursor = GatewayAdapter.GetCursor(RuntimeCursor);
         Debug.Assert(gatewayAdapterCursor != null);

         // clear old ranges - might contain old locates.
         gatewayAdapterCursor.Ranges.Clear();

         // TODO 1:
         // fm_crsr_cache_open ???

         // Copy ranges from runtime to gateway
         if (RuntimeCursor.RuntimeCursorData.Ranges != null)
            foreach (RangeData runtimeRange in RuntimeCursor.RuntimeCursorData.Ranges)
            {
               RangeData gatewayRange = new RangeData(runtimeRange);
               RangeConvert(gatewayRange, runtimeRange, gatewayAdapterCursor);
               gatewayAdapterCursor.Ranges.Add(gatewayRange);
            }

         // TODO 2:
         // fm_copy_sql_ranges

         result.ErrorCode = GatewayAdapter.Gateway.CrsrOpen(gatewayAdapterCursor);
         SetErrorDetails(result);         
         return result;
      }

      /// <summary>
      /// fm_copy_ranges
      /// </summary>
      /// <param name="range"></param>
      private void RangeConvert(RangeData gatewayRange, RangeData runtimeRange, GatewayAdapterCursor gatewayAdapterCursor)
      {
         DBField field = gatewayAdapterCursor.Definition.FieldsDefinition[gatewayRange.FieldIndex];
         ConvertBoundaryToGateway(gatewayRange.Min, runtimeRange.Min, field);
         ConvertBoundaryToGateway(gatewayRange.Max, runtimeRange.Max, field);
      }


      /// <summary>
      /// convert baoundary value to gateway
      /// </summary>
      /// <param name="boundaryValue"></param>
      /// <param name="runtimeBoundaryValue"></param>
      /// <param name="field"></param>
      private void ConvertBoundaryToGateway(BoundaryValue boundaryValue, BoundaryValue runtimeBoundaryValue, DBField field)
      {
         if (boundaryValue.Type != RangeType.RangeNoVal && !boundaryValue.Discard)
         {
            if (ValueInGatewayFormat)
            {
               boundaryValue.Value.Value = runtimeBoundaryValue.Value.Value;
            }
            else
            {
               boundaryValue.Value.Value = GatewayAdapter.StorageConvertor.ConvertMgValueToGateway(field, runtimeBoundaryValue.Value.Value);
            }
         }
      }

     

   }
}
