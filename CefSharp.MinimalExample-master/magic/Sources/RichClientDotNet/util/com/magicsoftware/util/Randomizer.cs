using System;

namespace com.magicsoftware.util
{
   public static class Randomizer
   {
      // globals for randomize function of NUMBER
      private static bool _initialized;
      private static Double _mod;
      private static Double _mul;
      private static Double _seed;

      static Randomizer()
      {
         _initialized = false;
         _mod = 0;
         _mul = 0;
         _seed = 0;
      }

      /// <summary>
      ///   get if the randomizer initialized
      /// </summary>
      public static bool get_initialized()
      {
         return _initialized;
      }

      /// <summary>
      ///   get rand_mod for randomizer
      /// </summary>
      public static Double get_mod()
      {
         return _mod;
      }

      /// <summary>
      ///   get rand_mul for randomizer
      /// </summary>
      public static Double get_mul()
      {
         return _mul;
      }

      /// <summary>
      ///   get rand_seed for randomizer
      /// </summary>
      public static Double get_seed()
      {
         return _seed;
      }

      /// <summary>
      ///   set randomizer initialized
      /// </summary>
      public static void set_initialized()
      {
         _initialized = true;
      }

      /// <summary>
      ///   initialize rand_mod and return reference to it
      /// </summary>
      public static Double set_mod(Double randMod)
      {
         _mod = randMod;
         return _mod;
      }

      /// <summary>
      ///   initialize rand_mul and return reference to it
      /// </summary>
      public static Double set_mul(Double randMul)
      {
         _mul = randMul;
         return _mul;
      }

      /// <summary>
      ///   initialize rand_seed and return reference to it
      /// </summary>
      public static Double set_seed(Double randSeed)
      {
         _seed = randSeed;
         return _seed;
      }
   }
}
