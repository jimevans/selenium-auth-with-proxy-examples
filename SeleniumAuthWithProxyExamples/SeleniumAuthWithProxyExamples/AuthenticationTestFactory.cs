using System;
using System.Collections.Generic;
using System.Text;

namespace SeleniumAuthWithProxyExamples
{
    /// <summary>
    /// A static factory class for creating test instances.
    /// </summary>
    public static class AuthenticationTestFactory
    {
        public static AuthenticationTest CreateTest(AuthenticationKind testType, BrowserKind browser)
        {
            switch (testType)
            {
                case AuthenticationKind.Basic:
                    return new BasicAuthenticationTest(browser);

                case AuthenticationKind.Digest:
                    return new DigestAuthenticationTest(browser);

                default:
                    return new NtlmAuthenticationTest(browser);
            }
        }
    }
}
