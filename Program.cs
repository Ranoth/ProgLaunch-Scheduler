using ProgLaunch_Scheduler.Models;
using System.ComponentModel;
using System.Management;
using System.Xml.Linq;

internal class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var triggerList = new BindingList<Trigger>();
        var removedTrigger = new Trigger();

        WatchProcesses();
        UpdateXml();
        do
        {
            //Thread.Sleep(1000);
        } while (PromptInput());

        void WatchProcesses()
        {
            var startedProcessWatcher = new ManagementEventWatcher("SELECT ProcessName FROM Win32_ProcessStartTrace");
            var stoppedProcessWatcher = new ManagementEventWatcher("SELECT ProcessName FROM Win32_ProcessStopTrace");

            startedProcessWatcher.EventArrived += (s, e) =>
            {
                var slimShady = triggerList.FirstOrDefault(x => x.TriggerProcess.Contains(e.NewEvent["ProcessName"].ToString()));
                if (slimShady != null) slimShady.Start();
            };
            stoppedProcessWatcher.EventArrived += (s, e) =>
            {
                var slimShady = triggerList.FirstOrDefault(x => x.TriggerProcess.Contains(e.NewEvent["ProcessName"].ToString()));
                if (slimShady != null) slimShady.Stop();
            };

            startedProcessWatcher.Start();
            stoppedProcessWatcher.Start();
        }

        bool PromptInput()
        {
            var trigger = new Trigger();

            Console.WriteLine("Select action (bind / remove / list / quit)");
            string select = Console.ReadLine();

            if (select == "bind")
            {
                Console.WriteLine("Select process to bind");
                trigger.TriggerProcess = Console.ReadLine();

                Console.WriteLine("Select executable bind");
                OpenFileDialog dialog = new OpenFileDialog
                {
                    Multiselect = false,
                };

                bool more = true;
                do
                {
                    dialog.ShowDialog();
                    if (dialog.ShowDialog() == DialogResult.OK) trigger.LaunchProcessPaths.Add(dialog.FileName);
                    Console.WriteLine("Select more ? Y / n");
                    string yesNo = Console.ReadLine();
                    if (yesNo == "Y" || yesNo == "y" || yesNo == "yes") more = true;
                    else if (yesNo == "N" || yesNo == "n" || yesNo == "no") more = false;
                } while (more);

                Console.WriteLine("Select Arguments");
                trigger.Arguments = Console.ReadLine();

                triggerList.Add(trigger);
            }
            else if (select == "remove")
            {
                if (triggerList.Any())
                {
                    Console.WriteLine("Select process to remove");
                    string _ = Console.ReadLine();
                    removedTrigger = triggerList.FirstOrDefault(x => x.TriggerProcess == _);
                    DisplayTrigger(removedTrigger);
                    triggerList.Remove(removedTrigger);
                }
            }
            else if (select == "list") foreach (var item in triggerList) DisplayTrigger(item);
            else if (select == "quit") return false;
            return true;
        }

        void DisplayTrigger(Trigger trigger)
        {
            Console.WriteLine(trigger.TriggerProcess);
            trigger.LaunchProcessPaths.ForEach((x) => Console.WriteLine(x));
            Console.WriteLine(trigger.Arguments);
        }

        void UpdateXml()
        {
            if (!File.Exists(nameof(Trigger) + "s.xml"))
            {
                var xml = new XElement(nameof(Trigger) + "s");
                new XDocument(xml).Save(nameof(Trigger) + "s.xml");
            }
            var xDoc = XDocument.Load(nameof(Trigger) + "s.xml");
            if (File.Exists(nameof(Trigger) + "s.xml"))
            {
                Trigger _;
                var xQuery = from elements in xDoc.Descendants(nameof(Trigger) + "s").Elements(nameof(Trigger))
                             select new Trigger
                             {
                                 TriggerProcess = elements?.Attribute(nameof(_.TriggerProcess)).Value,
                                 Arguments = elements?.Attribute(nameof(_.Arguments)).Value,
                                 LaunchProcessPaths = elements?.Element(nameof(_.LaunchProcessPaths)).Elements("LaunchProcessPath").Select(x => x.Attribute("Path").Value).ToList()
                             };
                triggerList = new BindingList<Trigger>(xQuery.ToList());
            }

            triggerList.ListChanged += (s, e) =>
            {
                switch (e.ListChangedType)
                {
                    case ListChangedType.ItemAdded:
                        var trigger = triggerList[e.NewIndex];
                        DisplayTrigger(trigger);

                        xDoc.Element(nameof(Trigger) + "s").Add(
                            new XElement(nameof(Trigger),
                                new XAttribute(nameof(trigger.TriggerProcess), trigger.TriggerProcess),
                                new XAttribute(nameof(trigger.Arguments), trigger.Arguments),
                                new XElement(nameof(trigger.LaunchProcessPaths), trigger.LaunchProcessPaths.Select(x =>
                                {
                                    return new XElement("LaunchProcessPath", new XAttribute("Path", x));
                                }))));

                        xDoc.Save(nameof(Trigger) + "s.xml");

                        break;
                    case ListChangedType.ItemDeleted:
                        Trigger _;
                        xDoc.Descendants(nameof(Trigger) + "s").Elements(nameof(Trigger))
                            .FirstOrDefault(x => x.Attribute(nameof(_.TriggerProcess)).Value == removedTrigger.TriggerProcess).Remove();

                        xDoc.Save(nameof(Trigger) + "s.xml");

                        break;
                }
            };
        }
    }


}