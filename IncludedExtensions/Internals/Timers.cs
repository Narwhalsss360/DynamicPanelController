using Panel.Communication;
using PanelExtension;
using Profiling;
using Profiling.ProfilingTypes.Mappings;
using Profiling.ProfilingTypes.PanelItems;

namespace IncludedExtensions.Internals
{
    [PanelActionDescriptor("Timers")]
    public class Timers : Extension, IPanelAction
    {
        private const string StartStopKey = "Start/Stop";
        private const string Start = "Start";
        private const string Stop = "Stop";

        private const string IDKey = "ID";

        private const string IOTypeKey = "IO Type";
        private const string IOButton = "Button";
        private const string IOAbsolute = "Absolute";

        private const string AutoResetKey = "Auto Reset";

        private const string ActionNameKey = "Action";
        private const string DoAfterBadOptionsKey = "Do After Bad Options";
        private const string IntervalKey = "Interval";

        private const string TrueEntry = "True";
        private const string FalseEntry = "False";

        private const string ButtonUpdateStateKey = "Button State";

        private Dictionary<string, string?> Options = new();
        private readonly System.Timers.Timer Timer = new();

        public Timers()
            : base()
        {
            Timer.Elapsed += TimerElapsed;
            CommunicationsStopped += ApplicationStoppedCommunication;
            Exit += ApplicationExiting;
            SelectedProfileChanged += ProfilesChanged;
        }

        public string?[]?[]? ValidOptions()
        {
            if (Options.ContainsKey(StartStopKey))
            {
                if (Options[StartStopKey] == Stop)
                {
                    if (Options.ContainsKey(IOTypeKey))
                    {
                        if (Options[IOTypeKey] == IOButton)
                        {
                            List<string> ButtonStateUpdateValidOptions = new() { ButtonUpdateStateKey };
                            ButtonStateUpdateValidOptions.AddRange(Enum.GetNames<ButtonUpdateStates>());

                            return new string?[]?[]
                            {
                                new string?[]
                                {
                                    StartStopKey,
                                    Start,
                                    Stop
                                },
                                new string?[]
                                {
                                    IDKey,
                                    null
                                },
                                new string?[]
                                {
                                    IOTypeKey,
                                    //IOAbsolute,
                                    IOButton
                                },
                                ButtonStateUpdateValidOptions.ToArray()
                            };
                        }
                    }
                }
                else
                {
                    if (Application is null)
                    {
                        return new string?[]?[]
                        {
                            new string?[]
                            {
                                StartStopKey,
                                Start,
                                Stop
                            },
                            new string?[]
                            {
                                IDKey,
                                null
                            },
                            new string?[]
                            {
                                IOTypeKey,
                                IOAbsolute,
                                IOButton
                            },
                            new string?[]
                            {
                                AutoResetKey,
                                TrueEntry,
                                FalseEntry
                            }
                        };
                    }
                    List<string> ActionNames = new() { ActionNameKey };
                    Array.ForEach(Application.Actions, Action => ActionNames.Add(Action.GetPanelItemDescriptor()?.Name));

                    return new string?[]?[]
                    {
                        new string?[]
                        {
                            StartStopKey,
                            Start,
                            Stop
                        },
                        new string?[]
                        {
                            AutoResetKey,
                            TrueEntry,
                            FalseEntry
                        },
                        new string?[]
                        {
                            IntervalKey,
                            null
                        },
                        new string?[]
                        {
                            DoAfterBadOptionsKey,
                            TrueEntry,
                            FalseEntry
                        },
                        ActionNames.ToArray(),
                        null
                    };
                }
            }

            return new string?[]?[]
                {
                    new string?[]
                    {
                        StartStopKey,
                        Start,
                        Stop
                    },
                    new string?[]
                    {
                        IDKey,
                        null
                    },
                    new string?[]
                    {
                        IOTypeKey,
                        IOAbsolute,
                        IOButton
                    }
                };
        }

        public string? SetOptions(Dictionary<string, string?> Options)
        {
            this.Options = Options;

            if (!Options.ContainsKey(StartStopKey))
                return "Stop/Start not specified.";

            if (Options[StartStopKey] == Stop)
            {
                if (!Options.ContainsKey(IDKey))
                    return "Must specify ID.";
                if (!int.TryParse(Options[IDKey], out _))
                    return "ID is not an number.";

                if (!Options.ContainsKey(IOTypeKey))
                    return "No IO Type specified.";

                if (Options[IOTypeKey] == IOButton && !Options.ContainsKey(ButtonUpdateStateKey))
                    return "Must specify ButtonUpdateStateKey";
            }
            else if (Options[StartStopKey] == Start)
            {
                if (!Options.ContainsKey(ActionNameKey))
                    return "No Action specify.";

                if (!Options.ContainsKey(DoAfterBadOptionsKey))
                    return "Do after bad options not specified.";

                if (!Options.ContainsKey(AutoResetKey))
                    return "Auto Reset not specified.";
            }
            else
            {
                return "Unkown Start/Stop Value";
            }


            return null;
        }

        public Dictionary<string, string?>? GetOptions()
        {
            if (Options.ContainsKey(StartStopKey) && false)
            {
                if (Options[StartStopKey] == Start)
                {
                    if (Options.ContainsKey(StartStopKey))
                        _ = Options.Remove(StartStopKey);

                    if (Options.ContainsKey(IDKey))
                        _ = Options.Remove(IDKey);

                    if (Options.ContainsKey(IOTypeKey))
                        _ = Options.Remove(IOTypeKey);

                    if (Options.ContainsKey(ButtonUpdateStateKey))
                        _ = Options.Remove(ButtonUpdateStateKey);
                }
                else
                {
                    if (Options.ContainsKey(AutoResetKey))
                        _ = Options.Remove(AutoResetKey);

                    if (Options.ContainsKey(ActionNameKey))
                        _ = Options.Remove(ActionNameKey);

                    if (Options.ContainsKey(DoAfterBadOptionsKey))
                        _ = Options.Remove(DoAfterBadOptionsKey);
                }
            }
            return Options;
        }

        public object? Do()
        {
            if (Application is null)
                return "Application is null";

            if (!Options.ContainsKey(StartStopKey))
                return "Stop/Start not specified.";

            int Interval = 0;

            if (Options[StartStopKey] == Stop)
            {
                if (!Options.ContainsKey(IDKey))
                    return "Must specify ID.";

                if (!int.TryParse(Options[IDKey], out var ID))
                    return "ID is not an number.";

                if (!Options.ContainsKey(IOTypeKey))
                    return "No IO Type specified.";

                if (Options[IOTypeKey] == IOButton && !Options.ContainsKey(ButtonUpdateStateKey))
                    return "Must specify ButtonUpdateStateKey";
            }
            else if (Options[StartStopKey] == Start)
            {
                if (!Options.ContainsKey(ActionNameKey))
                    return "No Action specify.";

                if (!Options.ContainsKey(DoAfterBadOptionsKey))
                    return "Do after bad options not specified.";

                if (!Options.ContainsKey(AutoResetKey))
                    return "Auto Reset not specified.";

                if (!Options.ContainsKey(IntervalKey))
                    return "Interval not specified";

                if (!int.TryParse(Options[IntervalKey], out Interval))
                    return "Interval not a number";
            }
            else
            {
                return "Unkown Start/Stop Value";
            }

            if (Application.CurrentProfile is not PanelProfile CurrentProfile)
                return "CurrentProfile was null.";

            Mapping[] MappingSource;
            Predicate<Mapping> FindingPredicate;

            if (Options[StartStopKey] == Stop)
            {
                if (Options[IOTypeKey] == IOButton)
                {
                    if (!Options.ContainsKey(ButtonUpdateStateKey))
                        return "ButtonUpdateStateKey was not specified.";

                    MappingSource = CurrentProfile.ActionMappings.ToArray();
                    FindingPredicate = (Action) => $"{Action.ID}" == Options[IDKey] && (Action as ActionMapping)?.UpdateState == Options[ButtonUpdateStateKey]?.ToButtonUpdateState();
                }
                else if (Options[IOTypeKey] == IOAbsolute)
                {
                    MappingSource = CurrentProfile.AbsoluteActionMappings.ToArray();
                    FindingPredicate = (AbsoluteAction) => $"{AbsoluteAction.ID}" == Options[IDKey];
                }
                else
                {
                    return $"IOType was not {IOButton} or {IOAbsolute}.";
                }

                if (Array.Find(MappingSource, FindingPredicate) is not Mapping Mapping)
                    return $"Could not find mapping with ID {Options[IDKey]}.";

                if (Mapping is ActionMapping ActionMapping)
                {
                    if (ActionMapping.Action is not Timers ActionTimer)
                        return "Action mapping action is not an instance of Timers";

                    ActionTimer.Timer.Stop();
                }
            }
            else if (Options[StartStopKey] == Start)
            {
                if (Options.ContainsKey(AutoResetKey))
                    Timer.AutoReset = Options[AutoResetKey] == TrueEntry;
                Timer.Interval = Interval;
                Timer.Start();
            }
            else
            {
                return "Unkown Start/Stop Value";
            }

            return null;
        }

        private void TimerElapsed(object? Sender, EventArgs Args)
        {
            if (Application is null)
                return;

            if (!Options.ContainsKey(ActionNameKey))
            {
                Application.Logger.Log(ILogger.Levels.Error, "Action Not Specified.", $"{Application.CurrentProfile?.Name} | Timer");
                return;
            }

            if (!Options.ContainsKey(DoAfterBadOptionsKey))
            {
                Application.Logger.Log(ILogger.Levels.Error, $"{DoAfterBadOptionsKey} not specified.", $"{Application.CurrentProfile?.Name} | Timer");
                return;
            }

            if (Array.Find(Application.Actions, Action => Action.GetPanelActionDescriptor()?.Name == Options[ActionNameKey]) is not Type ActionType)
            {
                Application.Logger.Log(ILogger.Levels.Error, $"Couldn't find action with name {Options[ActionNameKey]}.", $"{Application.CurrentProfile?.Name} | Timer");
                return;
            }

            IPanelAction? ActionToDo;

            try
            {
                ActionToDo = (IPanelAction?)Activator.CreateInstance(ActionType);
            }
            catch (Exception)
            {
                Application.Logger.Log(ILogger.Levels.Error, $"Could not create instance of {Options[ActionNameKey]}.", $"{Application.CurrentProfile?.Name} | Timer");
                return;
            }

            if (ActionToDo is null)
            {
                Application.Logger.Log(ILogger.Levels.Error, $"Could not create instance of {Options[ActionNameKey]}.", $"{Application.CurrentProfile?.Name} | Timer");
                return;
            }

            Dictionary<string, string?> OptionsToSet = new(Options);

            if (OptionsToSet.ContainsKey(StartStopKey))
                _ = OptionsToSet.Remove(StartStopKey);

            if (OptionsToSet.ContainsKey(IDKey))
                _ = OptionsToSet.Remove(IDKey);

            if (OptionsToSet.ContainsKey(IOTypeKey))
                _ = OptionsToSet.Remove(IOTypeKey);

            if (OptionsToSet.ContainsKey(AutoResetKey))
                _ = OptionsToSet.Remove(AutoResetKey);

            if (OptionsToSet.ContainsKey(ActionNameKey))
                _ = OptionsToSet.Remove(ActionNameKey);

            if (OptionsToSet.ContainsKey(DoAfterBadOptionsKey))
                _ = OptionsToSet.Remove(DoAfterBadOptionsKey);

            if (OptionsToSet.ContainsKey(ButtonUpdateStateKey))
                _ = OptionsToSet.Remove(ButtonUpdateStateKey);


            if (ActionToDo.SetOptions(OptionsToSet) is string Message)
            {
                Application.Logger.Log(ILogger.Levels.Warning, $"There was a problem settings options for {Options[ActionNameKey]}. {Message}", $"{Application.CurrentProfile?.Name} | Timer");
                if (Options[DoAfterBadOptionsKey] == FalseEntry)
                    return;
            }

            _ = ActionToDo.Do();
        }

        private void ApplicationStoppedCommunication(object? Sender, EventArgs Args)
        {
            if (Timer.Enabled)
                Timer.Stop();
        }

        private void ApplicationExiting(object? Sender, EventArgs Args)
        {
            if (Timer.Enabled)
                Timer.Stop();
        }

        private void ProfilesChanged(object? Sender, EventArgs Args)
        {
            if (Timer.Enabled)
                Timer.Stop();
        }
    }
}