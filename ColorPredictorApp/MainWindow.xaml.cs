using Microsoft.ML;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ColorPredictorApp
{
    public partial class MainWindow : Window
    {
        // Stores the path of the currently selected image
        private string currentImagePath;

        // Constructor to initialize the main window
        public MainWindow()
        {
            InitializeComponent();
        }

        // Event handler to close the application when the close button is clicked
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Event handler to minimize the application when the minimize button is clicked
        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // Allows the user to drag the window by holding the left mouse button
        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        // Opens a file dialog to allow the user to select an image
        private void OnChooseImageClicked(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image (*.jpg;*.png)|*.jpg;*.png"
            };

            if (dialog.ShowDialog() == true)
            {
                currentImagePath = dialog.FileName;
                ImagePreview.Source = new BitmapImage(new Uri(currentImagePath));
            }
        }

        // Event handler to predict colors in the selected image
        private void OnPredictClicked(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentImagePath))
            {
                MessageBox.Show("First load the image.");
                return;
            }

            int colorCount = ColorCountComboBox.SelectedIndex + 1;
            var predictedColors = PredictColors(currentImagePath, colorCount);
            ShowPredictedColors(predictedColors);
        }

        // Predicts colors in an image using ML.NET models
        private List<Color> PredictColors(string imagePath, int numberOfColors)
        {
            var mlContext = new MLContext();
            var colors = new List<Color>(); // Stores the predicted colors

            int startingModelIndex = 0;

            // Determines the starting model index based on the number of colors
            switch (numberOfColors)
            {
                case 1: startingModelIndex = 1; break;
                case 2: startingModelIndex = 2; break;
                case 3: startingModelIndex = 4; break;
                case 4: startingModelIndex = 7; break;
                case 5: startingModelIndex = 11; break;
                default:
                    MessageBox.Show("Unsupported number of colors.");
                    return colors;
            }

            // Loops through the number of colors to predict each color
            for (int i = 0; i < numberOfColors; i++)
            {
                byte r = 0, g = 0, b = 0;

                // Predicts each color channel (R, G, B) using separate models
                foreach (var channel in new[] { "R", "G", "B" })
                {
                    int modelIndex = startingModelIndex + i;
                    string modelPath = System.IO.Path.Combine("Models", $"model{modelIndex}_{channel}.zip");

                    if (!File.Exists(modelPath))
                    {
                        MessageBox.Show($"Missing model: {modelPath}");
                        return colors;
                    }

                    ITransformer model;
                    using (var stream = new FileStream(modelPath, FileMode.Open, FileAccess.Read))
                    {
                        model = mlContext.Model.Load(stream, out _);
                    }

                    var predictionEngine = mlContext.Model.CreatePredictionEngine<ColorData, PredictionResult>(model);
                    var prediction = predictionEngine.Predict(new ColorData { ImagePath = imagePath });
                    byte value = (byte)Math.Clamp(prediction.Score, 0, 255);

                    // Assigns the predicted value to the appropriate channel
                    switch (channel)
                    {
                        case "R": r = value; break;
                        case "G": g = value; break;
                        case "B": b = value; break;
                    }
                }

                colors.Add(Color.FromRgb(r, g, b));
            }

            return colors; // Returns the list of predicted colors
        }

        // Displays the predicted colors in the UI
        private void ShowPredictedColors(List<Color> colors)
        {
            ColorResultPanel.Children.Clear();

            foreach (var color in colors)
            {
                var rgbText = $"RGB({color.R}, {color.G}, {color.B})";
                var hexText = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

                var stack = new StackPanel
                {
                    Margin = new Thickness(5),
                    Width = 128
                };

                // Adds a color preview block
                stack.Children.Add(new Border
                {
                    Height = 64,
                    Background = new SolidColorBrush(color),
                    BorderThickness = new Thickness(1)
                });

                // Adds the RGB text below the color block
                stack.Children.Add(new TextBlock
                {
                    Text = rgbText,
                    TextAlignment = TextAlignment.Center,
                    Foreground = new SolidColorBrush(Colors.White),
                    Margin = new Thickness(0, 5, 0, 0)
                });

                // Adds the HEX text below the RGB text
                stack.Children.Add(new TextBlock
                {
                    Text = hexText,
                    TextAlignment = TextAlignment.Center,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Colors.White)
                });

                ColorResultPanel.Children.Add(stack);
            }
        }
    }
}