using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Drawing;
using System.Security.Cryptography;

public class MainForm : Form {
    private TextBox inputBox;
    private TextBox customArgsBox;
    private ComboBox localOutputFormat;
    private ComboBox youtubeFormat;
    private Button browseButton;
    private Button convertLocalButton;
    private Button downloadYoutubeButton;
    private Label statusLabel;
    private ProgressBar downloadProgressBar;

    private string ffmpegPath = "";
    private string ytDlpPath = "";
    private string downloadsFolder = "";

    private static readonly Color AccentColor = Color.FromArgb(0, 120, 215);

    public MainForm() {
        LoadConfig();

        downloadsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

        this.Text = "Convertly File Converter";
        this.Width = 840;
        this.Height = 580;
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.BackColor = BackColor;
        this.ForeColor = ForeColor;

        TableLayoutPanel mainLayout = new TableLayoutPanel() {
            Dock = DockStyle.Fill,
            Padding = new Padding(15),
            ColumnCount = 1,
            RowCount = 7,
            BackColor = SystemColors.Window
        };

        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));

        Label inputLabel = new Label() {
            Text = "Input file path or YouTube URL (drag & drop supported):",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = AccentColor
        };

        mainLayout.Controls.Add(inputLabel, 0, 0);

        TableLayoutPanel inputRow = new TableLayoutPanel() {
            ColumnCount = 2,
            RowCount = 1,
            Dock = DockStyle.Fill
        };

        inputRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        inputRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));

        inputBox = new TextBox() {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Multiline = true,
        };

        inputBox.AllowDrop = true;
        inputBox.DragEnter += (s, e) => {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        };

        inputBox.DragDrop += (s, e) => {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop)) {
                string[]? files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files != null && files.Length > 0) inputBox.Text = files[0];
            }
        };

        browseButton = new Button() {
            Text = "Browse...",
            Width = 90,
            Height = 30,
            BackColor = AccentColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };


        browseButton.FlatAppearance.BorderColor = AccentColor;
        browseButton.Click += (s, e) => {
            using OpenFileDialog ofd = new OpenFileDialog() {
                Title = "Select a media file",
                Filter = "Media Files|*.mp4;*.mkv;*.webm;*.avi;*.mov;*.flv;*.mp3;*.wav;*.flac;*.aac;*.ogg;*.m4a|All Files|*.*"
            };

            if (ofd.ShowDialog() == DialogResult.OK) inputBox.Text = ofd.FileName;
        };

        inputRow.Controls.Add(inputBox, 0, 0);
        inputRow.Controls.Add(browseButton, 1, 0);
        mainLayout.Controls.Add(inputRow, 0, 1);

        GroupBox localGroup = new GroupBox() {
            Text = "Local File Conversion",
            Dock = DockStyle.Fill,
            ForeColor = AccentColor,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };

        TableLayoutPanel localPanel = new TableLayoutPanel() {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(10)
        };

        localPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        localPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        localPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        Label outputLabel = new Label() {
            Text = "Output format:",
            AutoSize = true,
            Anchor = AnchorStyles.Right
        };

        localOutputFormat = new ComboBox() {
            Width = 120,
            Font = new Font("Segoe UI", 16F)
        };

        localOutputFormat.Items.AddRange(new string[] { "mp3", "wav", "flac", "aac", "ogg", "m4a", "opus" });
        localOutputFormat.SelectedIndex = 0;
        convertLocalButton = new Button() {
            Text = "Convert Local File",
            Width = 180,
            Height = 40,
            BackColor = AccentColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };

        convertLocalButton.FlatAppearance.BorderColor = AccentColor;
        convertLocalButton.Click += async (s, e) => await ConvertLocalFileAsync();
        localPanel.Controls.Add(outputLabel, 0, 0);
        localPanel.Controls.Add(localOutputFormat, 1, 0);
        localPanel.Controls.Add(convertLocalButton, 2, 0);
        localGroup.Controls.Add(localPanel);
        mainLayout.Controls.Add(localGroup, 0, 2);

        GroupBox youtubeGroup = new GroupBox() {
            Text = "YouTube Video Downloader",
            Dock = DockStyle.Fill,
            ForeColor = AccentColor,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };

        TableLayoutPanel youtubePanel = new TableLayoutPanel() { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Padding = new Padding(10) };

        youtubePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        youtubePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        youtubePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));

        Label youtubeFormatLabel = new Label() {
            Text = "Download as:",
            AutoSize = true,
            Anchor = AnchorStyles.Right
        };

        youtubeFormat = new ComboBox() {
            Width = 280,
            Font = new Font("Segoe UI", 16F)
        };

        youtubeFormat.Items.AddRange(new object[] {
            "Best video & audio (MP4)",
            "Best video (MP4)",
            "Best audio (MP3)",
            "Highest resolution (MP4)",
            "720p (MP4)",
            "480p (MP4)",
            "360p (MP4)",
            "WEBM best",
            "MKV best (true best quality)",
            "Audio only (M4A)",
            "Audio only (WAV)",
            "Audio only (OPUS)"
        });

        youtubeFormat.SelectedIndex = 0;
        downloadYoutubeButton = new Button() {
            Text = "Download from YouTube",
            Width = 260,
            Height = 40,
            BackColor = AccentColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };

        downloadYoutubeButton.FlatAppearance.BorderColor = AccentColor;
        downloadYoutubeButton.Click += async (s, e) => await DownloadFromYoutubeAsync();
        youtubePanel.Controls.Add(youtubeFormatLabel, 0, 0);
        youtubePanel.Controls.Add(youtubeFormat, 1, 0);
        youtubePanel.Controls.Add(downloadYoutubeButton, 2, 0);
        youtubeGroup.Controls.Add(youtubePanel);
        mainLayout.Controls.Add(youtubeGroup, 0, 3);

        Label argsLabel = new Label() {
            Text = "Custom arguments (optional - advanced users):",
            AutoSize = true,
            ForeColor = AccentColor,
            Font = new Font("Segoe UI", 9, FontStyle.Italic)
        };

        customArgsBox = new TextBox() {
            Dock = DockStyle.Fill,
            Multiline = true,
            Height = 80,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Color.White
        };

        mainLayout.Controls.Add(argsLabel, 0, 4);
        mainLayout.Controls.Add(customArgsBox, 0, 5);

        TableLayoutPanel statusPanel = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(0, 10, 0, 0)
        };

        statusPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        statusPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

        statusLabel = new Label {
            Text = "Ready",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = AccentColor,
            Font = new Font("Segoe UI", 11, FontStyle.Bold)
        };

        downloadProgressBar = new ProgressBar {
            Dock = DockStyle.Fill,
            Style = ProgressBarStyle.Continuous,
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            ForeColor = AccentColor
        };

        statusPanel.Controls.Add(statusLabel, 0, 0);
        statusPanel.Controls.Add(downloadProgressBar, 0, 1);

        mainLayout.Controls.Add(statusPanel, 0, 6);

        this.Controls.Add(mainLayout);
    }

    private void LoadConfig() {
        try {
            string configContent = ExtractEmbeddedResource("config.json");
            var config = JsonSerializer.Deserialize<Config>(configContent);

            if (config == null) {
                MessageBox.Show("Failed to read embedded config.json.", "Error");
                Environment.Exit(1);
            }

            ffmpegPath = ExtractEmbeddedFile("ffmpeg.exe");
            ytDlpPath = ExtractEmbeddedFile("yt-dlp.exe");

            if (string.IsNullOrEmpty(ffmpegPath) || string.IsNullOrEmpty(ytDlpPath)) {
                MessageBox.Show("Failed to extract required tools.", "Error");
                Environment.Exit(1);
            }
        } catch (Exception ex) {
            MessageBox.Show("Error during startup: " + ex.Message, "Error");
            Environment.Exit(1);
        }
    }

    private string ExtractEmbeddedResource(string resourceName) {
        var assembly = Assembly.GetExecutingAssembly();
        string fullName = $"ConvertlyFileConverter.{resourceName}";

        using var stream = assembly.GetManifestResourceStream(fullName);
        if (stream == null) throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private string ExtractEmbeddedFile(string resourceName) {
        var assembly = Assembly.GetExecutingAssembly();
        string fullName = $"ConvertlyFileConverter.{resourceName}";

        string tempDir = Path.Combine(Path.GetTempPath(), "ConvertlyFileConverter");
        Directory.CreateDirectory(tempDir);
        string outputPath = Path.Combine(tempDir, resourceName);

        if (File.Exists(outputPath)) return outputPath;

        using var resourceStream = assembly.GetManifestResourceStream(fullName);
        if (resourceStream == null) throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");

        using var fileStream = File.Create(outputPath);
        resourceStream.CopyTo(fileStream);

        return outputPath;
    }

    private string GetUniqueOutputPath(string desiredPath) {
        if (!File.Exists(desiredPath)) return desiredPath;

        string directory = Path.GetDirectoryName(desiredPath)!;
        string fileName = Path.GetFileNameWithoutExtension(desiredPath)!;
        string extension = Path.GetExtension(desiredPath);

        int counter = 1;
        string newPath;
        do {
            newPath = Path.Combine(directory, $"{fileName} ({counter}){extension}");
            counter++;
        } while (File.Exists(newPath));

        return newPath;
    }

    private async Task ConvertLocalFileAsync() {
        string input = inputBox.Text.Trim();
        if (string.IsNullOrEmpty(input) || !File.Exists(input)) {
            MessageBox.Show("Please select a valid local file.");
            return;
        }

        object? selectedOutputObj = localOutputFormat.SelectedItem;
        if (selectedOutputObj == null) {
            MessageBox.Show("Please select an output format.");
            return;
        }

        string outputExt = selectedOutputObj.ToString() ?? "mp3";

        string directory = Path.GetDirectoryName(input)!;
        string fileName = Path.GetFileNameWithoutExtension(input)!;

        string baseOutput = Path.Combine(directory, fileName + "." + outputExt);
        string output = GetUniqueOutputPath(baseOutput);

        string args = $"-i \"{input}\" {customArgsBox.Text.Trim()} \"{output}\"";

        statusLabel.Text = "Converting...";
        downloadProgressBar.Value = 0;
        this.Cursor = Cursors.WaitCursor;
        convertLocalButton.Enabled = false;

        await Task.Run(() => RunCommand(ffmpegPath, args, false));

        convertLocalButton.Enabled = true;
        statusLabel.Text = "Ready";
        downloadProgressBar.Value = 0;
        this.Cursor = Cursors.Default;
    }

    private async Task DownloadFromYoutubeAsync() {
        string url = inputBox.Text.Trim();
        if (string.IsNullOrEmpty(url)) {
            MessageBox.Show("Please enter a YouTube URL.");
            return;
        }

        object? selectedFormatObj = youtubeFormat.SelectedItem;
        if (selectedFormatObj == null) {
            MessageBox.Show("Please select a download format.");
            return;
        }

        string formatOption = selectedFormatObj.ToString() ?? "Best quality video + audio (MP4)";
        string custom = customArgsBox.Text.Trim();
        string formatArgs = "";

        switch (formatOption) {
            case "Best quality video + audio (MP4)":
                formatArgs = "-f \"bestvideo[ext=mp4]+bestaudio[ext=m4a]/best\"";
                break;
            case "Best video only (MP4)":
                formatArgs = "-f \"bestvideo[ext=mp4]\"";
                break;
            case "Best audio only (MP3)":
                formatArgs = "-x --audio-format mp3";
                break;
            case "Highest resolution (MP4)":
                formatArgs = "-f \"bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best\"";
                break;
            case "720p (MP4)":
                formatArgs = "-f \"bestvideo[height<=720][ext=mp4]+bestaudio[ext=m4a]/best[height<=720]\"";
                break;
            case "480p (MP4)":
                formatArgs = "-f \"bestvideo[height<=480][ext=mp4]+bestaudio[ext=m4a]/best[height<=480]\"";
                break;
            case "360p (MP4)":
                formatArgs = "-f \"bestvideo[height<=360][ext=mp4]+bestaudio[ext=m4a]/best[height<=360]\"";
                break;
            case "WEBM best":
                formatArgs = "-f \"bestvideo[ext=webm]+bestaudio[ext=webm]/best[ext=webm]/best\"";
                break;
            case "MKV best (true best quality)":
                formatArgs = "-f \"bestvideo+bestaudio/best\" --merge-output-format mkv";
                break;
            case "Audio only (M4A)":
                formatArgs = "-x --audio-format m4a";
                break;
            case "Audio only (WAV)":
                formatArgs = "-x --audio-format wav";
                break;
            case "Audio only (OPUS)":
                formatArgs = "-x --audio-format opus";
                break;
            default:
                formatArgs = "-f \"best\"";
                break;
        }

        string outputTemplate = Path.Combine(downloadsFolder, "%(title)s.%(ext)s");
        string args = $"{formatArgs} -o \"{outputTemplate}\" {custom} \"{url}\"";

        statusLabel.Text = "Starting download...";
        downloadProgressBar.Value = 0;
        this.Cursor = Cursors.WaitCursor;
        downloadYoutubeButton.Enabled = false;

        await Task.Run(() => RunCommand(ytDlpPath, args, true));

        downloadYoutubeButton.Enabled = true;
        statusLabel.Text = "Ready";
        downloadProgressBar.Value = 0;
        this.Cursor = Cursors.Default;
    }

    private void RunCommand(string file, string args, bool isYoutubeDownload) {
        try {
            var process = new Process();
            process.StartInfo.FileName = file;
            process.StartInfo.Arguments = args;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            process.OutputDataReceived += (sender, e) => UpdateProgress(e.Data, isYoutubeDownload);
            process.ErrorDataReceived += (sender, e) => UpdateProgress(e.Data, isYoutubeDownload);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            this.Invoke(new System.Windows.Forms.MethodInvoker(() => {
                if (process.ExitCode == 0) {
                    MessageBox.Show("Operation finished successfully!\n\nFiles saved to your Downloads folder.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                } else {
                    MessageBox.Show("Operation completed with warnings or errors.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }));
        } catch (Exception ex) {
            this.Invoke(new System.Windows.Forms.MethodInvoker(() => {
                MessageBox.Show("Error: " + ex.Message, "Error");
                statusLabel.Text = "Ready";
                downloadProgressBar.Value = 0;
                this.Cursor = Cursors.Default;
            }));
        }
    }

    private void UpdateProgress(string? line, bool isYoutubeDownload) {
        if (!isYoutubeDownload || line == null) return;
        if (line.Contains("[download]") && line.Contains("%")) {
            int percentIndex = line.IndexOf('%');
            if (percentIndex > 0) {
                string beforePercent = line.Substring(0, percentIndex + 1);
                int lastSpace = beforePercent.LastIndexOf(' ');
                if (lastSpace > 0) {
                    string percentStr = beforePercent.Substring(lastSpace + 1).Replace("%", "").Trim();
                    if (double.TryParse(percentStr, out double percent)) {
                        int progressValue = (int)Math.Round(percent);
                        this.Invoke(new System.Windows.Forms.MethodInvoker(() => {
                            downloadProgressBar.Value = Math.Min(progressValue, 100);
                            statusLabel.Text = $"Downloading... {progressValue}%";
                        }));
                    }
                }
            }
        }
    }
}

class Config
{
    public string? ffmpegPath { get; set; }
    public string? ytDlpPath { get; set; }
}