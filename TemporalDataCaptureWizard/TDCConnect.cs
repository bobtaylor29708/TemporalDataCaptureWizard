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
using System;
using System.Data.SqlClient;
using System.Windows.Forms;
using ToolManTaylor;

namespace TemporalDataCaptureWizard
{
    public partial class TDCConnectToSQL : ConnectToSQLBase
    {
        public TDCConnectToSQL()
        {
            InitializeComponent();
            MinimumVersion = 12;
        }
        public override void DoWork(SqlConnection con, Microsoft.SqlServer.Management.Common.SqlConnectionInfo sci)
        {
            try
            {
                con.Open();
                SqlCommand cmd = new SqlCommand("SELECT SERVERPROPERTY('ProductVersion')", con);
                string verison = (string)cmd.ExecuteScalar();
                con.Close();
                string[] verParts = verison.Split('.');

                // Must be SQL Azure Database or SQL Server 2016 or later
                if ((Convert.ToInt32(verParts[0]) >= MinimumVersion && sci.Authentication == SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryIntegrated) ||
                    (Convert.ToInt32(verParts[0]) >= MinimumVersion && sci.Authentication == SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryPassword) ||
                        (Convert.ToInt32(verParts[0]) >= MinimumVersion + 1 && sci.Authentication == SqlConnectionInfo.AuthenticationMethod.NotSpecified))
                {
                    // Now present the main screen to the user for selection of database and processing options
                    Results r = new Results(con);
                    r.ConnectionInfo = sci;
                    r.ConnectionTimeout = ConnectionTimeout;
                    r.ExecutionTimeout = ExecutionTimeout;
                    r.ShowDialog();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Temporal Data Capture is only available on SQL Azure Database or SQL Server 2016 or later.", "Temporal Data Capture");
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
    }
}