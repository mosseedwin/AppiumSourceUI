using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace PageSourceUI
{
    public class Settings
    {
        private static string PathRoot
        {
            get => Path.GetDirectoryName(typeof(MainViewModel).Assembly.Location);
        }

        internal static string FILENAME
        {
            get => Path.Combine(PathRoot, "settings.cfg");
        }

        private static XmlSerializer XmlSerializer;

        private static Settings _Default;
        public static Settings Default
        {
            get
            {
                if (_Default == null)
                {
                    XmlSerializer = new XmlSerializer(typeof(Settings));
                    if (!File.Exists(FILENAME))
                    {
                        XmlSerializer.Serialize(XmlWriter.Create(FILENAME), new Settings());
                    }
                    try
                    {
                        _Default = (Settings)XmlSerializer.Deserialize(XmlReader.Create(FILENAME));
                    }
                    catch
                    {
                    }
                }
                return _Default;
            }
        }

        public string AppiumURL { get; set; } = "http://127.0.0.1:4723";

        public string[] AttributesIgnored { get; set; } = new string[]
        {
            "x",
            "y",
            "width",
            "height",
            "FrameworkId",
            "RuntimeId",
            "ProcessId",
            "Selection",
            "Orientation",
            "LocalizedControlType"
        };

        public string[] AttributesPermanent { get; set; } = new string[]
        {
            "IsEnabled",
            "IsSelected"
        };
    }
}
