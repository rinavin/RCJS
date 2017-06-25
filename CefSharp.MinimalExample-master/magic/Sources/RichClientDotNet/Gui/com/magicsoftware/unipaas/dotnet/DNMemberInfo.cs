using System;
using System.Reflection;

namespace com.magicsoftware.unipaas.dotnet
{
   /// <summary>
   /// This class contains info about the members of the DotNet Object. It forms a list from inner most
   /// member to the outer most member. It is used to set values on the parent if any member value is changed.
   /// </summary>
   public class DNMemberInfo
   {
      public MemberInfo memberInfo;  // reflection memberInfo
      public Object value;           // value of member
      public Object[] indexes;       // index value ARRAY_ELEMENT & INDEXER
      public int dnObjectCollectionIsn;            // reference to the field
      public DNMemberInfo parent;    // reference to the parent

      /// <summary>
      /// inits an DNMemberInfo object
      /// </summary>
      /// <param name="memberInfo">MemberInfo of 'value'</param>
      /// <param name="value">the actual object</param>
      /// <param name="indexes">of an array object</param>
      /// <param name="dnObjectCollectionIsn">key of DNObjectCollection</param>
      /// <param name="parent">parent object</param>
      private void init(MemberInfo memberInfo, Object value, Object[] indexes, int dnObjectCollectionIsn, DNMemberInfo parent)
      {
         if (memberInfo != null)
            this.memberInfo = memberInfo;
         else if (value != null)
            this.memberInfo = value.GetType();
         else
            this.memberInfo = null;

         this.value = value;
         this.indexes = indexes;
         this.dnObjectCollectionIsn = dnObjectCollectionIsn;
         this.parent = parent;
      }

      /// <summary>
      /// CTOR
      /// </summary>
      public DNMemberInfo()
      {
         init(null, null, null, -1, null);
      }

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="memberInfo">MemberInfo of 'value'</param>
      /// <param name="value">the actual object</param>
      /// <param name="indexes">of an array object</param>
      /// <param name="dnObjectCollectionIsn">key of DNObjectCollection</param>
      /// <param name="parent">parent object</param>
      public DNMemberInfo(MemberInfo memberInfo, Object value, Object[] indexes, int dnObjectCollectionIsn, DNMemberInfo parent)
      {
         init(memberInfo, value, indexes, dnObjectCollectionIsn, parent);
      }

      //We donot need functions as we set only once and all remaining places it is get.
   }
}