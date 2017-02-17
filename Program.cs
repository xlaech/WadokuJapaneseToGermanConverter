using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace ConsoleApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("This program will look up all the translations for the 25k list");

            string infile = "25kwordsin.csv";

            string[] words = System.IO.File.ReadAllLines(infile);
            Dictionary<int, Tuple<string, Task<string>>> translations = new Dictionary<int, Tuple<string, Task<string>>>();

            int cnt = 0;
            foreach (string word in words) {
                cnt++;
                
                // Start a Task for getting the word
                System.Console.WriteLine("[" + cnt + "|" + words.Length + "] Task for '" + word + "' created...");
                Task<string> translation = asyncGetTrans(word);
                translation.Start();
                translations.Add(cnt, new Tuple<string, Task<string>>(word, translation));
            }
                
            // while not all Tasks have finished
            while(translations.Values
                              .Select(x => x.Item2.IsCompleted)
                              .Aggregate((x,y) => x && y)) 
            {
                // get number of threads finished
                int completedThreads = translations.Values
                                                 .Select(x => x.Item2.IsCompleted ? 1 : 0)
                                                 .Aggregate((x,y) => x + y); 

                System.Console.WriteLine("[" + completedThreads + "|" + translations.Count + "] finished");
                Thread.Sleep(3000);
            }

            // write result to a file
            System.IO.File.WriteAllLines("outfile.csv", translations.Values
                            .Select(x => x.Item1 + ";" + x.Item2.Result));

	    }

        static Task<string> asyncGetTrans(string word) {
            return new Task<string>(() => {
                System.Console.WriteLine("Start search for word '" + word + "'...");
                List<string> translation = translate(word);

                if (translation == null) {
                    System.Console.WriteLine("Word '" + word + "' was not found");
                    return "";
                }

                return toNumberListString(translation)
                            .Replace("&nbsp","")
                            .Replace("<br>","")
                            .Replace("<br/>","");
            });
        }

        /// <SUMMARY>
        /// Transforms a list of Strings in an enumberated string
        /// eg. "1. hallo 2. test 3. jojo"
        /// </summary>
        /// <returns>Enumerated String</returns>
        /// <param name="inList">List of strings to work with</param>
        static string toNumberListString(List<string> inList) 
        {
            if (inList.Count == 1)
                return inList.First();

            string retString = "";
            for (int i=1; i <= Math.Min(inList.Count, 5); i++)
                retString += i + ". " + inList[i-1] + " ";

            return retString;
        }

        /// <SUMMARY>
        /// Tries to look up a japanese word on wadoku
        /// </summary>
        /// <returns>The words translation if found, null else</returns>
        /// <param name="word">The japanese word to search for</param>
        static List<string> translate(string word)
        {
            string url = "https://www.wadoku.de/search/" + word;
            List<string> translations = new List<string>();

            var html = new HtmlDocument();
            var httpClient = new HttpClient();
            var result = httpClient.GetStringAsync(url);

            //load Data from page
            html.LoadHtml(result.Result);
            var root = html.DocumentNode;

            try {
                // Get the first translation found
                HtmlNode section = root.Descendants("section")
                                    .Where(x => x.GetAttributeValue("class", "").Equals("senses")).First();

                // for each specific translation, only get the first one
                foreach(HtmlNode sense in section.Descendants("span")
                                                 .Where(x => x.GetAttributeValue("class", "").Equals("sense"))) 
                {
                    translations.Add(sense.InnerText.Split(';')[1].Replace(".",""));
                }
            } catch(Exception) {
                // we couldn't find a translation
                return null;
            }

            return translations;
        }
    }
}
