using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium;
using System;
using System.Linq;
using System.Threading;
using LoginAutomation.Tests.Utils;
using Newtonsoft.Json;
using OpenQA.Selenium.DevTools.V137.Debugger;
using System.IO;
using LoginAutomation.Tests.Models;
using System.Timers;
using System.Xml.Linq;

namespace LoginAutomation.Tests.Pages
{
    public class LoginPage
    {
        private readonly IWebDriver _driver;      
        private static readonly string projectRoot = Directory.GetParent(AppContext.BaseDirectory).Parent.Parent.Parent.FullName;// Start from bin\Debug\net8.0   Go up 3 levels → project root
       
        private static readonly Dictionary<string, string> DanyaB = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(projectRoot, Config.Get("AppSettings:DanyaBDataPointsFile"))));
        private static readonly Dictionary<string, string> Capital = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(projectRoot, Config.Get("AppSettings:CapitalDataPointsFile"))));

        Dictionary<string, string> L = Capital; //JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(projectRoot, Config.Get("AppSettings:DataPointsFile"))));

        private readonly string CustomersFile = File.ReadAllText(Path.Combine(projectRoot, Config.Get("AppSettings:CustomerData")));
        private readonly string ItemsFile = File.ReadAllText(Path.Combine(projectRoot, Config.Get("AppSettings:ItemData")));
        private readonly string OrderFile = File.ReadAllText(Path.Combine(projectRoot, Config.Get("AppSettings:OrderData")));

        public LoginPage(IWebDriver driver)
        {
            _driver = driver;
        }

        public void Navigate()
        {
            var url = L["BaseUrl"];
            Logger.Info($"Navigating to: {url}");
            _driver.Navigate().GoToUrl(url);
            _driver.Manage().Window.Maximize();
        }
        public string Login(string username, string password, bool rememberMe = false)
        { 
            Logger.Info($"Attempting login with Username='{username}' and Password={password}");

            _driver.FindElement(By.Id(L["Username"])).SendKeys(username);
            _driver.FindElement(By.Id(L["Password"])).SendKeys(password);

            var remember = L["RememberMe"];
            //rememberMe = true;
            if (!string.IsNullOrWhiteSpace(remember) && rememberMe)
            {
                var checkbox = _driver.FindElement(By.CssSelector(remember));
                if (!checkbox.Selected)
                {
                    checkbox.Click();
                    Logger.Info("RememberMe checkbox selected.");
                }
            }

            _driver.FindElement(By.Id(L["LoginButton"])).Submit();
            Logger.Info("Login button submitted.");

            var successUrlPart = L["SuccessUrlContains"];

            // Wait up to 5 seconds for either success URL or error message
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(3));
            bool success = false;
            var error = string.Empty;

            error = GetErrorMessage();
            if (string.IsNullOrEmpty(error))
            {
                error = GetValidationMessage();
            }

            if (string.IsNullOrEmpty(error))
            {
                try
                {
                    success = wait.Until(d => d.Url.ToLowerInvariant().Contains(successUrlPart.ToLowerInvariant()));
                }
                catch (WebDriverTimeoutException)
                {
                    // If URL didn't change in 5s, we’ll check for errors
                    error = "Login failed due to timeout...";
                    Logger.Warn("Login failed due to timeout...");
                }
            }
            else
            {
                Logger.Warn($"Login failed with error: {error}");
            }


            if (success)
            {
                Logger.Info("Login successful. Navigating through application...");
            }


            return error;
        }
        public void logout()
        {
            // Step 8: Logout
            Logger.Info("Logging out...");
            Thread.Sleep(5000);
            _driver.FindElement(By.Id(L["LogoutButton"])).Click();
            Thread.Sleep(3000);
            _driver.FindElement(By.Id(L["YesLogout"])).Click();
            Thread.Sleep(3000);
        }
        public string GetErrorMessage()
        {
            try
            {
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
                var errorElement = wait.Until(d =>
                {
                    var elems = d.FindElements(By.CssSelector(".alert.alert-danger.alert-dismissable"));
                    return elems.FirstOrDefault(e => e.Displayed && !string.IsNullOrWhiteSpace(e.Text));
                });

                if (errorElement == null) return string.Empty;

                // Remove close button text ("×") explicitly
                var fullText = errorElement.Text.Trim();
                var button = errorElement.FindElements(By.TagName("button")).FirstOrDefault();

                if (button != null)
                {
                    var buttonText = button.Text.Trim();
                    if (!string.IsNullOrWhiteSpace(buttonText))
                    {
                        fullText = fullText.Replace(buttonText, "").Trim();
                    }
                }

                return fullText;
            }
            catch (WebDriverTimeoutException)
            {
                Logger.Info("No error message displayed within timeout.");
                return string.Empty;
            }
        }
        public string GetValidationMessage()
        {
            try
            {
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));

                // Collect possible validation messages
                var validationElements = wait.Until(d =>
                {
                    var elems = new List<IWebElement>();

                    // Username validation
                    elems.AddRange(d.FindElements(By.CssSelector(L["UsernameValidation"])));

                    // Password validation
                    elems.AddRange(d.FindElements(By.CssSelector(L["PasswordValidation"])));

                    return elems
                        .Where(e => e.Displayed && !string.IsNullOrWhiteSpace(e.Text))
                        .ToList();
                });

                if (!validationElements.Any()) return string.Empty;

                var messages = new List<string>();

                foreach (var elem in validationElements)
                {
                    var text = elem.Text.Trim();
                    var button = elem.FindElements(By.TagName("button")).FirstOrDefault();

                    if (button != null)
                    {
                        var buttonText = button.Text.Trim();
                        if (!string.IsNullOrWhiteSpace(buttonText))
                        {
                            text = text.Replace(buttonText, "").Trim();
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(text))
                        messages.Add(text);
                }

                return string.Join(" , ", messages);
            }
            catch (WebDriverTimeoutException)
            {
                Logger.Info("No validation message displayed within timeout.");
                return string.Empty;
            }
        }
        public void CloseErrorBox()
        {
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
            // Wait until the alert is visible
            var alert = wait.Until(d => d.FindElement(By.CssSelector(L["Invalid"])));
            // Find the close button inside the alert
            var closeBtn = alert.FindElement(By.CssSelector("button.close"));
            // Click the X button
            closeBtn.Click();
        }

        #region DanyaB Methods
        public void explore()
        {
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
                throw;
            }
        }
        public void AddCustomer()
        {
            // Step 5: Navigate to Customers Page
            int time = 3000;
            Thread.Sleep(time);
            Logger.Info("Navigating to Customers page...");
            _driver.FindElement(By.PartialLinkText(L["subMenu_2_4"])).Click();
            Thread.Sleep(time);

            // Step 6: Read Customers.json
            Logger.Info($"Reading JSON file: {CustomersFile}");
            var customers = JsonConvert.DeserializeObject<CustomerModel>(CustomersFile);
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
                IWebElement salesRepDropdown = _driver.FindElement(By.Id(L["Customer_sales_rep"]));
                new SelectElement(salesRepDropdown).SelectByText(details.SalesRep);

                // Dropdown: PT
                Logger.Info($"{customerIndex} Selecting PT='{details.PT000872}'");
                IWebElement ptDropdown = _driver.FindElement(By.Id(L["Customer_pt000872"]));
                new SelectElement(ptDropdown).SelectByText(details.PT000872);

                // TODO: Add Billing + Shipping details here if required

                Logger.Info($"{customerIndex} Saving customer...");
                _driver.FindElement(By.Id(L["SaveCustomerButton"])).Click();


                string error = GetErrorMessage();

                if (string.IsNullOrEmpty(error))
                {
                    Thread.Sleep(time);
                    Logger.Info($"[INFO] Customer '{details.CustomerName}' saved successfully.");
                }
                else
                {
                    Logger.Error($"[ERROR] Failed to add customer '{details.CustomerName}': due to - {error}");
                    CloseErrorBox();

                    Thread.Sleep(time);

                    _driver.FindElement(By.PartialLinkText(L["subMenu_2_4"])).Click();
                    Thread.Sleep(time);
                    //Assert.Fail($"Failed to add customer '{details.CustomerName}': {error}");
                }
                customerIndex++;
            }

        }
        public void AddItem()
        {
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

                string error = GetErrorMessage();

                if (string.IsNullOrEmpty(error))
                {
                    Thread.Sleep(time);
                    Logger.Info($"[INFO] Item '{details.Item}' saved successfully.");
                }
                else
                {
                    Logger.Error($"[ERROR] Failed to add Item '{details.Item}': due to - {error}");
                    CloseErrorBox();

                    Thread.Sleep(time);

                    _driver.FindElement(By.PartialLinkText(L["subMenu_2_3"])).Click();
                    Thread.Sleep(time);
                    //Assert.Fail($"Failed to add customer '{details.CustomerName}': {error}");
                }

                itemIndex++;
            }
        }
        public void CreateOrder()
        {
            Assert.Fail("CreateOrder method Not implemented yet...");
            Logger.Info("CreateOrder method Not implemented yet...");
        }
        #endregion

        #region Capital Methods
        public void Capital_explore()
        {
            int time = 3000;
            Logger.Info("Exploring application menus...");

            try
            {
                _driver.FindElement(By.LinkText(L["mainMenu_1"])).Click();
                Thread.Sleep(time);
                _driver.FindElement(By.PartialLinkText(L["subMenu_1_1"])).Click();
                Thread.Sleep(time);
                Logger.Info("Finished navigating 1st set of menus.");

                _driver.FindElement(By.LinkText(L["mainMenu_2"])).Click();
                Thread.Sleep(time);
                _driver.FindElement(By.PartialLinkText(L["subMenu_2_1"])).Click();
                Thread.Sleep(time);
                _driver.FindElement(By.PartialLinkText(L["subMenu_2_2"])).Click();
                Thread.Sleep(time);
                _driver.FindElement(By.PartialLinkText(L["subMenu_2_3"])).Click();
                Thread.Sleep(time);
                Logger.Info("Finished navigating 2nd set of menus.");


                _driver.FindElement(By.LinkText(L["mainMenu_3"])).Click();
                Thread.Sleep(time);
                _driver.FindElement(By.PartialLinkText(L["subMenu_3_1"])).Click();
                Thread.Sleep(time);
                _driver.FindElement(By.PartialLinkText(L["subMenu_3_2"])).Click();
                Thread.Sleep(time);
                _driver.FindElement(By.PartialLinkText(L["subMenu_3_3"])).Click();
                Thread.Sleep(time);
                Logger.Info("Finished navigating 3rd set of menus.");


                _driver.FindElement(By.LinkText(L["mainMenu_4"])).Click();
                Thread.Sleep(time);
                _driver.FindElement(By.PartialLinkText(L["subMenu_4_2"])).Click();
                Thread.Sleep(time);
                _driver.FindElement(By.PartialLinkText(L["subMenu_4_3"])).Click();
                Thread.Sleep(time);
                _driver.FindElement(By.PartialLinkText(L["subMenu_4_4"])).Click();
                Thread.Sleep(time);
                _driver.FindElement(By.PartialLinkText(L["subMenu_4_5"])).Click();
                Thread.Sleep(time);
                _driver.FindElement(By.PartialLinkText(L["subMenu_4_6"])).Click();
                Thread.Sleep(time);
                Logger.Info("Finished navigating 4th set of menus.");



                _driver.FindElement(By.Id(L["LogoutButton"])).Click();
                Thread.Sleep(time);
                _driver.FindElement(By.Id(L["NoLogout"])).Click();
                Thread.Sleep(time);
                _driver.FindElement(By.Id(L["LogoutButton"])).Click();
                Thread.Sleep(time);
                _driver.FindElement(By.Id(L["YesLogout"])).Click();
                Thread.Sleep(time);

                Logger.Info("Logout sequence completed successfully.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error while exploring menus: {ex.Message}");
                throw;
            }
        }
        public void Capital_AddItem()
        {
            // Step 3: Navigate to Item Page
            int time = 3000;
            Thread.Sleep(time);
            Logger.Info("Navigating to Items page...");
            _driver.FindElement(By.LinkText(L["mainMenu_3"])).Click();
            Thread.Sleep(time);
            _driver.FindElement(By.PartialLinkText(L["subMenu_3_1"])).Click();
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

                string error = GetErrorMessage();

                if (string.IsNullOrEmpty(error))
                {
                    Thread.Sleep(time);
                    Logger.Info($"[INFO] Item '{details.Item}' saved successfully.");
                }
                else
                {
                    Logger.Error($"[ERROR] Failed to add Item '{details.Item}': due to - {error}");
                    CloseErrorBox();

                    Thread.Sleep(time);

                    _driver.FindElement(By.PartialLinkText(L["subMenu_2_3"])).Click();
                    Thread.Sleep(time);
                    //Assert.Fail($"Failed to add customer '{details.CustomerName}': {error}");
                }

                itemIndex++;
            }
        }
        public string Capital_CreateOrder()
        {
            string error = string.Empty;
            int time = 3000;
            Thread.Sleep(time);
            try
            {
                Logger.Info("[ORDER FLOW] Navigating to Orders page...");
                _driver.FindElement(By.LinkText(L["mainMenu_4"])).Click();
                Thread.Sleep(time);
                _driver.FindElement(By.PartialLinkText(L["subMenu_4_1"])).Click();
                Thread.Sleep(time);
                _driver.FindElement(By.Id(L["AddOrderButton"])).Click();
                Thread.Sleep(time);
                Logger.Info("[ORDER FLOW] Orders page loaded.");

                var steps = _driver.FindElements(By.CssSelector("ul.steps > li"));
                Logger.Info($"[ORDER FLOW] Found {steps.Count} order steps on screen.");

                foreach (var step in steps)
                {
                    Thread.Sleep(time);
                    var badge = step.FindElement(By.CssSelector("span.badge"));
                    string badgeClass = badge.GetAttribute("class");
                    string stepNumber = badge.Text.Trim();
                    string stepName = step.FindElement(By.CssSelector("small")).Text.Trim();

                    Logger.Info($"[ORDER STEP] Step {stepNumber} ({stepName}) → Status class: {badgeClass}");

                    if (badgeClass.Contains("badge-warning"))
                    {
                        Logger.Warn($"[ORDER STEP] Step {stepNumber} is in WARNING state, taking action...");

                        switch (stepNumber)
                        {
                            case "1":
                                Logger.Info("[STEP 1] Adding order details...");
                                error = addOrderDetails();
                                if (!string.IsNullOrEmpty(error))
                                {
                                    Logger.Error($"Error adding order details: {error}");
                                    return error;
                                }
                                _driver.FindElement(By.XPath("//button[contains(.,'Finalize')]")).Click();
                                error += GetErrorMessage();
                                if (!string.IsNullOrEmpty(error))
                                {
                                    Logger.Error($"Error finalizing order: {error}");
                                    return error;
                                }
                                Thread.Sleep(time);
                                Logger.Info("[STEP 1] Finalize clicked.");
                                break;

                            case "2":
                                Logger.Info("[STEP 2] Clicking Pack button...");
                                TryClick("//button[contains(.,'Pack')]", "Pack");
                                error += GetErrorMessage();
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
                                bool shippedClicked = TryClickWithRetry("//button[contains(.,'Mark Shipped')]", "Mark Shipped", 3, 60000);
                                error += GetErrorMessage();
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
                                TryClick("//button[contains(.,'Delivered')]", "Delivered");
                                break;
                        }
                    }
                }

                return error;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                Logger.Error($"Error in CreateOrder: {ex.Message}");
                return error;
            }
        }

        private void TryClick(string xpath, string buttonName)
        {
            try
            {
                _driver.FindElement(By.XPath(xpath)).Click();
                Logger.Info($"[CLICK] {buttonName} button clicked successfully.");
                Thread.Sleep(3000);
            }
            catch (NoSuchElementException)
            {
                Logger.Warn($"[CLICK] {buttonName} button not found.");
            }
        }
        private bool TryClickWithRetry(string xpath, string buttonName, int attempts, int waitMs)
        {
            for (int i = 0; i < attempts; i++)
            {
                try
                {
                    _driver.FindElement(By.XPath(xpath)).Click();
                    Logger.Info($"[CLICK] {buttonName} button clicked successfully.");
                    return true;
                }
                catch (NoSuchElementException)
                {
                    Logger.Info($"[CLICK] {buttonName} not found. Retrying in {waitMs / 1000}s...");
                    Thread.Sleep(waitMs);
                    _driver.Navigate().Refresh();
                    Thread.Sleep(3000);
                }
            }
            return false;
        }
        public string addOrderDetails()
        {
            string error = string.Empty;
            var OrderDetails = JsonConvert.DeserializeObject<OrdersModel>(OrderFile);
            int time = 3000;

            try
            {
                Logger.Info($"[ORDER DETAILS] Starting to fill order for customer: {OrderDetails.Order.SelectCustomerDetails.CustomerName}");

                // Customer
                Logger.Info("[ORDER DETAILS] Selecting customer...");

                _driver.FindElement(By.Id(L["SelectCustomer"])).Click();

                // Wait for the iframe to appear
                var iframe = new WebDriverWait(_driver, TimeSpan.FromSeconds(10))
                    .Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.CssSelector("iframe.cboxIframe")));
                // Switch to the iframe
                _driver.SwitchTo().Frame(iframe);
                Thread.Sleep(time);

                _driver.FindElement(By.Id(L["SearchCustomer"])).SendKeys(OrderDetails.Order.SelectCustomerDetails.CustomerName);
                _driver.FindElement(By.Id(L["SearchCustomerButton"])).Click();
                Thread.Sleep(time);
                IWebElement customerRow = _driver.FindElement(By.XPath($"//div[@id='bodyDiv']//div[@id='customerName'][contains(normalize-space(.), '{OrderDetails.Order.SelectCustomerDetails.CustomerName}')]"));
                customerRow.FindElement(By.XPath(".//input[@type='radio']")).Click();
                Thread.Sleep(time);
                _driver.FindElement(By.Id(L["SearchCustomerSubmit"])).Click();

                Logger.Info("[ORDER DETAILS] Customer selected.");
                // When done, switch back to the main document
                _driver.SwitchTo().DefaultContent();
                Thread.Sleep(time);

                // Order info
                Logger.Info("[ORDER DETAILS] Filling order metadata...");

                _driver.FindElement(By.Id(L["OrderDetails_po_number"])).SendKeys(OrderDetails.Order.OrderDetails.po_number);
                _driver.FindElement(By.Id(L["OrderDetails_customer_reference_number"])).SendKeys(OrderDetails.Order.OrderDetails.customer_reference_number);
                Thread.Sleep(time);
                new SelectElement(_driver.FindElement(By.Id(L["OrderDetails_fulfillment_type"]))).SelectByText(OrderDetails.Order.OrderDetails.fulfillment_type);
                Thread.Sleep(time);
                new SelectElement(_driver.FindElement(By.Id(L["OrderDetails_shipping_method"]))).SelectByText(OrderDetails.Order.OrderDetails.shipping_method);
                Thread.Sleep(time);
                _driver.FindElement(By.Id(L["OrderDetails_account_number"])).SendKeys(OrderDetails.Order.OrderDetails.account_number);
                _driver.FindElement(By.Id(L["OrderDetails_reference_1"])).SendKeys(OrderDetails.Order.OrderDetails.reference_1);
                _driver.FindElement(By.Id(L["OrderDetails_reference_2"])).SendKeys(OrderDetails.Order.OrderDetails.reference_2);
                _driver.FindElement(By.Id(L["OrderDetails_customer_message"])).SendKeys(OrderDetails.Order.OrderDetails.customer_message);

                Logger.Info("[ORDER DETAILS] Order metadata filled.");
                Thread.Sleep(time);


                Logger.Info("[ORDER DETAILS] Filling addresses...");
                // Ship To
                _driver.FindElement(By.Id(L["ShipTo_first_name"])).SendKeys(OrderDetails.Order.ShipTo.first_name);
                _driver.FindElement(By.Id(L["ShipTo_last_name"])).SendKeys(OrderDetails.Order.ShipTo.last_name);
                _driver.FindElement(By.Id(L["ShipTo_phone"])).SendKeys(OrderDetails.Order.ShipTo.phone);
                _driver.FindElement(By.Id(L["ShipTo_email"])).SendKeys(OrderDetails.Order.ShipTo.email);
                _driver.FindElement(By.Id(L["ShipTo_address_1"])).SendKeys(OrderDetails.Order.ShipTo.address_1);
                _driver.FindElement(By.Id(L["ShipTo_address_2"])).SendKeys(OrderDetails.Order.ShipTo.address_2);
                new SelectElement(_driver.FindElement(By.Id(L["ShipTo_country"]))).SelectByText(OrderDetails.Order.ShipTo.country);
                _driver.FindElement(By.Id(L["ShipTo_city"])).SendKeys(OrderDetails.Order.ShipTo.city);
                new SelectElement(_driver.FindElement(By.Id(L["ShipTo_state"]))).SelectByText(OrderDetails.Order.ShipTo.state);
                _driver.FindElement(By.Id(L["ShipTo_zip"])).SendKeys(OrderDetails.Order.ShipTo.zip);
                _driver.FindElement(By.Id(L["ShipTo_notes"])).SendKeys(OrderDetails.Order.ShipTo.notes);
                Thread.Sleep(time);
                // Bill To
                _driver.FindElement(By.Id(L["BillTo_first_name"])).Clear();
                _driver.FindElement(By.Id(L["BillTo_first_name"])).SendKeys(OrderDetails.Order.BillTo.first_name);
                _driver.FindElement(By.Id(L["BillTo_last_name"])).Clear();
                _driver.FindElement(By.Id(L["BillTo_last_name"])).SendKeys(OrderDetails.Order.BillTo.last_name);
                _driver.FindElement(By.Id(L["BillTo_phone"])).Clear();
                _driver.FindElement(By.Id(L["BillTo_phone"])).SendKeys(OrderDetails.Order.BillTo.phone);
                _driver.FindElement(By.Id(L["BillTo_email"])).Clear();
                _driver.FindElement(By.Id(L["BillTo_email"])).SendKeys(OrderDetails.Order.BillTo.email);
                _driver.FindElement(By.Id(L["BillTo_address_1"])).Clear();
                _driver.FindElement(By.Id(L["BillTo_address_1"])).SendKeys(OrderDetails.Order.BillTo.address_1);
                _driver.FindElement(By.Id(L["BillTo_address_2"])).Clear();
                _driver.FindElement(By.Id(L["BillTo_address_2"])).SendKeys(OrderDetails.Order.BillTo.address_2);
                new SelectElement(_driver.FindElement(By.Id(L["BillTo_country"]))).SelectByText(OrderDetails.Order.BillTo.country);
                _driver.FindElement(By.Id(L["BillTo_city"])).Clear();
                _driver.FindElement(By.Id(L["BillTo_city"])).SendKeys(OrderDetails.Order.BillTo.city);
                new SelectElement(_driver.FindElement(By.Id(L["BillTo_state"]))).SelectByText(OrderDetails.Order.BillTo.state);
                _driver.FindElement(By.Id(L["BillTo_zip"])).Clear();
                _driver.FindElement(By.Id(L["BillTo_zip"])).SendKeys(OrderDetails.Order.BillTo.zip);
                _driver.FindElement(By.Id(L["BillTo_notes"])).Clear();
                _driver.FindElement(By.Id(L["BillTo_notes"])).SendKeys(OrderDetails.Order.BillTo.notes);
                Thread.Sleep(time);
                // Ship From
                _driver.FindElement(By.Id(L["ShipFrom_first_name"])).Clear();
                _driver.FindElement(By.Id(L["ShipFrom_first_name"])).SendKeys(OrderDetails.Order.ShipFrom.first_name);
                _driver.FindElement(By.Id(L["ShipFrom_last_name"])).Clear();
                _driver.FindElement(By.Id(L["ShipFrom_last_name"])).SendKeys(OrderDetails.Order.ShipFrom.last_name);
                _driver.FindElement(By.Id(L["ShipFrom_phone"])).Clear();
                _driver.FindElement(By.Id(L["ShipFrom_phone"])).SendKeys(OrderDetails.Order.ShipFrom.phone);
                _driver.FindElement(By.Id(L["ShipFrom_email"])).Clear();
                _driver.FindElement(By.Id(L["ShipFrom_email"])).SendKeys(OrderDetails.Order.ShipFrom.email);
                _driver.FindElement(By.Id(L["ShipFrom_address_1"])).Clear();
                _driver.FindElement(By.Id(L["ShipFrom_address_1"])).SendKeys(OrderDetails.Order.ShipFrom.address_1);
                _driver.FindElement(By.Id(L["ShipFrom_address_2"])).Clear();
                _driver.FindElement(By.Id(L["ShipFrom_address_2"])).SendKeys(OrderDetails.Order.ShipFrom.address_2);
                new SelectElement(_driver.FindElement(By.Id(L["ShipFrom_country"]))).SelectByText(OrderDetails.Order.ShipFrom.country);
                _driver.FindElement(By.Id(L["ShipFrom_city"])).Clear();
                _driver.FindElement(By.Id(L["ShipFrom_city"])).SendKeys(OrderDetails.Order.ShipFrom.city);
                new SelectElement(_driver.FindElement(By.Id(L["ShipFrom_state"]))).SelectByText(OrderDetails.Order.ShipFrom.state);
                _driver.FindElement(By.Id(L["ShipFrom_zip"])).Clear();
                _driver.FindElement(By.Id(L["ShipFrom_zip"])).SendKeys(OrderDetails.Order.ShipFrom.zip);
                _driver.FindElement(By.Id(L["ShipFrom_notes"])).Clear();
                _driver.FindElement(By.Id(L["ShipFrom_notes"])).SendKeys(OrderDetails.Order.ShipFrom.notes);

                Logger.Info("[ORDER DETAILS] All addresses filled.");
                Thread.Sleep(time);


                // Items
                var items = OrderDetails.Order.Items;
                Logger.Info($"[ORDER DETAILS] Adding {items.Count} items...");
                _driver.FindElement(By.Id(L["AddOrderItemButton"])).Click();
                Thread.Sleep(time);

                iframe = new WebDriverWait(_driver, TimeSpan.FromSeconds(10))
                    .Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.CssSelector("iframe.cboxIframe")));
                // Switch to the iframe
                _driver.SwitchTo().Frame(iframe);

                foreach (var item in items)
                {
                    Logger.Info($"[ORDER ITEM] Adding item: {item.ItemName} | Qty={item.Quantity} | Price={item.Price}");
                    _driver.FindElement(By.Id(L["SearchItem"])).Clear();
                    _driver.FindElement(By.Id(L["SearchItem"])).SendKeys(item.ItemName);
                    _driver.FindElement(By.Id(L["SearchItemButton"])).Click();
                    Thread.Sleep(time);
                    _driver.FindElement(By.XPath($"//div[@class='grid-body']//div[@class='row no-margin'][.//div[contains(normalize-space(.), '{item.ItemName}')]]")).Click();
                    Thread.Sleep(time);
                    _driver.FindElement(By.Id(L["ItemWarehouse"])).SendKeys(item.Warehouse);
                    _driver.FindElement(By.Id(L["ItemPrice"])).SendKeys(item.Price);
                    _driver.FindElement(By.Id(L["ItemQuantity"])).SendKeys(item.Quantity);
                    _driver.FindElement(By.Id(L["AddItemSubmit"])).Click();
                    error= GetErrorMessage();
                    if (!string.IsNullOrEmpty(error))
                    {
                        Logger.Error($"[ERROR] Failed to add Item '{item.ItemName}': due to - {error}");
                        return error;
                    }
                    Thread.Sleep(time);
                }

                // When done, switch back to the main document
                _driver.SwitchTo().DefaultContent();
                Thread.Sleep(time);
                _driver.FindElement(By.Id(L["CloseItemPopup"])).Click();
                Logger.Info("[ORDER DETAILS] Items added and popup closed.");

                return error;
            }
            catch (Exception ex)
            {
                error = $"Error in addOrderDetails: {ex.Message}";
                Logger.Error($"Error in addOrderDetails: {ex.Message}");
                return error;
            }           
        }
        #endregion
    }
}
