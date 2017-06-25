using System;

namespace com.magicsoftware.unipaas.gui.low
{
   public class MgImageListCacheKey
   {
      private const int PRIME_NUMBER = 37;
      private const int SEED = 23;

      internal int numberOfImages = -1;
      internal String imageListFileName;
   
      /// <summary>
      /// 
      /// </summary>
      /// <param name="imageListFileName"></param>
      /// <param name="numberOfImages"></param>
      public MgImageListCacheKey(String imageListFileName, int numberOfImages)
      {
         this.imageListFileName = imageListFileName;
         this.numberOfImages = numberOfImages;
      }

     
      /// <summary>
      /// 
      /// </summary>
      /// <param name="imageListFileName"></param>
      public MgImageListCacheKey(String imageListFileName)
      {         
         this.imageListFileName = imageListFileName;       
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      public override int GetHashCode()
      {
         int hash = SEED;

         hash = PRIME_NUMBER * hash + (imageListFileName != null ? imageListFileName.GetHashCode() : 0);
         hash = PRIME_NUMBER * hash + numberOfImages;

         return hash;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="obj"></param>
      /// <returns></returns>
      public override bool Equals(Object obj)
      {
         if (obj != null && obj is MgImageListCacheKey)
         {
            MgImageListCacheKey otherImageListCacheKey = (MgImageListCacheKey)obj;

            if (object.ReferenceEquals(this, otherImageListCacheKey))
               return true;
            else
            {
               if ((numberOfImages == otherImageListCacheKey.numberOfImages) &&
                  ((imageListFileName == null && otherImageListCacheKey.imageListFileName == null) || (imageListFileName.Equals(otherImageListCacheKey.imageListFileName))))
                  return true;
            }
         }
         return false;
      }
   }
}