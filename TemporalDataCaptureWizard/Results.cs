/*
This Sample Code is provided for the purpose of illustration only and is not intended to be used in a production environment.  
THIS SAMPLE CODE AND ANY RELATED INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.  
We grant You a nonexclusive, royalty-free right to use and modify the 
Sample Code and to reproduce and distribute the object code form of the Sample Code, provided that You agree: (i) to not use Our name, logo, or trademarks to market Your software product in which the Sample Code is embedded; 
(ii) to include a valid copyright notice on Your software product in which the Sample Code is 
embedded; and 
(iii) to indemnify, hold harmless, and defend Us and Our suppliers from and against any claims or lawsuits, including attorneys’ fees, that arise or result from the use or distribution of the Sample Code.
Please note: None of the conditions outlined in the disclaimer above will supercede the terms and conditions contained within the Premier Customer Services Description.
*/
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace TemporalDataCaptureWizard
{
    public partial class Results : Form
    {
        #region Properties
        public const string TDCVersion ="1.0.0.0";
        public int ConnectionTimeout { get; set; }
        public int ExecutionTimeout { get; set; }
        public decimal Threshold { get; set; }
        public bool CheckIndexUsage { get; set; }
        public string ServerName { get; set; }
        public string DatabaseName { get; set; }
        // Manually implement this property, since we need to set SavedConnectionString based on this set
        SqlConnectionInfo connectionInfo;
        public SqlConnectionInfo ConnectionInfo
        {
            get { return connectionInfo; }
            set { connectionInfo = value; SavedConnectionString = connectionInfo.ConnectionString; }
        }
        SqlConnection con;
        public SqlConnection connection
        {
            get { return con; }
            set { con = value;}
        }
        public string SavedConnectionString { get; set; }
        //public List<int> SelectedRows = new List<int>();
        public StringBuilder sqlScript  = new StringBuilder();
       
#endregion Properties

        #region Form Methods
        public Results(SqlConnection c)
        {
            InitializeComponent();
            connection = c;
            // Define columns, styles, etc.
            SetupDataGridView();
        }
        #endregion Form Methods

        #region Event Handlers
        private void Close_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void Process_Click(object sender, EventArgs e)
        {
            // Don't allow multiple clicks
            btnProcess.Enabled = false;

            // This is going to take awhile - change to wait cursor
            Cursor current = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;

            // Connect to SQL Server
            ConnectionInfo.DatabaseName = DatabaseName;
            SqlConnection con = new SqlConnection(ConnectionInfo.ConnectionString);
            SqlCommand cmd = new SqlCommand();
            // This SqlCommand will be used for the detailed queries
            cmd.Connection = con;
            cmd.CommandTimeout = ExecutionTimeout;

            SqlParameter schemaNameParameter = new SqlParameter();
            schemaNameParameter.ParameterName = "@table_schema";
            schemaNameParameter.SqlDbType = SqlDbType.NVarChar;
            schemaNameParameter.Direction = ParameterDirection.Input;
            cmd.Parameters.Add(schemaNameParameter);

            SqlParameter tableNameParameter = new SqlParameter();
            tableNameParameter.ParameterName = "@table_name";
            tableNameParameter.SqlDbType = SqlDbType.NVarChar;
            tableNameParameter.Direction = ParameterDirection.Input;
            cmd.Parameters.Add(tableNameParameter);

            SqlParameter isEligibleParameter = new SqlParameter();
            isEligibleParameter.ParameterName = "@is_eligible";
            isEligibleParameter.SqlDbType = SqlDbType.Bit;
            isEligibleParameter.Direction = ParameterDirection.Output;
            cmd.Parameters.Add(isEligibleParameter);

            SqlParameter msgParameter = new SqlParameter();
            msgParameter.ParameterName = "@msg";
            msgParameter.SqlDbType = SqlDbType.NVarChar;
            msgParameter.Size = 4000;
            msgParameter.Direction = ParameterDirection.Output;
            cmd.Parameters.Add(msgParameter);

            con.Open();
            // Do processing here
            try
            {
                // let's get us the list of databases, find ours
                // and iterate all the tables within our selected database
                ServerConnection c = new ServerConnection(ConnectionInfo);
                Server s = new Server(c);
                Database d = s.Databases[connectionInfo.DatabaseName];

                TableCollection t = d.Tables;
                try
                {
                    foreach (Table tbl in t)
                    {
                        // Don't reprocess our schemas
                        if(tbl.Schema == "tdc" || tbl.Schema == "history")
                        {
                            continue;
                        }
                        // Can this table be TDC enabled?
                        cmd.Parameters["@table_schema"].Value = tbl.Schema;
                        cmd.Parameters["@table_name"].Value = tbl.Name;

                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = "tdc.usp_CanTableBeTDC_Enabled";
                        // Retrieve our results and display to the user
                        cmd.ExecuteNonQuery();
                        // Lets display results to the user
                        DisplayRow(tbl.Schema, tbl.Name, Convert.ToBoolean(cmd.Parameters["@is_eligible"].Value), cmd.Parameters["@msg"].Value.ToString());
//                        if(Convert.ToBoolean(cmd.Parameters["@is_eligible"].Value) == false)
//                        {
////                            MessageBox.Show(cmd.Parameters["@msg"].Value.ToString());
//                            //dgResults
//                        }
                    }
                }
                catch (SqlException ex)
                {
                    SqlError error = ex.Errors[0];
                    if (error.Number == 207)
                    {
                        MessageBox.Show("Due to a defect in the system stored procedure used to estimate compression savings, tables that contain columns with embedded special characters cannot be processed.\r\nThis defect has been reported to the Product Team.", "Compression Estimator", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (SqlException sqlEX)
            {
                MessageBox.Show(sqlEX.Message, sqlEX.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, ex.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                con.Close();
            }
            Cursor.Current = current;
            btnSave.Enabled = true;
            btnScript.Enabled = true;
            btnProcess.Enabled = false;
        }
        private void Save_Click(object sender, EventArgs e)
        {
            // Save Dialog
            saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.CreatePrompt = false;
            saveFileDialog1.FileName = DatabaseName + " Temporal Data Capture Wizard Results";
            saveFileDialog1.DefaultExt = ".txt";
            saveFileDialog1.OverwritePrompt = true;
            saveFileDialog1.InitialDirectory = @"C:\temp\";
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string filename = saveFileDialog1.FileName;
                FileStream fs = File.Open(filename,FileMode.OpenOrCreate,FileAccess.Write);
                string line = "Schema\tTable\tEligible\tMessage\r\n";
                AddText(fs, line);

                foreach(DataGridViewRow r in dgResults.Rows)
                {
                    line = "";
                    // ignore the checkbox column
                    for(int c =0; c < 4;c++)
                    {
                        line += r.Cells[c].Value.ToString();
                        line += "\t";
                    }
                    line = line.Substring(0, line.Length - 1);
                    line += "\r\n";
                    AddText(fs, line);
                }
                fs.Flush();
                fs.Close();
            }
            dgResults.Rows.Clear();
            btnProcess.Enabled = true;
            btnSave.Enabled = false;

        }
        private void Script_Click(object sender, EventArgs e)
        {
            sqlScript.Remove(0, sqlScript.Length);
            sqlScript.Append("-- This TDC script was created by Temporal Data Capture Wizard\r\n");
            sqlScript.Append("USE " + DatabaseName + ";" + "\r\nGO\r\n");
            foreach (DataGridViewRow r in dgResults.Rows)
            {
                DataGridViewCheckBoxCell c = (DataGridViewCheckBoxCell)r.Cells["Script"];
                //if ((bool)c.Value == true)
                //{
                    Script(r);
                //}
            }
            SaveSQLScript();
        }
        private void Databases_SelectedIndexChanged(object sender, EventArgs e)
        {
            Cursor now = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            if (cbDatabases.Text != "")
            {
                btnProcess.Enabled = true;
                DatabaseName = cbDatabases.Text;
                connectionInfo.DatabaseName = DatabaseName;
                dgResults.Rows.Clear();
                int results = ValidateTDCComponentsInstalledAndUpToDate(cbDatabases.Text);
                switch(results)
                {
                    case 0:
                        break;
                    case 1:
                        if (DialogResult.Yes == MessageBox.Show("Temporal Data Capture components are not installed in the selected database. Deploy them?", "Deploy", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation))
                        {
                            if (InstallTDCComponents() == false)
                            {
                                MessageBox.Show("Unable to install components. Please check the installation log for more information.", "TDC Wizard", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            else
                            {
                                MessageBox.Show("TDC Components were succesfully installed.", "TDC Wizard", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                            break;
                    case 2:
                        if (DialogResult.Yes == MessageBox.Show("Temporal Data Capture components are not the current version in this database. Update them?", "Deploy", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation))
                        {
                            FileInfo file = new FileInfo(".\\SetupTDC.sql");
                            string script = file.OpenText().ReadToEnd();
                            script.Replace("GO", ";");
                            using (SqlConnection con = new SqlConnection(ConnectionInfo.ConnectionString))
                            {
                                SqlCommand cmd = new SqlCommand();
                                cmd.Connection = con;
                                con.Open();
                                cmd.CommandText = script;
                                cmd.ExecuteNonQuery();
                            }
                        }
                        break;
                }
            }
            Cursor.Current = now;
        }

        private void Results_Load(object sender, EventArgs e)
        {
            ServerConnection c = new ServerConnection(ConnectionInfo);
            Server s = new Server(c);
            foreach (Database d in s.Databases)
            {
                {
                    // need to exclude system databases and Reporting Services databases
                    if (d.Name != "master" && d.Name != "tempdb" && d.Name != "model" && d.Name != "msdb" && !d.Name.Contains("ReportServer$"))
                    {
                        cbDatabases.Items.Add(d.Name);
                    }
                }
            }
        }

        #endregion Event Handlers

        #region Helper Functions
        private static void AddText(FileStream fs, string value)
        {
            byte[] info = new UTF8Encoding(true).GetBytes(value);
            fs.Write(info, 0, info.Length);
        }

        private void DisplayRow(String schemaName, String tableName, Boolean isEligible, String message)
        {
            message = isEligible == true ? "" : message;
            string[] row = { schemaName, tableName, isEligible.ToString(), message };
            int r = dgResults.Rows.Add(row);
            var r2 = dgResults.Rows[r];
            var c4 = r2.Cells[4];
            if (isEligible == false)
            {
                c4.ReadOnly = true;
                c4.ToolTipText = "Cannot script this table.";
            }
            else
            {
                c4.ReadOnly = false;
                c4.ToolTipText = "Click to include this table in the script.";
            }
        }
        private void SetupDataGridView()
        {
            this.Controls.Add(dgResults);

            dgResults.ColumnCount = 4;

            dgResults.Name = "dgResults";

            dgResults.Columns[0].Name = "Schema Name";
            dgResults.Columns[0].Width = 75;
            dgResults.Columns[1].Name = "Table Name";
            dgResults.Columns[1].Width = 100;
            dgResults.Columns[2].Name = "Ok?";
            dgResults.Columns[2].Width = 50;
            dgResults.Columns[3].Name = "Message";
            dgResults.Columns[3].Width = 400;

            DataGridViewCheckBoxColumn column = new DataGridViewCheckBoxColumn();
            {
                column.HeaderText = "Script?";
                column.Name = "Script";
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                column.FlatStyle = FlatStyle.Standard;
                column.ThreeState = false;
                column.ValueType = typeof(Boolean);
                column.TrueValue = 1;
                column.FalseValue = 0;
                column.CellTemplate = new DataGridViewCheckBoxCell(false);
                column.CellTemplate.Value = false;
                column.CellTemplate.Style.NullValue = false;
            }
            dgResults.Columns.Add(column);
            dgResults.Columns[4].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            dgResults.MultiSelect = false;
            dgResults.ClearSelection();
            dgResults.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;
            dgResults.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            dgResults.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            dgResults.GridColor = Color.DarkGreen;

            dgResults.ColumnHeadersDefaultCellStyle.Font = new Font(dgResults.Font, FontStyle.Bold);

            // Set the selection background color for all the cells.
            dgResults.DefaultCellStyle.SelectionBackColor = Color.DarkGreen;
            dgResults.DefaultCellStyle.SelectionForeColor = Color.White;

            // Set RowHeadersDefaultCellStyle.SelectionBackColor so that its default
            // value won't override DataGridView.DefaultCellStyle.SelectionBackColor.
            dgResults.RowHeadersDefaultCellStyle.SelectionBackColor = Color.Empty;
            dgResults.RowHeadersDefaultCellStyle.SelectionForeColor = Color.Empty;

            // Set the background color for all rows and for alternating rows. 
            // The value for alternating rows overrides the value for all rows. 
            dgResults.RowsDefaultCellStyle.BackColor = Color.White;
            dgResults.RowsDefaultCellStyle.ForeColor = Color.DarkGreen;
            dgResults.AlternatingRowsDefaultCellStyle.BackColor = Color.LightGreen;
            dgResults.AlternatingRowsDefaultCellStyle.ForeColor = Color.Black;

            // Set the row and column header styles.
            dgResults.ColumnHeadersDefaultCellStyle.BackColor = Color.Navy;
            dgResults.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;

        }


        private void Script(DataGridViewRow row)
        {
            
            StringBuilder scriptFragment = new StringBuilder();
            string[] cols = new string[5];
            RowToStringArray(row, ref cols);
            if(cols[4] == "True")
            {
                scriptFragment.Append("EXEC tdc.usp_tdc_enable_table ");
                scriptFragment.Append("'"+cols[0].ToString()+"'");
                scriptFragment.Append(",");
                scriptFragment.Append("'" + cols[1].ToString() + "';");
                scriptFragment.Append("\r\nGO\r\n");
            }
            sqlScript.Append(scriptFragment.ToString());
        }
        // Helper to ease the getting of column values
        private void RowToStringArray(DataGridViewRow row, ref string[] cols)
        {
            for (int c = 0; c < 4; c++)
            {
                cols[c] = row.Cells[c].Value.ToString();
            }
            DataGridViewCheckBoxCell cell = row.Cells[4] as DataGridViewCheckBoxCell;
            if (cell.Value == null)
            {
                cols[4] = "false";
            }
            else
            {
                cols[4] = ((bool)cell.Value).ToString();
            }
        }
        public void SaveSQLScript()
        {
            saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.CreatePrompt = false;
            saveFileDialog1.FileName = DatabaseName + " Selected For TDE Scripts";
            saveFileDialog1.DefaultExt = ".sql";
            saveFileDialog1.OverwritePrompt = true;
            saveFileDialog1.InitialDirectory = @"C:\temp\";
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string filename = saveFileDialog1.FileName;
                FileMode mode = File.Exists(filename) ? FileMode.Truncate : FileMode.CreateNew;
                FileStream fs = File.Open(filename, mode, FileAccess.ReadWrite);
                
                AddText(fs, sqlScript.ToString());
                fs.Flush();
                fs.Close();
            }
        }

        private int ValidateTDCComponentsInstalledAndUpToDate(string databaseName)
        {
            int results = 0;
            ConnectionInfo.DatabaseName = databaseName;
            using (SqlConnection con = new SqlConnection(ConnectionInfo.ConnectionString))
            {
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                con.Open();
                cmd.CommandText = "SELECT count(*) from sys.schemas where name in ('tdc','"+databaseName+"_tdc_history')";
                int count = (int)cmd.ExecuteScalar();
                if (count < 2)
                {
                    results = 1;
                    return results;
                }
                cmd.CommandText = "select count(*) from sys.tables as t join sys.schemas as s on t.schema_id = s.schema_id where t.name = 'Version' and s.name = 'tdc';";
                int tblCount = (int)cmd.ExecuteScalar();
                if (tblCount != 1)
                {
                    results = 2;
                    return results;
                }
                cmd.CommandText = "SELECT tdc_version from tdc.Version;";
                string ver = (string)cmd.ExecuteScalar();
                if (ver != TDCVersion)
                {
                    results = 2;
                    return results;
                }
            }                
            return results;
        }
        private bool InstallTDCComponents()
        {
            bool results = true;
            using (SqlConnection con = new SqlConnection(ConnectionInfo.ConnectionString))
            {
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                con.Open();
                SQLScripts ss = new SQLScripts();
                foreach (string s in ss.Scripts)
                {
                    FileInfo file = new FileInfo(s);
                    string script = file.OpenText().ReadToEnd();
                    cmd.CommandText = script;
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (SqlException ex)
                    {
                        MessageBox.Show(ex.Message, ex.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        results = false;
                        con.Close();
                        return results;
                    }
                }
             }
            return results;
        }
    }
    #endregion Helper Functions
}
