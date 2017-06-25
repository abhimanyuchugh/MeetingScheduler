using System;
using System.Collections.Generic;

namespace MeetingScheduler
{
    internal class MeetingScheduler
    {
        public bool TryScheduleMeeting(List<PersonAvailability> personAvailabilities, HashSet<string> attendees,
            DateTime fromDate, DateTime toDate, TimeSpan duration, out DateTime scheduledTime)
        {
            scheduledTime = DateTime.MinValue;
            bool allAttendeesCovered;
            var availabilityEvents = GetAvailabilityEvents(personAvailabilities, attendees, fromDate, toDate, duration, out allAttendeesCovered);
            // if any attendee doesn't exist after the above filtering, terminate early
            // since we won't be able to schedule meeting with everyone
            if (!allAttendeesCovered)
                return false;
            // Sort by event time
            availabilityEvents.Sort();
            DateTime maxStartTime = fromDate;
            var currAttendeesAvailable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var availabilityEvent in availabilityEvents)
            {
                var person = availabilityEvent.PersonAvailability.Person;
                if (availabilityEvent.IsStart)
                {
                    if (maxStartTime < availabilityEvent.Timestamp)
                        maxStartTime = availabilityEvent.Timestamp;
                    // mark person as available
                    currAttendeesAvailable.Add(person.Name);
                    continue;
                }
                var isEndEventOutsideDateRange = availabilityEvent.Timestamp > toDate;
                var endTime = isEndEventOutsideDateRange ? toDate : availabilityEvent.Timestamp;
                // if all attendees are available and the current overlapping availability is
                // the same length as (or more than) the duration, we've found a meeting time
                if (currAttendeesAvailable.Count == attendees.Count && maxStartTime != null &&
                    (endTime - maxStartTime) >= duration)
                {
                    scheduledTime = maxStartTime;
                    return true;
                }
                // if the end event has reached the date limit, there's no need to continue
                // since this person wouldn't be available anymore so we would never find a valid meeting time
                if (isEndEventOutsideDateRange)
                    return false;
                // mark person as unavailable
                currAttendeesAvailable.Remove(person.Name);
            }
            return false;
        }

        private List<PersonAvailabilityEvent> GetAvailabilityEvents(List<PersonAvailability> personAvailabilities, HashSet<string> attendees,
            DateTime fromDate, DateTime toDate, TimeSpan duration, out bool allAttendeesCovered)
        {
            var attendeesNotCovered = new HashSet<string>(attendees);
            var availabilityEvents = new List<PersonAvailabilityEvent>();
            foreach (var personAvailability in personAvailabilities)
            {
                // ignore availabilities for people not in the attendees list or those with shorter durations than required
                // or outside the given date range
                if (!attendees.Contains(personAvailability.Person.Name) ||
                    personAvailability.Duration < duration || personAvailability.From > toDate ||
                    personAvailability.To < fromDate)
                    continue;
                attendeesNotCovered.Remove(personAvailability.Person.Name);
                // create start and end events
                availabilityEvents.Add(new PersonAvailabilityEvent(true, personAvailability.From, personAvailability));
                availabilityEvents.Add(new PersonAvailabilityEvent(false, personAvailability.To, personAvailability));
            }
            allAttendeesCovered = attendeesNotCovered.Count == 0;
            return availabilityEvents;
        }
    }
}
