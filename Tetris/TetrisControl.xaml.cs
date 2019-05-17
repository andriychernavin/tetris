using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Windows.Threading;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Data;
using System.Reflection;
//using System.Windows.Input;
using System.Diagnostics;

namespace Tetris
{
    /// <summary>
    /// Interaction logic for TetrisControl.xaml
    /// </summary>

    public class TetrisControlCommands
    {
        public static RoutedUICommand Start { private set; get; }
        public static RoutedUICommand Pause { private set; get; }
        public static RoutedUICommand Stop { private set; get; }

        public static RoutedUICommand Left { private set; get; }
        public static RoutedUICommand Right { private set; get; }
        public static RoutedUICommand Rotate { private set; get; }
        public static RoutedUICommand Drop { private set; get; }

        static TetrisControlCommands()
        {
            Start = new RoutedUICommand("Start", "Start", typeof(TetrisControl));

            Pause = new RoutedUICommand("Pause", "Pause", typeof(TetrisControl));

            Stop = new RoutedUICommand("Stop", "Stop", typeof(TetrisControl));

            {
                InputGestureCollection input = new InputGestureCollection();
                input.Add(new KeyGesture(Key.Left));
                Left = new RoutedUICommand("Left", "Left", typeof(TetrisControl), input);
            }

            {
                InputGestureCollection input = new InputGestureCollection();
                input.Add(new KeyGesture(Key.Right));
                Right = new RoutedUICommand("Right", "Right", typeof(TetrisControl), input);
            }

            {
                InputGestureCollection input = new InputGestureCollection();
                input.Add(new KeyGesture(Key.Up));
                Rotate = new RoutedUICommand("Rotate", "Rotate", typeof(TetrisControl), input);
            }

            {
                InputGestureCollection input = new InputGestureCollection();
                input.Add(new KeyGesture(Key.Down));
                //input.Add(new KeyGesture(Key.Space));
                Drop = new RoutedUICommand("Drop", "Drop", typeof(TetrisControl), input);
            }
        }
    }

    public partial class TetrisControl : UserControl, INotifyPropertyChanged
    {
        #region properties

        private TetrisBase Tetris;

        public TetrisFiguresEnum FigureSet // набор фигур
        {
            set { SetValue(FigureSetProperty, value); }
            get { return (TetrisFiguresEnum)GetValue(FigureSetProperty); }
        }
        public static readonly DependencyProperty FigureSetProperty;
        private TetrisFiguresEnum PrevFigureSet = 0; // предыдущий набор фигур
        private bool FigureSetChanged = false; // признак изменения набора фигур во время игры

        public TetrisFigureColorsEnum ColorSet // набор цветов
        {
            set { SetValue(ColorSetProperty, value); }
            get { return (TetrisFigureColorsEnum)GetValue(ColorSetProperty); }
        }
        public static readonly DependencyProperty ColorSetProperty;
        private TetrisFigureColorsEnum PrevColorSet = 0; // предыдущий набор цветов

        private static BrushConverter BrushConverter = new BrushConverter();

        private Thickness PanelTankMargin; // начальное значение margin

        private bool AutoPause = false; // автоматически включена пауза при потере фокуса

        private Random Random = new Random((int)DateTime.Now.Ticks); // генератор случайных чисел

        public EventHandler<TetrisGameOverEventArgs> GameOver; // делегат для сохранения результатов игры

        #endregion

        #region size properties

        const double MinBlockSize = 10; // минимальный размер точки
        const double MaxBlockSize = 1000; // максимальный размер точки

        double BlockSize; // рассчитанный размер точки согласно размерам контрола

        const double NextBlockSize = 20; // размер точки следующей фигуры

        const int MinTankWidth = 2;
        const int MaxTankWidth = 100;
        const int MinTankHeight = 2;
        const int MaxTankHeight = 100;

        const int BlockCornerRadius = 2;

        int MaxNextWidth; // максимальная ширина следующей фигуры
        int MaxNextHeight; // максимальная высота следующей фигуры

        public int TankWidth // ширина стакана
        {
            //set { TankWidthValue = value; NotifyPropertyChanged(); }
            //get { return TankWidthValue; }

            set { SetValue(TankWidthProperty, value); }
            get { return (int)GetValue(TankWidthProperty); }
        }
        public static readonly DependencyProperty TankWidthProperty;

        public int TankHeight // высота стакана
        {
            //set { TankHeightValue = value; NotifyPropertyChanged(); }
            //get { return TankHeightValue; }

            set { SetValue(TankHeightProperty, value); }
            get { return (int)GetValue(TankHeightProperty); }
        }
        public static readonly DependencyProperty TankHeightProperty;

        private static bool ValidateTankWidth(object value)
        {
            int Value = (int)value;

            return true;
            //return (MinTankWidth <= Value && Value <= MaxTankWidth);
        }

        private static object CorrectTankWidth(DependencyObject d, object value)
        {
            int Value = (int)value;

            if (Value < MinTankWidth) return MinTankWidth;
            if (Value > MaxTankWidth) return MaxTankWidth;

            return Value;
        }

        private static bool ValidateTankHeight(object value)
        {
            int Value = (int)value;

            return true;
            //return (MinTankHeight<= Value && Value <= MaxTankHeight);
        }

        private static object CorrectTankHeight(DependencyObject d, object value)
        {
            int Value = (int)value;

            if (Value < MinTankHeight) return MinTankHeight;
            if (Value > MaxTankHeight) return MaxTankHeight;

            return Value;
        }

        #endregion

        #region constructor and init

        static TetrisControl()
        {
            TankWidthProperty = DependencyProperty.Register("TankWidth", typeof(int), typeof(TetrisControl), new PropertyMetadata(MinTankWidth, null, CorrectTankWidth), new ValidateValueCallback(ValidateTankWidth));
            TankHeightProperty = DependencyProperty.Register("TankHeight", typeof(int), typeof(TetrisControl), new PropertyMetadata(MinTankHeight, null, CorrectTankHeight), new ValidateValueCallback(ValidateTankHeight));
            FigureSetProperty = DependencyProperty.Register("FigureSet", typeof(TetrisFiguresEnum), typeof(TetrisControl));
            ColorSetProperty = DependencyProperty.Register("ColorSet", typeof(TetrisFigureColorsEnum), typeof(TetrisControl));
        }

        public TetrisControl()
        {
            InitializeComponent();

            InitializeCommands();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // запоминаем начальное значение, чтобы использовать его при пересчете размеров

            PanelTankMargin = new Thickness(PanelTank.Margin.Left, PanelTank.Margin.Top, PanelTank.Margin.Right, PanelTank.Margin.Bottom);

            // список настроек

            {
                DataTable Table = new DataTable();
                Table.Columns.Add("Id");
                Table.Columns.Add("Name");

                foreach (Enum Value in Enum.GetValues(typeof(TetrisFiguresEnum)))
                {
                    DataRow Row = Table.NewRow();
                    Row["Id"] = Value;
                    Row["Name"] = GetDescription(Value);
                    Table.Rows.Add(Row);
                }

                TextFigureSet.DataContext = Table.DefaultView;
            }

            {
                DataTable Table = new DataTable();
                Table.Columns.Add("Id");
                Table.Columns.Add("Name");

                foreach (Enum Value in Enum.GetValues(typeof(TetrisFigureColorsEnum)))
                {
                    DataRow Row = Table.NewRow();
                    Row["Id"] = Value;
                    Row["Name"] = GetDescription(Value);
                    Table.Rows.Add(Row);
                }

                TextColorSet.DataContext = Table.DefaultView;
            }

            // инициализация

            InitializeControls();
        }

        // устанавливаем набор фигур
        private void SetFiguresAndColors()
        {
            if (Tetris == null) return;

            if (Application.Current.Dispatcher.CheckAccess())
            {
                InvokeSetFiguresAndColors();
            }
            else
            {
                Dispatcher.Invoke(() => InvokeSetFiguresAndColors());
            }
        }

        private void InvokeSetFiguresAndColors()
        {
            bool FiguresChanged = false;
            bool ColorsChanged = false;

            // фигуры

            if (FigureSet == TetrisFiguresEnum.Random || FigureSet != PrevFigureSet)
            {
                FiguresChanged = true;

                FigureSetChanged |= FigureSet == TetrisFiguresEnum.Random || (FigureSet != PrevFigureSet && PrevFigureSet != 0);

                // определяем набор фигур

                TetrisFiguresEnum ChoosenFigureSet;

                TetrisFigures Figures;

                if (FigureSet == TetrisFiguresEnum.Random)
                {
                    var List = Enum.GetValues(typeof(TetrisFiguresEnum)).Cast<TetrisFiguresEnum>().Where(e => e != PrevFigureSet && e != TetrisFiguresEnum.Random);

                    if (List.Count() == 0) throw new Exception("No figure sets to choose");

                    ChoosenFigureSet = List.ElementAt(Random.Next(List.Count()));
                }
                else
                {
                    ChoosenFigureSet = FigureSet;
                }

                PrevFigureSet = ChoosenFigureSet;

                // получаем набор фигур

                switch (ChoosenFigureSet)
                {
                    case (TetrisFiguresEnum.Standard):
                        Figures = new TetrisFiguresStandard();
                        break;
                    case (TetrisFiguresEnum.Figures2x2):
                        Figures = new TetrisFiguresRandom(2, true, 2, 4);
                        //Figures = new TetrisFiguresRandom(4, true, 16, 16);
                        break;
                    case (TetrisFiguresEnum.Figures3x3):
                        Figures = new TetrisFiguresRandom(3, true, 2, 5);
                        break;
                    case (TetrisFiguresEnum.Figures4x4):
                        Figures = new TetrisFiguresRandom(4, true, 2, 5);
                        break;
                    default:
                        throw new Exception("Unknown figure set");
                }

                // устанавливаем набор фигур

                Tetris.Figures = Figures;

                if (Application.Current.Dispatcher.CheckAccess())
                {
                    InitializeNextFigure();
                }
                else
                {
                    Application.Current.Dispatcher.BeginInvoke((Action)(() => InitializeNextFigure()), DispatcherPriority.Normal);
                }
            }

            // цвета

            if (ColorSet == TetrisFigureColorsEnum.Random || ColorSet != PrevColorSet)
            {
                ColorsChanged = true;

                // определяем набор цветов

                TetrisFigureColorsEnum ChoosenColorSet;

                TetrisFigureColors FigureColors;

                if (ColorSet == TetrisFigureColorsEnum.Random)
                {
                    var List = Enum.GetValues(typeof(TetrisFigureColorsEnum)).Cast<TetrisFigureColorsEnum>().Where(e => e != PrevColorSet && e != TetrisFigureColorsEnum.Random);

                    if (List.Count() == 0) throw new Exception("No color sets to choose");

                    int i = Random.Next(List.Count());

                    ChoosenColorSet = List.ElementAt(i);
                }
                else
                {
                    ChoosenColorSet = ColorSet;
                }

                PrevColorSet = ChoosenColorSet;

                // получаем набор цветов

                switch (ChoosenColorSet)
                {
                    case (TetrisFigureColorsEnum.Standard):
                        FigureColors = new TetrisColorsStandard();
                        break;
                    case (TetrisFigureColorsEnum.Dark):
                        FigureColors = new TetrisColorsDark();
                        break;
                    case (TetrisFigureColorsEnum.Gray):
                        FigureColors = new TetrisColorsGray();
                        break;
                    case (TetrisFigureColorsEnum.White):
                        FigureColors = new TetrisColorsWhite();
                        break;
                    case (TetrisFigureColorsEnum.Black):
                        FigureColors = new TetrisColorsBlack();
                        break;
                    default:
                        throw new Exception("Unknown color set");
                }

                // устанавливаем набор цветов

                Tetris.FigureColors = FigureColors;
            }

            // следующая фигура

            if (Tetris.NextFigure != null)
            {
                if (FiguresChanged)
                {
                    Tetris.CreateNextFigure();
                }
                else if (ColorsChanged)
                {
                    Tetris.ColorNextFigure();
                }
            }
        }

        private void InitializeNextFigure()
        {
            GridNext.Children.Clear();

            MaxNextWidth = Tetris.Figures.MaxWidth;
            MaxNextHeight = Tetris.Figures.MaxHeight;

            GridNext.Columns = MaxNextWidth;
            GridNext.Rows = MaxNextHeight;

            for (int X = 0; X < GridNext.Columns; X++)
            {
                for (int Y = 0; Y < GridNext.Rows; Y++)
                {
                    Rectangle Block = new Rectangle();
                    Block.RadiusX = BlockCornerRadius;
                    Block.RadiusY = BlockCornerRadius;

                    Block.Visibility = Visibility.Hidden;

                    GridNext.Children.Add(Block);
                }
            }

            this.InvalidateMeasure();
        }

        public void Initialize()
        {
            FinalizeControls();
            InitializeControls();
        }

        private void InitializeControls()
        {
            // движок

            Tetris = new TetrisBase(TankWidth, TankHeight);

            Tetris.GameOver = OnGameOver;

            SetFiguresAndColors();

            Tetris.PropertyChanged += Tetris_PropertyChanged;

            TankWidth = Tetris.Width;
            TankHeight = Tetris.Height;

            this.DataContext = Tetris;

            AutoPause = false;

            // стакан (настраиваем элементы формы без указания размеров)

            GridTank.Children.Clear();

            GridTank.Columns = TankWidth;
            GridTank.Rows = TankHeight;

            for (int X = 0; X < GridTank.Columns; X++)
            {
                for (int Y = 0; Y < GridTank.Rows; Y++)
                {
                    Rectangle Block = new Rectangle();
                    Block.RadiusX = BlockCornerRadius;
                    Block.RadiusY = BlockCornerRadius;

                    Block.Visibility = Visibility.Hidden;

                    GridTank.Children.Add(Block);
                }
            }

            // следующая фигура

            InitializeNextFigure();

            // прочее

            AutoPause = false;

            this.DataContext = Tetris;

            this.InvalidateMeasure();
        }

        private void FinalizeControls()
        {
            Tetris?.Dispose();

            Tetris = null;

            PrevFigureSet = 0;
            PrevColorSet = 0;

            this.InvalidateMeasure();
        }

        private void OnGameOver(object sender, TetrisGameOverEventArgs e)
        {
            if (GameOver != null)
            {
                e.FigureSet = FigureSetChanged ? TetrisFiguresEnum.Random : FigureSet;

                GameOver(this, e);
            }
        }

        #endregion

        #region resize

        protected override Size MeasureOverride(Size availableSize)
        {
            // определяем размеры элементов формы
            // padding не учитывается, по необходимости добавить в формулы

            BlockSize = 0;

            if (Tetris == null) return new Size();

            double BlockWidth;
            double BlockHeight;

            Size MaxSize = new Size(Double.PositiveInfinity, Double.PositiveInfinity);

            PanelTank.Margin = new Thickness(PanelTankMargin.Left, PanelTankMargin.Top, PanelTankMargin.Right, PanelTankMargin.Bottom);

            // размеры боковых панелей

            GridNext.Width = MaxNextWidth * NextBlockSize;
            GridNext.Height = MaxNextHeight * NextBlockSize;

            GridControls.Measure(MaxSize);
            GridSettings.Measure(MaxSize);

            double SidePanelWidth = Math.Max(GridControls.DesiredSize.Width, GridSettings.DesiredSize.Width);
            double SidePanelHeight = Math.Max(GridControls.DesiredSize.Height, GridSettings.DesiredSize.Height);

            if (MainControl.UseLayoutRounding)
            {
                SidePanelWidth = Math.Ceiling(SidePanelWidth);
                SidePanelHeight = Math.Ceiling(SidePanelHeight);
            }

            PanelControls.Width = SidePanelWidth;
            PanelControls.Height = SidePanelHeight;

            PanelSettings.Width = SidePanelWidth;
            PanelSettings.Height = SidePanelHeight;

            // размеры элемента в стакане

            if (availableSize.Width < Double.PositiveInfinity)
                BlockWidth = (availableSize.Width - SidePanelWidth * 2 - PanelTank.Margin.Left - PanelTank.Margin.Right) / TankWidth;
            else
                BlockWidth = Double.PositiveInfinity;

            if (availableSize.Height < Double.PositiveInfinity)
                BlockHeight = (availableSize.Height - PanelTank.Margin.Top - PanelTank.Margin.Bottom) / TankHeight;
            else
                BlockHeight = Double.PositiveInfinity;

            BlockSize = Math.Min(BlockWidth, BlockHeight); // берем минимальный размер, чтобы элементы были квадратными

            if (MainControl.UseLayoutRounding) BlockSize = Math.Floor(BlockSize); // отбрасываем дробную часть, чтобы попадать в точки на форме

            if (BlockSize < MinBlockSize) BlockSize = MinBlockSize;
            if (BlockSize > MaxBlockSize) BlockSize = MaxBlockSize;

            // размеры стакана

            PanelTank.Width = TankWidth * BlockSize + PanelTank.Margin.Left + PanelTank.Margin.Right;
            PanelTank.Height = TankHeight * BlockSize + PanelTank.Margin.Top + PanelTank.Margin.Bottom;

            // добавляем margin сверху и снизу, если остается пустое место

            if (PanelTank.Height < availableSize.Height && availableSize.Height < double.PositiveInfinity)
            {
                double y1 = Math.Floor((availableSize.Height - PanelTank.Height) / 2);
                double y2 = availableSize.Height - PanelTank.Height - y1;

                PanelTank.Margin = new Thickness(PanelTank.Margin.Left, PanelTank.Margin.Top + y1, PanelTank.Margin.Right, PanelTank.Margin.Bottom + y2);

                PanelTank.Height += y1 + y2;
            }

            // размеры всего контрола

            double ControlWidth = PanelTank.Width + SidePanelWidth * 2;
            double ControlHeight = Math.Max(PanelTank.Height, SidePanelHeight);

            return new Size(ControlWidth, ControlHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            // устанавливаем размеры элементов формы

            base.ArrangeOverride(finalSize);

            if (Tetris == null) return finalSize;

            if (finalSize.Width > 0 && finalSize.Height > 0)
            {
                MainPanel.Width = finalSize.Width;
                MainPanel.Height = finalSize.Height;

                double PanelHeight = TankHeight * BlockSize;

                PanelControls.Height = finalSize.Height;
                PanelSettings.Height = finalSize.Height;

                PanelTank.Width = TankWidth * BlockSize;
                PanelTank.Height = PanelHeight;
            }

            return finalSize;
        }

        #endregion

        #region property changed

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region visualization

        private void Tetris_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Tetris == null) return;

            // перерисовываем все элементы
            //Dispatcher.BeginInvoke((Action)InvokeDraw, DispatcherPriority.Normal);

            // обновляем состояние кнопок
            //Dispatcher.BeginInvoke((Action)(() => { CommandManager.InvalidateRequerySuggested(); }), DispatcherPriority.Normal);

            // обновляем набор фигур и цветов при изменении уровня
            if (Tetris.State == TetrisState.Started && e.PropertyName == "Level")
            {
                SetFiguresAndColors();
            }

            if (Application.Current.Dispatcher.CheckAccess())
            {
                InvokeDraw(e.PropertyName);
            }
            else
            {
                Application.Current.Dispatcher.BeginInvoke((Action)(() => InvokeDraw(e.PropertyName)), DispatcherPriority.Normal);
            }
        }

        private void InvokeDraw_ColorText()
        {
            //string Text = "";
            Paragraph TextBlock = new Paragraph();

            void AddText(string text, Color? color = null)
            {
                Run run = new Run(text);

                if (color != null) run.Foreground = GetBrush(color ?? Colors.Black);

                //Text += text;
                TextBlock.Inlines.Add(run);
            }

            TetrisBlock[,] TetrisView = Tetris.GetView();

            int Width = TetrisView.GetLength(0);
            int Height = TetrisView.GetLength(1);

            TetrisBlock[,] FigureView = Tetris.NextFigure?.GetView();

            for (int Y = 0; Y < Height; Y++)
            {
                if (TextBlock.Inlines.Count() > 0) AddText("\n");

                for (int X = 0; X < Width; X++)
                {
                    if (TetrisView[X, Y] != null)
                        AddText("XX", TetrisView[X, Y].Color);
                    else
                        AddText("---", Colors.LightGray);
                }

                AddText(Tetris.Lines >= Height - Y ? " + " : " - ");

                switch (Y)
                {
                    case 1:
                        AddText("  State: " + Tetris.State.ToString());
                        break;
                    case 3:
                        AddText("  Level: " + Tetris.Level.ToString());
                        break;
                    case 5:
                        AddText("  Score: " + Tetris.Score.ToString());
                        break;
                    case 7:
                        AddText("  Next figure: ");
                        break;
                    default:
                        const int FirstLine = 9;

                        if (FigureView != null && Y >= FirstLine && Y < FirstLine + FigureView.GetLength(1))
                        {
                            AddText("  ");

                            int FigureY = Y - FirstLine;

                            for (int FigureX = 0; FigureX < FigureView.GetLength(0); FigureX++)
                            {
                                if (FigureView[FigureX, FigureY] != null)
                                    AddText("XX", FigureView[FigureX, FigureY].Color);
                                else
                                    AddText("---", Colors.White);
                            }
                        }
                        break;
                }
            }

            FlowDocument Document = new FlowDocument();

            Document.Blocks.Add(TextBlock);

            //Tank.Text = Text;
            //ColorTank.Document = Document;
        }

        private void InvokeDraw_EntireControl()
        {
            // стакан

            TetrisBlock[,] TetrisView = Tetris.GetView();

            for (int i = 0; i < GridTank.Children.Count; i++)
            {
                int X = i % GridTank.Columns;
                int Y = i / GridTank.Columns;

                UIElement Block = GridTank.Children[i];

                if (TetrisView[X, Y] != null)
                {
                    Block.Visibility = Visibility.Visible;
                    (Block as Rectangle).Fill = GetBrush(TetrisView[X, Y].Color);
                }
                else
                {
                    Block.Visibility = Visibility.Hidden;
                }
            }

            // следующая фигура

            TetrisBlock[,] FigureView = Tetris.NextFigure?.GetView();

            if (FigureView != null)
            {
                int NextWidth = FigureView.GetLength(0);
                int NextHeight = FigureView.GetLength(1);

                int OffsetX = (MaxNextWidth - NextWidth) / 2;
                int OffsetY = 0;

                for (int i = 0; i < GridNext.Children.Count; i++)
                {
                    int X = i % GridNext.Columns - OffsetX;
                    int Y = i / GridNext.Columns - OffsetY;

                    if (0 <= X && X < NextWidth && 0 <= Y && Y < NextHeight && FigureView[X, Y] != null)
                    {
                        UIElement Block = GridNext.Children[i];

                        Block.Visibility = Visibility.Visible;

                        (Block as Rectangle).Fill = GetBrush(FigureView[X, Y].Color);
                    }
                    else
                    {
                        GridNext.Children[i].Visibility = Visibility.Hidden;
                    }
                }
            }
            else
            {
                for (int i = 0; i < GridNext.Children.Count; i++)
                {
                    GridNext.Children[i].Visibility = Visibility.Hidden;
                }
            }

            // надписи

            ValueLevel.Content = Tetris.Level.ToString();
            ValueLines.Content = Tetris.Lines.ToString();
            ValueScore.Content = Tetris.Score.ToString();
        }

        private void InvokeDraw(string propertyName)
        {
            int BlockX = 0;
            int BlockY = 0;

            if (propertyName.Length > 5 && propertyName.Substring(0, 5) == "TankX")
            {
                // выделяем координаты точки в стакане
                // ошибки при распознавании не проверяем

                int i = propertyName.IndexOf('Y', 6);

                BlockX = int.Parse(propertyName.Substring(5, i - 5));
                BlockY = int.Parse(propertyName.Substring(i + 1));

                // раньше попадали задачи на перерисовку в предыдущем стакане
                // после добавления Tetris.Dispose() проблема не проявляется
                //if (BlockX >= Tetris.Width || BlockY >= Tetris.Height) return;

                propertyName = "Block";
            }

            switch (propertyName)
            {
                case "Block":
                    // точка в стакане

                    {
                        TetrisBlock TetrisView = Tetris.GetView(BlockX, BlockY);

                        UIElement Block = GridTank.Children[BlockX + BlockY * Tetris.Width];

                        if (TetrisView != null)
                        {
                            Block.Visibility = Visibility.Visible;
                            (Block as Rectangle).Fill = GetBrush(TetrisView.Color);
                        }
                        else
                        {
                            Block.Visibility = Visibility.Hidden;
                        }
                    }

                    break;

                case "Tank":
                    // стакан

                    {
                        TetrisBlock[,] TetrisView = Tetris.GetView();

                        for (int i = 0; i < GridTank.Children.Count; i++)
                        {
                            int X = i % GridTank.Columns;
                            int Y = i / GridTank.Columns;

                            UIElement Block = GridTank.Children[i];

                            if (TetrisView[X, Y] != null)
                            {
                                Block.Visibility = Visibility.Visible;
                                (Block as Rectangle).Fill = GetBrush(TetrisView[X, Y].Color);
                            }
                            else
                            {
                                Block.Visibility = Visibility.Hidden;
                            }
                        }
                    }

                    break;

                case "NextFigure":
                    // следующая фигура

                    TetrisBlock[,] FigureView = Tetris.NextFigure?.GetView();

                    if (FigureView != null)
                    {
                        int NextWidth = FigureView.GetLength(0);
                        int NextHeight = FigureView.GetLength(1);

                        int OffsetX = (MaxNextWidth - NextWidth) / 2;
                        int OffsetY = 0;

                        for (int i = 0; i < GridNext.Children.Count; i++)
                        {
                            int X = i % GridNext.Columns - OffsetX;
                            int Y = i / GridNext.Columns - OffsetY;

                            if (0 <= X && X < NextWidth && 0 <= Y && Y < NextHeight && FigureView[X, Y] != null)
                            {
                                UIElement Block = GridNext.Children[i];

                                Block.Visibility = Visibility.Visible;

                                (Block as Rectangle).Fill = GetBrush(FigureView[X, Y].Color);
                            }
                            else
                            {
                                GridNext.Children[i].Visibility = Visibility.Hidden;
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < GridNext.Children.Count; i++)
                        {
                            GridNext.Children[i].Visibility = Visibility.Hidden;
                        }
                    }

                    break;

                case "State":
                    // статус

                    CommandManager.InvalidateRequerySuggested();

                    break;
            }
        }

        #endregion

        #region commands

        private void InitializeCommands()
        {
            this.CommandBindings.Add(new CommandBinding(TetrisControlCommands.Start, Start_Executed, Start_CanExecute));
            this.CommandBindings.Add(new CommandBinding(TetrisControlCommands.Pause, Pause_Executed, Pause_CanExecute));
            this.CommandBindings.Add(new CommandBinding(TetrisControlCommands.Stop, Stop_Executed, Stop_CanExecute));

            this.CommandBindings.Add(new CommandBinding(TetrisControlCommands.Left, Left_Executed));
            this.CommandBindings.Add(new CommandBinding(TetrisControlCommands.Right, Right_Executed));
            this.CommandBindings.Add(new CommandBinding(TetrisControlCommands.Rotate, Rotate_Executed));
            this.CommandBindings.Add(new CommandBinding(TetrisControlCommands.Drop, Drop_Executed));
        }

        private void Start_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SetFocus();

            if (Tetris.State == TetrisState.Stopped)
            {
                PrevFigureSet = 0;
                FigureSetChanged = false;

                SetFiguresAndColors();
            }

            Tetris.Start();
        }
        private void Start_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Tetris != null && (Tetris.State == TetrisState.Stopped || Tetris.State == TetrisState.Paused);
        }

        private void Pause_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Tetris.Pause();
        }
        private void Pause_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Tetris != null && (Tetris.State == TetrisState.Started || Tetris.State == TetrisState.Paused);
        }

        private void Stop_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Tetris.Stop();
        }
        private void Stop_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Tetris != null && (Tetris.State == TetrisState.Started || Tetris.State == TetrisState.Paused);
        }

        private void Left_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Tetris.Left();
        }
        private void Right_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Tetris.Right();
        }
        private void Rotate_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Tetris.Rotate();
        }
        private void Drop_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Tetris.Drop();
        }

        #endregion

        #region settings

        private void ButtonWidthMinus_Click(object sender, RoutedEventArgs e)
        {
            TankWidth--;

            FinalizeControls();
            InitializeControls();
        }

        private void ButtonWidthPlus_Click(object sender, RoutedEventArgs e)
        {
            TankWidth++;

            FinalizeControls();
            InitializeControls();
        }

        private void TextTankWidth_LostFocus(object sender, RoutedEventArgs e)
        {
            int PrevTankWidth = TankWidth;

            TextTankWidth.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();

            if (TankWidth != PrevTankWidth)
            {
                FinalizeControls();
                InitializeControls();
            }
        }

        private void ButtonHeightMinus_Click(object sender, RoutedEventArgs e)
        {
            TankHeight--;

            FinalizeControls();
            InitializeControls();
        }

        private void ButtonHeightPlus_Click(object sender, RoutedEventArgs e)
        {
            TankHeight++;

            FinalizeControls();
            InitializeControls();
        }

        private void TextTankHeight_LostFocus(object sender, RoutedEventArgs e)
        {
            int PrevTankHeight= TankHeight;

            TextTankHeight.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();

            if (TankHeight != PrevTankHeight)
            {
                FinalizeControls();
                InitializeControls();
            }
        }

        private void TextFigureSet_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetFiguresAndColors();

            SetFocus();
        }

        private void TextColorSet_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetFiguresAndColors();

            SetFocus();
        }

        private void TextFigureSet_DropDownClosed(object sender, EventArgs e)
        {
            SetFocus();
        }

        private void TextColorSet_DropDownClosed(object sender, EventArgs e)
        {
            SetFocus();
        }

        #endregion

        #region focus

        private void SetFocus()
        {
            Keyboard.Focus(MainPanel);
        }

        private void MainControl_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!MainPanel.IsKeyboardFocusWithin) SetFocus();
        }

        private void PanelTank_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!MainPanel.IsKeyboardFocused) SetFocus();
        }

        private void MainControl_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (Tetris.State == TetrisState.Started)
            {
                AutoPause = true;
                Tetris.Pause();
            }
        }

        private void MainControl_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (Tetris != null && Tetris.State == TetrisState.Paused && AutoPause)
            {
                Tetris.Pause();
            }
        }

        #endregion

        private Brush GetBrush(Color color)
        {
            return (Brush)BrushConverter.ConvertFromString(color.ToString());
        }

        static string GetDescription(Enum enumElement)
        {
            // <summary>Приведение значения перечисления в удобочитаемый формат.</summary>
            // <remarks>Для корректной работы необходимо использовать атрибут [Description("Name")] для каждого элемента перечисления.</remarks>
            // <param name="enumElement">Элемент перечисления</param>
            // <returns>Название элемента</returns>
            
            Type type = enumElement.GetType();

            MemberInfo[] memInfo = type.GetMember(enumElement.ToString());
            if (memInfo != null && memInfo.Length > 0)
            {
                object[] attrs = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (attrs != null && attrs.Length > 0) return ((DescriptionAttribute)attrs[0]).Description;
            }

            return enumElement.ToString();
        }
    }
}
