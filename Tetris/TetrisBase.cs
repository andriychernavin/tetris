using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Threading;
using System.Windows.Media;

//using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Tetris
{
    // состояния игры
    public enum TetrisState
    {
        Started,
        Paused,
        Stopped
    }

    // команды игры
    public enum TetrisCommand
    {
        Left, // пользовательские команды
        Right,
        Rotate,
        Drop,
        Down // служебные команды
    }

    // движок игры
    public class TetrisBase : INotifyPropertyChanged, IDisposable
    {
        #region properties

        private TetrisBlock[,] Tank; // стакан, координаты стандартные (слева направо, сверху вниз)

        public TetrisFigures Figures // набор фигур
        {
            set { TetrisFiguresValue = value; } // обновить тек и след фигуры
            get { return TetrisFiguresValue; }
        }
        private TetrisFigures TetrisFiguresValue;

        public TetrisFigureColors FigureColors // набор цветов
        {
            set { FigureColorsValue = value; } // обновить цвет тек и след фигуры
            get { return FigureColorsValue; }
        }

        private TetrisFigureColors FigureColorsValue;

        private TetrisFigure CurrentFigure; // текущая фигура

        public TetrisFigure NextFigure // следующая фигура
        {
            private set { NextFigureValue = value; OnPropertyChanged(); }
            get { return NextFigureValue; }
        }
        private TetrisFigure NextFigureValue;

        private ObservableQueue<TetrisCommand> Commands = new ObservableQueue<TetrisCommand>(); // список команд для обработки

        private TetrisCommand? DelayedCommand; // ожидающая команда (Left, Right)

        private Timer TimerDown; // таймер для движения фигур

        private bool DropFlag; // признак бросания фигуры

        public TetrisState State // статус игры
        {
            private set { StateValue = value; OnPropertyChanged(); }
            get { return StateValue; }
        }
        private TetrisState StateValue;

        public int Level // уровень
        {
            private set { LevelValue = value; OnPropertyChanged(); }
            get { return LevelValue; }
        }
        private int LevelValue;

        public int Lines // количество заполненных линий на текущем уровне
        {
            private set { LinesValue = value; OnPropertyChanged(); }
            get { return LinesValue; }
        }
        private int LinesValue;

        public int Score // результат
        {
            private set { ScoreValue = value; OnPropertyChanged(); }
            get { return ScoreValue; }
        }
        private int ScoreValue;

        const int MaxDelay = 1500; // начальная длина шага
        const int MinDelay = 150; // конечная длина шага
        const int DelayStep = 150; // уменьшение длины шага за уровень
        const int DropDelay = 20; // длина шага при бросании фигуры
        const int DropLastDelay = 150; // длина последнего шага при бросании фигуры (100 - мало, 300 - много)
        const int PackDelay = 100; // пауза при исчезновении строки

        public int Width // ширина стакана
        {
            get { return Tank.GetLength(0); }
        }

        public int Height // высота стакана
        {
            get { return Tank.GetLength(1); }
        }

        public EventHandler<TetrisGameOverEventArgs> GameOver; // делегат для сохранения результатов игры

        #endregion

        #region constructor

        public TetrisBase(int desiredWidth, int desiredHeight, TetrisFigures figures = null, TetrisFigureColors colors = null)
        {
            Figures = figures ?? new TetrisFiguresStandard();

            FigureColors = colors ?? new TetrisColorsStandard();

            State = TetrisState.Stopped;

            Tank = new TetrisBlock[Math.Max(desiredWidth, Figures.MaxWidth), Math.Max(desiredHeight, Figures.MaxHeight)];

            OnPropertyChanged("Tank");

            Commands.CollectionChanged += CommandsChanged;
        }

        public void Dispose()
        {
            StopTimerDown();
        }

        #endregion

        #region property changed

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName]string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region figure processing

        // добавление фигуры в стакан
        private bool CreateCurrentFigure()
        {
            CurrentFigure = NextFigure ?? Figures.GetFigure(FigureColors.GetColor());

            CurrentFigure.OffsetX = (Width - CurrentFigure.Width) / 2;
            CurrentFigure.OffsetY = 0;

            // проверяем место для фигуры

            if (!TryCurrentFigure(CurrentFigure)) return false;

            // добавляем фигуру в стакан

            ShowCurrentFigure();

            // добавляем следующую фигуру

            CreateNextFigure();

            return true;
        }

        public void CreateNextFigure()
        {
            NextFigure = Figures.GetFigure(FigureColors.GetColor());
        }

        public void ColorNextFigure()
        {
            NextFigure.Color = FigureColors.GetColor();

            OnPropertyChanged("NextFigure");
        }

        // проверка перемещения влево
        private bool TryLeft()
        {
            TetrisFigure Figure = (TetrisFigure)CurrentFigure.Clone();

            Figure.MoveLeft();

            return TryCurrentFigure(Figure);
        }

        // перемещение влево
        private void MoveLeft()
        {
            HideCurrentFigure();

            CurrentFigure.MoveLeft();

            ShowCurrentFigure();
        }

        // проверка перемещения вправо
        private bool TryRight()
        {
            TetrisFigure Figure = (TetrisFigure)CurrentFigure.Clone();

            Figure.MoveRight();

            return TryCurrentFigure(Figure);
        }

        // перемещение вправо
        private void MoveRight()
        {
            HideCurrentFigure();

            CurrentFigure.MoveRight();

            ShowCurrentFigure();
        }

        // проверка перемещения вниз
        private bool TryDown()
        {
            TetrisFigure Figure = (TetrisFigure)CurrentFigure.Clone();

            Figure.MoveDown();

            return TryCurrentFigure(Figure);
        }

        // перемещение вниз
        private void MoveDown()
        {
            HideCurrentFigure();

            CurrentFigure.MoveDown();

            ShowCurrentFigure();
        }

        // проверка перемещения поворотом
        private bool TryRotate()
        {
            TetrisFigure Figure = (TetrisFigure)CurrentFigure.Clone();

            Figure.MoveRotate();

            return TryCurrentFigure(Figure);
        }

        // перемещение поворотом
        private void MoveRotate()
        {
            HideCurrentFigure();

            CurrentFigure.MoveRotate();

            ShowCurrentFigure();
        }

        // проверка возможности перемещения фигуры на заданную позицию
        private bool TryCurrentFigure(TetrisFigure Figure)
        {
            foreach (TetrisBlock Block in Figure.Blocks)
            {
                int X = Block.X + Figure.OffsetX;
                int Y = Block.Y + Figure.OffsetY;

                // проверяем попадание в стакан
                if (X < 0 || X > Width - 1 || Y < 0 || Y > Height - 1) return false;

                // проверяем наложение на другие элементы (кроме текущей фигуры)
                if (Tank[X, Y] != null && CurrentFigure.Blocks.SingleOrDefault(p => p == Tank[X, Y]) == null) return false;
            }

            return true;
        }

        // убираем текущую фигуру с картинки
        private void HideCurrentFigure()
        {
            foreach (TetrisBlock Block in CurrentFigure.Blocks)
            {
                int X = Block.X + CurrentFigure.OffsetX;
                int Y = Block.Y + CurrentFigure.OffsetY;

                Tank[X, Y] = null;

                OnPropertyChanged("TankX" + X.ToString() + "Y" + Y.ToString());
            }
        }

        // добавляем текущую фигуру на картинку
        private void ShowCurrentFigure()
        {
            foreach (TetrisBlock Block in CurrentFigure.Blocks)
            {
                int X = Block.X + CurrentFigure.OffsetX;
                int Y = Block.Y + CurrentFigure.OffsetY;

                Tank[X, Y] = Block;

                OnPropertyChanged("TankX" + X.ToString() + "Y" + Y.ToString());
            }
        }

        #endregion

        #region tank processing

        // упаковка стакана
        private void Pack()
        {
            int TankWidth = Width;
            int TankHeight = Height;

            int InitialLevel = Level;

            for (int Y = 0; Y < TankHeight; Y++)
            {
                int BlockCount = 0;

                for (int X = 0; X < TankWidth; X++)
                    if (Tank[X, Y] != null)
                        BlockCount++;

                if (BlockCount == TankWidth)
                {
                    for (int X = 0; X < TankWidth; X++)
                    {
                        for (int Y1 = Y; Y1 > 0; Y1--)
                            Tank[X, Y1] = Tank[X, Y1 - 1];

                        Tank[X, 0] = null;
                    }

                    Score += TankWidth * InitialLevel;

                    if (Level == InitialLevel)
                    {
                        Lines += 1;

                        if (Lines == TankHeight)
                        {
                            Level += 1;
                            Lines = 0;
                        }
                    }

                    Thread.Sleep(PackDelay);

                    OnPropertyChanged("Tank");
                }
            }
        }

        #endregion

        #region start and stop commands

        public void Start()
        {
            switch (State)
            {
                case TetrisState.Paused:

                    State = TetrisState.Started;
                    lock (Commands) Commands.Clear();
                    StartTimerDown();

                    break;

                case TetrisState.Stopped:

                    Array.Clear(Tank, 0, Tank.GetLength(0) * Tank.GetLength(1));

                    OnPropertyChanged("Tank");

                    CurrentFigure = null;
                    NextFigure = null;

                    lock (Commands) Commands.Clear();

                    Level = 1;
                    Lines = 0;
                    Score = 0;

                    if (CreateCurrentFigure())
                    {
                        State = TetrisState.Started;
                        lock (Commands) Commands.Clear();
                        StartTimerDown();
                    }
                    else
                    {
                        State = TetrisState.Stopped;
                    }

                    break;
            }
        }

        public void Pause()
        {
            switch (State)
            {
                case TetrisState.Started:

                    State = TetrisState.Paused;

                    StopTimerDown();

                    break;

                case TetrisState.Paused:

                    State = TetrisState.Started;
                    lock (Commands) Commands.Clear();
                    StartTimerDown();

                    break;
            }
        }

        public void Stop()
        {
            switch (State)
            {
                case TetrisState.Started:
                case TetrisState.Paused:

                    CurrentFigure = null;
                    NextFigure = null;

                    lock (Commands) Commands.Clear();

                    StopTimerDown();

                    State = TetrisState.Stopped;

                    break;
            }
        }

        #endregion

        #region timer commands

        private void StartTimerDown()
        {
            if (TimerDown != null) StopTimerDown();

            DropFlag = false;

            DelayedCommand = null;

            int period = TimerDownPeriod();

            TimerDown = new Timer(new TimerCallback(TimerDownExecute), null, period, period);
        }

        private void ChangeTimerDown(int dueTime, int period)
        {
            TimerDown.Change(dueTime, period);
        }

        private void StopTimerDown()
        {
            TimerDown?.Dispose();
            TimerDown = null;
        }

        private void TimerDownExecute(object sender)
        {
            if (IsRunning())
            {
                lock (Commands) Commands.Clear();
                Commands.Enqueue(TetrisCommand.Down);
            }
        }

        private int TimerDownPeriod()
        {
            return Math.Max(MaxDelay - (Level - 1) * DelayStep, MinDelay);
        }

        #endregion

        #region user commands

        public void Left()
        {
            if (IsRunning()) Commands.Enqueue(TetrisCommand.Left);
        }

        public void Right()
        {
            if (IsRunning()) Commands.Enqueue(TetrisCommand.Right);
        }

        public void Rotate()
        {
            if (IsRunning()) Commands.Enqueue(TetrisCommand.Rotate);
        }

        public void Drop()
        {
            if (IsRunning()) Commands.Enqueue(TetrisCommand.Drop);
        }

        #endregion

        #region command processing

        private void CommandsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            lock (Commands)
            {
                if (Commands.Count() == 0) return;

                //Debug.WriteLine("Thread: " + Thread.CurrentThread.ManagedThreadId);

                switch (Commands.Dequeue())
                {
                    case TetrisCommand.Left:

                        if (TryLeft())
                            MoveLeft();
                        else if (DropFlag)
                            DelayedCommand = TetrisCommand.Left;

                        break;

                    case TetrisCommand.Right:

                        if (TryRight())
                            MoveRight();
                        else if (DropFlag)
                            DelayedCommand = TetrisCommand.Right;

                        break;

                    case TetrisCommand.Rotate:

                        if (TryRotate()) MoveRotate();

                        break;

                    case TetrisCommand.Drop:

                        DropFlag = !DropFlag;
                        DelayedCommand = null;
                        ChangeTimerDown(0, DropFlag ? DropDelay : TimerDownPeriod());

                        break;

                    case TetrisCommand.Down:

                        if (TryDown())
                        {
                            MoveDown();

                            if (DropFlag && DelayedCommand != null)
                            {
                                switch (DelayedCommand)
                                {
                                    case TetrisCommand.Left:
                                        if (TryLeft())
                                        {
                                            MoveLeft();
                                            DropFlag = false;
                                            DelayedCommand = null;
                                            ChangeTimerDown(DropDelay, TimerDownPeriod());
                                        }
                                        break;
                                    case TetrisCommand.Right:
                                        if (TryRight())
                                        {
                                            MoveRight();
                                            DropFlag = false;
                                            DelayedCommand = null;
                                            ChangeTimerDown(DropDelay, TimerDownPeriod());
                                        }
                                        break;
                                }
                            }

                            if (DropFlag && !TryDown())
                            {
                                DropFlag = false;
                                DelayedCommand = null;
                                ChangeTimerDown(DropLastDelay, TimerDownPeriod());
                            }
                        }
                        else
                        {
                            StopTimerDown();

                            Pack();

                            if (CreateCurrentFigure())
                            {
                                StartTimerDown();
                            }
                            else
                            {
                                Stop();
                                OnGameOver();
                            }
                        }

                        break;
                }
            }
        }

        #endregion

        private bool IsRunning() // проверка работы игры
        {
            return (State == TetrisState.Started);
        }

        public TetrisBlock[,] GetView() // массив для отображения стакана
        {
            return Tank;
        }

        public TetrisBlock GetView(int X, int Y) // точка из стакана
        {
            return Tank[X, Y];
        }

        private void OnGameOver()
        {
            if (GameOver != null)
            {
                TetrisGameOverEventArgs e = new TetrisGameOverEventArgs();

                e.Width = Width;
                e.Height = Height;
                e.Level = Level;
                e.Score = Score;

                GameOver(this, e);
            }

            //GameOver?.Invoke(this, e);
        }
    }

    public class TetrisGameOverEventArgs : EventArgs
    {
        public int Width;
        public int Height;
        public int Level;
        public int Score;
        public TetrisFiguresEnum FigureSet;
    }

    // очередь с подпиской на изменение
    public class ObservableQueue<T> : Queue<T>, System.Collections.Specialized.INotifyCollectionChanged //System.ComponentModel.INotifyPropertyChanged
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        //public event PropertyChangedEventHandler PropertyChanged;

        public new void Clear()
        {
            base.Clear();

            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs())
        }

        public new void Enqueue(T obj)
        {
            base.Enqueue(obj);

            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, obj));
            //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs())
        }

        public new T Dequeue()
        {
            T obj = base.Dequeue();

            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, obj));
            //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs())

            return obj;
        }
    }
}
