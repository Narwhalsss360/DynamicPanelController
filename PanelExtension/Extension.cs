using Profiling;
using System.Reflection;

namespace PanelExtension
{
    public delegate int ExtensionLoader(Assembly AssemblyToLoad, string? ModuleName);
    public delegate void ExtensionRefresher(Extension? Instance = null);
    public delegate object? PanelExtensionSubscriber(Extension Instance);
    public delegate object? PanelExtensionUnsubscriber(Extension Instance);

    public class Extension
    {
        static PanelExtensionSubscriber? Subscriber { get; set; }
        static PanelExtensionUnsubscriber? Unsubscriber { get; set; }
        static ExtensionLoader? ExtensionLoader { get; set; }
        static ExtensionRefresher? Refresher { get; set; }
        public bool ExtensionSubscribed { get; private set; } = false;
        public object? ExtensionSubscriptionResult { get; private set; } = null;
        public event EventHandler? Exit;

        public class ApplicationVariables
        {
            class DeadLog : ILogger
            {
                public event EventHandler? LogChanged;

                public void Error(string Message)
                {
                }

                public string GetLog()
                {
                    return string.Empty;
                }

                public void Info(string Message)
                {
                }

                public void Warn(string Message)
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

        ApplicationVariables? InnerApplication { get; set; } = null;

        public ApplicationVariables? Application
        {
            get
            {
                RefreshApplicationVariables();
                return InnerApplication;
            }
            private set
            {
                InnerApplication = value;
            }
        }

        public Extension()
        {
            TrySubscribe();
        }

        public Extension(bool Subscribe)
        {
            Application = new();
            if (Subscribe)
                TrySubscribe();
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

        protected void RefreshApplicationVariables() => Refresher?.Invoke(this);

        public static int RequestLoad(Assembly AssemblyToLoad, string? ModuleName = null)
        {
            if (ExtensionLoader is null)
                return int.MinValue;
            return ExtensionLoader(AssemblyToLoad, ModuleName);
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

        private void ApplicationExiting(object? Sender, EventArgs Args) => Exit?.Invoke(Sender, Args);

        ~Extension()
        {
            if (ExtensionSubscribed)
                if (TryUnsubscribe() is not null)
                    throw new NotImplementedException("Could not unsubscribe on destructor.");
        }
    }
}