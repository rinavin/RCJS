using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace com.magicsoftware.gatewaytypes
{

   public class DbPos
   {
      byte[] pos;

      public byte[] Pos
      {
         get { return pos; }
         set { pos = value; }
      }
      bool isZero;



      /// <summary>
      /// set zero
      /// </summary>
      /// <param name="initializeToZero"></param>
      public DbPos(bool initializeToZero)
      {
         if (initializeToZero)
            SetZero();
      }

      public DbPos() { }

      /// <summary>
      /// clone this object
      /// </summary>
      /// <returns></returns>
      public DbPos Clone()
      {
         DbPos dbPos = new DbPos(isZero);
         if (!IsZero)
            dbPos.Set(pos);
         return dbPos;
      }

      /// <summary>
      ///  GetPos
      /// </summary>
      public byte[] Get()
      {
         return pos;
      }

      /// <summary>
      ///  Check isZero.
      /// </summary>
      [XmlAttribute]
      public bool IsZero
      {
         get
         {
            return isZero;
         }
         //for serializer only
         set { isZero = value; }
      }

      /// <summary>
      ///  SetZero().
      /// </summary>
      public void SetZero()
      {
         isZero = true;
      }

      /// <summary>
      ///  Copy byetes into pos
      /// </summary>
      /// <param name="value"></param>
      public void Set(byte[] value)
      {
         pos = new byte[value.Length];
         for (int i = 0; i < value.Length; i++)
         {
            pos[i] = value[i];
         }
         isZero = false;
      }

      /// <summary>
      ///  Allocate bytes of length.
      /// </summary>
      /// <param name="length"></param>
      public void Alloc(int length)
      {
         pos = new byte[length];

         isZero = true;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="obj"></param>
      /// <returns></returns>
      public override bool Equals(object obj)
      {
         DbPos dbPos = (DbPos)obj;
         if (base.Equals(obj))
            return true;

         if (isZero != dbPos.isZero)
            return false;

         if (isZero) //both positions are zero
            return true;

         if (pos.Length != dbPos.pos.Length)
            return false;

         for (int i = 0; i < pos.Length; i++)
            if (pos[i] != dbPos.pos[i])
               return false;

         return true;
      }

      public override int GetHashCode()
      {
         return base.GetHashCode();
      }
      
   }
}
