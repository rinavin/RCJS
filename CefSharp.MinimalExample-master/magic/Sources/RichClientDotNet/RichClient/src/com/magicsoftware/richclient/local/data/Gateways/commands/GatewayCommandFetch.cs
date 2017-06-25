using System;
using System.Diagnostics;
using com.magicsoftware.gatewaytypes;
using System.Xml.Serialization;

namespace com.magicsoftware.richclient.local.data.gateways.commands
{
   public class GatewayCommandFetch : GatewayCommandFetchBase
   {
      internal override GatewayResult Execute()
      {
         Record();
         GatewayResult result = new GatewayResult();
         GatewayAdapterCursor gatewayAdapterCursor = GatewayAdapter.GetCursor(RuntimeCursor);
         Debug.Assert(gatewayAdapterCursor != null);

         gatewayAdapterCursor.Definition.ClearFlag(CursorProperties.DummyFetch);
         
         // TODO 1 ???:
         //if (mg_crsr->blobs)
         //   crsr_fill_blob_info(db_crsr, mg_crsr, dbd->dbh);

         // TODO 2: for join
         //link_save_area = fm_link_crsr_buf_save(join_info->db_join_crsr);
         //retval = SQL_wrap_crsr_fetch_join(join_info->db_join_crsr,
         //            !mg_crsr->fetchSource.is_set(FETCH_BYPASS_TRANS_CACHE));
         //fm_link_crsr_buf_restore(join_info->db_join_crsr, link_save_area);

         // TODO 3: for not join
         //if (not join)
            result.ErrorCode = GatewayAdapter.Gateway.CrsrFetch(gatewayAdapterCursor);

            if (result.Success)
            {
               // TODO 4: for dummy fetch
               //   if (db_crsr->properties.is_set (DUMMY_FETCH)  && mg_crsr->dummy_fetch_buf != NULL)

               // TODO 5: check if a record that was fetched is within the ranges
               // fm_check_rngs

               // TODO 6: fetch Blobs
               // CrsrFetchBLOBs & CrsrFetchBLOBsJoin

               // TODO 7: If not DUMMY_FETCH
               // fm_flds_mg_4_db
               ConvertToRuntime(gatewayAdapterCursor);
               RecordData();
            }
            else
               // TODO: Error handling.
               // Temporary !!!
               if (result.ErrorCode != GatewayErrorCode.NoRecord)
                  SetErrorDetails(result);

            return result;
      }
   }
}
