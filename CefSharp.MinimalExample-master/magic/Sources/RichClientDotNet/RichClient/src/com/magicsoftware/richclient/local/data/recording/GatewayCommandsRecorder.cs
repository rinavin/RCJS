using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using util.com.magicsoftware.util;
using System.Reflection;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.richclient.local.data.gateways.commands;

namespace com.magicsoftware.richclient.local.data.recording
{
   public class GatewayCommandsRecorder : RecorderBase<GatewayCommandBase>
   {
      ClassSerializer serializer = new ClassSerializer();
      List<CommandString> commands = new List<CommandString>();

      public class CommandString
      {
         public String CommandType { get; set; }
         public String CommandText { get; set; }

      }

      /// <summary>
      /// filename to save the commands
      /// </summary>
      public override string FileName
      {
         get
         {
            return serializer.FileName;
         }
         set
         {
            serializer.FileName = value;
         }
      }

      public string XMLString
      {
         get
         {
            return serializer.XMLString;
         }
         set
         {
            serializer.XMLString = value;
         }
      }
      protected override void Add(GatewayCommandBase command)
      {
         CommandString commandsString = new CommandString() { CommandType = command.GetType().ToString() };
         commandsString.CommandText = serializer.SerializeToString(command);
         commands.Add(commandsString);
      }

      public override void Save()
      {
         serializer.SerializeToFile(commands);
      }

      

      public override Object Load()
      {
         List<CommandString> commandsStrings = (List<CommandString>)serializer.Deserialize(typeof(List<CommandString>));
         Assembly assembly = typeof(GatewayCommandBase).Assembly;
         List<GatewayCommandBase> commands = new List<GatewayCommandBase>();
         foreach (var item in commandsStrings)
         {
            Type type = assembly.GetType(item.CommandType);
            GatewayCommandBase command = (GatewayCommandBase)serializer.DeserializeFromString(type, item.CommandText);
            command.ShouldUpdateDataBaseLocation = false;
            commands.Add(command);
         }
         return (Object)commands;
      }
   }
}
