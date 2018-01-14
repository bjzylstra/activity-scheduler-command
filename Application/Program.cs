using System;

namespace ActivityScheduler
{
    class Program
    {
        static int Main(string[] args)
        {
            var programArguments = ProgramArguments.Create(args);

            if (programArguments == null) return -1;

            var activityDefinitions = ActivityDefinition.ReadActivityDefinitions(programArguments.ActivityDefinitionsPath);

            if (activityDefinitions == null) return -2;

            Console.WriteLine("Found {0} activity definitions in the file: {1}",
                activityDefinitions.Count, programArguments.ActivityDefinitionsPath);

            var camperRequestsList = CamperRequests.ReadCamperRequests(programArguments.CamperRequestsPath,
                activityDefinitions);

            if (camperRequestsList == null) return -2;

            Console.WriteLine("Found {0} campers in the file: {1}",
                camperRequestsList.Count, programArguments.CamperRequestsPath);

            return 0;
        }
    }
}
