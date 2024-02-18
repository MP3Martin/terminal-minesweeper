namespace terminal_minesweeper {
    internal static class Utils {
        public static string ConsoleColorToAnsi(ConsoleColor color, bool isBackground) {
            return color switch {
                ConsoleColor.Black => isBackground ? "\u001B[40m" : "\u001B[30m",
                ConsoleColor.Blue => isBackground ? "\u001B[104m" : "\u001B[94m",
                ConsoleColor.Cyan => isBackground ? "\u001B[106m" : "\u001B[96m",
                ConsoleColor.DarkBlue => isBackground ? "\u001B[44m" : "\u001B[34m",
                ConsoleColor.DarkCyan => isBackground ? "\u001B[46m" : "\u001B[36m",
                ConsoleColor.DarkGray => isBackground ? "\u001B[100m" : "\u001B[90m",
                ConsoleColor.DarkGreen => isBackground ? "\u001B[42m" : "\u001B[32m",
                ConsoleColor.DarkMagenta => isBackground ? "\u001B[45m" : "\u001B[35m",
                ConsoleColor.DarkRed => isBackground ? "\u001B[41m" : "\u001B[31m",
                ConsoleColor.DarkYellow => isBackground ? "\u001B[43m" : "\u001B[33m",
                ConsoleColor.Gray => isBackground ? "\u001B[47m" : "\u001B[37m",
                ConsoleColor.Green => isBackground ? "\u001B[102m" : "\u001B[92m",
                ConsoleColor.Magenta => isBackground ? "\u001B[105m" : "\u001B[95m",
                ConsoleColor.Red => isBackground ? "\u001B[101m" : "\u001B[91m",
                ConsoleColor.White => isBackground ? "\u001B[107m" : "\u001B[97m",
                ConsoleColor.Yellow => isBackground ? "\u001B[103m" : "\u001B[93m",
                _ => isBackground ? "\u001B[49m" : "\u001B[39m",
            };
        }
    }
}
