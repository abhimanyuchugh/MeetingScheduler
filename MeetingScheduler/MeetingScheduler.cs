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
            Dictionary<string, int> currAttendeesEventCount;
            var availabilityEvents = GetAvailabilityEvents(personAvailabilities, attendees, fromDate, toDate, 
                                                           duration, out currAttendeesEventCount);
            // if any attendee doesn't exist after the above filtering, terminate early
            // since we won't be able to schedule meeting with everyone
            if (currAttendeesEventCount.Count != attendees.Count)
                return false;
            // Sort by event time
            availabilityEvents.Sort();
            DateTime maxStartTime = fromDate;
            var currAttendeesAvailable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var availabilityEvent in availabilityEvents)
            {
                var person = availabilityEvent.PersonAvailability.Person;
                // get the current event count for this person
                int eventCount;
                if (!currAttendeesEventCount.TryGetValue(person.Name, out eventCount))
                {
                    eventCount = 0;
                }
                if (availabilityEvent.IsStart)
                {
                    // if it's a start event and we have already encountered another start event for this person,
                    // this must mean they overlap and we can effectively consider them to be a single longer event
                    // hence, we don't need to compare the second start event's time against maxStartTime
                    if (eventCount <= 0 && maxStartTime < availabilityEvent.Timestamp)
                    {
                        maxStartTime = availabilityEvent.Timestamp;
                    }
                    // increment event count and mark person as available
                    currAttendeesEventCount[person.Name] = ++eventCount;
                    currAttendeesAvailable.Add(person.Name);
                    continue;
                }
                // if it's an end event and we haven't yet encountered a start event for this person, skip it
                if (eventCount <= 0)
                    continue;
                currAttendeesEventCount[person.Name]--;
                // if we've encountered multiple overlapping start events for this person,
                // we should only look at the latest end event for them
                // since they can effectively be merged into a single longer event (as per above comment)
                if (eventCount == 1)
                {
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
            }
            return false;
        }

        private List<PersonAvailabilityEvent> GetAvailabilityEvents(List<PersonAvailability> personAvailabilities, HashSet<string> attendees,
            DateTime fromDate, DateTime toDate, TimeSpan duration, out Dictionary<string, int> currAttendeesEventCount)
        {
            var availabilityEvents = new List<PersonAvailabilityEvent>();
            currAttendeesEventCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var personAvailability in personAvailabilities)
            {
                // ignore availabilities for people not in the attendees list or those with shorter durations than required
                // or outside the given date range
                if (!attendees.Contains(personAvailability.Person.Name) ||
                    personAvailability.Duration < duration || personAvailability.From > toDate ||
                    personAvailability.To < fromDate)
                    continue;
                int eventCount;
                if (!currAttendeesEventCount.TryGetValue(personAvailability.Person.Name, out eventCount))
                    currAttendeesEventCount[personAvailability.Person.Name] = 0;
                // create start and end events
                availabilityEvents.Add(new PersonAvailabilityEvent(true, personAvailability.From, personAvailability));
                availabilityEvents.Add(new PersonAvailabilityEvent(false, personAvailability.To, personAvailability));
            }
            return availabilityEvents;
        }
    }
}
