using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace MarshallApp
{
    public partial class App : Application
    {
        // ReSharper disable once InconsistentNaming
        public const string APPNAME = "Marshall";
        public static readonly string DefaultMarshallProjectsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Marshall Projects");
        protected override void OnStartup(StartupEventArgs e)
        {
            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
    
            base.OnStartup(e);
            
            if(!Directory.Exists(DefaultMarshallProjectsPath)) 
                Directory.CreateDirectory(DefaultMarshallProjectsPath);
        }
    }
}
