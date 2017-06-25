using System;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;

namespace MgDotNet
{
   class DotNetUtils
   {
      /// <summary>
      /// returns hashcode for string
      /// </summary>
      /// <param name="str"></param>
      /// <returns></returns>
      internal static int GetHashCode(String str)
      {
         int num;
         int i = 0;
         int num2 = 0x1505;
         int num3 = num2;

         while (i < str.Length)
         {
            num = str[i];
            num2 = ((num2 << 5) + num2) ^ num;
            if (i + 1 == str.Length)
               break;
            num = str[i + 1];
            num3 = ((num3 << 5) + num3) ^ num;
            i += 2;
         }
         return (num2 + (num3 * 0x5d588b65));
      }

      /// <summary>
      /// returns hashcode MemberInfo
      /// </summary>
      /// <param name="m"></param>
      /// <returns></returns>
      internal static int GetHashCode(MemberInfo m)
      {
         return GetHashCode(m.MemberType + ":" + MemeberToString(m));
      }

      /// <summary>
      /// replacement for MemberInfo ToString() method
      /// The method is implemented differently on Modile, so we are rewriting the .NET implementation
      /// </summary>
      /// <param name="m"></param>
      /// <returns></returns>
      internal static string MemeberToString(MemberInfo m)
      {



         switch (m.MemberType)
         {
            case MemberTypes.Event:
               EventInfo eventInfo = (EventInfo)m;
               return SigToString(eventInfo.EventHandlerType) + " " + eventInfo.Name;

            case MemberTypes.Field:
               FieldInfo fieldInfo = (FieldInfo)m;
               return (SigToString(fieldInfo.FieldType) + " " + fieldInfo.Name);

            case MemberTypes.Constructor:
            case MemberTypes.Method:
               MethodBase methodBase = (MethodBase)m;
               if (methodBase.IsGenericMethod)
                  return methodBase.ToString();
               String returnType = "Void ";
               if (m.MemberType == MemberTypes.Method)
                  returnType = SigToString(((MethodInfo)methodBase).ReturnType) + " ";

               return returnType + ConstructName(methodBase);

            case MemberTypes.Property:
               PropertyInfo p = (PropertyInfo)m;
               string str = SigToString(p.PropertyType) + " " + p.Name;
               ParameterInfo[] arguments = p.GetIndexParameters();
               if (arguments == null || arguments.Length == 0)
               {
                  return str;
               }

               return (str + " [" + ConstructParameters(arguments) + "]");
            default:
               Debug.Assert(false);
               break;
         }
         return "";
      }

      /// <summary>
      /// get CallingConventions from method
      /// </summary>
      /// <param name="arguments"></param>
      /// <returns></returns>
      private static CallingConventions GetCallingConversions(ParameterInfo[] arguments)
      {
         CallingConventions callingConvention = CallingConventions.Standard;
         if (IsParams(arguments[arguments.Length - 1]))
            callingConvention |= CallingConventions.VarArgs;
         return callingConvention;
      }


      /// <summary>
      /// construct signature for parameters od method or property
      /// </summary>
      /// <param name="arguments"></param>
      /// <returns></returns>
      internal static string ConstructParameters(ParameterInfo[] arguments)
         //Type[] parameters, CallingConventions callingConvention)
      {
         if (arguments == null || arguments.Length == 0)
         {
            return "";
         }
         Type[] parameters = new Type[arguments.Length];
         for (int i = 0; i < parameters.Length; i++)
         {
            parameters[i] = arguments[i].ParameterType;
         }
         CallingConventions callingConvention = GetCallingConversions(arguments);
         string str = "";
         string str2 = "";
         for (int i = 0; i < parameters.Length; i++)
         {
            Type type = parameters[i];
            str = str + str2 + SigToString(type);
            if (type.IsByRef)
            {
               str = str.TrimEnd(new char[] { '&' }) + " ByRef";
            }
            str2 = ", ";
         }

         if ((callingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs)
         {
            str = str + str2 + "...";
         }
         return str;
      }

      /// <summary>
      /// get type's name
      /// </summary>
      /// <param name="t"></param>
      /// <returns></returns>
      internal static string SigToString(Type t)
      {
         Type elementType = t;
         while (elementType.HasElementType)
         {
            elementType = elementType.GetElementType();
         }
         if (elementType.IsNestedPublic)
         {
            return t.Name;
         }
         string str = t.ToString();
         bool isIntPtr = (elementType == typeof(IntPtr));
         if ((!elementType.IsPrimitive && !isIntPtr && (elementType != typeof(void))) && (elementType != typeof(TypedReference)))
         {
            return str;
         }
         return str.Substring("System.".Length);
      }


      /// <summary>
      /// is params 
      /// </summary>
      /// <param name="param"></param>
      /// <returns></returns>
      internal static bool IsParams(ParameterInfo param)
      {
         Object[] objects = param.GetCustomAttributes(true);
         foreach (Object obj in objects)
         {
            if (obj is ParamArrayAttribute)
               return true;
         }
         return false;
      }

      /// <summary>
      /// construct method name
      /// </summary>
      /// <param name="mi"></param>
      /// <returns></returns>
      internal static string ConstructName(MethodBase mi)
      {
         return (mi.Name + "(" + ConstructParameters(mi.GetParameters()) + ")");
      }


      /// <summary>
      /// find type of member that belongs to the lowest class in class inheritence tree
      /// </summary>
      /// <param name="contextType"></param>
      /// <param name="name"></param>
      /// <returns></returns>
      static MemberTypes findLowestMemeberType(Type contextType, string name)
      {
         MemberTypes memberType = 0;
         BindingFlags bf = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
         while (contextType != typeof(object))
         {
            MemberInfo[] members = contextType.GetMember(name, bf);
            if (members.Length > 0)
            {
               memberType = members[0].MemberType;
               break;
            }
            contextType = contextType.BaseType; //go to the parent class and look there

         }
         return memberType;
      }


      /*
       * 
       public class parent
   {
      public int Prop;


      public string this[string a] { get { return ""; } }

     
      public string val;
   }

   public class child : parent
   {
      public int val;
      public string Prop { get; set; }
      public int this[int a] { get { return 5; } }

   }

   public class grandChild : child
   {
      public void Prop(int a)
      {
         return;
      }

      
   }
       */
      /// <summary>
      /// QCR #775374 : In .NET parent members can be overriden in inherited classes. When return type of member in child class and parent class
      /// are different both member present in reflection. When parent member is property or field  it can not be used.
      /// If it is method or indexer overloading is OK.
      /// Above code that compiles OK. In runtime we have multiple members val and Prop and we need to remove member defined in base classes.
      /// 
      /// 
      /// </summary>
      /// <param name="contextType"></param>
      /// <param name="name"></param>
      /// <param name="memberInfos"></param>
      /// <returns></returns>
      static MemberInfo[] removeOverridenMembers(Type contextType, string name, MemberInfo[] memberInfos)
      {

         BindingFlags bf = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;


         Debug.Assert(memberInfos.Length > 1);
         MemberTypes memberType = memberInfos[0].MemberType;
         bool haveDifferentTypes = false; //this member is true if we have in the class member with name that have different membe type,
         //for example field and property
         for (int i = 1; i < memberInfos.Length && !haveDifferentTypes; i++)
         {
            if (memberType != memberInfos[i].MemberType)
               haveDifferentTypes = true;
         }
         //we have member of different types 
         if (haveDifferentTypes)
            memberType = findLowestMemeberType(contextType, name);

         MemberInfo[] members = GetMember(contextType, name, memberType, bf | BindingFlags.FlattenHierarchy);
         bool isMethodBaseOrIndexer = members[0] is MethodBase || (members[0] is PropertyInfo &&
           ((PropertyInfo)members[0]).GetIndexParameters().Length > 0);

         // Methods/constructors and indexers are overloaded. Fields and properties are overriden
         if (isMethodBaseOrIndexer)
         {
            if (memberType == MemberTypes.Constructor)
               return members;

            // To avoid "Ambiguous signature match" error, get distinct overloaded definitions in class hierarchy.
            members = getDistinctMemberInfoList(contextType, name, memberType, bf);
            return members;
         }

         //property /field /event can be only one, find one definded in lowest class

         while (contextType != typeof(object))
         {
            members = GetMember(contextType, name, memberType, bf | BindingFlags.DeclaredOnly);
            if (members.Length > 0)
            {
               Debug.Assert(members.Length == 1);
               return members;
            }
            contextType = contextType.BaseType;

         }
         return new MemberInfo[] { };

      }

      /// <summary>
      /// get list of all overloaded definitions of methods/indexer from the class hierarchy, for specfied member
      /// </summary>
      /// <param name="contextType"></param>
      /// <param name="name"></param>
      /// <param name="memberType"></param>
      /// <param name="bf"></param>
      /// <returns></returns>
      static MemberInfo[] getDistinctMemberInfoList(Type contextType, string name, MemberTypes memberType, BindingFlags bf)
      {
         List<MemberInfo> list = new List<MemberInfo>();
#if !PocketPC
         while (contextType != null)
         {
            MemberInfo[] memberInfos = contextType.GetMember(name, memberType, bf | BindingFlags.DeclaredOnly);
            for (int i = 0; i < memberInfos.Length; i++)
            {
               MemberInfo iMember = memberInfos[i];
               bool addToList = true;

               for (int j = 0; (j < list.Count && addToList); j++)
               {
                  MemberInfo jMember = list[j];

                  // signature is already exist in derived class
                  if (isExactMemberSignaturesMatch(iMember, jMember))
                     addToList = false;
               }

               if (addToList)
                  list.Add(iMember);
            }

            // search in parent class
            contextType = contextType.BaseType;
         }
#endif
         MemberInfo[] members = list.ToArray();
         return members;
      }

      /// <summary>
      /// compare signatures of members
      /// </summary>
      /// <param name="memberInfo1"></param>
      /// <param name="memberInfo2"></param>
      /// <returns></returns>
      static bool isExactMemberSignaturesMatch(MemberInfo memberInfo1, MemberInfo memberInfo2)
      {
         if (memberInfo1 is MethodBase)
            return isExactMethodSignaturesMatch((MethodInfo)memberInfo1, (MethodInfo)memberInfo2);
         else if (memberInfo1 is PropertyInfo && ((PropertyInfo)memberInfo1).GetIndexParameters().Length > 0)
            return isExactIndexerSignaturesMatch((PropertyInfo)memberInfo1, (PropertyInfo)memberInfo2);
         else
            Debug.Assert(false);

         return true;
      }

      /// <summary>
      /// compare signatures of methods
      /// </summary>
      /// <param name="methodInfo1"></param>
      /// <param name="methodInfo2"></param>
      /// <returns></returns>
      static bool isExactMethodSignaturesMatch(MethodInfo methodInfo1, MethodInfo methodInfo2)
      {
         ParameterInfo[] paramList1 = methodInfo1.GetParameters();
         ParameterInfo[] paramList2 = methodInfo2.GetParameters();

         if (!isExactParametersMatch(paramList1, paramList2))
            return false;

         return true;
      }

      /// <summary>
      /// compare signatures of indexers
      /// </summary>
      /// <param name="propertyInfo1"></param>
      /// <param name="propertyInfo2"></param>
      /// <returns></returns>
      static bool isExactIndexerSignaturesMatch(PropertyInfo propertyInfo1, PropertyInfo propertyInfo2)
      {
         ParameterInfo[] paramList1 = propertyInfo1.GetIndexParameters();
         ParameterInfo[] paramList2 = propertyInfo2.GetIndexParameters();

         if (!isExactParametersMatch(paramList1, paramList2))
            return false;

         return true;
      }

      /// <summary>
      /// compare signatures
      /// </summary>
      /// <param name="paramList1"></param>
      /// <param name="paramList2"></param>
      /// <returns></returns>
      static bool isExactParametersMatch(ParameterInfo[] paramList1, ParameterInfo[] paramList2)
      {
         if (paramList1.Length != paramList2.Length)
            return false;

         for (int i = 0; i < paramList1.Length; i++)
         {
            Type type1 = paramList1[i].ParameterType;
            Type type2 = paramList2[i].ParameterType;

            if (type1.IsByRef != type2.IsByRef)
               return false;

            if (type1.FullName != type2.FullName)
               return false;
         }

         return true;
      }

      /// <summary>
      /// there is no such method in mobile - implement it here
      /// </summary>
      /// <param name="type"></param>
      /// <param name="name"></param>
      /// <param name="memberTypes"></param>
      /// <param name="bindingAttr"></param>
      /// <returns></returns>
      public static  MemberInfo[] GetMember(Type type, string name, MemberTypes memberTypes, BindingFlags bindingAttr)
      {
#if PocketPC
      MemberInfo[] members = type.GetMember(name,  bindingAttr);
         List<MemberInfo> result = new List<MemberInfo>();
         foreach (var item in members)
         {
            if ((item.MemberType & memberTypes) > 0)
               result.Add(item);
         }
         return result.ToArray();
#else

         return type.GetMember(name, memberTypes, bindingAttr);
#endif
      }

      /// <summary>
      /// get member info according to the name
      /// removes for the list irrelevant members for parent classeds
      /// </summary>
      /// <param name="type"></param>
      /// <param name="name"></param>
      /// <param name="bflags"></param>
      /// <returns></returns>
      internal static MemberInfo[] GetPotentialMemeberInfos(Type type, String name, BindingFlags bflags, bool interfaceOnly)
      {
         List<Type> types = GetTypeAndInterfaceAncestors(type, interfaceOnly);
         List<MemberInfo> list = new List<MemberInfo>();
         foreach (var item in types)
         {
            list.AddRange(item.GetMember(name, bflags | BindingFlags.FlattenHierarchy));
         }
         MemberInfo[] memberInfos = list.ToArray();
         if (memberInfos.Length > 1)
            memberInfos = removeOverridenMembers(type, name, memberInfos);
         return memberInfos;
      }

      /// <summary>
      /// This Method is used to overcome Reflection interface problem
      /// Binding flag FlattenHierarchy does not work on interface, i.e.
      /// type.GetMemebers(..) on interface does not returns members of parents of the interface
      /// only members of the interface itself.
      /// See article
      /// http://stackoverflow.com/questions/358835/getproperties-to-return-all-properties-for-an-interface-inheritance-hierarchy
      /// This method creates list of all types in which we should look for members of the given type
      /// If the type is NOT interface, the list will include only the type itself
      /// if type is interface, the list will include the type and all its ancestors
      /// QCR #750064
      /// 
      /// </summary>
      /// <param name="type"></param>
      /// <returns></returns>
      static internal List<Type> GetTypeAndInterfaceAncestors(Type type, bool interfaceOnly)
      {
         List<Type> considered = new List<Type>();


         considered.Add(type);
         if (type.IsInterface || !interfaceOnly)
         {
            Queue<Type> queue = new Queue<Type>();

            queue.Enqueue(type);
            while (queue.Count > 0)
            {
               Type curType = queue.Dequeue();
               foreach (Type tmp in curType.GetInterfaces())
               {
                  if (!considered.Contains(tmp))
                  {
                     considered.Add(tmp);
                     queue.Enqueue(tmp);
                  }
               }


            }
         }
         return considered;
      }

    
   }
}
