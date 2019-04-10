using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Media;

namespace Tetris
{
    // список доступных наборов цветов
    public enum TetrisFigureColorsEnum
    {
        Standard = 1,
        Dark,
        Gray,
        White,
        Black,
        Random
    }

    // абстрактный набор цветов
    abstract public class TetrisFigureColors
    {
        public List<Color> FigureColors = new List<Color>(); // список цветов

        private Random Random = new Random((int)DateTime.Now.Ticks); // генератор случайных чисел

        // получить случайный цвет из набора
        public Color GetColor()
        {
            if (FigureColors.Count() == 0) throw new Exception("The set contains no colors");

            return FigureColors[Random.Next(FigureColors.Count())];
        }

        protected void AddColor(Color Color)
        {
            FigureColors.Add(Color);
        }
        protected void AddColor(string HexColor)
        {
            Color Color;

            switch (HexColor.Length)
            {
                case 6:

                    {
                        byte R = byte.Parse(HexColor.Substring(0, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
                        byte G = byte.Parse(HexColor.Substring(2, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
                        byte B = byte.Parse(HexColor.Substring(4, 2), System.Globalization.NumberStyles.AllowHexSpecifier);

                        Color = Color.FromRgb(R, G, B);
                    }

                    break;

                case 8:

                    {
                        byte A = byte.Parse(HexColor.Substring(0, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
                        byte R = byte.Parse(HexColor.Substring(2, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
                        byte G = byte.Parse(HexColor.Substring(4, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
                        byte B = byte.Parse(HexColor.Substring(6, 2), System.Globalization.NumberStyles.AllowHexSpecifier);

                        Color = Color.FromArgb(A, R, G, B);
                    }

                    break;

                default:

                    throw new Exception("Wrong color code");
            }

            FigureColors.Add(Color);
        }
    }

    // набор светлых цветов
    public class TetrisColorsStandard : TetrisFigureColors
    {
        public TetrisColorsStandard()
        {
            AddColor(Colors.Red);
            AddColor(Colors.Green);
            AddColor(Colors.Blue);
            AddColor(Colors.Cyan);
            AddColor(Colors.Magenta);
            AddColor(Colors.Yellow);
        }
    }

    // набор белого цвета
    public class TetrisColorsWhite : TetrisFigureColors
    {
        public TetrisColorsWhite()
        {
            AddColor("FAFAFA");
        }
    }

    // набор черного цвета
    public class TetrisColorsBlack : TetrisFigureColors
    {
        public TetrisColorsBlack()
        {
            AddColor("2A2A2A");
        }
    }

    // набор серых цветов
    public class TetrisColorsGray: TetrisFigureColors
    {
        public TetrisColorsGray()
        {
            AddColor("555555");
            AddColor("777777");
            AddColor("999999");
            AddColor("BBBBBB");
            AddColor("DDDDDD");
        }
    }

    // набор темных цветов
    public class TetrisColorsDark : TetrisFigureColors
    {
        public TetrisColorsDark()
        {
            AddColor(Colors.DarkRed);
            AddColor(Colors.DarkGreen);
            AddColor(Colors.DarkBlue);
            AddColor(Colors.DarkCyan);
            AddColor(Colors.DarkMagenta);
            AddColor(Colors.Brown);
        }
    }
}
