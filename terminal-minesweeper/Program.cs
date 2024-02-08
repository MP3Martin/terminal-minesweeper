using static terminal_minesweeper.Program.Ship;
using static terminal_minesweeper.Program.ShipsGame;

namespace terminal_minesweeper {
    internal class Program {
        const string Name = "terminal-minesweeper";
        const string Version = "v1.0.0";
        static class Consts {
            public static (int, int) ShipCountRange { get; } = (2, 4);
            public static (int, int) ShipSizeXRange { get; } = (2, 4);
            public static Coords GridSize { get; } = new(10, 10);

            public const int MaxTries = 3;
        }
        static void Main(string[] args) {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Title = $"{Name} @{Version}";

            Console.Write(@"Welcome!

### Controls:
 - WASD or ↑←↓→ to move the cursor
 - ENTER or SPACE to ""click""

### How to play:
 - You have 3 attempts to shoot at least one ship

Press any key to continue . . . ");
            Console.ReadKey(true);

            Console.CursorVisible = false;
            ShipsGame game = new();
            while (true) {
                if (game.Loop()) break;
            }
        }

        public class ShipsGame {
            public Random RandomGen = new();
            private Coords _curPos;
            public Coords CurPos {
                get => _curPos;
                set {
                    if (value.Y >= Grid.GetLength(0)) value.Y = 0;
                    if (value.Y < 0) value.Y = Grid.GetLength(0) - 1;
                    if (value.X >= Grid.GetLength(1)) value.X = 0;
                    if (value.X < 0) value.X = Grid.GetLength(1) - 1;
                    _curPos = value;
                }
            }
            public GridCellValue[,] Grid = new GridCellValue[0, 0];
            public List<Ship> Ships = new();
            public List<Coords> MissedCoords = new();
            public bool GameEnd = false;
            public int AttemptsLeft;

            public bool Loop() {
                Console.Clear();
                // reset vars
                Grid = new GridCellValue[Consts.GridSize.Y, Consts.GridSize.X];
                AttemptsLeft = Consts.MaxTries;
                CurPos = new(0, 0);
                GameEnd = false;
                Ships = new();
                MissedCoords = new();

                // generate random ships
                int shipsToAddCount = RandomGen.Next(Consts.ShipCountRange.Item1, Consts.ShipCountRange.Item2 + 1);
                foreach (int i in Enumerable.Range(0, shipsToAddCount)) {
                    int shipSizeX = RandomGen.Next(Consts.ShipSizeXRange.Item1, Consts.ShipSizeXRange.Item2 + 1);
                    Ships.Add(new(shipSizeX, 1));
                }
                Ships.ForEach((ship) => { ship.Offset = GetRandomShipCoords(ship, Grid, this); });

                bool shotSuccess = false;
                while (AttemptsLeft > 0) {
                    CursorShotInput();
                    shotSuccess = CheckShot();
                    UpdateTerminal();
                    AttemptsLeft--;
                    if (!shotSuccess) {
                        MissedCoords.Add(CurPos);
                        if (AttemptsLeft <= 0) break;
                        ShowInfoScreen(new List<StringColorPair> {
                            new("You MISSED! You have ", ConsoleColor.Gray),
                            new($"{AttemptsLeft} {(AttemptsLeft == 1 ? "attempt" : "attempts")} left"),
                            new(".", ConsoleColor.Gray)
                        });
                    } else break;
                }
                GameEnd = true;
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
                int attempts = Consts.MaxTries - AttemptsLeft;
                Console.WriteLine($"! (in {attempts} {(attempts == 1 ? "attempt" : "attempts")})");
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
                bool shotSuccess = false;
                foreach (var ship in Ships) {
                    if (ship.IsAt(CurPos)) {
                        shotSuccess = true;
                        ship.Shot = true;
                    }
                }

                return shotSuccess;
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
                UpdateGrid(ref Grid, Ships, MissedCoords);
                PrintColoredPairs(CreateGridString(Grid, this, CurPos));
            }

            public Ship? ShipAt(Coords coords) {
                foreach (var ship in Ships) {
                    if (ship.CoordList.Contains(coords)) return ship;
                }
                return null;
            }

            public static void UpdateGrid(ref GridCellValue[,] grid, List<Ship> ships, List<Coords> missedCoords) {
                Array.Clear(grid);
                foreach (var ship in ships) {
                    List<Coords> shipCoords = ship.CoordList;
                    foreach (var coords in shipCoords) {
                        GridCellValue cellValue = ship.Shot ? GridCellValue.ShotShip : GridCellValue.Ship;
                        grid[coords.Y, coords.X] = cellValue;
                    }
                    foreach (var coords in missedCoords) {
                        grid[coords.Y, coords.X] = GridCellValue.Missed;
                    }
                }
            }

            public static Coords GetRandomShipCoords(in Ship ship, in GridCellValue[,] grid, ShipsGame game) {
                List<Coords> allShipCoords = new();
                foreach (var shipItem in game.Ships) {
                    allShipCoords.AddRange(shipItem.CoordList);
                }
                int tries = 0;
                Coords newShipCoords;
                while (tries < ((game.Grid.GetLength(0) + game.Grid.GetLength(1)) * 3)) {
                    newShipCoords = new(
                        game.RandomGen.Next(0, (grid.GetLength(1) - ship.SizeX) + 1),
                        game.RandomGen.Next(0, (grid.GetLength(0) - ship.SizeY) + 1)
                    );
                    Ship newShip = new(ship.SizeX, ship.SizeY, newShipCoords);
                    List<Coords> allNewShipCoords = newShip.CoordList;
                    // Return the new coords if the new ship's coordinates don't overlap with any other ship's coordinates
                    if (!allShipCoords.Intersect(allNewShipCoords).Any()) return newShipCoords;
                    tries++;
                }
                newShipCoords = new(0, 0);
                return newShipCoords;
            }

            public enum GridCellValue {
                None,
                Ship,
                ShotShip,
                Cursor,
                Missed
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

        public static List<StringColorPair> CreateGridString(GridCellValue[,] grid, ShipsGame game, Coords cursor) {
            List<StringColorPair> output = new();
            for (int y = 0; y < grid.GetLength(0); y++) {
                for (int x = 0; x < grid.GetLength(1); x++) {
                    var gridItem = grid[y, x];

                    // hide ships if the game is not finished
                    if (gridItem == GridCellValue.Ship && !game.GameEnd) {
                        gridItem = GridCellValue.None;
                    }

                    // draw cursor
                    if (cursor.X == x && cursor.Y == y) {
                        gridItem = GridCellValue.Cursor;
                    }

                    var item = GridCellValueSymbolColorDict[gridItem];
                    output.Add(new(item.Str, item.Color));
                }
                output.Add(new("\n", ConsoleColor.White));
            }

            const ConsoleColor currentPosColor = ConsoleColor.DarkCyan;

            // add the horizontal border grid (A, B, C, ...)
            string loopingAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            int alphabetFullLoopCount = 0;
            int loopCount = game.Grid.GetLength(1);
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

        public class Ship {
            public Ship(int sizeX, int sizeY, Coords? coords = null) {
                coords ??= new(0, 0);
                SizeX = sizeX;
                SizeY = sizeY;
                Offset = (Coords)coords;
            }

            public bool IsAt(Coords coords) {
                return (CoordList.Contains(coords));
            }

            public List<Coords> CoordList {
                get {
                    List<Coords> shipCoords = new();
                    for (int y = Offset.Y; y < SizeY + Offset.Y; y++) {
                        for (int x = Offset.X; x < SizeX + Offset.X; x++) {
                            shipCoords.Add(new Coords(x, y));
                        }
                    }
                    return shipCoords;
                }
            }

            public int SizeX;
            public int SizeY;
            public Coords Offset = new(0, 0);

            public bool Shot = false;

            public static Dictionary<GridCellValue, StringColorPair> GridCellValueSymbolColorDict = new(){
                { GridCellValue.None, new("▀ ", ConsoleColor.DarkGray)},
                { GridCellValue.Ship, new("▀ ", ConsoleColor.DarkCyan)},
                { GridCellValue.ShotShip, new("▀ ", ConsoleColor.Green)},
                { GridCellValue.Cursor, new("▀ ", ConsoleColor.DarkYellow)},
                { GridCellValue.Missed, new("▀ ", ConsoleColor.Red)}
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
