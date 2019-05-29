using System;

namespace SeleniumAuthWithProxyExamples
{
    class Program
    {
        static void Main(string[] args)
        {
            // If you want to use a different browser, change this line
            // to a different BrowserKind value.
            BrowserKind browser = BrowserKind.Firefox;

            // If you want to test a different authentication type, change
            // this line to a different AuthenticationKind value.
            AuthenticationKind authKind = AuthenticationKind.Basic;

            AuthenticationTest test = AuthenticationTestFactory.CreateTest(authKind, browser);

            // If you need to, you can change the test web app host name and port
            // here, by uncommenting and modifying the following lines:
            // test.TestWebAppHostName = "www.seleniumhq-test.test";
            // test.TestWebAppPort = 5000;
            test.SetUp();
            try
            {
                Console.WriteLine("Starting test for {0} authentication with browser {1}", authKind, browser);
                Console.WriteLine();
                test.Execute();
                Console.WriteLine();
                Console.WriteLine("Test is complete. Press <Enter> to shut down running components.");
                Console.ReadLine();
            }
            finally
            {
                test.TearDown();
            }
        }
    }
}
