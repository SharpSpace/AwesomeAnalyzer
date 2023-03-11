using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace FleetManagement.Service
{
    public class MeasureTime : IDisposable
    {
        private readonly bool _disabled;

        private readonly string _info;

        private readonly string _path;

        private readonly long _stopWatch;

        public MeasureTime(
            bool disabled = false,
            [CallerMemberName] string info = "",
            [CallerFilePath] string path = ""
        )
        {
            _disabled = disabled;
            _info = disabled ? string.Empty : info;
            _path = disabled ? string.Empty : path;
            _stopWatch = disabled ? Int64.MaxValue : Stopwatch.GetTimestamp();
        }

        public void Dispose()
        {
            if (_disabled)
            {
                return;
            }

            var elapsedTimestamp = Stopwatch.GetTimestamp() - _stopWatch;
            var tickFrequency = ((double)(10_000 * 1_000)) / Stopwatch.Frequency;
            var elapsed = TimeSpan.FromTicks(unchecked((long)(elapsedTimestamp * tickFrequency)));

            var message = $"{_path.Substring(_path.LastIndexOf('\\') + 1)}->{_info} in {elapsed.Milliseconds}ms";

            Debug.WriteLine($"{nameof(MeasureTime)} {message}");
            Console.WriteLine($"{nameof(MeasureTime)} {message}");
        }
    }
}