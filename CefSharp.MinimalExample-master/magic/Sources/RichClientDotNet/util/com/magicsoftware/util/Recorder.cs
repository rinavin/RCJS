using System;
using System.Collections.Generic;
using System.Text;

namespace util.com.magicsoftware.util
{
   /// <summary>
   /// this is a recorder that allows to record data
   /// Will be used for unit test
   /// </summary>
   /// <typeparam name="T"></typeparam>
   public abstract class RecorderBase<T>
   {
      /// <summary>
      /// file to record the data to
      /// </summary>
      public virtual string FileName { get; set; }

      /// <summary>
      /// true while recording
      /// </summary>
      public bool Recording { get; set; }

      /// <summary>
      /// start recording
      /// </summary>
      public void StartRecording()
      {
         Recording = true;
      }

      /// <summary>
      /// stop recording
      /// </summary>
      public void StopRecording()
      {
         Recording = false;
      }

      /// <summary>
      /// record data
      /// </summary>
      /// <param name="t"></param>
      public void Record(T t)
      {
         if (Recording)
            Add(t);
      }

      /// <summary>
      /// add data to the recorded data
      /// </summary>
      /// <param name="t"></param>
      protected abstract void Add(T t);

      /// <summary>
      /// saves the recorded data
      /// </summary>
      public abstract void Save();

      /// <summary>
      /// loads recorded data
      /// </summary>
      /// <returns></returns>
      public abstract Object Load();
 
   }
}
