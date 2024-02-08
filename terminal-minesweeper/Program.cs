using static terminal_minesweeper.Program.Mine;
using static terminal_minesweeper.Program.MinesweeperGame;
using static terminal_minesweeper.Program.MinesweeperGame.GridCell;

// TODO: Try to set everything to private and then only set some stuff to public
namespace terminal_minesweeper {
    internal class Program {
        const string Name = "terminal-minesweeper";
        const string Version = "v1.0.0";
        static class Consts {
            public static (int, int) MineCountRange { get; } = (5, 6);
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
 - Clear all cells that have no mine without triggering any mine

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
            public List<Coords> UncoveredCellsCoords = new();
            public int CellsUncovered = 0;
            public bool GameEnd = false;
            public bool FirstAttempt;

            public bool Loop() {
                Console.Clear();
                // reset vars
                GameGrid = new(Consts.GridSize.Y, Consts.GridSize.X);
                CurPos = new(0, 0);
                GameEnd = false;
                Mines = new();
                UncoveredCellsCoords = new();
                CellsUncovered = 0;
                FirstAttempt = true;

                // generate random mines
                int minesToAddCount = RandomGen.Next(Consts.MineCountRange.Item1, Consts.MineCountRange.Item2 + 1);
                foreach (int i in Enumerable.Range(0, minesToAddCount)) {
                    Mines.Add(new Mine());
                }
                Mines.ForEach((mine) => { mine.Coordinates = GetRandomMineCoords(mine, GameGrid, this); });

                bool shotSuccess = false;
                while (!GameEnd) {
                    UpdateTerminal();
                    CursorShotInput();
                    //shotSuccess = CheckShot();
                    UpdateTerminal();
                    //AttemptsLeft--;
                    //if (!shotSuccess) {
                    //    MissedCoords.Add(CurPos);
                    //    if (AttemptsLeft <= 0) break;
                    //    ShowInfoScreen(new List<StringColorPair> {
                    //        new("You MISSED! You have ", ConsoleColor.Gray),
                    //        new($"{AttemptsLeft} {(AttemptsLeft == 1 ? "attempt" : "attempts")} left"),
                    //        new(".", ConsoleColor.Gray)
                    //    });
                    //} else break;
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
                Console.WriteLine($"! ({CellsUncovered} cells uncovered, {""} flags placed)");
                return EndScreenInput();
            }

            public static bool EndScreenInput() {
                const int enterPressCountToContinue = 3;
                static void PrintKeyInfo(int enterPressCount = enterPressCountToContinue) {
                    PrintColoredPairs(new() {
                        new("\nPress "),
                        new("E ", ConsoleColor.Blue),
                        new("or "),
                        new($"{(enterPressCount == 0 ? "✓" : enterPressCount)}×ENTER ", ConsoleColor.Blue),
                        new("to "),
                        new("Play ", ConsoleColor.Blue),
                        new("again."),

                        new("\nPress "),
                        new("X ", Color: ConsoleColor.Magenta),
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

            private bool CheckShot() {
                Mine? mine = MineAt(CurPos);
                if (mine != null) {
                    mine.Uncovered = true;
                    return true;
                }

                return false;
            }

            private void CursorShotInput() {
                bool fired = false;
                while (!fired) {
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
                        case ConsoleKey.Enter or ConsoleKey.Spacebar:
                            fired = true;
                            break;
                        default:
                            break;
                    }
                }
            }

            public void UpdateTerminal() {
                //Console.Clear();
                Console.SetCursorPosition(0, 0);
                UpdateGrid(ref GameGrid, Mines, UncoveredCellsCoords);
                PrintColoredPairs(CreateGridString(GameGrid, this, CurPos));
            }

            public Mine? MineAt(Coords coords) {
                foreach (var mine in Mines) {
                    if (mine.Coordinates == coords) return mine;
                }
                return null;
            }

            public static void UpdateGrid(ref Grid grid, List<Mine> mines, List<Coords> flagsCoords) {
                grid.Reset();
                foreach (var mine in mines) {
                    Coords mineCoords = mine.Coordinates;
                    bool uncovered = mine.Uncovered;
                    GridCellDisplayType cellDisplayType = uncovered ? GridCellDisplayType.Uncovered : GridCellDisplayType.Covered;
                    grid[mineCoords.Y, mineCoords.X] = new(cellDisplayType);
                    foreach (var flagCoords in flagsCoords) {
                        grid[flagCoords.Y, flagCoords.X] = new(GridCellDisplayType.Flag);
                    }
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
                    public ConsoleColor Color;
                    public GridCellDisplayData(int? number = null, ConsoleColor color = ConsoleColor.White) {
                        Number = number;
                        Color = color;
                    }
                }
            }

            public static void ShowInfoScreen(List<StringColorPair> stringColorPairs) {
                Console.Clear();
                PrintColoredPairs(stringColorPairs);
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
                ShowInfoScreen(new List<StringColorPair>() { new(text) });
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
                public int GetLength(int dimension) => _grid.GetLength(dimension);
            }
        }

        public static void PrintColoredPairs(List<StringColorPair> colorStringPairs) {
            foreach (var colorStringPair in colorStringPairs) {
                Console.ForegroundColor = colorStringPair.Color;
                Console.Write(colorStringPair.Str);
            }
            //Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
        }

        public static List<StringColorPair> CreateGridString(Grid grid, MinesweeperGame game, Coords cursor) {
            List<StringColorPair> output = new();
            for (int y = 0; y < grid.GetLength(0); y++) {
                for (int x = 0; x < grid.GetLength(1); x++) {
                    GridCell gridItem = grid[y, x];
                    gridItem.Data.Color = ConsoleColor.White;

                    // show the mines if the game has ended
                    if ((game.MineAt(new(x, y)) != null) && game.GameEnd) {
                        gridItem.Type = GridCellDisplayType.Mine;
                    }

                    // draw cursor
                    if (cursor.X == x && cursor.Y == y) {
                        gridItem.Data.Color = ConsoleColor.DarkYellow;
                    }

                    var itemStr = GridCellDisplayTypeStringDict[gridItem.Type];
                    output.Add(new(itemStr, gridItem.Data.Color));
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
                if (i == loopCount - 1) output.Insert(i + 1, new("\n"));
                if (loopingAlphabet.StartsWith('A')) alphabetFullLoopCount++;
            }

            // add the vertical border grid (1, 2, 3, ...)
            int line = 0;
            int numbersFullLoopCount = -1;
            for (int i = 0; i < output.Count; i++) {
                if (output[i].Str == "\n") {
                    if (i < output.Count - 1) {
                        int displayNum = (line % 9) + 1;
                        if (displayNum == 1) numbersFullLoopCount++;
                        char spaceAfterNumber = ' ';
                        if (numbersFullLoopCount == 1) spaceAfterNumber = '\'';
                        if (numbersFullLoopCount > 1) spaceAfterNumber = '"';
                        output.Insert(i + 1,
                            new(displayNum.ToString() + spaceAfterNumber,
                            line == cursor.Y ? currentPosColor : ConsoleColor.Gray)
                        );
                    }
                    line++;
                }
            }
            output[0] = new("  " + output[0].Str, output[0].Color);

            if (output.Last().Str == "\n") output.RemoveAt(output.Count - 1);
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

            public bool Uncovered = false;

            //public static Dictionary<GridCellDisplayType, StringColorPair> GridCellValueSymbolColorDict = new(){
            //    { GridCellDisplayType.None, new("▀ ", ConsoleColor.DarkGray)},
            //    { GridCellDisplayType.Ship, new("▀ ", ConsoleColor.DarkCyan)},
            //    { GridCellDisplayType.ShotShip, new("▀ ", ConsoleColor.Green)},
            //    { GridCellDisplayType.Cursor, new("▀ ", ConsoleColor.DarkYellow)},
            //    { GridCellDisplayType.Missed, new("▀ ", ConsoleColor.Red)}
            //};

            public static Dictionary<GridCellDisplayType, string> GridCellDisplayTypeStringDict = new() {
                [GridCellDisplayType.Flag] = "⚑ ",
                [GridCellDisplayType.Mine] = "💣 ",
                [GridCellDisplayType.Covered] = "■ " // □
            };
        }

        public record struct Coords(int X, int Y);
        public record struct StringColorPair(string Str, ConsoleColor Color = ConsoleColor.White);

        static void JumpToPrevLineClear(int lineCount = 1) {
            foreach (int _ in Enumerable.Range(0, lineCount)) {
                Console.CursorTop--;
                Console.CursorLeft = 0;
                Console.Write(new string(' ', Console.BufferWidth - 1));
                Console.CursorLeft = 0;
            }
        }

        private static void ClearConsoleKeyInput() {
            if (Console.KeyAvailable) Console.ReadKey(true);
        }
    }
}
