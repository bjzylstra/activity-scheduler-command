using System;

namespace ActivityScheduler
{
    class Program
    {
        static int Main(string[] args)
        {
            ProgramArguments programArguments = ProgramArguments.Create(args);

            if (programArguments == null) return -1;

            Console.WriteLine("File is {0}", programArguments.InputCSV);
            return 0;
        }
    }
}
