using System;
using System.Collections.Generic;
using System.Threading;

namespace StockingBot
{
    public static class Scheduler
    {
        private static List<Timer> Timers = new List<Timer>();

        public static void Clear()
        {
            foreach (Timer timer in Timers) {
                if (timer != null) {
                    timer.Dispose();
                }
            }

            Timers.Clear();
        }

        public static void Schedule(long ticks, Action action)
        {
            TimeSpan until = new TimeSpan(ticks - DateTime.Now.Ticks);
            Timer timer = null;

            if (until < TimeSpan.Zero) {
                until = TimeSpan.Zero;
            }

            timer = new Timer(new TimerCallback(x => {
                Timers.Remove(timer);
                action.Invoke();
                timer.Dispose();
            }), null, until, Timeout.InfiniteTimeSpan);

            Timers.Add(timer);
        }
    }
}
