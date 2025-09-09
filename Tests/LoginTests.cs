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
        private readonly Dictionary<string, string> L = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Config.Get("AppSettings:UIDataPointsFile")));
        private static IEnumerable<LoginTestCase> LoadCases()
        {
            string location = Config.Get("AppSettings:baseFolder");
            var path = Path.Combine(location, "LoginTestData.json");
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<List<LoginTestCase>>(json) ?? new();
        }

        public static IEnumerable<TestCaseData> Cases() =>
            LoadCases().Select((tc, i) => new TestCaseData(tc).SetName($"{i + 1}.Login_{tc.Expected}_{tc.Username ?? "EMPTY"}"));

        [Test, TestCaseSource(nameof(Cases))]
        public void Run_Login_Scenarios(LoginTestCase tc)
        {
            var startTime = DateTime.Now;
            string status = "Fail";
            Logger.Info($"Running test case: Username='{tc.Username}', Expected='{tc.Expected}'");

            var page = new LoginPage(Driver);
            page.Navigate();
            Logger.Info("Navigated to login page.");

            string errorMsg = page.Login(tc.Username ?? "", tc.Password ?? "", tc.RememberMe);
            if(string.IsNullOrEmpty(errorMsg))
            {
                page.explore();
            }            

            var expect = (tc.Expected ?? "Error").ToLowerInvariant();
            var successUrlPart = Config.Get("AppSettings:LogoutUrlContains");
            try
            {
                switch (expect)
                {
                    case "success":
                        if (!string.IsNullOrWhiteSpace(successUrlPart))
                        {
                            StringAssert.Contains(successUrlPart.ToLowerInvariant(), Driver.Url.ToLowerInvariant());
                            Logger.Info($"✅ Success: Redirected to expected URL {Driver.Url}");
                            status = "Pass";
                        }
                        else
                        {
                            Logger.Error($"❌ Failed: Expected URL '{successUrlPart}', but got '{Driver.Url}'");
                            Assert.Fail($"Expected URL '{successUrlPart}', but got '{Driver.Url}'");
                        }
                        break;

                    case "error":
                        Logger.Info($"Captured error message: {errorMsg}");

                        Assert.IsTrue(errorMsg.Contains("invalid") || errorMsg.Contains("locked"),
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
                string ClientName = Config.Get("AppSettings:ClientName");
                Logger.Info($"[TEST END] {TestContext.CurrentContext.Test.Name} for Client-{ClientName} | Status={status} | Duration={(endTime - startTime).TotalSeconds} sec");
                StatusLogger.LogTestStatus(TestContext.CurrentContext.Test.Name, startTime, endTime, status, ClientName);
                DbLogger.LogTestResult(TestID, TestContext.CurrentContext.Test.Name, startTime, endTime, status, ClientName);
            }
        }


        [Test]
        public void Add_Customers_Test()
        {
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
                var username = Config.Get("AppSettings:Username");
                var password = Config.Get("AppSettings:Password");
                bool RememberMe = Config.GetBool("AppSettings:RememberMe");

                string errorMsg = Page.Login(username ?? "", password ?? "",RememberMe);

                if (!string.IsNullOrEmpty(errorMsg))
                { 
                    status = "Fail";
                    Assert.Fail("Login failed – stopping test execution."); // 🚀 EARLY EXIT
                }

                // Step 5: Navigate to Customers Page
                int time = 3000;
                Thread.Sleep(time);
                Logger.Info("Navigating to Customers page...");
                _driver.FindElement(By.PartialLinkText(L["subMenu_2_4"])).Click();
                Thread.Sleep(time);

                // Step 6: Read Customers.json
                string location = Config.Get("AppSettings:baseFolder");
                var filePath = Path.Combine(location, "Customers.json");
                Logger.Info($"Reading JSON file: {filePath}");

                var jsonContent = File.ReadAllText(filePath);
                var customers = JsonConvert.DeserializeObject<CustomerModel>(jsonContent);
                Logger.Info($"[INFO] Loaded {customers.Customers.Count} customers from JSON.");

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
                    IWebElement salesRepDropdown = Driver.FindElement(By.Id(L["Customer_sales_rep"]));
                    new SelectElement(salesRepDropdown).SelectByText(details.SalesRep);

                    // Dropdown: PT
                    Logger.Info($"{customerIndex} Selecting PT='{details.PT000872}'");
                    IWebElement ptDropdown = Driver.FindElement(By.Id(L["Customer_pt000872"]));
                    new SelectElement(ptDropdown).SelectByText(details.PT000872);

                    // TODO: Add Billing + Shipping details here if required

                    Logger.Info($"{customerIndex} Saving customer...");
                    _driver.FindElement(By.Id(L["SaveCustomerButton"])).Click();


                    string error = Page.GetErrorMessage();

                    if (string.IsNullOrEmpty(error))
                    {
                        Thread.Sleep(time);
                        Logger.Info($"[INFO] Customer '{details.CustomerName}' saved successfully.");
                    }
                    else
                    {
                        Logger.Error($"[ERROR] Failed to add customer '{details.CustomerName}': due to - {error}");
                        Page.CloseErrorBox();

                        Thread.Sleep(time);

                        _driver.FindElement(By.PartialLinkText(L["subMenu_2_4"])).Click();
                        Thread.Sleep(time);
                        //Assert.Fail($"Failed to add customer '{details.CustomerName}': {error}");
                    }
                    customerIndex++;
                }

                // Step 8: Logout
                Logger.Info("Logging out...");
                Thread.Sleep(10000);
                _driver.FindElement(By.Id(L["LogoutButton"])).Click();
                Thread.Sleep(time);
                _driver.FindElement(By.Id(L["YesLogout"])).Click();
                Thread.Sleep(time);

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
                string ClientName = Config.Get("AppSettings:ClientName");
                Logger.Info($"[TEST END] {TestContext.CurrentContext.Test.Name} for Client-{ClientName} | Status={status} | Duration={(endTime - startTime).TotalSeconds} sec");

                StatusLogger.LogTestStatus(TestContext.CurrentContext.Test.Name, startTime, endTime, status, ClientName);
                DbLogger.LogTestResult(TestID, TestContext.CurrentContext.Test.Name, startTime, endTime, status, ClientName);
            }
        }

        [Test]
        public void Add_Items_Test()
        {
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
                var username = Config.Get("AppSettings:Username");
                var password = Config.Get("AppSettings:Password");
                bool RememberMe = Config.GetBool("AppSettings:RememberMe");

                string errorMsg = Page.Login(username ?? "", password ?? "", RememberMe);

                if (!string.IsNullOrEmpty(errorMsg))
                {
                    status = "Fail";
                    Assert.Fail("Login failed – stopping test execution."); // 🚀 EARLY EXIT
                }

                // Step 3: Navigate to Item Page
                int time = 3000;
                Thread.Sleep(time);
                Logger.Info("Navigating to Items page...");
                _driver.FindElement(By.PartialLinkText(L["subMenu_2_3"])).Click();
                Thread.Sleep(time);

                // Step 4: Read Customers.json
                string location = Config.Get("AppSettings:baseFolder");
                var filePath = Path.Combine(location, "Items.json");
                Logger.Info($"Reading JSON file: {filePath}");

                var jsonContent = File.ReadAllText(filePath);
                var items = JsonConvert.DeserializeObject<ItemsModel>(jsonContent);
                Logger.Info($"[INFO] Loaded {items.Items.Count} items from JSON.");

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
                    new SelectElement(Driver.FindElement(By.Id(L["ItemDetails_item_type"]))).SelectByText(details.ItemType);
                    new SelectElement(Driver.FindElement(By.Id(L["ItemDetails_status"]))).SelectByText(details.Status);
                    new SelectElement(Driver.FindElement(By.Id(L["ItemDetails_taxed"]))).SelectByText(details.Taxed);
                    new SelectElement(Driver.FindElement(By.Id(L["ItemDetails_country_of_origin"]))).SelectByText(details.CountryOfOrigin);
                    _driver.FindElement(By.Id(L["ItemDetails_item_color"])).SendKeys(details.ItemColor);
                    _driver.FindElement(By.Id(L["ItemDetails_re_order_point"])).SendKeys(details.ReOrderPoint);
                    _driver.FindElement(By.Id(L["ItemDetails_supplier"])).SendKeys(details.Supplier);
                    _driver.FindElement(By.Id(L["ItemDetails_brand"])).SendKeys(details.Brand);
                    new SelectElement(Driver.FindElement(By.Id(L["ItemDetails_preferred_shipping_carrier"]))).SelectByText(details.PreferredShippingCarrier);
                    new SelectElement(Driver.FindElement(By.Id(L["ItemDetails_preferred_shipping_method"]))).SelectByText(details.PreferredShippingMethod);
                    new SelectElement(Driver.FindElement(By.Id(L["ItemDetails_usps_package_type"]))).SelectByText(details.USPSPackageType);
                    _driver.FindElement(By.Id(L["ItemDetails_picture_url"])).SendKeys(details.PictureUrl);
                    new SelectElement(Driver.FindElement(By.Id(L["ItemDetails_gender"]))).SelectByText(details.Gender);
                    _driver.FindElement(By.Id(L["ItemDetails_size"])).SendKeys(details.Size);
                    _driver.FindElement(By.Id(L["ItemDetails_seller_cost"])).SendKeys(details.SellerCost);
                    _driver.FindElement(By.Id(L["ItemDetails_price"])).SendKeys(details.Price);
                    new SelectElement(Driver.FindElement(By.Id(L["ItemDetails_ups_surepost"]))).SelectByText(details.UPSSurepost);
                    Thread.Sleep(time);

                    // Fill Item Dimensions
                    _driver.FindElement(By.Id(L["ItemDimensionsDetails_item_weight_lbs"])).SendKeys(dim.ItemWeight);
                    new SelectElement(Driver.FindElement(By.Id(L["ItemDimensionsDetails_item_weight_lbs_Unit"]))).SelectByText(dim.ItemWeightUnits);
                    _driver.FindElement(By.Id(L["ItemDimensionsDetails_item_length_inches"])).SendKeys(dim.ItemLength);
                    new SelectElement(Driver.FindElement(By.Id(L["ItemDimensionsDetails_item_length_inches_Unit"]))).SelectByText(dim.ItemLengthUnits);
                    _driver.FindElement(By.Id(L["ItemDimensionsDetails_item_width_inches"])).SendKeys(dim.ItemWidth);
                    new SelectElement(Driver.FindElement(By.Id(L["ItemDimensionsDetails_item_width_inches_Unit"]))).SelectByText(dim.ItemWidthUnits);
                   _driver.FindElement(By.Id(L["ItemDimensionsDetails_item_height_inches"])).SendKeys(dim.ItemHeight);
                    new SelectElement(Driver.FindElement(By.Id(L["ItemDimensionsDetails_item_height_inches_Unit"]))).SelectByText(dim.ItemHeightUnits);
                    Thread.Sleep(time);

                    // Fill Item Packaging
                    _driver.FindElement(By.Id(L["ItemPackagingDetails_package_length"])).SendKeys(pack.PackageLength);
                    new SelectElement(Driver.FindElement(By.Id(L["ItemPackagingDetails_package_length_Unit"]))).SelectByText(pack.PackageLengthUnits);
                    _driver.FindElement(By.Id(L["ItemPackagingDetails_package_width"])).SendKeys(pack.PackageWidth);
                    new SelectElement(Driver.FindElement(By.Id(L["ItemPackagingDetails_package_width_Unit"]))).SelectByText(pack.PackageWidthUnits);
                    _driver.FindElement(By.Id(L["ItemPackagingDetails_package_height"])).SendKeys(pack.PackageHeight);
                    new SelectElement(Driver.FindElement(By.Id(L["ItemPackagingDetails_package_height_Unit"]))).SelectByText(pack.PackageHeightUnits);
                    _driver.FindElement(By.Id(L["ItemPackagingDetails_item_volume"])).SendKeys(pack.ItemVolume);
                    new SelectElement(Driver.FindElement(By.Id(L["ItemPackagingDetails_item_volume_Unit"]))).SelectByText(pack.ItemVolumeUnits);
                    _driver.FindElement(By.Id(L["ItemPackagingDetails_package_weight"])).SendKeys(pack.PackageWeight);
                    new SelectElement(Driver.FindElement(By.Id(L["ItemPackagingDetails_package_weight_Unit"]))).SelectByText(pack.PackageWeightUnits);
                    //_driver.FindElement(By.Id(L["ItemPackagingDetails_quantity_in_stock"])).SendKeys(pack.QuantityinStock);
                    //new SelectElement(Driver.FindElement(By.Id(L["ItemPackagingDetails_quantity_in_stock_Unit"]))).SelectByText(pack.QuantityinStockUnits);
                    _driver.FindElement(By.Id(L["ItemPackagingDetails_min_order_qty"])).SendKeys(pack.MinOrderQty);
                    //new SelectElement(Driver.FindElement(By.Id(L["ItemPackagingDetails_min_order_qty_Unit"]))).SelectByText(pack.MinOrderQtyUnits);
                    _driver.FindElement(By.Id(L["ItemPackagingDetails_number_of_items_package"])).SendKeys(pack.NumberofItemsPackage);
                    Thread.Sleep(time);

                    // Fill Warehouse Details
                    //_driver.FindElement(By.XPath(L["WarehouseDetails_main_warehouse_quantity_in_stock"])).SendKeys(ware.Quantity);
                    Thread.Sleep(time);


                    Logger.Info($"{itemIndex} Saving Item...");
                    _driver.FindElement(By.Id(L["SaveItemButton"])).Click(); 
                    Thread.Sleep(time);

                    string error = Page.GetErrorMessage();

                    if (string.IsNullOrEmpty(error))
                    {
                        Thread.Sleep(time);
                        Logger.Info($"[INFO] Item '{details.Item}' saved successfully.");
                    }
                    else
                    {
                        Logger.Error($"[ERROR] Failed to add Item '{details.Item}': due to - {error}");
                        Page.CloseErrorBox();

                        Thread.Sleep(time);

                        _driver.FindElement(By.PartialLinkText(L["subMenu_2_4"])).Click();
                        Thread.Sleep(time);
                        //Assert.Fail($"Failed to add customer '{details.CustomerName}': {error}");
                    }

                    itemIndex++;
                }

                // Step 5: Logout
                Logger.Info("Logging out...");
                Thread.Sleep(10000);
                _driver.FindElement(By.Id(L["LogoutButton"])).Click();
                Thread.Sleep(time);
                _driver.FindElement(By.Id(L["YesLogout"])).Click();
                Thread.Sleep(time);

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
                string ClientName = Config.Get("AppSettings:ClientName");
                Logger.Info($"[TEST END] {TestContext.CurrentContext.Test.Name} for Client-{ClientName} | Status={status} | Duration={(endTime - startTime).TotalSeconds} sec");

                StatusLogger.LogTestStatus(TestContext.CurrentContext.Test.Name, startTime, endTime, status, ClientName);
                DbLogger.LogTestResult(TestID, TestContext.CurrentContext.Test.Name, startTime, endTime, status, ClientName);
            }
        }
    }
}
