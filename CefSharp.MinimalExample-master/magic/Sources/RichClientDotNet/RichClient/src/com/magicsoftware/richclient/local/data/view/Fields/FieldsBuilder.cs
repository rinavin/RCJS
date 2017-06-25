using System.Collections.Generic;
using com.magicsoftware.richclient.data;
using com.magicsoftware.richclient.tasks;

namespace com.magicsoftware.richclient.local.data.view.fields
{
   /// <summary>
   /// builder of fileds adaptors array
   /// </summary>
   internal class FieldsBuilder
   {
      internal List<IFieldView> Build(Task task)
      {
         List<IFieldView> result = new List<IFieldView>();
         FieldsTable fieldsTable = (FieldsTable)task.DataView.GetFieldsTab();
         int size = fieldsTable.getSize();

         for (int i = 0; i < size; i++)
         {
            Field field = (Field)fieldsTable.getField(i);
            if (!field.IsEventHandlerField)
               result.Add(new FieldAdaptor() { Field = field });
         }

         return result;
      }
   }
}
