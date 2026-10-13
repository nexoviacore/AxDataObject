using Newtonsoft.Json;
using System.Globalization;

namespace ARMAxpertJobsWorkerService
{
    public class JobScheduler
    {
        public DateTime GetNextOccurrence(string scheduleJson)
        {
            try
            {
                dynamic schedule = JsonConvert.DeserializeObject(scheduleJson);

                //DateTime startDate = DateTime.ParseExact(
                //    schedule.startDate.ToString(),
                //    "dd/MM/yyyy",
                //    CultureInfo.InvariantCulture
                //);
                string[] formats = { "dd/MM/yyyy", "dd-MM-yyyy" };
                DateTime startDate;
                if (!DateTime.TryParseExact(schedule.startDate.ToString(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate))
                {
                    throw new Exception("Invalid start date");
                }

                string period = schedule.period?.ToString()?.Trim().ToLower();
                string sendOnValue = schedule.sendOn?.ToString();
                string firstDayOfWeekValue = schedule.firstDayOfWeek?.ToString();

                TimeSpan sendTime = GetTimeSpan(schedule.sendTime.ToString());

                DateTime currentTime = DateTime.Now;

                DateTime nextOccurrence = startDate.Date;

                if (nextOccurrence < currentTime.Date)
                    nextOccurrence = currentTime.Date;

                switch (period)
                {
                    case "daily":
                    case "every day":
                        if (nextOccurrence.Date + sendTime <= currentTime)
                            nextOccurrence = nextOccurrence.AddDays(1);
                        break;

                    case "weekly":
                    case "every week":
                        DayOfWeek sendOn = Enum.Parse<DayOfWeek>(sendOnValue, true);
                        nextOccurrence = GetNextDayOfWeek(nextOccurrence, sendOn, sendTime, currentTime);
                        break;

                    case "monthly":
                    case "every month":
                        nextOccurrence = HandleMonthly(nextOccurrence, sendOnValue, sendTime, currentTime, firstDayOfWeekValue);
                        break;

                    case "quarterly":
                    case "every quarter":
                        nextOccurrence = HandleQuarterly(nextOccurrence, sendOnValue, sendTime, currentTime);
                        break;

                    case "yearly":
                        while (startDate.Date + sendTime <= currentTime)
                            startDate = startDate.AddYears(1);

                        nextOccurrence = startDate;
                        break;

                    case "custom":
                        int minutes = Convert.ToInt32(sendOnValue);
                        nextOccurrence = startDate;

                        while (nextOccurrence + sendTime <= currentTime)
                            nextOccurrence = nextOccurrence.AddMinutes(minutes);

                        return nextOccurrence + sendTime;

                    default:
                        throw new Exception("Invalid period specified");
                }

                return nextOccurrence.Date + sendTime;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Scheduler Exception: {ex}");
                return DateTime.Now.AddMinutes(1);
            }
        }

        private DateTime HandleMonthly(
            DateTime baseDate,
            string sendOn,
            TimeSpan sendTime,
            DateTime currentTime,
            string firstDayOfWeekValue)
        {
            if (string.IsNullOrEmpty(sendOn))
                return baseDate;

            sendOn = sendOn.ToLower();

            if (sendOn == "first day")
            {
                DateTime candidate = new DateTime(baseDate.Year, baseDate.Month, 1);
                if (candidate + sendTime <= currentTime)
                    candidate = candidate.AddMonths(1);

                return candidate;
            }

            if (sendOn == "last day")
            {
                DateTime candidate = new DateTime(
                    baseDate.Year,
                    baseDate.Month,
                    DateTime.DaysInMonth(baseDate.Year, baseDate.Month)
                );

                if (candidate + sendTime <= currentTime)
                    candidate = candidate.AddMonths(1);

                return candidate;
            }

            if (sendOn == "first day of last week")
            {
                DayOfWeek firstDayOfWeek = Enum.Parse<DayOfWeek>(firstDayOfWeekValue, true);

                DateTime lastDay = new DateTime(
                    baseDate.Year,
                    baseDate.Month,
                    DateTime.DaysInMonth(baseDate.Year, baseDate.Month)
                );

                int offset = (7 + (int)lastDay.DayOfWeek - (int)firstDayOfWeek) % 7;
                DateTime candidate = lastDay.AddDays(-offset);

                if (candidate + sendTime <= currentTime)
                    candidate = candidate.AddMonths(1);

                return candidate;
            }

            if (int.TryParse(sendOn, out int day))
            {
                DateTime candidate = new DateTime(baseDate.Year, baseDate.Month, 1)
                    .AddDays(day - 1);

                if (candidate + sendTime <= currentTime)
                    candidate = candidate.AddMonths(1);

                return candidate;
            }

            return baseDate;
        }

        private DateTime HandleQuarterly(
            DateTime baseDate,
            string sendOn,
            TimeSpan sendTime,
            DateTime currentTime)
        {
            int quarterStartMonth = ((baseDate.Month - 1) / 3) * 3 + 1;

            DateTime quarterStart = new DateTime(baseDate.Year, quarterStartMonth, 1);
            DateTime quarterEnd = quarterStart.AddMonths(3).AddDays(-1);

            if (sendOn.ToLower() == "first day")
            {
                if (quarterStart + sendTime <= currentTime)
                    quarterStart = quarterStart.AddMonths(3);

                return quarterStart;
            }

            if (sendOn.ToLower() == "last day")
            {
                if (quarterEnd + sendTime <= currentTime)
                    quarterEnd = quarterEnd.AddMonths(3);

                return quarterEnd;
            }

            if (int.TryParse(sendOn, out int day))
            {
                DateTime candidate = quarterStart.AddDays(day - 1);

                if (candidate + sendTime <= currentTime)
                    candidate = candidate.AddMonths(3);

                return candidate;
            }

            return quarterStart;
        }

        public DateTime GetNextDayOfWeek(
            DateTime currentDate,
            DayOfWeek desiredDay,
            TimeSpan sendTime,
            DateTime currentTime)
        {
            int daysUntil = (7 + (int)desiredDay - (int)currentDate.DayOfWeek) % 7;
            DateTime candidate = currentDate.AddDays(daysUntil);

            if (candidate + sendTime <= currentTime)
                candidate = candidate.AddDays(7);

            return candidate;
        }

        public TimeSpan GetTimeSpan(string time)
        {
            if (DateTime.TryParseExact(
                time,
                "HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime result24))
            {
                return result24.TimeOfDay;
            }

            if (DateTime.TryParseExact(
                time,
                "h:mm tt",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime result12))
            {
                return result12.TimeOfDay;
            }

            throw new ArgumentException("Invalid time format", nameof(time));
        }
    }
}