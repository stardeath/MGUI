using MGUI.Core.UI;
using MGUI.Core.UI.XAML;
using MGUI.Shared.Helpers;
using Microsoft.Xna.Framework.Content;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace MGUI.Samples2
{
    public abstract class SampleBase : ViewModelBase
    {
        public ContentManager Content { get; }
        public MGDesktop Desktop { get; }
        public MGWindow Window { get; }

        private bool _IsVisible;
        public bool IsVisible
        {
            get => _IsVisible;
            set
            {
                if (_IsVisible != value)
                {
                    _IsVisible = value;
                    NPC(nameof(IsVisible));

                    if (IsVisible)
                        Desktop.Windows.Add(Window);
                    else
                        Desktop.Windows.Remove(Window);
                    VisibilityChanged?.Invoke(this, IsVisible);
                }
            }
        }

        public event EventHandler<bool> VisibilityChanged;

        public void Show() => IsVisible = true;
        public void Hide() => IsVisible = false;

        /// <param name="Initialize">Optional. This delegate is invoked before the XAML content is parsed,
        /// so you may wish to use this delegate to add resources to the <paramref name="Desktop"/> that may be required in order to parse the XAML.</param>
        protected SampleBase(ContentManager Content, MGDesktop Desktop, string ProjectFolderName, string XAMLFilename, Action Initialize = null)
        {
            this.Content = Content;
            this.Desktop = Desktop;
            string ResourceName = $"{nameof(MGUI)}.{nameof(Samples2)}.{(ProjectFolderName == null ? "" : ProjectFolderName + ".")}{XAMLFilename}";
            string XAML = GeneralUtils.ReadEmbeddedResourceAsString(Assembly.GetExecutingAssembly(), ResourceName);
            Initialize?.Invoke();
            Window = XAMLParser.LoadRootWindow(Desktop, XAML, false, true);
            Window.WindowClosed += (sender, e) => IsVisible = false;
        }

        protected static void OpenURL(string URL)
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = URL,
                UseShellExecute = true
            });
        }
    }

    public class DataContextTest : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public virtual void NotifyPropertyChanged(string PropertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        /// <summary>Notify Property Changed for the given <paramref name="PropertyName"/></summary>
        public void NPC(string PropertyName) => NotifyPropertyChanged(PropertyName);

        private string _TestString;
        public string TestString
        {
            get => _TestString;
            set
            {
                if (_TestString != value)
                {
                    _TestString = value;
                    NPC(nameof(TestString));
                }
            }
        }

        public DataContextTest()
        {
            TestString = "Hello World";
        }
    }

    public class Compendium : SampleBase
    {
        public Compendium(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, null, $"{nameof(Compendium)}.xaml")
        {
            Window.IsCloseButtonVisible = false;

#if DEBUG
            //HUD.Show();
#endif

            Window.WindowDataContext = this;
        }
    }
}
