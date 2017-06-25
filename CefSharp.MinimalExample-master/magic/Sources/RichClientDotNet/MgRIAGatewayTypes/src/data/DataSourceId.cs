using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using com.magicsoftware.util;

namespace com.magicsoftware.gatewaytypes.data
{
 
 
   public class DataSourceId
   {
      [XmlAttribute]
      public int CtlIdx { get; set; }
      [XmlAttribute]
      public int Isn { get; set; }

      private const int PRIME_NUMBER = 37;
      private const int SEED = 23;

      /// <summary>
      /// CTOR
      /// </summary>
      public DataSourceId() { }

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="ctlIdx"></param>
      /// <param name="isn"></param>
      public DataSourceId(int ctlIdx, int isn)
      {
         CtlIdx = ctlIdx;
         Isn = isn;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      public override int GetHashCode()
      {
         HashCodeBuilder hashBuilder = new HashCodeBuilder(SEED, PRIME_NUMBER);
         hashBuilder.Append(CtlIdx).Append(Isn);
         return hashBuilder.HashCode;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="obj"></param>
      /// <returns></returns>
      public override bool Equals(object obj)
      {
         var other = obj as DataSourceId;
         if (other != null)
            return (this.CtlIdx == other.CtlIdx) && (this.Isn == other.Isn);
         else
            return false;
      }

      public override string ToString()
      {
         return "{DataSourceId: " + CtlIdx + "," + Isn + "}";
      }
   }
}
