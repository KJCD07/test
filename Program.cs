using System;
using System;
using Microsoft.Win32;
namespace ConsoleApp1
{
 

class Program
    {
        static void Main()
        {
            string smtpServer = GetSmtpServerFromRegistry();

            if (!string.IsNullOrEmpty(smtpServer))
            {
                Console.WriteLine($"SMTP Server: {smtpServer}");
            }
            else
            {
                Console.WriteLine("SMTP server information not found in the registry.");
            }
        }

        static string GetSmtpServerFromRegistry()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings"))
                {
                    if (key != null)
                    {
                        // Look for the "SMTP server" value
                        object smtpServerValue = key.GetValue("SMTP server");

                        if (smtpServerValue != null)
                        {
                            return smtpServerValue.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving SMTP server information: {ex.Message}");
            }

            return null;
        }
    }

}

