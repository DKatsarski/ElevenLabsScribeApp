using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TranscriptionApp
{
    // 1. Update the class to capture Start and End times
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

        // Timestamps come as doubles (seconds, e.g., 1.54)
        [JsonPropertyName("start")]
        public double Start { get; set; }

        [JsonPropertyName("end")]
        public double End { get; set; }
    }

    class Program
    {
        private static void Main(string[] args)
        {
            string inputFilePath = @"C:\Users\ddkat\Desktop\Borovan 1.mp3";
            string outputFilePath = Path.ChangeExtension(inputFilePath, ".txt");
            string apiKey = "sk_9a6746ca19cea7211a8d46b113bf0a207657314c125e32ca";

            var client = new RestClient("https://api.elevenlabs.io/v1");
            var request = new RestRequest("speech-to-text", Method.Post);

            request.AddHeader("xi-api-key", apiKey);
            request.AddParameter("model_id", "scribe_v1");
            request.AddFile("file", inputFilePath);
            request.AddParameter("diarize", "true");
            // Important: timestamps are enabled by default on scribe_v1, but good to be aware

            Console.WriteLine($"Uploading {Path.GetFileName(inputFilePath)}...");

            var response = client.Execute(request);

            if (response.IsSuccessful)
            {
                var data = JsonSerializer.Deserialize<TranscriptionResponse>(response.Content);

                if (data != null && data.Words != null && data.Words.Count > 0)
                {
                    Console.WriteLine("Processing segments...");
                    StringBuilder sb = new StringBuilder();

                    // --- LOGIC TO GROUP WORDS INTO SEGMENTS ---

                    List<WordData> currentSegment = new List<WordData>();
                    string currentSpeaker = data.Words[0].SpeakerId;

                    foreach (var word in data.Words)
                    {
                        string wordSpeaker = string.IsNullOrEmpty(word.SpeakerId) ? "Unknown" : word.SpeakerId;

                        // If speaker changed, write the PREVIOUS segment to the file
                        if (wordSpeaker != currentSpeaker && currentSegment.Count > 0)
                        {
                            AppendSegment(sb, currentSegment, currentSpeaker);

                            // Reset for new speaker
                            currentSegment.Clear();
                            currentSpeaker = wordSpeaker;
                        }

                        // Add word to current buffer
                        currentSegment.Add(word);
                    }

                    // Don't forget to write the very last segment remaining in the buffer
                    if (currentSegment.Count > 0)
                    {
                        AppendSegment(sb, currentSegment, currentSpeaker);
                    }

                    // Save file with UTF8 encoding (crucial for Cyrillic/Bulgarian)
                    File.WriteAllText(outputFilePath, sb.ToString(), Encoding.UTF8);
                    Console.WriteLine($"Saved to: {outputFilePath}");
                }
            }
            else
            {
                Console.WriteLine($"Error: {response.Content}");
            }
            Console.ReadLine();
        }

        // --- HELPER METHODS ---

        private static void AppendSegment(StringBuilder sb, List<WordData> words, string speaker)
        {
            if (words.Count == 0) return;

            // 1. Get Start time of the FIRST word
            var startTime = FormatTime(words[0].Start);

            // 2. Get End time of the LAST word
            var endTime = FormatTime(words[words.Count - 1].End);

            // 3. Build the text sentence
            string sentence = string.Join(" ", words.ConvertAll(w => w.Text));

            // 4. Format exactly as requested
            // Line 1: 00:00:35,700
            sb.AppendLine(startTime);

            // Line 2: --> 00:00:38,269 [Speaker 2]
            sb.AppendLine($"--> {endTime} [{speaker}]");

            // Line 3: The text
            sb.AppendLine(sentence);

            // Line 4: Empty space
            sb.AppendLine();
        }

        // Converts seconds (double) to "00:00:00,000" format
        private static string FormatTime(double seconds)
        {
            TimeSpan t = TimeSpan.FromSeconds(seconds);
            // The custom format string ensures 3 digits for milliseconds
            return t.ToString(@"hh\:mm\:ss\,fff");
        }
    }
}