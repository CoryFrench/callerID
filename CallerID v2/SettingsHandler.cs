using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace CallerID_v2
{
    class SettingsHandler
    {
        public void Save(MainWindow main)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\CallerIDSettings.ini";

            //Try to open file for writing
            try
            {
                using (StreamWriter sw = new StreamWriter(path))
                {
                    sw.WriteLine($"RGB:{main.SettingsColorRed.Text.PadLeft(3, '0')}{main.SettingsColorGreen.Text.PadLeft(3, '0')}{main.SettingsColorBlue.Text.PadLeft(3, '0')}");
                    sw.WriteLine($"TrimColor:{main.Background}");
                    sw.WriteLine($"Ratio:{main.SettingsRatioBox.Text},{main.SettingsRatioButton.Text}");
                    sw.WriteLine($"POS:{main.Top},{main.Left}");
                    sw.WriteLine($"Size:{main.Width},{main.Height}");
                    sw.WriteLine($"OneClick:{main.OneClickCopyCheckbox.IsChecked}");
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Failed to save settings\n{ex.Message}");
            }
        }

        public void Load(MainWindow main)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\CallerIDSettings.ini";
            if (File.Exists(path))
            {

                //Try to open file for reading
                try
                {
                    using (StreamReader sr = new StreamReader(path))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (line.StartsWith("OneClick:"))
                            {
                                if (line.Substring(line.IndexOf(":") + 1) == "True")
                                {
                                    main.OneClickCopyCheckbox.IsChecked = true;
                                }
                            }
                            if (line.StartsWith("RGB:"))
                            {
                                main.SettingsColorRed.Text = string.Concat(line[4].ToString() + line[5].ToString() + line[6].ToString());
                                main.SettingsColorGreen.Text = string.Concat(line[7].ToString() + line[8].ToString() + line[9].ToString());
                                main.SettingsColorBlue.Text = string.Concat(line[10].ToString() + line[11].ToString() + line[12].ToString());
                            }
                            if (line.StartsWith("TrimColor:"))
                            {
                                switch (line.Substring(line.IndexOf(":") + 1))
                                {
                                    case "#FFFF0000":
                                        main.RedColorRadio.IsChecked = true;
                                        main.Background = Brushes.Red;
                                        break;
                                    case "#FF008000":
                                        main.GreenColorRadio.IsChecked = true;
                                        main.Background = Brushes.Green;
                                        break;
                                    case "#FF0000FF":
                                        main.BlueColorRadio.IsChecked = true;
                                        main.Background = Brushes.Blue;
                                        break;
                                    case "#FF000000":
                                        main.BlackColorRadio.IsChecked = true;
                                        main.Background = Brushes.Black;
                                        break;
                                    default:
                                        main.CustomColorRadio.IsChecked = true;
                                        int redInt = int.Parse(main.SettingsColorRed.Text);
                                        int greenInt = int.Parse(main.SettingsColorGreen.Text);
                                        int blueInt = int.Parse(main.SettingsColorBlue.Text);
                                        main.ColorSample.Background = new SolidColorBrush(Color.FromArgb(255, (byte)redInt, (byte)greenInt, (byte)blueInt));
                                        main.Background = new SolidColorBrush(Color.FromArgb(255, (byte)redInt, (byte)greenInt, (byte)blueInt));
                                        break;

                                }
                            }
                            if (line.StartsWith("Ratio:"))
                            {
                                main.SettingsRatioBox.Text = line.Substring(line.IndexOf(':') + 1, line.IndexOf(',') - line.IndexOf(':') - 1);
                                main.SettingsRatioButton.Text = line.Substring(line.IndexOf(',') + 1);
                            }
                            if (line.StartsWith("POS:"))
                            {
                                main.Top = double.Parse(line.Substring(line.IndexOf(':') + 1, line.IndexOf(",") - line.IndexOf(":") - 1));
                                main.Left = double.Parse(line.Substring(line.IndexOf(',') + 1));
                            }
                            if (line.StartsWith("Size:"))
                            {
                                main.Width = double.Parse(line.Substring(line.IndexOf(':') + 1, line.IndexOf(",") - line.IndexOf(":") - 1));
                                main.Height = double.Parse(line.Substring(line.IndexOf(',') + 1));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load settings\n{ex.Message}");
                }
            }
        }

    }
}
