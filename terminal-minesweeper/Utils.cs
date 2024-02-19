using System.Text;
using static System.ConsoleColor;
using static terminal_minesweeper.Program;

namespace terminal_minesweeper {
    internal static class Utils {
        private static string ConsoleColorToAnsi(ConsoleColor color, bool isBackground) {
            return color switch {
                Black => isBackground ? "\u001B[40m" : "\u001B[30m",
                Blue => isBackground ? "\u001B[104m" : "\u001B[94m",
                Cyan => isBackground ? "\u001B[106m" : "\u001B[96m",
                DarkBlue => isBackground ? "\u001B[44m" : "\u001B[34m",
                DarkCyan => isBackground ? "\u001B[46m" : "\u001B[36m",
                DarkGray => isBackground ? "\u001B[100m" : "\u001B[90m",
                DarkGreen => isBackground ? "\u001B[42m" : "\u001B[32m",
                DarkMagenta => isBackground ? "\u001B[45m" : "\u001B[35m",
                DarkRed => isBackground ? "\u001B[41m" : "\u001B[31m",
                DarkYellow => isBackground ? "\u001B[43m" : "\u001B[33m",
                Gray => isBackground ? "\u001B[47m" : "\u001B[37m",
                Green => isBackground ? "\u001B[102m" : "\u001B[92m",
                Magenta => isBackground ? "\u001B[105m" : "\u001B[95m",
                Red => isBackground ? "\u001B[101m" : "\u001B[91m",
                White => isBackground ? "\u001B[107m" : "\u001B[97m",
                Yellow => isBackground ? "\u001B[103m" : "\u001B[93m",
                _ => isBackground ? "\u001B[49m" : "\u001B[39m"
            };
        }

        public static void JumpToPrevLineClear(int lineCount = 1) {
            foreach (var _ in Enumerable.Range(0, lineCount)) {
                Console.CursorTop--;
                Console.CursorLeft = 0;
                Console.Write(new string(' ', Console.BufferWidth - 1));
                Console.CursorLeft = 0;
            }
        }

        public static void ClearConsoleKeyInput() {
            if (Console.KeyAvailable) Console.ReadKey(true);
        }

        public static void PrintColoredStrings(StringColorData colorStringData, bool noNewLine = false, ConsoleColor? defaultBackgroundColor = null, ConsoleColor? defaultColor = null) {
            PrintColoredStrings(new List<StringColorData> { colorStringData }, noNewLine, defaultBackgroundColor, defaultColor);
        }
        public static void PrintColoredStrings(List<StringColorData> colorStringData, bool noNewLine = false, ConsoleColor? defaultBackgroundColor = null, ConsoleColor? defaultColor = null) {
            var toPrint = new StringBuilder();
            foreach (var colorStringDataItem in colorStringData) {
                var foregroundColor = colorStringDataItem.Color ?? defaultColor ?? White;
                var backgroundColor = colorStringDataItem.BgColor ?? defaultBackgroundColor ?? Console.BackgroundColor;

                toPrint.Append(ConsoleColorToAnsi(foregroundColor, false) + ConsoleColorToAnsi(backgroundColor, true) + colorStringDataItem.String + "\x1b[0m");
            }
            if (!noNewLine) toPrint.AppendLine();
            Console.Write(toPrint.ToString());
        }

        public static int NumInput(List<StringColorData> prompt, ConsoleColor defaultBackgroundColor, string? defaultInput, int? defaultOutput, int? min = null) {
            var ok = false;
            var input = 0;
            while (!ok) {
                PrintColoredStrings(prompt, true, defaultBackgroundColor);
                var readLine = Console.ReadLine() ?? "";
                ok = int.TryParse(readLine, out input);
                if (defaultInput != null && defaultOutput != null && readLine == defaultInput) return (int)defaultOutput;
                if (input < min) ok = false;
                if (!ok) JumpToPrevLineClear();
            }
            return input;
        }
    }
}
