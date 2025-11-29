using RestSharp;
using System;
using System.Collections.Generic; // Needed for List<>
using System.IO;
using System.Text; // Needed for StringBuilder
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TranscriptionApp
{
    // 1. We need a nested class structure to hold the word-level details
    public class TranscriptionResponse
    {
        [JsonPropertyName("language_code")]
        public string LanguageCode { get; set; }

        // Instead of just "text", we now grab the list of words
        [JsonPropertyName("words")]
        public List<WordData> Words { get; set; }
    }

    public class WordData
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("speaker_id")]
        public string SpeakerId { get; set; }
    }

    class Program
    {
        private static void Main(string[] args)
        {
            // --- CONFIGURATION ---
            string inputFilePath = @"C:\Users\ddkat\Desktop\Borovan 1.mp3";
            string outputFilePath = Path.ChangeExtension(inputFilePath, ".txt");
            string apiKey = "sk_9a6746ca19cea7211a8d46b113bf0a207657314c125e32ca";

            var client = new RestClient("https://api.elevenlabs.io/v1");
            var request = new RestRequest("speech-to-text", Method.Post);

            request.AddHeader("xi-api-key", apiKey);
            request.AddParameter("model_id", "scribe_v1");
            request.AddFile("file", inputFilePath);

            // --- IMPORTANT CHANGE 1: Enable Diarization ---
            request.AddParameter("diarize", "true");

            Console.WriteLine($"Uploading {Path.GetFileName(inputFilePath)}...");

            var response = client.Execute(request);

            if (response.IsSuccessful)
            {
                var data = JsonSerializer.Deserialize<TranscriptionResponse>(response.Content);

                if (data != null && data.Words != null && data.Words.Count > 0)
                {
                    Console.WriteLine("Transcription received. Formatting by speaker...");

                    // --- IMPORTANT CHANGE 2: Build the formatted string ---
                    StringBuilder sb = new StringBuilder();

                    sb.AppendLine($"File: {Path.GetFileName(inputFilePath)}");
                    sb.AppendLine($"Language: {data.LanguageCode}");
                    sb.AppendLine("----------------------------------------");
                    sb.AppendLine();

                    string currentSpeaker = null;

                    foreach (var word in data.Words)
                    {
                        // Handle cases where speaker_id might be null/empty
                        string speakerLabel = string.IsNullOrEmpty(word.SpeakerId) ? "Unknown" : word.SpeakerId;

                        // If the speaker has changed since the last word...
                        if (speakerLabel != currentSpeaker)
                        {
                            // If it's not the very first line, add a double break for readability
                            if (currentSpeaker != null)
                                sb.AppendLine().AppendLine();

                            // Write the Speaker Name (e.g., "speaker_0:")
                            sb.Append($"{speakerLabel}: ");

                            // Update our tracker
                            currentSpeaker = speakerLabel;
                        }

                        // Add the word and a space
                        sb.Append(word.Text + " ");
                    }

                    // Save the formatted content
                    try
                    {
                        File.WriteAllText(outputFilePath, sb.ToString());
                        Console.WriteLine($"Success! File saved to: {outputFilePath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error saving file: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("Response successful, but no words/speakers detected.");
                }
            }
            else
            {
                Console.WriteLine($"API Error: {response.StatusCode} - {response.Content}");
            }

            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }
    }
}