using System.Text.RegularExpressions;
using static terminal_minesweeper.Program.Mine;
using static terminal_minesweeper.Program.MinesweeperGame;
using static terminal_minesweeper.Program.MinesweeperGame.GridCell;

// TODO: Game size selection
// TODO: Try to set everything to private and then only set some stuff to public
namespace terminal_minesweeper {
    internal class Program {
        const string Name = "terminal-minesweeper";
        const string Version = "v1.0.0";
        static class Consts {
            public static (int, int) MineCountRange { get; } = (5, 12);
            public static Coords GridSize { get; } = new(10, 10);
        }
        static void Main(string[] args) {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Title = $"{Name} @{Version}";

            Console.Write(@"Welcome!

### Controls:
 - WASD or ↑←↓→ to move the cursor
 - ENTER or SPACE to reveal what's under the cursor position
 - E or F to flag/unflag the cell on the cursor position as a mine (only visual, doesn't change any functionality)

### How to win:
 - Uncover all the cells that have no mine without triggering any mine

Press any key to continue . . . ");
            Console.ReadKey(true);

            Console.CursorVisible = false;
            MinesweeperGame game = new();
            while (true) {
                if (game.Loop()) break;
            }
        }

        public class MinesweeperGame {
            public Random RandomGen = new();
            private Coords _curPos;
            public Coords CurPos {
                get => _curPos;
                set {
                    if (value.Y >= GameGrid.GetLength(0)) value.Y = 0;
                    if (value.Y < 0) value.Y = GameGrid.GetLength(0) - 1;
                    if (value.X >= GameGrid.GetLength(1)) value.X = 0;
                    if (value.X < 0) value.X = GameGrid.GetLength(1) - 1;
                    _curPos = value;
                }
            }
            public Grid GameGrid = new(0, 0);
            public List<Mine> Mines = new();
            public List<Coords> FlaggedCellsCoords = new();
            public List<Coords> UncoveredCellsCoords = new();
            public int ManuallyUncoveredCells = 0;
            public bool GameEnd = false;
            public bool FirstAttempt;

            public bool Loop() {
                Console.Clear();
                // reset vars
                GameGrid = new(Consts.GridSize.Y, Consts.GridSize.X);
                CurPos = new(0, 0);
                GameEnd = false;
                Mines = new();
                FlaggedCellsCoords = new();
                UncoveredCellsCoords = new();
                ManuallyUncoveredCells = 0;
                FirstAttempt = true;

                // generate random mines
                int minesToAddCount = RandomGen.Next(Consts.MineCountRange.Item1, Consts.MineCountRange.Item2 + 1);
                foreach (int i in Enumerable.Range(0, minesToAddCount)) {
                    Mines.Add(new Mine());
                }
                Mines.ForEach((mine) => { mine.Coordinates = GetRandomMineCoords(mine, GameGrid, this); });
                // calc and assign the number to every cell
                for (int y = 0; y < GameGrid.GetLength(0); y++) {
                    for (int x = 0; x < GameGrid.GetLength(1); x++) {
                        if (MineAt(new(x, y)) != null) continue;
                        var cellsAround = GetCellsAround(new(x, y), GameGrid);
                        int mineCountAround = cellsAround.Where(cell => MineAt(cell) != null).Count();
                        GameGrid[y, x].Data.Number = mineCountAround;
                    }

                }

                bool shotSuccess = false;
                while (!GameEnd) {
                    UpdateTerminal();
                    CursorShotInput();
                    UncoveredCellsCoords.Add(CurPos);

                    UpdateTerminal();
                }
                //GameEnd = true;
                UpdateTerminal();

                Thread.Sleep(300);
                ClearConsoleKeyInput();
                // hide the cursor
                _curPos = new(-1, -1);
                UpdateTerminal();

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("\nYou ");
                if (shotSuccess) {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("WIN");
                } else {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("LOSE");
                }
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"! ({ManuallyUncoveredCells} cells uncovered, {""} flags placed)");
                return EndScreenInput();
            }

            public static bool EndScreenInput() {
                const int enterPressCountToContinue = 3;
                static void PrintKeyInfo(int enterPressCount = enterPressCountToContinue) {
                    PrintColoredStrings(new() {
                        new("\nPress "),
                        new("E ", ConsoleColor.Blue),
                        new("or "),
                        new($"{(enterPressCount == 0 ? "✓" : enterPressCount)}×ENTER ", ConsoleColor.Blue),
                        new("to "),
                        new("Play ", ConsoleColor.Blue),
                        new("again."),

                        new("\nPress "),
                        new("X ", ConsoleColor.Magenta),
                        new("to "),
                        new("Exit", ConsoleColor.Magenta),
                        new("."),
                    });
                }
                int enterPressCount = 0;
                PrintKeyInfo();
                bool ok = false;
                while (!ok) {
                    ConsoleKey key = Console.ReadKey(true).Key;
                    ok = true;
                    switch (key) {
                        case ConsoleKey.X:
                            return true;
                        case ConsoleKey.E:
                            break;
                        case ConsoleKey.Enter or ConsoleKey.Spacebar:
                            enterPressCount++;
                            ok = false;
                            JumpToPrevLineClear(3);
                            PrintKeyInfo(enterPressCountToContinue - enterPressCount);
                            if (enterPressCount >= enterPressCountToContinue) {
                                ok = true;
                                Thread.Sleep(350);
                                ClearConsoleKeyInput();
                            }
                            break;
                        default:
                            ok = false;
                            break;
                    }
                }
                return false;
            }

            private void CursorShotInput() {
                bool uncovered = false;
                while (!uncovered) {
                    UpdateTerminal();
                    ConsoleKey key = Console.ReadKey(true).Key;
                    switch (key) {
                        case ConsoleKey.W or ConsoleKey.UpArrow:
                            CurPos = new(CurPos.X, CurPos.Y - 1);
                            break;
                        case ConsoleKey.A or ConsoleKey.LeftArrow:
                            CurPos = new(CurPos.X - 1, CurPos.Y);
                            break;
                        case ConsoleKey.S or ConsoleKey.DownArrow:
                            CurPos = new(CurPos.X, CurPos.Y + 1);
                            break;
                        case ConsoleKey.D or ConsoleKey.RightArrow:
                            CurPos = new(CurPos.X + 1, CurPos.Y);
                            break;
                        case ConsoleKey.E or ConsoleKey.F:
                            // do nothing if the cell is uncovered
                            if (UncoveredCellsCoords.Contains(CurPos)) break;

                            // toggle flagged
                            if (FlaggedCellsCoords.Contains(CurPos)) {
                                FlaggedCellsCoords.Remove(CurPos);
                            } else {
                                FlaggedCellsCoords.Add(CurPos);
                            }
                            break;
                        case ConsoleKey.Enter or ConsoleKey.Spacebar:
                            if (FlaggedCellsCoords.Contains(CurPos)) break;
                            uncovered = true;
                            break;
                        default:
                            break;
                    }
                }
            }

            public void UpdateTerminal() {
                if (ConsoleResize.CheckResized()) Console.Clear();
                Console.SetCursorPosition(0, 0);
                UpdateGrid(ref GameGrid, Mines, FlaggedCellsCoords, UncoveredCellsCoords);
                PrintColoredStrings(CreateGridString(GameGrid, this, CurPos));
            }

            public Mine? MineAt(Coords coords) {
                foreach (var mine in Mines) {
                    if (mine.Coordinates == coords) return mine;
                }
                return null;
            }

            public static void UpdateGrid(ref Grid grid, List<Mine> mines, List<Coords> flagsCoords, List<Coords> uncoveredCoords) {
                for (int y = 0; y < grid.GetLength(0); y++) {
                    for (int x = 0; x < grid.GetLength(1); x++) {
                        grid[y, x].Type = GridCellDisplayType.Covered;
                    }
                }
                foreach (var flagCoords in flagsCoords) {
                    grid[flagCoords.Y, flagCoords.X].Type = GridCellDisplayType.Flag;

                }
                foreach (var uncoveredCoordsItem in uncoveredCoords) {
                    grid[uncoveredCoordsItem.Y, uncoveredCoordsItem.X].Type = GridCellDisplayType.Uncovered;
                }
            }

            public static Coords GetRandomMineCoords(in Mine mine, in Grid grid, MinesweeperGame game) {
                List<Coords> allMineCoords = game.Mines.Select(mineItem => mineItem.Coordinates).ToList();
                int tries = 0;
                Coords newMineCoords;
                while (tries < ((game.GameGrid.GetLength(0) + game.GameGrid.GetLength(1)) * 3)) {
                    newMineCoords = new(
                        game.RandomGen.Next(0, grid.GetLength(1)),
                        game.RandomGen.Next(0, grid.GetLength(0))
                    );
                    if (!allMineCoords.Contains(newMineCoords)) return newMineCoords;
                    tries++;
                }
                return new(0, 0);
            }

            public class GridCell {
                public GridCellDisplayType Type = GridCellDisplayType.Covered;
                public GridCellDisplayData Data = new();
                public GridCell(GridCellDisplayType type = GridCellDisplayType.Covered, GridCellDisplayData? data = null) {
                    Type = type;
                    Data = data ?? Data;

                }
                public enum GridCellDisplayType {
                    Covered,
                    Uncovered,
                    Flag,
                    Mine
                }
                public class GridCellDisplayData {
                    public int? Number;
                    public GridCellDisplayData(int? number = null) {
                        Number = number;
                    }
                }
            }

            public static void ShowInfoScreen(List<StringColorData> stringColorData) {
                Console.Clear();
                PrintColoredStrings(stringColorData);
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write("Press any key to continue . . . ");
                Console.ResetColor();
                Console.CursorVisible = true;
                Console.ReadKey(true);
                Console.CursorVisible = false;
                Console.WriteLine();
                Console.Clear();
            }

            public static void ShowInfoScreen(string text) {
                ShowInfoScreen(new List<StringColorData>() { new(text) });
            }

            public class Grid {
                private GridCell[,] _grid = new GridCell[0, 0];
                public Grid(int sizeY, int sizeX) {
                    _grid = new GridCell[sizeY, sizeX];
                    Reset();
                }
                public void Reset() {
                    _grid = new GridCell[_grid.GetLength(0), _grid.GetLength(1)];
                    for (int y = 0; y < _grid.GetLength(0); y++) {
                        for (int x = 0; x < _grid.GetLength(1); x++) {
                            _grid[y, x] = new();
                        }
                    }
                }
                public GridCell this[int indexY, int indexX] {
                    get => _grid[indexY, indexX];
                    set => _grid[indexY, indexX] = value;
                }
                public GridCell this[Coords coords] {
                    get => this[coords.Y, coords.X];
                    set => this[coords.Y, coords.X] = value;
                }
                public int GetLength(int dimension) => _grid.GetLength(dimension);
            }
        }

        public static class ConsoleResize {
            private static Coords? previousSize = null;
            private static Coords? _currentSize = null;
            private static Coords? currentSize {
                get => _currentSize;
                set {
                    previousSize = _currentSize;
                    _currentSize = value;
                }
            }
            public static bool CheckResized() {
                currentSize = new(Console.BufferWidth, Console.BufferHeight);
                return currentSize != previousSize;
            }
        }

        public static void PrintColoredStrings(List<StringColorData> colorStringData) {
            foreach (var colorStringPair in colorStringData) {
                Console.ForegroundColor = colorStringPair.Color;
                Console.BackgroundColor = colorStringPair.BGColor ?? default;
                Console.Write(colorStringPair.String);
            }
            //Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
        }

        public static List<StringColorData> CreateGridString(Grid grid, MinesweeperGame game, Coords cursor) {
            List<StringColorData> output = new();
            for (int y = 0; y < grid.GetLength(0); y++) {
                for (int x = 0; x < grid.GetLength(1); x++) {
                    GridCell gridItem = grid[y, x];
                    StringColorData stringColorData = new("") {
                        Color = ConsoleColor.White
                    };

                    // show the mines if the game has ended
                    if ((game.MineAt(new(x, y)) != null) && (game.GameEnd || game.UncoveredCellsCoords.Contains(new(x, y)))) {
                        gridItem.Type = GridCellDisplayType.Mine;
                    }

                    // draw cursor
                    if (cursor.X == x && cursor.Y == y) {
                        stringColorData.BGColor = ConsoleColor.DarkGray;
                    }

                    // update stringColorData with data from gridItem
                    GridCellDisplayTypeStringDict.TryGetValue(gridItem.Type, out var itemStr);
                    stringColorData.String = itemStr ?? "";

                    // draw numbers
                    if (gridItem.Type == GridCellDisplayType.Uncovered) {
                        int? number = gridItem.Data.Number;
                        stringColorData.String = gridItem.Data.Number + " ";
                        if (number == 0) stringColorData.String = "  ";

                        stringColorData.Color = gridItem.Data.Number switch {
                            1 => ConsoleColor.Blue,
                            2 => ConsoleColor.Green,
                            3 => ConsoleColor.Red,
                            4 => ConsoleColor.DarkBlue,
                            5 => ConsoleColor.DarkYellow,
                            6 => ConsoleColor.DarkCyan,
                            7 => ConsoleColor.Magenta,
                            8 => ConsoleColor.Yellow,
                            _ => stringColorData.Color
                        };
                    }

                    // draw flag
                    if (gridItem.Type == GridCellDisplayType.Flag) stringColorData.Color = ConsoleColor.DarkRed;

                    output.Add(stringColorData);
                }
                output.Add(new("\n", ConsoleColor.White));
            }

            const ConsoleColor currentPosColor = ConsoleColor.DarkCyan;

            // add the horizontal border grid (A, B, C, ...)
            string loopingAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            int alphabetFullLoopCount = 0;
            int loopCount = game.GameGrid.GetLength(1);
            foreach (int i in Enumerable.Range(0, loopCount)) {
                output.Insert(i,
                    new(loopingAlphabet[0].ToString() + (alphabetFullLoopCount == 0 ? " " : "'"),
                    i == cursor.X ? currentPosColor : ConsoleColor.Gray)
                );
                List<char> alphabetList = loopingAlphabet.ToList();
                char temp = alphabetList[0];
                alphabetList.RemoveAt(0);
                alphabetList.Add(temp);
                loopingAlphabet = string.Join("", alphabetList);
                if (i + 1 == loopCount) {
                    foreach (int j in Enumerable.Range(0, 2)) {
                        output.Insert(i + 1 + j, new("\n"));
                    }
                }

                if (loopingAlphabet.StartsWith('A')) alphabetFullLoopCount++;
            }

            // add the vertical border grid (1, 2, 3, ...)
            int line = -1;
            int numbersFullLoopCount = -1;
            int realLineIndex = 0;
            for (int i = 0; i < output.Count - 1; i++) {
                int newLineCount = Regex.Matches(output[i].String, "\n").Count;
                realLineIndex += newLineCount;
                if (realLineIndex <= 1 || newLineCount == 0) { continue; } else {
                    line += newLineCount;
                }
                int displayNum = (line % 9) + 1;
                if (displayNum == 1) numbersFullLoopCount++;
                string spaceAfterNumber = " ";
                if (numbersFullLoopCount == 1) spaceAfterNumber = "'";
                if (numbersFullLoopCount > 1) spaceAfterNumber = "\"";
                if (numbersFullLoopCount > 2) spaceAfterNumber = "\"'";
                spaceAfterNumber += new string(' ', 3 - spaceAfterNumber.Length);
                output.Insert(i + 1,
                    new(displayNum.ToString() + spaceAfterNumber,
                    line == cursor.Y ? currentPosColor : ConsoleColor.Gray)
                );
            }

            output.Insert(0, new("    "));

            if (output.Last().String == "\n") output.RemoveAt(output.Count - 1);
            return output;
        }

        public class Mine {
            public Mine(Coords? coords = null) {
                coords ??= new(0, 0);
                Coordinates = (Coords)coords;
            }

            public bool IsAt(Coords coords) {
                return Coordinates == coords;
            }

            public Coords Coordinates = new(0, 0);

            public static Dictionary<GridCellDisplayType, string> GridCellDisplayTypeStringDict = new() {
                [GridCellDisplayType.Flag] = "⚑ ",
                [GridCellDisplayType.Mine] = "💣",
                [GridCellDisplayType.Covered] = "■ " // "▇ " // "■ " // □
            };
        }

        public record struct Coords(int X, int Y);
        public class StringColorData {
            public string String = "";
            public ConsoleColor Color;
            public ConsoleColor? BGColor = null;
            public StringColorData(string str, ConsoleColor color = ConsoleColor.White, ConsoleColor? bgColor = null) {
                String = str;
                Color = color;
                BGColor = bgColor ?? BGColor;
            }
        }

        public static void JumpToPrevLineClear(int lineCount = 1) {
            foreach (int _ in Enumerable.Range(0, lineCount)) {
                Console.CursorTop--;
                Console.CursorLeft = 0;
                Console.Write(new string(' ', Console.BufferWidth - 1));
                Console.CursorLeft = 0;
            }
        }

        public static void ClearConsoleKeyInput() {
            if (Console.KeyAvailable) Console.ReadKey(true);
        }

        public static List<Coords> GetCellsAround(Coords offset, Grid grid) {
            List<Coords> result = new();

            // generate square of coords
            for (int y = -1; y < 2; y++) {
                for (int x = -1; x < 2; x++) {
                    Coords toAdd = new(x, y);
                    result.Add(toAdd);
                }
            }

            // remove the center
            result.Remove(new(0, 0));

            // add offset to the result
            for (int i = 0; i < result.Count; i++) {
                Coords item = result[i];
                result[i] = new(item.X + offset.X, item.Y + offset.Y);
            }

            // remove coords that are outside of the grid
            foreach (var item in result.ToList()) {
                if (item.X >= grid.GetLength(1) || item.Y >= grid.GetLength(0) || item.X < 0 || item.Y < 0)
                    result.Remove(item);
            }
            return result.Distinct().ToList();
        }
    }
}
