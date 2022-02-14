using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AzureDevopsCommitsParser
{
    class Program
    {
        static List<AdoEntry> entries = new List<AdoEntry>();

        static List<string> ommitTitles = new List<string>() { "Update Environment_pipeline.yml for Azure Pipelines", "Merge branch '", "Update azure-pipelines-investigate.yml for Azure Pipelines" };

        static async Task Main(string[] args)
        {
            // 1. open commits on ADO (e.g. https://dev.azure.com/{client}/_git/{project}/commits?user=Lukasz%20Fliegel&userId=e9947b46-88ca-6ed2-aa2b-f982a8715a10)
            // 2. scroll down to load all commits (filter to a month if not all are exported
            // 3. ctrl+s - save file (put it into \inputs\Commits.html) (confirm all rows were exported) (if not all wre exported scroll up, save additional file, repeat :))
            // 4. rename file (to break link with folder, remove folder)
            // 5. open file (notpad++) replace "><" with ">\n<"
            // 6. set files to "copy always" in VS properties

            //var fileNames = new List<string> { "Input/commits.html", "Input/commits2.html" };
            // november
            var fileNames = new List<string> { "Input/Commits_nov.html", "Input/Commits2_nov.html", "Input/Commits3_nov.html", "Input/Commits4_nov.html" };
            //var fileName = "Input/commits.html";
            //await ExtractFile(fileName);

            //fileName = "Input/commits2.html";
            //await ExtractFile(fileName);

            foreach (var fileName in fileNames)
            {
                await ExtractFile(fileName);
            }

            foreach (var entry in entries.Distinct())
            {
                Console.WriteLine($"{entry.ParsedDateTime.ToString("MMMM")};{entry.ParsedDateTime.Year};{entry.Title};;{entry.Url};{entry.ExtractedDateTime}");
            }
        }

        private static async Task ExtractFile(string fileName)
        {
            using (var sr = new StreamReader(fileName))
            {
                AdoEntry extractedEntry = new AdoEntry();

                while (!sr.EndOfStream)
                {                    
                    var line = await sr.ReadLineAsync();
                    if(line.Contains("class=\"bolt-table-row bolt-list-row single-click-activation v-align-middle selectable-text\""))
                    {
                        // new entry, so lets's save extracted entry
                        if (extractedEntry != null && !string.IsNullOrEmpty(extractedEntry.Title) && !ommitTitles.Any(p => extractedEntry.Title.Contains(p)))
                            entries.Add(extractedEntry);

                        extractedEntry = new AdoEntry();
                        var url = ExtractUrl(line, "<a href=\"");
                        if (!string.IsNullOrEmpty(url))
                            extractedEntry.Url = url;
                    }
                    var textToFindAndRemove = "<div class=\"commit-title text-ellipsis font-weight-semibold flex-grow\">";
                    var title = ExtractInnerPartOfHtml(line, textToFindAndRemove);
                    if(!string.IsNullOrEmpty(title))
                        extractedEntry.Title = title;

                    textToFindAndRemove = "<time class=\"bolt-time-item white-space-nowrap\" datetime=";
                    var date = ExtractInnerPartOfHtml(line, textToFindAndRemove);
                    if (!string.IsNullOrEmpty(date))
                    {
                        extractedEntry.ExtractedDateTime = date.Remove(date.IndexOf("at") - 1);
                    }                    
                }
            }
        }

        private static string ExtractInnerPartOfHtml(string line, string textToFindAndRemove)
        {
            if (line.Contains(textToFindAndRemove))
            {
                var startIndex = line.IndexOf(textToFindAndRemove);
                var endIndex = line.IndexOf('>');
                line = line.Remove(startIndex, endIndex - startIndex + 1);
                line = line.Replace("</div>", "");
                line = line.Replace("</time>", "");
                return line;
            }

            return string.Empty;
        }

        private static string ExtractUrl(string line, string textToFindAndRemove)
        {
            if (line.Contains(textToFindAndRemove))
            {
                var startIndex = line.IndexOf(textToFindAndRemove);
                var endIndex = line.IndexOf('"');
                line = line.Remove(startIndex, endIndex - startIndex + 1);
                var newStartIndex = line.IndexOf('"');
                line = line.Remove(newStartIndex);
                line = line.Replace("</div>", "");
                line = line.Replace("</time>", "");
                return line;
            }

            return string.Empty;
        }
    }

    class AdoEntry : IEquatable<AdoEntry>
    {
        public string Title { get; set; }
        public string ExtractedDateTime { get; set; }
        public DateTime ParsedDateTime { get => DateTime.Parse(ExtractedDateTime); }
        public string Url { get; set; }

        public bool Equals(AdoEntry other)
        {
            return other.ExtractedDateTime == ExtractedDateTime
                && other.Title == Title
                && other.Url == Url;
        }
    }
}
