namespace terminal_minesweeper {
    internal static partial class Program {
        private static class ConsoleResize {
            private static Coords? _previousSize;
            private static Coords? _currentSize;
            private static Coords? CurrentSize {
                get => _currentSize;
                set {
                    _previousSize = _currentSize;
                    _currentSize = value;
                }
            }
            public static bool CheckResized() {
                CurrentSize = new Coords(Console.BufferWidth, Console.BufferHeight);
                return CurrentSize != _previousSize;
            }
        }
    }
}
