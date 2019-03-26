namespace OpusMagnumCoder
{
    public class Step
    {
        public readonly Action Action;
        public readonly int Index;

        public Step(int index, Action action)
        {
            Action = action;
            Index = index;
        }
    }
}