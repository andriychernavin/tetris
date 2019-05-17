using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Media;
using System.Collections;

namespace Tetris
{
    // элементы, из которых состоят фигуры и весь стакан
    public class TetrisBlock : ICloneable
    {
        public int X; // координата X в фигуре
        public int Y; // координата Y в фигуре

        public Color Color; // цвет элемента

        public byte Mark; // отметка обработки элемента при расчете фигуры

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

    // фигуры
    public class TetrisFigure : ICloneable
    {
        public TetrisBlock[] Blocks; // элементы фигуры

        public int OffsetX = 0; // координата X в стакане
        public int OffsetY = 0; // координата Y в стакане

        // конструктор по элементам
        public TetrisFigure(TetrisBlock[] blocks)
        {
            Blocks = blocks;
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
        public TetrisFigure MoveLeft()
        {
            OffsetX -= 1;

            return this;
        }

        // перемещение вправо
        public TetrisFigure MoveRight()
        {
            OffsetX += 1;

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

            // смещение по горизонтали = (ширина до - ширина после) / 2
            // смещение по вертикали = (высота до - высота после) / 2

            OffsetX += (InitialWidth - InitialHeight) / 2;
            OffsetY += (InitialHeight - InitialWidth) / 2;

            // если фигура не помещается сверху - опускаем

            if (OffsetY < 0) OffsetY = 0;

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
    }
}
