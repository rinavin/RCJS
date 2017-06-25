using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.env;

namespace com.magicsoftware.richclient.local.data.recording
{
   /// <summary>
   /// builder for recorder manager
   /// </summary>
   internal class RecordingManagerBuilder
   {
      /// <summary>
      /// builds recorder manager
      /// </summary>
      /// <returns></returns>
      internal RecordingManager Build()
      {
         RecordingManager recordingManager = new RecordingManager();
         EnvParamsTable env = ClientManager.Instance.getEnvParamsTable();
         recordingManager.EnableRecording = String.Equals("Y", env.get("[MAGIC_SPECIALS]EnableRecording"),
            StringComparison.CurrentCultureIgnoreCase);
         if (recordingManager.EnableRecording)
         {
            string gatewayCommandsRecorderFileName = env.get("[MAGIC_SPECIALS]GatewayRecorderFileName");
            if (gatewayCommandsRecorderFileName != null)
               recordingManager.GatewayCommandsRecorderFileName = gatewayCommandsRecorderFileName;

            string applicationDefinitionsRecorderFileName = env.get("[MAGIC_SPECIALS]ApplicationRecorderFileName");
            if (applicationDefinitionsRecorderFileName != null)
               recordingManager.ApplicationDefinitionsRecorderFileName = applicationDefinitionsRecorderFileName;
         }
         return recordingManager;

      }
   }
}
