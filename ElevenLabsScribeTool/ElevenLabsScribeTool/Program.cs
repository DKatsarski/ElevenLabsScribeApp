using System.Net.Http.Headers;

namespace ElevenLabsScribeTool
{
    public class Program
    {
        // 1. Define the API Endpoint
        private const string ApiUrl = "https://api.elevenlabs.io/v1/speech-to-text/convert";

        // 2. Paste your API Key here
        private const string ApiKey = "sk_9a6746ca19cea7211a8d46b113bf0a207657314c125e32ca";

        static async Task Main(string[] args)
        {
            // Update this path to your actual file
            string filePath = @"G:\My Drive\BAN\PROEKTI\NEETS\Focus Groups\Borovan 1.mp3";

            if (!File.Exists(filePath))
            {
                Console.WriteLine("File not found!");
                return;
            }

            Console.WriteLine("Opening file stream...");

            // 1. INCREASE TIMEOUT: Large files need more than the default 100 seconds
            using var handler = new HttpClientHandler();
            // Optional: If you have proxy issues, you can set handler.Proxy = ...

            using var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromMinutes(120); // Give it 2 hours to be safe
            client.DefaultRequestHeaders.Add("xi-api-key", ApiKey);

            try
            {
                using var form = new MultipartFormDataContent();

                // 2. STREAMING: Do NOT use ReadAllBytes. Use OpenRead for streaming.
                using var fileStream = File.OpenRead(filePath);
                using var streamContent = new StreamContent(fileStream);

                // 3. BUFFER SIZE: Set a larger buffer for efficiency (optional but helps)
                // Default is usually fine, but streamContent handles it.

                streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/mpeg");

                // Add the file (Key: "file")
                form.Add(streamContent, "file", Path.GetFileName(filePath));

                // Add other parameters
                form.Add(new StringContent("scribe_v1"), "model_id");
                form.Add(new StringContent("bg"), "language_code");
                form.Add(new StringContent("true"), "tag_audio_events");
                form.Add(new StringContent("true"), "diarize");

                Console.WriteLine($"Starting upload of {filePath} ({fileStream.Length / 1024 / 1024} MB)...");
                Console.WriteLine("Please wait. Do not close this window.");

                // 4. SEND
                var response = await client.PostAsync(ApiUrl, form);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("\n--- SUCCESS! ---");
                    // Save the JSON response
                    await File.WriteAllTextAsync(filePath + "_transcript.json", responseString);
                    Console.WriteLine($"Transcript saved to: {filePath}_transcript.json");
                }
                else
                {
                    Console.WriteLine($"\nError ({response.StatusCode}):");
                    Console.WriteLine(responseString);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nCRITICAL ERROR: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Error: {ex.InnerException.Message}");
                }
            }
        }
    }
}