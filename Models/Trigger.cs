using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgLaunch_Scheduler.Models
{
    public class Trigger
    {
        public string TriggerProcess { get; set; }
        public string? Arguments { get; set; }
        public List<string> LaunchProcessPaths { get; set; } = new();

        private List<Process> currentProcesses = new();

        public void Start()
        {
            var currentProcess = new Process();
            foreach (var item in LaunchProcessPaths)
            {
                currentProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = item,
                    WorkingDirectory = item.Remove(item.LastIndexOf("\\")),
                    Arguments = Arguments
                });

                currentProcesses.Add(currentProcess);
            }
        }

        public void Stop()
        {
            foreach (var item in currentProcesses)
            {
                item.Kill();
            }

            currentProcesses.Clear();
        }
    }
}
