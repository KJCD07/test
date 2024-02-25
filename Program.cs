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



DECLARE @rowdelimi VARCHAR(MAX) = CHAR(10)
DECLARE @bulkload NVARCHAR(MAX)
DECLARE @file NVARCHAR(MAX) = 'D:\OneDrive\Pictures\Test.txt'

SET @bulkload = 'BULK INSERT [dbo].[t] FROM ''' + @file + ''' WITH (ROWTERMINATOR = ''' + @rowdelimi + ''')'

EXEC sp_executesql @bulkload







DECLARE @rowdelimi VARCHAR(MAX) = CHAR(10)
DECLARE @bulkload NVARCHAR(MAX)
DECLARE @file NVARCHAR(MAX) = 'D:\OneDrive\Pictures\Test.txt'

SET @bulkload = 'BULK INSERT [dbo].[t] FROM ''' + @file + ''' WITH (ROWTERMINATOR = ''' + @rowdelimi + ''')'

EXEC sp_executesql @bulkload




DECLARE @rowdelimi VARCHAR(MAX) = CHAR(10)
DECLARE @bulkload VARCHAR(MAX)
DECLARE @file VARCHAR(MAX) = 'D:\OneDrive\Pictures\Test.txt'

BEGIN TRY
    SET @bulkload = 'BULK INSERT [dbo].[t] FROM ''' + @file + ''' WITH (ROWTERMINATOR = ''' + @rowdelimi + ''')'
    EXEC sp_executesql @bulkload
END TRY
BEGIN CATCH
    -- Error handling
    PRINT 'Error Number: ' + CAST(ERROR_NUMBER() AS VARCHAR) + ', ' + 'Error Message: ' + ERROR_MESSAGE()
END CATCH







-- Specify the user you want to check
DECLARE @userName NVARCHAR(100) = 'YourUserName'

-- Check if the user exists in the current database
IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @userName AND type_desc = 'SQL_USER')
BEGIN
    -- Check BULK INSERT permission for the user in the current database
    IF EXISTS (SELECT 1 FROM sys.database_permissions WHERE grantee_principal_id = DATABASE_PRINCIPAL_ID(@userName) AND type = 'BULK INSERT')
    BEGIN
        PRINT 'The user ' + @userName + ' has BULK INSERT permission in the current database.'
    END
    ELSE
    BEGIN
        PRINT 'The user ' + @userName + ' does not have BULK INSERT permission in the current database.'
    END
END
ELSE
BEGIN
    PRINT 'The user ' + @userName + ' does not exist in the current database.'
END











