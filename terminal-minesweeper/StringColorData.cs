namespace terminal_minesweeper {
    internal static partial class Program {
        private class StringColorDataList {
            private readonly List<StringColorData> _data;
            public StringColorDataList(ConsoleColor? defaultColor = null, params StringColorData[] dataParams) {
                if (defaultColor is not null) {
                    dataParams = dataParams.Select(i => {
                        i.Color ??= defaultColor;
                        return i;
                    }).ToArray();
                }
                _data = dataParams.ToList();
            }

            public StringColorDataList(params StringColorData[] dataParams) : this(null, dataParams) { }

            public static implicit operator List<StringColorData>(StringColorDataList dataList) {
                return dataList._data.ToList();
            }
        }

        private class StringColorData {
            public readonly AdditionalData Data = new();
            public ConsoleColor? BgColor;
            public ConsoleColor? Color;
            public string String;

            public StringColorData(string str, ConsoleColor? color = null, ConsoleColor? bgColor = null, AdditionalData? data = null) {
                String = str;
                Color = color ?? Color;
                BgColor = bgColor ?? BgColor;
                Data = data ?? Data;
            }

            public static implicit operator StringColorData(string str) {
                return new(str);
            }

            public static implicit operator StringColorData(ValueTuple<string, ConsoleColor> tuple) {
                return new(tuple.Item1, tuple.Item2);
            }

            public class AdditionalData {
                public bool? CellLeft;
                public bool? CellTop;
            }
        }
    }
}
