using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.Design;
using System.Windows.Forms.Design;

namespace RuntimeDesigner
{
   /// <summary>
   /// designer options
   /// </summary>
   class MgDesignerOptionService : DesignerOptionService
   {

      protected override void PopulateOptionCollection(DesignerOptionCollection options)
      {

         if (options.Parent == null)
         {

            DesignerOptionCollection doc =
            this.CreateOptionCollection(options, "WindowsFormsDesigner", null);

            DesignerOptions doptions = new DesignerOptions();

            doptions.UseSmartTags = true;
            doptions.UseSnapLines = true;
            this.CreateOptionCollection(doc, "General", doptions);

         }

      }
   }
}
