using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using System;
using System.IO;
using System.Reflection;

namespace SeleniumAuthWithProxyExamples
{
    /// <summary>
    /// A static factory class for creating WebDriver instances with optional proxies.
    /// </summary>
    public static class BrowserFactory
    {
        /// <summary>
        /// Creates a WebDriver instance for the desired browser using the specified proxy settings.
        /// </summary>
        /// <param name="kind">The browser to launch.</param>
        /// <returns>A WebDriver instance using the specified proxy settings.</returns>
        public static IWebDriver CreateWebDriver(BrowserKind kind)
        {
            return CreateWebDriver(kind, null);
        }

        /// <summary>
        /// Creates a WebDriver instance for the desired browser using the specified proxy settings.
        /// </summary>
        /// <param name="kind">The browser to launch.</param>
        /// <param name="proxy">The WebDriver Proxy object containing the proxy settings.</param>
        /// <returns>A WebDriver instance using the specified proxy settings.</returns>
        public static IWebDriver CreateWebDriver(BrowserKind kind, Proxy proxy)
        {
            IWebDriver driver = null;
            switch (kind)
            {
                case BrowserKind.InternetExplorer:
                    driver = CreateInternetExplorerDriverWithProxy(proxy);
                    break;

                case BrowserKind.Firefox:
                    driver = CreateFirefoxDriverWithProxy(proxy);
                    break;

                case BrowserKind.Chrome:
                    driver = CreateChromeDriverWithProxy(proxy);
                    break;

                case BrowserKind.Edge:
                    throw new InvalidOperationException("Edge driver does not support proxies before Edge 75.");

                case BrowserKind.Safari:
                    throw new InvalidOperationException("This demo app must be run on Windows, because of reliance on NTLM authentication.");
            }

            return driver;
        }

        /// <summary>
        /// Creates an InternetExplorerDriver instance using the specified proxy settings.
        /// </summary>
        /// <param name="proxy">The WebDriver Proxy object containing the proxy settings.</param>
        /// <returns>An InternetExplorerDriver instance using the specified proxy settings</returns>
        private static IWebDriver CreateInternetExplorerDriverWithProxy(Proxy proxy)
        {
            InternetExplorerDriverService service = InternetExplorerDriverService.CreateDefaultService(GetCurrentDirectory());

            InternetExplorerOptions options = new InternetExplorerOptions();
            if (proxy != null)
            {
                options.Proxy = proxy;

                // Make IE not use the system proxy, and clear its cache before
                // launch. This makes the behavior of IE consistent with other
                // browsers' behavior.
                options.UsePerProcessProxy = true;
                options.EnsureCleanSession = true;
            }

            IWebDriver driver = new InternetExplorerDriver(service, options);
            return driver;
        }

        /// <summary>
        /// Creates an FirefoxDriver instance using the specified proxy settings.
        /// </summary>
        /// <param name="proxy">The WebDriver Proxy object containing the proxy settings.</param>
        /// <returns>An FirefoxDriver instance using the specified proxy settings</returns>
        private static IWebDriver CreateFirefoxDriverWithProxy(Proxy proxy)
        {
            FirefoxDriverService service = FirefoxDriverService.CreateDefaultService(GetCurrentDirectory());

            FirefoxOptions firefoxOptions = new FirefoxOptions();
            if (proxy != null)
            {
                firefoxOptions.Proxy = proxy;
            }

            IWebDriver driver = new FirefoxDriver(service, firefoxOptions);
            return driver;
        }

        /// <summary>
        /// Creates an ChromeDriver instance using the specified proxy settings.
        /// </summary>
        /// <param name="proxy">The WebDriver Proxy object containing the proxy settings.</param>
        /// <returns>An ChromeDriver instance using the specified proxy settings</returns>
        private static IWebDriver CreateChromeDriverWithProxy(Proxy proxy)
        {
            ChromeDriverService service = ChromeDriverService.CreateDefaultService(GetCurrentDirectory());

            ChromeOptions chromeOptions = new ChromeOptions();

            // Force spec-compliant protocol dialect for now. In
            // Selenium 4.x and with ChromeDriver 75+, this is no
            // longer necessary.
            chromeOptions.UseSpecCompliantProtocol = true;

            if (proxy != null)
            {
                chromeOptions.Proxy = proxy;
            }

            IWebDriver driver = new ChromeDriver(service, chromeOptions);
            return driver;
        }

        private static IWebDriver CreateEdgeDriverWithProxy(Proxy proxy)
        {
            EdgeOptions edgeOptions = new EdgeOptions();

            IWebDriver driver = new EdgeDriver(edgeOptions);
            return driver;
        }

        private static string GetCurrentDirectory()
        {
            FileInfo info = new FileInfo(Assembly.GetExecutingAssembly().Location);
            return info.DirectoryName;
        }
    }
}
