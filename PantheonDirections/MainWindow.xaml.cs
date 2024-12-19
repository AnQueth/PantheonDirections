using System.IO;
using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Tesseract;
using System.Drawing;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PantheonDirections
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //
        private List<Coordinate> coordinatesList = new List<Coordinate>();
        private const string CoordinatesFilePath = "coordinates.json";


        public MainWindow()
        {
            InitializeComponent();
            LoadCoordinatesList();
            DisplayCoordinatesList();
        }

        private (double x, double y) Parse(string text)
        {
            string pattern = @"([\d\.\-]+) ([\d\.\-]+) ([\d\.\-]+) ([\d\.\-]+)";
            Regex regex = new Regex(pattern);
            Match match = regex.Match(text);
            if (match.Success)
            {
                return (double.Parse(match.Groups[1].Value), double.Parse(match.Groups[3].Value));

            }
            return default;
        }

        private void SaveCoordinatesList()
        {
            var json = JsonSerializer.Serialize(coordinatesList);
            File.WriteAllText(CoordinatesFilePath, json);
        }

        private void LoadCoordinatesList()
        {
            if (File.Exists(CoordinatesFilePath))
            {
                var json = File.ReadAllText(CoordinatesFilePath);
                coordinatesList = JsonSerializer.Deserialize<List<Coordinate>>(json) ?? new List<Coordinate>();
            }
        }

        private (string x, string y) DoOCR()
        {
            if (Clipboard.ContainsImage())
            {
                var image = Clipboard.GetImage();
                var bitmapSource = ConvertToBitmap(image);

                PastedImage.Source = bitmapSource;

                // Perform OCR
                string ocrResult = PerformOcr(bitmapSource);
                OcrResultTextBox.Text = ocrResult;

                (var x, var y) = Parse(ocrResult);

                return (x.ToString(), y.ToString());


            }
            else
            {
                MessageBox.Show("Clipboard does not contain an image.");
            }

            return default;
        }

        private void PasteImageTarget_Click(object sender, RoutedEventArgs e)
        {
            (var x, var y) = DoOCR();
            TargetX.Text = x?.ToString();
            TargetY.Text = y?.ToString();
        }
        private void PasteImageInitial_Click(object sender, RoutedEventArgs e)
        {
            (var x, var y) = DoOCR();
            InitialX.Text = x?.ToString();
            InitialY.Text = y?.ToString();


        }

        private BitmapSource ConvertToBitmap(BitmapSource source)
        {
            var encoder = new PngBitmapEncoder();
            var memoryStream = new MemoryStream();
            var bitmapFrame = BitmapFrame.Create(source);
            encoder.Frames.Add(bitmapFrame);
            encoder.Save(memoryStream);
            return BitmapFrame.Create(memoryStream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        }

        private string PerformOcr(BitmapSource bitmapSource)
        {
            string ocrResult = string.Empty;
            try
            {
                Bitmap bitmap = BitmapFromSource(bitmapSource);

                using (var engine = new TesseractEngine(@"./tessdata ", "eng", EngineMode.Default))
                {
                    using (var img = PixConverter.ToPix(bitmap))
                    {
                        using (var page = engine.Process(img))
                        {
                            ocrResult = page.GetText();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ocrResult = "Error: " + ex.Message;
            }

            return ocrResult;
        }

        private Bitmap BitmapFromSource(BitmapSource bitmapsource)
        {
            Bitmap bitmap;
            using (var outStream = new MemoryStream())
            {
                var enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapsource));
                enc.Save(outStream);
                bitmap = new Bitmap(outStream);
            }
            return bitmap;
        }

        private void CalculateDirection_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(InitialX.Text, out double x1) &&
                double.TryParse(InitialY.Text, out double y1) &&
                double.TryParse(TargetX.Text, out double x2) &&
                double.TryParse(TargetY.Text, out double y2))
            {
                FindDirection(x1, y1, x2, y2);
            }
            else
            {
                MessageBox.Show("Please enter valid coordinates.");
            }
        }

        private void SaveCoordinates_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(TargetX.Text, out double x2) &&
                double.TryParse(TargetY.Text, out double y2))
            {
                var description = Description.Text;
                var coords = new Coordinate { Description = description, X = x2, Y = y2 };
                coordinatesList.Add(coords);
                DisplayCoordinatesList();
                SaveCoordinatesList();
                MessageBox.Show("Coordinates saved successfully!");
            }
            else
            {
                MessageBox.Show("Please enter valid target coordinates.");
            }
        }

        private void DisplayCoordinatesList()
        {
            CoordinatesList.Children.Clear();
            foreach (var coords in coordinatesList)
            {
                var entryStackPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
                var entryTextBlock = new TextBlock { Text = $"{coords.Description}: ({coords.X}, {coords.Y})", Margin = new Thickness(5) };
                var getDirectionButton = new Button { Content = "Get Direction", Margin = new Thickness(5) };
                getDirectionButton.Click += (s, e) => GetDirection(coords.X, coords.Y);
                var deleteButton = new Button { Content = "Delete", Margin = new Thickness(5) };
                deleteButton.Click += (s, e) => DeleteCoordinate(coords);

                entryStackPanel.Children.Add(entryTextBlock);
                entryStackPanel.Children.Add(getDirectionButton);
                entryStackPanel.Children.Add(deleteButton);
                CoordinatesList.Children.Add(entryStackPanel);
            }
        }

        private void GetDirection(double targetX, double targetY)
        {
            if (double.TryParse(InitialX.Text, out double x1) &&
                double.TryParse(InitialY.Text, out double y1))
            {
                FindDirection(x1, y1, targetX, targetY);
            }
            else
            {
                MessageBox.Show("Please enter valid initial coordinates.");
            }
        }

        private void FindDirection(double x1, double y1, double x2, double y2)
        {
            string directionX = x2 > x1 ? "East" : x2 < x1 ? "West" : "";
            string directionY = y2 > y1 ? "North" : y2 < y1 ? "South" : "";
            string direction = !string.IsNullOrEmpty(directionX) && !string.IsNullOrEmpty(directionY) ? $"{directionY} {directionX}" :
                               !string.IsNullOrEmpty(directionX) ? directionX :
                               !string.IsNullOrEmpty(directionY) ? directionY : "Same Point";

            ResultTextBlock.Text = "Direction: " + direction;
        }

        private void DeleteCoordinate(Coordinate coords)
        {
            coordinatesList.Remove(coords);
            SaveCoordinatesList();
            DisplayCoordinatesList();
        }


        public class Coordinate
        {
            public string Description { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
        }
    }


}

