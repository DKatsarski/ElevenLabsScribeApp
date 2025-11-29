using RestSharp;
using System;
using System.Text.Json; // Used to parse the result
using System.Text.Json.Serialization;

namespace ElevenLabsScribeTool
{
    // 1. Define a class to hold the response data
    public class TranscriptionResponse
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("language_code")]
        public string LanguageCode { get; set; }
    }

    class Program
    {
        private static void Main(string[] args)
        {
            // 2. Setup the Client (Base URL)
            var client = new RestClient("https://api.elevenlabs.io/v1");

            // 3. Setup the Request (Endpoint Resource)
            var request = new RestRequest("speech-to-text", Method.Post);

            // 4. Headers
            // REPLACE THIS with your actual API Key
            request.AddHeader("xi-api-key", "sk_9a6746ca19cea7211a8d46b113bf0a207657314c125e32ca");

            // 5. Add Parameters
            // Instead of the long string, we use AddParameter for form fields
            request.AddParameter("model_id", "scribe_v1");
            // request.AddParameter("tag_audio_events", "true"); // Example of adding other options

            // 6. Add the File
            // RestSharp handles opening the file, streaming it, and closing it.
            string filePath = @"C:\Users\ddkat\Desktop\Borovan 1.mp3";
            request.AddFile("file", filePath);

            Console.WriteLine("Uploading and Transcribing...");

            // 7. Execute
            var response = client.Execute(request);

            // 8. Handle the Transcription
            if (response.IsSuccessful)
            {
                // Deserialize the JSON string into our C# Object
                var data = JsonSerializer.Deserialize<TranscriptionResponse>(response.Content);

                Console.WriteLine("--- Transcription Success ---");
                Console.WriteLine($"Language Detected: {data.LanguageCode}");
                Console.WriteLine("Text:");
                Console.WriteLine(data.Text);
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode}");
                Console.WriteLine(response.Content); // Print error details from API
            }

            Console.ReadLine();
        }
    }
}