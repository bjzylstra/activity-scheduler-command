using System;

namespace ActivityScheduler
{
    class Program
    {
        static int Main(string[] args)
        {
            var programArguments = ProgramArguments.Create(args);

            if (programArguments == null) return -1;

            var camperRequestsList = CamperRequests.ReadCamperRequests(programArguments.InputCSV);

            if (camperRequestsList == null) return -2;

            Console.WriteLine("Found {0} campers in the csv file: {1}", 
                camperRequestsList.Count, programArguments.InputCSV);
            return 0;
        }
    }
}
