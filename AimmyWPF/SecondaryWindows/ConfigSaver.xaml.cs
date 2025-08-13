using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace SecondaryWindows
{
    /// <summary>
    /// Interaction logic for ConfigSaver.xaml
    /// </summary>
    public partial class ConfigSaver : Window
    {
        public Dictionary<string, dynamic> aimmySettings = new Dictionary<string, dynamic>();

        private string ExtraStrings = string.Empty;

        public ConfigSaver(Dictionary<string, dynamic> CurrentAimmySettings, string lastLoadedModel)
        {
            InitializeComponent();
            aimmySettings = CurrentAimmySettings;

            if (lastLoadedModel != "N/A")
            {
                RecommendedModelNameTextBox.Text = lastLoadedModel.Split(".")[0];
            }
        }

        private static string GetSafeConfigFilePath(string rawName)
        {
            string baseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "configs");
            Directory.CreateDirectory(baseDirectory);

            string nameOnly = rawName ?? string.Empty;
            nameOnly = nameOnly.Trim();

            // Replace invalid characters and collapse whitespace
            var invalid = Path.GetInvalidFileNameChars();
            nameOnly = new string(nameOnly.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
            nameOnly = Regex.Replace(nameOnly, "\\s+", " ").Trim();

            if (string.IsNullOrWhiteSpace(nameOnly))
            {
                nameOnly = "config";
            }

            // Ensure .cfg extension
            if (!nameOnly.EndsWith(".cfg", StringComparison.OrdinalIgnoreCase))
            {
                nameOnly += ".cfg";
            }

            string combined = Path.Combine(baseDirectory, nameOnly);

            // Normalize and ensure it stays within baseDirectory
            string fullPath = Path.GetFullPath(combined);
            string fullBase = Path.GetFullPath(baseDirectory) + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(fullBase, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Invalid configuration file name.");
            }

            return fullPath;
        }

        private void WriteJSON()
        {
            try
            {
                var extendedSettings = new Dictionary<string, object>();
                foreach (var kvp in aimmySettings)
                {
                    if (kvp.Key == "Suggested_Model")
                    {
                        if (RecommendedModelNameTextBox.Text != string.Empty)
                            extendedSettings[kvp.Key] = RecommendedModelNameTextBox.Text + ".onnx" + ExtraStrings;
                        else
                            extendedSettings[kvp.Key] = "";
                    }
                    else
                        extendedSettings[kvp.Key] = kvp.Value;
                }

                // Add topmost
                extendedSettings["TopMost"] = this.Topmost ? true : false;

                string json = JsonConvert.SerializeObject(extendedSettings, Formatting.Indented);
                string safePath = GetSafeConfigFilePath(ConfigNameTextbox.Text);
                File.WriteAllText(safePath, json);
            }
            catch (Exception x)
            {
                Console.WriteLine("Error saving configuration: " + x.Message);
            }

            new NoticeBar("Config has been saved to bin/configs.").Show();

            this.Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string safePath;
            try
            {
                safePath = GetSafeConfigFilePath(ConfigNameTextbox.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Aimmy - Configuration Saver");
                return;
            }

            if (File.Exists(safePath))
            {
                if (MessageBox.Show("A config already exists with the same name, would you like to overwrite it?",
                    "Aimmy - Configuration Saver", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    WriteJSON();
            }
            else
                WriteJSON();
        }

        private void DownloadableModelCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ExtraStrings = " (Found in Downloadable Model menu)";
            Storyboard Animation = (Storyboard)TryFindResource("EnableSwitch");
            Animation.Begin();
        }

        private void DownloadableModelCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ExtraStrings = "";
            Storyboard Animation = (Storyboard)TryFindResource("DisableSwitch");
            Animation.Begin();
        }

        #region Window Controls

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        #endregion Window Controls
    }
}