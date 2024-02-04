using System;
using System.Collections;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace LinkedServerConnectivity_Test_Tool
{
    public partial class Form1 : Form
    {

        NameValueCollection applicationSettings = ConfigurationManager.GetSection("appSettings") as NameValueCollection;
        private string connectionString;
        private string databaseName;
        //string eventSource = "DataScrubbingApp";
        string eventSource;
        DateTime currentDate = DateTime.Now;
        string logFilePath;
        string generatedConfigurescript;

        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn
           (
               int nLeftRect,     // x-coordinate of upper-left corner
               int nTopRect,      // y-coordinate of upper-left corner
               int nRightRect,    // x-coordinate of lower-right corner
               int nBottomRect,   // y-coordinate of lower-right corner
               int nWidthEllipse, // width of ellipse
               int nHeightEllipse // height of ellipse
           );

        public Form1()
        {
            InitializeComponent();
            //ExecuteGrpbox.Paint += PaintBorderlessGroupBox;
        }

        public void LoadEnv(ComboBox name)
        {


            if (applicationSettings.Count == 0)
            {
                Errorlabel.Text = "Application Settings are not defined";
            }
            else
            {
                foreach (var key in applicationSettings.AllKeys)
                {
                    //Console.WriteLine(key + " = " + applicationSettings[key]);

                    //EnvcomboBox.Items.Add(applicationSettings[key].ToString());

                    name.Items.Add(key);
                }
            }

        }

        public void configureEvenlog()
        {

            // Create the event source if it doesn't exist
            if (!EventLog.SourceExists(eventSource))
            {
                EventLog.CreateEventSource(eventSource, "Application");

            }

        }

        public void configurelogs()
        {


            string dateString = currentDate.ToString("yyyy-MM-dd");

            // Get the directory where the application is running
            string directoryPath = AppDomain.CurrentDomain.BaseDirectory;

            // Specify the log file path
            logFilePath = Path.Combine(directoryPath, $"LogFile_{dateString}.log");

        }

        public void writelogs(Exception ex)
        {
            // Write the log message to the file
            using (StreamWriter writer = new StreamWriter(logFilePath, true))
            {
                // Append the log message along with timestamp
                writer.WriteLine($"{currentDate:yyyy-MM-dd HH:mm:ss}: {ex.Message.ToString()}");
            }

        }

        public void settingupsize(GroupBox gp)
        {
            gp.Size = new System.Drawing.Size(830, 538);
            gp.Location = new System.Drawing.Point(165, 113);

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                configurelogs();
                // configureEvenlog();


                content();
                Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 20, 20));
                GrpBoxReadMe.Visible = true;
                setupgroupBox.Visible = false;
                ExecuteGrpbox.Visible = false;
                grpboxLogs.Visible = false;
                settingupsize(GrpBoxReadMe);

                clbTables.TabStop = true;
                clbColumns.TabStop = true;


            }
            catch (Exception ex)
            {
                //  EventLog.WriteEntry(eventSource, ex.Message, EventLogEntryType.Error);
                writelogs(ex);
                //throw ex;
            }
        }

        private void EnvcomboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (!EnvcomboBox.SelectedItem.ToString().Contains("Select"))
                {
                    connectionString = applicationSettings[EnvcomboBox.SelectedItem.ToString()].ToString();
                    ExtractDatabaseName(connectionString);
                    lbldbnamevalue.Text = databaseName.ToString();
                    PopulateTablesClb(connectionString);
                }
                else
                {
                    lbldbnamevalue.Text = "";
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }



        public void loadColumns()
        {
            string modifiedItems = "";
            StringBuilder resultString = new StringBuilder();
            string tbl = string.Empty;
            try
            {
                if (clbTables.CheckedItems.Count > 0)
                {
                    foreach (var selectedItem in clbTables.CheckedItems)
                    {

                        modifiedItems = $"'{selectedItem}'";
                        resultString.Append(modifiedItems.ToString());
                        resultString.Append(",");

                    }

                    if (resultString.Length > 0)
                    {
                        resultString.Length -= 1; // Remove the last two characters (", ")
                    }

                    string query = $"SELECT " +
     $" COALESCE(pcu.TABLE_NAME, c.TABLE_NAME) AS ParentTableName, " +
  $" COALESCE(pcu.COLUMN_NAME, c.COLUMN_NAME) AS ParentColumnName, " +
     $" ccu.COLUMN_NAME AS ChildColumnName, " +
    $"  ccu.TABLE_NAME AS ChildTableName, " +
    $"  ccu.TABLE_SCHEMA AS ChildTableSchema " +
  $"FROM " +
  $"    INFORMATION_SCHEMA.COLUMNS c " +
  $"LEFT JOIN " +
  $"    INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE pcu ON c.COLUMN_NAME = pcu.COLUMN_NAME AND c.TABLE_NAME = pcu.TABLE_NAME " +
  $" LEFT JOIN " +
     $" INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc ON pcu.CONSTRAINT_NAME = rc.UNIQUE_CONSTRAINT_NAME " +
  $" LEFT JOIN " +
   $"   INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE ccu ON rc.CONSTRAINT_NAME = ccu.CONSTRAINT_NAME " +
  $"WHERE " +
    $" c.TABLE_NAME  IN({resultString}) order by ParentTableName asc";
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();

                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                clbColumns.Items.Clear();

                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {

                                        if (!string.IsNullOrEmpty(reader["ChildColumnName"].ToString()))
                                        {
                                            String data = reader["ChildColumnName"].ToString() + "-->" + reader["ChildTableSchema"].ToString() + "." + reader["ChildTableName"].ToString();
                                            clbColumns.Items.Add(data);
                                        }else
                                        {
                                            String data = reader["ParentColumnName"].ToString() + "-->" + reader["ParentTableName"].ToString();
                                            clbColumns.Items.Add(data);
                                        }
                                    }
                                }

                            }
                        }

                    }
                }
                else
                {
                    clbColumns.Items.Clear();
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        private void btnConnectivity_Click(object sender, EventArgs e)
        {
            try
            {

                loadColumns();

            }
            catch (Exception ex)
            {

                throw ex;
            }


        }



        private void PopulateTablesClb(String connstr)
        {
            // Connection string to your database
            string connectionString = connstr;

            // SQL query to retrieve data
            string query = "select name from sys.tables where type = 'u' order by 1 asc";

            try
            {
                // Create a SqlConnection
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    // Open the connection
                    connection.Open();

                    // Create a SqlCommand with the query and connection
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Execute the query and retrieve data
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            // Clear existing items in CheckedListBox
                            clbTables.Items.Clear();

                            // Iterate through the result set
                            while (reader.Read())
                            {
                                // Add each item to CheckedListBox
                                clbTables.Items.Add(reader["name"].ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., display an error message)
                MessageBox.Show("Error: " + ex.Message);
            }
        }



        private void clbTables_SelectedValueChanged(object sender, EventArgs e)
        {
            try
            {
                loadColumns();

            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        private void btnGenerateScript_Click(object sender, EventArgs e)
        {
            try
            {

                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;

                // Combine the app directory with the desired file name
                string filePath = Path.Combine(appDirectory, "GeneratedScript.txt");



                if (clbColumns.CheckedItems.Count > 0)
                {

                    // Create a StringBuilder to build the dynamic update script
                    StringBuilder updateScriptBuilder = new StringBuilder();
                    updateScriptBuilder.AppendLine(databaseName);
                    updateScriptBuilder.AppendLine("Go");


                    // Iterate through selected items in the CheckedListBox
                    foreach (var selectedColumn in clbColumns.CheckedItems)
                    {
                        string[] values = selectedColumn.ToString().Split(new string[] { "-->" }, StringSplitOptions.None);



                        // Get the new value for the column (replace this with your logic to get the new value)
                        string newValue = "NewValue";

                        // Build the update statement for each selected column
                        string updateStatement = $"UPDATE {values[1]} SET {values[0]} = '{newValue}';";

                        // Append the update statement to the StringBuilder

                        updateScriptBuilder.AppendLine(updateStatement);
                        updateScriptBuilder.AppendLine("Go");


                    }

                    // Get the final update script
                    string finalUpdateScript = updateScriptBuilder.ToString();

                    // Display the generated update script (for testing purposes)
                    MessageBox.Show($"Generated Update Script:{Environment.NewLine}{finalUpdateScript}");

                    using (StreamWriter writer = new StreamWriter(filePath))
                    {
                        // Replace "YourStringToWrite" with the actual string you want to write

                        writer.Write(finalUpdateScript);
                    }

                    // Open the saved file
                    Process.Start(filePath);



                }
                else
                {
                    MessageBox.Show("Please select columns in the CheckedListBox.");
                }

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private void btnSetup_Click(object sender, EventArgs e)
        {

            settingupsize(setupgroupBox);
            setupgroupBox.Visible = true;
            ExecuteGrpbox.Visible = false;
            GrpBoxReadMe.Visible = false;
            CmbSetupEnv.Items.Clear();
            CmbSetupEnv.Items.Add("Select an Option");
            CmbSetupEnv.SelectedIndex = 0;
            LoadEnv(CmbSetupEnv);



        }

        private void btnclose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnExecutepanel_Click(object sender, EventArgs e)
        {
            try
            {
                settingupsize(ExecuteGrpbox);
                setupgroupBox.Visible = false;
                ExecuteGrpbox.Visible = true;
                clbColumns.Visible = false;
                grpboxLogs.Visible = false;
                EnvcomboBox.Items.Clear();
                EnvcomboBox.Items.Add("Select an Option");
                EnvcomboBox.SelectedIndex = 0;
                LoadEnv(EnvcomboBox);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }




        private void PaintBorderlessGroupBox(object sender, PaintEventArgs p)
        {
            GroupBox box = (GroupBox)sender;
            //  p.Graphics.Clear(SystemColors.Control);
            p.Graphics.DrawString(box.Text, box.Font, Brushes.Black, 0, 0);
        }

        private void ExecuteGrpbox_Enter(object sender, EventArgs e)
        {

        }

        private void SetupGroupBox_Enter(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void setupgroupBox_Enter_1(object sender, EventArgs e)
        {

        }

        private void lblContent_Click(object sender, EventArgs e)
        {

        }

        private void btnReadMe_Click(object sender, EventArgs e)
        {
            try
            {

                settingupsize(GrpBoxReadMe);

                GrpBoxReadMe.Visible = true;
                setupgroupBox.Visible = false;
                ExecuteGrpbox.Visible = false;
                grpboxLogs.Visible = false;
                content();



            }
            catch (Exception ex)

            {

                throw ex;
            }
        }

        public void content()
        {
            lblContent.AutoSize = true;

            lblContent.Text = "The Data Scrubbing Tool is a powerful utility designed to help you clean and sanitize your datasets effectively. It enables you to identify and rectify \n inconsistencies, errors, and sensitive information within your data,ensuring that you maintain data integrity and comply with privacy regulations.\n \n\n  ";
            lblContent.Dock = DockStyle.Top;

            // Features Label
            Label lblFeatures = new Label();
            lblFeatures.Text = "Features";
            lblFeatures.Font = new System.Drawing.Font(lblFeatures.Font, System.Drawing.FontStyle.Bold);
            lblFeatures.Location = new System.Drawing.Point(10, 10);
            GrpBoxReadMe.Controls.Add(lblFeatures);

            // Log Files and Query Generation
            Label lblLogFiles = new Label();
            lblLogFiles.Text = "Log Files and Query Generation:";
            lblLogFiles.Location = new System.Drawing.Point(20, 40);
            GrpBoxReadMe.Controls.Add(lblLogFiles);

            // Log Files Description
            Label lblLogFilesDesc = new Label();
            lblLogFilesDesc.Text = "Log files and generated queries are stored in the same folder where the application is executed from. This ensures easy access to logs and queries for monitoring and auditing purposes.";
            lblLogFilesDesc.Location = new System.Drawing.Point(40, 70);
            GrpBoxReadMe.Controls.Add(lblLogFilesDesc);

            // Data Scrubbing Process Label
            Label lblDataScrubbing = new Label();
            lblDataScrubbing.Text = "Data Scrubbing Process:";
            lblDataScrubbing.Location = new System.Drawing.Point(20, 120);
            GrpBoxReadMe.Controls.Add(lblDataScrubbing);

            // Configuration Step Label
            Label lblConfiguration = new Label();
            lblConfiguration.Text = "Step 1: Configuration";
            lblConfiguration.Location = new System.Drawing.Point(40, 150);
            GrpBoxReadMe.Controls.Add(lblConfiguration);

            // Configuration Description
            Label lblConfigurationDesc = new Label();
            lblConfigurationDesc.Text = "Click on the \"Configure\" icon to execute the basic setup for the data scrubbing process.";
            lblConfigurationDesc.Location = new System.Drawing.Point(60, 180);
            GrpBoxReadMe.Controls.Add(lblConfigurationDesc);

            // Execution Step Label
            Label lblExecution = new Label();
            lblExecution.Text = "Step 2: Execution";
            lblExecution.Location = new System.Drawing.Point(40, 220);
            GrpBoxReadMe.Controls.Add(lblExecution);

            // Execution Description
            Label lblExecutionDesc1 = new Label();
            lblExecutionDesc1.Text = "Click on the \"Execute\" icon to initiate the execution process.";
            lblExecutionDesc1.Location = new System.Drawing.Point(60, 250);
            GrpBoxReadMe.Controls.Add(lblExecutionDesc1);

            Label lblExecutionDesc2 = new Label();
            lblExecutionDesc2.Text = "Select the environment, tables, and columns to be scrubbed.";
            lblExecutionDesc2.Location = new System.Drawing.Point(60, 270);
            GrpBoxReadMe.Controls.Add(lblExecutionDesc2);

            Label lblExecutionDesc3 = new Label();
            lblExecutionDesc3.Text = "Click on \"Generate Script\" to preview the generated query.";
            lblExecutionDesc3.Location = new System.Drawing.Point(60, 290);
            GrpBoxReadMe.Controls.Add(lblExecutionDesc3);

            Label lblExecutionDesc4 = new Label();
            lblExecutionDesc4.Text = "Click on \"Execute\" to run the query against the database.";
            lblExecutionDesc4.Location = new System.Drawing.Point(60, 310);
            GrpBoxReadMe.Controls.Add(lblExecutionDesc4);

            // View Logs Label
            Label lblViewLogs = new Label();
            lblViewLogs.Text = "View Logs:";
            lblViewLogs.Location = new System.Drawing.Point(20, 360);
            GrpBoxReadMe.Controls.Add(lblViewLogs);

            // View Logs Description
            Label lblViewLogsDesc = new Label();
            lblViewLogsDesc.Text = "Click on the \"Logs\" icon to access the latest logs of the application. This feature provides insight into the application's recent activities and helps in troubleshooting.";
            lblViewLogsDesc.Location = new System.Drawing.Point(40, 390);
            GrpBoxReadMe.Controls.Add(lblViewLogsDesc);






        }

        private void btnShowQuerySetup_Click(object sender, EventArgs e)
        {
            try
            {

                StringBuilder updateScriptBuilder = new StringBuilder();
                updateScriptBuilder.AppendLine(databaseName);
                updateScriptBuilder.AppendLine("Go");


                generatedConfigurescript = updateScriptBuilder.ToString();

                // Display the generated update script (for testing purposes)
                MessageBox.Show($"Generated  Script:{Environment.NewLine}{generatedConfigurescript}");

                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;

                // Combine the app directory with the desired file name
                string filePath = Path.Combine(appDirectory, "ConfigureScript.txt");

                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    // Replace "YourStringToWrite" with the actual string you want to write

                    writer.Write(generatedConfigurescript);
                }

                // Open the saved file
                Process.Start(filePath);



            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private void btnRunSetup_Click(object sender, EventArgs e)
        {
            try
            {

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        private void ExtractDatabaseName(string connectionString)
        {
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);

                // Extract the database name
                databaseName = builder.InitialCatalog;

            }
            catch (ArgumentException ex)
            {
                MessageBox.Show("Invalid connection string: " + ex.Message);
                throw ex;
                // Handle the exception if the connection string is invalid

            }
        }

        private void clbTables_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (clbTables.CheckedItems.Count > 0)
            {
                // comboBoxTables();

                clbColumns.Visible = true;


            }
            else
            {
                clbColumns.Visible = false;
            }

        }

        private void btnLogs_Click(object sender, EventArgs e)
        {
            try
            {

                settingupsize(grpboxLogs);
                setupgroupBox.Visible = false;
                ExecuteGrpbox.Visible = false;
                GrpBoxReadMe.Visible = false;
                grpboxLogs.Visible = true;
                Populatelogdetails();


            }
            catch (Exception ex)
            {
                //MessageBox.Show("Your error message here", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                writelogs(ex);

            }

        }



        public void Populatelogdetails()
        {

            try
            {
                string applicationDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string fileExtension = ".log";

                // Get the latest log files in the directory
                string[] latestLogFiles = GetLatestLogFiles(applicationDirectory, fileExtension);

                lbllogfilelocvalue.Text = latestLogFiles[0].ToString();
                lbllogfilename.Text = Path.GetFileName(latestLogFiles[0].ToString());
                linkLabellog.Links.Clear();
                linkLabellog.Links.Add(0, linkLabellog.Text.Length, latestLogFiles[0].ToString());


            }
            catch (Exception ex)
            {


                //MessageBox.Show("Your error message here", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                throw ex;


            }

        }


        static string[] GetLatestLogFiles(string directory, string fileExtension)
        {
            try
            {
                // Get all files with the specified extension in the directory
                string[] logFiles = Directory.GetFiles(directory, "*" + fileExtension);

                // Order files by creation time in descending order
                var sortedLogFiles = logFiles
                    .Select(file => new FileInfo(file))
                    .OrderByDescending(fileInfo => fileInfo.CreationTime)
                    .ToArray();


                string[] latestLogFiles = sortedLogFiles
                    .Take(1)
                    .Select(fileInfo => fileInfo.FullName)
                    .ToArray();

                var name = sortedLogFiles
                     .Take(1)
                     .Select(fileInfo => fileInfo.Name);

                //lblfilelocvalue.Text = name;

                //lbllogfilelocvalue.Text = "";

                return latestLogFiles;
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error: {ex.Message}");
                //return new string[0]; // Return an empty array in case of an error
                throw ex;

            }
        }

        private void linkLabellog_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                string url = e.Link.LinkData as string;
                if (url != null)
                {
                    System.Diagnostics.Process.Start(url);
                }
                else
                {
                    MessageBox.Show("There is no log files available");

                }

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        private void lbllogfilelocvalue_Click_1(object sender, EventArgs e)
        {


        }

        private void CmbSetupEnv_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!CmbSetupEnv.SelectedItem.ToString().Contains("Select"))
            {
                connectionString = applicationSettings[CmbSetupEnv.SelectedItem.ToString()].ToString();
                ExtractDatabaseName(connectionString);
                lblconfigureDBvalue.Text = databaseName.ToString();
            }
            else
            {
                lblconfigureDBvalue.Text = "";
            }


        }

        private void comboBoxTables()
        {
            string selectedTableName = clbTables.SelectedItem.ToString();

            // Check if the table has child tables
            if (HasChildTables(selectedTableName))
            {
                // If it has child tables, retrieve columns of both parent and child tables
                RetrieveColumnsOfParentAndChild(selectedTableName);
            }
            else
            {
                // If it doesn't have child tables, retrieve columns of the parent table only
                RetrieveColumnsOfParent(selectedTableName);
            }
        }

        private bool HasChildTables(string parentTableName)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    // Open the connection
                    connection.Open();

                    DataTable childTables = connection.GetSchema("ForeignKeys", new[] { null, null, parentTableName });

                    return childTables.Rows.Count > 0;


                }
            }

            catch (Exception ex)
            {
                MessageBox.Show("Error checking child tables: " + ex.Message);
                return false;
            }

        }

        private void RetrieveColumnsOfParent(string parentTableName)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    // Open the connection
                    connection.Open();

                    DataTable parentColumns = connection.GetSchema("Columns", new[] { null, null, parentTableName });

                    // Process parent table columns as needed
                    foreach (DataRow row in parentColumns.Rows)
                    {
                        string columnName = row["COLUMN_NAME"].ToString();
                        // Do something with the column name
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error retrieving columns of parent table: " + ex.Message);
            }

        }

        private void RetrieveColumnsOfParentAndChild(string parentTableName)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    // Open the connection
                    connection.Open();

                    DataTable parentColumns = connection.GetSchema("Columns", new[] { null, null, parentTableName });

                    // Process parent table columns as needed
                    foreach (DataRow row in parentColumns.Rows)
                    {
                        string columnName = row["COLUMN_NAME"].ToString();
                        // Do something with the column name
                    }

                    DataTable childTables = connection.GetSchema("ForeignKeys", new[] { null, null, null, null, null, parentTableName });

                    foreach (DataRow childTableRow in childTables.Rows)
                    {
                        string childTableName = childTableRow["FK_TABLE_NAME"].ToString();
                        DataTable childColumns = connection.GetSchema("Columns", new[] { null, null, childTableName });

                        // Process child table columns as needed
                        foreach (DataRow childColumnRow in childColumns.Rows)
                        {
                            string childColumnName = childColumnRow["COLUMN_NAME"].ToString();
                            // Do something with the child column name
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error retrieving columns of parent and child tables: " + ex.Message);
            }

        }



    }
}
