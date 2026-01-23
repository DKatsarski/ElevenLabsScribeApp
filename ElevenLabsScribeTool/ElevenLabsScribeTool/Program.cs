using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using RestSharp;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Run = DocumentFormat.OpenXml.Wordprocessing.Run;

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
            Console.WriteLine("Drag and drop a .mp3 file here and press Enter:");
            string input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
                return;

           
            string inputFilePath = input.Trim().Trim('"');
            //string inputFilePath = @"C:\Users\ddkat\Desktop\NormalizedInterviews\Borovan 2 N.mp3";

            // SECURITY: Ideally, set this in your Windows Environment Variables
            Console.WriteLine("Enter API Key here and press Enter:");
            string apiKey = Environment.GetEnvironmentVariable("ELEVENLABS_API_KEYss") ?? Console.ReadLine().Trim().Trim('"');
            // sk_35ea0edc284d36ac68c5140295fca20bbc908f26569741243dcf032d1ac44d4a
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

        private static void BoldText(string path)
        {
            string filePath = path;

            Console.WriteLine($"Processing file: {filePath}...");

            try
            {
                using (WordprocessingDocument doc = WordprocessingDocument.Open(filePath, true))
                {
                    var body = doc.MainDocumentPart.Document.Body;

                    // Regex explanation:
                    // ^\s* : Start of line (ignoring leading whitespace)
                    // (
                    //   \d{2}:\d{2}:\d{2},\d{3}     : Matches timestamps like 00:00:50,900
                    //   |                           : OR
                    //   -->\s*\d{2}:\d{2}:\d{2},\d{3}\s*\[speaker_.*\] : Matches lines like --> 00:01:12,630 [speaker_0]
                    // )
                    // \s*$          : End of line (ignoring trailing whitespace)
                    var pattern = @"^\s*(\d{2}:\d{2}:\d{2},\d{3}|-->\s*\d{2}:\d{2}:\d{2},\d{3}\s*\[speaker_.*\])\s*$";
                    var regex = new Regex(pattern, RegexOptions.IgnoreCase);

                    int count = 0;

                    // Iterate through every paragraph in the document
                    foreach (var para in body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
                    {
                        // Check the full text of the paragraph
                        string text = para.InnerText;

                        if (!string.IsNullOrWhiteSpace(text) && regex.IsMatch(text))
                        {
                            // If it matches, loop through all "Runs" (text chunks) in the paragraph and bold them
                            foreach (var run in para.Elements<Run>())
                            {
                                RunProperties runProps = run.RunProperties;
                                if (runProps == null)
                                {
                                    runProps = new RunProperties();
                                    run.PrependChild(runProps);
                                }

                                // Apply Bold formatting
                                runProps.Bold = new Bold();
                            }
                            count++;
                        }
                    }

                    // Save changes
                    doc.MainDocumentPart.Document.Save();
                    Console.WriteLine($"Done! Bolded {count} lines.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }
}