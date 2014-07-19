using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AutomationDrivers.Core.Exceptions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;

namespace AutomationDrivers.SeleniumDriver
{
    public class SeleniumDriver
    {
        public enum Browser
        {
            InternetExplorer = 1,
            InternetExplorer64 = 2,
            Firefox = 3,
            Chrome = 4,
            PhantomJs = 5,
            Safari = 6,
            iPad = 7,
            iPhone = 8,
            Android = 9
        }

        private static TimeSpan DefaultCommandTimeout = TimeSpan.FromSeconds(60);

        public static void Bootstrap()
        {
            Bootstrap(Browser.Firefox);
        }

        public static void Bootstrap(Browser browser)
        {
            Bootstrap(browser, DefaultCommandTimeout);
        }

        public static void Bootstrap(Browser browser, TimeSpan commandTimeout)
        {
            FluentSettings.Current.ContainerRegistration = (container) =>
            {
                container.Register<ICommandProvider, CommandProvider>();
                container.Register<IAssertProvider, AssertProvider>();
                container.Register<IFileStoreProvider, LocalFileStoreProvider>();

                var browserDriver = GenerateBrowserSpecificDriver(browser, commandTimeout);
                container.Register<IWebDriver>((c, o) => browserDriver());
            };
        }

        public static void Bootstrap(params Browser[] browsers)
        {
            Bootstrap(DefaultCommandTimeout, browsers);
        }

        public static void Bootstrap(TimeSpan commandTimeout, params Browser[] browsers)
        {
            if (browsers.Length == 1)
            {
                Bootstrap(browsers.First());
                return;
            }

            FluentSettings.Current.ContainerRegistration = (container) =>
            {
                FluentTest.IsMultiBrowserTest = true;

                var webDrivers = new List<Func<IWebDriver>>();
                browsers.Distinct().ToList().ForEach(x => webDrivers.Add(GenerateBrowserSpecificDriver(x, commandTimeout)));

                var commandProviders = new CommandProviderList(webDrivers.Select(x => new CommandProvider(x, new LocalFileStoreProvider())));
                container.Register<CommandProviderList>(commandProviders);

                container.Register<ICommandProvider, MultiCommandProvider>();
                container.Register<IAssertProvider, MultiAssertProvider>();
                container.Register<IFileStoreProvider, LocalFileStoreProvider>();
            };
        }

        public static void Bootstrap(Uri driverUri, Browser browser)
        {
            Bootstrap(driverUri, browser, DefaultCommandTimeout);
        }

        public static void Bootstrap(Uri driverUri, Browser browser, TimeSpan commandTimeout)
        {
            FluentSettings.Current.ContainerRegistration = (container) =>
            {
                container.Register<ICommandProvider, CommandProvider>();
                container.Register<IAssertProvider, AssertProvider>();
                container.Register<IFileStoreProvider, LocalFileStoreProvider>();

                DesiredCapabilities browserCapabilities = GenerateDesiredCapabilities(browser);
                container.Register<IWebDriver, RemoteWebDriver>(new EnhancedRemoteWebDriver(driverUri, browserCapabilities, commandTimeout));
            };
        }

        public static void Bootstrap(Uri driverUri, Browser browser, Dictionary<string, object> capabilities)
        {
            Bootstrap(driverUri, browser, capabilities, DefaultCommandTimeout);
        }

        public static void Bootstrap(Uri driverUri, Browser browser, Dictionary<string, object> capabilities, TimeSpan commandTimeout)
        {
            FluentSettings.Current.ContainerRegistration = (container) =>
            {
                container.Register<ICommandProvider, CommandProvider>();
                container.Register<IAssertProvider, AssertProvider>();
                container.Register<IFileStoreProvider, LocalFileStoreProvider>();

                DesiredCapabilities browserCapabilities = GenerateDesiredCapabilities(browser);
                foreach (var cap in capabilities)
                {
                    browserCapabilities.SetCapability(cap.Key, cap.Value);
                }

                container.Register<IWebDriver, RemoteWebDriver>(new EnhancedRemoteWebDriver(driverUri, browserCapabilities, commandTimeout));
            };
        }

        public static void Bootstrap(Uri driverUri, Dictionary<string, object> capabilities)
        {
            Bootstrap(driverUri, capabilities, DefaultCommandTimeout);
        }

        public static void Bootstrap(Uri driverUri, Dictionary<string, object> capabilities, TimeSpan commandTimeout)
        {
            FluentSettings.Current.ContainerRegistration = (container) =>
            {
                container.Register<ICommandProvider, CommandProvider>();
                container.Register<IAssertProvider, AssertProvider>();
                container.Register<IFileStoreProvider, LocalFileStoreProvider>();

                DesiredCapabilities browserCapabilities = new DesiredCapabilities(capabilities);
                container.Register<IWebDriver, RemoteWebDriver>(new EnhancedRemoteWebDriver(driverUri, browserCapabilities, commandTimeout));
            };
        }

        private static Func<IWebDriver> GenerateBrowserSpecificDriver(Browser browser)
        {
            return GenerateBrowserSpecificDriver(browser, DefaultCommandTimeout);
        }

        private static Func<IWebDriver> GenerateBrowserSpecificDriver(Browser browser, TimeSpan commandTimeout)
        {
            string driverPath = string.Empty;
            switch (browser)
            {
                case Browser.InternetExplorer:
                    driverPath = EmbeddedResources.UnpackFromAssembly("IEDriverServer32.exe", "IEDriverServer.exe", Assembly.GetAssembly(typeof(SeleniumDriver)));
                    return new Func<IWebDriver>(() => new Wrappers.IEDriverWrapper(Path.GetDirectoryName(driverPath), commandTimeout));
                case Browser.InternetExplorer64:
                    driverPath = EmbeddedResources.UnpackFromAssembly("IEDriverServer64.exe", "IEDriverServer.exe", Assembly.GetAssembly(typeof(SeleniumDriver)));
                    return new Func<IWebDriver>(() => new Wrappers.IEDriverWrapper(Path.GetDirectoryName(driverPath), commandTimeout));
                case Browser.Firefox:
                    return new Func<IWebDriver>(() =>
                    {
                        var firefoxBinary = new OpenQA.Selenium.Firefox.FirefoxBinary();
                        return new OpenQA.Selenium.Firefox.FirefoxDriver(firefoxBinary, new OpenQA.Selenium.Firefox.FirefoxProfile
                        {
                            EnableNativeEvents = false,
                            AcceptUntrustedCertificates = true,
                            AlwaysLoadNoFocusLibrary = true
                        }, commandTimeout);
                    });
                case Browser.Chrome:
                    driverPath = EmbeddedResources.UnpackFromAssembly("chromedriver.exe", Assembly.GetAssembly(typeof(SeleniumDriver)));

                    var chromeService = ChromeDriverService.CreateDefaultService(Path.GetDirectoryName(driverPath));
                    chromeService.SuppressInitialDiagnosticInformation = true;

                    var chromeOptions = new ChromeOptions();
                    chromeOptions.AddArgument("--log-level=3");

                    return new Func<IWebDriver>(() => new OpenQA.Selenium.Chrome.ChromeDriver(chromeService, chromeOptions, commandTimeout));
                case Browser.PhantomJs:
                    driverPath = EmbeddedResources.UnpackFromAssembly("phantomjs.exe", Assembly.GetAssembly(typeof(SeleniumDriver)));

                    var phantomOptions = new OpenQA.Selenium.PhantomJS.PhantomJSOptions();
                    return new Func<IWebDriver>(() => new OpenQA.Selenium.PhantomJS.PhantomJSDriver(Path.GetDirectoryName(driverPath), phantomOptions, commandTimeout));
            }

            throw new NotImplementedException("Selected browser " + browser.ToString() + " is not supported yet.");
        }

        private static DesiredCapabilities GenerateDesiredCapabilities(Browser browser)
        {
            DesiredCapabilities browserCapabilities = null;

            switch (browser)
            {
                case Browser.InternetExplorer:
                case Browser.InternetExplorer64:
                    browserCapabilities = DesiredCapabilities.InternetExplorer();
                    break;
                case Browser.Firefox:
                    browserCapabilities = DesiredCapabilities.Firefox();
                    break;
                case Browser.Chrome:
                    browserCapabilities = DesiredCapabilities.Chrome();
                    break;
                case Browser.PhantomJs:
                    browserCapabilities = DesiredCapabilities.PhantomJS();
                    break;
                case Browser.Safari:
                    browserCapabilities = DesiredCapabilities.Safari();
                    break;
                default:
                    throw new AutomationDriverException("Selected browser [{0}] not supported. Unable to determine appropriate capabilities.", browser.ToString());
            }

            browserCapabilities.IsJavaScriptEnabled = true;
            return browserCapabilities;
        }
    }
}
