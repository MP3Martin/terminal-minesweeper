namespace terminal_minesweeper {
    internal static partial class Program {
        public partial class MinesweeperGame {
            public class GridCell {
                public enum GridCellDisplayType {
                    Covered,
                    Uncovered,
                    Flag,
                    Mine
                }

                public static readonly Dictionary<GridCellDisplayType, string> GridCellDisplayTypeStringDict = new() {
                    [GridCellDisplayType.Flag] = "⚑ ",
                    [GridCellDisplayType.Mine] = "💣",
                    [GridCellDisplayType.Covered] = "■ "
                };

                public readonly GridCellDisplayData Data = new();
                public GridCellDisplayType Type;
                public GridCell(GridCellDisplayType type = GridCellDisplayType.Covered, GridCellDisplayData? data = null) {
                    Type = type;
                    Data = data ?? Data;

                }
                public class GridCellDisplayData {
                    public int? Number;
                    public GridCellDisplayData(int? number = null) {
                        Number = number;
                    }
                }
            }
        }
    }
}
