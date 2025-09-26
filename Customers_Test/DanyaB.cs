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
    [Category("DanyaB")]
    public class DanyaB : BaseTest
    {
        private static readonly string projectRoot = Directory.GetParent(AppContext.BaseDirectory).Parent.Parent.Parent.FullName;         // Start from bin\Debug\net8.0   Go up 3 levels → project root
        private static readonly Dictionary<string, string> L = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(projectRoot, Config.Get("AppSettings:DanyaBDataPointsFile"))));
        
        private static readonly string LoginTestCaseFile = File.ReadAllText(Path.Combine(projectRoot, $"TestData\\Danya B\\LoginTestData.json"));
        private readonly string ItemsFile = File.ReadAllText(Path.Combine(projectRoot, $"TestData\\Danya B\\Items.json"));
        private readonly string CustomersFile = File.ReadAllText(Path.Combine(projectRoot, $"TestData\\Danya B\\Customers.json"));
        private readonly string CustomersItemMappingFile = File.ReadAllText(Path.Combine(projectRoot, $"TestData\\Danya B\\CustomerItemMapping.json"));
        private readonly string OrderFile = File.ReadAllText(Path.Combine(projectRoot, $"TestData\\Danya B\\Orders.json"));

        private static IEnumerable<LoginTestCase> LoadCases()
        {
            return JsonConvert.DeserializeObject<List<LoginTestCase>>(LoginTestCaseFile) ?? new();
        }

        public static IEnumerable<TestCaseData> Cases() => LoadCases().Select((tc, i) => new TestCaseData(tc).SetName($"{i + 1}.Login_{tc.Expected}_{tc.Username ?? "EMPTY"}"));

        [Test, TestCaseSource(nameof(Cases)), Order(1), Category("DanyaB"), Category("Run_Login_Scenarios")]
        public void Run_Login_Scenarios(LoginTestCase tc)
        {
            string Screen = Screens["Run_Login_Scenarios"];
            var startTime = DateTime.Now;
            string status = "Fail";
            Logger.Info($"Running test case: Username='{tc.Username}', Expected='{tc.Expected}'");

            var page = new CommonTestCasesHelper(Driver);
            page.Navigate(L);
            Logger.Info("Navigated to login page.");

            string errorMsg = page.Login(L,tc.Username ?? "", tc.Password ?? "", tc.RememberMe);
            if (string.IsNullOrEmpty(errorMsg))
            {
                errorMsg = explore(L);                
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
        [Test, Order(2), Category("DanyaB"), Category("Add_Customers_Test")]
        public void Add_Customers_Test()
        {
            string Screen = Screens["Add_Customers_Test"];
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

                string errorMsg = Page.Login(L,username ?? "", password ?? "", RememberMe);

                if (!string.IsNullOrEmpty(errorMsg))
                {
                    status = "Fail";
                    Assert.Fail("Login failed – stopping test execution."); // 🚀 EARLY EXIT
                }

                // Step 3: Add Customers
                errorMsg = AddCustomer(L);
                
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
        [Test, Order(3), Category("DanyaB"), Category("Add_Items_Test")]
        public void Add_Items_Test()
        {
            string Screen = Screens["Add_Items_Test"];
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

                string errorMsg = Page.Login(L,username ?? "", password ?? "", RememberMe);

                if (!string.IsNullOrEmpty(errorMsg))
                {
                    status = "Fail";
                    Assert.Fail("Login failed – stopping test execution."); // 🚀 EARLY EXIT
                }

                // Step 3: Add Items
                errorMsg = AddItem(L);
                
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
        [Test, Order(4), Category("DanyaB"), Category(" ")]
        public void Customer_Item_Mapping_Test()
        {
            string Screen = Screens["Customer_Item_Mapping_Test"];
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

                string errorMsg = Page.Login(L,username ?? "", password ?? "", RememberMe);

                if (!string.IsNullOrEmpty(errorMsg))
                {
                    status = "Fail";
                    Assert.Fail("Login failed – stopping test execution."); // 🚀 EARLY EXIT
                }

                // Step 3: Add Customers
                errorMsg = CustomerItemMapping(L);
       
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
        [Test, Order(5), Category("DanyaB"), Category(" ")]
        public void Order_Creation_Test()
        {
            string Screen = Screens["Order_Creation_Test"];
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
                string errorMsg = Page.Login(L,username ?? "", password ?? "", RememberMe);

                if (!string.IsNullOrEmpty(errorMsg))
                {
                    Logger.Error($"[STEP 2] Login failed: {errorMsg}");
                    Assert.Fail("Login failed – stopping test execution.");
                }
                Logger.Info("[STEP 2] Login successful.");

                // Step 3: Order creation based on client
                Logger.Info($"[STEP 3] Starting order creation for client: {L["ClientName"]}");
                errorMsg = CreateOrder(L);
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




        #region DanyaB Methods
        IWebDriver _driver;
        public string explore(Dictionary<string, string> L)
        {
            _driver = Driver;
            string error = string.Empty;
            int time = 3000;
            Logger.Info("Exploring application menus...");
            try
            {
                _driver.FindElement(By.LinkText(L["mainMenu_1"])).Click();
                Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_1_1"])).Click();
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_1_2"])).Click();
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_1_3"])).Click();
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_1_4"])).Click();
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_1_5"])).Click();
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_1_6"])).Click();
                //Thread.Sleep(time);
                //Logger.Info("Finished navigating first set of menus.");

                //_driver.FindElement(By.LinkText(L["mainMenu_2"])).Click();
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_2_1"])).Click();
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_2_2"])).Click();
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_2_3"])).Click();
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_2_4"])).Click();
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_2_5"])).Click();
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_2_6"])).Click();
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_2_7"])).Click();
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_2_8"])).Click();
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_2_9"])).Click();
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_2_10"])).Click();
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_2_11"])).Click();
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L["subMenu_2_12"])).Click();
                //Thread.Sleep(time);
                //Logger.Info("Finished navigating second set of menus.");

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
        public string AddCustomer(Dictionary<string, string> L)
        {
            _driver = Driver;
            var page = new CommonTestCasesHelper(Driver);
            string error = string.Empty;
            // Step 5: Navigate to Customers Page
            int time = 3000;
            Thread.Sleep(time);
            Logger.Info("Navigating to Customers page...");
            _driver.FindElement(By.PartialLinkText(L["subMenu_2_4"])).Click();
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
                    Logger.Info($"{customerIndex} Adding Customer: " +
                                $"Name='{details.CustomerName}', " +
                                $"Company='{details.CompanyName}', " +
                                $"Email='{details.CustomerEmailAddress}', " +
                                $"Phone='{details.CustomerPhoneNumber}', " +
                                $"SalesRep='{details.SalesRep}', " +
                                $"PT='{details.PT000872}'");
                    _driver.FindElement(By.Id(L["AddCustomerButton"])).Click();
                    Thread.Sleep(time);

                    // Fill Customer Details
                    _driver.FindElement(By.Id(L["Customer_customer_name"])).SendKeys(details.CustomerName);
                    _driver.FindElement(By.Id(L["Customer_company_name"])).SendKeys(details.CompanyName);
                    _driver.FindElement(By.Id(L["Customer_customer_phone_number"])).SendKeys(details.CustomerPhoneNumber);
                    _driver.FindElement(By.Id(L["Customer_customer_email_address"])).SendKeys(details.CustomerEmailAddress);
                    _driver.FindElement(By.Id(L["Customer_contact_first_name"])).SendKeys(details.ContactFirstName);
                    _driver.FindElement(By.Id(L["Customer_contact_last_name"])).SendKeys(details.ContactLastName);

                    // Dropdown: SalesRep
                    Logger.Info($"{customerIndex} Selecting SalesRep='{details.SalesRep}'");
                    IWebElement salesRepDropdown = _driver.FindElement(By.Id(L["Customer_sales_rep"]));
                    new SelectElement(salesRepDropdown).SelectByText(details.SalesRep);

                    // Dropdown: PT
                    Logger.Info($"{customerIndex} Selecting PT='{details.PT000872}'");
                    IWebElement ptDropdown = _driver.FindElement(By.Id(L["Customer_pt000872"]));
                    new SelectElement(ptDropdown).SelectByText(details.PT000872);

                    // TODO: Add Billing + Shipping details here if required

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
                        page.CloseErrorBox(L);
                        Thread.Sleep(time);
                        _driver.FindElement(By.PartialLinkText(L["subMenu_2_4"])).Click();
                        Thread.Sleep(time);
                        //Assert.Fail($"Failed to add customer '{details.CustomerName}': {error}");
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
        public string AddItem(Dictionary<string, string> L)
        {
            _driver = Driver;
            var page = new CommonTestCasesHelper(Driver);
            string error = string.Empty;
            // Step 3: Navigate to Item Page
            int time = 3000;
            Thread.Sleep(time);
            Logger.Info("Navigating to Items page...");
            _driver.FindElement(By.PartialLinkText(L["subMenu_2_3"])).Click();
            Thread.Sleep(time);
            // Step 4: Read Items.json
            //Logger.Info($"Reading JSON file: {ItemsFile}");
            var items = JsonConvert.DeserializeObject<ItemsModel>(ItemsFile);
            Logger.Info($"[INFO] Loaded {items.Items.Count} items from JSON.");
            try
            {
                int itemIndex = 1;
                foreach (var item in items.Items)
                {
                    var details = item.ItemDetails;
                    var dim = item.ItemDimensionsDetails;
                    var pack = item.ItemPackagingDetails;
                    var ware = item.WarehouseDetails;
                    Logger.Info($"{itemIndex} Adding Item: " +
                                $"Item='{details.Item}', " +
                                $"UPCCode='{details.UPCCode}', " +
                                $"ManufacturePart='{details.ManufacturePart}', " +
                                $"Inventory='{details.Inventory}', " +
                                $"ItemDescription='{details.ItemDescription}' Soon....");
                    _driver.FindElement(By.Id(L["AddItemButton"])).Click();
                    Thread.Sleep(time);

                    // Fill Item Details
                    _driver.FindElement(By.Id(L["ItemDetails_item_number"])).SendKeys(details.Item);
                    _driver.FindElement(By.Id(L["ItemDetails_upc_code"])).SendKeys(details.UPCCode);
                    _driver.FindElement(By.Id(L["ItemDetails_manufacture_part_number"])).SendKeys(details.ManufacturePart);
                    _driver.FindElement(By.Id(L["ItemDetails_inventory_number"])).SendKeys(details.Inventory);
                    _driver.FindElement(By.Id(L["ItemDetails_item_description"])).SendKeys(details.ItemDescription);
                    _driver.FindElement(By.Id(L["ItemDetails_notes"])).SendKeys(details.Notes);
                    new SelectElement(_driver.FindElement(By.Id(L["ItemDetails_item_type"]))).SelectByText(details.ItemType);
                    new SelectElement(_driver.FindElement(By.Id(L["ItemDetails_status"]))).SelectByText(details.Status);
                    new SelectElement(_driver.FindElement(By.Id(L["ItemDetails_taxed"]))).SelectByText(details.Taxed);
                    new SelectElement(_driver.FindElement(By.Id(L["ItemDetails_country_of_origin"]))).SelectByText(details.CountryOfOrigin);
                    _driver.FindElement(By.Id(L["ItemDetails_item_color"])).SendKeys(details.ItemColor);
                    _driver.FindElement(By.Id(L["ItemDetails_re_order_point"])).SendKeys(details.ReOrderPoint);
                    _driver.FindElement(By.Id(L["ItemDetails_supplier"])).SendKeys(details.Supplier);
                    _driver.FindElement(By.Id(L["ItemDetails_brand"])).SendKeys(details.Brand);
                    new SelectElement(_driver.FindElement(By.Id(L["ItemDetails_preferred_shipping_carrier"]))).SelectByText(details.PreferredShippingCarrier);
                    new SelectElement(_driver.FindElement(By.Id(L["ItemDetails_preferred_shipping_method"]))).SelectByText(details.PreferredShippingMethod);
                    new SelectElement(_driver.FindElement(By.Id(L["ItemDetails_usps_package_type"]))).SelectByText(details.USPSPackageType);
                    _driver.FindElement(By.Id(L["ItemDetails_picture_url"])).SendKeys(details.PictureUrl);
                    new SelectElement(_driver.FindElement(By.Id(L["ItemDetails_gender"]))).SelectByText(details.Gender);
                    _driver.FindElement(By.Id(L["ItemDetails_size"])).SendKeys(details.Size);
                    _driver.FindElement(By.Id(L["ItemDetails_seller_cost"])).SendKeys(details.SellerCost);
                    _driver.FindElement(By.Id(L["ItemDetails_price"])).SendKeys(details.Price);
                    new SelectElement(_driver.FindElement(By.Id(L["ItemDetails_ups_surepost"]))).SelectByText(details.UPSSurepost);
                    Thread.Sleep(time);

                    // Fill Item Dimensions
                    _driver.FindElement(By.Id(L["ItemDimensionsDetails_item_weight_lbs"])).SendKeys(dim.ItemWeight);
                    new SelectElement(_driver.FindElement(By.Id(L["ItemDimensionsDetails_item_weight_lbs_Unit"]))).SelectByText(dim.ItemWeightUnits);
                    _driver.FindElement(By.Id(L["ItemDimensionsDetails_item_length_inches"])).SendKeys(dim.ItemLength);
                    new SelectElement(_driver.FindElement(By.Id(L["ItemDimensionsDetails_item_length_inches_Unit"]))).SelectByText(dim.ItemLengthUnits);
                    _driver.FindElement(By.Id(L["ItemDimensionsDetails_item_width_inches"])).SendKeys(dim.ItemWidth);
                    new SelectElement(_driver.FindElement(By.Id(L["ItemDimensionsDetails_item_width_inches_Unit"]))).SelectByText(dim.ItemWidthUnits);
                    _driver.FindElement(By.Id(L["ItemDimensionsDetails_item_height_inches"])).SendKeys(dim.ItemHeight);
                    new SelectElement(_driver.FindElement(By.Id(L["ItemDimensionsDetails_item_height_inches_Unit"]))).SelectByText(dim.ItemHeightUnits);
                    Thread.Sleep(time);

                    // Fill Item Packaging
                    _driver.FindElement(By.Id(L["ItemPackagingDetails_package_length"])).SendKeys(pack.PackageLength);
                    new SelectElement(_driver.FindElement(By.Id(L["ItemPackagingDetails_package_length_Unit"]))).SelectByText(pack.PackageLengthUnits);
                    _driver.FindElement(By.Id(L["ItemPackagingDetails_package_width"])).SendKeys(pack.PackageWidth);
                    new SelectElement(_driver.FindElement(By.Id(L["ItemPackagingDetails_package_width_Unit"]))).SelectByText(pack.PackageWidthUnits);
                    _driver.FindElement(By.Id(L["ItemPackagingDetails_package_height"])).SendKeys(pack.PackageHeight);
                    new SelectElement(_driver.FindElement(By.Id(L["ItemPackagingDetails_package_height_Unit"]))).SelectByText(pack.PackageHeightUnits);
                    _driver.FindElement(By.Id(L["ItemPackagingDetails_item_volume"])).SendKeys(pack.ItemVolume);
                    new SelectElement(_driver.FindElement(By.Id(L["ItemPackagingDetails_item_volume_Unit"]))).SelectByText(pack.ItemVolumeUnits);
                    _driver.FindElement(By.Id(L["ItemPackagingDetails_package_weight"])).SendKeys(pack.PackageWeight);
                    new SelectElement(_driver.FindElement(By.Id(L["ItemPackagingDetails_package_weight_Unit"]))).SelectByText(pack.PackageWeightUnits);
                    //_driver.FindElement(By.Id(L["ItemPackagingDetails_quantity_in_stock"])).SendKeys(pack.QuantityinStock);
                    //new SelectElement(_driver.FindElement(By.Id(L["ItemPackagingDetails_quantity_in_stock_Unit"]))).SelectByText(pack.QuantityinStockUnits);
                    _driver.FindElement(By.Id(L["ItemPackagingDetails_min_order_qty"])).SendKeys(pack.MinOrderQty);
                    //new SelectElement(_driver.FindElement(By.Id(L["ItemPackagingDetails_min_order_qty_Unit"]))).SelectByText(pack.MinOrderQtyUnits);
                    _driver.FindElement(By.Id(L["ItemPackagingDetails_number_of_items_package"])).SendKeys(pack.NumberofItemsPackage);
                    Thread.Sleep(time);

                    // Fill Warehouse Details
                    //_driver.FindElement(By.XPath(L["WarehouseDetails_main_warehouse_quantity_in_stock"])).SendKeys(ware.Quantity);
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
                        page.CloseErrorBox(L);
                        Thread.Sleep(time);
                        _driver.FindElement(By.PartialLinkText(L["subMenu_2_3"])).Click();
                        Thread.Sleep(time);
                        //Assert.Fail($"Failed to add customer '{details.CustomerName}': {error}");
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
        public string CustomerItemMapping(Dictionary<string, string> L)
        {
            _driver = Driver;
            string error = string.Empty;
            Assert.Fail("CustomerItemMapping method Not implemented yet...");
            Logger.Info("CustomerItemMapping method Not implemented yet...");
            return error;
        }
        public string CreateOrder(Dictionary<string, string> L)
        {
            _driver = Driver;
            string error = string.Empty;
            Assert.Fail("CreateOrder method Not implemented yet...");
            Logger.Info("CreateOrder method Not implemented yet...");
            return error;
        }
        #endregion
    }
}
