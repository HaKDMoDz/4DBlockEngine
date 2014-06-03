

/* Code based on: http://code.google.com/p/xnagameconsole/ */

namespace _4DMonoEngine.Core.Debugging.Console
{
    enum OutputLineType
    {
        Command,
        Output
    }

    class OutputLine
    {
        public string Output { get; set; }
        public OutputLineType Type { get; set; }

        public OutputLine(string output, OutputLineType type)
        {
            Output = output;
            Type = type;
        }

        public override string ToString()
        {
            return Output;
        }
    }
}
