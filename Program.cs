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

            string infile = @"25kwordsin.csv";

            string[] words = System.IO.File.ReadAllLines(infile);
            Dictionary<string, string> translations = new Dictionary<string, string>();

            foreach (string word in words) {
                string translation = translate(word);

                if (translation != null) 
                    translations.Add(word, translation);
                else
                    System.Console.WriteLine("WARNING: '" + word + "' not found...");
            }

	    }

        /// <SUMMARY>
        /// Tries to look up a japanese word on wadoku
        /// </summary>
        /// <returns>The words translation if found, null else</returns>
        /// <param name="word">The japanese word to search for</param>
        static string translate(string word)
        {
            string url = "https://www.wadoku.de/search/" + word;
            string translation = "";

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
                    translation += sense.InnerHtml.Split(';').First();
            } catch(Exception e) {
                // we couldn't find a translation
                return null;
            }

            return translation;
        }
    }
}
