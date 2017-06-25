using System;
using System.Collections;
using util.com.magicsoftware.util;

namespace com.magicsoftware.richclient.security
{
   // This class is used to manage the user rights on the client side. It is updated with
   // data from the server, and can be used to get the user rights without calling the server
   internal class UserRights
   {
      /* Suppose, host ctl has 2 rights and comp ctl has 3 rights.                     */
      /* In this case, size of rights_ is 6 (0th is unused).                           */
      /* So, the rights_ has the following look 0 H1 H2 C1 C2 C3.                      */
      /* Suppose, C2 is used in the Host CTL.                                          */
      /* Now, if C1 is not exposed, the RealIdx of the C2 is 3 (H1, H2, C2)            */
      /* But its corresponding entry in the rights_ is at 4th position.                */
      /* So, we do not have direct mapping between the RealIdx and rights_.            */
      /* RichClient does not have all the information by which it can determine the    */
      /* correct index in the rights_.                                                 */
      /* So, the server should send it the map.                                        */
      /* This map will contain CtlIdx, RealIdx and IndexInUserRights_.                 */
      /* Each entry in the map will be delimited by a ';'.                             */
      /* And then whole map will be appended to the rights (again delimited by a ';'). */
      /* So, the token before the first ';' will be the rights_ and the other would be */
      /* the rights map.                                                               */
      /* Now, client can simply get the index in right_ from the map and use it.       */
      /* On client, this map will be saved as a HashTable.                             */
      /* The key would be a combination of the CtlIdx and RealIdx bundled in RightKey  */
      /* class.                                                                        */
      private struct RightKey
      {
         private int _ctlIdx;
         private int _realIdx;

         internal RightKey(int ctlIdx, int realIdx)
         {
            _ctlIdx = ctlIdx;
            _realIdx = realIdx;
         }
      }

      private readonly BitArray _rights;
      private readonly Hashtable _rightsTab;
      internal string RightsHashKey { get; private set; }

      internal delegate void RightsChanged_Delegate();
      internal static event RightsChanged_Delegate RightsChanged;
      private void OnRightsChanged()
      {
         if (RightsChanged != null)
            RightsChanged();
      }

      /// <summary>
      /// 
      /// </summary>
      internal UserRights()
      {
         _rights = new BitArray(0);
         _rightsTab = new Hashtable();
         RightsHashKey = "";
      }

      internal Boolean getRight(int ctlIdx, int realIdx)
      {
         Boolean hasRight = false;
         if (realIdx == 0 || realIdx > _rights.Length)
            Logger.Instance.WriteExceptionToLog("UserRights.getRight(): bad index");
         else
         {
            RightKey rightKey = new RightKey(ctlIdx, realIdx);
            int indexInRights = (int)_rightsTab[rightKey];
            hasRight = _rights.Get(indexInRights);
         }
         return hasRight;
      } 

      // Get the user rights data string, as sent by the server, and fill the user rights array
      internal void fillUserRights(String rights)
      {
         // Start by reseting all rights to false
         _rights.SetAll(false);
         _rightsTab.Clear();

         //Even if project doesn't have any right, rights string is not blank
         System.Diagnostics.Debug.Assert(rights.Trim().Length > 0);

         // The first token in the rights is the userRights.
         // The second one is the rights ISNs hash.
         // And, the third onwards, is the map between CtlIdx. RealIdx and index in the userRights.
         string[] userRights = rights.Split(";".ToCharArray());

         string[] parsedRights = userRights[0].Split(new[] { ',' });
         // the 1st comma delimited value is the maximum number of rights possible for the user, considering all opened applications
         _rights.Length = Convert.ToInt32(parsedRights[0]);
         // Loop on user rights, set them if they are found, else turn them off 
         for (ushort i = 1; i < parsedRights.Length; i++)
         {
            int idx = Convert.ToInt32(parsedRights[i]);
            try
            {
               _rights.Set(idx, true);
            }
            catch
            {
               Logger.Instance.WriteWarningToLog("(Temp)Backwards compatibility between client >= 1.8.1.341 and server <= 1.8.1.340");
               _rights.SetAll(false);
               _rightsTab.Clear();
               foreach (var item in parsedRights)
               {
                  idx = Convert.ToInt32(item);
                  if (idx >= _rights.Length)
                     _rights.Length = idx + 1;
                  _rights.Set(idx, true);
               }
               break;
            }
         }

         setRightsHashKey(userRights[1]);

         for (int i = 2; i < userRights.Length; i++)
         {
            String[] userRight = userRights[i].Split(",".ToCharArray());

            System.Diagnostics.Debug.Assert(userRight.Length == 3);

            RightKey rightKey = new RightKey(Convert.ToInt32(userRight[0]), Convert.ToInt32(userRight[1]));

            _rightsTab.Add(rightKey, Convert.ToInt32(userRight[2]));
         }
      }

      /// <summary> Sets the RightsHashKey and raises RightsChanged event, if needed.
      /// </summary>
      /// <param name="newRights">- new rights hash code value</param>
      internal void setRightsHashKey(String newRights)
      {
         newRights = newRights.Trim();
         if (!RightsHashKey.Equals(newRights))
         {
            RightsHashKey = newRights;
            OnRightsChanged();
         }
      }
   }
}
