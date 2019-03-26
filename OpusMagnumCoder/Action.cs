using System;

namespace OpusMagnumCoder
{
    public enum Action
    {
        ROTATE_CLOCKWISE = 'R',
        ROTATE_COUNTERCLOCKWISE = 'r',
        EXTEND = 'E',
        RETRACT = 'e',
        GRAB = 'G',
        DROP = 'g',
        PIVOT_CLOCKWISE = 'P',
        PIVOT_COUNTERCLOCKWISE = 'p',
        FORWARD = 'A',
        BACK = 'a',
        REPEAT = 'C',
        RESET = 'X',
        NOOP = 'O'
    }

    public static class ActionExtension
    {
        public static ArmState Apply(this Action action, ArmState state)
        {
            switch (action)
            {
                case Action.ROTATE_CLOCKWISE:
                    state.Rotation--;
                    break;
                case Action.ROTATE_COUNTERCLOCKWISE:
                    state.Rotation++;
                    break;
                case Action.EXTEND:
                    state.Length++;
                    break;
                case Action.RETRACT:
                    state.Length--;
                    break;
                case Action.GRAB:
                    state.Grabbed = true;
                    break;
                case Action.DROP:
                    state.Grabbed = false;
                    break;
                case Action.PIVOT_CLOCKWISE:
                case Action.PIVOT_COUNTERCLOCKWISE:
                    break;
                case Action.FORWARD:
                    state.TrackPosition++;
                    break;
                case Action.BACK:
                    state.TrackPosition--;
                    break;
                case Action.REPEAT:
                case Action.RESET:
                    throw new NotImplementedException();
                case Action.NOOP:
                    break;
            }

            return state;
        }

        public static Step AsStep(this Action action, int index)
        {
            return new Step(index, action);
        }
    }
}