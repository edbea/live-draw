﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Brush = System.Windows.Media.Brush;
using Point = System.Windows.Point;

namespace AntFu7.LiveDraw   
{
    public partial class MainWindow : Window
    {
        private static readonly Duration Duration1 = (Duration)Application.Current.Resources["Duration1"];
        private static readonly Duration Duration2 = (Duration)Application.Current.Resources["Duration2"];
        private static readonly Duration Duration3 = (Duration)Application.Current.Resources["Duration3"];
        private static readonly Duration Duration4 = (Duration)Application.Current.Resources["Duration4"];
        private static readonly Duration Duration5 = (Duration)Application.Current.Resources["Duration5"];
        private static readonly Duration Duration7 = (Duration)Application.Current.Resources["Duration7"];
        private static readonly Duration Duration10 = (Duration)Application.Current.Resources["Duration10"];

        private static Mutex mutex = new Mutex(true, "alldream-livedraw");

        /*#region Mouse Throught

        private const int WsExTransparent = 0x20;
        private const int GwlExstyle = (-20);

        [DllImport("user32", EntryPoint = "SetWindowLong")]
        private static extern uint SetWindowLong(IntPtr hwnd, int nIndex, uint dwNewLong);

        [DllImport("user32", EntryPoint = "GetWindowLong")]
        private static extern uint GetWindowLong(IntPtr hwnd, int nIndex);

        private void SetThrought(bool t)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            var extendedStyle = GetWindowLong(hwnd, GwlExstyle);
            if (t)
                SetWindowLong(hwnd, GwlExstyle, extendedStyle | WsExTransparent);
            else
                SetWindowLong(hwnd, GwlExstyle, extendedStyle & ~(uint)WsExTransparent);
        }


        #endregion*/

        #region /---------Lifetime---------/

        public MainWindow()
        {
            if (mutex.WaitOne(TimeSpan.Zero, true))
            {

                _history = new Stack<StrokesHistoryNode>();
                _redoHistory = new Stack<StrokesHistoryNode>();
                if (!Directory.Exists("Save"))
                    Directory.CreateDirectory("Save");

                InitializeComponent();

                SetColor(DefaultColorPicker);
                
                SetEnable(false,_mode);
                
                SetTopMost(true);
                
                SetBrushSize(2);

                ExtraToolPanel.Opacity = 0;

                MainInkCanvas.Strokes.StrokesChanged += StrokesChanged;
                //RightDocking();

            }
            else
            {
                Application.Current.Shutdown(0);
            }
        }

        private void Exit(object sender, EventArgs e)
        {
            if (IsUnsaved()) 
                QuickSave("ExitingAutoSave_");

            Application.Current.Shutdown(0);
        }

        #endregion


        #region /---------Judge--------/

        private bool _saved;

        private bool IsUnsaved()
        {
            return MainInkCanvas.Strokes.Count != 0 && !_saved;
        }

        private bool PromptToSave()
        {
            if (!IsUnsaved())
                return true;
            var r = MessageBox.Show("You have unsaved work, do you want to save it now?", "Unsaved data",
                MessageBoxButton.YesNoCancel);
            if (r == MessageBoxResult.Yes || r == MessageBoxResult.OK)
            {
                QuickSave();
                return true;
            }
            if (r == MessageBoxResult.No || r == MessageBoxResult.None)
                return true;
            return false;
        }

        #endregion


        #region /---------Setter---------/

        private ColorPicker _selectedColor;
        private bool _inkVisibility = true;
        private bool _displayExtraToolPanel = false;
        private bool _eraserMode = false;
        private bool _enable = false;
        private readonly int[] _brushSizes = { 2, 5, 8, 13, 20 };
        private int _brushIndex = 0;
        private bool _displayOrientation;
        private DrawMode _mode = DrawMode.Pen;
        private InkCanvasEditingMode _lastEditingMode = InkCanvasEditingMode.Ink;

        private void SetExtralToolPanel(bool v)
        {
            if (v)
            {
                DetailTogglerRotate.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(180, Duration5));
                //DefaultColorPicker.Size = ColorPickerButtonSize.Middle;
                ExtraToolPanel.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, Duration4));
                //PaletteGrip.BeginAnimation(WidthProperty, new DoubleAnimation(130, Duration3));
                //MinimizeButton.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, Duration3));
                //MinimizeButton.BeginAnimation(HeightProperty, new DoubleAnimation(0, 25, Duration3));
            }
            else
            {
                DetailTogglerRotate.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(0, Duration5));
                //DefaultColorPicker.Size = ColorPickerButtonSize.Small;
                ExtraToolPanel.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0, Duration4));
                //PaletteGrip.BeginAnimation(WidthProperty, new DoubleAnimation(80, Duration3));
                //MinimizeButton.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0, Duration3));
                //MinimizeButton.BeginAnimation(HeightProperty, new DoubleAnimation(25, 0, Duration3));
            }
            _displayExtraToolPanel = v;
        }
        private void SetInkVisibility(bool v)
        {
            MainInkCanvas.BeginAnimation(OpacityProperty,
                v ? new DoubleAnimation(0, 1, Duration3) : new DoubleAnimation(1, 0, Duration3));
            HideButton.IsActived = !v;
            SetEnable(v,_mode);
            _inkVisibility = v;
        }
        private void SetEnable(bool b,DrawMode mode)
        {
            EnableButton.IsActived = !b;
            Background = Application.Current.Resources[b ? "FakeTransparent" : "TrueTransparent"] as Brush;
            _enable = b;
            _mode = mode;

            switch (_mode)
            {
                case DrawMode.Pen:
                    break;
                case DrawMode.Text:
                    break;
                case DrawMode.Line:
                    break;
                case DrawMode.Arrow:
                    break;
                case DrawMode.Rectangle:
                    break;
                case DrawMode.Circle:
                    break;
                case DrawMode.Ray:
                    break;
                default:
                    _mode = DrawMode.Pen;
                    break;
            }

            _lastEditingMode = _mode == DrawMode.Pen ? InkCanvasEditingMode.Ink : InkCanvasEditingMode.None;
            if(_eraserMode == false)
            {
                MainInkCanvas.EditingMode = _lastEditingMode;
            }

            PenButton.IsActived = _enable == true && _mode == DrawMode.Pen;
            TextButton.IsActived = _enable == true && _mode == DrawMode.Text;
            LineButton.IsActived = _enable == true && _mode == DrawMode.Line;
            ArrowButton.IsActived = _enable == true && _mode == DrawMode.Arrow;
            RectangleButton.IsActived = _enable == true && _mode == DrawMode.Rectangle;
            CircleButton.IsActived = _enable == true && _mode == DrawMode.Circle;
            RayButton.IsActived = _enable == true && _mode == DrawMode.Ray;
        }
        private void SetColor(ColorPicker b)
        {
            if (ReferenceEquals(_selectedColor, b)) return;
            var solidColorBrush = b.Background as SolidColorBrush;
            if (solidColorBrush == null) return;

            var ani = new ColorAnimation(solidColorBrush.Color, Duration3);

            MainInkCanvas.DefaultDrawingAttributes.Color = solidColorBrush.Color;
            brushPreview.Background.BeginAnimation(SolidColorBrush.ColorProperty, ani);
            b.IsActived = true;
            if (_selectedColor != null)
                _selectedColor.IsActived = false;
            _selectedColor = b;
        }
        private void SetBrushSize(double s)
        {
            MainInkCanvas.DefaultDrawingAttributes.Height = s;
            MainInkCanvas.DefaultDrawingAttributes.Width = s;
            brushPreview?.BeginAnimation(HeightProperty, new DoubleAnimation(s, Duration4));
            brushPreview?.BeginAnimation(WidthProperty, new DoubleAnimation(s, Duration4));
        }
        private void SetEraserMode(bool v)
        {
            if (v)
            {
                _lastEditingMode = MainInkCanvas.EditingMode;
                MainInkCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
                SetStaticInfo("擦除模式");
            }
            else
            {
                MainInkCanvas.EditingMode = _lastEditingMode;
                SetStaticInfo("");
            }

            EraserButton.IsActived = v;
            _eraserMode = v;
        }
        private void SetOrientation(bool v)
        {
            PaletteRotate.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(v ? -90:0, Duration4));
            Palette.BeginAnimation(MinWidthProperty, new DoubleAnimation(v? 90:0, Duration7));
            //PaletteGrip.BeginAnimation(WidthProperty, new DoubleAnimation((double)Application.Current.Resources[v ? "VerticalModeGrip" : "HorizontalModeGrip"], Duration3));
            //BasicButtonPanel.BeginAnimation(WidthProperty, new DoubleAnimation((double)Application.Current.Resources[v ? "VerticalModeFlowPanel" : "HorizontalModeFlowPanel"], Duration3));
            //PaletteFlowPanel.BeginAnimation(WidthProperty, new DoubleAnimation((double)Application.Current.Resources[v ? "VerticalModeFlowPanel" : "HorizontalModeFlowPanel"], Duration3));
            //ColorPickersPanel.BeginAnimation(WidthProperty, new DoubleAnimation((double)Application.Current.Resources[v ? "VerticalModeColorPickersPanel" : "HorizontalModeColorPickersPanel"], Duration3));
            _displayOrientation = v;
        }
        private void SetTopMost(bool v)
        {
            PinButton.IsActived = v;
            Topmost = v;
        }
        #endregion


        #region /---------IO---------/
        private StrokeCollection _preLoadStrokes = null;
        private void QuickSave(string filename = "QuickSave_")
        {
            Save(new FileStream("Save\\" + filename + GenerateFileName(), FileMode.OpenOrCreate));
        }
        private void Save(Stream fs)
        {
            try
            {
                if (fs == Stream.Null) return;
                MainInkCanvas.Strokes.Save(fs);
                _saved = true;
                Display("Ink saved");
                fs.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                Display("Fail to save");
            }
        }
        private StrokeCollection Load(Stream fs)
        {
            try
            {
                return new StrokeCollection(fs);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                Display("Fail to load");
            }
            return new StrokeCollection();
        }
        private void AnimatedReload(StrokeCollection sc)
        {
            _preLoadStrokes = sc;
            var ani = new DoubleAnimation(0, Duration3);
            ani.Completed += LoadAniCompleted;
            MainInkCanvas.BeginAnimation(OpacityProperty, ani);
        }
        private void LoadAniCompleted(object sender, EventArgs e)
        {
            if (_preLoadStrokes == null) return;
            MainInkCanvas.Strokes = _preLoadStrokes;
            Display("Ink loaded");
            _saved = true;
            ClearHistory();
            MainInkCanvas.BeginAnimation(OpacityProperty, new DoubleAnimation(1, Duration3));
        }

        private static string[] GetSavePathList()
        {
            return Directory.GetFiles("Save", "*.fdw");
        }
        private static string GetFileNameFromPath(string path)
        {
            return Path.GetFileName(path);
        }
        #endregion


        #region /---------Generator---------/
        private static string GenerateFileName(string fileExt = ".fdw")
        {
            return DateTime.Now.ToString("yyyyMMdd-HHmmss") + fileExt;
        }

        private List<Point> GenerateEclipseGeometry(Point st, Point ed)
        {
            double a = 0.5 * (ed.X - st.X);
            double b = 0.5 * (ed.Y - st.Y);
            List<Point> pointList = new List<Point>();
            for (double r = 0; r <= 2 * Math.PI; r = r + 0.01)
            {
                pointList.Add(new Point(0.5 * (st.X + ed.X) + a * Math.Cos(r), 0.5 * (st.Y + ed.Y) + b * Math.Sin(r)));
            }
            return pointList;
        }
        #endregion

        #region Shape Drawer
        private Point _drawerIntPos;
        private bool _drawerIsMove = false;
        private Stroke _drawerLastStroke;
        private void MainInkCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if(_enable == false || _mode == DrawMode.Pen || _mode == DrawMode.None || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            _ignoreStrokesChange = true;
            _drawerIsMove = true;
            _drawerIntPos = e.GetPosition(MainInkCanvas);
            _drawerLastStroke = null;
        }

        private void MainInkCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_drawerIsMove == true)
            {
                _drawerIsMove = false;
                _ignoreStrokesChange = false;

                if (_drawerLastStroke != null)
                {
                    StrokeCollection collection = new StrokeCollection();
                    collection.Add(_drawerLastStroke);
                    Push(_history, new StrokesHistoryNode(collection, StrokesHistoryNodeType.Added));
                }
            }
        }

        private void MainInkCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_drawerIsMove == false)
                return;

            DrawingAttributes drawingAttributes = MainInkCanvas.DefaultDrawingAttributes.Clone();
            Stroke stroke = null;

            drawingAttributes.StylusTip = StylusTip.Rectangle;
            drawingAttributes.IgnorePressure = true;

            Point endP = e.GetPosition(MainInkCanvas);

            if (_mode == DrawMode.Text)
            {

            }
            else if(_mode == DrawMode.Line)
            {
                List<Point> pointList = new List<Point>
                {
                    new Point(_drawerIntPos.X, _drawerIntPos.Y),
                    new Point(endP.X, endP.Y),
                };

                StylusPointCollection point = new StylusPointCollection(pointList);
                stroke = new Stroke(point)
                {
                    DrawingAttributes = drawingAttributes,
                };
            }
            else if(_mode == DrawMode.Arrow)
            {
                //FUCK THE MATH!!!!!!!!!!!!!!!FUCK !FUCK~!
                double w=15, h = 15;
                double theta = Math.Atan2(_drawerIntPos.Y - endP.Y, _drawerIntPos.X - endP.X);
                double sint = Math.Sin(theta);
                double cost = Math.Cos(theta);

                List<Point> pointList = new List<Point>
                {
                    new Point(_drawerIntPos.X, _drawerIntPos.Y),
                    new Point(endP.X , endP.Y),
                    new Point(endP.X + (w*cost - h*sint), endP.Y + (w*sint + h*cost)),
                    new Point(endP.X,endP.Y),
                    new Point(endP.X + (w*cost + h*sint), endP.Y - (h*cost - w*sint)),
                };

                StylusPointCollection point = new StylusPointCollection(pointList);

                drawingAttributes.FitToCurve = false;//must be false,other wise rectangle can not be drawed correct

                stroke = new Stroke(point)
                {
                    DrawingAttributes = drawingAttributes,
                };
            }
            else if (_mode == DrawMode.Rectangle)
            {
                List<Point> pointList = new List<Point>
                {
                    new Point(_drawerIntPos.X, _drawerIntPos.Y),
                    new Point(_drawerIntPos.X, endP.Y),
                    new Point(endP.X, endP.Y),
                    new Point(endP.X, _drawerIntPos.Y),
                    new Point(_drawerIntPos.X, _drawerIntPos.Y),
                };

                drawingAttributes.FitToCurve = false;//must be false,other wise rectangle can not be drawed correct

                StylusPointCollection point = new StylusPointCollection(pointList);
                stroke = new Stroke(point)
                {
                    DrawingAttributes = drawingAttributes,
                };
            }
            else if(_mode == DrawMode.Circle)
            {
                List<Point> pointList = GenerateEclipseGeometry(_drawerIntPos, endP);
                StylusPointCollection point = new StylusPointCollection(pointList);
                stroke = new Stroke(point)
                {
                    DrawingAttributes = drawingAttributes
                };
            }

            if (_drawerLastStroke != null)
                MainInkCanvas.Strokes.Remove(_drawerLastStroke);

            if (stroke != null)
                MainInkCanvas.Strokes.Add(stroke);

            _drawerLastStroke = stroke;
        }

        #endregion


        #region /---------Helper---------/
        private string _staticInfo = "";
        private bool _displayingInfo;

        private async void Display(string info)
        {
            InfoBox.Text = info;
            _displayingInfo = true;
            await InfoDisplayTimeUp(new Progress<string>(box => InfoBox.Text = box));
        }
        private Task InfoDisplayTimeUp(IProgress<string> box)
        {
            return Task.Run(() =>
            {
                Task.Delay(2000).Wait();
                box.Report(_staticInfo);
                _displayingInfo = false;
            });
        }
        private void SetStaticInfo(string info)
        {
            _staticInfo = info;
            if (!_displayingInfo)
                InfoBox.Text = _staticInfo;
        }

        private static Stream SaveDialog(string initFileName, string fileExt = ".fdw", string filter = "Free Draw Save (*.fdw)|*fdw")
        {
            var dialog = new Microsoft.Win32.SaveFileDialog()
            {
                DefaultExt = fileExt,
                Filter = filter,
                FileName = initFileName,
                InitialDirectory = Directory.GetCurrentDirectory() + "Save"
            };
            return dialog.ShowDialog() == true ? dialog.OpenFile() : Stream.Null;
        }
        private static Stream OpenDialog(string fileExt = ".fdw", string filter = "Free Draw Save (*.fdw)|*fdw")
        {
            var dialog = new Microsoft.Win32.OpenFileDialog()
            {
                DefaultExt = fileExt,
                Filter = filter,
            };
            return dialog.ShowDialog() == true ? dialog.OpenFile() : Stream.Null;
        }
        #endregion


        #region /---------Ink---------/
        private readonly Stack<StrokesHistoryNode> _history;
        private readonly Stack<StrokesHistoryNode> _redoHistory;
        private bool _ignoreStrokesChange;

        private void Undo()
        {
            if (!CanUndo()) return;
            var last = Pop(_history);
            _ignoreStrokesChange = true;
            if (last.Type == StrokesHistoryNodeType.Added)
                MainInkCanvas.Strokes.Remove(last.Strokes);
            else
                MainInkCanvas.Strokes.Add(last.Strokes);
            _ignoreStrokesChange = false;
            Push(_redoHistory, last);
        }
        private void Redo()
        {
            if (!CanRedo()) return;
            var last = Pop(_redoHistory);
            _ignoreStrokesChange = true;
            if (last.Type == StrokesHistoryNodeType.Removed)
                MainInkCanvas.Strokes.Remove(last.Strokes);
            else
                MainInkCanvas.Strokes.Add(last.Strokes);
            _ignoreStrokesChange = false;
            Push(_history, last);
        }

        private static void Push(Stack<StrokesHistoryNode> collection, StrokesHistoryNode node)
        {
            collection.Push(node);
        }
        private static StrokesHistoryNode Pop(Stack<StrokesHistoryNode> collection)
        {
            return collection.Count == 0 ? null : collection.Pop();
        }
        private bool CanUndo()
        {
            return _history.Count != 0;
        }
        private bool CanRedo()
        {
            return _redoHistory.Count != 0;
        }
        private void StrokesChanged(object sender, StrokeCollectionChangedEventArgs e)
        {
            if (_ignoreStrokesChange) return;
            _saved = false;
            if (e.Added.Count != 0)
                Push(_history, new StrokesHistoryNode(e.Added, StrokesHistoryNodeType.Added));
            if (e.Removed.Count != 0)
                Push(_history, new StrokesHistoryNode(e.Removed, StrokesHistoryNodeType.Removed));
            ClearHistory(_redoHistory);
        }

        private void ClearHistory()
        {
            ClearHistory(_history);
            ClearHistory(_redoHistory);
        }
        private static void ClearHistory(Stack<StrokesHistoryNode> collection)
        {
            collection?.Clear();
        }
        private void Clear()
        {
            MainInkCanvas.Strokes.Clear();
            ClearHistory();
        }

        private void AnimatedClear()
        {
            //no need any more
            //if (!PromptToSave()) return;
            var ani = new DoubleAnimation(0, Duration3);
            ani.Completed += ClearAniComplete; ;
            MainInkCanvas.BeginAnimation(OpacityProperty, ani);
        }
        private void ClearAniComplete(object sender, EventArgs e)
        {
            Clear();
            Display("Cleared");
            MainInkCanvas.BeginAnimation(OpacityProperty, new DoubleAnimation(1, Duration3));
        }
        #endregion

        #region /---------Color Picker------/
        private void ColorPickers_Click(object sender, RoutedEventArgs e)
        {
            var border = sender as ColorPicker;
            if (border == null) return;
            SetColor(border);
        }
        #endregion

        #region /---------Extra Button---------/
        private void PinButton_Click(object sender, RoutedEventArgs e)
        {
            SetTopMost(!Topmost);
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainInkCanvas.Strokes.Count == 0)
            {
                Display("Nothing to save");
                return;
            }
            QuickSave();
        }
        private void SaveButton_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (MainInkCanvas.Strokes.Count == 0)
            {
                Display("Nothing to save");
                return;
            }
            Save(SaveDialog(GenerateFileName()));
        }
        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            if (!PromptToSave()) return;
            var s = OpenDialog();
            if (s == Stream.Null) return;
            AnimatedReload(Load(s));
        }
        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainInkCanvas.Strokes.Count == 0)
            {
                Display("Nothing to save");
                return;
            }
            try
            {
                var s = SaveDialog("ImageExport_" + GenerateFileName(".png"), ".png",
                    "Portable Network Graphics (*png)|*png");
                if (s == Stream.Null) return;
                var rtb = new RenderTargetBitmap((int)MainInkCanvas.ActualWidth, (int)MainInkCanvas.ActualHeight, 96d,
                    96d, PixelFormats.Pbgra32);
                rtb.Render(MainInkCanvas);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));
                encoder.Save(s);
                s.Close();
                Display("Image Exported");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                Display("Export failed");
            }
        }
        private delegate void NoArgDelegate();
        private void ExportButton_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (MainInkCanvas.Strokes.Count == 0)
            {
                Display("Nothing to save");
                return;
            }
            try
            {
                var s = SaveDialog("ImageExportWithBackground_" + GenerateFileName(".png"), ".png", "Portable Network Graphics (*png)|*png");
                if (s == Stream.Null) return;
                Palette.Opacity = 0;
                Palette.Dispatcher.Invoke(DispatcherPriority.Render, (NoArgDelegate)delegate { });
                Thread.Sleep(100);
                var fromHwnd = Graphics.FromHwnd(IntPtr.Zero);
                var w = (int)(SystemParameters.PrimaryScreenWidth * fromHwnd.DpiX / 96.0);
                var h = (int)(SystemParameters.PrimaryScreenHeight * fromHwnd.DpiY / 96.0);
                var image = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                Graphics.FromImage(image).CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(w, h), CopyPixelOperation.SourceCopy);
                image.Save(s, ImageFormat.Png);
                Palette.Opacity = 1;
                s.Close();
                Display("Image Exported");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                Display("Export failed");
            }
        }
        #endregion

        #region /---------Tool Button------/
        private void BrushSwitchButton_Click(object sender, RoutedEventArgs e)
        {
            _brushIndex++;
            if (_brushIndex > _brushSizes.Count() - 1) _brushIndex = 0;
            SetBrushSize(_brushSizes[_brushIndex]);
        }
        private void EnableButton_Click(object sender, RoutedEventArgs e)
        {
            SetEnable(false,_mode);
        }
        private void PenButton_Click(object sender, RoutedEventArgs e)
        {
            SetEnable(true, DrawMode.Pen);
            SetEraserMode(false);
        }

        private void TextButton_Click(object sender, RoutedEventArgs e)
        {
            SetEnable(true, DrawMode.Text);
            SetEraserMode(false);
        }

        private void LineButton_Click(object sender, RoutedEventArgs e)
        {
            SetEnable(true, DrawMode.Line);
            SetEraserMode(false);
        }

        private void ArrowButton_Click(object sender, RoutedEventArgs e)
        {
            SetEnable(true, DrawMode.Arrow);
            SetEraserMode(false);
        }

        private void RectangleButton_Click(object sender, RoutedEventArgs e)
        {
            SetEnable(true, DrawMode.Rectangle);
            SetEraserMode(false);
        }

        private void CircleButton_Click(object sender, RoutedEventArgs e)
        {
            SetEnable(true, DrawMode.Circle);
            SetEraserMode(false);
        }

        private void RayButton_Click(object sender, RoutedEventArgs e)
        {
            SetEnable(true, DrawMode.Ray);
            SetEraserMode(false);
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            Undo();
        }
        private void RedoButton_Click(object sender, RoutedEventArgs e)
        {
            Redo();
        }
        private void EraserButton_Click(object sender, RoutedEventArgs e)
        {
            SetEraserMode(!_eraserMode);
        }
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            AnimatedClear();
        }

        #endregion

        #region /---------UI---------/
        private void DetailToggler_Click(object sender, RoutedEventArgs e)
        {
            SetExtralToolPanel(!_displayExtraToolPanel);
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Topmost = false;
            var anim = new DoubleAnimation(0, Duration3);
            anim.Completed += Exit;
            BeginAnimation(OpacityProperty, anim);
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //SetBrushSize(e.NewValue);
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void HideButton_Click(object sender, RoutedEventArgs e)
        {
            SetInkVisibility(!_inkVisibility);
        }
        
        private void OrientationButton_Click(object sender, RoutedEventArgs e)
        {
            SetOrientation(!_displayOrientation);
        }
        #endregion


        #region  /---------Docking---------/

        enum DockingDirection
        {
            None,
            Top,
            Left,
            Right
        }
        private int _dockingEdgeThreshold = 30;
        private int _dockingAwaitTime = 10000;
        private int _dockingSideIndent = 290;
        private void AnimatedCanvasMoving(UIElement ctr, Point to, Duration dur)
        {
            ctr.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(Canvas.GetTop(ctr), to.Y, dur));
            ctr.BeginAnimation(Canvas.LeftProperty, new DoubleAnimation(Canvas.GetLeft(ctr), to.X, dur));
        }

        private DockingDirection CheckDocking()
        {
            var left = Canvas.GetLeft(Palette);
            var right = Canvas.GetRight(Palette);
            var top = Canvas.GetTop(Palette);

            if (left > 0 && left < _dockingEdgeThreshold)
                return DockingDirection.Left;
            if (right > 0 && right < _dockingEdgeThreshold)
                return DockingDirection.Right;
            if (top > 0 && top < _dockingEdgeThreshold)
                return DockingDirection.Top;
            return DockingDirection.None;
        }

        private void RightDocking()
        {
            AnimatedCanvasMoving(Palette, new Point(ActualWidth + _dockingSideIndent, Canvas.GetTop(Palette)), Duration5);
        }
        private void LeftDocking()
        {
            AnimatedCanvasMoving(Palette, new Point(0 - _dockingSideIndent, Canvas.GetTop(Palette)), Duration5);
        }
        private void TopDocking()
        {

        }

        private async void AwaitDocking()
        {

            await Docking();
        }

        private Task Docking()
        {
            return Task.Run(() =>
            {
                Thread.Sleep(_dockingAwaitTime);
                var direction = CheckDocking();
                if (direction == DockingDirection.Left) LeftDocking();
                if (direction == DockingDirection.Right) RightDocking();
                if (direction == DockingDirection.Top) TopDocking();
            });
        }
        #endregion


        #region /---------Dragging---------/
        private Point _lastMousePosition;
        private bool _isDraging;
        private bool _tempEnable;

        private void StartDrag()
        {
            _lastMousePosition = Mouse.GetPosition(this);
            _isDraging = true;
            Palette.Background = new SolidColorBrush(Colors.Transparent);
            _tempEnable = _enable;
            SetEnable(true,_mode);
        }
        private void EndDrag()
        {
            if(_isDraging == true)
            {
                SetEnable(_tempEnable,_mode);
            }

            _isDraging = false;
            Palette.Background = null;
        }
        private void PaletteGrip_MouseDown(object sender, MouseButtonEventArgs e)
        {
            StartDrag();
        }
        private void Palette_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDraging) return;
            var currentMousePosition = Mouse.GetPosition(this);
            var offset = currentMousePosition - _lastMousePosition;

            Canvas.SetTop(Palette, Canvas.GetTop(Palette) + offset.Y);
            Canvas.SetLeft(Palette, Canvas.GetLeft(Palette) + offset.X);

            _lastMousePosition = currentMousePosition;
        }
        private void Palette_MouseUp(object sender, MouseButtonEventArgs e)
        { EndDrag(); }
        private void Palette_MouseLeave(object sender, MouseEventArgs e)
        { 
            EndDrag();
        }



        #endregion
    }
}
