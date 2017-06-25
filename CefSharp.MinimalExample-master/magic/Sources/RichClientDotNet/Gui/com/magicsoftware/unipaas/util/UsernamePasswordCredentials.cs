using System;
using System.Text;

namespace com.magicsoftware.unipaas.util
{
   /// <summary>
   /// 
   /// </summary>
   public class UsernamePasswordCredentials
   {
      public UsernamePasswordCredentials()
      {
      }

      public UsernamePasswordCredentials(String userName, String password)
      {
         if (userName == null)
         {
            throw new ArgumentException("Username may not be null");
         }
         Username = userName;
         Password = password;
      }

      public String Username { get; set; }
      public String Password { get; set; }

      /// <summary> Get this object string.
      /// 
      /// </summary>
      /// <returns> the username:password formed string
      /// </returns>
      public override String ToString()
      {
         var result = new StringBuilder();
         result.Append(Username);
         result.Append(":");
         result.Append(Password ?? "null");
         return result.ToString();
      }
   }

   public class ChangePasswordCredentials : UsernamePasswordCredentials
   {
      public String NewUserName { get; set; }
      public String NewPassword { get; set; }

      public ChangePasswordCredentials()
      {
      }
      
      public ChangePasswordCredentials(String oldUserName, String newUserName,
                                       String password)
         : base(oldUserName, password)
      {
         if (newUserName == null)
         {
            throw new ArgumentException("Username may not be null");
         }
         this.NewUserName = newUserName;
      }

      public override string ToString()
      {
         var result = new StringBuilder();
         result.Append(Username);
         result.Append(":");
         result.Append(NewUserName);
         result.Append(":");
         result.Append(Password ?? "null");

         return result.ToString();
      }
   }
}