using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;

namespace OpusMagnumCoder
{
    public class CodeParser
    {
        private readonly Dictionary<string, string> _macros = new Dictionary<string, string>();
        private readonly ConcurrentDictionary<string, int> _signals = new ConcurrentDictionary<string, int>();

        public string ToCode(Part arm)
        {
            var index = 0;
            var state = new ArmState {Rotation = arm.Rotation, TrackPosition = 0, Length = arm.Size, Grabbed = false};
            var initialState = state;
            return StepsToString(arm.Steps, ref index, ref state, initialState);
        }

        private static string StepsToString(IList<Step> steps, ref int index, ref ArmState state, ArmState initialState)
        {
            var result = "";
            foreach (var step in steps)
            {
                while (index < step.Index)
                {
                    result += "\r\n";
                    index++;
                }

                switch (step.Action)
                {
                    case Action.RESET:
                        if (state.Grabbed)
                        {
                            result += Action.DROP.ToString().ToLowerInvariant() + "\r\n";
                            index++;
                        }

                        index = GoTo(index, (t, a) =>
                        {
                            result += a.ToString().ToLowerInvariant() + "\r\n";
                            return t + 1;
                        }, state, initialState);
                        state = initialState;
                        break;
                    case Action.REPEAT:
                        IList<Step> replay = new List<Step>();
                        var i = steps.IndexOf(step) - 1;
                        while (i >= 0 && steps[i].Action == Action.REPEAT) i--;
                        while (i >= 0 && steps[i].Action != Action.REPEAT)
                        {
                            replay.Insert(0, steps[i]);
                            i--;
                        }

                        var start = steps[i + 1].Index;
                        var subIndex = start;
                        result += StepsToString(replay, ref subIndex, ref state, initialState);
                        index += subIndex - start;
                        break;
                    default:
                        result += step.Action.ToString().ToLowerInvariant() + "\r\n";
                        state = step.Action.Apply(state);
                        break;
                }

                index++;
            }

            return result;
        }

        public void Clear()
        {
            _signals.Clear();
            _macros.Clear();
        }

        public void AddMacro(string s, string l)
        {
            _macros.Add(s.ToLowerInvariant().Trim(), l);
        }

        public void FillFromCode(Part arm, string code)
        {
            var state = new ArmState {Rotation = arm.Rotation, TrackPosition = 0, Length = arm.Size, Grabbed = false};
            var initialState = state;
            var index = 0;
            arm.Steps.Clear();
            ParseLines(arm, Regex.Split(code, @"\r\n"), ref state, initialState, ref index);
        }

        private void ParseLines(Part arm, IEnumerable<string> lines, ref ArmState state, ArmState initialState,
            ref int index)
        {
            foreach (var line in lines)
                switch (line.ToLowerInvariant().Trim())
                {
                    case "":
                        index++;
                        break;
                    case "rc":
                    case "rotate_clockwise":
                        (state, index) = ApplyToArm(arm, state, index, Action.ROTATE_CLOCKWISE);
                        break;
                    case "rcc":
                    case "rotate_counterclockwise":
                        (state, index) = ApplyToArm(arm, state, index, Action.ROTATE_COUNTERCLOCKWISE);
                        break;
                    case "e":
                    case "extend":
                        (state, index) = ApplyToArm(arm, state, index, Action.EXTEND);
                        break;
                    case "r":
                    case "retract":
                        (state, index) = ApplyToArm(arm, state, index, Action.RETRACT);
                        break;
                    case "g":
                    case "grab":
                        (state, index) = ApplyToArm(arm, state, index, Action.GRAB);
                        break;
                    case "d":
                    case "drop":
                        (state, index) = ApplyToArm(arm, state, index, Action.DROP);
                        break;
                    case "pc":
                    case "pivot_clockwise":
                        (state, index) = ApplyToArm(arm, state, index, Action.PIVOT_CLOCKWISE);
                        break;
                    case "pcc":
                    case "pivot_counterclockwise":
                        (state, index) = ApplyToArm(arm, state, index, Action.PIVOT_COUNTERCLOCKWISE);
                        break;
                    case "f":
                    case "forward":
                        (state, index) = ApplyToArm(arm, state, index, Action.FORWARD);
                        break;
                    case "b":
                    case "back":
                        (state, index) = ApplyToArm(arm, state, index, Action.BACK);
                        break;
                    case "rep":
                    case "repeat":
                        Repeat(arm, ref index);
                        break;
                    case "res":
                    case "reset":
                        GoToArm(arm, ref state, ref index, initialState);
                        break;
                    case "noop":
                        arm.Steps.Add(new Step(index++, Action.NOOP));
                        break;
                    default:
                        var gotoMatch = new Regex(@"goto( -?[0-9]+)( -?[0-9]+)?( -?[0-9]+)? *").Match(line);
                        if (gotoMatch.Success)
                        {
                            var target = new ArmState
                            {
                                Rotation = int.Parse(gotoMatch.Groups[1].Value),
                                TrackPosition = gotoMatch.Groups[2].Length > 0
                                    ? int.Parse(gotoMatch.Groups[2].Value)
                                    : state.TrackPosition,
                                Length = gotoMatch.Groups[3].Length > 0
                                    ? int.Parse(gotoMatch.Groups[3].Value)
                                    : state.Length
                            };
                            GoToArm(arm, ref state, ref index, target);
                            break;
                        }

                        var signal = new Regex(@"signal (.+) *").Match(line);
                        if (signal.Success)
                        {
                            _signals.TryAdd(signal.Groups[1].Value, index);
                            break;
                        }

                        var wait = new Regex(@"wait (.+) *").Match(line);
                        if (wait.Success)
                        {
                            int signalIndex;
                            var tries = 0;
                            while (!_signals.TryGetValue(wait.Groups[1].Value, out signalIndex))
                            {
                                tries++;
                                if (tries > 10000) throw new Exception(line + " without signal");
                                Thread.Sleep(10);
                            }

                            if (index < signalIndex) index = signalIndex;
                            break;
                        }

                        if (_macros.ContainsKey(line.ToLowerInvariant().Trim()))
                        {
                            ParseLines(arm, Regex.Split(_macros[line.ToLowerInvariant().Trim()], @"\r\n"), ref state,
                                initialState, ref index);
                            break;
                        }

                        throw new Exception("Cannot parse '" + line + "'");
                }
        }

        private static T GoTo<T>(T t, Func<T, Action, T> action, ArmState fromState, ArmState toState)
        {
            foreach (var tuple in new (Func<ArmState, ArmState, bool>, Action)[]
            {
                ((from, to) => from.Rotation > to.Rotation, Action.ROTATE_CLOCKWISE),
                ((from, to) => from.Rotation < to.Rotation, Action.ROTATE_COUNTERCLOCKWISE),
                ((from, to) => from.TrackPosition < to.TrackPosition, Action.FORWARD),
                ((from, to) => from.TrackPosition > to.TrackPosition, Action.BACK),
                ((from, to) => from.Length < to.Length, Action.EXTEND),
                ((from, to) => from.Length > to.Length, Action.RETRACT)
            })
                while (tuple.Item1(fromState, toState))
                {
                    fromState = tuple.Item2.Apply(fromState);
                    t = action.Invoke(t, tuple.Item2);
                }

            return t;
        }

        private static void GoToArm(Part arm, ref ArmState state, ref int index, ArmState target)
        {
            index = GoTo(index, (i, a) =>
            {
                arm.Steps.Add(a.AsStep(i++));
                return i;
            }, state, target);
            state = target;
        }

        private static (ArmState, int) ApplyToArm(Part arm, ArmState state, int index, Action action)
        {
            state = action.Apply(state);
            arm.Steps.Add(action.AsStep(index++));
            return (state, index);
        }

        private static void Repeat(Part arm, ref int index)
        {
            arm.Steps.Add(new Step(index++, Action.REPEAT));
            throw new NotImplementedException();
        }
    }

    public struct ArmState
    {
        public int Rotation;
        public int TrackPosition;
        public int Length;
        public bool Grabbed;
    }
}