using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace INNTelegramBot
{
    public class InteractWithEGRUL
    {
        private static string downloadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Downloads");

        public static void DeleteAllFromDirectory()
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(downloadDirectory);

            foreach (FileInfo file in directoryInfo.GetFiles())
            {
                file.Delete();
            }
        }

        public static FileInfo? GetPDFFromDirectory()
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(downloadDirectory);

            return directoryInfo.GetFiles().FirstOrDefault();
        }

        public static async Task DownloadPDF(string inn)
        {
            var driverService = ChromeDriverService.CreateDefaultService();
            var options = new ChromeOptions();

            options.AddUserProfilePreference("download.default_directory", downloadDirectory);
            options.AddUserProfilePreference("download.prompt_for_download", false);
            options.AddUserProfilePreference("disable-popup-blocking", "true");

            options.AddArgument("--headless");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");

            using (IWebDriver driver = new ChromeDriver(driverService, options))
            {
                try
                {
                    driver.Navigate().GoToUrl("https://egrul.nalog.ru/index.html");

                    IWebElement formInput = driver.FindElement(By.Id("query"));
                    formInput.SendKeys(inn);

                    IWebElement submitButton = driver.FindElement(By.Id("btnSearch"));
                    submitButton.Click();

                    // Ждем появления кнопки для скачивания PDF
                    WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
                    wait.Until(ExpectedConditions.ElementIsVisible(By.ClassName("op-excerpt")));

                    // Нажимаем кнопку для скачивания PDF
                    IWebElement downloadButton = driver.FindElement(By.ClassName("op-excerpt"));
                    downloadButton.Click();

                    await Task.Delay(5000);

                    if (Directory.GetFiles(downloadDirectory, "*.pdf").Length > 0)
                    {
                        Console.WriteLine("PDF файл успешно скачан.");
                    }
                    else
                    {
                        Console.WriteLine("Не удалось скачать PDF файл.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Произошла ошибка: " + ex.Message);
                }
                finally
                {
                    driver.Quit();
                }
            }
        }
    }
}
