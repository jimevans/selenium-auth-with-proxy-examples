using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BenderProxy;
using BenderProxy.Headers;
using BenderProxy.Readers;
using BenderProxy.Writers;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using PassedBall;
using TestWebApplication;

namespace SeleniumAuthWithProxyExamples
{
    /// <summary>
    /// Class representing a test for HTTP Digest authentication.
    /// </summary>
    public class NtlmAuthenticationTest : AuthenticationTest
    {
        private IWebHost host;
        private IWebDriver driver;
        private HttpProxyServer proxyServer;

        /// <summary>
        /// Initializes a new instances of the <see cref="BasicAuthenticationTest"/> class.
        /// </summary>
        /// <param name="browserKind">The browser against which to execute the test.</param>
        public NtlmAuthenticationTest(BrowserKind browserKind) 
            : base(browserKind)
        {
        }

        /// <summary>
        /// Sets up the test execution, including starting web server, browser, and proxy.
        /// </summary>
        public override void SetUp()
        {
            // Start the test web application server.
            Console.Write("Starting test web app... ");
            host = WebHost.CreateDefaultBuilder()
                    .UseStartup<Startup>()
                    .UseKestrel((options) =>
                    {
                        options.ListenLocalhost(TestWebAppPort);
                    })
                    .UseHttpSys((options) =>
                    {
                        options.Authentication.Schemes = AuthenticationSchemes.NTLM | AuthenticationSchemes.Negotiate;
                    })
                    .ConfigureLogging((builder) =>
                    {
                        builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Warning);
                    })
                    .UseUrls(string.Format("http://{0}:{1}", TestWebAppHostName, TestWebAppPort))
                    .UseIISIntegration()
                    .Build();

            // Mixing sync and async APIs isn't ideal, but for testing purposes,
            // we will do so here.
            host.StartAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            Console.WriteLine("Started!");

            // Start the proxy server.
            Console.Write("Starting the proxy server... ");
            proxyServer = new HttpProxyServer("localhost", new HttpProxy());
            proxyServer.Start().WaitOne();
            Console.WriteLine("Started on port {0}", proxyServer.ProxyEndPoint.Port);

            // Hook up the OnResponseReceived handler, which happens after a
            // response is received from the web server, but before it is
            // delivered to the browser.
            proxyServer.Proxy.OnResponseReceived = OnResponseReceived;

            // Setup the Selenium Proxy object, and create a driver instance
            // with the browser configured to use the proxy.
            Console.WriteLine("Starting WebDriver instance for {0}", BrowserKind);
            Proxy proxy = new Proxy();
            proxy.HttpProxy = string.Format("{0}:{1}", "127.0.0.1", proxyServer.ProxyEndPoint.Port);
            this.driver = BrowserFactory.CreateWebDriver(this.BrowserKind, proxy);
        }

        /// <summary>
        /// Executes the test.
        /// </summary>
        public override void Execute()
        {
            string url = string.Format("http://{0}:{1}/api/auth/ntlm", TestWebAppHostName, TestWebAppPort);

            Console.WriteLine("Navigating to {0}", url);
            driver.Navigate().GoToUrl(url);
        }

        /// <summary>
        /// Tears down the test execution, including stopping the web server, browser, and proxy.
        /// </summary>
        public override void TearDown()
        {
            // Quit the browser.
            Console.WriteLine("Quitting WebDriver...");
            driver.Quit();

            // Stop the proxy.
            Console.WriteLine("Stopping proxy...");
            proxyServer.Stop();

            // Stop the test web application server.
            Console.WriteLine("Shutting down test web app...");
            host.StopAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Handles the OnResponseReceived for the proxy, which occurs after the response is
        /// received from the web server, but before it is forwarded on to the browser.
        /// </summary>
        /// <param name="context">A <see cref="BenderProxy.ProcessingContext"/> object.</param>
        public override void OnResponseReceived(ProcessingContext context)
        {
            string userName = "NtlmAuthTestUser";
            string password = "NtlmAuthTestP@ssw0rd!";

            if (context.ResponseHeader != null && context.ResponseHeader.StatusCode == 401)
            {
                // Only process requests for localhost or the redirected-
                // via-hosts-file-entry host, and where NTLM auth is requested.
                List<string> candidateUrls = new List<string>() { string.Format("localhost:{0}", TestWebAppPort), string.Format("{0}:{1}", TestWebAppHostName, TestWebAppPort) };
                if (candidateUrls.Contains(context.RequestHeader.Host) && context.ResponseHeader.WWWAuthenticate != null && context.ResponseHeader.WWWAuthenticate.Contains(NtlmGenerator.AuthorizationHeaderMarker))
                {
                    // Read the headers from the response and finish reading the response
                    // body, if any.
                    Console.WriteLine("Received 401 - Unauthorized response");
                    context.ServerStream.ReadTimeout = 5000;
                    context.ServerStream.WriteTimeout = 5000;
                    StreamReader reader = new StreamReader(context.ServerStream);
                    if (context.ResponseHeader.EntityHeaders.ContentLength != 0)
                    {
                        string drainBody = ReadFromStream(reader);
                    }

                    // We do not want the proxy to do any further processing after
                    // handling this message.
                    context.StopProcessing();

                    // Read the WWW-Authenticate header. Because of the way the test
                    // web app is configured, it returns multiple headers, with
                    // different schemes. We need to select the correct one.
                    string authHeader = GetAuthenticationHeader(context.ResponseHeader.WWWAuthenticate, NtlmGenerator.AuthorizationHeaderMarker);
                    Console.WriteLine("Processing WWW-Authenticate header: {0}", authHeader);

                    // Generate the initial message (the "type 1" or "Negotiation" message")
                    // and get the response, using BenderProxy's HttpMessageWriter and
                    // HttpHeaderReader and support classes.
                    Console.WriteLine("Generating authorization header value for Negotiate ('Type 1') message");
                    NtlmNegotiateMessageGenerator type1 = new NtlmNegotiateMessageGenerator();
                    string type1HeaderValue = type1.GenerateAuthorizationHeader();
                    context.RequestHeader.Authorization = type1HeaderValue;
                    Console.WriteLine("Resending request with Authorization header: {0}", type1HeaderValue);
                    HttpMessageWriter writer = new HttpMessageWriter(context.ServerStream);
                    writer.Write(context.RequestHeader);

                    HttpHeaderReader headerReader = new HttpHeaderReader(reader);
                    HttpResponseHeader challengeHeader = new HttpResponseHeader(headerReader.ReadHttpMessageHeader());
                    string challengeAuthHeader = challengeHeader.WWWAuthenticate;
                    string challengeBody = ReadFromStream(reader);

                    if (!string.IsNullOrEmpty(challengeAuthHeader) && challengeAuthHeader.StartsWith(NtlmGenerator.AuthorizationHeaderMarker))
                    {
                        // If a proper message was received (the "type 2" or "Challenge" message),
                        // parse it, and generate the proper authentication header (the "type 3"
                        // or "Authorization" message).
                        Console.WriteLine("Received 401 response with Challenge ('Type 2') message: {0}", challengeAuthHeader);
                        NtlmChallengeMessageGenerator type2 = new NtlmChallengeMessageGenerator(challengeAuthHeader);
                        Console.WriteLine("Generating Authorization ('Type 3') message for user name '{0}' and password '{1}'", userName, password);
                        NtlmAuthenticateMessageGenerator type3 = new NtlmAuthenticateMessageGenerator(null, null, userName, password, type2);
                        string type3HeaderValue = type3.GenerateAuthorizationHeader();
                        Console.WriteLine("Resending request with Authorization header: {0}", type3HeaderValue);
                        context.RequestHeader.Authorization = type3HeaderValue;
                        writer.Write(context.RequestHeader);

                        // Get the authorized response from the server, and forward it on to
                        // the browser.
                        HttpResponseHeader header = new HttpResponseHeader(headerReader.ReadHttpMessageHeader());
                        string body = ReadFromStream(reader);
                        Stream bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(body));
                        new HttpResponseWriter(context.ClientStream).Write(header, bodyStream, bodyStream.Length);
                        context.ClientStream.Flush();
                    }
                }
            }
        }
    }
}
