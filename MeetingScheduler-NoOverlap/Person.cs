using System;
using System.IO;

namespace MeetingScheduler
{
    internal class Person
    {
        public Person(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
    }

    internal class PersonAvailability
    {
        public PersonAvailability(Person person, DateTime from, DateTime to)
        {
            Person = person;
            From = from;
            To = to;
            Duration = to - from;
        }

        public static bool TryParse(string data, out PersonAvailability personAvailability)
        {
            personAvailability = null;
            if (string.IsNullOrEmpty(data)) return false;
            var splits = data.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (splits.Length < 4) return false;
            for (int i = 0; i < splits.Length; i++)
            {
                splits[i] = splits[i].Trim();
            }
            var person = new Person(splits[0]);
            DateTime date;
            TimeSpan from, to;
            if (!DateTime.TryParseExact(splits[1], "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.AssumeLocal, out date) ||
                !TimeSpan.TryParse(splits[2], out from) ||
                !TimeSpan.TryParse(splits[3], out to) ||
                to < from)
                return false;
            personAvailability = new PersonAvailability(person, date.Add(from), date.Add(to));
            return true;
        }

        public static PersonAvailability Parse(string data)
        {
            PersonAvailability personAvailability;
            if (!TryParse(data, out personAvailability))
                throw new InvalidDataException(string.Format("Invalid row in availability data: {0}", data));
            return personAvailability;
        }

        public Person Person { get; private set; }
        public DateTime From { get; private set; }
        public DateTime To { get; private set; }
        public TimeSpan Duration { get; private set; }
    }

    internal class PersonAvailabilityEvent : IComparable<PersonAvailabilityEvent>
    {
        public PersonAvailabilityEvent(bool isStart, DateTime timestamp, PersonAvailability personAvailability)
        {
            IsStart = isStart;
            Timestamp = timestamp;
            PersonAvailability = personAvailability;
        }

        public bool IsStart { get; private set; }
        public DateTime Timestamp { get; private set; }
        public PersonAvailability PersonAvailability { get; private set; }

        public int CompareTo(PersonAvailabilityEvent other)
        {
            if (other == null) return -1;
            return Timestamp.CompareTo(other.Timestamp);
        }
    }

}
