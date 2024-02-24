namespace terminal_minesweeper {
    internal static partial class Program {
        public partial class MinesweeperGame {
            private class Grid {
                private GridCell[,] _grid;
                public Grid(int sizeY, int sizeX) {
                    _grid = new GridCell[sizeY, sizeX];
                    Reset();
                }
                public GridCell this[int indexY, int indexX] => _grid[indexY, indexX];
                public GridCell this[Coords coords] => this[coords.Y, coords.X];

                private void Reset() {
                    _grid = new GridCell[_grid.GetLength(0), _grid.GetLength(1)];
                    for (var y = 0; y < _grid.GetLength(0); y++) {
                        for (var x = 0; x < _grid.GetLength(1); x++) {
                            _grid[y, x] = new();
                        }
                    }
                }
            }
        }
    }
}
