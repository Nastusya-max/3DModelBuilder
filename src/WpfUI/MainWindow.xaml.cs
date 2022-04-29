using FellowOakDicom;
using FellowOakDicom.Imaging;
using Microsoft.Win32;
using ModelBuilder.Builders.DualContouring;
using ModelBuilder.Core.Extensions;
using ModelBuilder.Surfaces.DicomSurface;
using System;
using System.IO;
using System.Linq;
using System.Windows;
namespace WpfUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string DicomDirectoryFilename = "DICOMDIR";
        private const string ObjExtension = ".obj";

        private DicomDirectoryReader _directoryReader;

        public MainWindow()
        {
            InitializeComponent();

            new DicomSetupBuilder()
                .RegisterServices(x => x.AddImageManager<ImageSharpImageManager>())
                .Build();
        }

        private void UpdateSeriesSelectState()
        {
            seriesSelect.SelectedItem = null;
            seriesSelect.Items.Clear();
            seriesSelect.IsEnabled = false;

            if (!File.Exists(input.Text) || Path.GetFileName(input.Text) != DicomDirectoryFilename)
            {
                return;
            }
            var options = DicomSurfaceOptions.Default;
            options.ActivationThreshold = 100;
            _directoryReader = DicomDirectoryReader.Open(input.Text, options);
            var seriesInfos = _directoryReader.GetAllPatients()
                .SelectMany(x => x.Studies.SelectMany(y => y.Series.Select(z => new SeriesInfo
                {
                    Id = z.Id,
                    Title = $"{x.Name} {z.ImagesCount} images",
                }))).ToArray();

            foreach (var item in seriesInfos)
            {
                seriesSelect.Items.Add(item);
            }

            seriesSelect.IsEnabled = true;
        }

        private void UpdateBuildButtonState()
        {
            buildBtn.IsEnabled = false;
            if (!seriesSelect.IsEnabled || seriesSelect.SelectedItem == null)
            {
                return;
            }

            var outputDirectory = Path.GetDirectoryName(output.Text);
            if (!Directory.Exists(outputDirectory))
            {
                return;
            }

            var outputFilenameExtension = Path.GetExtension(output.Text);
            if (outputFilenameExtension != ObjExtension)
            {
                return;
            }

            buildBtn.IsEnabled = true;
        }

        private void inputBtn_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.CheckFileExists = true;
            openFileDialog.Filter = $"DICOMDIR|{DicomDirectoryFilename}";
            if (openFileDialog.ShowDialog() == false)
            {
                return;
            }

            input.Text = openFileDialog.FileName;
            UpdateSeriesSelectState();
        }

        private void outputBtn_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Wavefront|*.obj";
            saveFileDialog.CheckPathExists = true;
            saveFileDialog.InitialDirectory = Path.GetDirectoryName(input.Text) ?? "";
            saveFileDialog.FileName = "output.obj";
            if (saveFileDialog.ShowDialog() == false)
            {
                return;
            }

            output.Text = saveFileDialog.FileName;
            UpdateBuildButtonState();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var seriesInfo = seriesSelect.SelectedItem as SeriesInfo;
            var surface = _directoryReader.GetSurface(seriesInfo.Id);

            var options = DualContouringBuilderOptions.Default;
            options.ChangePointSelectionMode = ChangePointSelectionMode.BestPointSelection;
            options.CellSize = 2.5f;
            var builder = new DualContouringBuilder(options);
            var mesh = builder.Build(surface, surface.SurfaceBounds);

            mesh.WriteToObj(output.Text);

            MessageBox.Show("Operation completed");
        }

        private void seriesSelect_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateBuildButtonState();
        }
    }

    class SeriesInfo
    {
        public string Title { get; set; }

        public string Id { get; set; }

        public override string ToString()
        {
            return Title;
        }
    }
}
