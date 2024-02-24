using System.Runtime.InteropServices;
using System.Text;
using static System.ConsoleColor;
using static terminal_minesweeper.Utils;

namespace terminal_minesweeper {
    internal static partial class Program {
        private static void Main() {
            Console.OutputEncoding = Encoding.UTF8;
#if _WINDOWS
            EnableVirtualTerminalProcessing();
#endif
            Console.Title = $"{Consts.Name} @{Consts.Version}";
            var defaultBackgroundColor = Console.BackgroundColor;

            Console.Write(@"Welcome!

### Controls:
 - WASD or ↑←↓→ to move the cursor
 - ENTER or SPACE to reveal what's under the cursor position
 - E or F to flag/unflag the cell on the cursor position as a mine (only visual, doesn't change any functionality)

### How to win:
 - Uncover all the cells that have no mine without triggering any mine

");

            const int defaultGameSize = 10;
            Coords gameSize = new() {
                X = NumInput(new StringColorDataList(Gray, "↔️ Enter game ", ("width  ", White), "(press enter for default): "), defaultBackgroundColor,
                    "", defaultGameSize, 3),
                Y = NumInput(new StringColorDataList(Gray, "↕️ Enter game ", ("height ", White), "(press enter for default): "), defaultBackgroundColor,
                    "", defaultGameSize, 3)
            };
            int? mineCount = NumInput(new StringColorDataList(Gray, "💣 Enter ", ("mine count  ", White), "(press enter for default): "),
                defaultBackgroundColor, "", -1, 1);
            mineCount = mineCount == -1 ? null : mineCount;

            Console.CursorVisible = false;

            MinesweeperGame game = new(gameSize, defaultBackgroundColor, mineCount);

            Console.CancelKeyPress += (_, _) => {
                Console.CursorVisible = true;
            };

            while (true) {
                if (game.Loop()) break;
            }
            Console.CursorVisible = true;
        }

        private static class Consts {
            public const string Name = "terminal-minesweeper";
            public const string Version = "v1.0.7";
            public const string CheatCode = "cheat";
        }

        public record struct Coords(int X, int Y);

#if _WINDOWS
        // Import kernel32.dll for SetConsoleMode
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        // Constants for standard input, output, and error devices
        // ReSharper disable InconsistentNaming
        private const int STD_OUTPUT_HANDLE = -11;

        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
        // ReSharper restore InconsistentNaming

        private static void EnableVirtualTerminalProcessing() {
            var hOut = GetStdHandle(STD_OUTPUT_HANDLE);
            if (!GetConsoleMode(hOut, out var mode)) return;
            mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
            SetConsoleMode(hOut, mode);
        }
#endif
    }
}
