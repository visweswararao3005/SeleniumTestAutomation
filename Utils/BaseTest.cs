using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;

namespace TestAutomation.Utils
{
    public class BaseTest
    {
        protected IWebDriver Driver { get; private set; }
        public static string TestID { get; private set; }
        public static string? client;

        [OneTimeSetUp]
        public void GlobalSetup()
        {
            client = Environment.GetEnvironmentVariable("CLIENT_NAME") ?? "DanyaB";

            StatusLogger.LogRuntimeStart();
            TestID = $"{client}_{DateTime.Now:yyyyMMddHHmmss}";
        }

        [SetUp]
        public void SetUp()
        {
            // 🔹 Skip tests that don't match the client
            var categories = TestContext.CurrentContext.Test.Properties["Category"] as ICollection<object>;
            if (categories != null && categories.Count > 0 && !categories.Any(c => c?.ToString() == client))
            {
                Assert.Ignore($"Skipping test for client '{string.Join(",", categories)}' because CLIENT_NAME is '{client}'");
            }


            Logger.Info($"===== Test Started: {TestContext.CurrentContext.Test.Name} for Client {client} =====");
            var headless = Config.GetBool("AppSettings:Headless", false);

            var options = new ChromeOptions();
            if (headless) options.AddArgument("--headless=new");
            options.AddArgument("--disable-gpu");

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