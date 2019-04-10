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
    // список доступных наборов фигур
    public enum TetrisFiguresEnum
    {
        [Description("Classic")]
        Standard = 1,

        [Description("2 x 2")]
        Figures2x2,

        [Description("3 x 3")]
        Figures3x3,

        [Description("4 x 4")]
        Figures4x4,
        
        Random
    }

    // абстрактный набор фигур
    abstract public class TetrisFigures
    {
        protected List<TetrisFigure> Figures = new List<TetrisFigure>(); // список фигур

        private Random Random = new Random((int)DateTime.Now.Ticks); // генератор случайных чисел

        // добавить фигуру в набор
        public void AddFigure(TetrisFigure Figure)
        {
            Figures.Add(Figure);
        }

        // получить случайную фигуру из набора
        public TetrisFigure GetFigure()
        {
            if (Figures.Count() == 0) throw new Exception("The set contains no figures");

            TetrisFigure Figure = (TetrisFigure)Figures[Random.Next(Figures.Count())].Clone();

            return Figure;
        }

        // получить случайную фигуру заданного цвета из набора
        public TetrisFigure GetFigure(Color color)
        {
            TetrisFigure Figure = GetFigure();

            Figure.Color = color;

            return Figure;
        }

        // максимальная ширина фигуры в наборе
        public int MaxWidth
        {
            get { return Figures.Max(x => x.Width); }
        }

        // максимальная высота фигуры в наборе
        public int MaxHeight
        {
            get { return Figures.Max(x => x.Height); }
        }
    }

    // набор стандартных фигур
    public class TetrisFiguresStandard : TetrisFigures
    {
        public TetrisFiguresStandard()
        {
            AddFigure(new TetrisFigure(new int[,] { { 0, 0 }, { 1, 0 }, { 2, 0 }, { 3, 0 } })); // палка
            AddFigure(new TetrisFigure(new int[,] { { 0, 0 }, { 1, 0 }, { 0, 1 }, { 1, 1 } })); // квадрат
            AddFigure(new TetrisFigure(new int[,] { { 0, 0 }, { 1, 0 }, { 2, 0 }, { 2, 1 } })); // г
            AddFigure(new TetrisFigure(new int[,] { { 0, 0 }, { 1, 0 }, { 2, 0 }, { 0, 1 } })); // г наоборот
            AddFigure(new TetrisFigure(new int[,] { { 0, 0 }, { 1, 0 }, { 2, 0 }, { 1, 1 } })); // т
            AddFigure(new TetrisFigure(new int[,] { { 0, 0 }, { 1, 0 }, { 1, 1 }, { 2, 1 } })); // z
            AddFigure(new TetrisFigure(new int[,] { { 1, 0 }, { 2, 0 }, { 0, 1 }, { 1, 1 } })); // z наоборот
        }
    }

    // набор случайных фигур
    public class TetrisFiguresRandom : TetrisFigures
    {
        public TetrisFiguresRandom(int Size, bool CheckConnected = true, int MinBlocks = 0, int MaxBlocks = 0)
        {
            int Variants = (int)Math.Pow(2, Size * Size); // количество вариантов для анализа

            for (int Variant = 1; Variant < Variants; Variant++)
            {
                bool[] Bits = Convert.ToString(Variant, 2).PadLeft(Size * Size, '0').Select(c => c == '1').ToArray();

                // проверяем количество точек

                int BlockCount = Bits.Count(b => b);

                if ((MinBlocks != 0 && BlockCount < MinBlocks) || (MaxBlocks != 0 && BlockCount > MaxBlocks)) continue;

                // заполняем точки фигуры, проверяем положение в левом верхнем углу

                TetrisBlock[] Blocks = new TetrisBlock[BlockCount];

                bool FirstRow = false;
                bool FirstColumn = false;

                int BlockNumber = 0;

                for (int X = 0; X < Size; X++)
                    for (int Y = 0; Y < Size; Y++)
                        if (Bits[X * Size + Y])
                        {
                            Blocks[BlockNumber] = new TetrisBlock(X, Y);

                            BlockNumber++;

                            if (Y == 0) FirstRow = true;
                            if (X == 0) FirstColumn = true;
                        }

                if (!FirstRow || !FirstColumn) continue;
                
                // проверяем связность фигуры

                if (CheckConnected)
                {
                    // http://algolist.manual.ru/maths/graphs/linked.php

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

                    // остались не связанные точки
                    if (Blocks.Any(p => p.Mark == 1)) continue;
                }

                // проверяем совпадение фигур и добавляем фигуру

                TetrisFigure Figure = new TetrisFigure(Blocks);

                if (!Figures.Any(f => f.IsSimilar(Figure))) AddFigure(Figure);
            }
        }
    }
}
