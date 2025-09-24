using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium;
using Newtonsoft.Json;
using TestAutomation.Models;
using TestAutomation.Utils;

namespace TestAutomation.Helper
{
    public class CommonTestCasesHelper
    {
        private readonly IWebDriver _driver;              
        public CommonTestCasesHelper(IWebDriver driver)
        {
            _driver = driver; 
        }

        public void Navigate(Dictionary<string, string> L)
        {
            var url = L["BaseUrl"];
            Logger.Info($"Navigating to: {url}");
            _driver.Navigate().GoToUrl(url);
            _driver.Manage().Window.Maximize();
        }
        public string Login(Dictionary<string, string> L ,string username, string password, bool rememberMe = false)
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
                error = GetValidationMessage(L);
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
        public void logout(Dictionary<string, string> L)
        {
            // Step 8: Logout
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
                //Logger.Info("No error message displayed within timeout.");
                return string.Empty;
            }
        }
        public string GetValidationMessage(Dictionary<string, string> L)
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
        public void CloseErrorBox(Dictionary<string, string> L)
        {
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
            // Wait until the alert is visible
            var alert = wait.Until(d => d.FindElement(By.CssSelector(L["Invalid"])));
            // Find the close button inside the alert
            var closeBtn = alert.FindElement(By.CssSelector("button.close"));
            // Click the X button
            closeBtn.Click();
        }
        public void TryClick(string xpath, string buttonName)
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
        public bool TryClickWithRetry(string xpath, string buttonName, int attempts, int waitMs)
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


        // like wise for Capital and BestPet


    }
}