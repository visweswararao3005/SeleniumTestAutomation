using LoginAutomation.Tests.Models;
using LoginAutomation.Tests.Pages;
using LoginAutomation.Tests.Utils;
using LoginAutomation.Tests.Pages;
using Newtonsoft.Json;
using NUnit.Framework;
using NUnit.Framework.Internal;
using OpenQA.Selenium;
using OpenQA.Selenium.DevTools.V137.Debugger;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Logger = LoginAutomation.Tests.Utils.Logger;

namespace LoginAutomation.Tests.Tests
{
    [TestFixture]
    public class LoginTests : BaseTest
    {
        private static readonly string projectRoot = Directory.GetParent(AppContext.BaseDirectory).Parent.Parent.Parent.FullName;         // Start from bin\Debug\net8.0   Go up 3 levels → project root
       
        private static readonly Dictionary<string, string> DanyaB = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(projectRoot, Config.Get("AppSettings:DanyaBDataPointsFile"))));
        private static readonly Dictionary<string, string> Capital = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(projectRoot, Config.Get("AppSettings:CapitalDataPointsFile"))));
        private static readonly Dictionary<string, string> BestPet = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(projectRoot, Config.Get("AppSettings:BestPetDataPointsFile"))));

        Dictionary<string, string> L = BestPet; //JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(projectRoot, Config.Get("AppSettings:DataPointsFile"))));

        private readonly Dictionary<string, string> Screens = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(projectRoot, Config.Get("AppSettings:screenshotPath"))));

        private static readonly string LoginTestCaseFile = File.ReadAllText(Path.Combine(projectRoot, Config.Get("AppSettings:LoginTestData")));
        private static readonly string CustomersFile = File.ReadAllText(Path.Combine(projectRoot, Config.Get("AppSettings:CustomerData")));
        private static readonly string ItemsFile = File.ReadAllText(Path.Combine(projectRoot, Config.Get("AppSettings:ItemData")));

        private static IEnumerable<LoginTestCase> LoadCases()
        {
            return JsonConvert.DeserializeObject<List<LoginTestCase>>(LoginTestCaseFile) ?? new();
        }

        public static IEnumerable<TestCaseData> Cases() => LoadCases().Select((tc, i) => new TestCaseData(tc).SetName($"{i + 1}.Login_{tc.Expected}_{tc.Username ?? "EMPTY"}"));

        [Test, TestCaseSource(nameof(Cases)), Order(1)]
        public void Run_Login_Scenarios(LoginTestCase tc)
        {
            string Screen = Screens["LoginScreen"];
            var startTime = DateTime.Now;
            string status = "Fail";
            Logger.Info($"Running test case: Username='{tc.Username}', Expected='{tc.Expected}'");

            var page = new LoginPage(Driver);
            page.Navigate();
            Logger.Info("Navigated to login page.");

            string errorMsg = page.Login(tc.Username ?? "", tc.Password ?? "", tc.RememberMe);
            if(string.IsNullOrEmpty(errorMsg))
            {
                if (L["ClientName"] == "Danya B")
                {
                    page.explore();
                }
                if (L["ClientName"] == "Capital")
                {
                    page.Capital_explore();
                }
                if (L["ClientName"] == "BestPet")
                {
                    page.BestPet_explore();
                }
            }            

            var expect = (tc.Expected ?? "Error").ToLowerInvariant();
            var LogoutUrlPart = L["LogoutUrlContains"];
            try
            {
                switch (expect)
                {
                    case "success":
                        if (!string.IsNullOrWhiteSpace(LogoutUrlPart))
                        {
                            if (!(string.IsNullOrEmpty(errorMsg)))
                            {
                                Logger.Info($"Captured error message: {errorMsg}");
                                status = "Fail";
                                Assert.Fail($"Login failed Due to : {errorMsg}");
                            }
                            StringAssert.Contains(LogoutUrlPart.ToLowerInvariant(), Driver.Url.ToLowerInvariant());
                            Logger.Info($"✅ Success: Redirected to expected URL {Driver.Url}");
                            status = "Pass";
                        }
                        else
                        {
                            Logger.Error($"❌ Failed: Expected URL '{LogoutUrlPart}', but got '{Driver.Url}'");
                            Assert.Fail($"Expected URL '{LogoutUrlPart}', but got '{Driver.Url}'");
                        }
                        break;

                    case "error":
                        Logger.Info($"Captured error message: {errorMsg}");

                        Assert.IsTrue(errorMsg.Contains("invalid") || errorMsg.Contains("locked") || errorMsg.Contains("Invalid"),
                            $"Expected error message but got: {errorMsg}");
                        Assert.IsNotEmpty(errorMsg, "Expected an error/validation message but none was found.");
                        Logger.Info("✅ Error case validated successfully.");
                        status = "Pass";

                        break;

                    case "validation":
                        string validationMsg = page.GetValidationMessage();
                        Logger.Info($"Captured validation message: {validationMsg}");

                        Assert.IsTrue(validationMsg.Contains("required") || validationMsg.Contains("Invalid"),
                            $"Expected validation message but got: {validationMsg}");
                        Assert.IsNotEmpty(validationMsg, "Expected a validation message but none was found.");

                        Logger.Info("✅ Validation case validated successfully.");
                        status = "Pass";
                        break;

                    default:
                        Logger.Error($"❌ Unknown Expected value: '{tc.Expected}'. Use Success|Error|Validation.");
                        Assert.Fail($"Unknown Expected value: '{tc.Expected}'. Use Success|Error|Validation.");
                        break;
                }
            }
            catch (AssertionException)
            {
                status = "Fail";
                throw; // rethrow so NUnit still reports failure
            }
            finally
            {
                var endTime = DateTime.Now;
                string ClientName = L["ClientName"];
                Logger.Info($"[TEST END] {TestContext.CurrentContext.Test.Name} for Client-{ClientName} | Status={status} | Duration={(endTime - startTime).TotalSeconds} sec");
                StatusLogger.LogTestStatus(TestContext.CurrentContext.Test.Name, startTime, endTime, status, Screen, ClientName);
                DbLogger.LogTestResult(TestID, TestContext.CurrentContext.Test.Name, startTime, endTime, status, Screen, ClientName);
            }
        }
        [Test, Order(2)]
        public void Add_Customers_Test()
        {
            string Screen = Screens["CustomerScreen"];
            var startTime = DateTime.Now;
            string status = "Fail";
            var Page = new LoginPage(Driver);

            try
            {
                Logger.Info($"[TEST START] {TestContext.CurrentContext.Test.Name}");

                // Step 1: Navigate to URL
                Page.Navigate();
                Logger.Info("Navigated to login page.");

                // Step 2: Login
                var username = L["USERNAME"];
                var password = L["PASSWORD"];
                bool RememberMe = Convert.ToBoolean(L["REMEMBERME"]);

                string errorMsg = Page.Login(username ?? "", password ?? "", RememberMe);

                if (!string.IsNullOrEmpty(errorMsg))
                {
                    status = "Fail";
                    Assert.Fail("Login failed – stopping test execution."); // 🚀 EARLY EXIT
                }

                // Step 3: Add Customers
                if (L["ClientName"] == "Danya B")
                {
                    Page.AddCustomer();
                }
                if (L["ClientName"] == "Capital")
                {
                    Page.Captial_AddCustomer();
                }
                if (L["ClientName"] == "BestPet")
                {
                    Page.BestPet_AddCustomer();
                }

                // Step 4: Logout
                Page.logout();

                status = "Pass";
                Logger.Info("[TEST RESULT] Test completed successfully.");
            }
            catch (AssertionException)
            {
                status = "Fail";
                throw; // NUnit reports failure
            }
            catch (Exception ex)
            {
                status = "Fail";
                Logger.Error($"[ERROR] Test failed: {ex.Message}");
                throw;
            }
            finally
            {
                var endTime = DateTime.Now;
                string ClientName = L["ClientName"];
                Logger.Info($"[TEST END] {TestContext.CurrentContext.Test.Name} for Client-{ClientName} | Status={status} | Duration={(endTime - startTime).TotalSeconds} sec");
                StatusLogger.LogTestStatus(TestContext.CurrentContext.Test.Name, startTime, endTime, status, Screen, ClientName);
                DbLogger.LogTestResult(TestID, TestContext.CurrentContext.Test.Name, startTime, endTime, status, Screen, ClientName);
            }
        }
        [Test, Order(3)]
        public void Add_Items_Test()
        {
            string Screen = Screens["ItemScreen"];
            var startTime = DateTime.Now;
            string status = "Fail";
            IWebDriver _driver = Driver;
            var Page = new LoginPage(_driver);
            try
            {
                Logger.Info($"[TEST START] {TestContext.CurrentContext.Test.Name}");

                // Step 1: Navigate to URL
                Page.Navigate();
                Logger.Info("Navigated to login page.");

                // Step 2: Login
                var username = L["USERNAME"];
                var password = L["PASSWORD"];
                bool RememberMe = Convert.ToBoolean(L["REMEMBERME"]);

                string errorMsg = Page.Login(username ?? "", password ?? "", RememberMe);

                if (!string.IsNullOrEmpty(errorMsg))
                {
                    status = "Fail";
                    Assert.Fail("Login failed – stopping test execution."); // 🚀 EARLY EXIT
                }

                // Step 3: Add Items
                if (L["ClientName"] == "Danya B")
                {
                    Page.AddItem();
                }
                if (L["ClientName"] == "Capital")
                {
                    Page.Capital_AddItem();
                }
                if (L["ClientName"] == "BestPet")
                {
                    Page.BestPet_AddItem();
                }

                // Step 4: Logout
                Page.logout();

                status = "Pass";
                Logger.Info("[TEST RESULT] Test completed successfully.");

            }
            catch (AssertionException)
            {
                status = "Fail";
                throw; // NUnit reports failure
            }
            catch (Exception ex)
            {
                status = "Fail";
                Logger.Error($"[ERROR] Test failed: {ex.Message}");
                throw;
            }
            finally
            {
                var endTime = DateTime.Now;
                string ClientName = L["ClientName"];
                Logger.Info($"[TEST END] {TestContext.CurrentContext.Test.Name} for Client-{ClientName} | Status={status} | Duration={(endTime - startTime).TotalSeconds} sec");
                StatusLogger.LogTestStatus(TestContext.CurrentContext.Test.Name, startTime, endTime, status, Screen, ClientName);
                DbLogger.LogTestResult(TestID, TestContext.CurrentContext.Test.Name, startTime, endTime, status, Screen, ClientName);
            }
        }
        [Test, Order(4)]
        public void Customer_Item_Mapping_Test()
        {
            string Screen = Screens["CustomerScreen"];
            var startTime = DateTime.Now;
            string status = "Fail";
            var Page = new LoginPage(Driver);

            try
            {
                Logger.Info($"[TEST START] {TestContext.CurrentContext.Test.Name}");

                // Step 1: Navigate to URL
                Page.Navigate();
                Logger.Info("Navigated to login page.");

                // Step 2: Login
                var username = L["USERNAME"];
                var password = L["PASSWORD"];
                bool RememberMe = Convert.ToBoolean(L["REMEMBERME"]);

                string errorMsg = Page.Login(username ?? "", password ?? "", RememberMe);

                if (!string.IsNullOrEmpty(errorMsg))
                {
                    status = "Fail";
                    Assert.Fail("Login failed – stopping test execution."); // 🚀 EARLY EXIT
                }

                // Step 3: Add Customers
                if (L["ClientName"] == "Danya B")
                {
                    Page.CustomerItemMapping();
                }
                if (L["ClientName"] == "Capital")
                {

                    Page.Capital_CustomerItemMapping();
                }
                if (L["ClientName"] == "BestPet")
                {
                    errorMsg = Page.BestPet_CustomerItemMapping();
                    if (!string.IsNullOrEmpty(errorMsg))
                    {
                        Logger.Error($"[STEP 3] Customer Item Mapping failed: {errorMsg}");
                        Assert.Fail("Customer Item Mapping failed – stopping test execution.");
                    }
                }

                // Step 4: Logout
                Page.logout();

                status = "Pass";
                Logger.Info("[TEST RESULT] Test completed successfully.");

            }
            catch (AssertionException)
            {
                status = "Fail";
                throw; // NUnit reports failure
            }
            catch (Exception ex)
            {
                status = "Fail";
                Logger.Error($"[ERROR] Test failed: {ex.Message}");
                throw;
            }
            finally
            {
                var endTime = DateTime.Now;
                string ClientName = L["ClientName"];
                Logger.Info($"[TEST END] {TestContext.CurrentContext.Test.Name} for Client-{ClientName} | Status={status} | Duration={(endTime - startTime).TotalSeconds} sec");
                StatusLogger.LogTestStatus(TestContext.CurrentContext.Test.Name, startTime, endTime, status, Screen, ClientName);
                DbLogger.LogTestResult(TestID, TestContext.CurrentContext.Test.Name, startTime, endTime, status, Screen, ClientName);
            }
        }
        [Test, Order(5)]
        public void Order_Creation_Test()
        {
            string Screen = Screens["OrderScreen"];
            var startTime = DateTime.Now;
            string status = "Fail";
            IWebDriver _driver = Driver;
            var Page = new LoginPage(_driver);

            Logger.Info($"[TEST START] {TestContext.CurrentContext.Test.Name}");

            try
            {
                // Step 1: Navigate to URL
                Logger.Info("[STEP 1] Navigating to Login page...");
                Page.Navigate();
                Logger.Info("[STEP 1] Successfully loaded login page.");

                // Step 2: Login
                var username = L["USERNAME"];
                var password = L["PASSWORD"];
                bool RememberMe = Convert.ToBoolean(L["REMEMBERME"]);

                Logger.Info($"[STEP 2] Logging in as '{username}' (RememberMe={RememberMe})");
                string errorMsg = Page.Login(username ?? "", password ?? "", RememberMe);

                if (!string.IsNullOrEmpty(errorMsg))
                {
                    Logger.Error($"[STEP 2] Login failed: {errorMsg}");
                    Assert.Fail("Login failed – stopping test execution.");
                }
                Logger.Info("[STEP 2] Login successful.");

                // Step 3: Order creation based on client
                Logger.Info($"[STEP 3] Starting order creation for client: {L["ClientName"]}");
                if (L["ClientName"] == "Danya B")
                {
                    Page.CreateOrder();
                }
                else if (L["ClientName"] == "Capital")
                {
                    errorMsg = Page.Capital_CreateOrder();
                    if (!string.IsNullOrEmpty(errorMsg))
                    {
                        Logger.Error($"[STEP 3] Order creation failed: {errorMsg}");
                        Assert.Fail("Order creation failed – stopping test execution.");
                    }
                }
                else if (L["ClientName"] == "BestPet")
                {
                    errorMsg = Page.BestPet_CreateOrder();
                    if (!string.IsNullOrEmpty(errorMsg))
                    {
                        Logger.Error($"[STEP 3] Order creation failed: {errorMsg}");
                        Assert.Fail("Order creation failed – stopping test execution.");
                    }
                }
                Logger.Info("[STEP 3] Order creation completed.");

                // Step 4: Logout
                Logger.Info("[STEP 4] Logging out...");
                Page.logout();
                Logger.Info("[STEP 4] Logout successful.");

                status = "Pass";
                Logger.Info("[TEST RESULT] Test completed successfully.");
            }
            catch (AssertionException)
            {
                status = "Fail";
                throw;
            }
            catch (Exception ex)
            {
                status = "Fail";
                Logger.Error($"[ERROR] Test failed: {ex}");
                throw;
            }
            finally
            {
                var endTime = DateTime.Now;
                string clientName = L["ClientName"];
                Logger.Info($"[TEST END] {TestContext.CurrentContext.Test.Name} | Client={clientName} | Status={status} | Duration={(endTime - startTime).TotalSeconds:F2}s");

                StatusLogger.LogTestStatus(TestContext.CurrentContext.Test.Name, startTime, endTime, status, Screen, clientName);
                DbLogger.LogTestResult(TestID, TestContext.CurrentContext.Test.Name, startTime, endTime, status, Screen, clientName);
            }
        }
    }
}
