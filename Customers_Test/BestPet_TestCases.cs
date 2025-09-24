using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestAutomation.Helper;
using TestAutomation.Models;
using TestAutomation.Utils;

namespace TestAutomation.Tests
{
    [TestFixture]
    [Category("BestPet")]
    public class BestPet_TestCases : BaseTest 
    {
        private static readonly string projectRoot = Directory.GetParent(AppContext.BaseDirectory).Parent.Parent.Parent.FullName;         // Start from bin\Debug\net8.0   Go up 3 levels → project root
        private static readonly Dictionary<string, string> L = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(projectRoot, Config.Get("AppSettings:BestPetDataPointsFile"))));
        private readonly Dictionary<string, string> Screens = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(projectRoot, Config.Get("AppSettings:screenshotPath"))));

        private static readonly string LoginTestCaseFile = File.ReadAllText(Path.Combine(projectRoot, $"TestData\\BestPet\\LoginTestData.json"));
        private readonly string ItemsFile = File.ReadAllText(Path.Combine(projectRoot, $"TestData\\BestPet\\Items.json"));
        private readonly string CustomersFile = File.ReadAllText(Path.Combine(projectRoot, $"TestData\\BestPet\\Customers.json"));
        private readonly string CustomersItemMappingFile = File.ReadAllText(Path.Combine(projectRoot, $"TestData\\BestPet\\CustomerItemMapping.json"));
        private readonly string OrderFile = File.ReadAllText(Path.Combine(projectRoot, $"TestData\\BestPet\\Orders.json"));

        private static IEnumerable<LoginTestCase> LoadCases()
        {
            return JsonConvert.DeserializeObject<List<LoginTestCase>>(LoginTestCaseFile) ?? new();
        }
        public static IEnumerable<TestCaseData> Cases() => LoadCases().Select((tc, i) => new TestCaseData(tc).SetName($"{i + 1}.Login_{tc.Expected}_{tc.Username ?? "EMPTY"}"));

        [Test, TestCaseSource(nameof(Cases)), Order(1), Category("BestPet"), Category("Run_Login_Scenarios")]
        public void Run_Login_Scenarios(LoginTestCase tc)
        {
            string Screen = Screens["LoginScreen"];
            var startTime = DateTime.Now;
            string status = "Fail";
            Logger.Info($"Running test case: Username='{tc.Username}', Expected='{tc.Expected}'");

            var page = new CommonTestCasesHelper(Driver);
            page.Navigate(L);
            Logger.Info("Navigated to login page.");

            string errorMsg = page.Login(L, tc.Username ?? "", tc.Password ?? "", tc.RememberMe);
            if (string.IsNullOrEmpty(errorMsg))
            {
                errorMsg = BestPet_explore(L);
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
                            if (!string.IsNullOrEmpty(errorMsg))
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
                        string validationMsg = page.GetValidationMessage(L);
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
        [Test, Order(2), Category("BestPet"), Category("Add_Customers_Test")]
        public void Add_Customers_Test()
        {
            string Screen = Screens["CustomerScreen"];
            var startTime = DateTime.Now;
            string status = "Fail";

            var Page = new CommonTestCasesHelper(Driver);
            try
            {
                Logger.Info($"[TEST START] {TestContext.CurrentContext.Test.Name}");

                // Step 1: Navigate to URL
                Page.Navigate(L);
                Logger.Info("Navigated to login page.");

                // Step 2: Login
                var username = L["USERNAME"];
                var password = L["PASSWORD"];
                bool RememberMe = Convert.ToBoolean(L["REMEMBERME"]);

                string errorMsg = Page.Login(L, username ?? "", password ?? "", RememberMe);

                if (!string.IsNullOrEmpty(errorMsg))
                {
                    status = "Fail";
                    Assert.Fail("Login failed – stopping test execution."); // 🚀 EARLY EXIT
                }

                // Step 3: Add Customers
                errorMsg = BestPet_AddCustomer(L);

                if (!string.IsNullOrEmpty(errorMsg))
                {
                    status = "Fail";
                    Logger.Error($"[STEP 3] Adding Customer failed due to : {errorMsg}");
                    Assert.Fail("Failed – stopping test execution."); // 🚀 EARLY EXIT
                }
                // Step 4: Logout
                Page.logout(L);

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
        [Test, Order(3), Category("BestPet"), Category("Add_Items_Test")]
        public void Add_Items_Test()
        {
            string Screen = Screens["ItemScreen"];
            var startTime = DateTime.Now;
            string status = "Fail";
            IWebDriver _driver = Driver;
            var Page = new CommonTestCasesHelper(_driver);
            try
            {
                Logger.Info($"[TEST START] {TestContext.CurrentContext.Test.Name}");

                // Step 1: Navigate to URL
                Page.Navigate(L);
                Logger.Info("Navigated to login page.");

                // Step 2: Login
                var username = L["USERNAME"];
                var password = L["PASSWORD"];
                bool RememberMe = Convert.ToBoolean(L["REMEMBERME"]);

                string errorMsg = Page.Login(L, username ?? "", password ?? "", RememberMe);

                if (!string.IsNullOrEmpty(errorMsg))
                {
                    status = "Fail";
                    Assert.Fail("Login failed – stopping test execution."); // 🚀 EARLY EXIT
                }

                // Step 3: Add Items
                errorMsg = BestPet_AddItem(L);

                if (!string.IsNullOrEmpty(errorMsg))
                {
                    status = "Fail";
                    Logger.Error($"[STEP 3] Adding Item failed due to : {errorMsg}");
                    Assert.Fail("Failed – stopping test execution."); // 🚀 EARLY EXIT
                }

                // Step 4: Logout
                Page.logout(L);

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
        [Test, Order(4), Category("BestPet"), Category("Customer_Item_Mapping_Test")]
        public void Customer_Item_Mapping_Test()
        {
            string Screen = Screens["CustomerScreen"];
            var startTime = DateTime.Now;
            string status = "Fail";
            var Page = new CommonTestCasesHelper(Driver);

            try
            {
                Logger.Info($"[TEST START] {TestContext.CurrentContext.Test.Name}");

                // Step 1: Navigate to URL
                Page.Navigate(L);
                Logger.Info("Navigated to login page.");

                // Step 2: Login
                var username = L["USERNAME"];
                var password = L["PASSWORD"];
                bool RememberMe = Convert.ToBoolean(L["REMEMBERME"]);

                string errorMsg = Page.Login(L, username ?? "", password ?? "", RememberMe);

                if (!string.IsNullOrEmpty(errorMsg))
                {
                    status = "Fail";
                    Assert.Fail("Login failed – stopping test execution."); // 🚀 EARLY EXIT
                }

                // Step 3: Add Customers
                errorMsg = BestPet_CustomerItemMapping(L);

                if (!string.IsNullOrEmpty(errorMsg))
                {
                    status = "Fail";
                    Logger.Error($"[STEP 3] Customer Item Mapping failed: {errorMsg}");
                    Assert.Fail("Customer Item Mapping failed – stopping test execution.");
                }
                // Step 4: Logout
                Page.logout(L);

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
        [Test, Order(5), Category("BestPet"), Category("Order_Creation_Test")]
        public void Order_Creation_Test()
        {
            string Screen = Screens["OrderScreen"];
            var startTime = DateTime.Now;
            string status = "Fail";
            IWebDriver _driver = Driver;
            var Page = new CommonTestCasesHelper(_driver);

            Logger.Info($"[TEST START] {TestContext.CurrentContext.Test.Name}");

            try
            {
                // Step 1: Navigate to URL
                Logger.Info("[STEP 1] Navigating to Login page...");
                Page.Navigate(L);
                Logger.Info("[STEP 1] Successfully loaded login page.");

                // Step 2: Login
                var username = L["USERNAME"];
                var password = L["PASSWORD"];
                bool RememberMe = Convert.ToBoolean(L["REMEMBERME"]);

                Logger.Info($"[STEP 2] Logging in as '{username}' (RememberMe={RememberMe})");
                string errorMsg = Page.Login(L, username ?? "", password ?? "", RememberMe);

                if (!string.IsNullOrEmpty(errorMsg))
                {
                    Logger.Error($"[STEP 2] Login failed: {errorMsg}");
                    Assert.Fail("Login failed – stopping test execution.");
                }
                Logger.Info("[STEP 2] Login successful.");

                // Step 3: Order creation based on client
                Logger.Info($"[STEP 3] Starting order creation for client: {L["ClientName"]}");
                errorMsg = BestPet_CreateOrder(L);
                if (!string.IsNullOrEmpty(errorMsg))
                {
                    status = "Fail";
                    Logger.Error($"[STEP 3] Order creation failed: {errorMsg}");
                    Assert.Fail("Order creation failed – stopping test execution.");
                }
                Logger.Info("[STEP 3] Order creation completed.");

                // Step 4: Logout
                Logger.Info("[STEP 4] Logging out...");
                Page.logout(L);
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



        #region BestPet Methods
        IWebDriver _driver;
        public string BestPet_explore(Dictionary<string, string> L)
        {
            _driver = Driver;
            int time = 3000;
            Logger.Info("Exploring application menus...");
            string error = string.Empty;
            try
            {
                // Dashboard
                _driver.FindElement(By.LinkText(L["mainMenu_1"])).Click();
                Thread.Sleep(time);
                _driver.FindElement(By.PartialLinkText(L["subMenu_1_1"])).Click(); Thread.Sleep(time);
                _driver.FindElement(By.PartialLinkText(L["subMenu_1_2"])).Click(); Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_1_3"])).Click(); Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_1_4"])).Click(); Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_1_5"])).Click(); Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_1_6"])).Click(); Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_1_7"])).Click(); Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_1_8"])).Click(); Thread.Sleep(time);
                //Logger.Info($"Finished navigating {L["mainMenu_1"]} menus.");

                //// Admin
                //_driver.FindElement(By.LinkText(L["mainMenu_2"])).Click();
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_2_1"])).Click(); Thread.Sleep(time);
                //Logger.Info($"Finished navigating {L["mainMenu_2"]} menus.");

                //// Company
                //_driver.FindElement(By.LinkText(L["mainMenu_3"])).Click();
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_3_1"])).Click(); Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_3_2"])).Click(); Thread.Sleep(time);
                //Logger.Info($"Finished navigating {L["mainMenu_3"]} menus.");

                //// Inventory
                //_driver.FindElement(By.LinkText(L["mainMenu_4"])).Click();
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_4_1"])).Click(); Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_4_2"])).Click(); Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_4_3"])).Click(); Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_4_4"])).Click(); Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_4_5"])).Click(); Thread.Sleep(time);
                //Logger.Info($"Finished navigating {L["mainMenu_4"]} menus.");

                //// Sales Order
                //_driver.FindElement(By.LinkText(L["mainMenu_5"])).Click();
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_5_1"])).Click(); Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_5_2"])).Click(); Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_5_3"])).Click(); Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_5_4"])).Click(); Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_5_5"])).Click(); Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_5_6"])).Click(); Thread.Sleep(time);
                //Logger.Info($"Finished navigating {L["mainMenu_5"]} menus.");

                //// Warehouse
                //_driver.FindElement(By.LinkText(L["mainMenu_6"])).Click();
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_6_1"])).Click(); Thread.Sleep(time);
                //Logger.Info($"Finished navigating {L["mainMenu_6"]} menus.");

                //// Purchase Order
                //_driver.FindElement(By.LinkText(L["mainMenu_7"])).Click();
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_7_1"])).Click(); Thread.Sleep(time);
                //Logger.Info($"Finished navigating {L["mainMenu_7"]} menus.");

                //// Accounting
                //_driver.FindElement(By.LinkText(L["mainMenu_8"])).Click();
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_8_1"])).Click(); Thread.Sleep(time);
                //Logger.Info($"Finished navigating {L["mainMenu_8"]} menus.");

                //// Scan history list
                //_driver.FindElement(By.LinkText(L["mainMenu_9"])).Click();
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_9_1"])).Click(); Thread.Sleep(time);
                //Logger.Info($"Finished navigating {L["mainMenu_9"]} menus.");

                //// File Imports
                //_driver.FindElement(By.LinkText(L["mainMenu_10"])).Click();
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_10_1"])).Click(); Thread.Sleep(time);
                //Logger.Info($"Finished navigating {L["mainMenu_10"]} menus.");

                //// Reports
                //_driver.FindElement(By.LinkText(L["mainMenu_11"])).Click();
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_11_1"])).Click(); Thread.Sleep(time);
                //Logger.Info($"Finished navigating {L["mainMenu_11"]} menus.");

                // Resource
                _driver.FindElement(By.LinkText(L["mainMenu_12"])).Click();
                Thread.Sleep(time);
                _driver.FindElement(By.PartialLinkText(L["subMenu_12_1"])).Click(); Thread.Sleep(time);
                Logger.Info($"Finished navigating {L["mainMenu_12"]} menus.");


                //_driver.FindElement(By.Id(L["LogoutButton"])).Click();
                //Thread.Sleep(time);
                //_driver.FindElement(By.Id(L["NoLogout"])).Click();
                //Thread.Sleep(time);
                _driver.FindElement(By.Id(L["LogoutButton"])).Click();
                Thread.Sleep(time);
                _driver.FindElement(By.Id(L["YesLogout"])).Click();
                Thread.Sleep(time);

                Logger.Info("Logout sequence completed successfully.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error while exploring menus: {ex.Message}");
                return $"Error while exploring menus: {ex.Message}";
                throw;
            }
            return error;
        }
        public string BestPet_AddCustomer(Dictionary<string, string> L)
        {
            _driver = Driver;
            var page = new CommonTestCasesHelper(Driver);
            string error = string.Empty;
            // Step 5: Navigate to Customers Page
            int time = 3000;
            Thread.Sleep(time);
            Logger.Info("Navigating to Customers page...");
            _driver.FindElement(By.PartialLinkText(L["mainMenu_3"])).Click();
            Thread.Sleep(time);
            _driver.FindElement(By.PartialLinkText(L["subMenu_3_1"])).Click();
            Thread.Sleep(time);

            // Step 6: Read Customers.json
            //Logger.Info($"Reading JSON file: {CustomersFile}");
            var customers = JsonConvert.DeserializeObject<CustomerModel>(CustomersFile);
            Logger.Info($"[INFO] Loaded {customers.Customers.Count} customers from JSON.");
            try
            {
                int customerIndex = 1;
                foreach (var customer in customers.Customers)
                {
                    var details = customer.CustomerDetails;
                    var billing = customer.BillingDetails;
                    var shipping = customer.ShippingDetails;

                    Logger.Info($"{customerIndex} Adding Customer: " +
                                $"Name='{details.CustomerName}', " +
                                $"Email='{details.CustomerEmailAddress}', " +
                                $"Phone='{details.CustomerPhoneNumber}', " +
                                $"SalesRep='{details.SalesRep}' ");

                    _driver.FindElement(By.Id(L["AddCustomerButton"])).Click();
                    Thread.Sleep(time);

                    // --------- Customer Details -------------
                    _driver.FindElement(By.Id(L["Customer_customer_name"])).SendKeys(details.CustomerName);
                    _driver.FindElement(By.Id(L["Customer_short_name"])).SendKeys(details.CompanyName);
                    new SelectElement(_driver.FindElement(By.Id(L["Customer_Status"]))).SelectByText(details.Status);
                    new SelectElement(_driver.FindElement(By.Id(L["Customer_customer_type"]))).SelectByText(details.CustomerType);
                    //new SelectElement(_driver.FindElement(By.Id(L["Customer_sales_rep"]))).SelectByText(details.SalesRep);
                    _driver.FindElement(By.Id(L["Customer_fedex_account_number"])).SendKeys(details.FedexAccountNumber);
                    _driver.FindElement(By.Id(L["Customer_ups_customer_account_number"])).SendKeys(details.UPSAccountNumber);
                    new SelectElement(_driver.FindElement(By.Id(L["Customer_type"]))).SelectByText(details.Type);
                    Thread.Sleep(time);

                    // --------- Billing Address -------------
                    _driver.FindElement(By.Id(L["billing_name1"])).SendKeys(billing.Name1);
                    _driver.FindElement(By.Id(L["billing_name2"])).SendKeys(billing.Name2);
                    _driver.FindElement(By.Id(L["billing_email"])).SendKeys(billing.Email);
                    _driver.FindElement(By.Id(L["billing_phone"])).SendKeys(billing.Phone);
                    _driver.FindElement(By.Id(L["billing_address1"])).SendKeys(billing.Address1);
                    _driver.FindElement(By.Id(L["billing_address2"])).SendKeys(billing.Address2);
                    _driver.FindElement(By.Id(L["billing_city"])).SendKeys(billing.City);
                    new SelectElement(_driver.FindElement(By.Id(L["billing_state"]))).SelectByText(billing.State);
                    _driver.FindElement(By.Id(L["billing_postal_code"])).SendKeys(billing.PostalCode);
                    new SelectElement(_driver.FindElement(By.Id(L["billing_country"]))).SelectByText(billing.Country);
                    _driver.FindElement(By.Id(L["billing_notes"])).SendKeys(billing.Notes);
                    Thread.Sleep(time);

                    // --------- Shipping Address -------------
                    _driver.FindElement(By.Id(L["shipping_name1"])).SendKeys(shipping.Name1);
                    _driver.FindElement(By.Id(L["shipping_name2"])).SendKeys(shipping.Name2);
                    _driver.FindElement(By.Id(L["shipping_email"])).SendKeys(shipping.Email);
                    _driver.FindElement(By.Id(L["shipping_phone"])).SendKeys(shipping.Phone);
                    _driver.FindElement(By.Id(L["shipping_address1"])).SendKeys(shipping.Address1);
                    _driver.FindElement(By.Id(L["shipping_address2"])).SendKeys(shipping.Address2);
                    _driver.FindElement(By.Id(L["shipping_city"])).SendKeys(shipping.City);
                    new SelectElement(_driver.FindElement(By.Id(L["shipping_state"]))).SelectByText(shipping.State);
                    _driver.FindElement(By.Id(L["shipping_postal_code"])).SendKeys(shipping.PostalCode);
                    new SelectElement(_driver.FindElement(By.Id(L["shipping_country"]))).SelectByText(shipping.Country);
                    _driver.FindElement(By.Id(L["shipping_notes"])).SendKeys(shipping.Notes);
                    Thread.Sleep(time);

                    // --------- Save -------------
                    Logger.Info($"{customerIndex} Saving customer...");
                    _driver.FindElement(By.Id(L["SaveCustomerButton"])).Click();


                    error = page.GetErrorMessage();

                    if (string.IsNullOrEmpty(error))
                    {
                        Thread.Sleep(time);
                        Logger.Info($"[INFO] Customer '{details.CustomerName}' saved successfully.");
                    }
                    else
                    {
                        Logger.Error($"[ERROR] Failed to add customer '{details.CustomerName}': due to - {error}");
                        Assert.Fail($"Failed to add customer '{details.CustomerName}':due to - {error}");
                        page.CloseErrorBox(L);

                        Thread.Sleep(time);
                        _driver.FindElement(By.PartialLinkText(L["mainMenu_3"])).Click();
                        Thread.Sleep(time);
                        _driver.FindElement(By.PartialLinkText(L["subMenu_3_1"])).Click();
                        Thread.Sleep(time);
                    }
                    customerIndex++;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error while adding item: {ex.Message}");
                return $"Error while adding item: {ex.Message}";
                throw;
            }
            return error;
        }
        public string BestPet_AddItem(Dictionary<string, string> L)
        {
            _driver = Driver;
            var page = new CommonTestCasesHelper(Driver);
            string error = string.Empty;
            try
            {
                // Step 3: Navigate to Item Page
                int time = 3000;
                Thread.Sleep(time);
                Logger.Info("Navigating to Items page...");
                _driver.FindElement(By.PartialLinkText(L["mainMenu_4"])).Click();
                Thread.Sleep(time);
                _driver.FindElement(By.PartialLinkText(L["subMenu_4_1"])).Click();
                Thread.Sleep(time);
                // Step 4: Read Items.json
                //Logger.Info($"Reading JSON file: {ItemsFile}");
                var items = JsonConvert.DeserializeObject<ItemsModel>(ItemsFile);
                Logger.Info($"[INFO] Loaded {items.Items.Count} items from JSON.");

                int itemIndex = 1;
                foreach (var item in items.Items)
                {
                    var details = item.ItemDetails;
                    var dim = item.ItemDimensionsDetails;
                    var ship = item.ShipDetails;
                    var box = item.BoxDetails;
                    var carton = item.CartonDetails;

                    Logger.Info($"{itemIndex} Adding Item: " +
                                $"Item='{details.Item}', " +
                                $"UPCCode='{details.UPCCode}', " +
                                $"ManufacturePart='{details.ManufacturePart}', " +
                                $"Inventory='{details.Inventory}', " +
                                $"ItemDescription='{details.ItemDescription}' Soon....");

                    _driver.FindElement(By.Id(L["AddItemButton"])).Click();
                    Thread.Sleep(time);


                    // -------------------- ITEM DETAILS --------------------
                    _driver.FindElement(By.Id(L["ItemDetails_item_number"])).SendKeys(details.Item);
                    _driver.FindElement(By.Id(L["ItemDetails_upc_code"])).SendKeys(details.UPCCode);
                    _driver.FindElement(By.Id(L["ItemDetails_manufacture_part_number"])).SendKeys(details.ManufacturePart);
                    _driver.FindElement(By.Id(L["ItemDetails_inventory_number"])).SendKeys(details.Inventory);
                    _driver.FindElement(By.Id(L["ItemDetails_item_description"])).SendKeys(details.ItemDescription);
                    _driver.FindElement(By.Id(L["ItemDetails_notes"])).SendKeys(details.Notes);
                    new SelectElement(_driver.FindElement(By.Id(L["ItemDetails_item_type"]))).SelectByText(details.ItemType);
                    new SelectElement(_driver.FindElement(By.Id(L["ItemDetails_status"]))).SelectByText(details.Status);
                    _driver.FindElement(By.Id(L["ItemDetails_price"])).SendKeys(details.Price);
                    new SelectElement(_driver.FindElement(By.Id(L["ItemDetails_item_um"]))).SelectByText(details.ItemUM);
                    _driver.FindElement(By.Id(L["ItemDetails_backorder_date"])).SendKeys(details.BackOrderDate);
                    _driver.FindElement(By.Id(L["ItemDetails_backorder_available_qty"])).SendKeys(details.BackOrderAvailableQTY);
                    new SelectElement(_driver.FindElement(By.Id(L["ItemDetails_discontinued"]))).SelectByText(details.Discontinued);
                    new SelectElement(_driver.FindElement(By.Id(L["ItemDetails_fulfillment_type"]))).SelectByText(details.FulfillmentType);
                    new SelectElement(_driver.FindElement(By.Id(L["ItemDetails_product_type"]))).SelectByText(details.ProductType);

                    Thread.Sleep(time);

                    // -------------------- ITEM DIMENSIONS --------------------
                    _driver.FindElement(By.Id(L["ItemDimensionsDetails_item_weight_lbs"])).SendKeys(dim.ItemWeight);
                    new SelectElement(_driver.FindElement(By.Id(L["ItemDimensionsDetails_item_weight_lbs_Unit"]))).SelectByText(dim.ItemWeightUnits);
                    _driver.FindElement(By.Id(L["ItemDimensionsDetails_item_length_inches"])).SendKeys(dim.ItemLength);
                    new SelectElement(_driver.FindElement(By.Id(L["ItemDimensionsDetails_item_length_inches_Unit"]))).SelectByText(dim.ItemLengthUnits);
                    _driver.FindElement(By.Id(L["ItemDimensionsDetails_item_width_inches"])).SendKeys(dim.ItemWidth);
                    new SelectElement(_driver.FindElement(By.Id(L["ItemDimensionsDetails_item_width_inches_Unit"]))).SelectByText(dim.ItemWidthUnits);
                    _driver.FindElement(By.Id(L["ItemDimensionsDetails_item_height_inches"])).SendKeys(dim.ItemHeight);
                    new SelectElement(_driver.FindElement(By.Id(L["ItemDimensionsDetails_item_height_inches_Unit"]))).SelectByText(dim.ItemHeightUnits);
                    _driver.FindElement(By.Id(L["ItemDimensionsDetails_item_Volume"])).SendKeys(dim.ItemVolume);
                    new SelectElement(_driver.FindElement(By.Id(L["ItemDimensionsDetails_item_Volume_Unit"]))).SelectByText(dim.ItemVolumeUnits);
                    Thread.Sleep(time);

                    // -------------------- SHIP DETAILS --------------------
                    _driver.FindElement(By.Id(L["ship_weight"])).SendKeys(ship.ShipWeight);
                    new SelectElement(_driver.FindElement(By.Id(L["ship_weight_Units"]))).SelectByText(ship.ShipWeightUnits);
                    _driver.FindElement(By.Id(L["ship_length"])).SendKeys(ship.ShipLength);
                    new SelectElement(_driver.FindElement(By.Id(L["ship_length_Units"]))).SelectByText(ship.ShipLengthUnits);
                    _driver.FindElement(By.Id(L["ship_width"])).SendKeys(ship.ShipWidth);
                    new SelectElement(_driver.FindElement(By.Id(L["ship_width_Units"]))).SelectByText(ship.ShipWidthUnits);
                    _driver.FindElement(By.Id(L["ship_height"])).SendKeys(ship.ShipHeight);
                    new SelectElement(_driver.FindElement(By.Id(L["ship_height_Units"]))).SelectByText(ship.ShipHeightUnits);
                    _driver.FindElement(By.Id(L["ship_volume"])).SendKeys(ship.ShipVolume);
                    new SelectElement(_driver.FindElement(By.Id(L["ship_volume_Units"]))).SelectByText(ship.ShipVolumeUnits);
                    Thread.Sleep(time);

                    // -------------------- BOX DETAILS --------------------
                    _driver.FindElement(By.Id(L["box_weight"])).SendKeys(box.BoxWeight);
                    new SelectElement(_driver.FindElement(By.Id(L["box_weight_Units"]))).SelectByText(box.BoxWeightUnits);
                    _driver.FindElement(By.Id(L["box_length"])).SendKeys(box.BoxLength);
                    new SelectElement(_driver.FindElement(By.Id(L["box_length_Units"]))).SelectByText(box.BoxLengthUnits);
                    _driver.FindElement(By.Id(L["box_width"])).SendKeys(box.BoxWidth);
                    new SelectElement(_driver.FindElement(By.Id(L["box_width_Units"]))).SelectByText(box.BoxWidthUnits);
                    _driver.FindElement(By.Id(L["box_height"])).SendKeys(box.BoxHeight);
                    new SelectElement(_driver.FindElement(By.Id(L["box_height_Units"]))).SelectByText(box.BoxHeightUnits);
                    _driver.FindElement(By.Id(L["box_volume"])).SendKeys(box.BoxVolume);
                    new SelectElement(_driver.FindElement(By.Id(L["box_volume_Units"]))).SelectByText(box.BoxVolumeUnits);
                    _driver.FindElement(By.Id(L["items_per_box"])).SendKeys(box.ItemsPerBox);
                    Thread.Sleep(time);

                    // -------------------- CARTON DETAILS --------------------
                    _driver.FindElement(By.Id(L["carton_weight"])).SendKeys(carton.CartonWeight);
                    new SelectElement(_driver.FindElement(By.Id(L["carton_weight_Units"]))).SelectByText(carton.CartonWeightUnits);
                    _driver.FindElement(By.Id(L["carton_length"])).SendKeys(carton.CartonLength);
                    new SelectElement(_driver.FindElement(By.Id(L["carton_length_Units"]))).SelectByText(carton.CartonLengthUnits);
                    _driver.FindElement(By.Id(L["carton_width"])).SendKeys(carton.CartonWidth);
                    new SelectElement(_driver.FindElement(By.Id(L["carton_width_Units"]))).SelectByText(carton.CartonWidthUnits);
                    _driver.FindElement(By.Id(L["carton_height"])).SendKeys(carton.CartonHeight);
                    new SelectElement(_driver.FindElement(By.Id(L["carton_height_Units"]))).SelectByText(carton.CartonHeightUnits);
                    _driver.FindElement(By.Id(L["carton_volume"])).SendKeys(carton.CartonVolume);
                    new SelectElement(_driver.FindElement(By.Id(L["carton_volume_Units"]))).SelectByText(carton.CartonVolumeUnits);
                    _driver.FindElement(By.Id(L["items_per_carton"])).SendKeys(carton.ItemPerCarton);
                    Thread.Sleep(time);


                    Logger.Info($"{itemIndex} Saving Item...");
                    _driver.FindElement(By.Id(L["SaveItemButton"])).Click();
                    Thread.Sleep(time);

                    error = page.GetErrorMessage();

                    if (string.IsNullOrEmpty(error))
                    {
                        Thread.Sleep(time);
                        Logger.Info($"[INFO] Item '{details.Item}' saved successfully.");
                    }
                    else
                    {
                        Logger.Error($"[ERROR] Failed to add Item '{details.Item}': due to - {error}");
                        Assert.Fail($" Failed to add Item '{details.Item}': due to - {error}");
                        page.CloseErrorBox(L);

                        Thread.Sleep(time);

                        _driver.FindElement(By.PartialLinkText(L["subMenu_4_1"])).Click();
                        Thread.Sleep(time);
                        //
                    }

                    itemIndex++;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error while adding item: {ex.Message}");
                return $"Error while adding item: {ex.Message}";
                throw;
            }
            return error;
        }
        public string BestPet_CustomerItemMapping(Dictionary<string, string> L)
        {
            _driver = Driver;
            var page = new CommonTestCasesHelper(Driver);
            string error = string.Empty;
            int time = 3000;
            Thread.Sleep(time);

            try
            {
                Logger.Info("Navigating to Customers page...");
                _driver.FindElement(By.PartialLinkText(L["mainMenu_3"])).Click();
                Thread.Sleep(time);

                // Read CustomersItemMapping.json
                var mapping = JsonConvert.DeserializeObject<CustomerItemMappingModel>(CustomersItemMappingFile);
                Logger.Info($"[INFO] Loaded {mapping.Mappings.Count} Customers-Item Mapping from JSON.");

                int index = 1;
                foreach (var map in mapping.Mappings)
                {
                    _driver.FindElement(By.PartialLinkText(L["subMenu_3_1"])).Click();
                    Thread.Sleep(time);

                    var customerName = map.CustomerName;
                    var items = map.Items;
                    Logger.Info($"{index}. Adding Customers-Item: Customer Name='{customerName}' Number of Items {items.Count} ");

                    // Step 1: Locate row by customer name
                    try
                    {
                        // search with Customer name 
                        // Search button in header section
                        _driver.FindElement(By.CssSelector("div.command-item-display button#btnSearch")).Click();
                        Thread.Sleep(time);
                        _driver.FindElement(By.Id(L["CustomerItem_CustomerName"])).SendKeys(customerName);
                        Thread.Sleep(time);
                        // Search button in Footer section
                        _driver.FindElement(By.XPath("//footer//button[contains(.,'Search')]")).Click();
                        Thread.Sleep(time);

                        var row = _driver.FindElements(By.CssSelector("div.row"))
                                         .FirstOrDefault(r => r.Text.Contains(customerName));

                        if (row == null)
                        {
                            Logger.Warn($"Customer '{customerName}' not found on page.");
                            continue;
                        }

                        // Step 2: Click gear (dropdown-toggle) button
                        var gearButton = row.FindElement(By.CssSelector("button.dropdown-toggle"));
                        gearButton.Click();
                        Thread.Sleep(time);

                        // Step 3: Click "Manage Customer Items" option
                        var manageButton = row.FindElement(By.XPath(".//button[contains(text(),'Manage Customer Items')]"));
                        manageButton.Click();
                        Thread.Sleep(time);

                        // Step 4: Add items
                        foreach (var item in items)
                        {
                            Logger.Info($"    Adding Item: Name={item.Name}, UPC={item.UPC}, Qty={item.Quantity}");

                            // 1. Select Vendor SKU# from dropdown
                            new SelectElement(_driver.FindElement(By.Id(L["CustomerItem_ItemName"]))).SelectByText(item.Name);
                            // 4. Fill Item UPC
                            _driver.FindElement(By.Id(L["CustomerItem_ItemUPC"])).SendKeys(item.UPC);
                            // 6. Fill Reporting Quantity
                            _driver.FindElement(By.Id(L["CustomerItem_ItemQuantity"])).SendKeys(item.Quantity);
                            Thread.Sleep(time);
                            // 7. Click Save
                            _driver.FindElement(By.Id(L["CustomerItem_SaveItem"])).Click();
                            Thread.Sleep(time);
                        }
                        _driver.Navigate().Refresh();
                        Thread.Sleep(time);
                    }
                    catch (Exception exRow)
                    {
                        Logger.Error($"Error processing customer '{customerName}': {exRow.Message}");
                        continue;
                    }

                    index++;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                Logger.Error($"Error in Customer Item Mapping: {ex.Message}");
                return error;
            }

            return error;
        }
        public string BestPet_CreateOrder(Dictionary<string, string> L)
        {
            _driver = Driver;
            var page = new CommonTestCasesHelper(Driver);
            string error = string.Empty;
            int time = 3000;
            Thread.Sleep(time);
            try
            {
                Logger.Info("[ORDER FLOW] Navigating to Orders page...");
                _driver.FindElement(By.LinkText(L["mainMenu_5"])).Click();
                Thread.Sleep(time);
                _driver.FindElement(By.PartialLinkText(L["subMenu_5_1"])).Click();
                Thread.Sleep(time);
                _driver.FindElement(By.Id(L["AddOrderButton"])).Click();
                Thread.Sleep(time);
                Logger.Info("[ORDER FLOW] Orders page loaded.");

                var steps = _driver.FindElements(By.CssSelector("ul.steps > li"));
                Logger.Info($"[ORDER FLOW] Found {steps.Count} order steps on screen.");


                var OrderDetails = JsonConvert.DeserializeObject<OrdersModel>(OrderFile);

                foreach (var order in OrderDetails.Orders)
                {
                    for (int i = 0; i < 6; i++) // steps 1..6
                    {
                        Thread.Sleep(time);

                        var currentSteps = _driver.FindElements(By.CssSelector("ul.steps > li"));
                        var step = currentSteps[i];

                        var badge = step.FindElement(By.CssSelector("span.badge"));
                        string badgeClass = badge.GetAttribute("class");
                        string stepNumber = badge.Text.Trim();
                        string stepName = step.FindElement(By.CssSelector("small")).Text.Trim();

                        Logger.Info($"[ORDER STEP] Step {stepNumber} ({stepName}) → Status class: {badgeClass}");

                        if (badgeClass.Contains("badge-warning") || badgeClass.Contains("badge-danger"))
                        {
                            Logger.Warn($"[ORDER STEP] Step {stepNumber} is in WARNING state, taking action...");
                            switch (stepNumber)
                            {
                                case "1":
                                    Logger.Info("[STEP 1] Adding order details...");
                                    error = BestPet_addOrderDetails(order ,L);
                                    if (!string.IsNullOrEmpty(error))
                                    {
                                        //Logger.Error($"Error adding order details: {error}");
                                        return error;
                                    }
                                    _driver.FindElement(By.XPath("//button[contains(.,'Finalize')]")).Click();
                                    error += page.GetErrorMessage();
                                    if (!string.IsNullOrEmpty(error))
                                    {
                                        Logger.Error($"Error finalizing order: {error}");
                                        return error;
                                    }
                                    Thread.Sleep(time);
                                    Logger.Info("[STEP 1] Finalize clicked.");
                                    _driver.Navigate().Refresh();
                                    break;

                                case "2":
                                    Logger.Info("[STEP 2] Adding Pack details...");
                                    error = BestPet_addOrderPackDetails(order , L);
                                    if (!string.IsNullOrEmpty(error))
                                    {
                                        //Logger.Error($"Error adding pack details: {error}");
                                        return error;
                                    }
                                    Logger.Info("[STEP 2] Clicking Pack button...");
                                    page.TryClick("//button[contains(.,'Pack')]", "Pack");
                                    error += page.GetErrorMessage();
                                    if (!string.IsNullOrEmpty(error))
                                    {
                                        Logger.Error($"Error clicking Pack: {error}");
                                        return error;
                                    }
                                    break;

                                case "3":
                                    Logger.Info("[STEP 3] Packed - nothing to do.");
                                    break;

                                case "4":
                                    Logger.Info("[STEP 4] Waiting for Mark Shipped button...");
                                    bool shippedClicked = page.TryClickWithRetry("//button[contains(.,'Mark Shipped')]", "Mark Shipped", 3, 60000);
                                    error += page.GetErrorMessage();
                                    if (!string.IsNullOrEmpty(error))
                                    {
                                        Logger.Error($"Error clicking Mark Shipped: {error}");
                                        return error;
                                    }
                                    if (!shippedClicked)
                                        Logger.Warn("Mark Shipped button never appeared.");
                                    break;

                                case "5":
                                    Logger.Info("[STEP 5] Shipped - nothing to do.");
                                    break;

                                case "6":
                                    Logger.Info("[STEP 6] Clicking Delivered button...");
                                    page.TryClick("//button[contains(.,'Delivered')]", "Delivered");
                                    break;
                            }
                        }
                        //if (badgeClass.Contains("badge-danger"))
                        //{
                        //    Logger.Warn($"[ORDER STEP] Step {stepNumber} is in Danger state");
                        //    Assert.Fail($"[ORDER STEP] Step {stepNumber} is in Danger state");
                        //}
                    }
                }
                _driver.FindElement(By.PartialLinkText(L["subMenu_5_1"])).Click();
                Thread.Sleep(5000);
                return error;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                Logger.Error($"Error in CreateOrder: {ex.Message}");
                return error;
            }
        }


        public string BestPet_addOrderDetails(Order order, Dictionary<string, string> L)
        {
            _driver = Driver;
            var page = new CommonTestCasesHelper(Driver);
            string error = string.Empty;
            var OrderDetails = order;
            int time = 3000;

            try
            {
                Logger.Info($"[ORDER DETAILS] Starting to fill order for customer: {OrderDetails.SelectCustomerDetails.CustomerName}");

                // Customer
                Logger.Info("[ORDER DETAILS] Selecting customer...");

                _driver.FindElement(By.Id(L["SelectCustomer"])).Click();

                // Wait for the iframe to appear
                var iframe = new WebDriverWait(_driver, TimeSpan.FromSeconds(10))
                    .Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.CssSelector("iframe.cboxIframe")));
                // Switch to the iframe
                _driver.SwitchTo().Frame(iframe);
                Thread.Sleep(time);

                _driver.FindElement(By.Id(L["SearchCustomer"])).SendKeys(OrderDetails.SelectCustomerDetails.CustomerName);
                _driver.FindElement(By.Id(L["SearchCustomerButton"])).Click();
                Thread.Sleep(time);
                try
                {
                    IWebElement customerRow = _driver.FindElement(By.XPath($"//div[@id='bodyDiv']//div[@id='customerName'][contains(normalize-space(.), '{OrderDetails.SelectCustomerDetails.CustomerName}')]"));
                    customerRow.FindElement(By.XPath(".//input[@type='radio']")).Click();
                }
                catch (NoSuchElementException)
                {
                    string errorMsg = $"Error: Could not locate customer with name '{OrderDetails.SelectCustomerDetails.CustomerName}' in the order details section.";
                    return errorMsg; // Return your custom error message
                }
                Thread.Sleep(time);
                _driver.FindElement(By.Id(L["SearchCustomerSubmit"])).Click();

                Logger.Info("[ORDER DETAILS] Customer selected.");
                // When done, switch back to the main document
                _driver.SwitchTo().DefaultContent();
                Thread.Sleep(time);

                // Order info
                Logger.Info("[ORDER DETAILS] Filling order metadata...");

                _driver.FindElement(By.Id(L["OrderDetails_po_number"])).SendKeys(OrderDetails.OrderDetails.po_number);
                _driver.FindElement(By.Id(L["OrderDetails_customer_reference_number"])).SendKeys(OrderDetails.OrderDetails.customer_reference_number);
                Thread.Sleep(time);
                new SelectElement(_driver.FindElement(By.Id(L["OrderDetails_fulfillment_type"]))).SelectByText(OrderDetails.OrderDetails.fulfillment_type);
                Thread.Sleep(time);
                new SelectElement(_driver.FindElement(By.Id(L["OrderDetails_shipping_method"]))).SelectByText(OrderDetails.OrderDetails.shipping_method);
                Thread.Sleep(time);
                _driver.FindElement(By.Id(L["OrderDetails_account_number"])).SendKeys(OrderDetails.OrderDetails.account_number);
                _driver.FindElement(By.Id(L["OrderDetails_reference_1"])).SendKeys(OrderDetails.OrderDetails.reference_1);
                _driver.FindElement(By.Id(L["OrderDetails_reference_2"])).SendKeys(OrderDetails.OrderDetails.reference_2);
                _driver.FindElement(By.Id(L["OrderDetails_customer_message"])).SendKeys(OrderDetails.OrderDetails.customer_message);

                Logger.Info("[ORDER DETAILS] Order metadata filled.");
                Thread.Sleep(time);


                Logger.Info("[ORDER DETAILS] Filling addresses...");
                // Ship To
                _driver.FindElement(By.Id(L["ShipTo_first_name"])).SendKeys(OrderDetails.ShipTo.first_name);
                _driver.FindElement(By.Id(L["ShipTo_last_name"])).SendKeys(OrderDetails.ShipTo.last_name);
                _driver.FindElement(By.Id(L["ShipTo_phone"])).SendKeys(OrderDetails.ShipTo.phone);
                _driver.FindElement(By.Id(L["ShipTo_email"])).SendKeys(OrderDetails.ShipTo.email);
                _driver.FindElement(By.Id(L["ShipTo_address_1"])).SendKeys(OrderDetails.ShipTo.address_1);
                _driver.FindElement(By.Id(L["ShipTo_address_2"])).SendKeys(OrderDetails.ShipTo.address_2);
                new SelectElement(_driver.FindElement(By.Id(L["ShipTo_country"]))).SelectByText(OrderDetails.ShipTo.country);
                _driver.FindElement(By.Id(L["ShipTo_city"])).SendKeys(OrderDetails.ShipTo.city);
                new SelectElement(_driver.FindElement(By.Id(L["ShipTo_state"]))).SelectByText(OrderDetails.ShipTo.state);
                _driver.FindElement(By.Id(L["ShipTo_zip"])).SendKeys(OrderDetails.ShipTo.zip);
                _driver.FindElement(By.Id(L["ShipTo_notes"])).SendKeys(OrderDetails.ShipTo.notes);
                Thread.Sleep(time);
                // Bill To
                _driver.FindElement(By.Id(L["BillTo_first_name"])).Clear();
                _driver.FindElement(By.Id(L["BillTo_first_name"])).SendKeys(OrderDetails.BillTo.first_name);
                _driver.FindElement(By.Id(L["BillTo_last_name"])).Clear();
                _driver.FindElement(By.Id(L["BillTo_last_name"])).SendKeys(OrderDetails.BillTo.last_name);
                _driver.FindElement(By.Id(L["BillTo_phone"])).Clear();
                _driver.FindElement(By.Id(L["BillTo_phone"])).SendKeys(OrderDetails.BillTo.phone);
                _driver.FindElement(By.Id(L["BillTo_email"])).Clear();
                _driver.FindElement(By.Id(L["BillTo_email"])).SendKeys(OrderDetails.BillTo.email);
                _driver.FindElement(By.Id(L["BillTo_address_1"])).Clear();
                _driver.FindElement(By.Id(L["BillTo_address_1"])).SendKeys(OrderDetails.BillTo.address_1);
                _driver.FindElement(By.Id(L["BillTo_address_2"])).Clear();
                _driver.FindElement(By.Id(L["BillTo_address_2"])).SendKeys(OrderDetails.BillTo.address_2);
                new SelectElement(_driver.FindElement(By.Id(L["BillTo_country"]))).SelectByText(OrderDetails.BillTo.country);
                _driver.FindElement(By.Id(L["BillTo_city"])).Clear();
                _driver.FindElement(By.Id(L["BillTo_city"])).SendKeys(OrderDetails.BillTo.city);
                new SelectElement(_driver.FindElement(By.Id(L["BillTo_state"]))).SelectByText(OrderDetails.BillTo.state);
                _driver.FindElement(By.Id(L["BillTo_zip"])).Clear();
                _driver.FindElement(By.Id(L["BillTo_zip"])).SendKeys(OrderDetails.BillTo.zip);
                _driver.FindElement(By.Id(L["BillTo_notes"])).Clear();
                _driver.FindElement(By.Id(L["BillTo_notes"])).SendKeys(OrderDetails.BillTo.notes);
                Thread.Sleep(time);
                // Ship From
                _driver.FindElement(By.Id(L["ShipFrom_first_name"])).Clear();
                _driver.FindElement(By.Id(L["ShipFrom_first_name"])).SendKeys(OrderDetails.ShipFrom.first_name);
                _driver.FindElement(By.Id(L["ShipFrom_last_name"])).Clear();
                _driver.FindElement(By.Id(L["ShipFrom_last_name"])).SendKeys(OrderDetails.ShipFrom.last_name);
                _driver.FindElement(By.Id(L["ShipFrom_phone"])).Clear();
                _driver.FindElement(By.Id(L["ShipFrom_phone"])).SendKeys(OrderDetails.ShipFrom.phone);
                _driver.FindElement(By.Id(L["ShipFrom_email"])).Clear();
                _driver.FindElement(By.Id(L["ShipFrom_email"])).SendKeys(OrderDetails.ShipFrom.email);
                _driver.FindElement(By.Id(L["ShipFrom_address_1"])).Clear();
                _driver.FindElement(By.Id(L["ShipFrom_address_1"])).SendKeys(OrderDetails.ShipFrom.address_1);
                _driver.FindElement(By.Id(L["ShipFrom_address_2"])).Clear();
                _driver.FindElement(By.Id(L["ShipFrom_address_2"])).SendKeys(OrderDetails.ShipFrom.address_2);
                new SelectElement(_driver.FindElement(By.Id(L["ShipFrom_country"]))).SelectByText(OrderDetails.ShipFrom.country);
                _driver.FindElement(By.Id(L["ShipFrom_city"])).Clear();
                _driver.FindElement(By.Id(L["ShipFrom_city"])).SendKeys(OrderDetails.ShipFrom.city);
                new SelectElement(_driver.FindElement(By.Id(L["ShipFrom_state"]))).SelectByText(OrderDetails.ShipFrom.state);
                _driver.FindElement(By.Id(L["ShipFrom_zip"])).Clear();
                _driver.FindElement(By.Id(L["ShipFrom_zip"])).SendKeys(OrderDetails.ShipFrom.zip);
                _driver.FindElement(By.Id(L["ShipFrom_notes"])).Clear();
                _driver.FindElement(By.Id(L["ShipFrom_notes"])).SendKeys(OrderDetails.ShipFrom.notes);

                Logger.Info("[ORDER DETAILS] All addresses filled.");
                Thread.Sleep(time);


                // Items
                var items = OrderDetails.Items;
                Logger.Info($"[ORDER DETAILS] Adding {items.Count} items...");
                _driver.FindElement(By.Id(L["AddOrderItemButton"])).Click();
                Thread.Sleep(time);

                iframe = new WebDriverWait(_driver, TimeSpan.FromSeconds(10))
                    .Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.CssSelector("iframe.cboxIframe")));
                // Switch to the iframe
                _driver.SwitchTo().Frame(iframe);

                foreach (var item in items)
                {
                    string error1 = string.Empty;
                    Logger.Info($"[ORDER ITEM] Adding item: {item.ItemName} | Qty={item.Quantity} | Price={item.Price}");
                    _driver.FindElement(By.Id(L["SearchItem"])).Clear();
                    _driver.FindElement(By.Id(L["SearchItem"])).SendKeys(item.ItemName);
                    _driver.FindElement(By.Id(L["SearchItemButton"])).Click();
                    Thread.Sleep(time);
                    try
                    {
                        _driver.FindElement(By.XPath($"//div[@class='grid-body']//div[@class='row no-margin'][.//div[contains(normalize-space(.), '{item.ItemName}')]]")).Click();
                    }
                    catch (NoSuchElementException)
                    {
                        error1 = $"Could not locate item with name '{item.ItemName}' in the order details section. ";
                        error += error1;
                        Logger.Error($"[ERROR] Failed to add Item '{item.ItemName}': due to - {error1}");
                        continue; // Skip to the next item in the loop
                    }
                    Thread.Sleep(time);
                    new SelectElement(_driver.FindElement(By.Id(L["ItemWarehouse"]))).SelectByText(item.Warehouse);
                    //_driver.FindElement(By.Id(L["ItemPrice"])).SendKeys(item.Price);
                    _driver.FindElement(By.Id(L["ItemQuantity"])).SendKeys(item.Quantity);
                    _driver.FindElement(By.Id(L["AddItemSubmit"])).Click();
                    error1 += page.GetErrorMessage();
                    if (!string.IsNullOrEmpty(error1))
                    {
                        Logger.Error($"[ERROR] Failed to add Item '{item.ItemName}': due to - {error}");
                        //return error;
                    }
                    else
                    {
                        Logger.Info($"[ORDER ITEM] Item '{item.ItemName}' added successfully.");
                    }
                    Thread.Sleep(time);
                    error += error1;
                }

                // When done, switch back to the main document
                _driver.SwitchTo().DefaultContent();
                Thread.Sleep(time);
                _driver.FindElement(By.Id(L["CloseItemPopup"])).Click();
                Logger.Info("[ORDER DETAILS] Items popup closed.");

                return error;
            }
            catch (Exception ex)
            {
                error = $"Error in addOrderDetails: {ex.Message}";
                Logger.Error($"Error in addOrderDetails: {ex.Message}");
                return error;
            }
        }
        public string BestPet_addOrderPackDetails(Order order , Dictionary<string, string> L)
        {
            _driver = Driver;
            string error = string.Empty;
            var OrderDetails = order;
            int time = 3000;

            try
            {
                Logger.Info($"[ORDER DETAILS] Starting to fill PACK details for customer: {OrderDetails.SelectCustomerDetails.CustomerName}");
                var pack = OrderDetails.Packages;

                // Step 1: Add Package
                Logger.Info("[STEP 1] Clicking Add Package button...");
                _driver.FindElement(By.Id(L["AddpackageButton"])).Click();

                Logger.Info("[STEP 1] Waiting for Add Package iframe...");
                var iframe = new WebDriverWait(_driver, TimeSpan.FromSeconds(10))
                    .Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.CssSelector("iframe.cboxIframe")));

                Logger.Info("[STEP 1] Switching to Add Package iframe...");
                _driver.SwitchTo().Frame(iframe);
                Thread.Sleep(time);

                Logger.Info($"[STEP 1] Filling package details: Size={pack.PackageSize}, Weight={pack.PackageWeight}, Reference={pack.PackageReference}");
                new SelectElement(_driver.FindElement(By.Id(L["PackageSize"]))).SelectByText(pack.PackageSize);
                _driver.FindElement(By.Id(L["PackageWeight"])).SendKeys(pack.PackageWeight);
                _driver.FindElement(By.Id(L["PackageReference"])).SendKeys(pack.PackageReference);
                Thread.Sleep(time);

                Logger.Info("[STEP 1] Submitting Add Package form...");
                _driver.FindElement(By.Id(L["AddPackageSubmit"])).Click();
                Thread.Sleep(time);

                Logger.Info("[STEP 1] Switching back to main content...");
                _driver.SwitchTo().DefaultContent();
                Thread.Sleep(time);

                // Step 2: Manage Package Items
                Logger.Info("[STEP 2] Clicking Manage Package button...");
                _driver.FindElement(By.Id(L["ManagePackageButton"])).Click();

                Logger.Info("[STEP 2] Waiting for Manage Package iframe...");
                iframe = new WebDriverWait(_driver, TimeSpan.FromSeconds(10))
                    .Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.CssSelector("iframe.cboxIframe")));

                Logger.Info("[STEP 2] Switching to Manage Package iframe...");
                _driver.SwitchTo().Frame(iframe);
                Thread.Sleep(time);

                var items = OrderDetails.Items;
                Logger.Info($"[STEP 2] Updating quantity for {items.Count} items...");

                foreach (var item in items)
                {
                    Logger.Info($"[ITEM] Setting quantity for {item.ItemName} -> {item.Quantity}");
                    var row = _driver.FindElement(By.CssSelector($"tr.trRow[data-item-name='{item.ItemName}']"));
                    var qtyInput = row.FindElement(By.CssSelector("input.quantity"));
                    qtyInput.Clear();
                    qtyInput.SendKeys(item.Quantity);
                    Thread.Sleep(time);
                }

                Logger.Info("[STEP 2] Submitting Manage Package items...");
                _driver.FindElement(By.Id(L["ManagePackageSubmit"])).Click();

                Logger.Info("[STEP 2] Switching back to main content...");
                _driver.SwitchTo().DefaultContent();
                Thread.Sleep(time);

                Logger.Info("[SUCCESS] Package and item details submitted successfully.");
            }
            catch (Exception ex)
            {
                error = $"Error in addOrderPackDetails: {ex.Message}";
                Logger.Error($"[EXCEPTION] Error in addOrderPackDetails: {ex.Message}");
                Logger.Error($"[STACKTRACE] {ex.StackTrace}");
                return error;
            }

            return error;
        }

        #endregion

    }
}
