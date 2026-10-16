using System.Diagnostics;
using NUnit.Framework;

namespace Registry.Test;

[SetUpFixture]
public class TestSetup
{
    //  public static RegistryHiveOnDemand SamOnDemand;
    //   public static RegistryHiveOnDemand SamHasBigEndianOnDemand;
    // public static RegistryHiveOnDemand SamDupeNameOnDemand;
    //  public static RegistryHiveOnDemand SystemOnDemand;
    //   public static RegistryHiveOnDemand NtUser1OnDemand;
    // public static RegistryHiveOnDemand UsrClassDeletedBagsOnDemand;
    //   public static RegistryHiveOnDemand SoftwareOnDemand;
    //    public static RegistryHive NtUserSlack;
    // public static RegistryHive UsrClass1;
    // public static RegistryHiveOnDemand UsrClass1OnDemand;
    //    public static RegistryHive UsrClassBeef;
    //   public static RegistryHive Bcd;
    //   public static RegistryHive UsrclassDeleted;
    //  public static RegistryHive UsrclassAcronis;
    //   public static RegistryHive Sam;
    //  public static RegistryHive SamRootValue;
    //   public static RegistryHiveOnDemand Security;
    // public static RegistryHiveOnDemand DriversOnDemand;
    //   public static RegistryHiveOnDemand UsrClassFtp;
    // public static RegistryHive System;
    //   public static RegistryHiveOnDemand SanOther;
    //  public static RegistryHive Drivers;

    //ncrunch: no coverage start

    [OneTimeSetUp]
    public void InitializeObjects()
    {
        Debug.WriteLine("Initializing hives...");
        //SamOnDemand = new RegistryHiveOnDemand(TestHives.Path(@"SAM"));

        //var SamHasBigEndianOnDemand = new RegistryHiveOnDemand(TestHives.Path(@"SAM_hasBigEndianDWord"));
        //   var   SamDupeNameOnDemand = new RegistryHiveOnDemand(TestHives.Path(@"SAM_DUPENAME"));
        //var NtUser1OnDemand = new RegistryHiveOnDemand(TestHives.Path(@"NTUSER1.DAT"));

        //var UsrClassDeletedBagsOnDemand = new RegistryHiveOnDemand(TestHives.Path(@"UsrClassDeletedBags.dat"));
        // var SoftwareOnDemand = new RegistryHiveOnDemand(TestHives.Path(@"SOFTWARE"));
        //  var     SystemOnDemand = new RegistryHiveOnDemand(TestHives.Path(@"SYSTEM"));

//          var  Bcd = new RegistryHive(TestHives.Path(@"BCD"));
//            Bcd.FlushRecordListsAfterParse = false;
//            Bcd.RecoverDeleted = true;
//            Bcd.ParseHive();

//          var  UsrclassDeleted = new RegistryHive(TestHives.Path(@"UsrClassDeletedBags.dat"));
//            UsrclassDeleted.RecoverDeleted = true;
//            UsrclassDeleted.FlushRecordListsAfterParse = false;
//            UsrclassDeleted.ParseHive();

//            var UsrclassAcronis = new RegistryHive(TestHives.Path(@"Acronis_0x52_Usrclass.dat"));
//            UsrclassAcronis.RecoverDeleted = true;
//            UsrclassAcronis.FlushRecordListsAfterParse = false;
//            UsrclassAcronis.ParseHive();

//           var UsrClass1 = new RegistryHive(TestHives.Path(@"UsrClass 1.dat"));
//            UsrClass1.RecoverDeleted = true;
//            UsrClass1.FlushRecordListsAfterParse = false;
//            UsrClass1.ParseHive();

        //var UsrClass1OnDemand = new RegistryHiveOnDemand(TestHives.Path(@"UsrClass 1.dat"));

//         var   UsrClassBeef = new RegistryHive(TestHives.Path(@"UsrClass BEEF000E.dat"));
//            UsrClassBeef.RecoverDeleted = true;
//            UsrClassBeef.FlushRecordListsAfterParse = false;
//            UsrClassBeef.ParseHive();

//           var NtUserSlack = new RegistryHive(TestHives.Path(@"NTUSER slack.DAT"));
//            NtUserSlack.FlushRecordListsAfterParse = false;
//            NtUserSlack.ParseHive();

//            Sam = new RegistryHive(TestHives.Path(@"SAM"));
//            Sam.FlushRecordListsAfterParse = false;
//            Sam.ParseHive();


//           var  Security = new RegistryHiveOnDemand(TestHives.Path(@"SECURITY"));
//           var DriversOnDemand = new RegistryHiveOnDemand(TestHives.Path(@"DRIVERS"));

//           var Drivers = new RegistryHive(TestHives.Path(@"DRIVERS"));
//            Drivers.FlushRecordListsAfterParse = false;
//            Drivers.RecoverDeleted = true;
//            Drivers.ParseHive();

//          var  System = new RegistryHive(TestHives.Path(@"System"));
//            System.FlushRecordListsAfterParse = false;
//            System.ParseHive();

//          var  SanOther = new RegistryHiveOnDemand(TestHives.Path(@"SAN(OTHER)"));
//          var  UsrClassFtp = new RegistryHiveOnDemand(TestHives.Path(@"UsrClass FTP.dat"));
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        Debug.WriteLine("Unit testing complete. Tearing down...");
    }
}