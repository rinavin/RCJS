using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.unipaas.management.gui
{
   /// <summary>
   /// A rule for refreshing a property only if the form is marked as 'refreshed'. This
   /// helps in reducing refresh cycles.
   /// </summary>
   public class RefreshOnlyAfterFormIsRefreshedRule : IPropertyRefreshRule
   {
      MgFormBase dependencyForm;

      /// <summary>
      /// Instantiates a new rule that is dependent on the specified form.
      /// </summary>
      /// <param name="dependencyForm">The form whose state will be checked to determine whether a property should be refreshed or not.</param>
      public RefreshOnlyAfterFormIsRefreshedRule(MgFormBase dependencyForm)
      {
         this.dependencyForm = dependencyForm;
      }

      public bool ShouldRefresh
      {
         get { return dependencyForm.FormRefreshed; }
      }
   }
}
