using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
            Dictionary<string, List<string>> translations = new Dictionary<string, List<string>>();

            int cnt = 1;
            foreach (string word in words) {
                List<string> translation = translate(word);

                System.Console.WriteLine("[" + cnt + "] Get word '" + word + "'...");

                // handle duplicate words
                if (translations.ContainsKey(word)) {
                    System.Console.WriteLine("WARNING: duplicate word '" + word + "' found...");
                    translations.Add("line: " + cnt, new List<string>() {"DOUBLEKEYERR: " + word});
                    continue;
                }


                if (translation != null) {
                    translations.Add(word, translation);
                } else {
                    System.Console.WriteLine("WARNING: '" + word + "' not found...");
                    translations.Add(word, new List<string>() {""});
                }

                cnt++;
            }

            // write result to a file
            System.IO.File.WriteAllLines("outfile.csv", translations
                            .Select(x => x.Key + ";" + toNumberListString(x.Value).Replace("&nbsp","")));

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
