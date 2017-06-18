using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace MeetingScheduler
{
    class Program
    {
        static void Main(string[] args)
        {
            HashSet<string> attendees;
            DateTime fromDate, toDate;
            TimeSpan duration;
            if (!TryParseInput(args, out attendees, out fromDate, out toDate, out duration))
                return;
            var personAvailabilities = ReadPersonAvailabilityData();
            if (personAvailabilities == null || personAvailabilities.Count == 0)
            {
                Console.WriteLine("No person availability data found!");
                return;
            }
            DateTime scheduledTime;
            string message;
            var meetingScheduler = new MeetingScheduler();
            if (meetingScheduler.TryScheduleMeeting(personAvailabilities, attendees, fromDate, toDate, duration, out scheduledTime))
            {
                message = string.Format("A meeting can be scheduled at: {0}", scheduledTime.ToString("yyyy-MM-dd HH:mm"));
            }
            else
            {
                message = string.Format("It is not possible to schedule a meeting for these attendees with the given date range and duration.");
            }
            Console.WriteLine();
            Console.WriteLine("------------------------------------------");
            Console.WriteLine("RESULT: " + message);
            Console.WriteLine("------------------------------------------");
            Console.WriteLine();
            Console.WriteLine("Press any key to terminate...");
            Console.ReadKey();
        }

        private static bool TryParseInput(string[] args, out HashSet<string> attendees, out DateTime fromDate, out DateTime toDate, out TimeSpan duration)
        {
            const string message = "Please pass in 4 parameters in this format: [ListOfAttendees: comma-separated] [FromDate: yyyy-MM-dd] [ToDate: yyyy-MM-dd] [Duration: hh:mm]";
            attendees = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            fromDate = DateTime.MinValue;
            toDate = DateTime.MinValue;
            duration = TimeSpan.Zero;
            if (args == null || args.Length < 4)
            {
                Console.WriteLine(message);
                return false;
            }

            var attendeesStr = args[0];
            var fromDateStr = args[1];
            var toDateStr = args[2];
            var durationStr = args[3];

            var attendeesArray = attendeesStr.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var attendee in attendeesArray)
            {
                var attendeeTrimmed = attendee.Trim();
                if (!string.IsNullOrEmpty(attendeeTrimmed))
                    attendees.Add(attendeeTrimmed);
            }
            if (attendees.Count == 0 ||
                !DateTime.TryParseExact(fromDateStr, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.AssumeLocal, out fromDate) ||
                !DateTime.TryParseExact(toDateStr, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.AssumeLocal, out toDate) ||
                !TimeSpan.TryParse(durationStr, out duration))
            {
                Console.WriteLine(message);
                return false;
            }
            return true;
        }

        private static List<PersonAvailability> ReadPersonAvailabilityData()
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = GetAvailabilityDataResourceName(assembly);
            using (var resourceStream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(resourceStream))
            {
                var result = new List<PersonAvailability>();
                string line = reader.ReadLine(); // Ignore first header line
                while(line != null && (line = reader.ReadLine()) != null)
                {
                    PersonAvailability personAvailability;
                    if (!PersonAvailability.TryParse(line, out personAvailability))
                        continue;
                    result.Add(personAvailability);
                }
                return result;
            }
        }

        private static string GetAvailabilityDataResourceName(Assembly assembly)
        {
            string resourceName = "Data.AvailabilityData.csv";
            return assembly.GetName().Name + "." + resourceName;
        }
    }
}
