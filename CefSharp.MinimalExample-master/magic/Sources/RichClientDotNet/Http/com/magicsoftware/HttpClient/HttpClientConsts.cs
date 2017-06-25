using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.httpclient.utils
{

    public class HttpClientConsts
    {
        /// <summary> This class is used to define some HTTP client constants. 
        /// Rules: 
        /// 1. Any class that need one or more of these constants must implement this interface 
        ///    or access the constant directly using the interface name as its prefix.
        /// 2. Please define the constants using UPPERCASE letters to make them stand out clearly.</summary>
        

        // execution properties
        public const string HTTP_COMPRESSION_LEVEL = "HTTPCompressionLevel";
        internal const string HTTP_COMPRESSION_LEVEL_NONE = "NONE";
        internal const string HTTP_COMPRESSION_LEVEL_MINIMUM = "MINIMUM";
        internal const string HTTP_COMPRESSION_LEVEL_NORMAL = "NORMAL";
        internal const string HTTP_COMPRESSION_LEVEL_MAXIMUM = "MAXIMUM";

        internal const string RSA_COOKIES = "RSACookies";
        internal const string HTTP_EXPECT100CONTINUE = "Expect100Continue"; // Add HTTP header "Expect:100Continue"
        internal const string USE_HIGHEST_SECURITY_PROTOCOL = "UseHighestSecurityProtocol"; //This property decides to use TLS v1.2 (implemented at .NET v4.5) or TLS v1.0

        /// <summary>Authentication dialog</summary>
        internal const string PROXY_AUTH_CAPTION = "Proxy Server Authentication";
        internal const string WEB_AUTH_CAPTION = "Web Server Authentication";
    }
}
