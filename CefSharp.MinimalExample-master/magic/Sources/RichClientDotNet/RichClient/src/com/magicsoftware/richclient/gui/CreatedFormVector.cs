using System.Collections.Generic;

namespace com.magicsoftware.richclient.gui
{
   /// <summary>
   /// wrapper class for interactions with vector of created MgForms
   /// </summary>
   internal class CreatedFormVector
   {
      private readonly List<MgForm> _createdFormVec;

      /// <summary>
      /// CTOR
      /// </summary>
      internal CreatedFormVector()
      {
         _createdFormVec = new List<MgForm>();
      }

      /// <summary>
      /// add form into vector
      /// </summary>
      /// <param name="mgForm"></param>
      public void add(MgForm mgForm)
      {
         _createdFormVec.Add(mgForm);
      }

      /// <summary>
      /// remove form into vector
      /// </summary>
      /// <param name="mgForm"></param>
      public void remove(MgForm mgForm)
      {
         _createdFormVec.Remove(mgForm);
      }

      /// <summary>
      /// return count of elements in vector
      /// </summary>
      /// <returns></returns>
      public int Count()
      {
         return _createdFormVec.Count;
      }

      /// <summary>
      /// clears the vector
      /// </summary>
      public void Clear()
      {
         _createdFormVec.Clear();
      }

      /// <summary>
      /// gets the element at 'index'
      /// </summary>
      /// <param name="index"></param>
      /// <returns></returns>
      public MgForm get(int index)
      {
         return _createdFormVec[index];
      }
   }
}
