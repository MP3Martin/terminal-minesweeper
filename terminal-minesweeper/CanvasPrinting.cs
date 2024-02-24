namespace terminal_minesweeper {
    internal static partial class Program {
        public class CanvasPrinting {
            public const string CursorAtZeroZero = "\u001b[1;1H";
            private bool _printing;
            public bool Enabled;
            private void PrintInBackground(string str) {
                if (!Enabled) return;
                Console.Write(str);
                _printing = false;
            }
            public void Print(string str) {
                if (!Enabled || _printing) return;
                if (ConsoleResize.CheckResized()) Console.Clear();
                _printing = true;
                Task.Run(() => PrintInBackground(CursorAtZeroZero + str));
            }
            public void DisableWaitForFinish() {
                Enabled = false;
                Task.Run(() => {
                    while (_printing) {
                        Task.Delay(10);
                    }
                }).Wait();
            }
        }
    }
}
