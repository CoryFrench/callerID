using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Reflection;
using System.Windows;
using System.Net;

namespace CallerID_v2
{
    class UpdateHandler
    {
        public void checkForUpdates()
        {
            //Check SQL database for latest version #
            string newestVersion = String.Empty;
            try
            {
                using (MySqlConnection sqlConnection = new MySqlConnection(CallerID_v2.Properties.Resources.SQLConnectionString))
                {
                    MySqlCommand sqlCommand = new MySqlCommand("SELECT VALUE FROM calleridadmin WHERE PROPERTY='CURRENTVERSION'");
                    sqlCommand.Connection = sqlConnection;
                    sqlConnection.Open();
                    newestVersion = (string)sqlCommand.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to get newest version info\n{ex.Message}");
            }

            //If there's a newer version available, open web location containing download button
            if (newestVersion != String.Empty && newestVersion != CallerID_v2.Properties.Resources.CURRENTVERSION)
            {
                try
                {
                    var result = MessageBox.Show($"Version {newestVersion} available!\nWould you like to download it now?", $"CallerIDv{CallerID_v2.Properties.Resources.CURRENTVERSION}", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start("https://cidhost.weebly.com/");
                        System.Windows.Application.Current.Shutdown();
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show($"Failed to open web address\n{ex.Message}");
                }
                
            }
        }
    }
}
