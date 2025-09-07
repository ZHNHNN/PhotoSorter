using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PhotoSorter.Full
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Log(string message)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(() => Log(message)));
                return;
            }
            TxtLog.AppendText($"{DateTime.Now:HH:mm:ss} {message}{Environment.NewLine}");
            TxtLog.ScrollToEnd();
        }

        private void OnDragOver(object sender, System.Windows.DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void OnDrop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (paths != null && paths.Length > 0 && Directory.Exists(paths[0]))
                {
                    TxtPath.Text = paths[0];
                }
            }
        }

        private void OnOpenFolder(object sender, RoutedEventArgs e)
        {
            var path = TxtPath.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                Process.Start(new ProcessStartInfo("explorer.exe", path) { UseShellExecute = true });
            else
                Process.Start(new ProcessStartInfo("explorer.exe") { UseShellExecute = true });
        }

        private void OnCopyChecked(object sender, RoutedEventArgs e)
        {
            var en = RbCopy.IsChecked == true;
            TxtOut.IsEnabled = en;
            BtnOut.IsEnabled = en;
        }

        private void OnChooseOut(object sender, RoutedEventArgs e)
        {
            var dlg = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "选择输出文件夹",
                ShowNewFolderButton = true
            };
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TxtOut.Text = dlg.SelectedPath;
            }
        }

        private async void OnRun(object sender, RoutedEventArgs e)
        {
            var root = TxtPath.Text.Trim();
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            {
                Log("请选择有效的文件夹");
                return;
            }

            bool includeSub = ChkIncludeSub.IsChecked == true;
            bool dry = ChkDryRun.IsChecked == true;
            bool colorFirst = RbColorFirst.IsChecked == true;
            bool copyMode = RbCopy.IsChecked == true;
            string outDir = TxtOut.Text.Trim();

            if (copyMode && string.IsNullOrWhiteSpace(outDir))
            {
                Log("复制模式需要设置输出文件夹");
                return;
            }

            try
            {
                await Task.Run(() => ProcessFolder(root, includeSub, colorFirst, copyMode, outDir, dry));
                Log("完成。");
            }
            catch (Exception ex)
            {
                Log("错误: " + ex.Message);
            }
        }

        private static readonly string[] ImgExt = new[] { ".jpg",".jpeg",".png",".gif",".bmp",".webp",".tif",".tiff" };

        private void ProcessFolder(string root, bool includeSub, bool colorFirst, bool copyMode, string outDir, bool dry)
        {
            var targets = new List<string> { root };
            if (includeSub)
            {
                targets.AddRange(Directory.GetDirectories(root));
            }

            foreach (var dir in targets)
            {
                Log($"处理: {dir}");
                var files = Directory.GetFiles(dir).Where(p => ImgExt.Contains(Path.GetExtension(p).ToLower())).ToList();
                if (files.Count == 0) { Log("(无图片)"); continue; }

                var infos = files.Select(p => Analyze(p)).ToList();
                var ordered = colorFirst
                    ? infos.OrderBy(i => i.Orientation).ThenBy(i => i.Hue).ThenBy(i => i.Brightness)
                    : infos.OrderBy(i => i.Orientation).ThenBy(i => i.Brightness).ThenBy(i => i.Hue);

                int index = 1;
                foreach (var it in ordered)
                {
                    var newName = index.ToString("D3") + Path.GetExtension(it.Path).ToLower();
                    if (copyMode)
                    {
                        var destDir = string.IsNullOrWhiteSpace(outDir) ? dir : outDir;
                        Directory.CreateDirectory(destDir);
                        var dest = Path.Combine(destDir, newName);
                        Log($"COPY {it.Path} -> {dest}");
                        if (!dry) File.Copy(it.Path, dest, true);
                    }
                    else
                    {
                        var dest = Path.Combine(dir, newName);
                        Log($"RENAME {Path.GetFileName(it.Path)} -> {newName}");
                        if (!dry)
                        {
                            var tmp = Path.Combine(dir, Guid.NewGuid().ToString("N") + Path.GetExtension(it.Path));
                            File.Move(it.Path, tmp);
                            File.Move(tmp, dest);
                        }
                    }
                    index++;
                }
            }
        }

        private class FileInfoEx
        {
            public string Path { get; set; } = string.Empty;
            public int Orientation { get; set; }
            public float Hue { get; set; }
            public float Brightness { get; set; }
        }

        private FileInfoEx Analyze(string path)
        {
            try
            {
                using (var img = System.Drawing.Image.FromFile(path))
                using (var bmp = new System.Drawing.Bitmap(img, new System.Drawing.Size(64, Math.Max(1, img.Height * 64 / Math.Max(1, img.Width)))))
                {
                    long r = 0, g = 0, b = 0; int n = 0;
                    for (int y = 0; y < bmp.Height; y += 2)
                    for (int x = 0; x < bmp.Width; x += 2)
                    {
                        var c = bmp.GetPixel(x, y);
                        r += c.R; g += c.G; b += c.B; n++;
                    }
                    if (n == 0) n = 1;
                    var avg = System.Drawing.Color.FromArgb((int)(r / n), (int)(g / n), (int)(b / n));
                    float hue = avg.GetHue();
                    float brightness = (0.2126f * avg.R + 0.7152f * avg.G + 0.0722f * avg.B) / 255f;
                    int orient = img.Width >= img.Height ? 0 : 1;
                    return new FileInfoEx { Path = path, Orientation = orient, Hue = hue, Brightness = brightness };
                }
            }
            catch
            {
                return new FileInfoEx { Path = path, Orientation = 0, Hue = 0, Brightness = 0.5f };
            }
        }
    }
}

