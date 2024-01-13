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
        Classic = 1,

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

        private bool RandomRotate; // признак случайного вращения новой фигуры

        public TetrisFigures(bool RandomRotate = true)
        {
            this.RandomRotate = RandomRotate;
        }

        // добавить фигуру в набор
        public void AddFigure(TetrisFigure Figure)
        {
            Figures.Add(Figure);
        }

        // получить случайную фигуру из набора
        public TetrisFigure GetFigure()
        {
            if (Figures.Count() == 0) throw new Exception("Set contains no figures");

            int TotalChance = 0;

            Figures.ForEach(f => TotalChance += f.Chance);

            int RandomChance = Random.Next(TotalChance) + 1;

            TotalChance = 0;

            return Figures.First(f => (TotalChance += f.Chance) >= RandomChance).Clone() as TetrisFigure;
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
            get
            {
                if (RandomRotate)
                    return Math.Max(Figures.Max(x => x.Width), Figures.Max(x => x.Height));
                else
                    return Figures.Max(x => x.Width);
            }
        }

        // максимальная высота фигуры в наборе
        public int MaxHeight
        {
            get
            {
                if (RandomRotate)
                    return Math.Max(Figures.Max(x => x.Width), Figures.Max(x => x.Height));
                else
                    return Figures.Max(x => x.Height);
            }
        }

        // устанавливает вероятность выбора для каждой фигуры
        public void SetChance(int Chance, int MinBlocks = 0, int MaxBlocks = 0, TetrisFigureBindingTypeEnum? BindingType = null)
        {
            if (Chance < 1 || Chance > 100) throw new Exception("Wrong figure chance");

            foreach (TetrisFigure Figure in Figures)
            {
                if ((MinBlocks == 0 || MinBlocks <= Figure.Blocks.Length) && (MaxBlocks == 0 || MaxBlocks >= Figure.Blocks.Length) && (!BindingType.HasValue || BindingType.Value.HasFlag(Figure.BindingType)))
                    Figure.Chance = Chance;
            }
        }
    }

    // набор стандартных фигур
    public class TetrisFiguresClassic : TetrisFigures
    {
        public TetrisFiguresClassic() : base()
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

    // генератор набора фигур
    public class TetrisFiguresGenerator : TetrisFigures
    {
        public TetrisFiguresGenerator(bool RandomRotate = true) : base(RandomRotate) { }

        public void AddFigures(int SizeX, int SizeY, int MinBlocks = 0, int MaxBlocks = 0, TetrisFigureBindingTypeEnum BindingType = TetrisFigureBindingTypeEnum.Sides, int Chance = 100)
        {
            int Variants = (int)Math.Pow(2, SizeX * SizeY); // количество вариантов для анализа

            for (int Variant = 1; Variant < Variants; Variant++)
            {
                bool[] Bits = Convert.ToString(Variant, 2).PadLeft(SizeX * SizeY, '0').Select(c => c == '1').ToArray();

                // проверяем количество точек

                int BlockCount = Bits.Count(b => b);

                if ((MinBlocks != 0 && BlockCount < MinBlocks) || (MaxBlocks != 0 && BlockCount > MaxBlocks)) continue;

                // заполняем точки фигуры, проверяем положение в левом верхнем углу

                TetrisBlock[] Blocks = new TetrisBlock[BlockCount];

                bool FirstRow = false;
                bool FirstColumn = false;

                int BlockNumber = 0;

                for (int X = 0; X < SizeX; X++)
                    for (int Y = 0; Y < SizeY; Y++)
                        if (Bits[X * SizeY + Y])
                        {
                            Blocks[BlockNumber] = new TetrisBlock(X, Y);

                            BlockNumber++;

                            if (Y == 0) FirstRow = true;
                            if (X == 0) FirstColumn = true;
                        }

                if (!FirstRow || !FirstColumn) continue;

                TetrisFigure Figure = new TetrisFigure(Blocks, Chance);
                
                // проверяем связность фигуры

                if (!BindingType.HasFlag(Figure.BindingType)) continue;

                // проверяем совпадение фигур и добавляем фигуру или повышаем вероятность фигуры

                TetrisFigure FoundFigure = Figures.FirstOrDefault(f => f.IsSimilar(Figure));

                if (FoundFigure == null)
                {
                    AddFigure(Figure);
                }
                else if (FoundFigure.Chance < Figure.Chance)
                {
                    FoundFigure.Chance = Figure.Chance;
                }
            }
        }
    }
}
