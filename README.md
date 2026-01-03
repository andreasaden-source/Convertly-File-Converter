# Convertly File Converter (Windows)

![menu](./assets/menu_preview.png)

---

**Convertly File Converter** is a file converter program developed for Windows. It embeds ffmpeg and yt-dlp into one single bundle supported by a GUI for easy use. This way users can convert files and download from YouTube.

## Development

If you would like to futher develop the project, you can download the code as .zip. Make sure you have the .NET framework downloaded as the project is programmed in C#.

Additionally, you will have to download the ffmpeg.exe and yt-dlp.exe files from their websites and place them in your root folder.

1. Download the latest Windows binaries:
   - FFmpeg: https://www.gyan.dev/ffmpeg/builds/ (choose "full" build → extract `ffmpeg.exe` from bin folder)
   - yt-dlp: https://github.com/yt-dlp/yt-dlp/releases (download `yt-dlp.exe`)
2. Make sure config.json correctly matches the files names described above.

If you made changes and want to deploy it you can run the following:

```bash
dotnet clean
```

```bash
dotnet publish ConvertlyFileConverter.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

The output exe can be found in:
**bin\Release\net10.0-windows\win-x64\publish**

## Support

We will do our best to maintain and add updates in the near future. We are planning on adding more, but you are more than welcome to copy and use this project for yourself as well as publish it.

## Credits & Third-Party Tools

This application uses the following external tools:

- **[yt-dlp](https://github.com/yt-dlp/yt-dlp)** – Licensed under the Unlicense (public domain).  
  A feature-rich command-line audio/video downloader.

- **[FFmpeg](https://ffmpeg.org)** – Licensed under the LGPL v2.1 (with some optional GPL parts).  
  A complete, cross-platform solution to record, convert and stream audio and video.

Both tools are bundled as standalone executables and executed externally.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Arguments

## For Local File Conversion (FFmpeg)

These go **between** the input and output file.

| Purpose                         | Arguments                                 | Example / Notes                         |
| ------------------------------- | ----------------------------------------- | --------------------------------------- |
| Faster encoding (lower quality) | `-preset veryfast`                        | Quick conversion, larger file           |
| Better quality (slower)         | `-preset slow -crf 18`                    | Great for video (H.264)                 |
| High-quality audio              | `-b:a 320k`                               | 320 kbps (best for MP3)                 |
| Specific audio bitrate          | `-b:a 192k` or `-b:a 128k`                | Lower bitrate = smaller file            |
| Specific video bitrate          | `-b:v 2500k`                              | e.g., 2.5 Mbps video                    |
| Resize video                    | `-vf scale=1280:720`                      | Force 720p resolution                   |
| Crop video                      | `-vf crop=640:480:100:50`                 | Crop to 640×480 starting at x=100, y=50 |
| Extract audio only              | `-vn`                                     | Disable video stream                    |
| Extract video only              | `-an`                                     | Disable audio stream                    |
| Speed up video (2×)             | `-filter:v "setpts=0.5*PTS"`              | Makes video twice as fast               |
| Slow down video (½×)            | `-filter:v "setpts=2.0*PTS"`              | Makes video half speed                  |
| Combine multiple filters        | `-preset fast -crf 23 -vf scale=1280:720` | Example combo                           |

## For YouTube / Video Download (yt-dlp)

These go **after** the format/output options and **before** the URL.

| Purpose                                | Arguments                                                              | Example / Notes                               |
| -------------------------------------- | ---------------------------------------------------------------------- | --------------------------------------------- |
| Embed subtitles (English)              | `--embed-subs --sub-lang en`                                           | Subtitles burned into video                   |
| Add metadata (title, artist, etc.)     | `--add-metadata`                                                       | Works great with audio downloads              |
| Best possible audio quality            | `-x --audio-quality 0`                                                 | Use with audio formats                        |
| Limit download speed                   | `--limit-rate 500K`                                                    | 500 KB/s max (useful on slow connections)     |
| Faster download (multiple connections) | `--external-downloader aria2c --external-downloader-args "-x 8 -k 1M"` | Needs aria2c installed on your system         |
| Download entire playlist               | `--yes-playlist`                                                       | Forces playlist download                      |
| Download only one video (not playlist) | `--no-playlist`                                                        | Even if URL is a playlist                     |
| Download only videos after a date      | `--dateafter 20240101`                                                 | Format: YYYYMMDD                              |
| Retry more times on failure            | `--retries 20`                                                         | Helpful on unstable networks                  |
| Download thumbnails                    | `--write-thumbnail --convert-thumbnails jpg`                           | Saves cover art                               |
| Skip download (test only)              | `--simulate`                                                           | Shows what would be downloaded without saving |
