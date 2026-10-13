using Newtonsoft.Json;

namespace ARMCommon.Helpers
{
    public class AxpertScheduler
    {

        public DateTime GetNextOccurrence(string scheduleJson)
        {
            try
            {
                dynamic schedule = Newtonsoft.Json.JsonConvert.DeserializeObject(scheduleJson);
                DateTime startDate = DateTime.Parse(schedule.startDate.ToString());
                string weekdayday = schedule.sendOn;
                string period = schedule.period;


                TimeSpan sendTime = GetTimeSpan(schedule.sendTime.ToString());

                DateTime currentTime = DateTime.Now;
                DateTime nextOccurrence = startDate;
                if (startDate < DateTime.Now.Date)
                    nextOccurrence = DateTime.Now.Date;

                //while (nextOccurrence.Date + sendTime <= currentTime)
                {
                    switch (period.Trim().ToLower())
                    {
                        case "daily":
                        case "every day":
                            if (nextOccurrence.Date + sendTime < currentTime)
                                nextOccurrence = nextOccurrence.AddDays(1);
                            break;
                        case "weekly":
                        case "every week":
                            DayOfWeek sendOn = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), weekdayday);

                            string fullDayName = sendOn.ToString();
                            if (nextOccurrence.DayOfWeek != sendOn || nextOccurrence.Date + sendTime <= currentTime)
                            {
                                nextOccurrence = GetNextDayOfWeek(nextOccurrence, sendOn, sendTime);
                            }
                            break;

                        case "monthly":
                        case "every month":
                            if (nextOccurrence.Date < startDate.Date)
                            {
                                nextOccurrence = startDate;
                            }
                            switch (schedule.sendOn.ToString().ToLower())
                            {
                                case "first day":
                                    if (nextOccurrence.Date == new DateTime(nextOccurrence.Year, nextOccurrence.Month, 1).Date && nextOccurrence.Date + sendTime > currentTime)
                                    {
                                        //Do nothing                                        
                                    }
                                    else
                                        nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, 1).AddMonths(1);
                                    break;
                                case "last day":
                                    if (nextOccurrence.Date + sendTime > currentTime)
                                    {
                                        nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, DateTime.DaysInMonth(nextOccurrence.Year, nextOccurrence.Month));
                                    }
                                    else
                                        nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, DateTime.DaysInMonth(nextOccurrence.Year, nextOccurrence.Month)).AddMonths(1);
                                    break;
                                case "first day of last week":
                                    DayOfWeek firstDayOfWeek = Enum.Parse<DayOfWeek>(schedule.firstDayOfWeek.ToString(), true);
                                    int daysUntilFirstDayOfWeek = (firstDayOfWeek - (DayOfWeek)new DateTime(nextOccurrence.Year, nextOccurrence.Month, DateTime.DaysInMonth(nextOccurrence.Year, nextOccurrence.Month)).DayOfWeek + 7) % 7;
                                    nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, DateTime.DaysInMonth(nextOccurrence.Year, nextOccurrence.Month)).AddDays(-daysUntilFirstDayOfWeek);
                                    if (nextOccurrence.Date + sendTime < startDate + sendTime)
                                    {
                                        daysUntilFirstDayOfWeek = (firstDayOfWeek - (DayOfWeek)new DateTime(nextOccurrence.Year, nextOccurrence.Month, DateTime.DaysInMonth(nextOccurrence.Year, nextOccurrence.Month)).DayOfWeek + 7) % 7;
                                        nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, DateTime.DaysInMonth(nextOccurrence.Year, nextOccurrence.Month)).AddMonths(1).AddDays(-daysUntilFirstDayOfWeek);
                                    }
                                    break;
                                default:
                                    try
                                    {
                                        int sendDay = Convert.ToInt32(schedule.sendOn.ToString().ToLower());
                                        sendDay = sendDay - 1;
                                        if (nextOccurrence.Date == new DateTime(nextOccurrence.Year, nextOccurrence.Month, 1).AddDays(sendDay).Date && new DateTime(nextOccurrence.Year, nextOccurrence.Month, 1).AddDays(sendDay).Date + sendTime > currentTime)
                                        {
                                            //Do nothing                                        
                                        }
                                        else
                                            nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, 1).AddMonths(1).AddDays(sendDay);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine("Invalid sendon value specified in the monthly schedule : " + ex.Message);
                                    }
                                    break;
                            }
                            break;

                        case "quarterly":
                        case "every quarter":
                            if (nextOccurrence.Date < startDate.Date)
                            {
                                nextOccurrence = startDate;
                            }
                            switch (schedule.sendOn.ToString().ToLower())
                            {
                                case "first day":
                                    if (nextOccurrence.Date == new DateTime(nextOccurrence.Year, ((nextOccurrence.Month - 1) / 3) * 3 + 1, 1).Date && nextOccurrence.Date + sendTime > currentTime)
                                    {
                                        //Do nothing                                        
                                    }

                                    else
                                        nextOccurrence = new DateTime(nextOccurrence.Year, ((nextOccurrence.Month - 1) / 3) * 3 + 1, 1).AddMonths(3);
                                    break;
                                case "last day":
                                    if (nextOccurrence.Date + sendTime > currentTime)
                                    {
                                        nextOccurrence = new DateTime(nextOccurrence.Year, ((nextOccurrence.Month - 1) / 3) * 3 + 3, DateTime.DaysInMonth(nextOccurrence.Year, ((nextOccurrence.Month - 1) / 3) * 3 + 3));
                                    }
                                    else
                                        nextOccurrence = new DateTime(nextOccurrence.Year, ((nextOccurrence.Month - 1) / 3) * 3 + 3, DateTime.DaysInMonth(nextOccurrence.Year, ((nextOccurrence.Month - 1) / 3) * 3 + 3)).AddMonths(3);
                                    break;
                                case "first day of last week":
                                    DayOfWeek firstDayOfWeek = Enum.Parse<DayOfWeek>(schedule.firstDayOfWeek.ToString(), true);
                                    int daysUntilLastMonthFirstDayOfWeek = (firstDayOfWeek - (DayOfWeek)new DateTime(nextOccurrence.Year, ((nextOccurrence.Month - 1) / 3) * 3 + 3, DateTime.DaysInMonth(nextOccurrence.Year, ((nextOccurrence.Month - 1) / 3) * 3 + 3)).DayOfWeek + 7) % 7;
                                    nextOccurrence = new DateTime(nextOccurrence.Year, ((nextOccurrence.Month - 1) / 3) * 3 + 3, DateTime.DaysInMonth(nextOccurrence.Year, ((nextOccurrence.Month - 1) / 3) * 3 + 3)).AddDays(-daysUntilLastMonthFirstDayOfWeek);

                                    nextOccurrence = new DateTime(nextOccurrence.Year, nextOccurrence.Month, DateTime.DaysInMonth(nextOccurrence.Year, nextOccurrence.Month)).AddDays(-daysUntilLastMonthFirstDayOfWeek);
                                    if (nextOccurrence.Date + sendTime < startDate + sendTime)
                                    {
                                        daysUntilLastMonthFirstDayOfWeek = (firstDayOfWeek - (DayOfWeek)new DateTime(nextOccurrence.Year, ((nextOccurrence.Month - 1) / 3) * 3 + 3, DateTime.DaysInMonth(nextOccurrence.Year, ((nextOccurrence.Month - 1) / 3) * 3 + 3)).DayOfWeek + 7) % 7;
                                        nextOccurrence = new DateTime(nextOccurrence.Year, ((nextOccurrence.Month - 1) / 3) * 3 + 3, DateTime.DaysInMonth(nextOccurrence.Year, ((nextOccurrence.Month - 1) / 3) * 3 + 3)).AddDays(-daysUntilLastMonthFirstDayOfWeek);
                                    }

                                    break;
                                default:
                                    try
                                    {
                                        int sendDay = Convert.ToInt32(schedule.sendOn.ToString().ToLower());
                                        sendDay = sendDay - 1;
                                        if (nextOccurrence.Date == new DateTime(nextOccurrence.Year, ((nextOccurrence.Month - 1) / 3) * 3 + 1, 1).AddDays(sendDay).Date && new DateTime(nextOccurrence.Year, ((nextOccurrence.Month - 1) / 3) * 3 + 1, 1).AddDays(sendDay).Date + sendTime > currentTime)
                                        {
                                            //Do nothing                                        
                                        }
                                        else if (new DateTime(nextOccurrence.Year, ((nextOccurrence.Month - 1) / 3) * 3 + 1, 1).AddDays(sendDay).Date + sendTime > currentTime)
                                        {
                                            nextOccurrence = new DateTime(nextOccurrence.Year, ((nextOccurrence.Month - 1) / 3) * 3 + 1, 1).AddDays(sendDay).Date;
                                        }
                                        else
                                            nextOccurrence = new DateTime(nextOccurrence.Year, ((nextOccurrence.Month - 1) / 3) * 3 + 1, 1).AddMonths(3).AddDays(sendDay);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine("Invalid sendon value specified in the quarterly schedule : " + ex.Message);
                                    }
                                    break;
                            }
                            break;
                        case "yearly":
                            while (startDate + sendTime < currentTime)
                            {
                                nextOccurrence = startDate.AddYears(1);
                                startDate = startDate.AddYears(1);
                            }
                            break;
                        case "custom":
                            nextOccurrence = startDate;
                            while (nextOccurrence + sendTime < currentTime)
                            {
                                nextOccurrence = nextOccurrence.AddMinutes(Convert.ToInt32(schedule.sendOn.ToString()));
                            }
                            return nextOccurrence + sendTime;
                            break;
                        default:
                            Console.WriteLine("Invalid period specified in the schedule : " + scheduleJson);
                            break;
                    }
                }

                // Combine the next occurrence date with the specified send time
                nextOccurrence = nextOccurrence.Date + sendTime;

                return nextOccurrence;
            }

            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {JsonConvert.SerializeObject(ex)}");
            }
            return DateTime.Now.AddDays(-100);
        }

        public DateTime GetNextDayOfWeek(DateTime currentDate, DayOfWeek desiredDay, TimeSpan sendTime)
        {
            int daysUntilDesiredDay = (7 + (int)desiredDay - (int)currentDate.DayOfWeek) % 7;
            DateTime nextOccurrence = currentDate.AddDays(daysUntilDesiredDay);
            if (nextOccurrence.Date + sendTime <= DateTime.Now)
            {
                nextOccurrence = nextOccurrence.AddDays(1);
                daysUntilDesiredDay = (7 + (int)desiredDay - (int)nextOccurrence.DayOfWeek) % 7;
                nextOccurrence = nextOccurrence.AddDays(daysUntilDesiredDay);
            }
            return nextOccurrence;
        }

        public TimeSpan GetTimeSpan(string time)
        {
            if (DateTime.TryParseExact(time, "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime result24Hour))
            {
                return result24Hour.TimeOfDay;
            }

            if (DateTime.TryParseExact(time, "h:mm tt", null, System.Globalization.DateTimeStyles.None, out DateTime result12Hour))
            {
                return result12Hour.TimeOfDay;
            }
            throw new ArgumentException("Invalid time format", nameof(time));
        }
    }
}
