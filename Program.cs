using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using Newtonsoft.Json.Linq;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos.Streams;

class Program
{
    static async Task Main(string[] args)
    {
        Console.Title = "Nebula Bot [Console App Spotify]";
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.Write("Do you want to search for song or play your saved offline songs (y/n)?: ");
        string choice = Console.ReadLine();
        if(choice.ToLower() == "y")
        {
            while (true) // Loop indefinitely until user exits
            {
                await PlaySong();
            }
        }
        if(choice.ToLower() == "n")
        {
            OfflineMode();
        }

    }

    public static void OfflineMode()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var offlineModePath = Path.Combine(currentDirectory, "OfflineMode");
        var artistDirectories = Directory.GetDirectories(offlineModePath);

        Thread.Sleep(2000);
        Console.Clear();
        Console.WriteLine("Saved Online Artists?");
        Console.WriteLine("----------------------------------------------");

        int artistCount = 0;
        List<string> artistNames = new List<string>();

        foreach (var artistDirectory in artistDirectories)
        {
            artistCount++;
            string artistName = artistDirectory.Replace($"{offlineModePath}\\", "");
            Console.WriteLine($"[{artistCount}] {artistName}");
            artistNames.Add(artistName);
        }
        Console.WriteLine("\n");
        Console.Write("Enter Option: ");
        int selectedOption = Convert.ToInt32(Console.ReadLine());

        var selectedArtistPath = Path.Combine(offlineModePath, artistNames[selectedOption - 1]);

        Thread.Sleep(2000);
        Console.Clear();

        var getSelectedArtistFiles = Directory.GetFiles(selectedArtistPath);
        
        artistCount = 0;
        foreach(var artistFile in getSelectedArtistFiles)
        {
            
            artistCount++;
            Console.WriteLine($"[{artistCount}]" + artistFile.Replace(selectedArtistPath + "\\", "").Replace(".mp3", "").Replace("_", ""));
        }

        Console.WriteLine("\n");
        Console.Write("Do you want to play them all one by one or just play one of the selected song (y/n): ");
        string selectOption = Console.ReadLine();

        if (selectOption.ToLower() == "y")
        {
            PlayAllSongs(getSelectedArtistFiles).Wait();
        }
        else if (selectOption.ToLower() == "n")
        {
            Console.Write("Select a Song: ");
            int selectSong = Convert.ToInt32(Console.ReadLine());
            PlayMp3File(getSelectedArtistFiles[selectSong - 1]).Wait();
        }


    }
    static async Task PlaySong()
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.Write("Enter The Name Of The Song: ");
        string songName = Console.ReadLine();

        string deezerApiUrl = $"https://api.deezer.com/search?q={Uri.EscapeDataString(songName)}";

        using (HttpClient httpClient = new HttpClient())
        {
            HttpResponseMessage response = await httpClient.GetAsync(deezerApiUrl);

            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                JObject json = JObject.Parse(responseBody);

                JArray data = (JArray)json["data"];
                if (data.Count > 0)
                {
                    var song = data[0]; // Get the first song only

                    Console.WriteLine($"Title: {song["title"]}");
                    Console.WriteLine($"Artist: {song["artist"]["name"]}");
                    Console.WriteLine($"Album: {song["album"]["title"]}");
                    //Console.WriteLine($"Preview Song: {song["preview"]}");

                    Console.Write("Do you want to save the song for offline mode (y/n): ");
                    string offlineMode = Console.ReadLine();
                    if(offlineMode.ToLower() == "y")
                    {
                        var dir = Directory.GetCurrentDirectory() + $"\\OfflineMode\\{song["artist"]["name"]}";
                        if(Directory.Exists(dir))
                        {
                            var previewUrl = song["preview"].ToString();

                            Thread.Sleep(3000);

                            Console.Clear();

                            Console.WriteLine("Searching For Song.....");
                            var youtube = new YoutubeClient();

                            string query = song["artist"]["name"].ToString() + " " + song["title"].ToString();

                            try
                            {
                                // Search for the video
                                var searchResults = await youtube.Search.GetVideosAsync(query);
                                var video = searchResults.FirstOrDefault(v => !v.Title.Contains("Music Video"));

                                if (video == null)
                                {
                                    Console.WriteLine("Song was not found for the search query.");
                                    return;
                                }

                                Console.WriteLine($"Found Song: {video.Title} by {video.Author}");

                                // Get the video ID
                                var videoId = video.Id;

                                // Get the audio-only stream
                                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoId);
                                var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                                // Prepare the file path
                                string outputFileName = GetValidFileName(video.Title);
                                var outputFilePath = dir + $"\\{outputFileName.Replace(outputFileName, song["title"].ToString() + " - " + song["artist"]["name"].ToString())}.mp3";

                                // Download the audio stream directly to the file path
                                await youtube.Videos.Streams.DownloadAsync(streamInfo, outputFilePath);

                                Console.WriteLine($"Downloaded {outputFileName}.mp3 to {outputFilePath}");

                                // Fetch lyrics and print
                                string lyrics = await GetLyricsFromAPI(song["title"].ToString(), song["artist"]["name"].ToString());

                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine("Lyrics:");
                                Console.WriteLine(lyrics);

                                // Play the downloaded MP3 file using NAudio
                                await PlayMp3File(outputFilePath);

                                Console.ResetColor();
                                // Clear the console
                                Console.Clear();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"An error occurred: {ex.Message}");
                            }
                        }
                        else if(!Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                            var previewUrl = song["preview"].ToString();

                            Thread.Sleep(3000);

                            Console.Clear();

                            Console.WriteLine("Searching For Song.....");
                            var youtube = new YoutubeClient();

                            string query = song["artist"]["name"].ToString() + " " + song["title"].ToString();

                            try
                            {
                                // Search for the video
                                var searchResults = await youtube.Search.GetVideosAsync(query);
                                var video = searchResults.FirstOrDefault(v => !v.Title.Contains("Music Video"));

                                if (video == null)
                                {
                                    Console.WriteLine("Song was not found for the search query.");
                                    return;
                                }

                                Console.WriteLine($"Found Song: {video.Title} by {video.Author}");

                                // Get the video ID
                                var videoId = video.Id;

                                // Get the audio-only stream
                                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoId);
                                var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                                // Prepare the file path
                                string outputFileName = GetValidFileName(video.Title);
                                var outputFilePath = dir + $"\\{outputFileName.Replace(outputFileName, song["title"].ToString() + " - " + song["artist"]["name"].ToString())}.mp3";

                                // Download the audio stream directly to the file path
                                await youtube.Videos.Streams.DownloadAsync(streamInfo, outputFilePath);

                                Console.WriteLine($"Downloaded {outputFileName}.mp3 to {outputFilePath}");

                                // Fetch lyrics and print
                                string lyrics = await GetLyricsFromAPI(song["title"].ToString(), song["artist"]["name"].ToString());

                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine("Lyrics:");
                                Console.WriteLine(lyrics);

                                // Play the downloaded MP3 file using NAudio
                                await PlayMp3File(outputFilePath);

                                Console.ResetColor();
                                // Clear the console
                                Console.Clear();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"An error occurred: {ex.Message}");
                            }
                        }


                       
                    }

                    else if (offlineMode.ToLower() == "n")
                    {
                        var previewUrl = song["preview"].ToString();

                        Thread.Sleep(3000);

                        Console.Clear();

                        Console.WriteLine("Searching For Song.....");
                        string artistName = song["artist"]["name"].ToString();
                        string query = songName + " " + artistName;

                        var youtube = new YoutubeClient();

                        var searchResults = await youtube.Search.GetVideosAsync(query);

                        // Filter out videos labeled as "Music Video"
                        var video = searchResults.FirstOrDefault(v => !v.Title.Contains("Music Video"));

                        if (video == null)
                        {
                            Console.WriteLine("Song was not found for the search query.");
                            return;
                        }

                        Console.WriteLine($"Found Song: {video.Title} by {video.Author}");

                        // Get the video ID
                        var videoId = video.Id;

                        // Get the audio-only stream
                        var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoId);
                        var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                        // Prepare the file path
                        

                        // Fetch lyrics and print
                        string lyrics = await GetLyricsFromAPI(song["title"].ToString(), artistName);

                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Lyrics:");
                        Console.WriteLine(lyrics);

                        if (streamInfo != null)
                        {
                            // Download the audio stream to a temporary file
                            await PlayAudioStreamAsync(youtube, streamInfo, video.Title);
                        }
                        else
                        {
                            Console.WriteLine("No audio stream found for the selected video.");
                        }

                        Console.ResetColor();
                        // Clear the console
                        Console.Clear();
                    }
                }
                else
                {
                    Console.WriteLine("No matching songs found.");
                    Console.Clear();
                }
            }
            else
            {
                Console.WriteLine($"Failed to retrieve song information. Status code: {response.StatusCode}");
            }
        }
    }
    static async Task PlayAudioStreamAsync(YoutubeClient youtube, IStreamInfo audioStreamInfo, string videoTitle)
    {
        var tempFilePath = Path.GetTempFileName();

        try
        {
            await youtube.Videos.Streams.DownloadAsync(audioStreamInfo, tempFilePath);

            using (var mediaFoundationReader = new MediaFoundationReader(tempFilePath))
            using (var waveOut = new WaveOutEvent())
            {
                waveOut.Init(mediaFoundationReader);
                waveOut.Play();

                while (waveOut.PlaybackState == PlaybackState.Playing)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true);
                        if (key.Key == ConsoleKey.Spacebar) // Pause on spacebar press
                        {
                            waveOut.Pause();
                            Console.WriteLine("Playback paused. Press spacebar to resume.");
                            Console.ResetColor();
                            Console.Clear();
                        }
                    }

                    var elapsedTime = mediaFoundationReader.CurrentTime;

                    Console.Title = $"{videoTitle} | Elapsed: {elapsedTime:mm\\:ss} / {mediaFoundationReader.TotalTime:mm\\:ss}";
                    await Task.Delay(500);
                }
            }
        }
        finally
        {
            File.Delete(tempFilePath);
        }
    }

    private static async Task PlayAllSongs(string[] songFiles)
    {
        foreach (var songFile in songFiles)
        {
            await PlayMp3File(songFile);
        }
        Console.Clear();
        Main(new string[0]).Wait(); // Return to the main menu
    }
    private static async Task PlayMp3File(string filePath)
    {
        try
        {
            using (var audioFile = new AudioFileReader(filePath))
            using (var outputDevice = new WaveOutEvent())
            {
                outputDevice.Init(audioFile);
                outputDevice.Play();

                // Display elapsed time and song name dynamically in console title
                while (outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true);
                        if (key.Key == ConsoleKey.Spacebar) // Pause on spacebar press
                        {
                            outputDevice.Stop();
                            //Console.WriteLine("Playback paused. Press spacebar to resume.");
                            Console.ResetColor();
                            Console.Clear();
                            await PlaySong();
                        }
                    }

                    Console.Title = $"{Path.GetFileNameWithoutExtension(filePath)} | Elapsed: {audioFile.CurrentTime.ToString("mm\\:ss")} / {audioFile.TotalTime.ToString("mm\\:ss")}";
                    await Task.Delay(500);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while playing the file: {ex.Message}");
        }
    }

    static async Task<string> GetLyricsFromAPI(string songTitle, string artistName)
    {
        string lyricsApiUrl = $"https://api.lyrics.ovh/v1/{Uri.EscapeDataString(artistName)}/{Uri.EscapeDataString(songTitle)}";
        using (HttpClient httpClient = new HttpClient())
        {
            HttpResponseMessage response = await httpClient.GetAsync(lyricsApiUrl);
            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                JObject json = JObject.Parse(responseBody);
                string lyrics = json["lyrics"].ToString();
                return lyrics;
            }
            else
            {
                return "Lyrics not available";
            }
        }
    }

    // Replace invalid characters in a file name with underscores
    static string GetValidFileName(string fileName)
    {
        string invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
        foreach (char c in invalidChars)
        {
            fileName = fileName.Replace(c, '_');
        }
        return fileName;
    }
}
