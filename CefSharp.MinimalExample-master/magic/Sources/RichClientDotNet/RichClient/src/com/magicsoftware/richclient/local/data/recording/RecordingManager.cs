using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.richclient.local.data.gateways.commands;

namespace com.magicsoftware.richclient.local.data.recording
{
   /// <summary>
   /// manages recording of gateway commands and applicattion data
   /// </summary>
   internal class RecordingManager
   {
      /// <summary>
      /// true if recording is enable
      /// </summary>
      internal bool EnableRecording { get; set; }

      /// <summary>
      /// name of the gateway commands recorder fileName
      /// </summary>
      internal string GatewayCommandsRecorderFileName { get; set; }

      /// <summary>
      /// name of the application definition recorder fileName
      /// </summary>

      internal string ApplicationDefinitionsRecorderFileName { get; set; }

      /// <summary>
      /// 
      /// </summary>
      internal string GatewayDataRecorderFileName { get; set; }

      GatewayCommandsRecorder gatewayCommandsRecorder = new GatewayCommandsRecorder();
      public GatewayDataRecorder gatewayDataRecorder = new GatewayDataRecorder();

      internal RecordingManager()
      {
         //this data should be configurable from magic ini
         GatewayCommandsRecorderFileName = "commands.xml";
         ApplicationDefinitionsRecorderFileName = "appData.xml";
         GatewayDataRecorderFileName = "Data.xml";

      }

      /// <summary>
      /// stop recording
      /// </summary>
      internal void StopRecording()
      {
         if (gatewayCommandsRecorder.Recording)
         {
            gatewayCommandsRecorder.StopRecording();
            gatewayCommandsRecorder.Save();
            gatewayDataRecorder.StopRecording();
            gatewayDataRecorder.Save();
            RecordApplicationDefinitions();
         }
      }

      /// <summary>
      /// start recording
      /// </summary>
      internal void StartRecording()
      {
         if (EnableRecording)
         {
            gatewayCommandsRecorder.FileName = GatewayCommandsRecorderFileName;
            gatewayDataRecorder.FileName = GatewayDataRecorderFileName;
            ClientManager.Instance.LocalManager.GatewaysManager.Recorder = gatewayCommandsRecorder;
            ClientManager.Instance.LocalManager.GatewaysManager.DataRecorder = gatewayDataRecorder;
            gatewayCommandsRecorder.StartRecording();
            gatewayDataRecorder.StartRecording();

         }
      }

      /// <summary>
      /// record application definition data
      /// </summary>
      private void RecordApplicationDefinitions()
      {
         ApplicationDefinitions applicationDefinitions = ClientManager.Instance.LocalManager.ApplicationDefinitions;
         ApplicationDefinitionsRecorder applicationDefinitionsRecorder = new ApplicationDefinitionsRecorder() { FileName = ApplicationDefinitionsRecorderFileName };
         applicationDefinitionsRecorder.StartRecording();
         applicationDefinitionsRecorder.Record(applicationDefinitions);
         applicationDefinitionsRecorder.StopRecording();
         applicationDefinitionsRecorder.Save();
      }

      internal List<GatewayCommandBase> LoadGatewayCommands()
      {
         gatewayCommandsRecorder.FileName = GatewayCommandsRecorderFileName;
         return (List<GatewayCommandBase>)gatewayCommandsRecorder.Load();
      }

      internal ApplicationDefinitions LoadApplicationDefinitions()
      {
         ApplicationDefinitionsRecorder applicationDefinitionsRecorder = new ApplicationDefinitionsRecorder() { FileName = ApplicationDefinitionsRecorderFileName };
         return (ApplicationDefinitions)applicationDefinitionsRecorder.Load();
      }

   }

}
