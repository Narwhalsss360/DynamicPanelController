using Profiling;
using System.Collections.Specialized;
using System.Reflection;

namespace PanelExtension
{
    public delegate int ExtensionLoader(Assembly AssemblyToLoad, string? ModuleName);
    public delegate void ExtensionRefresher(Extension? Instance = null);
    public delegate void ProfileIndexSelector(int Index);
    public delegate object? PanelExtensionSubscriber(Extension Instance);
    public delegate object? PanelExtensionUnsubscriber(Extension Instance);

    public class SettingsChangedEventArgs : EventArgs
    {
        public readonly ApplicationSettings NewSettings;

        public SettingsChangedEventArgs(ApplicationSettings NewSettings)
            : base()
        {
            this.NewSettings = NewSettings;
        }
    }

    public delegate void SettingsChangedEventHandler(object? Sender, SettingsChangedEventArgs Args);

    public class SelectedProfileChangedEventArgs : EventArgs
    {
        public readonly int NewIndex;
        public readonly PanelProfile? NewProfile;

        public SelectedProfileChangedEventArgs(int NewIndex, PanelProfile? NewProfile)
            : base()
        {
            this.NewIndex = NewIndex;
            this.NewProfile = NewProfile;
        }
    }

    public delegate void SelectedProfileChangedEventHandler(object? Sender, SelectedProfileChangedEventArgs Args);

    public class Extension : IDisposable
    {
        private static PanelExtensionSubscriber? Subscriber { get; set; }
        private static PanelExtensionUnsubscriber? Unsubscriber { get; set; }
        private static ExtensionLoader? ExtensionLoader { get; set; }
        private static ExtensionRefresher? Refresher { get; set; }
        private static ProfileIndexSelector? SelectProfileIndex { get; set; }

        public bool ExtensionSubscribed { get; private set; } = false;
        public object? ExtensionSubscriptionResult { get; private set; } = null;

        public event EventHandler? Exit;
        public event NotifyCollectionChangedEventHandler? ProfilesListChanged;
        public event SelectedProfileChangedEventHandler? SelectedProfileChanged;
        public event EventHandler? CommunicationsStarted;
        public event EventHandler? CommunicationsStopped;
        public event SettingsChangedEventHandler? SettingsChanged;
        public event EventHandler? Disposed;

        public class ApplicationVariables
        {
            private class DeadLog : ILogger
            {
                public event LogChangedEventHanlder? LogChanged;

                public string FormatMessage(ILogger.Levels? Level, object? Sender, string Message)
                {
                    return string.Empty;
                }

                public string GetLog()
                {
                    return string.Empty;
                }

                public void Verbose(string Message, object? Sender = null)
                {
                }

                public void Info(string Message, object? Sender)
                {
                }

                public void Warn(string Message, object? Sender)
                {
                }

                public void Error(string Message, object? Sender)
                {
                }
            }

            public DateTime? LastLoad { get; private set; } = null;
            public ILogger Logger { get; private set; } = new DeadLog();
            public PanelProfile? CurrentProfile { get; private set; } = new();
            public int CurrentProfileIndex { get; private set; } = -1;
            public PanelProfile[] Profiles { get; private set; } = new PanelProfile[0];
            public Type[] Actions { get; private set; } = new Type[0];
            public Type[] AbsoluteActions { get; private set; } = new Type[0];
            public Type[] Sources { get; private set; } = new Type[0];
            public ApplicationSettings Settings { get; private set; } = new();

            public ApplicationVariables()
            {

            }
        }

        private ApplicationVariables? InnerApplication { get; set; } = null;

        public ApplicationVariables? Application
        {
            get
            {
                RefreshApplicationVariables();
                return InnerApplication;
            }
            private set => InnerApplication = value;
        }

        public Extension()
        {
            _ = TrySubscribe();
        }

        public Extension(bool Subscribe)
        {
            Application = new();
            if (Subscribe)
                _ = TrySubscribe();
        }

        protected object? TrySubscribe()
        {
            if (Subscriber is null)
                return "No subscriber available.";

            ExtensionSubscriptionResult = Subscriber(this);
            if (ExtensionSubscriptionResult is null)
                ExtensionSubscribed = true;

            return ExtensionSubscriptionResult;
        }

        protected void RefreshApplicationVariables()
        {
            Refresher?.Invoke(this);
        }

        public static int RequestLoad(Assembly AssemblyToLoad, string? ModuleName = null)
        {
            return ExtensionLoader is null ? int.MinValue : ExtensionLoader(AssemblyToLoad, ModuleName);
        }

        public static void SelectIndex(int Index)
        {
            SelectProfileIndex?.Invoke(Index);
        }

        protected object? TryUnsubscribe()
        {
            if (Unsubscriber is null)
                return "No subscriber available.";

            ExtensionSubscriptionResult = Unsubscriber(this);
            if (ExtensionSubscriptionResult is null)
                ExtensionSubscribed = false;

            return ExtensionSubscriptionResult;
        }

        private void ProfilesListChangedWrapper(object? Sender, NotifyCollectionChangedEventArgs Args)
        {
            ProfilesListChanged?.Invoke(Sender, Args);
        }

        private void SelectedProfileChangedWrapper(object? Sender, SelectedProfileChangedEventArgs Args)
        {
            SelectedProfileChanged?.Invoke(Sender, Args);
        }

        private void CommunicationsStartedWrapper(object? Sender, EventArgs Args)
        {
            CommunicationsStarted?.Invoke(Sender, Args);
        }

        private void CommunicationsStoppedWrapper(object? Sender, EventArgs Args)
        {
            CommunicationsStopped?.Invoke(Sender, Args);
        }

        private void SettingsChangedWrapper(object? Sender, SettingsChangedEventArgs Args)
        {
            SettingsChanged?.Invoke(Sender, Args);
        }

        private void ApplicationExiting(object? Sender, EventArgs Args)
        {
            Exit?.Invoke(Sender, Args);
        }

        protected virtual void Dispose(bool Disposing)
        {
            if (!Disposing)
                return;
            if (!ExtensionSubscribed)
                return;
            _ = TryUnsubscribe();
            Disposed?.Invoke(this, new EventArgs());
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Extension()
        {
            Dispose();
        }
    }
}