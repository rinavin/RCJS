using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace Tests.UniUtils.TestUtils
{
   /// <summary>
   /// Test utility class to signal the invocation of methods in a test class.<para/>
   /// To use it, you should create a class and add a member of this type:<para/>
   /// <example>
   /// class MyClass {
   ///    MethodInvocationFlags&lt;MyClass&gt; invocationFlags = new MethodInvocationFlags&lt;MyClass&gt;(BindingFlags.Instance | BindingFlags.Public);
   ///    
   /// }
   /// </example>
   /// Then, you can use it within each method you want to flag as being used:
   /// <example>
   /// void MyMethod()
   /// {
   ///    ....
   ///    invocationFlags.SignalMethodAsInvoked();
   /// }
   /// </example>
   /// </summary>
   /// <typeparam name="T"></typeparam>
   internal class MethodInvocationFlags<T>
   {
      Dictionary<string, bool> invocationFlags = new Dictionary<string, bool>();

      public MethodInvocationFlags(BindingFlags methodBindingFlags)
      {
         MethodInfo[] methods = typeof(T).GetMethods(methodBindingFlags);
         foreach (var method in methods)
         {
            invocationFlags.Add(method.Name, false);
         }
      }

      public void SignalMethodWasInvoked()
      {
         StackTrace st = new StackTrace(1);
         StackFrame caller = st.GetFrame(0);
         invocationFlags[caller.GetMethod().Name] = true;
      }

      public void Reset()
      {
         List<string> keys = new List<string>(invocationFlags.Keys);
         foreach (var key in keys)
         {
            invocationFlags[key] = false;
         }
      }

      public bool MethodWasInvoked(string methodName)
      {
         return invocationFlags[methodName];
      }
   }
}
