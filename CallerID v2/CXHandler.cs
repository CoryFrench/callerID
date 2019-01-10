using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using MySql.Data.MySqlClient;

namespace CallerID_v2
{
    class CXHandler
    {
        private static readonly string connString = CallerID_v2.Properties.Resources.SQLConnectionString;
        private static readonly string logLocation = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\3CXPhone for Windows\Logs\3CXPhoneForWindows.log";
        private static string currentNumber;
        public bool forceRecheck { get; set; }

        /// <summary>
        /// Default constructor, set forceRecheck false for add number to database
        /// </summary>
        public CXHandler()
        {
            forceRecheck = false;
        }

        /// <summary>
        /// Gets the names associated with the last number found in the 3CX log file.
        /// Returns "No calls" if no numbers found in log. (Checked)
        /// </summary>
        /// <returns>Returns List of strings from all names matching last number in 3CX log.</returns>
        public List<string> getNamesFromLastNumber()
        {
            string toCheck = getLastNumber();
            var toReturn = new List<string>();
            if (toCheck != string.Empty)
            {
                toCheck = toCheck.Insert(6, "-");
                toCheck = toCheck.Insert(3, ")");
                toCheck = toCheck.Insert(0, "(");
                if (toCheck != string.Empty)
                {
                    try
                    {

                        using (MySqlConnection connection = new MySqlConnection(connString))
                        {
                            MySqlCommand command = new MySqlCommand($"SELECT * FROM CallerID.callerid WHERE (PHONE1 LIKE '{toCheck}' OR PHONE2 LIKE '{toCheck}' OR PHONE3 LIKE '{toCheck}' OR PHONE4 LIKE '{toCheck}' OR " +
                            $"PHONE5 LIKE '{toCheck}' OR PHONE6 LIKE '{toCheck}' OR PHONE7 LIKE '{toCheck}' OR PHONE8 LIKE '{toCheck}')");
                            command.Connection = connection;
                            connection.Open();
                            MySqlDataReader reader = command.ExecuteReader();
                            while (reader.Read())
                            {
                                toReturn.Add(Convert.ToString(reader["NAME"]));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"SQL Error\n{ex.Message}");
                    }
                    toReturn.Add(toCheck);
                }
            }
            else
            {
                toReturn.Add("No calls in 3CX Log");
            }
            return toReturn;
        }

        /// <summary>
        /// Used by other UI parts, gets the names, if any, tied to a user specified number. (Checked)
        /// </summary>
        /// <param name="toCheck"> The number to check </param>
        /// <returns>Returns List of strings containing any numbers linked to user-provided number</returns>
        public List<string> getNamesFromSpecificNumber(string toCheck)
        {
            var _toCheck = toCheck;
            var toReturn = new List<string>();

            if (toCheck.Length == 10)
            {
                _toCheck = _toCheck.Insert(6, "-");
                _toCheck = _toCheck.Insert(3, ")");
                _toCheck = _toCheck.Insert(0, "(");
            }

            if (_toCheck != string.Empty)
            {
                try
                {
                    using (MySqlConnection connection = new MySqlConnection(connString))
                    {
                        MySqlCommand command = new MySqlCommand($"SELECT * FROM CallerID.callerid WHERE (PHONE1 LIKE '{_toCheck}' OR PHONE2 LIKE '{_toCheck}' OR PHONE3 LIKE '{_toCheck}' OR PHONE4 LIKE '{_toCheck}' OR " +
                        $"PHONE5 LIKE '{_toCheck}' OR PHONE6 LIKE '{_toCheck}' OR PHONE7 LIKE '{_toCheck}' OR PHONE8 LIKE '{_toCheck}')");
                        command.Connection = connection;
                        connection.Open();
                        MySqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            toReturn.Add(Convert.ToString(reader["NAME"]));
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"SQL Error\n{ex.Message}");
                }

                toReturn.Add(toCheck);
            }

            return toReturn;
        }

        /// <summary>
        /// Used by RefreshCallInfo, gets the last number present from the 3CX Log. (Checked)
        /// </summary>
        /// <returns>Returns string containing last number found in 3CX log</returns>
        public string getLastNumber()
        {
            string lastNumber = string.Empty;
            try
            {
                FileStream fs = new FileStream(logLocation, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using (StreamReader sr = new StreamReader(fs))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains("sip:+"))
                        {
                            lastNumber = line.Substring(line.IndexOf('+') + 2, line.IndexOf('@') - line.IndexOf('+') - 2);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to get last number\n{ex.Message}");
            }
            return lastNumber;
        }

        /// <summary>
        /// Starts the RefreshCallInfo method in background thread. Run once. (Checked)
        /// </summary>
        /// <param name="main">Passes in main UI window for editing, passed to RefreshCallInfo method</param>
        public void Start(MainWindow main)
        {
            try
            {
                Thread idThread = new Thread(() => RefreshCallInfo(main));
                idThread.IsBackground = true;
                idThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start CX thread\n{ex.Message}");
            }

        }

        /// <summary>
        /// Main CallerID method to check calls and update UI
        /// Only needs to be called once, loops forever. (Checked)
        /// </summary>
        /// <param name="main">Main UI Form</param>
        void RefreshCallInfo(MainWindow main)
        {
            while (true)
            {
                List<string> nameList = getNamesFromLastNumber();
                if (currentNumber != getLastNumber() || forceRecheck == true)
                {
                    // Update current number to most recent number in 3CX Log
                    currentNumber = getLastNumber();
                    try
                    {

                        main.Dispatcher.Invoke((Action)(() =>
                        {
                            main.AutoListBox.Items.Clear();
                            main.EditCurrentNumberText.Text = currentNumber;
                            foreach (string s in nameList)
                            {
                                main.AutoListBox.Items.Add(s);
                                main.PreviousCallsListBox.Items.Add(s);
                            }
                        }));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to update UI info\n{ex.Message}");
                    }
                    if (forceRecheck)
                    {
                        forceRecheck = !forceRecheck;
                    }
                }
                Thread.Sleep(1000);
            }
        }
    }
}
