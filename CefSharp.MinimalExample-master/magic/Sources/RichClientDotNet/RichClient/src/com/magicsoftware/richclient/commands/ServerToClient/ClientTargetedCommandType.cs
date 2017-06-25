namespace com.magicsoftware.richclient.commands.ServerToClient
{
   public enum ClientTargetedCommandType
   {
      Abort = 'A',
      OpenURL = 'P',
      Verify = 'V',
      EnhancedVerify = 'E',
      Result = 'S',
      AddRange = 'R',
      ClientRefresh = 'C',
      AddLocate = 'L',
      AddSort = 'T',
      ResetRange = 'G',
      ResetLocate = 'O',
      ResetSort = 'U'
   }
}