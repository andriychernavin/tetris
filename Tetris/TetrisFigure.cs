using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Media;
using System.Collections;
using System.ComponentModel;

namespace Tetris
{
    // список вариантов связности фигуры
    public enum TetrisFigureBindingTypeEnum
    {
        None = 1,
        Corners = 2,
        Sides = 4
    }

    // элементы, из которых состоят фигуры и весь стакан
    public class TetrisBlock : ICloneable
    {
        public int X; // координата X в фигуре
        public int Y; // координата Y в фигуре

        public Color Color; // цвет элемента

        public byte Mark; // отметка обработки элемента при расчете фигуры

        public bool Shadow; // признак тени под активной фигурой

        public TetrisBlock(int x, int y, Color color)
        {
            X = x;
            Y = y;

            Color = color;
        }

        public TetrisBlock(int x, int y) : this(x, y, Colors.White) { }

        // конструктор для клонирования
        protected TetrisBlock(TetrisBlock another) : this(another.X, another.Y, another.Color) { }

        // клонирование объекта
        public object Clone()
        {
            return new TetrisBlock(this);
        }
    }

    public struct Coordinates
    {
        public int X;
        public int Y;

        public Coordinates(int X, int Y)
        {
            this.X = X;
            this.Y = Y;
        }
    }

    // фигуры
    public class TetrisFigure : ICloneable
    {
        public TetrisBlock[] Blocks; // элементы фигуры

        public int OffsetX = 0; // координата X в стакане
        public int OffsetY = 0; // координата Y в стакане

        public int ShadowOffsetY; // координата Y тени в стакане

        private List<Coordinates> Offsets = new List<Coordinates>(); // альтернативные координаты фигуры (в зависимости от центра вращения)

        public int Chance {
            get { 
                return ChanceValue; 
            }
            set { 
                if (value < 1 || value > 100) throw new ArgumentException("Chance must be between 1 and 100");
                ChanceValue = value; 
            }
        }
        private int ChanceValue = 100; // вероятность выбора фигуры в процентах

        // конструктор по элементам
        public TetrisFigure(TetrisBlock[] blocks, int chance = 100)
        {
            Blocks = blocks;
            Chance = chance;
        }

        // конструктор по координатам элементов
        public TetrisFigure(int[,] blocks)
        {
            Blocks = new TetrisBlock[blocks.GetLength(0)];

            for (int i = 0; i < Blocks.Length; i++)
                Blocks[i] = new TetrisBlock(blocks[i, 0], blocks[i, 1]);
        }

        // конструктор для клонирования
        protected TetrisFigure(TetrisFigure another) : this((TetrisBlock[])another.Blocks.Clone())
        {
            for (int i = 0; i < Blocks.Length; i++)
                Blocks[i] = (TetrisBlock)Blocks[i].Clone();

            OffsetX = another.OffsetX;
            OffsetY = another.OffsetY;

            Offsets = another.Offsets.ToList();

            Chance = another.Chance;

            ShadowOffsetY = another.ShadowOffsetY;
        }

        // клонирование объекта
        public object Clone()
        {
            return new TetrisFigure(this);
        }

        // ширина фигуры
        public int Width
        {
            get { return Blocks.Max(e => e.X) + 1; }
        }

        // высота фигуры
        public int Height
        {
            get { return Blocks.Max(e => e.Y) + 1; }
        }

        // установка цвета элементов
        public Color Color
        {
            set
            {
                foreach (TetrisBlock Block in Blocks)
                    Block.Color = value;
            }
        }

        // массив для отображения фигуры
        public TetrisBlock[,] GetView()
        {
            lock (Blocks)
            {
                TetrisBlock[,] View = new TetrisBlock[Width, Height];

                foreach (TetrisBlock Block in Blocks)
                    View[Block.X, Block.Y] = Block;

                return View;
            }
        }

        // проверка подобия фигур
        public bool IsSimilar(TetrisFigure other)
        {
            if (Blocks.Count() != other.Blocks.Count()) return false;

            TetrisFigure Figure = (TetrisFigure)other.Clone();

            bool CompareBlocks()
            {
                return Blocks.Count() == Figure.Blocks.Count() && Blocks.All(p1 => Figure.Blocks.Any(p2 => p1.X == p2.X && p1.Y == p2.Y));
            }

            // совпадение

            if (CompareBlocks()) return true;

            // совпадение после поворота на 90

            Figure.MoveRotate();

            if (CompareBlocks()) return true;

            // совпадение после поворота на 180

            Figure.MoveRotate();

            if (CompareBlocks()) return true;

            // совпадение после поворота на 270

            Figure.MoveRotate();

            if (CompareBlocks()) return true;

            // не совпало

            return false;
        }

        // перемещение влево
        public TetrisFigure MoveLeft(int distance = 1)
        {
            OffsetX -= distance;

            return this;
        }

        // перемещение вправо
        public TetrisFigure MoveRight(int distance = 1)
        {
            OffsetX += distance;

            return this;
        }

        // перемещение вниз
        public TetrisFigure MoveDown()
        {
            OffsetY += 1;

            return this;
        }

        // перемещение поворотом
        public TetrisFigure MoveRotate()
        {
            int InitialWidth = Width;
            int InitialHeight = Height;

            Offsets.Clear();

            // смещение по горизонтали = (ширина до - ширина после) / 2
            // смещение по вертикали = (высота до - высота после) / 2

            // точное смещение
            double RealOffsetX = OffsetX + (InitialWidth - InitialHeight) / 2.0;
            double RealOffsetY = OffsetY + (InitialHeight - InitialWidth) / 2.0;

            // округленное смещение (дробную часть отбрасываем)
            OffsetX += (InitialWidth - InitialHeight) / 2;
            OffsetY += (InitialHeight - InitialWidth) / 2;

            // если фигура не помещается сверху - опускаем

            if (RealOffsetY < 0) RealOffsetY = 0;
            if (OffsetY < 0) OffsetY = 0;

            // добавляем варианты смещения

            for (int Y = (int)Math.Floor(RealOffsetY); Y <= (int)Math.Ceiling(RealOffsetY); Y++)
            {
                for (int X = (int)Math.Floor(RealOffsetX); X <= (int)Math.Ceiling(RealOffsetX); X++)
                {
                    if (X != OffsetX || Y != OffsetY)
                    {
                        Offsets.Add(new Coordinates(X, Y));
                    }
                }
            }

            // преобразуем координаты каждой точки

            for (int i = 0; i < Blocks.Count(); i++)
            {
                int NewX = InitialHeight - Blocks[i].Y - 1;
                int NewY = Blocks[i].X;

                Blocks[i].X = NewX;
                Blocks[i].Y = NewY;
            }

            return this;
        }

        // попытка выбрать следующее смещение из списка Offsets
        public bool NextOffset()
        {
            if (Offsets.Count > 0)
            {
                Coordinates Offset = Offsets.First();

                Offsets.Remove(Offset);

                OffsetX = Offset.X;
                OffsetY = Offset.Y;

                return true;
            }
            else
            {
                return false;
            }
        }

        // тип связности фигуры
        public TetrisFigureBindingTypeEnum BindingType
        {
            get
            {
                // http://algolist.manual.ru/maths/graphs/linked.php

                {
                    // проверяем на связность сторонами

                    // начальное значение - точки не связаны
                    foreach (TetrisBlock Block in Blocks) Block.Mark = 1;

                    // начинаем проверку связности с этой точки
                    Blocks[0].Mark = 2;

                    TetrisBlock StartBlock;

                    while ((StartBlock = Blocks.FirstOrDefault(p => p.Mark == 2)) != null)
                    {
                        // точка обработана
                        StartBlock.Mark = 3;

                        // связанные точки ставим в очередь на обработку
                        foreach (TetrisBlock Block in Blocks.Where(p => p.Mark == 1 && Math.Abs(StartBlock.X - p.X) + Math.Abs(StartBlock.Y - p.Y) == 1)) Block.Mark = 2;
                    }

                    // все точки связаны
                    if (!Blocks.Any(p => p.Mark == 1)) return TetrisFigureBindingTypeEnum.Sides;
                }

                {
                    // проверяем на связность диагоналями

                    // начальное значение - точки не связаны
                    foreach (TetrisBlock Block in Blocks) Block.Mark = 1;

                    // начинаем проверку связности с этой точки
                    Blocks[0].Mark = 2;

                    TetrisBlock StartBlock;

                    while ((StartBlock = Blocks.FirstOrDefault(p => p.Mark == 2)) != null)
                    {
                        // точка обработана
                        StartBlock.Mark = 3;

                        // связанные точки ставим в очередь на обработку
                        foreach (TetrisBlock Block in Blocks.Where(p => p.Mark == 1 && Math.Abs(StartBlock.X - p.X) <= 1 && Math.Abs(StartBlock.Y - p.Y) <= 1)) Block.Mark = 2;
                    }

                    // все точки связаны
                    if (!Blocks.Any(p => p.Mark == 1)) return TetrisFigureBindingTypeEnum.Corners;
                }

                return TetrisFigureBindingTypeEnum.None;
            }
        }
    }
}
