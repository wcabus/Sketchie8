using System;
using System.Collections.Generic;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Input.Inking;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

namespace JustDraw.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private InkManager _inkManager = new InkManager();
        private uint _penID;
        private uint _touchID;
        private Point _previousContactPt;
        private Point _currentContactPt;
        private double x1;
        private double y1;
        private double x2;
        private double y2;

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            ////if (IsInDesignMode)
            ////{
            ////    // Code runs in Blend --> create design time data.
            ////}
            ////else
            ////{
            ////    // Code runs "for real"
            ////}

            InstantiateCommands();
        }

        private void InstantiateCommands ()
        {
            EraseCommand = new RelayCommand(() =>
                                                {
                                                    _inkManager.Mode = InkManipulationMode.Erasing;
                                                    var strokes = _inkManager.GetStrokes();

                                                    for (int i = 0; i < strokes.Count; i++)
                                                        strokes[i].Selected = true;

                                                    _inkManager.DeleteSelected();

                                                    DrawingCanvas.Background = CanvasBackground;
                                                    DrawingCanvas.Children.Clear();
                                                });

            SaveCommand = new RelayCommand(SaveDrawing);
        }

        private async void SaveDrawing()
        {
            try
            {
                FileSavePicker save = new FileSavePicker();
                save.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                save.DefaultFileExtension = ".png";
                save.FileTypeChoices.Add("PNG", new string[]{".png"});

                StorageFile fileSave = await save.PickSaveFileAsync();
                IOutputStream ab = await fileSave.OpenAsync(FileAccessMode.ReadWrite);
                if (ab != null)
                    await _inkManager.SaveAsync(ab);

                var dlg = new MessageDialog("Your image has been saved.");
                await dlg.ShowAsync();
            }
            catch (Exception ex)
            {
                var dlg = new MessageDialog(ex.Message);
                dlg.ShowAsync();
            }
        }

        private SolidColorBrush _canvasBackground = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
        private SolidColorBrush _canvasForeground = new SolidColorBrush(Color.FromArgb(255, 255,255,255));
        private double _strokeThickness = 4.0;

        public SolidColorBrush CanvasBackground
        {
            get { return _canvasBackground; }
            set
            {
                _canvasBackground = value;
                RaisePropertyChanged(() => CanvasBackground);
            }
        }

        public SolidColorBrush CanvasForeground
        {
            get { return _canvasForeground; }
            set
            {
                _canvasForeground = value;
                RaisePropertyChanged(() => CanvasForeground);
            }
        }

        public double StrokeThickness
        {
            get { return _strokeThickness; }
            set
            {
                _strokeThickness = value;
                RaisePropertyChanged(() => StrokeThickness);
            }
        }

        public RelayCommand EraseCommand { get; set; }
        public RelayCommand SaveCommand { get; set; }
        public RelayCommand SelectForeColorCommand { get; set; }

        public Canvas DrawingCanvas { get; set; }

        public void PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            //Where are we?
            PointerPoint pt = e.GetCurrentPoint(DrawingCanvas);
            _previousContactPt = pt.Position;

            //When drawing with a mouse device, only draw if the left button is pressed
            PointerDeviceType pointerDeviceType = e.Pointer.PointerDeviceType;
            if (pointerDeviceType == PointerDeviceType.Pen || pointerDeviceType == PointerDeviceType.Mouse && pt.Properties.IsLeftButtonPressed)
            {
                _inkManager.ProcessPointerDown(pt);
                _penID = pt.PointerId;

                e.Handled = true;
            }
            else if (pointerDeviceType == PointerDeviceType.Touch)
            {
                //Process touch input
            }
        }

        public void PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerId == _penID)
            {
                PointerPoint pt = e.GetCurrentPoint(DrawingCanvas);

                //Render a line using the selected foreground color as the pointer moves.
                _currentContactPt = pt.Position;
                if (Distance(_previousContactPt.X, _previousContactPt.Y, _currentContactPt.X, _currentContactPt.Y) > 1.0)
                {
                    Line line = new Line
                    {
                        X1 = _previousContactPt.X,
                        Y1 = _previousContactPt.Y,
                        X2 = _currentContactPt.X,
                        Y2 = _currentContactPt.Y,
                        StrokeThickness = StrokeThickness,
                        Stroke = CanvasForeground
                    };

                    _previousContactPt = _currentContactPt;

                    //Add the line to the canvas (draws it)
                    DrawingCanvas.Children.Add(line);

                    _inkManager.ProcessPointerUpdate(pt);
                }
            }
        }

        private double Distance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow((x2 - x1), 2) + Math.Pow((y2 - y1), 2));
        }

        public void PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerId == _penID)
            {
                PointerPoint pt = e.GetCurrentPoint(DrawingCanvas);

                _inkManager.ProcessPointerUp(pt);
            }

            _touchID = 0;
            _penID = 0;

            e.Handled = true;
        }
    }
}