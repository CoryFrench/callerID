using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.IO;
using System.Threading;
using System.Windows;

namespace CallerID_v2
{
    class LogHandler
    {
        public void Start(CXHandler cx)
        {
            try
            {
                Thread t1 = new Thread(() => logging(cx));
                t1.IsBackground = true;
                t1.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start log thread\n{ex.Message}");
            }

        }

        private void logging(CXHandler cx)
        {
            while (true)
            {
                List<Call> calls = new List<Call>();
                List<Call> callsToRemove = new List<Call>();
                Dictionary<string, int> sipCount = new Dictionary<string, int>();
                bool incomingCall = false;
                bool customerHangNeedPhone = false;
                bool customerHangNeedDate = false;
                bool secondBye = false;
                string currentSip = "";
                int currentHighValue = 0;
                DateTime customerHang = new DateTime();
                string customerPhone = "";
                FileStream readStream = new FileStream(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\3CXPhone for Windows\Logs\3CXPhoneForWindows.log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using (StreamReader sr = new StreamReader(readStream))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        //Track SIP of each line
                        if (line.Contains("sip:") && line[line.IndexOf("sip:") + 7] == '@')
                        {
                            int sipIndex = line.IndexOf("sip:") + 4;
                            string sipValue = line.Substring(sipIndex, 3);
                            if (!sipCount.ContainsKey(sipValue))
                            {
                                sipCount.Add(sipValue, 1);
                            }
                            else
                            {
                                int currentValue = sipCount[sipValue];
                                currentValue++;
                                sipCount[sipValue] = currentValue;
                            }
                        }

                        //Search for "Outgoing call"
                        if (line.Contains("CREATING OUTGOING CALL TO"))
                        {
                            bool existsInCallList = false;
                            //Determine Phone#, StartTime
                            DateTime currentStart = DateTime.ParseExact(line.Substring(1, 19), "dd.MM.yyyy HH:mm:ss", null);
                            DateTime currentEnd = new DateTime();
                            string currentNumber = line.Substring(line.IndexOf("TO") + 3, line.IndexOf(',') - (line.IndexOf("TO")) - 3);
                            //Check that call doesn't already exist in "Calls" List
                            foreach (Call c in calls)
                            {
                                if (c._start == currentStart && c._callNumber == currentNumber)
                                {
                                    existsInCallList = true;
                                }
                            }
                            //Confirm call doesn't already exist in "Calls" List.
                            if (existsInCallList == false)
                            {
                                //Add call to "Calls" List if does not exist.
                                calls.Add(new Call(currentStart, currentEnd, currentNumber, true));
                            }
                        }

                        //Search for "Incoming call"
                        else if (line.Contains("CREATING INCOMING CALL"))
                        {
                            incomingCall = true;
                        }
                        else if (incomingCall)
                        {
                            if (line.Contains("parsing name from"))
                            {
                                incomingCall = false;
                                bool existsInCallList = false;
                                //Determine Phone#, StartTime
                                DateTime currentStart = DateTime.ParseExact(line.Substring(1, 19), "dd.MM.yyyy HH:mm:ss", null);
                                DateTime currentEnd = new DateTime();
                                string currentNumber = line.Substring(line.IndexOf('+') + 1, line.IndexOf('@') - (line.IndexOf("+")) - 1);
                                //Check that call doesn't already exist in "Calls" List
                                foreach (Call c in calls)
                                {
                                    if (c._start == currentStart && c._callNumber == currentNumber)
                                    {
                                        existsInCallList = true;
                                    }
                                }
                                //Confirm call doesn't already exist in "Calls" List.
                                if (existsInCallList == false)
                                {
                                    //Add call to "Calls" List if does not exist.
                                    calls.Add(new Call(currentStart, currentEnd, currentNumber, true));
                                }
                            }
                        }


                        ///
                        /// At this point, the 3CX call log uses several different codes to represent if the external party hangs up, or if the internal number hangs up.
                        /// These next lines deal with all of the possible scenarios that exist within the 3CX logs
                        ///

                        // If tech hangs up call, and call is to a external number
                        else if (line.Contains("stop transmitting to port 0") && line.Contains(":+"))
                        {
                            foreach (Call c in calls)
                            {
                                if (c._callLive == true && (c._callNumber == line.Substring(line.IndexOf(":+") + 2, line.IndexOf('@') - line.IndexOf("sip:") - 5) || c._callNumber == line.Substring(line.IndexOf(":+") + 3, line.IndexOf('@') - line.IndexOf("sip:") - 6)))
                                {
                                    c._callLive = false;
                                    c._end = DateTime.ParseExact(line.Substring(1, 19), "dd.MM.yyyy HH:mm:ss", null);
                                }
                            }
                        }
                        // If customer hangs up call, the phrase CSeq: 2 BYE appears twice. The first one indiciates that a hang up is in-progress, and the next few lines contain that information
                        // This seciton tells the program to begin looking for the needed information to log the call
                        else if (line.Contains("CSeq: 2 BYE") && secondBye == false)
                        {
                            customerHangNeedDate = true;
                            customerHangNeedPhone = true;
                            secondBye = true;
                        }
                        // This occurs after the 'customer hangs up' portion begins, and the line containing 'sip:' contains the customers number. This is logged here, and flagged no longer looking for that info.
                        else if (customerHangNeedPhone && line.Contains("sip:"))
                        {
                            customerPhone = line.Substring(line.IndexOf("sip:") + 4, line.IndexOf('@') - (line.IndexOf("sip:")) - 4).Trim('+');
                            customerHangNeedPhone = false;
                        }
                        // This occurs after the 'customer hangs up' portion begins, and the line containing 'pjsua_core.c' contains the date and time of the call hang up.
                        // This flags the call as no longer looking for that info.
                        else if (customerHangNeedDate && line.Contains("pjsua_core.c"))
                        {
                            customerHang = DateTime.ParseExact(line.Substring(1, 19), "dd.MM.yyyy HH:mm:ss", null);
                            customerHangNeedDate = false;
                        }
                        // The second CSeq: 2 BYE indicates that the portion containing the call information has ended
                        else if (line.Contains("CSeq: 2 BYE") && secondBye == true)
                        {
                            secondBye = false;

                            foreach (Call c in calls)
                            {
                                // Matches the found customer phone # against the open calls list, and marks the call matching the customer # to the open call, closing it and finalizing call info.
                                // Hypothetically if two people called from the same number at the same time somehow, this could cause issues. To the best of my knowledge this isn't a possibility, however.
                                if (c._callLive == true && (c._callNumber == customerPhone || c._callNumber == ("1" + customerPhone)))
                                {
                                    c._callLive = false;
                                    c._end = customerHang;
                                }
                            }
                            customerHang = new DateTime();
                            customerPhone = "";
                        }
                    }


                    //After checking all calls, check to see if calls are already in Database. 
                    foreach (Call c in calls)
                    {
                        if (c._callLive == true)
                        {
                            callsToRemove.Add(c);
                        }
                        using (MySqlConnection checkerConnection = new MySqlConnection(CallerID_v2.Properties.Resources.SQLConnectionString))
                        {
                            MySqlCommand checkerCommand = new MySqlCommand($@"Select COUNT(*) FROM CallerID.callog WHERE NUMBER=@number AND START=@start");
                            checkerCommand.Parameters.AddWithValue("number", c._callNumber);
                            checkerCommand.Parameters.AddWithValue("start", c._start);
                            checkerCommand.Connection = checkerConnection;
                            checkerConnection.Open();
                            var result = checkerCommand.ExecuteScalar();
                            checkerConnection.Close();
                            if (Convert.ToInt16(result) > 0)
                            {
                                callsToRemove.Add(c);
                            }
                        }
                    }
                    //Removes calls from active list that already exist in the call database
                    foreach (Call c in callsToRemove)
                    {
                        if (calls.Contains(c))
                        {
                            calls.Remove(c);
                        }
                    }

                    //Determine Extension of user
                    foreach (KeyValuePair<string, int> kvp in sipCount)
                    {
                        if (kvp.Value > currentHighValue)
                        {
                            currentSip = kvp.Key;
                            currentHighValue = kvp.Value;
                        }
                    }
                    //Add remaining calls to database from "Calls" List.
                    foreach (Call C in calls)
                    {
                        try
                        {

                            string phoneToCheck = "";
                            if (C._callNumber == "*63") { phoneToCheck = "*63"; };
                            if (C._callNumber == "*62") { phoneToCheck = "*62"; };
                            if (C._callNumber == "420") { phoneToCheck = "420"; };
                            if (C._callNumber.Length > 10)
                            {
                                phoneToCheck = "(" + C._callNumber[1] + C._callNumber[2] + C._callNumber[3] + ")" + C._callNumber[4] + C._callNumber[5] + C._callNumber[6] + "-" + C._callNumber[7] + C._callNumber[8] + C._callNumber[9] + C._callNumber[10];
                            }
                            using (MySqlConnection submitConnection = new MySqlConnection(CallerID_v2.Properties.Resources.SQLConnectionString))
                            {
                                List<string> names = cx.getNamesFromSpecificNumber(phoneToCheck);
                                string logName = "";
                                if (names.Count != 0)
                                {
                                    for (int x = 0; x < names.Count - 1; x++)
                                    {
                                        if (x == names.Count - 2)
                                            logName += names[x];
                                        else
                                            logName += names[x] + ",";
                                    }
                                }
                                MySqlCommand submitCommand = new MySqlCommand($@"INSERT INTO CallerID.callog (EXTENSION, START, STOP, NUMBER, NAME) VALUES ('{currentSip}','{C._start.ToString("yyyy-MM-dd HH:mm:ss")}','{C._end.ToString("yyyy-MM-dd HH:mm:ss")}','{C._callNumber}','{logName}')");
                                submitCommand.Connection = submitConnection;
                                submitConnection.Open();
                                submitCommand.ExecuteNonQuery();
                                submitConnection.Close();
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Log SQL Error\n{ex.Message}");
                        }
                    }
                };
                Thread.Sleep(60000);
            }
        }
    }
}
