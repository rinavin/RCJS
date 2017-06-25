using System;
using System.Collections.Generic;
using System.Text;

namespace Controls.com.magicsoftware
{
   /// <summary>
   /// Indicates whether entity is Refreshable and handles it refresh
   /// </summary>
   public interface IRefreshable
   {
      /// <summary>
      /// refresh
      /// </summary>
      /// <param name="changed"></param>
      void Refresh(bool changed);

      /// <summary>
      /// sets that refresh is needed
      /// </summary>
      bool RefreshNeeded { get; set; }
   }
}
