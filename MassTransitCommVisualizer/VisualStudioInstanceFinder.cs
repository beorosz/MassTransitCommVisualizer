using System;
using System.Linq;
using Microsoft.Build.Locator;

namespace MassTransitCommVisualizer
{
    public interface IVisualStudioInstanceFinder
    {
        void RegisterInstalledVisualStudioInstance();
    }

    public class VisualStudioInstanceFinder : IVisualStudioInstanceFinder
    {
        private int[] vsMajorVersionsToCheck = { 17, 16 };
        public void RegisterInstalledVisualStudioInstance()
        {

            Console.WriteLine($"Looking for compatible Visual Studio instance...");
            var visualStudioInstances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
            var instance = visualStudioInstances
                .Where(vsInstance => vsMajorVersionsToCheck.Contains(vsInstance.Version.Major))
                .OrderByDescending(vsInstance => vsInstance.Version.Major)
                .FirstOrDefault();
            if (instance != null)
            {
                MSBuildLocator.RegisterInstance(instance);
                Console.WriteLine($"Instance '{instance.Name}' found and registered.");
                return;
            }

            throw new ApplicationException($"No compatible Visual Studio version found. Was looking for versions: {String.Join(", ", vsMajorVersionsToCheck)}");
        }
    }
}
