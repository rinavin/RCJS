using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using com.magicsoftware.unipaas.dotnet;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.unipaas.gui.low;
using com.magicsoftware.util;
#if !PocketPC
using System.Reflection.Emit;
using System.Diagnostics;
#endif

namespace com.magicsoftware.unipaas.util
{
   public static class ReflectionServices
   {
      //this class will represent an assembly
      internal class MgAssembly
      {
         internal String FullName { get; set; } //assembly qualified name
         internal String Name { get; set; } //assembly partial name
         internal bool UseSpecificVersion { get; set; } //use specific version of assemblies
         internal bool Loading { get; set; }
         internal bool IsGuiThreadExecution { get; set; } //thus, if assembly contains controls or other GUI types
         internal String FileName { get; set; }
         internal bool IsLoaded
         {
            get
            {
               return (_assembly != null);
            }
         }

         private Assembly _assembly;
         internal Assembly Assembly
         {
            get
            {
               if (_assembly == null)
                  Load();
               return _assembly;
            }
            set
            {
               _assembly = value;
            }
         }

         /// <summary>loads an assembly, can throw an exception in case of an error</summary>
         private void Load()
         {
            Loading = true;
            //step 1 -  look in GAC for same version
            try
            {
               _assembly = Assembly.Load(FullName);
            }
            catch (Exception)
            { }

            //step 2 - look in GAC for any version
#if !PocketPC
            if (_assembly == null && !UseSpecificVersion)
            {
               try
               {

#pragma warning disable
                  _assembly = Assembly.LoadWithPartialName(Name);
#pragma warning enable
               }
               catch (Exception)
               { }
            }
#endif

            //step 3 - assembly is not in GAC - bring assembly from server
            if (_assembly == null)
            {
               try
               {
                  if (FileName != null)
                  {
                     // load the assembly:
                     if (Misc.isWebURL(FileName, Manager.Environment.ForwardSlashUsage) ||
                         Events.IsRelativeRequestURL(FileName))
                     {
                        // get the assembly's local file name (from the server thru the cache manager).
                        FileName = Events.GetDNAssemblyFile(FileName);
                     }
                     _assembly = Assembly.LoadFrom(FileName);
                  }
               }
               catch (Exception ex)
               {
                  // TODO: It can be GUI Thread.
                  Events.WriteExceptionToLog(ex.Message);
               }
            }
            Loading = false;
         }
      }

      //a hash table of all assemblies
      internal static Dictionary<int, MgAssembly> assemblies = new Dictionary<int, MgAssembly>();
#if !PocketPC
      static Dictionary<Type, OpCode> typeOpCodes;
#endif
 
      /// <summary>static constructor</summary>
      static ReflectionServices()
      {
         AddAssembly("mscorlib", false, null, false);
#if !PocketPC
         typeOpCodes = new Dictionary<Type, OpCode> 
         { 
                                        {typeof (sbyte), OpCodes.Ldind_I1}, 
                                        {typeof (byte), OpCodes.Ldind_U1}, 
                                        {typeof (char), OpCodes.Ldind_U2}, 
                                        {typeof (short), OpCodes.Ldind_I2}, 
                                        {typeof (ushort), OpCodes.Ldind_U2}, 
                                        {typeof (int), OpCodes.Ldind_I4}, 
                                        {typeof (uint), OpCodes.Ldind_U4}, 
                                        {typeof (long), OpCodes.Ldind_I8}, 
                                        {typeof (ulong), OpCodes.Ldind_I8}, 
                                        {typeof (bool), OpCodes.Ldind_I1}, 
                                        {typeof (double), OpCodes.Ldind_R8}, 
                                        {typeof (float), OpCodes.Ldind_R4} 
         };
         AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
#endif
      }

#if !PocketPC
      /// <summary>
      /// if .NET could not find an assembly ma be we need to load it
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="args"></param>
      /// <returns></returns>
      static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
      {
         String name = args.Name.Substring(0, args.Name.IndexOf(','));

         Assembly assembly = null;
         MgAssembly mgAssembly;
         if (assemblies.TryGetValue(GetHashCode(name), out mgAssembly) )
         {
            if (!mgAssembly.Loading) // prevent infinite recursion
              assembly = mgAssembly.Assembly;
         }
         return assembly;
      }
#endif
      
      public static int GetHashCode(String str)
      {
         return MgDotNet.DotNetUtils.GetHashCode(str);
      }

      public static int GetHashCode(MemberInfo m)
      {
         return MgDotNet.DotNetUtils.GetHashCode(m);
      }

      /// <summary>returns true if type must be worked with in GUI thread</summary>
      /// <param name="type"></param>
      /// <returns></returns>
      internal static bool isGuiThreadExecution(Type type)
      {
         String name;
         int hashCode =  getAssemblyHashCode(type.Assembly.GetName().ToString(), out name);
         return IsGuiThreadExecution(hashCode);
      }

      /// <summary></summary>
      /// <param name="hashCode"></param>
      /// <returns></returns>
      private static bool IsGuiThreadExecution(int hashCode)
      {
         bool result = false;
         MgAssembly mgAssembly = null;
         if (assemblies.TryGetValue(hashCode, out mgAssembly))
            result = mgAssembly.IsGuiThreadExecution;
         return result;
      }

      /// <summary>adds an assembly to the list of the assemblies</summary>
      /// <param name="assembly"></param>
      /// <returns></returns>
      public static bool AddAssembly(String fullName, bool useSpecific, String fileName, bool isGuiThreadExecution)
      {
         bool added = false;

         String name;
         int hashCode = getAssemblyHashCode(fullName, out name);
         if (!assemblies.ContainsKey(hashCode))
         {
            MgAssembly assembly = new MgAssembly();
            assembly.FullName = fullName;
            assembly.UseSpecificVersion = useSpecific;
            assembly.FileName = fileName;
            assembly.Name = name;
            assembly.IsGuiThreadExecution = isGuiThreadExecution;
            assemblies.Add(hashCode, assembly);
            added = true;
         }

         return added;
      }

      /// <summary>
      /// removes an assembly from the assemblies list
      /// </summary>
      /// <param name="hashCode">assembly hash code</param>
      public static void RemoveAssembly (int hashCode)
      {
         string errMessage = null;

         if (assemblies.ContainsKey(hashCode))
            assemblies.Remove(hashCode);
         else
            errMessage = string.Format("Assembly with hashcode {0} not found.", hashCode);

         if (errMessage != null)
            Events.WriteErrorToLog(errMessage);
      }

      /// <summary>
      /// get assembly hash code
      /// </summary>
      /// <param name="fullName"></param>
      /// <param name="name"></param>
      /// <returns></returns>
      private static int getAssemblyHashCode(String fullName, out String name)
      {
         string[] tokens = fullName.Split(',');
         name = tokens[0];
         return GetHashCode(name);
      }

#if !PocketPC
      public static bool AddAssembly(String fullName, byte[] code, bool isGuiThreadExecution)
      {
         bool added = false;

         string[] tokens = fullName.Split(',');
         String name = tokens[0];
         int hashCode = GetHashCode(name);
         if (!assemblies.ContainsKey(hashCode))
         {
            MgAssembly assembly = new MgAssembly();
            assembly.FullName = fullName;
            assembly.UseSpecificVersion = false;
            assembly.FileName = fullName;
            assembly.Name = fullName;
            assembly.IsGuiThreadExecution = isGuiThreadExecution;
            try
            {
               if (code != null)
                  assembly.Assembly = Assembly.Load(code);
            }
            catch(Exception){}
            assemblies.Add(hashCode, assembly);
            added = true;
         }
         return added;
      }

      //Check if assembly content is stored in collection or not.
      public static bool ContainsAssembly(string assemblyName)
      {
         return assemblies.ContainsKey(GetHashCode(assemblyName));
      }
#endif

      /// <summary></summary>
      /// <param name="o"></param>
      /// <returns></returns>
      public static Type GetType(Object o)
      {
         return o.GetType();
      }

      /// <summary></summary>
      /// <param name="memberInfo"></param>
      /// <returns></returns>
      public static Type GetType(MemberInfo memberInfo)
      {
         Type type = null;

         if (memberInfo != null)
         {
            switch (memberInfo.MemberType)
            {
               case MemberTypes.Field:
                  type = ((FieldInfo)memberInfo).FieldType;
                  break;
               case MemberTypes.Property:
                  type = ((PropertyInfo)memberInfo).PropertyType;
                  break;
               case MemberTypes.Method:
                  type = ((MethodInfo)memberInfo).ReturnType;
                  break;
               case MemberTypes.Constructor:
                  type = ((ConstructorInfo)memberInfo).ReflectedType;
                  break;
               case MemberTypes.Event:
                  type = ((EventInfo)memberInfo).EventHandlerType;
                  break;
               default:
                  type = (Type)memberInfo;
                  break;
            }
         }

         return type;
      }

      /// <summary></summary>
      /// <param name="dnMemberInfo"></param>
      /// <returns></returns>
      public static Type GetType(DNMemberInfo dnMemberInfo)
      {
         Type type = null;

         if (dnMemberInfo == null)
            return null;

         if (dnMemberInfo.memberInfo != null)
            type = GetType(dnMemberInfo.memberInfo);
         else
         {
            if (dnMemberInfo.indexes != null && dnMemberInfo.parent != null) // for array elem
               type = dnMemberInfo.parent.value.GetType().GetElementType();
            else if (dnMemberInfo.dnObjectCollectionIsn != -1)
               type = DNManager.getInstance().DNObjectsCollection.GetDNType(dnMemberInfo.dnObjectCollectionIsn);
            else if (dnMemberInfo.value != null)
               type = dnMemberInfo.value.GetType();
         }

         return type;
      }

      //returns Type object by type name and assembly hash code
      public static Type GetType(int assemblyHashCode, String name)
      {
         MgAssembly mgAssembly;
         Type type = null;
         string errMessage = null;

         if (assemblies.TryGetValue(assemblyHashCode, out mgAssembly))
         {
            Assembly assembly = mgAssembly.Assembly;
            if (assembly != null) //assembly is loaded
            {
               try
               {
                  type = assembly.GetType(name);
               }
               catch (Exception e)
               {
                  errMessage = "Type " + name + " can not be loaded in assembly " + mgAssembly.FullName + ":" + e.Message;
               }
            }
            else
               errMessage = "Assembly " + mgAssembly.FullName + " can not be loaded";
         }
         else
            errMessage = string.Format("Assembly {0} with hashcode {1} not found.", name, assemblyHashCode);

         if (errMessage != null)
            Events.WriteErrorToLog(errMessage);

         return type;
      }

      /// <summary>returns the Type of the parameter</summary>
      /// <param name="parameterInfo"></param>
      /// <returns></returns>
      public static Type GetType(ParameterInfo parameterInfo)
      {
         Type type = parameterInfo.ParameterType;

         // for code snippet, if parameter is ref, type is appended by &.
         // for params, it is an array, so get the elementType
         if (type.IsByRef || IsParams(parameterInfo))
            type = type.GetElementType();

         return type;
      }

      /// <summary>this method will return member info according to the parametesit can be MethodInfo, FieldInfo,PropertyInfo, etc ...)</summary>
      /// <param name="type"></param>
      /// <param name="name"></param>
      /// <param name="isStatic"></param>
      /// <param name="hashCode"> hashcode of method, indexer or constructor , null otherwise</param>
      /// <returns></returns>
      public static MemberInfo GetMemeberInfo(Type type, String name, bool isStatic, int? hashCode)
      {
         BindingFlags bindingAttrs = (isStatic ? BindingFlags.Static : BindingFlags.Instance) | BindingFlags.Public;
         MemberInfo[] memberinfos = type.GetMember(name, bindingAttrs);
         if (hashCode != null)
         {
            foreach (var item in memberinfos)
            {
               if (GetHashCode(item) == hashCode)
                  return item;
            }

            //QCR #233349 may be the name is property of the type's interface ancestors, in this case
            //we can not always trust reflection to find us correct member
            memberinfos = MgDotNet.DotNetUtils.GetPotentialMemeberInfos(type, name, bindingAttrs, false);
            foreach (var item in memberinfos)
            {
               if (GetHashCode(item) == hashCode)
                  return item;
            }

            throw new ApplicationException(string.Format("There is no member '{0}' in type '{1}' that matches its signature", name, type));
         }
         else if (memberinfos.Length == 0)
            throw new ApplicationException(string.Format("Member '{0}' was not found in type '{1}'", name, type));
         else if (memberinfos.Length > 1)
            throw new ApplicationException(string.Format("Ambiguous member '{0}' in type '{1}'", name, type));
         else
            return memberinfos[0];
      }

      /// <summary>this method will return method info according to methodname and its parameters</summary>
      /// <param name="type"></param>
      /// <param name="methodName"></param>
      /// <param name="parameter types"></param>
      /// <param name="isConstructor">whether method is a constructor or not</param>
      /// <returns></returns>
      public static MemberInfo GetMethodInfo(Type type, String methodName, Type[] parameterTypes, bool isConstructor)
      {
         MemberInfo memberInfo = null;
         bool isStatic = false;

         if (isConstructor)
            memberInfo = type.GetConstructor(parameterTypes);
         else
         {
            memberInfo = type.GetMethod(methodName, parameterTypes); // return public and public static                
            isStatic = ((MethodInfo)memberInfo).IsStatic;
         }

         return GetMemeberInfo(type, methodName, isStatic, GetHashCode(memberInfo));
      }

      /// <summary>cast object from one type to an other</summary>
      /// <param name="o"></param>
      /// <param name="type"></param>
      /// <returns></returns>
      public static Object DynCast(Object o, Type type)
      {
         if (o == null)
         {
#if PocketPC
            return Convert.ChangeType(o, type, null); //TODO implement IFormatProvider for third parameter
#else
            return Convert.ChangeType(o, type);
#endif
         }

         //support enum conversion
         if (type.IsEnum && o.GetType().IsPrimitive)
         {
            Type underlyingType = Enum.GetUnderlyingType(type);
            if (underlyingType != o.GetType())
               o = DynCast(o, underlyingType);
            return Enum.ToObject(type, o);
         }

         // 1. same type
         // 2. type is in the inheritance hierarchy of 'o'
         // 3. type is an interface that 'o' implements
         else if (type.IsAssignableFrom(o.GetType()))
            return o;

         // implicit operator cast (if any)
         Object castedObj = ReflectionServices.ImplicitExplicitConvert(o, type, false);
         if (castedObj != null)
            return castedObj;

         castedObj = ReflectionServices.ImplicitExplicitConvert(o, type, true);
         if (castedObj != null)
            return castedObj;

         // common language runtime types (Boolean, SByte, Byte, Int16, UInt16, Int32, UInt32, Int64, UInt64, Single, 
         // Double, Decimal, DateTime, Char, and String) implements IConvertible for conversion.
         if (o is IConvertible)
         {
#if PocketPC
         return Convert.ChangeType(o, type, null); //TODO implement IFormatProvider for third parameter
#else
            return Convert.ChangeType(o, type);
#endif
         }
         else
            throw new Exception(string.Format("Unable to cast object of type '{0}' to type '{1}'.", o.GetType().Name, type.Name));
      }

      /// <summary>implicit operator cast for object 'o' to type</summary>
      /// <param name="o"></param>
      /// <param name="type"></param>
      /// <returns></returns>
      internal static Object ImplicitExplicitConvert(Object o, Type type, bool isExplicit)
      {
         MethodInfo[] methodinfos = o.GetType().GetMethods();
         String name = isExplicit ? "op_Explicit" : "op_Implicit";
         foreach (MethodInfo methodinfo in methodinfos)
            if (methodinfo.Name.Equals(name) && methodinfo.ReturnType.Equals(type))
               return InvokeMethod(methodinfo, null, new object[] { o }, false);

         return null;
      }

      /// <summary>checks if type1 is assignable from type2</summary>
      /// <param name="type1"></param>
      /// <param name="type2"></param>
      /// <returns></returns>
      public static bool IsAssignableFrom(Type type1, Type type2)
      {
         if (type1 != null && type2 != null)
            return type1.IsAssignableFrom(type2);
         return false;
      }

      /// <summary>
      /// return true if an object should be invoked on the different thread
      /// </summary>
      /// <param name="o"></param>
      /// <param name="type"></param>
      /// <param name="alwaysGuiThread">true, if we ahould always execute this in the GUI thread</param>
      /// <returns></returns>
      static bool InvokeRequired(Object o, Type type, bool alwaysGuiThread)
      {
         if (alwaysGuiThread)
            return !Misc.IsGuiThread();
         else
            return InvokeRequired(o, type);
      }

      /// <summary>return true if an object should be invoked on the different thread</summary>
      /// <param name="o"></param>
      /// <returns></returns>
      static bool InvokeRequired(Object o, Type type)
      {
         //if we already in gui thread - no need for additional checks
         if (Misc.IsGuiThread())
            return false;

         if (o is Control && ((Control)o).InvokeRequired)
            return true;
         else
         {
            if (isGuiThreadExecution(type))
               return true;

            while (type != typeof(Object) && type != null)
            {
               if (type.Namespace != null && (type.Namespace.Equals("System.Windows.Forms") ||
                   type.Namespace.Equals("System.Drawing") ||  type.Namespace.Equals("System.ComponentModel"))) //this object should be processed in GUI thread
               {
                  return !Misc.IsGuiThread();
               }
               type = type.BaseType;
            }
         }
         return false;
      }

      /// <summary>Get field</summary>
      /// <param name="fieldInfo"></param>
      /// <param name="o"></param>
      /// <returns></returns>
      public static Object GetFieldValue(FieldInfo fieldInfo, Object o)
      {
         if (InvokeRequired(o, fieldInfo.ReflectedType))
         {
            GuiInteractive guiUtils = new GuiInteractive();
            return guiUtils.ReflectionInvoke(fieldInfo, o, null);
         }
         else
            return fieldInfo.GetValue(o);
      }

      /// <summary>Set field</summary>
      /// <param name="fieldInfo"></param>
      /// <param name="o"></param>
      /// <param name="value"></param>
      public static void SetFieldValue(FieldInfo fieldInfo, Object o, Object value)
      {
         Object castedValue = DynCast(value, fieldInfo.FieldType);

         if (InvokeRequired(o, fieldInfo.ReflectedType))
         {
            GuiInteractive guiUtils = new GuiInteractive();
            guiUtils.ReflectionSet(fieldInfo, o, null, castedValue);
         }
         else
            fieldInfo.SetValue(o, castedValue);
      }

      /// <summary>Get Property</summary>
      /// <param name="propInfo"></param>
      /// <param name="o"></param>
      /// <param name="index"></param>
      /// <returns></returns>
      public static Object GetPropertyValue(PropertyInfo propInfo, Object o, object[] index)
      {
         try
         {
            if (InvokeRequired(o, propInfo.ReflectedType))
            {
               GuiInteractive guiUtils = new GuiInteractive();
               return guiUtils.ReflectionInvoke(propInfo, o, index);
            }
            else
               return propInfo.GetValue(o, index);
         }
         catch (Exception e)
         {
            if (e is TargetInvocationException && e.InnerException != null)
               throw e.InnerException;
            else
               throw e;
         }
      }

      /// <summary>
      /// Function for getting the property value from control.
      /// </summary>
      /// <param name="propInfo"></param>
      /// <param name="guiMgControl"></param>
      /// <param name="index"></param>
      /// <returns>property value</returns>
      internal static Object GetPropertyValue(PropertyInfo propInfo, GuiMgControl guiMgControl, object[] index)
      {
         //get the dot net control
         Control dotNetControl = (Control)ControlsMap.getInstance().object2Widget(guiMgControl);
         return GetPropertyValue(propInfo, dotNetControl, index);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="propInfo"></param>
      /// <param name="guiMgControl"></param>
      /// <param name="index"></param>
      /// <param name="value"></param>
      public static void SetPropertyValue(PropertyInfo propInfo, GuiMgControl guiMgControl, object[] index, Object value)
      {
         GuiInteractive guiUtils = new GuiInteractive();
         guiUtils.ReflectionSet(propInfo, guiMgControl, index, value);
      }

      /// <summary>Set Property</summary>
      /// <param name="propInfo"></param>
      /// <param name="o"></param>
      /// <param name="index"></param>
      /// <param name="value"></param>
      public static void SetPropertyValue(PropertyInfo propInfo, Object o, object[] index, Object value)
      {
         try
         {
            Object castedValue = DynCast(value, propInfo.PropertyType);

            if (InvokeRequired(o, propInfo.ReflectedType))
            {
               GuiInteractive guiUtils = new GuiInteractive();
               guiUtils.ReflectionSet(propInfo, o, index, castedValue);
            }
            else
               propInfo.SetValue(o, castedValue, index);
         }
         catch (Exception e)
         {
            if (e is TargetInvocationException && e.InnerException != null)
               throw e.InnerException;
            else
               throw e;
         }
      }

      /// <summary>call method</summary>
      /// <param name="methodInfo"></param>
      /// <param name="value"></param>
      /// <param name="parameters"></param>
      /// <param name="alwaysGuiThread">if true,executed in GUI thread</param>
      /// <returns></returns>
      public static Object InvokeMethod(MethodBase methodInfo, Object obj, Object[] parameters, bool alwaysGuiThread)
      {
         try
         {
            if (InvokeRequired(obj, methodInfo.ReflectedType, alwaysGuiThread))
            {
               GuiInteractive guiUtils = new GuiInteractive();
               return guiUtils.ReflectionInvoke(methodInfo, obj, parameters);
            }
            else
               return methodInfo.Invoke(obj, parameters);
         }
         catch (Exception e)
         {
            if (e is TargetInvocationException && e.InnerException != null)
               throw e.InnerException;
            else
               throw e;
         }
      }

      /// <summary>invoke snippet method</summary>
      /// <param name="assemblyHashCode"></param>
      /// <param name="methodInfo"></param>
      /// <param name="parameters"></param>
      /// <returns></returns>
      public static Object InvokeSnippetMethod(int assemblyHashCode, MethodBase methodInfo, Object[] parameters)
      {
         try
         {
            if (IsGuiThreadExecution(assemblyHashCode))
            {
               GuiInteractive guiUtils = new GuiInteractive();
               return guiUtils.ReflectionInvoke(methodInfo, null, parameters);
            }
            else
               return methodInfo.Invoke(null, parameters);
         }
         catch (Exception e)
         {
            if (e is TargetInvocationException && e.InnerException != null)
               throw e.InnerException;
            else
               throw e;
         }
      }

      /// <summary>get array element</summary>
      /// <param name="array"></param>
      /// <param name="indices"></param>
      /// <returns></returns>
      public static Object GetArrayElement(Array array, params int[] indices)
      {
         return array.GetValue(indices);
      }

      /// <summary>set array element</summary>
      /// <param name="array"></param>
      /// <param name="value"></param>
      /// <param name="indices"></param>
      public static void SetArrayElement(Array array, Object value, int[] indices)
      {
         array.SetValue(value, indices);
      }

      /// <summary>create an array</summary>
      /// <param name="arrayElement"> type of array element</param>
      /// <param name="length"> ranks of the array</param>
      /// <returns></returns>
      public static Array CreateArrayInstance(Type arrayElement, int[] length)
      {
         return (Array.CreateInstance(arrayElement, length));
      }

      /// <summary>return parameters info of method/constructor</summary>
      /// <param name="method"></param>
      /// <returns></returns>
      public static ParameterInfo[] GetParameters(MethodBase method)
      {
         return method.GetParameters();
      }

      /// <summary>return parameters info of an indexer</summary>
      /// <param name="propInfo"></param>
      /// <returns></returns>
      public static ParameterInfo[] GetIndexParameters(PropertyInfo propInfo)
      {
         return propInfo.GetIndexParameters();
      }

      /// <summary>create a type</summary>
      /// <param name="type"></param>
      /// <param name="constructorInfo"></param>
      /// <param name="parameters"></param>
      /// <returns></returns>
      public static Object CreateInstance(Type type, ConstructorInfo constructorInfo, Object[] parameters)
      {
         try
         {
            if (InvokeRequired(null, type))
            {
               GuiInteractive guiUtils = new GuiInteractive();
               return guiUtils.ReflectionInvoke(constructorInfo, type, parameters, (constructorInfo == null));
            }
            else
            {
               if (constructorInfo != null)
                  return constructorInfo.Invoke(parameters);
               else //default constructor
                  return type.Assembly.CreateInstance(type.FullName);
            }
         }
         catch (Exception e)
         {
            if (e is TargetInvocationException && e.InnerException != null)
               throw e.InnerException;
            else
               throw e;
         }
      }

      /// <summary>
      /// check if the parameter is Param
      /// </summary>
      /// <param name="paramInfo"></param>
      /// <returns></returns>
      public static bool IsParams(ParameterInfo paramInfo)
      {
         return paramInfo.GetCustomAttributes(typeof(ParamArrayAttribute), false).Length > 0;
      }

      /// <summary>add handler for the .NET object:
      /// create a dynamic delegate according to the given event type
      /// add method body to the delegate
      /// in the method body call DefaultDotNetHandler which will handle the event</summary>
      /// <param name="eventName"> name of the event</param>
      /// <param name="objectToHook"> object to hook</param>
      /// <param name="objectEvents"> once registered, events are added to this collection</param>
      /// <param name="reportErrors"> indicates whether to log errors while hooking event. There are some  
      /// default events that are hooked for each .NET control (see DNControlEvents._standardControlEvents). 
      /// Errors occurring while hooking these events should not be reported.</param>
      /// TODO handle static events
      internal static void addHandler(String eventName, Object objectToHook, DNObjectEventsCollection.ObjectEvents objectEvents, bool reportErrors)
      {
         //subscribe for events only once , check if handler already added
         if (objectEvents.delegateExist(eventName))
            return;

         Type type = objectToHook.GetType();

         // Get an EventInfo representing the  event, and get the type of delegate that handles the event.
         EventInfo eventInfo = type.GetEvent(eventName);
         if (eventInfo != null)
         {
            // Get the "add" accessor of the event and invoke it late-bound, passing in the delegate instance. 
            // This is equivalent to using the += operator in C#, or AddHandler in Visual Basic. 
            // The instance on which the "add" accessor is invoked is the form; 
            // the arguments must be passed as an array.
            MethodInfo addHandler = eventInfo.GetAddMethod();

            Delegate eventHandler = null;

            try
            {
#if !PocketPC
               eventHandler = createDynamicDelegate(objectToHook.GetHashCode(), eventInfo);
#else
            if (objectToHook is Control)
               eventHandler = DNControlEvents.getPredefinedEvent(eventName);
#endif
               try
               {
                  addHandler.Invoke(objectToHook, new Object[] { eventHandler });
               }
               catch (Exception e)
               {
                  if (e is TargetInvocationException && e.InnerException != null)
                     throw e.InnerException;
                  else
                     throw e;
               }

               objectEvents.add(eventName, eventHandler);
            }
            catch (Exception exception)
            {
               if (reportErrors)
               {
                  DNException dnException = Manager.GetCurrentRuntimeContext().DNException;
                  dnException.set(exception);
               }
            }
         }
         else
         {
            if (reportErrors)
               Events.WriteExceptionToLog(String.Format("Event type \"{0}\" not supported", eventName));
         }
      }

      /// <summary>remove handler</summary>
      /// <param name="o"></param>
      /// <param name="eventName"></param>
      /// <param name="handler"></param>
      public static void removeHandler(Object o, String eventName, Delegate eventHandler)
      {
         Type type = o.GetType();

         // Get an EventInfo representing the  event, and get the
         // type of delegate that handles the event.
         //
         EventInfo eventInfo = type.GetEvent(eventName);
         if (eventInfo != null)
         {
            MethodInfo removeHandler = eventInfo.GetRemoveMethod();
            try
            {
               removeHandler.Invoke(o, new Object[] { eventHandler });
            }
            catch (Exception e)
            {
               if (e is TargetInvocationException && e.InnerException != null)
                  throw e.InnerException;
               else
                  throw e;
            }
         }
      }

#if !PocketPC
      /// <summary> Remove the ValueChangedHandler. </summary>
      /// <param name="control"></param>
      internal static void RemoveDNControlValueChangedHandler(Control control)
      {
         Debug.Assert(Misc.IsGuiThread());

         DNObjectEventsCollection.ObjectEvents objectEvents = DNManager.getInstance().DNObjectEventsCollection.getObjectEvents(control);

         Debug.Assert(objectEvents != null);

         if (!String.IsNullOrEmpty(objectEvents.DNControlValueChangedEventName))
            removeHandler(control, objectEvents.DNControlValueChangedEventName, objectEvents.DNControlValueChangedDelegate);
      }

      /// <summary> Add the ValueChangedHandler. </summary>
      /// <param name="control"></param>
      internal static void AddDNControlValueChangedHandler(Control control)
      {
         Debug.Assert(Misc.IsGuiThread());

         DNObjectEventsCollection.ObjectEvents objectEvents = DNManager.getInstance().DNObjectEventsCollection.getObjectEvents(control);

         Debug.Assert(objectEvents != null);

         if (!String.IsNullOrEmpty(objectEvents.DNControlValueChangedEventName))
            AddDNControlValueChangedHandler(control, objectEvents.DNControlValueChangedEventName);
      }

      /// <summary>
      //This function registers "HandleDNControlValueChanged" to the event 
      //specified by the user for DN control property change.
      /// </summary>
      /// <param name="eventName">Event to be registered</param>
      /// <param name="obj">object on which, event will be registered</param>
      internal static void AddDNControlValueChangedHandler(object objectToHook, String eventName)
      {
         Control ctrl = (Control)objectToHook;

         DNObjectEventsCollection.ObjectEvents objectEvents = DNManager.getInstance().DNObjectEventsCollection.checkAndCreateObjectEvents(ctrl);

         //DNControlValueChangedDelegate and DNControlValueChangedEventName should be initialized only once. If initialized second time, assert.
         //Debug.Assert(objectEvents.DNControlValueChangedDelegate == null && objectEvents.DNControlValueChangedEventName == null);
         
         Type type = ctrl.GetType();

         //Get an EventInfo representing the  event, and get the type of delegate that handles the event.
         EventInfo eventInfo = type.GetEvent(eventName);
         if (eventInfo != null)
         {
            // Get the "add" accessor of the event and invoke it late-bound, passing in the delegate instance. 
            // This is equivalent to using the += operator in C#, or AddHandler in Visual Basic. 
            // The instance on which the "add" accessor is invoked is the form; 
            // the arguments must be passed as an array.
            MethodInfo addHandler = eventInfo.GetAddMethod();

            Delegate eventHandler = null;

            try
            {
               if (objectEvents.DNControlValueChangedDelegate != null)
               {
                  Debug.Assert(objectEvents.DNControlValueChangedEventName == eventName);
                  eventHandler = objectEvents.DNControlValueChangedDelegate;
               }
               else
               {
                  eventHandler = createDynamicDelegateForDNCtrlValueChangedEvent(ctrl.GetHashCode(), eventInfo);

                  //Add delegate(event handler) to object events.
                  objectEvents.DNControlValueChangedDelegate = eventHandler;
                  //Add event name to object events.
                  objectEvents.DNControlValueChangedEventName = eventName;
               }

               try
               {
                  addHandler.Invoke(objectToHook, new Object[] { eventHandler });
               }
               catch (Exception e)
               {
                  if (e is TargetInvocationException && e.InnerException != null)
                     throw e.InnerException;
                  else
                     throw e;
               }
            }
            catch (Exception exception)
            {
               DNException dnException = Manager.GetCurrentRuntimeContext().DNException;
               dnException.set(exception);
            }
         }
         else
         {
            Events.WriteExceptionToLog(String.Format("Event type \"{0}\" not supported", eventName));
         }
      }

      /// <summary>create a dynamic delegate according to the given event type;
      /// add method body to the delegate</summary>
      /// <param name="key"></param>
      /// <param name="eventInfo"></param>
      /// <returns></returns>
      private static Delegate createDynamicDelegateForDNCtrlValueChangedEvent(int hashCode, EventInfo eventInfo)
      {
          Type tDelegate = eventInfo.EventHandlerType;
         // Event handler methods can also be generated at run time,
         // using lightweight dynamic methods and Reflection.Emit. 
         // To construct an event handler, you need the return type
         // and parameter types of the delegate. These can be obtained
         // by examining the delegate's Invoke method. 
         //
         // It is not necessary to name dynamic methods, so the empty 
         // string can be used. The last argument associates the 
         // dynamic method with the current type, giving the delegate
         // access to all the public and private members of Example,
         // as if it were an instance method.
         //
         Type returnType = GetDelegateReturnType(tDelegate);
         if (returnType != typeof(void))
            throw new ApplicationException("Delegate has a return type.");
         
         Type[] argsTypes = GetDelegateParameterTypes(tDelegate);
         DynamicMethod handler =
             new DynamicMethod("",
                               null,
                               argsTypes);

          // Generate a method body. 
         ILGenerator ilgen = handler.GetILGenerator();

          //get method info of the requested handler
         MethodInfo defaultDotNetHandler = typeof(DotNetHandler).GetMethod("HandleDNControlValueChanged");
         
         //add object hashcode argument.
         ilgen.Emit(OpCodes.Ldc_I4, hashCode);

         //call defaultDotNetHandler with all the arguments
         ilgen.Emit(OpCodes.Call, defaultDotNetHandler);
         ilgen.Emit(OpCodes.Ret);

         // Complete the dynamic method by calling its CreateDelegate method. 
         // Use the "add" accessor to add the delegate to the invocation list for the event.
         return handler.CreateDelegate(tDelegate);
      }
      
      /// <summary>create a dynamic delegate according to the given event type;
      /// add method body to the delegate</summary>
      /// <param name="key"></param>
      /// <param name="eventInfo"></param>
      /// <returns></returns>
      private static Delegate createDynamicDelegate(int hashCode, EventInfo eventInfo)
      {
         Type tDelegate = eventInfo.EventHandlerType;
         // Event handler methods can also be generated at run time,
         // using lightweight dynamic methods and Reflection.Emit. 
         // To construct an event handler, you need the return type
         // and parameter types of the delegate. These can be obtained
         // by examining the delegate's Invoke method. 
         //
         // It is not necessary to name dynamic methods, so the empty 
         // string can be used. The last argument associates the 
         // dynamic method with the current type, giving the delegate
         // access to all the public and private members of Example,
         // as if it were an instance method.
         //
         Type returnType = GetDelegateReturnType(tDelegate);
         if (returnType != typeof(void))
            throw new ApplicationException("Delegate has a return type.");

         Type[] argsTypes = GetDelegateParameterTypes(tDelegate);
         DynamicMethod handler =
             new DynamicMethod("",
                               null,
                               argsTypes);
        
         // Generate a method body. 
         ILGenerator ilgen = handler.GetILGenerator();

         //get method info of the requested handler
         MethodInfo defaultDotNetHandler = typeof(DotNetHandler).GetMethod("handleDotNetEvent");

         int argsNumber = argsTypes.Length;

         //create array of object[] to put the handler arguments in
         LocalBuilder tempObjArrLB = ilgen.DeclareLocal(typeof(Object[]));
         ilgen.Emit(OpCodes.Ldc_I4, argsNumber);
         ilgen.Emit(OpCodes.Newarr, typeof(Object));
         ilgen.Emit(OpCodes.Stloc, tempObjArrLB);

         //put all handler arguments into the array
         for (int i = 0; i < argsNumber; i++)
         {
            ilgen.Emit(OpCodes.Ldloc, tempObjArrLB);
            ilgen.Emit(OpCodes.Ldc_I4, i);
            ilgen.Emit(OpCodes.Ldarg, i);

            //QCR #996853, handle byref arguments
            if (argsTypes[i].IsByRef)
               AddByRefArgument(ilgen, argsTypes[i].GetElementType());

            if (argsTypes[i].IsValueType) //QCR #988437, value types must be boxed
               ilgen.Emit(OpCodes.Box, argsTypes[i]);
            ilgen.Emit(OpCodes.Stelem_Ref);
         }

         //call defaultDotNetHandler with all the arguments
         ilgen.Emit(OpCodes.Ldc_I4, hashCode);      //add object hashcode argument
         ilgen.Emit(OpCodes.Ldstr, eventInfo.Name); //add event name argument
         ilgen.Emit(OpCodes.Ldloc, tempObjArrLB);   //add array of parameters argument
         ilgen.Emit(OpCodes.Call, defaultDotNetHandler);
         ilgen.Emit(OpCodes.Ret);

         // Complete the dynamic method by calling its CreateDelegate method. 
         // Use the "add" accessor to add the delegate to the invocation list for the event.
         return handler.CreateDelegate(tDelegate);
      }

      /// <summary>
      /// add argument by ref
      /// </summary>
      /// <param name="ilgen"></param>
      /// <param name="type"></param>
      private static void AddByRefArgument(ILGenerator ilgen, Type type)
      {
         if (type.IsValueType)
         {
            if (type.IsEnum)
            {
               AddByRefArgument(ilgen, Enum.GetUnderlyingType(type));
               return;
            }
            if (type.IsPrimitive)
            {
               OpCode opcode;
               typeOpCodes.TryGetValue(type, out opcode);
               Debug.Assert(opcode != null);
               ilgen.Emit(opcode);
            }
            else
               ilgen.Emit(OpCodes.Ldobj, type);
            ilgen.Emit(OpCodes.Box, type);
         }
         else
            ilgen.Emit(OpCodes.Ldind_Ref);
      }

      /// <summary>return parameters of the delegate</summary>
      /// <param name="d"></param>
      /// <returns></returns>
      static private Type[] GetDelegateParameterTypes(Type d)
      {
         if (d.BaseType != typeof(MulticastDelegate))
            throw new ApplicationException("Not a delegate.");

         MethodInfo invoke = d.GetMethod("Invoke");
         if (invoke == null)
            throw new ApplicationException("Not a delegate.");

         ParameterInfo[] parameters = invoke.GetParameters();
         Type[] typeParameters = new Type[parameters.Length];
         for (int i = 0; i < parameters.Length; i++)
         {
            typeParameters[i] = parameters[i].ParameterType;
         }

         return typeParameters;
      }

      /// <summary>get return type of the delegate</summary>
      /// <param name="d"></param>
      /// <returns></returns>
      static private Type GetDelegateReturnType(Type d)
      {
         if (d.BaseType != typeof(MulticastDelegate))
            throw new ApplicationException("Not a delegate.");

         MethodInfo invoke = d.GetMethod("Invoke"); 
         if (invoke == null)
            throw new ApplicationException("Not a delegate.");

         return invoke.ReturnType;
      }
#endif
   }
}
