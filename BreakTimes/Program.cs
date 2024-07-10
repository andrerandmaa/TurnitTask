using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Turnit
{
    public class Program
    {

        private static (DateTime Start, DateTime End)? ParseTimeEntry(string entry, string pattern)
        {
            try
            {
                if (Regex.IsMatch(entry, pattern))
                {
                    string startTime = entry.Substring(0, 5), endTime = "";
                    if (entry.Contains('-')) // For file format
                    {
                        endTime = entry.Substring(6);
                    }
                    else // For user input format
                    {
                        endTime = entry.Substring(5);
                    }

                    if (startTime != endTime)
                    {
                        // Turns inputs into DateTime format. Program can be expanded more easily with this in the future.
                        var start = DateTime.ParseExact(startTime, "HH:mm", null);
                        var end = DateTime.ParseExact(endTime, "HH:mm", null);

                        return (start, end);
                    }
                    else  // No point in registering break time which doesn't even last 1 minute.
                    { 
                        Console.WriteLine("End time can't be the same as start time.");
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }

        private static (string Start, string End, int DriverCount) FindBusiestPeriod(List<(DateTime Start, DateTime End)> timeEntries)
        {
            var events = new List<(DateTime Time, int StartOrEnd)>();

            foreach (var entry in timeEntries)
            {
                events.Add((entry.Start, 0));  // Start of a driver's time
                events.Add((entry.End, 1));   // End of a driver's time
            }

            // Sort first by DateTimes, then put Start times before End times.
            events = events.OrderBy(e => e.Time).ThenBy(e => e.StartOrEnd).ToList();

            int maxDrivers = 0, currentDrivers = 0, driversLeftAfterBreakEnd = 0;
            DateTime startBusiest = DateTime.MinValue, endBusiest = DateTime.MinValue;

            // Logic for finding busiest period start and end with number of drivers
            // Looking at task requirements, if there are multiple time periods with same number of drivers on break...
            // ... then display as the busiest period only the earliest time period.
            foreach (var e in events)
            {
                if (e.StartOrEnd == 0)
                {
                    currentDrivers += 1;
                } 
                else if (e.StartOrEnd == 1)
                {
                    currentDrivers -= 1;
                }

                if (currentDrivers > maxDrivers && e.StartOrEnd == 0)
                {
                    maxDrivers = currentDrivers;
                    startBusiest = e.Time;
                }
                else if (driversLeftAfterBreakEnd < currentDrivers && e.StartOrEnd == 1)
                {
                    driversLeftAfterBreakEnd = currentDrivers;
                    endBusiest = e.Time;
                } 
                else if (events.Count == 2 && e.StartOrEnd == 1) // Edge case, where the file is empty
                {
                    endBusiest = e.Time;
                }
            }
            // Turning DateTime format (dd.mm.yyyy HH:mm:ss) into string HH:mm
            string formattedStart = (startBusiest.ToString().Split(" ")[1].Split(":")[0] + ":" + startBusiest.ToString().Split(" ")[1].Split(":")[1]);
            string formattedEnd = (endBusiest.ToString().Split(" ")[1].Split(":")[0] + ":" + endBusiest.ToString().Split(" ")[1].Split(":")[1]);

            return (formattedStart, formattedEnd, maxDrivers);
        }

        private static string SearchFileContents(string filePath, List<(DateTime Start, DateTime End)> timeEntries)
        {
            try
            {
                string[] lines = File.ReadAllLines(filePath);
                timeEntries.Clear();

                foreach (string line in lines)
                {
                    // Pattern for accepted time format (example 00:00-23:59)
                    string pattern = @"^([01]\d|2[0-3]):[0-5]\d-([01]\d|2[0-3]):[0-5]\d$";
                    var times = ParseTimeEntry(line, pattern);

                    if (times != null)
                    {
                        timeEntries.Add(times.Value);
                    }
                    else
                    {
                        Console.WriteLine("Invalid time format in file: {0}", line);
                    }
                }

                return filePath;
            }
            catch (Exception e) // Handle any errors that might occur
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
                return "";
            }
        }

        private static void BreakTimeInput(string filePath, string[] input, List<(DateTime Start, DateTime End)> timeEntries)
        {
            // Pattern for accepted time format (example 00:0023:59)
            string pattern = @"^([01]\d|2[0-3]):[0-5]\d([01]\d|2[0-3]):[0-5]\d$";
            var times = ParseTimeEntry(input[0], pattern);

            if (times != null)
            {
                // Write user input (example 00:0023:59) into file with required formatting (example 00:00-23:59)
                using (StreamWriter writer = new StreamWriter(filePath, true))
                {
                    string reformattedInput = input[0].Substring(0, 5) + "-" + input[0].Substring(5);
                    writer.WriteLine(reformattedInput);
                }
                timeEntries.Add(times.Value);

                // Find new busiest period immediately after adding the new time input
                var busiestPeriod = FindBusiestPeriod(timeEntries);
                Console.WriteLine($"Busiest period: {busiestPeriod.Start}-{busiestPeriod.End} with {busiestPeriod.DriverCount} drivers taking a break.");
            }
            else
            {
                Console.WriteLine("Invalid format. Please enter times in the format <start time><end time> (example 13:1514:00).");
            }
        }

        public static void Main(string[] args)
        {
            List<(DateTime Start, DateTime End)> timeEntries = new List<(DateTime Start, DateTime End)>();

            Console.WriteLine("Program started.");
            Console.WriteLine("You can exit the application any time by typing 'exit'.");
            Console.WriteLine("Usage: filename <file path>");
            string filePath = "";

            // Command line application logic. Requires first input to be file, which can be read in.
            // After reading in the file, accepts user inputs for new times.
            while (true)
            {
                string[] input = Console.ReadLine().Trim().Split(' ');
                if (input[0].ToLower() == "exit")
                {
                    break;
                }

                else if (input[0].ToLower() == "filename" && input.Length == 2)
                {
                    filePath = SearchFileContents(input[1], timeEntries);
                    if (filePath != "")
                    {
                        if (timeEntries.Count > 0)
                        {
                            var busiestPeriod = FindBusiestPeriod(timeEntries);
                            Console.WriteLine($"Busiest period: {busiestPeriod.Start}-{busiestPeriod.End} with {busiestPeriod.DriverCount} drivers taking a break.");
                            Console.WriteLine("You can input new times into the file in format <start time><end time> (example 13:1514:00).");
                        }
                        else
                        {
                            Console.WriteLine($"No registered times for drivers taking a break.");
                            Console.WriteLine("You can input new times into the file in format <start time><end time> (example 13:1514:00).");
                        }
                    }
                }
                else if (filePath == "")
                {
                    Console.WriteLine("Usage: filename <file path>");
                }
                else if (filePath != "" && input.Length == 1)
                {
                    BreakTimeInput(filePath, input, timeEntries);
                } else
                {
                    Console.WriteLine("Unrecognized input. Please enter times in the format <start time><end time> (example 13:1514:00).");
                }
            }
        }
    }
}