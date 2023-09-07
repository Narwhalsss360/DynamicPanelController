using Profiling;
using System.Reflection;

namespace PanelExtension
{
    public delegate int ExtensionLoader(Assembly AssemblyToLoad, string? ModuleName);
    public delegate void ExtensionRefresher(Extension? Instance = null);
    public delegate void ProfileIndexSelector(int Index);
    public delegate object? PanelExtensionSubscriber(Extension Instance);
    public delegate object? PanelExtensionUnsubscriber(Extension Instance);

    public class Extension
    {
        private static PanelExtensionSubscriber? Subscriber { get; set; }
        private static PanelExtensionUnsubscriber? Unsubscriber { get; set; }
        private static ExtensionLoader? ExtensionLoader { get; set; }
        private static ExtensionRefresher? Refresher { get; set; }
        private static ProfileIndexSelector? SelectProfileIndex { get; set; }

        public bool ExtensionSubscribed { get; private set; } = false;
        public object? ExtensionSubscriptionResult { get; private set; } = null;

        public event EventHandler? Exit;
        public event EventHandler? ProfilesListChanged;
        public event EventHandler? SelectedProfileChanged;
        public event EventHandler? CommunicationsStarted;
        public event EventHandler? CommunicationsStopped;

        public class ApplicationVariables
        {
            private class DeadLog : ILogger
            {
                public event EventHandler? LogChanged;

                public string FormatMessage(ILogger.LogLevels? Level, object? Sender, string Message)
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
            public PanelProfile[] Profiles { get; private set; } = new PanelProfile[0];
            public Type[] Actions { get; private set; } = new Type[0];
            public Type[] AbsoluteActions { get; private set; } = new Type[0];
            public Type[] Sources { get; private set; } = new Type[0];

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

        private void ProfilesListChangedWrapper(object? Sender, EventArgs Args)
        {
            ProfilesListChanged?.Invoke(Sender, Args);
        }

        private void SelectedProfileChangedWrapper(object? Sender, EventArgs Args)
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

        private void ApplicationExiting(object? Sender, EventArgs Args)
        {
            Exit?.Invoke(Sender, Args);
        }

        ~Extension()
        {
            if (ExtensionSubscribed)
                if (TryUnsubscribe() is not null)
                    throw new NotImplementedException("Could not unsubscribe on destructor.");
        }
    }
}