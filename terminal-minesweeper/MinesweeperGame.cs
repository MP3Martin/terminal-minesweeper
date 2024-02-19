using static terminal_minesweeper.Utils;
using static terminal_minesweeper.Program.MinesweeperGame.GridCell;

namespace terminal_minesweeper {
    internal static partial class Program {
        public partial class MinesweeperGame {
            private readonly ConsoleColor _defaultBackgroundColor;
            private readonly Coords _gridSize;
            private readonly int? _mineCount;
            private readonly Random _randomGen = new();
            private Dictionary<Coords, Mine> _coordsMinesMap = new();
            private Coords _curPos;
            private HashSet<Coords> _flaggedCellsCoords = new();
            private bool _gameEnd;
            private Grid _gameGrid = new(0, 0);
            private bool _cheated;
            private bool _cheatMode;
            private string _cheatModeTyping = "";
            private int _manuallyUncoveredCells;
            private List<Mine> _mines = new();
            private HashSet<Coords> _uncoveredCellsCoords = new();

            public MinesweeperGame(Coords size, ConsoleColor defaultBackgroundColor, int? mineCount = null) {
                _gridSize = size;
                _mineCount = mineCount;

                _mineCount ??= Math.Max(1, (int)(_gridSize.X * _gridSize.Y * 0.15));
                _mineCount = Math.Min((int)_mineCount, _gridSize.X * _gridSize.Y);

                _defaultBackgroundColor = defaultBackgroundColor;
            }
            private Coords CurPos {
                get => _curPos;
                set {
                    if (value.Y >= _gridSize.Y) value.Y = 0;
                    if (value.Y < 0) value.Y = _gridSize.Y - 1;
                    if (value.X >= _gridSize.X) value.X = 0;
                    if (value.X < 0) value.X = _gridSize.X - 1;
                    _curPos = value;
                }
            }

            /// <returns>If the user requested the game to exit</returns>
            public bool Loop() {
                Console.Clear();
                Console.WriteLine("Generating...");
                // reset vars
                _coordsMinesMap = new();
                CurPos = new(0, 0);
                _flaggedCellsCoords = new();
                _gameEnd = false;
                _gameGrid = new(_gridSize.Y, _gridSize.X);
                _cheated = false;
                _cheatMode = false;
                _cheatModeTyping = "";
                _manuallyUncoveredCells = 0;
                _mines = new();
                _uncoveredCellsCoords = new();

                // generate random mines
                do {
                    foreach (var _ in Enumerable.Range(0, _mineCount ?? 1)) {
                        _mines.Add(new());
                    }
                    _mines.ForEach(mine => { mine.Coordinates = GetRandomMineCoords(); });
                    _mines.RemoveAll(mine => mine.Coordinates == new Coords(-1, -1));
                } while (_mines.Count < 1);

                _coordsMinesMap = _mines.ToDictionary(item => item.Coordinates, item => item);

                // calculate neighbour mine count for every cell
                RecalculateCellNumbers();

                bool gameWon;
                while (true) {
                    UpdateTerminal();
                    UserInput();
                    UncoverCell(CurPos);
                    if (IsMineAt(CurPos)) {
                        gameWon = false;
                        break;
                    }
                    // ReSharper disable once InvertIf
                    if (_uncoveredCellsCoords.Count + _mines.Count == _gridSize.X * _gridSize.Y) {
                        gameWon = true;
                        if (_uncoveredCellsCoords.Any(IsMineAt)) gameWon = false;
                        break;
                    }
                }
                _gameEnd = true;
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
                Console.WriteLine($"! ({_manuallyUncoveredCells} cells uncovered, " +
                    $"{_flaggedCellsCoords.Count} flags placed, " +
                    $"{_mines.Count(mine => _flaggedCellsCoords.Contains(mine.Coordinates))} " +
                    $"out of {_mines.Count} mines were flagged)"
                );
                if (_cheated) {
                    PrintColoredStrings(new StringColorData("⚠️ Warning: Cheat mode was activated at least once while playing this round! ⚠️", ConsoleColor.DarkYellow));
                }
                return EndScreenInput();
            }

            private void UncoverCell(Coords coords) {
                _flaggedCellsCoords.Remove(coords);
                _uncoveredCellsCoords.Add(coords);
            }

            private void RecalculateCellNumbers() {
                for (var y = 0; y < _gridSize.Y; y++) {
                    for (var x = 0; x < _gridSize.X; x++) {
                        if (IsMineAt(new(x, y))) continue;
                        var cellsAround = GetCellsAround(new(x, y));
                        var mineCountAround = cellsAround.Count(IsMineAt);
                        _gameGrid[y, x].Data.Number = mineCountAround;
                    }

                }

                // Remove removed mines from MinesAtCoords
                HashSet<Coords> allMineCoords = new(_mines.Select(item => item.Coordinates));
                foreach (var coords in _coordsMinesMap.Keys.ToList()) {
                    if (!allMineCoords.Contains(coords)) {
                        _coordsMinesMap.Remove(coords);
                    }
                }
            }

            private bool EndScreenInput() {
                const int enterPressCountToContinue = 3;
                void PrintKeyInfo(int enterPressCount = enterPressCountToContinue) {
                    PrintColoredStrings(new StringColorDataList(
                        "\nPress ",
                        new StringColorData($"{(enterPressCount == 0 ? "✓" : enterPressCount)}×ENTER", ConsoleColor.Blue),
                        "/",
                        new StringColorData("SPACE ", ConsoleColor.Blue),
                        "to ",
                        new StringColorData("Play ", ConsoleColor.Blue),
                        "again.",
                        "\nPress ",
                        new StringColorData("X ", ConsoleColor.Magenta),
                        "to ",
                        new StringColorData("Exit", ConsoleColor.Magenta),
                        "."
                    ), defaultBackgroundColor: _defaultBackgroundColor);
                }
                var enterPressCount = 0;
                PrintKeyInfo();
                while (true) {
                    var key = Console.ReadKey(true).Key;
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
                    }
                }
            }

            private void UserInput() {
                var uncovered = false;
                while (!uncovered) {
                    UpdateTerminal();
                    var input = Console.ReadKey(true);

                    // cheat mode
                    var inputChar = input.KeyChar;
                    _cheatMode = false;
                    _cheatModeTyping += char.ToLower(inputChar);
                    if (Consts.CheatCode.StartsWith(_cheatModeTyping, StringComparison.Ordinal)) {
                        if (_cheatModeTyping == Consts.CheatCode) {
                            _cheatMode = true;
                            _cheated = true;
                            _cheatModeTyping = "";
                            UpdateTerminal();
                        }
                        continue; // ignore the key press if it was a valid next cheat char
                    }

                    _cheatModeTyping = "";

                    var key = input.Key;
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
                            if (_uncoveredCellsCoords.Contains(CurPos)) break;

                            // toggle flagged
                            if (!_flaggedCellsCoords.Add(CurPos)) {
                                _flaggedCellsCoords.Remove(CurPos);
                            }

                            break;
                        case ConsoleKey.Enter or ConsoleKey.Spacebar:
                            // do nothing if the cell is already uncovered or the cell is flagged
                            if (_flaggedCellsCoords.Contains(CurPos) || _uncoveredCellsCoords.Contains(CurPos)) break;

                            // move/remove the mine if it is the first thing the user reveals
                            var mineAtCurPos = GetMineAt(CurPos);
                            if (_uncoveredCellsCoords.Count == 0 && mineAtCurPos != null) {
                                if (_mines.Count > 1) {
                                    _mines.Remove(mineAtCurPos);
                                } else {
                                    mineAtCurPos.Coordinates = GetRandomMineCoords();
                                }
                                RecalculateCellNumbers();
                            }

                            // auto-reveal
                            AutoReveal(CurPos);

                            _manuallyUncoveredCells++;
                            uncovered = true;
                            break;
                    }
                }
            }

            private void AutoReveal(Coords coords) {
                HashSet<Coords> visitedCells = new();
                var recursionDepth = 0;

                RecursiveAutoReveal(coords);
                return;

                void RecursiveAutoReveal(Coords currentCoords) {
                    if (visitedCells.Contains(currentCoords)) return; // skip if already visited
                    if (recursionDepth > 5000) return; // prevent stack overflow
                    visitedCells.Add(currentCoords);

                    UncoverCell(currentCoords);

                    // do not recursively reveal neighbours if the current cell has at least one neighbouring mine
                    if (_gameGrid[currentCoords].Data.Number > 0) return;
                    var neighbours = GetCellsAround(currentCoords);
                    foreach (var neighbour in neighbours) {
                        recursionDepth++;
                        RecursiveAutoReveal(neighbour);
                        recursionDepth--;
                    }
                }
            }

            private void UpdateTerminal() {
                if (ConsoleResize.CheckResized()) Console.Clear();
                Console.SetCursorPosition(0, 0);
                UpdateGrid(ref _gameGrid, _flaggedCellsCoords, _uncoveredCellsCoords);
                PrintColoredStrings(CreateGridString(), defaultBackgroundColor: _defaultBackgroundColor);
            }

            private Mine? GetMineAt(Coords coords) {
                _coordsMinesMap.TryGetValue(coords, out var result);
                return result;
            }
            private bool IsMineAt(Coords coords) {
                return GetMineAt(coords) != null;
            }

            private static void UpdateGrid(ref Grid grid, HashSet<Coords> flagsCoords, HashSet<Coords> uncoveredCoords) {
                for (var y = 0; y < grid.GetLength(0); y++) {
                    for (var x = 0; x < grid.GetLength(1); x++) {
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
                var allMineCoords = _mines.Select(item => item.Coordinates).ToHashSet();
                var tries = 0;
                while (tries < (_gridSize.Y + _gridSize.X) * 3) {
                    var newMineCoords = new Coords(
                        _randomGen.Next(0, _gridSize.X),
                        _randomGen.Next(0, _gridSize.Y)
                    );
                    if (!allMineCoords.Contains(newMineCoords)) return newMineCoords;
                    tries++;
                }
                return new(-1, -1);
            }

            private HashSet<Coords> GetCellsAround(Coords offset) {
                HashSet<Coords> result = new();

                // generate square of coords
                for (var y = -1; y <= 1; y++) {
                    for (var x = -1; x <= 1; x++) {
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
                result.RemoveWhere(item => item.X >= _gridSize.X || item.Y >= _gridSize.Y || item.X < 0 || item.Y < 0);

                return result;
            }

            private List<StringColorData> CreateGridString() {
                List<StringColorData> output = new();
                for (var y = 0; y < _gridSize.Y; y++) {
                    for (var x = 0; x < _gridSize.X; x++) {
                        var gridItem = _gameGrid[y, x];
                        StringColorData stringColorData = new("", ConsoleColor.White);

                        if (y == 0) stringColorData.Data.CellTop = true;
                        if (x == 0) {
                            stringColorData.Data.CellLeft = true;
                        }

                        // show the mines if the game has ended
                        if (IsMineAt(new(x, y)) && (_gameEnd || _uncoveredCellsCoords.Contains(new(x, y)) || _cheatMode)) {
                            gridItem.Type = GridCellDisplayType.Mine;
                        }

                        // draw cursor
                        if (CurPos.X == x && CurPos.Y == y) {
                            stringColorData.BgColor = ConsoleColor.DarkGray;
                        }

                        // update stringColorData with data from gridItem
                        GridCellDisplayTypeStringDict.TryGetValue(gridItem.Type, out var itemStr);
                        stringColorData.String = itemStr ?? "";

                        // draw numbers
                        if (gridItem.Type == GridCellDisplayType.Uncovered) {
                            var number = gridItem.Data.Number;
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
                var loopingAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var alphabetFullLoopCount = 0;
                var topCells = output.Where(item => item.Data.CellTop != null && (bool)item.Data.CellTop).ToList();
                var currentX = 0;
                foreach (var _ in topCells) {
                    StringColorData toAdd = new(loopingAlphabet[0] + (alphabetFullLoopCount == 0 ? " " : "'")) {
                        Color = CurPos.X == currentX ? currentPosColor : ConsoleColor.Gray
                    };

                    output.Insert(currentX, toAdd);

                    loopingAlphabet = loopingAlphabet[1..] + loopingAlphabet[0];
                    if (loopingAlphabet.StartsWith('A')) alphabetFullLoopCount++;
                    currentX++;
                }
                for (var i = 0; i < 2; i++) {
                    output.Insert(topCells.Count + i, "\n");
                }
            }

            private void AddVerticalBorder(ref List<StringColorData> output, ConsoleColor currentPosColor) {
                var line = 0;
                var numbersFullLoopCount = -1;
                var leftCells = output.Where(item => item.Data.CellLeft != null && (bool)item.Data.CellLeft).ToList();
                foreach (var leftCell in leftCells) {
                    var displayNum = line % 9 + 1;
                    if (displayNum == 1) numbersFullLoopCount++;
                    var spaceAfterNumber = numbersFullLoopCount switch {
                        1 => "'",
                        > 1 => "\"",
                        _ => ""
                    };
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
    }
}
