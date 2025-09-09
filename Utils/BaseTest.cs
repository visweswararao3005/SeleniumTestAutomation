using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

namespace LoginAutomation.Tests.Utils
{
    public class BaseTest
    {
        protected IWebDriver Driver { get; private set; }
        public static string TestID { get; private set; }

        [OneTimeSetUp]
        public void GlobalSetup()
        {
            StatusLogger.LogRuntimeStart();
            TestID = $"TestID_{DateTime.Now:yyyyMMddHHmmss}";
        }

        [SetUp]
        public void SetUp()
        {
            Logger.Info($"===== Test Started: {TestContext.CurrentContext.Test.Name} =====");
            var headless = Config.GetBool("AppSettings:Headless", false);

            var options = new ChromeOptions();
            if (headless) options.AddArgument("--headless=new");
            options.AddArgument("--disable-gpu");

            // If you installed Selenium.WebDriver.ChromeDriver, this just works.
            // Otherwise Selenium Manager will resolve a matching driver.
            Driver = new ChromeDriver(options);
            Logger.Info("Chrome WebDriver initialized.");
        }

        [TearDown]
        public void TearDown()
        {
            Logger.Info($"===== Test Ended: {TestContext.CurrentContext.Test.Name} =====\n");
            Driver?.Quit();
            Driver?.Dispose();
        }

    }
}