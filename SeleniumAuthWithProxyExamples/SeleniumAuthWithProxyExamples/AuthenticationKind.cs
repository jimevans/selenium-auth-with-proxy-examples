using System;
using System.Collections.Generic;
using System.Text;

namespace SeleniumAuthWithProxyExamples
{
    /// <summary>
    /// Represents the types of authentication supported
    /// </summary>
    public enum AuthenticationKind
    {
        /// <summary>
        /// HTTP Basic Authentication
        /// </summary>
        Basic,

        /// <summary>
        /// HTTP Digest Authentication
        /// </summary>
        Digest,

        /// <summary>
        /// NTLM Authentication
        /// </summary>
        Ntlm
    }
}
