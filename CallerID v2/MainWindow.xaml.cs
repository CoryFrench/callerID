using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.Generic;

namespace CallerID_v2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        CXHandler cxHandle;
        SettingsHandler settingsHandle;
        UpdateHandler updateHandle;
        LogHandler logHandle;

        public MainWindow()
        {
            InitializeComponent();
            this.Title = $"CallerID v{Properties.Resources.CURRENTVERSION}";
            cxHandle = new CXHandler();
            settingsHandle = new SettingsHandler();
            updateHandle = new UpdateHandler();
            logHandle = new LogHandler();
            logHandle.Start(cxHandle);
            cxHandle.Start(this);
            updateHandle.checkForUpdates();
        }

        #region Event Handlers

        private void OneClickCopyCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            // Hide and disable AutoCopyButton
            this.AutoCopyButton.Visibility = Visibility.Hidden;
            this.AutoCopyButton.IsEnabled = false;
            this.AutoCopyRow.Height = new GridLength(0);

            // Hide and disable SearchCopyButton
            this.SearchCopyButton.Visibility = Visibility.Hidden;
            this.SearchCopyButton.IsEnabled = false;
        }

        private void OneClickCopyCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            // Show and enable AutoCopyButton
            this.AutoCopyButton.Visibility = Visibility.Visible;
            this.AutoCopyButton.IsEnabled = true;
            this.AutoCopyRow.Height = new GridLength(1, GridUnitType.Star);

            // Show and enable SearchCopyButton
            this.SearchCopyButton.Visibility = Visibility.Visible;
            this.SearchCopyButton.IsEnabled = true;
        }

        private void ColorRadio_Click(object sender, RoutedEventArgs e)
        {
            RadioButton button = (RadioButton)sender;
            switch ((String)button.Content)
            {
                case "Red":
                    this.Background = Brushes.Red;
                    break;
                case "Green":
                    this.Background = Brushes.Green;
                    break;
                case "Blue":
                    this.Background = Brushes.Blue;
                    break;
                case "Yellow":
                    this.Background = Brushes.Yellow;
                    break;
                case "Black":
                    this.Background = Brushes.Black;
                    break;
                case "White":
                    this.Background = Brushes.White;
                    break;
                case "Custom":
                    if (IsLoaded)
                    {
                        float redInt = 0;
                        float greenInt = 0;
                        float blueInt = 0;
                        bool red = float.TryParse(this.SettingsColorRed.Text, out redInt);
                        bool green = float.TryParse(this.SettingsColorGreen.Text, out greenInt);
                        bool blue = float.TryParse(this.SettingsColorBlue.Text, out blueInt);
                        if (red && green && blue && redInt < 256 && greenInt < 256 && blueInt < 256)
                        {
                            this.Background = new SolidColorBrush(Color.FromArgb(255, (byte)redInt, (byte)greenInt, (byte)blueInt));
                        }
                    }
                    break;
            }
        }

        private void EditAddButton_Click(object sender, RoutedEventArgs e)
        {

            //Clear box after complete, change tab back to Auto
            if (this.EditTextbox.Text != string.Empty)
            {
                string toCheck = cxHandle.getLastNumber();
                toCheck = toCheck.Insert(6, "-");
                toCheck = toCheck.Insert(3, ")");
                toCheck = toCheck.Insert(0, "(");
                // Try to add current name to SQL DB with most recent number
                try
                {
                    using (MySql.Data.MySqlClient.MySqlConnection sqlConnection = new MySql.Data.MySqlClient.MySqlConnection(Properties.Resources.SQLConnectionString))
                    {
                        MySql.Data.MySqlClient.MySqlCommand sqlCommand = new MySql.Data.MySqlClient.MySqlCommand($@"INSERT INTO callerid(NAME,PHONE1) VALUES ('{EditTextbox.Text}','{toCheck}')");
                        sqlCommand.Connection = sqlConnection;
                        sqlConnection.Open();
                        sqlCommand.ExecuteNonQuery();
                        sqlConnection.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"SQL Edit Error\n{ex.Message}");
                }
                // Clear name field and return to main tab on adding
                this.EditTextbox.Text = String.Empty;
                this.Tabs.SelectedIndex = 0;
                // Force CID to update 
                cxHandle.forceRecheck = !cxHandle.forceRecheck;
            }
        }

        private void SearchCopyButton_Click(object sender, RoutedEventArgs e)
        {
            // Copy highlighted item from ListBox to clipboard
            try
            {
                if (SearchListPanel.SelectedIndex != -1)
                {
                    Clipboard.Clear();
                    Clipboard.SetText(SearchListPanel.SelectedItem.ToString());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy to clipboard\n{ex.Message}");
            }
        }

        private void SearchSearchButton_Click(object sender, RoutedEventArgs e)
        {
            //Clear old info prior to adding new info
            SearchListPanel.Items.Clear();

            long i = 0;
            bool isDigits = long.TryParse(this.SearchTextBox.Text, out i);
            if (this.SearchTextBox.Text.Length == 10 && isDigits)
            {
                List<string> nameList = cxHandle.getNamesFromSpecificNumber(SearchTextBox.Text);
                foreach (string s in nameList)
                {
                    this.SearchListPanel.Items.Add(s);
                }
            }
            else
            {
                this.SearchListPanel.Items.Add("Invalid number");
            }
            this.SearchTextBox.Text = String.Empty;

        }

        private void PreviousCallsClearButton_Click(object sender, RoutedEventArgs e)
        {
            this.PreviousCallsListBox.Items.Clear();
        }

        private void AutoListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.OneClickCopyCheckbox.IsChecked ?? false && AutoListBox.SelectedIndex != -1)
            {
                Clipboard.Clear();
                Clipboard.SetText(this.AutoListBox.SelectedItem.ToString());
            }
        }

        private void AutoCopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (AutoListBox.SelectedIndex != -1)
            {
                Clipboard.Clear();
                Clipboard.SetText(this.AutoListBox.SelectedItem.ToString());
            }
        }

        private void SettingsRatio_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded)
            {
                int boxInt = 0;
                int buttonInt = 0;
                bool box = int.TryParse(this.SettingsRatioBox.Text, out boxInt);
                bool button = int.TryParse(this.SettingsRatioButton.Text, out buttonInt);
                if (box && button)
                {
                    this.AutoBoxRow.Height = new GridLength(boxInt, GridUnitType.Star);
                    this.AutoCopyRow.Height = new GridLength(buttonInt, GridUnitType.Star);
                }
            }
        }

        private void CustomColor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded)
            {
                int redInt;
                int greenInt;
                int blueInt;
                bool red = int.TryParse(this.SettingsColorRed.Text, out redInt);
                bool green = int.TryParse(this.SettingsColorGreen.Text, out greenInt);
                bool blue = int.TryParse(this.SettingsColorBlue.Text, out blueInt);

                if (redInt < 256 && greenInt < 256 && blueInt < 256)
                {
                    this.ColorSample.Background = new SolidColorBrush(Color.FromArgb(255, (byte)redInt, (byte)greenInt, (byte)blueInt));
                    if (CustomColorRadio.IsChecked ?? false)
                    {
                        this.Background = new SolidColorBrush(Color.FromArgb(255, (byte)redInt, (byte)greenInt, (byte)blueInt));
                    }
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            settingsHandle.Save(this);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            settingsHandle.Load(this);
        }

        private void SearchListPanel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.OneClickCopyCheckbox.IsChecked ?? false && SearchListPanel.SelectedIndex != -1)
            {
                Clipboard.Clear();
                Clipboard.SetText(this.SearchListPanel.SelectedItem.ToString());
            }
        }
        #endregion

    }
}
