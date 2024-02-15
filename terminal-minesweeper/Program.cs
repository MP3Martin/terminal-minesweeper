using static terminal_minesweeper.Program.Mine;
using static terminal_minesweeper.Program.MinesweeperGame.GridCell;

namespace terminal_minesweeper {
    internal class Program {
        static class Consts {
            public const string CheatCode = "cheat";
        }
        const string Name = "terminal-minesweeper";
        const string Version = "v1.0.2";
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

");

            Coords gameSize = new() {
                X = NumInput("Enter game width (press enter for default): ", "", 10, 3),
                Y = NumInput("Enter game height (press enter for default): ", "", 10, 3)
            };

            Console.CursorVisible = false;
            MinesweeperGame game = new(gameSize);

            Console.CancelKeyPress += (_, _) => {
                Console.CursorVisible = true;
            };

            while (true) {
                if (game.Loop()) break;
            }
            Console.CursorVisible = true;
        }

        public class MinesweeperGame {
            private readonly Random RandomGen = new();
            private Coords _curPos;
            private Coords CurPos {
                get => _curPos;
                set {
                    if (value.Y >= GridSize.Y) value.Y = 0;
                    if (value.Y < 0) value.Y = GridSize.Y - 1;
                    if (value.X >= GridSize.X) value.X = 0;
                    if (value.X < 0) value.X = GridSize.X - 1;
                    _curPos = value;
                }
            }
            private Grid GameGrid = new(0, 0);
            private Coords GridSize = new(0, 0);
            private List<Mine> Mines = new();
            private HashSet<Coords> FlaggedCellsCoords = new();
            private HashSet<Coords> UncoveredCellsCoords = new();
            private int ManuallyUncoveredCells = 0;
            private bool GameEnd = false;
            private bool CheatMode = false;
            private string CheatModeTyping = "";
            private bool Cheated = false;

            public MinesweeperGame(Coords size) {
                GridSize = size;
            }

            /// <returns>If the user requested the game to exit</returns>
            public bool Loop() {
                Console.Clear();
                // reset vars
                CurPos = new(0, 0);
                GameGrid = new(GridSize.Y, GridSize.X);
                Mines = new();
                FlaggedCellsCoords = new();
                UncoveredCellsCoords = new();
                ManuallyUncoveredCells = 0;
                GameEnd = false;
                CheatMode = false;
                CheatModeTyping = "";
                Cheated = false;

                // generate random mines
                int minesToAddCount = Math.Max(0, (int)(GridSize.X * GridSize.Y * 0.15));
                do {
                    foreach (int i in Enumerable.Range(0, minesToAddCount)) {
                        Mines.Add(new Mine());
                    }
                    Mines.ForEach((mine) => { mine.Coordinates = GetRandomMineCoords(); });
                    Mines.RemoveAll(mine => mine.Coordinates == new Coords(-1, -1));
                } while (Mines.Count < 1);

                // calculate neighbour mine count for every cell
                RecalculateCellNumbers();

                bool gameWon = false;
                while (!GameEnd) {
                    UpdateTerminal();
                    UserInput();
                    UncoverCell(CurPos);
                    if (MineAt(CurPos) != null) {
                        GameEnd = true;
                        gameWon = false;
                    }
                    if (UncoveredCellsCoords.Count + Mines.Count == GridSize.X * GridSize.Y) {
                        GameEnd = true;
                        gameWon = true;
                        if (UncoveredCellsCoords.Any(coords => MineAt(coords) != null)) gameWon = false;
                    }
                }
                UpdateTerminal();
                Thread.Sleep(300);
                ClearConsoleKeyInput();
                // hide the cursor
                _curPos = new(-1, -1);
                UpdateTerminal();

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("\nYou ");
                if (gameWon) {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("WIN");
                } else {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("LOSE");
                }
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"! ({ManuallyUncoveredCells} cells uncovered, " +
                    $"{FlaggedCellsCoords.Count} flags placed, " +
                    $"{Mines.Count(mine => FlaggedCellsCoords.Contains(mine.Coordinates))} " +
                    $"out of {Mines.Count} mines were flagged)"
                );
                if (Cheated) {
                    PrintColoredStrings(new(){
                        new("⚠️ Warning: Cheat mode was activated at least once while playing this round! ⚠️", ConsoleColor.DarkYellow)
                    });
                }
                return EndScreenInput();
            }

            private void UncoverCell(Coords coords) {
                FlaggedCellsCoords.Remove(coords);
                UncoveredCellsCoords.Add(coords);
            }

            private void RecalculateCellNumbers() {
                for (int y = 0; y < GridSize.Y; y++) {
                    for (int x = 0; x < GridSize.X; x++) {
                        if (MineAt(new(x, y)) != null) continue;
                        var cellsAround = GetCellsAround(new(x, y));
                        int mineCountAround = cellsAround.Where(cell => MineAt(cell) != null).Count();
                        GameGrid[y, x].Data.Number = mineCountAround;
                    }

                }
            }

            private static bool EndScreenInput() {
                const int enterPressCountToContinue = 3;
                static void PrintKeyInfo(int enterPressCount = enterPressCountToContinue) {
                    PrintColoredStrings(new() {
                        "\nPress ",
                        new($"{(enterPressCount == 0 ? "✓" : enterPressCount)}×ENTER", ConsoleColor.Blue),
                        "/",
                        new("SPACE ", ConsoleColor.Blue),
                        "to ",
                        new("Play ", ConsoleColor.Blue),
                        "again.",

                        "\nPress ",
                        new("X ", ConsoleColor.Magenta),
                        "to ",
                        new("Exit", ConsoleColor.Magenta),
                        "."
                    });
                }
                int enterPressCount = 0;
                PrintKeyInfo();
                while (true) {
                    ConsoleKey key = Console.ReadKey(true).Key;
                    switch (key) {
                        case ConsoleKey.X:
                            return true;
                        case ConsoleKey.Enter or ConsoleKey.Spacebar:
                            enterPressCount++;
                            JumpToPrevLineClear(3);
                            PrintKeyInfo(enterPressCountToContinue - enterPressCount);
                            if (enterPressCount >= enterPressCountToContinue) {
                                Thread.Sleep(350);
                                ClearConsoleKeyInput();
                                return false;
                            }
                            break;
                        default:
                            break;
                    }
                }
            }

            private void UserInput() {
                bool uncovered = false;
                while (!uncovered) {
                    UpdateTerminal();
                    ConsoleKeyInfo input = Console.ReadKey(true);

                    // cheat mode
                    char inputChar = input.KeyChar;
                    CheatMode = false;
                    CheatModeTyping += char.ToLower(inputChar);
                    if (Consts.CheatCode.StartsWith(CheatModeTyping, StringComparison.Ordinal)) {
                        if (CheatModeTyping == Consts.CheatCode) {
                            CheatMode = true;
                            Cheated = true;
                            CheatModeTyping = "";
                            UpdateTerminal();
                        }
                        continue; // ignore the key press if it was a valid next cheat char
                    } else {
                        CheatModeTyping = "";
                    }

                    ConsoleKey key = input.Key;
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
                            // do nothing if the cell is already uncovered or the cell is flagged
                            if (FlaggedCellsCoords.Contains(CurPos) || UncoveredCellsCoords.Contains(CurPos)) break;

                            // move/remove the mine if it is the first thing the user reveals
                            Mine? mineAtCurPos = MineAt(CurPos);
                            if (UncoveredCellsCoords.Count == 0 && mineAtCurPos != null) {
                                if (Mines.Count > 1) {
                                    Mines.Remove(mineAtCurPos);
                                } else {
                                    mineAtCurPos.Coordinates = GetRandomMineCoords();
                                }
                                RecalculateCellNumbers();
                            }

                            // auto-reveal
                            AutoReveal(CurPos);

                            ManuallyUncoveredCells++;
                            uncovered = true;
                            break;
                        default:
                            break;
                    }
                }
            }

            private void AutoReveal(Coords coords) {
                HashSet<Coords> visitedCells = new();

                void RecursiveAutoReveal(Coords currentCoords) {
                    if (visitedCells.Contains(currentCoords)) return; // skip if already visited
                    visitedCells.Add(currentCoords);

                    UncoverCell(currentCoords);

                    // do not recursively reveal neighbours if the current cell has at least one neighbouring mine
                    if (GameGrid[currentCoords].Data.Number > 0) return;
                    HashSet<Coords> neighbours = GetCellsAround(currentCoords);
                    foreach (var neighbour in neighbours) {
                        RecursiveAutoReveal(neighbour);
                    }
                }

                RecursiveAutoReveal(coords);
            }

            private void UpdateTerminal() {
                if (ConsoleResize.CheckResized()) Console.Clear();
                Console.SetCursorPosition(0, 0);
                UpdateGrid(ref GameGrid, FlaggedCellsCoords, UncoveredCellsCoords);
                PrintColoredStrings(CreateGridString());
            }

            public Mine? MineAt(Coords coords) {
                return Mines.FirstOrDefault(mine => mine.Coordinates == coords);
            }

            private static void UpdateGrid(ref Grid grid, HashSet<Coords> flagsCoords, HashSet<Coords> uncoveredCoords) {
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

            private Coords GetRandomMineCoords() {
                HashSet<Coords> allMineCoords = Mines.Select(item => item.Coordinates).ToHashSet();
                int tries = 0;
                Coords newMineCoords;
                while (tries < ((GridSize.Y + GridSize.X) * 3)) {
                    newMineCoords = new(
                        RandomGen.Next(0, GridSize.X),
                        RandomGen.Next(0, GridSize.Y)
                    );
                    if (!allMineCoords.Contains(newMineCoords)) return newMineCoords;
                    tries++;
                }
                return new(-1, -1);
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

            private HashSet<Coords> GetCellsAround(Coords offset) {
                HashSet<Coords> result = new();

                // generate square of coords
                for (int y = -1; y <= 1; y++) {
                    for (int x = -1; x <= 1; x++) {
                        Coords toAdd = new(x, y);
                        result.Add(toAdd);
                    }
                }

                // remove the center
                result.Remove(new(0, 0));

                result = result.Select(item => {
                    item.X += offset.X;
                    item.Y += offset.Y;
                    return item;
                }).ToHashSet();

                // remove coords that are outside of the grid
                result.RemoveWhere(item => item.X >= GridSize.X || item.Y >= GridSize.Y || item.X < 0 || item.Y < 0);

                return result;
            }

            private List<StringColorData> CreateGridString() {
                List<StringColorData> output = new();
                for (int y = 0; y < GridSize.Y; y++) {
                    for (int x = 0; x < GridSize.X; x++) {
                        GridCell gridItem = GameGrid[y, x];
                        StringColorData stringColorData = new("", ConsoleColor.White);

                        if (y == 0) stringColorData.Data.CellTop = true;
                        if (x == 0) {
                            stringColorData.Data.CellLeft = true;
                            stringColorData.Data.UniqueY = y;
                        }

                        // show the mines if the game has ended
                        if ((MineAt(new(x, y)) != null) && (GameEnd || UncoveredCellsCoords.Contains(new(x, y)) || CheatMode)) {
                            gridItem.Type = GridCellDisplayType.Mine;
                        }

                        // draw cursor
                        if (CurPos.X == x && CurPos.Y == y) {
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

                // add the horizontal grid border (A, B, C, ...)
                AddHorizontalBorder(ref output, currentPosColor);

                // add the vertical grid border (1, 2, 3, ...)
                AddVerticalBorder(ref output, currentPosColor);

                if (output.Last().String == "\n") output.RemoveAt(output.Count - 1);
                return output;
            }

            private void AddHorizontalBorder(ref List<StringColorData> output, ConsoleColor currentPosColor) {
                string loopingAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                int alphabetFullLoopCount = 0;
                List<StringColorData> topCells = output.Where(item => item.Data.CellTop != null && (bool)item.Data.CellTop).ToList();
                int currentX = 0;
                foreach (var topCell in topCells) {
                    StringColorData toAdd = new(loopingAlphabet[0] + (alphabetFullLoopCount == 0 ? " " : "'")) {
                        Color = CurPos.X == currentX ? currentPosColor : ConsoleColor.Gray
                    };

                    output.Insert(currentX, toAdd);

                    loopingAlphabet = loopingAlphabet[1..] + loopingAlphabet[0];
                    if (loopingAlphabet.StartsWith('A')) alphabetFullLoopCount++;
                    currentX++;
                }
                for (int i = 0; i < 2; i++) {
                    output.Insert(topCells.Count + i, "\n");
                }
            }

            private void AddVerticalBorder(ref List<StringColorData> output, ConsoleColor currentPosColor) {
                int line = 0;
                int numbersFullLoopCount = -1;
                List<StringColorData> leftCells = output.Where(item => item.Data.CellLeft != null && (bool)item.Data.CellLeft).ToList();
                foreach (var leftCell in leftCells) {
                    int displayNum = (line % 9) + 1;
                    if (displayNum == 1) numbersFullLoopCount++;
                    string spaceAfterNumber = "";
                    if (numbersFullLoopCount == 1) spaceAfterNumber = "'";
                    if (numbersFullLoopCount > 1) spaceAfterNumber = "\"";
                    if (numbersFullLoopCount > 2) spaceAfterNumber = "\"'";
                    spaceAfterNumber += new string(' ', 3 - spaceAfterNumber.Length);

                    StringColorData toAdd = new(displayNum + spaceAfterNumber) {
                        Color = CurPos.Y == line ? currentPosColor : ConsoleColor.Gray
                    };

                    var leftCellIndex = output.IndexOf(leftCell);

                    output.Insert(leftCellIndex, toAdd);

                    line++;
                }
                output.Insert(0, "    ");
            }
        }

        private static class ConsoleResize {
            private static Coords? previousSize = null;
            private static Coords? _currentSize = null;
            private static Coords? CurrentSize {
                get => _currentSize;
                set {
                    previousSize = _currentSize;
                    _currentSize = value;
                }
            }
            public static bool CheckResized() {
                CurrentSize = new(Console.BufferWidth, Console.BufferHeight);
                return CurrentSize != previousSize;
            }
        }

        private static void PrintColoredStrings(List<StringColorData> colorStringData) {
            foreach (var colorStringPair in colorStringData) {
                Console.ForegroundColor = colorStringPair.Color;
                Console.BackgroundColor = colorStringPair.BGColor ?? default;
                Console.Write(colorStringPair.String);
            }
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
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
                [GridCellDisplayType.Covered] = "■ "
            };
        }

        public record struct Coords(int X, int Y);
        private class StringColorData {
            public string String = "";
            public ConsoleColor Color;
            public ConsoleColor? BGColor = null;
            public AdditionalData Data = new();

            public StringColorData(string str, ConsoleColor color = ConsoleColor.White, ConsoleColor? bgColor = null, AdditionalData? data = null) {
                String = str;
                Color = color;
                BGColor = bgColor ?? BGColor;
                Data = data ?? Data;
            }

            public class AdditionalData {
                public bool? CellLeft;
                public bool? CellTop;
                public int? UniqueY;
            }

            public static implicit operator StringColorData(string str) {
                return new(str);
            }
        }

        private static void JumpToPrevLineClear(int lineCount = 1) {
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

        private static int NumInput(string prompt, string? defaultInput, int? defaultOutput, int? min = null) {
            bool ok = false;
            int input = 0;
            while (!ok) {
                Console.Write(prompt);
                string readLine = Console.ReadLine() ?? "";
                ok = int.TryParse(readLine, out input);
                if (defaultInput != null && defaultOutput != null && readLine == defaultInput) return (int)defaultOutput;
                if (min != null && input < min) ok = false;
                if (!ok) JumpToPrevLineClear();
            }
            return input;
        }
    }
}
