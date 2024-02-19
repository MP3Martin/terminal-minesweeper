namespace terminal_minesweeper {
    internal static partial class Program {
        public class Mine {
            public Coords Coordinates;
            public Mine(Coords? coords = null) {
                coords ??= new(0, 0);
                Coordinates = (Coords)coords;
            }

            public bool IsAt(Coords coords) {
                return Coordinates == coords;
            }
        }
    }
}
