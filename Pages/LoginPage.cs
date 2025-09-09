using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium;
using System;
using System.Linq;
using System.Threading;
using LoginAutomation.Tests.Utils;

namespace LoginAutomation.Tests.Pages
{
    public class LoginPage
    {
        private readonly IWebDriver _driver;

        public LoginPage(IWebDriver driver)
        {
            _driver = driver;
        }

        // Locators pulled from config (no app details hardcoded)
        private string L(string name) => Config.Get($"Locators:{name}");

        public void Navigate()
        {
            var url = Config.Get("AppSettings:BaseUrl");
            Logger.Info($"Navigating to: {url}");
            _driver.Navigate().GoToUrl(url);
            _driver.Manage().Window.Maximize();
        }

        public string Login(string username, string password, bool rememberMe = false)
        { 
            Logger.Info($"Attempting login with Username='{username}' and Password={password}");

            _driver.FindElement(By.Id(L("Username"))).SendKeys(username);
            _driver.FindElement(By.Id(L("Password"))).SendKeys(password);

            var remember = L("RememberMe");
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

            _driver.FindElement(By.Id(L("LoginButton"))).Submit();
            Logger.Info("Login button submitted.");

            var successUrlPart = Config.Get("AppSettings:SuccessUrlContains");

            // Wait up to 5 seconds for either success URL or error message
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(2));
            bool success = false;
            var error = string.Empty;

            try
            {
                success = wait.Until(d => d.Url.ToLowerInvariant().Contains(successUrlPart.ToLowerInvariant()));
            }
            catch (WebDriverTimeoutException)
            {
                // If URL didn't change in 5s, we’ll check for errors
                error = "Login failed due to timeout...";
                Logger.Warn("Login failed due to timeout...");
                return error;
            }

            if (success)
            {
                Logger.Info("Login successful. Navigating through application...");
            }
            else
            {
                error = GetErrorMessage();
                if (string.IsNullOrEmpty(error))
                {
                    error = GetvalidationMessage();
                }

                if (string.IsNullOrEmpty(error))
                {
                    Logger.Warn("Login failed but no error/validation message was displayed.");
                }
                else
                {
                    Logger.Warn($"Login failed with error: {error}");
                }
            }


            return error;
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

        public string GetvalidationMessage()
        {
            try
            {
                var user = _driver.FindElement(By.CssSelector(L("UsernameValidation")));
                var password = _driver.FindElement(By.CssSelector(L("PasswordValidation")));

                string result = string.Empty;

                if (user != null && !string.IsNullOrWhiteSpace(user.Text))
                    result += user.Text.Trim() + " ,";
                if (password != null && !string.IsNullOrWhiteSpace(password.Text))
                    result += password.Text.Trim();

                result = result.Trim();

                return result;
            }
            catch (WebDriverTimeoutException)
            {
                Logger.Info("No error message displayed within timeout.");
                return string.Empty;
            }
            
        }

        public void explore()
        {
            int time = 3000;
            Logger.Info("Exploring application menus...");

            try
            {
                _driver.FindElement(By.LinkText(L("mainMenu_1"))).Click();
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L("subMenu_1_1"))).Click();
                //Thread.Sleep(time); 
                //_driver.FindElement(By.PartialLinkText(L("subMenu_1_2"))).Click(); 
                //Thread.Sleep(time); 
                //_driver.FindElement(By.PartialLinkText(L("subMenu_1_3"))).Click(); 
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L("subMenu_1_4"))).Click();
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L("subMenu_1_5"))).Click();
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L("subMenu_1_6"))).Click();
                //Thread.Sleep(time);
                //Logger.Info("Finished navigating first set of menus.");

                //_driver.FindElement(By.LinkText(L("mainMenu_2"))).Click();
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L("subMenu_2_1"))).Click();
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L("subMenu_2_2"))).Click();
                //Thread.Sleep(time); 
                //_driver.FindElement(By.PartialLinkText(L("subMenu_2_3"))).Click(); 
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L("subMenu_2_4"))).Click(); 
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L("subMenu_2_5"))).Click();
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L("subMenu_2_6"))).Click(); 
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L("subMenu_2_7"))).Click();
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L("subMenu_2_8"))).Click(); 
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L("subMenu_2_9"))).Click(); 
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L("subMenu_2_10"))).Click(); 
                //Thread.Sleep(time);
                //_driver.FindElement(By.PartialLinkText(L("subMenu_2_11"))).Click();
                //Thread.Sleep(time); 
                //_driver.FindElement(By.PartialLinkText(L("subMenu_2_12"))).Click();
                //Thread.Sleep(time);
                //Logger.Info("Finished navigating second set of menus.");

                //_driver.FindElement(By.Id(L("LogoutButton"))).Click();
                //Thread.Sleep(time);
                //_driver.FindElement(By.Id(L("NoLogout"))).Click();
                //Thread.Sleep(time);
                _driver.FindElement(By.Id(L("LogoutButton"))).Click();
                Thread.Sleep(time);
                _driver.FindElement(By.Id(L("YesLogout"))).Click();
                Thread.Sleep(time);

                Logger.Info("Logout sequence completed successfully.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error while exploring menus: {ex.Message}");
                throw;
            }
        }

        public void CloseErrorBox()
        {
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
            // Wait until the alert is visible
            var alert = wait.Until(d => d.FindElement(By.CssSelector(L("Invalid"))));
            // Find the close button inside the alert
            var closeBtn = alert.FindElement(By.CssSelector("button.close"));
            // Click the X button
            closeBtn.Click();
        }
    }
}
