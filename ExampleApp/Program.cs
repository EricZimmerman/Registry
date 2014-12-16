using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ExampleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var fName1 = @"..\..\UsrClass.dat"; //1274 hbin cells
            var fName2 = @"..\..\NTUser.dat"; //1928 hbin cells
            var fName3 = @"..\..\SOFTWARE";//27758 hbin cells


            //This is a testing harness for now. once parser is complete it will do a lot more

            // a few tests to make sure the hive isnt damaged at a basic level
            var fName1Test = new Registry.Registry(fName1,true);
            var meta1 =   fName1Test.Verify();
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
