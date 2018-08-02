using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KP.PackageVersionConflictLibray;

namespace KP.PackageVersionConflictFinder
{
    class Program
    {
        static void Main(string[] args)
        {
            
            Processor objProcessor = new Processor();

            objProcessor.Process();

            objProcessor.PackageConflictResult();

            objProcessor.PackageConflictReport();
            Console.WriteLine("Press any key to close the console!!");
            Console.ReadLine();



        }
    }
}
