using System;
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
    public class DigestAuthenticationTest : AuthenticationTest
    {
        private IWebHost host;
        private IWebDriver driver;
        private HttpProxyServer proxyServer;

        /// <summary>
        /// Initializes a new instances of the <see cref="BasicAuthenticationTest"/> class.
        /// </summary>
        /// <param name="browserKind">The browser against which to execute the test.</param>
        public DigestAuthenticationTest(BrowserKind browserKind) 
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
            string url = string.Format("http://{0}:{1}/api/auth/digest", TestWebAppHostName, TestWebAppPort);

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
            // These are valid credentials and other necessary initial data
            // for the Digest case.
            string userName = "leela";
            string password = "Nibbler";
            string httpMethod = context.RequestHeader.Method;
            string hostHeaderValue = context.RequestHeader.Host;
            string requestUri = context.RequestHeader.RequestURI;
            int hostIndex = requestUri.IndexOf(hostHeaderValue);
            if (hostIndex >= 0)
            {
                requestUri = requestUri.Substring(hostIndex + hostHeaderValue.Length);
            }

            // Only do any processing on the response if the response is 401,
            // or "Unauthorized".
            if (context.ResponseHeader != null && context.ResponseHeader.StatusCode == 401)
            {
                // Read the headers from the response and finish reading the response
                // body, if any.
                Console.WriteLine("Received 401 - Unauthorized response");
                context.ServerStream.ReadTimeout = 5000;
                context.ServerStream.WriteTimeout = 5000;
                StreamReader reader = new StreamReader(context.ServerStream);
                HttpHeaderReader headerReader = new HttpHeaderReader(reader);
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
                string authHeader = GetAuthenticationHeader(context.ResponseHeader.WWWAuthenticate, DigestGenerator.AuthorizationHeaderMarker);
                Console.WriteLine("Processing WWW-Authenticate header: {0}", authHeader);

                // Calculate the value for the Authorization header, and resend
                // the request (with the Authorization header) to the server
                // using BenderProxy's HttpMessageWriter.
                Console.WriteLine("Generating authorization header value for user name '{0}' and password '{1}'", userName, password);
                DigestGenerator generator = new DigestGenerator(userName, password, httpMethod, requestUri, authHeader);
                string authorizationHeaderValue = generator.GenerateAuthorizationHeader();
                Console.WriteLine("Resending request with Authorization header: {0}", authorizationHeaderValue);
                context.RequestHeader.Authorization = authorizationHeaderValue;
                HttpMessageWriter writer = new HttpMessageWriter(context.ServerStream);
                writer.Write(context.RequestHeader);

                // Get the authorized response, and forward it on to the browser, using
                // BenderProxy's HttpHeaderReader and support classes.
                HttpResponseHeader header = new HttpResponseHeader(headerReader.ReadHttpMessageHeader());
                string body = ReadFromStream(reader);
                Stream bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(body));
                new HttpResponseWriter(context.ClientStream).Write(header, bodyStream, bodyStream.Length);
            }
        }
    }
}
