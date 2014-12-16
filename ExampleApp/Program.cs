using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

// namespaces...
namespace ExampleApp
{
    // internal classes...
    internal class Program
    {
        // private methods...
        private static void Main(string[] args)
        {
            var fName1 = @"..\..\UsrClass.dat";//27758 hbin cells


            //This is a testing harness for now. once parser is complete it will do a lot more

            // a few tests to make sure the hive isnt damaged at a basic level
            var fName1Test = new Registry.Registry(fName1, false);
            var meta1 =   fName1Test.Verify();
            fName1Test.ParseHive();
            Trace.Assert(meta1.HasValidHeader == true && meta1.NumberofHBins == 1274);

            //var fName2Test = new Registry.Registry(fName2, true);
            //var meta2 = fName2Test.Verify();
            //Trace.Assert(meta2.HasValidHeader == true && meta2.NumberofHBins == 1928);

            //var fName3Test = new Registry.Registry(fName3, true);
            //var meta3 = fName3Test.Verify();
            //Trace.Assert(meta3.HasValidHeader == true && meta3.NumberofHBins == 27758);

            Debug.Write(1);
        }
    }
}
