using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace TranscriptionApp
{
    public class TranscriptionResponse
    {
        [JsonPropertyName("words")]
        public List<WordData> Words { get; set; }
    }

    public class WordData
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("speaker_id")]
        public string SpeakerId { get; set; }

        [JsonPropertyName("start")]
        public double Start { get; set; }

        [JsonPropertyName("end")]
        public double End { get; set; }
    }

    class Program
    {
        private static void Main(string[] args)
        {
            // --- CONFIGURATION ---
            // Note: Ensure this path points to your actual file
            string inputFilePath = @"C:\Users\ddkat\Desktop\Borovan 1 Normalized.mp3";

            // SECURITY: Ideally, set this in your Windows Environment Variables
            string apiKey = Environment.GetEnvironmentVariable("ELEVENLABS_API_KEY") ?? "YOUR_API_KEY_HERE";

            // TUNING: "bg" for Bulgarian, "en" for English
            string languageCode = "bg";

            Console.WriteLine("Enter number of speakers (leave empty for Auto):");
            string speakerInput = Console.ReadLine();
            int? numSpeakers = string.IsNullOrWhiteSpace(speakerInput) ? (int?)null : int.Parse(speakerInput);

            // --- API REQUEST ---
            var options = new RestClientOptions("https://api.elevenlabs.io/v1")
            {
                Timeout = Timeout.InfiniteTimeSpan
            };
            var client = new RestClient(options);
            var request = new RestRequest("speech-to-text", Method.Post);

            request.AddHeader("xi-api-key", apiKey);
            request.AddParameter("model_id", "scribe_v1");
            request.AddFile("file", inputFilePath);
            request.AddParameter("diarize", "true");

            if (!string.IsNullOrEmpty(languageCode))
            {
                request.AddParameter("language_code", languageCode);
            }

            if (numSpeakers.HasValue && numSpeakers.Value > 0)
            {
                request.AddParameter("num_speakers", numSpeakers.Value);
            }

            Console.WriteLine($"Uploading {Path.GetFileName(inputFilePath)}...");

            var response = client.Execute(request);

            if (response.IsSuccessful)
            {
                var data = JsonSerializer.Deserialize<TranscriptionResponse>(response.Content);

                if (data != null && data.Words != null && data.Words.Count > 0)
                {
                    Console.WriteLine($"Received {data.Words.Count} words. Processing...");

                    // CHANGED: Save as .txt
                    string outputFilePath = Path.ChangeExtension(inputFilePath, ".txt");

                    ProcessAndSave(data.Words, outputFilePath);
                }
                else
                {
                    Console.WriteLine("Response was successful but contained no words.");
                }
            }
            else
            {
                Console.WriteLine($"Error: {response.Content}");
            }

            Console.WriteLine("Done. Press Enter to exit.");
            Console.ReadLine();
        }

        private static void ProcessAndSave(List<WordData> words, string outputPath)
        {
            StringBuilder sb = new StringBuilder();
            List<WordData> buffer = new List<WordData>();

            string currentSpeaker = words[0].SpeakerId;

            foreach (var word in words)
            {
                string wordSpeaker = string.IsNullOrEmpty(word.SpeakerId) ? "Unknown" : word.SpeakerId;

                // Only flush the buffer if the speaker actually changes
                if (wordSpeaker != currentSpeaker)
                {
                    if (buffer.Count > 0)
                    {
                        AppendTextSegment(sb, buffer, currentSpeaker);
                        buffer.Clear();
                    }
                    currentSpeaker = wordSpeaker;
                }

                buffer.Add(word);
            }

            // Flush remaining words
            if (buffer.Count > 0)
            {
                AppendTextSegment(sb, buffer, currentSpeaker);
            }

            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
            Console.WriteLine($"Saved to: {outputPath}");
        }

        // CHANGED: Formats as readable Text (Original Format)
        private static void AppendTextSegment(StringBuilder sb, List<WordData> words, string speaker)
        {
            if (words.Count == 0) return;

            var startTime = FormatTime(words[0].Start);
            var endTime = FormatTime(words[words.Count - 1].End);
            string text = string.Join(" ", words.Select(w => w.Text));

            // Format:
            // 00:00:35,700
            // --> 00:00:38,269 [speaker_id]
            // The text content goes here.

            sb.AppendLine(startTime);
            sb.AppendLine($"--> {endTime} [{speaker}]");
            sb.AppendLine(text);
            sb.AppendLine();
        }

        private static string FormatTime(double seconds)
        {
            TimeSpan t = TimeSpan.FromSeconds(seconds);
            return t.ToString(@"hh\:mm\:ss\,fff");
        }
    }
}