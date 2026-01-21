using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Drag and drop a file here and press Enter:");
        string input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
            return;


        string filePath = input.Trim().Trim('"');
        //string filePath = @"C:\Users\ddkat\Desktop\NormalizedInterviews\Interview_BETAHAUS _Parental leave_ work-life balance.docx";

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
                foreach (var para in body.Elements<Paragraph>())
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