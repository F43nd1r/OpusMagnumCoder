using System;
using System.Collections.Generic;
using System.Threading;

namespace OpusMagnumCoder
{
    public abstract class Command
    {
        public abstract (int, ArmState, List<Step>) Resolve(int index, ArmState armState, int trackLoopLength,
            Dictionary<string, List<Command>> macros, Dictionary<string, int> signals);

        public bool Valid { get; protected set; } = true;
    }

    public class ActionCommand : Command
    {
        private readonly Action _action;

        public ActionCommand(Action action)
        {
            _action = action;
        }

        public override (int, ArmState, List<Step>) Resolve(int index, ArmState armState,
            int trackLoopLength,
            Dictionary<string, List<Command>> macros, Dictionary<string, int> signals)
        {
            return (index + 1, _action.Apply(armState), new List<Step> {_action.AsStep(index)});
        }
    }

    public class GotoCommand : Command
    {
        private readonly ArmState _target;

        public GotoCommand(ArmState target)
        {
            _target = target;
        }

        public override (int, ArmState, List<Step>) Resolve(int index, ArmState armState, int trackLoopLength,
            Dictionary<string, List<Command>> macros, Dictionary<string, int> signals)
        {
            List<Step> steps = new List<Step>();
            index = CodeParser.GoTo(index, (i, action) =>
            {
                steps.Add(action.AsStep(i));
                return i + 1;
            }, armState, _target, trackLoopLength);
            return (index, _target, steps);
        }
    }

    public class SignalCommand : Command
    {
        private readonly string _name;

        public SignalCommand(string name)
        {
            _name = name;
        }

        public override (int, ArmState, List<Step>) Resolve(int index, ArmState armState, int trackLoopLength,
            Dictionary<string, List<Command>> macros, Dictionary<string, int> signals)
        {
            lock (signals)
            {
                signals.Add(_name, index);
            }

            return (index, armState, new List<Step>());
        }
    }

    public class WaitCommand : Command
    {
        private readonly string _name;

        public WaitCommand(string name)
        {
            _name = name;
        }

        public override (int, ArmState, List<Step>) Resolve(int index, ArmState armState, int trackLoopLength,
            Dictionary<string, List<Command>> macros, Dictionary<string, int> signals)
        {
            int signalIndex;
            lock (signals)
            {
                while (!signals.TryGetValue(_name, out signalIndex))
                {
                    if (!Monitor.Wait(signals, 100))
                    {
                        Valid = false;
                        break;
                    }
                }
            }

            if (index < signalIndex)
            {
                index = signalIndex;
            }

            return (index, armState, new List<Step>());
        }
    }

    public class MacroCommand : Command
    {
        private readonly string _name;

        public MacroCommand(string name)
        {
            _name = name;
        }

        public override (int, ArmState, List<Step>) Resolve(int index, ArmState armState, int trackLoopLength, Dictionary<string, List<Command>> macros, Dictionary<string, int> signals)
        {
            List<Step> steps = new List<Step>();
            if (macros.ContainsKey(_name))
            {
                foreach (var command in macros[_name])
                {
                    List<Step> intermediate;
                    (index, armState, intermediate) =
                        command.Resolve(index, armState, trackLoopLength, macros, signals);
                    steps.AddRange(intermediate);
                }

            }
            else
            {
                Valid = false;
            }

            return (index, armState, steps);
        }
    }

    public class EmptyCommand : Command
    {
        public override (int, ArmState, List<Step>) Resolve(int index, ArmState armState, int trackLoopLength, Dictionary<string, List<Command>> macros, Dictionary<string, int> signals)
        {
            return (index + 1, armState, new List<Step>());
        }
    }

    public class InvalidCommand : Command
    {
        private readonly string _name;

        public InvalidCommand(string name)
        {
            _name = name;
        }
        public override (int, ArmState, List<Step>) Resolve(int index, ArmState armState, int trackLoopLength, Dictionary<string, List<Command>> macros, Dictionary<string, int> signals)
        {
            Valid = false;
            return (index, armState, new List<Step>());
        }
    }
}