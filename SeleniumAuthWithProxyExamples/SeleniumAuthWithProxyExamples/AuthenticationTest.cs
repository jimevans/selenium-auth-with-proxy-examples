using System;
using System.Collections.Generic;
using System.IO;
using BenderProxy;

namespace SeleniumAuthWithProxyExamples
{
    /// <summary>
    /// Abstract class representing an authentication test for an authentication scheme.
    /// </summary>
    public abstract class AuthenticationTest
    {

        /// <summary>
        /// Initializes a new instances of the <see cref="AuthenticationTest"/> class.
        /// </summary>
        /// <param name="browserKind">The browser against which to execute the test.</param>
        protected AuthenticationTest(BrowserKind browserKind)
        {
            this.BrowserKind = browserKind;
        }

        /// <summary>
        /// Gets the browser against which to execute the test.
        /// </summary>
        public BrowserKind BrowserKind { get; }

        /// <summary>
        /// Gets the host name for the test web application.
        /// </summary>
        public string TestWebAppHostName { get; set; } = "www.seleniumhq-test.test";

        /// <summary>
        /// Gets the port to be used by the test web application.
        /// </summary>
        public int TestWebAppPort { get; set; } = 5000;

        /// <summary>
        /// Sets up the test execution, including starting web server, browser, and proxy.
        /// </summary>
        public abstract void SetUp();

        /// <summary>
        /// Executes the test.
        /// </summary>
        public abstract void Execute();

        /// <summary>
        /// Tears down the test execution, including stopping the web server, browser, and proxy.
        /// </summary>
        public abstract void TearDown();

        /// <summary>
        /// Handles the OnResponseReceived for the proxy, which occurs after the response is
        /// received from the web server, but before it is forwarded on to the browser.
        /// </summary>
        /// <param name="context">A <see cref="BenderProxy.ProcessingContext"/> object.</param>
        public abstract void OnResponseReceived(ProcessingContext context);

        /// <summary>
        /// Utility method for reading the contents of a stream.
        /// </summary>
        /// <param name="reader">A <see cref="StreamReader"/> used to read from a stream.</param>
        /// <returns>The string the stream.</returns>
        protected string ReadFromStream(StreamReader reader)
        {
            List<char> totalResponse = new List<char>();
            bool continueReading = true;
            int bufferSize = 8192;
            int totalBytes = 0;
            while (continueReading)
            {
                char[] buffer = new char[bufferSize];
                int bytesRead = reader.Read(buffer, 0, bufferSize);
                if (bytesRead >= 0)
                {
                    totalResponse.AddRange(buffer);
                    totalBytes += bytesRead;
                }

                if (bytesRead < bufferSize)
                {
                    continueReading = false;
                }
            }

            string content = new string(totalResponse.ToArray(), 0, totalBytes);
            return content;
        }

        /// <summary>
        /// Utility method to find the expected authentication header to use when multiple headers are returned.
        /// </summary>
        /// <param name="authHeader">The combined value of all WWW-Authenticate headers.</param>
        /// <param name="expectedAuthScheme">The expected authentication scheme.</param>
        /// <returns>The header for the specified scheme, if one exists</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the expected authentication scheme is not in the list of headers supplied.
        /// </exception>
        protected string GetAuthenticationHeader(string authHeader, string expectedAuthScheme)
        {
            if (!authHeader.Contains(expectedAuthScheme))
            {
                string normalizedAuthHeader = authHeader.Replace("\r\n", ", ");
                throw new InvalidOperationException(string.Format("Could not find expected authentication scheme '{0}' in WWW-Authenticate header ('{1}')", expectedAuthScheme, normalizedAuthHeader));
            }

            string[] authHeaders = authHeader.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
            foreach (string individualHeader in authHeaders)
            {
                if (individualHeader.StartsWith(expectedAuthScheme))
                {
                    return individualHeader;
                }
            }

            return string.Empty;
        }
    }
}
