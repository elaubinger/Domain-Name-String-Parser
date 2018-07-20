using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace DomainStringParser
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                #region Parse Arguments
                string name = (args.Length > 0) ? args.First() : throw new ArgumentNullException("Proposed name must be provided as first argument");
                if (String.IsNullOrWhiteSpace(name)) throw new ArgumentException("Proposed name cannot be null or whitespace");
                name = name.ToLower();

                bool useOffline = args?.Contains("-offline") ?? false;
                #endregion

                #region Get Sorted List of Domains
                string fileContents = null;

                if (!useOffline)
                {
                    #region Get Domains From Online Source
                    string uri = @"http://data.iana.org/TLD/tlds-alpha-by-domain.txt";
                    Console.WriteLine($"Fetching top level domains from {uri}");
                    try
                    {
                        var request = WebRequest.CreateHttp(uri);
                        request.Credentials = new NetworkCredential();

                        var response = request.GetResponse();
                        var responseStream = response.GetResponseStream();
                        var reader = new StreamReader(responseStream);
                        fileContents = reader.ReadToEnd();

                        reader.Close();
                        reader.Dispose();
                        response.Close();
                    }
                    catch (Exception ex)
                    {
                        PrintException($"Exception whilst fetching top level domains", ex);
                        useOffline = true;
                    }
                    #endregion
                }

                if (useOffline)
                {
                    #region Get Domains From Local Source
                    string offlineFileName = @"offline_domains.txt";
                    var file = new FileInfo(offlineFileName);
                    if (!file.Exists) throw new FileNotFoundException($"{offlineFileName} was not found");
                    var reader = new StreamReader(file.OpenRead());
                    fileContents = reader.ReadToEnd();
                    #endregion
                }

                fileContents = fileContents ?? throw new FileNotFoundException("Could not find either online or offline source of top level domains");
                
                #region Parse File Contents
                var domainHashset = new HashSet<string>();

                // Parse file into domains
                foreach (var line in fileContents.Split(separator:
                    new string[] {
                        Environment.NewLine, "\n", "\r\n"
                    },
                    count: int.MaxValue,
                    options: StringSplitOptions.RemoveEmptyEntries
                    ))
                {
                    var eval = line.Trim().ToLower();

                    // Skip if empty
                    if (eval.Length == 0) continue;

                    // Skip if comment
                    if (eval.First() == '#') continue;
                    
                    if (!domainHashset.Contains(eval)) domainHashset.Add(eval);
                }

                // Sort domains alphabetically
                var domains = new List<string>(domainHashset);
                domains.Sort();
                #endregion
                #endregion

                Console.WriteLine("Finding matching domains");

                #region Check Domain Matches By Strength
                var matchesByStrength = new List<Tuple<string, Match>>();
                foreach(var domain in domains)
                {
                    for (int i = 0; i < domain.Length; i++)
                    {
                        var match = Regex.Match(name, $"{domain.Substring(0,domain.Length - i)}$");
                        if (match.Success)
                        {
                            matchesByStrength.Add(new Tuple<string, Match>(domain, match));
                            break;
                        }
                    }
                }

                matchesByStrength.Sort(
                    (a, b) =>
                    {
                        // Sort first by size of match preferring larger
                        int sizeCompare = -a.Item2.Length.CompareTo(b.Item2.Length);
                        if (sizeCompare != 0) return sizeCompare;

                        // Next sort by percent of name match represents
                        double
                            aNameRatio = name.Length / (double)a.Item2.Length,
                            bNameRatio = name.Length / (double)b.Item2.Length;

                        int ratioCompare = aNameRatio.CompareTo(bNameRatio);
                        if (ratioCompare != 0) return ratioCompare;

                        // Finally prefer shorter domain names
                        return a.Item1.Length.CompareTo(b.Item1.Length);
                    }
                );

                #endregion

                #region Format and Print List
                foreach(var match in matchesByStrength)
                    Console.WriteLine($"{name.Substring(0, name.Length - match.Item2.Length)}.{match.Item1}");
                if(matchesByStrength.Count == 0) Console.WriteLine("No matching domains found");
                #endregion

                Console.WriteLine("Press any key to exit");

                Console.Read();
            }
            catch (Exception ex)
            {
                PrintException($"Unexpected Exception occured", ex);
            }
        }

        private static void PrintException(string message, Exception ex) => Console.WriteLine($"{message} ({ex.GetType()}): {Environment.NewLine}{ex.StackTrace}");
    }
}
