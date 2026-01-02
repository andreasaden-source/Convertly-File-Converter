using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

public class MainForm : Form {
    private TextBox inputBox;
    private TextBox customArgsBox;
    private ComboBox localOutputFormat;
    private ComboBox youtubeFormat;
    private Button convertLocalButton;
    private Button downloadYoutubeButton;

    private string ffmpegPath = "";
    private string ytDlpPath = "";
    private string downloadsFolder = "";

    public MainForm() {
        LoadConfig();

        downloadsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

        this.Text = "Convertly File Converter";
        this.Width = 800;
        this.Height = 400;
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;

        Label inputLabel = new Label() { Text = "Input file path or YouTube URL (drag & drop supported):", Top = 20, Left = 20, Width = 500 };
        inputBox = new TextBox() { Top = 50, Left = 20, Width = 740 };
        inputBox.AllowDrop = true;
        inputBox.DragEnter += (s, e) => {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        };
        inputBox.DragDrop += (s, e) => {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop)) {
                string[]? files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files != null && files.Length > 0)
                    inputBox.Text = files[0];
            }
        };

        Label localLabel = new Label() { Text = "Local File Conversion (FFmpeg)", Top = 100, Left = 20, Font = new Font("Arial", 10, FontStyle.Bold) };

        Label outputFormatLabel = new Label() { Text = "Output format:", Top = 130, Left = 40, Width = 100 };
        localOutputFormat = new ComboBox() { Top = 128, Left = 150, Width = 120 };
        localOutputFormat.Items.AddRange(new string[] { "mp3", "wav", "flac", "aac", "ogg", "m4a", "opus" });
        localOutputFormat.SelectedIndex = 0;

        convertLocalButton = new Button() { Text = "Convert Local File", Top = 125, Left = 540, Width = 200 };
        convertLocalButton.Click += (s, e) => ConvertLocalFile();

        Label youtubeLabel = new Label() { Text = "YouTube / Video Download (yt-dlp)", Top = 180, Left = 20, Font = new Font("Arial", 10, FontStyle.Bold) };

        Label youtubeFormatLabel = new Label() { Text = "Download as:", Top = 210, Left = 40, Width = 100 };
        youtubeFormat = new ComboBox() { Top = 208, Left = 150, Width = 300 };
        youtubeFormat.Items.AddRange(new object[] {
            "Best quality video + audio (MP4)",
            "Best video only (MP4)",
            "Best audio only (MP3)",
            "Highest resolution (MP4)",
            "720p (MP4)",
            "480p (MP4)",
            "360p (MP4)",
            "WEBM best",
            "MKV best",
            "Audio only (M4A)",
            "Audio only (WAV)",
            "Audio only (OPUS)"
        });
        youtubeFormat.SelectedIndex = 2;

        downloadYoutubeButton = new Button() { Text = "Download from YouTube", Top = 205, Left = 470, Width = 290 };
        downloadYoutubeButton.Click += (s, e) => DownloadFromYoutube();

        Label argsLabel = new Label() { Text = "Custom arguments (optional):", Top = 260, Left = 20, Width = 300 };
        customArgsBox = new TextBox() { Top = 290, Left = 20, Width = 740 };

        this.Controls.Add(inputLabel);
        this.Controls.Add(inputBox);
        this.Controls.Add(localLabel);
        this.Controls.Add(outputFormatLabel);
        this.Controls.Add(localOutputFormat);
        this.Controls.Add(convertLocalButton);
        this.Controls.Add(youtubeLabel);
        this.Controls.Add(youtubeFormatLabel);
        this.Controls.Add(youtubeFormat);
        this.Controls.Add(downloadYoutubeButton);
        this.Controls.Add(argsLabel);
        this.Controls.Add(customArgsBox);
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
        }
        catch (Exception ex) {
            MessageBox.Show("Error during startup: " + ex.Message, "Error");
            Environment.Exit(1);
        }
    }

    private string ExtractEmbeddedResource(string resourceName) {
        var assembly = Assembly.GetExecutingAssembly();
        string fullName = $"ConvertlyFileConverter.{resourceName}";

        using var stream = assembly.GetManifestResourceStream(fullName);
        if (stream == null)
            throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private string ExtractEmbeddedFile(string resourceName) {
        var assembly = Assembly.GetExecutingAssembly();
        string fullName = $"ConvertlyFileConverter.{resourceName}";

        string tempDir = Path.Combine(Path.GetTempPath(), "ConvertlyFileConverter");
        Directory.CreateDirectory(tempDir);
        string outputPath = Path.Combine(tempDir, resourceName);

        if (File.Exists(outputPath))
            return outputPath;

        using var resourceStream = assembly.GetManifestResourceStream(fullName);
        if (resourceStream == null)
            throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");

        using var fileStream = File.Create(outputPath);
        resourceStream.CopyTo(fileStream);

        return outputPath;
    }

    private void ConvertLocalFile() {
        string input = inputBox.Text.Trim();
        if (string.IsNullOrEmpty(input) || !File.Exists(input)) {
            MessageBox.Show("Please enter or drop a valid local file.");
            return;
        }

        object? selectedOutputObj = localOutputFormat.SelectedItem;
        if (selectedOutputObj == null) {
            MessageBox.Show("Please select an output format.");
            return;
        }
        string outputExt = selectedOutputObj.ToString() ?? "mp3";
        string output = Path.ChangeExtension(input, outputExt);

        string args = $"-i \"{input}\" {customArgsBox.Text.Trim()} \"{output}\"";
        RunCommand(ffmpegPath, args);
    }

    private void DownloadFromYoutube() {
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
        string formatOption = selectedFormatObj.ToString() ?? "Best audio only (MP3)";

        string custom = customArgsBox.Text.Trim();
        string formatArgs = "";

        switch (formatOption) {
            case "Best quality video + audio (MP4)":
                formatArgs = "-f \"bestvideo+bestaudio/best\" --merge-output-format mp4";
                break;
            case "Best video only (MP4)":
                formatArgs = "-f \"bestvideo\" --merge-output-format mp4";
                break;
            case "Best audio only (MP3)":
                formatArgs = "-x --audio-format mp3";
                break;
            case "Highest resolution (MP4)":
                formatArgs = "-f \"bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best\"";
                break;
            case "720p (MP4)":
                formatArgs = "-f \"bestvideo[height<=720]+bestaudio/best[height<=720]\" --merge-output-format mp4";
                break;
            case "480p (MP4)":
                formatArgs = "-f \"bestvideo[height<=480]+bestaudio/best[height<=480]\" --merge-output-format mp4";
                break;
            case "360p (MP4)":
                formatArgs = "-f \"bestvideo[height<=360]+bestaudio/best[height<=360]\" --merge-output-format mp4";
                break;
            case "WEBM best":
                formatArgs = "-f \"bestvideo[ext=webm]+bestaudio[ext=webm]/best[ext=webm]/best\"";
                break;
            case "MKV best":
                formatArgs = "-f \"bestvideo+bestaudio\" --merge-output-format mkv";
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
        RunCommand(ytDlpPath, args);
    }

    private void RunCommand(string file, string args) {
        try {
            var process = new Process();
            process.StartInfo.FileName = file;
            process.StartInfo.Arguments = args;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            process.OutputDataReceived += (s, e) => {
                if (e.Data != null)
                    Console.WriteLine(e.Data);
            };
            process.ErrorDataReceived += (s, e) => {
                if (e.Data != null)
                    Console.WriteLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            MessageBox.Show("Operation finished successfully!\n\nFiles saved to your Downloads folder.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) {
            MessageBox.Show("Error: " + ex.Message, "Error");
        }
    }
}

class Config {
    public string? ffmpegPath { get; set; }
    public string? ytDlpPath { get; set; }
}