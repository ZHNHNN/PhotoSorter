using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PhotoSorter.Wpf
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            TryApplyMica();
        }

        private void TryApplyMica()
        {
            try
            {
                var hwnd = new System.Windows.Interop.WindowInteropHelper(this).EnsureHandle();
                // DWMWA_SYSTEMBACKDROP_TYPE = 38, DWMSBT_MAINWINDOW = 2 (Mica)
                const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
                int mica = 2;
                DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref mica, sizeof(int));
            }
            catch { }
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private void Log(string message)
        {
            TxtLog.AppendText($"{DateTime.Now:HH:mm:ss} {message}{Environment.NewLine}");
            TxtLog.ScrollToEnd();
        }

        private void OnDragOver(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
                e.Effects = System.Windows.DragDropEffects.Copy;
            else
                e.Effects = System.Windows.DragDropEffects.None;
            e.Handled = true;
        }

        private void OnDrop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var paths = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (paths != null && paths.Length > 0 && Directory.Exists(paths[0]))
                {
                    TxtPath.Text = paths[0];
                }
            }
        }

        private void OnBrowse(object sender, RoutedEventArgs e)
        {
            var dlg = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select a folder to process",
                ShowNewFolderButton = false
            };
            var r = dlg.ShowDialog();
            if (r == System.Windows.Forms.DialogResult.OK)
            {
                TxtPath.Text = dlg.SelectedPath;
            }
        }

        private void OnChooseOut(object sender, RoutedEventArgs e)
        {
            var dlg = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select output folder (Copy mode)",
                ShowNewFolderButton = true
            };
            var r = dlg.ShowDialog();
            if (r == System.Windows.Forms.DialogResult.OK)
            {
                TxtOut.Text = dlg.SelectedPath;
            }
        }

        private void OnCopyChecked(object sender, RoutedEventArgs e)
        {
            var en = RbCopy.IsChecked == true;
            TxtOut.IsEnabled = en;
            BtnOut.IsEnabled = en;
        }

    private void OnOpenFolder(object sender, RoutedEventArgs e)
    {
        var path = TxtPath.Text?.Trim();
        if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            Process.Start(new ProcessStartInfo("explorer.exe", path) { UseShellExecute = true });
        else
            Process.Start(new ProcessStartInfo("explorer.exe") { UseShellExecute = true });
    }

        // Removed copy-path button per new UX

        private async void OnRun(object sender, RoutedEventArgs e)
        {
            var path = TxtPath.Text.Trim();
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                Log("Please choose a valid folder.");
                return;
            }

            var appDir = AppContext.BaseDirectory;
            string scriptNormalize = Path.Combine(appDir, "scripts", "normalize-folders.ps1");
            string scriptColor = Path.Combine(appDir, "scripts", "reorder-photos-color-first.ps1");
            string scriptBright = Path.Combine(appDir, "scripts", "reorder-photos.ps1");

            bool normalize = ChkNormalize.IsChecked == true;
            bool includeSub = ChkIncludeSub.IsChecked == true;
            bool dry = ChkDryRun.IsChecked == true;
            bool useColor = RbColorFirst.IsChecked == true;
            bool copyMode = RbCopy.IsChecked == true;
            string outDir = TxtOut.Text.Trim();

            try
            {
                if (normalize)
                {
                    Log("[1/3] Normalizing subfolders ...");
                    await RunPwsh(scriptNormalize, $"-Path \"{path}\" -Pad 2");
                }

                var runner = useColor ? scriptColor : scriptBright;

                if (includeSub)
                {
                    Log("[2/3] 处理子文件夹...");
                    foreach (var d in Directory.GetDirectories(path))
                    {
                        Log("  -> " + d);
                        await RunPwsh(runner, ArgsFor(runner, d, copyMode, outDir, dry));
                    }
                }

                Log("[3/3] Processing root folder ...");
                await RunPwsh(runner, ArgsFor(runner, path, copyMode, outDir, dry));

                Log("完成。");
                Log("Done.");
            }
            catch (Exception ex)
            {
                Log("错误: " + ex.Message);
            }
        }

        private static string ArgsFor(string runner, string target, bool copyMode, string outDir, bool dry)
        {
            var sb = new StringBuilder();
            sb.Append($"-Path \"{target}\"");
            if (runner.EndsWith("reorder-photos.ps1", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append(" -Window 6");
            }
            sb.Append(copyMode ? $" -Mode Copy -OutDir \"{outDir}\"" : " -Mode InPlace");
            if (dry) sb.Append(" -DryRun");
            return sb.ToString();
        }

        private Task RunPwsh(string scriptPath, string args)
        {
            var tcs = new TaskCompletionSource<object>();

            var psi = new ProcessStartInfo
            {
                FileName = Environment.ExpandEnvironmentVariables("%SystemRoot%\\System32\\WindowsPowerShell\\v1.0\\powershell.exe"),
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" {args}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                CreateNoWindow = true
            };

            var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
            p.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Dispatcher.Invoke(() => Log(e.Data)); };
            p.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Dispatcher.Invoke(() => Log(e.Data)); };
            p.Exited += (s, e) => Dispatcher.Invoke(() => tcs.TrySetResult(null));
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            return tcs.Task;
        }
    }
}
