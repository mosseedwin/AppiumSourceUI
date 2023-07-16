using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Remote;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml;

namespace PageSourceUI
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public MainViewModel(MainWindow mainWindow)
        {
            MainWindow = mainWindow;
            RefreshCommand = new CustomCommand(OnRefresh);
            StartCommand = new CustomCommand(OnStart);
            StopCommand = new CustomCommand(OnStop);
            CopyCommand = new CustomCommand(OnCopy);
            _Applications = GetApplications();
            StartVisibility = Visibility.Visible;
            StopVisibility = Visibility.Collapsed;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private MainWindow MainWindow { get; }

        public ICommand RefreshCommand { get; }

        public ICommand StartCommand { get; }

        public ICommand StopCommand { get; }

        public ICommand CopyCommand { get; }

        private List<string> _Applications = new List<string>();

        public ICollectionView Applications
        {
            get
            {
                return CollectionViewSource.GetDefaultView(_Applications);
            }
        }

        public string Selected
        {
            get;
            set;
        }

        private int _Time = 1000;
        public int Time
        {
            get { return _Time; }
            set { _Time = value < 1000 ? 1000 : value; OnPropertyChanged(); }
        }

        private bool _TimeEnabled = true;
        public bool TimeEnabled
        {
            get { return _TimeEnabled; }
            set { _TimeEnabled = value; OnPropertyChanged(); }
        }

        private Visibility startVisibility;
        public Visibility StartVisibility
        {
            get { return startVisibility; }
            set { startVisibility = value; OnPropertyChanged(); }
        }

        private Visibility stopVisibility;
        public Visibility StopVisibility
        {
            get { return stopVisibility; }
            set { stopVisibility = value; OnPropertyChanged(); }
        }

        private List<string> GetApplications()
        {
            int pId = Process.GetCurrentProcess().Id;
            List<string> windowHandles = new List<string>() { "Root" };
            foreach (Process window in Process.GetProcesses())
            {
                if (window.Id == pId)
                {
                    continue;
                }
                try
                {
                    window.Refresh();
                    IntPtr ptr = window.MainWindowHandle;
                    if (ptr != IntPtr.Zero)
                    {
                        string name = window.MainWindowTitle + " @ " + ptr.ToInt32();
                        windowHandles.Add(name);
                    }
                }
                catch
                {
                }
            }
            return windowHandles;
        }

        private Thread ActionTask { get; set; }

        private XmlDocument XmlDocument { get; set; }

        private void OnRefresh()
        {
            _Applications = GetApplications();
            OnPropertyChanged(nameof(Applications));
        }

        private void OnStart()
        {
            StartVisibility = Visibility.Collapsed;
            StopVisibility = Visibility.Visible;
            TimeEnabled = false;
            MainWindow.Viewer.xmlDocument = null;
            try
            {
                ActionTask?.Abort();
            }
            catch
            {
            }
            if (ActionTask?.IsAlive != true)
            {
                ActionTask = new Thread(UpdatePageSource);
                ActionTask.Start();
            }
        }

        private void OnStop()
        {
            StartVisibility = Visibility.Visible;
            StopVisibility = Visibility.Collapsed;
            TimeEnabled = true;
            try
            {
                ActionTask?.Abort();
            }
            catch
            {
            }
        }

        private void OnCopy()
        {
            XmlDocument xdoc = XmlDocument;
            if (xdoc == null)
            {
                Clipboard.SetText(string.Empty);
                return;
            }
            StringBuilder builder = new StringBuilder();
            XmlWriter writer = XmlWriter.Create(
                builder,
                new XmlWriterSettings()
                {
                    Indent = true,
                    NewLineOnAttributes = false,
                    WriteEndDocumentOnClose = true,
                    CheckCharacters = true,
                    NewLineHandling = NewLineHandling.Replace,
                    CloseOutput = true,
                }
            );
            xdoc.Save(writer);
            string content = builder.ToString();
            Clipboard.SetText(content);
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void UpdatePageSource()
        {
            DateTime timeout = DateTime.UtcNow;
            DateTime now;
            WindowsDriver<WindowsElement> driver = null;
            string p = string.Empty;
            loop:
            now = DateTime.UtcNow;
            if (timeout > now)
            {
                Thread.CurrentThread.Join(100);
                goto loop;
            }
            driver = GetDriver(driver);
            string pageSource;
            try
            {
                pageSource = driver.PageSource;
            }
            catch
            {
                timeout = DateTime.UtcNow.AddMilliseconds(Time);
                goto loop;
            }
            XmlDocument xdoc = new XmlDocument();
            xdoc.LoadXml(pageSource);
            RemoveAttributes(xdoc.DocumentElement);
            string innerXml = xdoc.InnerXml;
            if (!p.Equals(innerXml))
            {
                p = innerXml;
                XmlDocument = xdoc;
                MainWindow.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(() => MainWindow.Viewer.xmlDocument = xdoc));
            }
            timeout = DateTime.UtcNow.AddMilliseconds(Time);
            goto loop;
        }

        private WindowsDriver<WindowsElement> GetDriver(WindowsDriver<WindowsElement> driver)
        {
            if (driver != null)
            {
                return driver;
            }
            DesiredCapabilities appCapabilities;
            if (!Selected.Equals("Root"))
            {
                int index = Selected.LastIndexOf(" @ ");
                int handle = int.Parse(Selected.Substring(index + 3));
                appCapabilities = new DesiredCapabilities();
                appCapabilities.SetCapability("appTopLevelWindow", "0x" + handle.ToString("X"));
                appCapabilities.SetCapability("deviceName", "WindowsPC");
                appCapabilities.SetCapability("platformName", "Windows 11");
            }
            else
            {
                appCapabilities = new DesiredCapabilities();
                appCapabilities.SetCapability("app", "Root");
                appCapabilities.SetCapability("deviceName", "WindowsPC");
                appCapabilities.SetCapability("platformName", "Windows 11");
            }
            WindowsDriver<WindowsElement> ret;
            do
            {
                try
                {
                    ret = new WindowsDriver<WindowsElement>(new Uri(Settings.Default.AppiumURL), appCapabilities, TimeSpan.FromSeconds(60));
                    ret.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(1.5);
                }
                catch
                {
                    ret = null;
                    Thread.CurrentThread.Join(10000);
                }
            } while (ret == null);
            return ret;
        }

        private void RemoveAttributes(XmlElement root)
        {
            foreach (string att in Settings.Default.AttributesIgnored)
            {
                root.RemoveAttribute(att);
            }
            foreach (XmlAttribute a in root.Attributes.OfType<XmlAttribute>().ToList())
            {
                if (Settings.Default.AttributesPermanent.Contains(a.Name))
                {
                    continue;
                }
                if (string.IsNullOrEmpty(a.Value) || "True".Equals(a.Value) || "False".Equals(a.Value))
                {
                    root.RemoveAttribute(a.Name);
                }
            }
            List<XmlElement> children = new List<XmlElement>();
            System.Collections.IEnumerator en = root.GetEnumerator();
            while (en.MoveNext())
            {
                if (en.Current is XmlElement el)
                {
                    children.Add(el);
                }
            }
            foreach (XmlElement child in children)
            {
                RemoveAttributes(child);
            }
        }
    }
}
