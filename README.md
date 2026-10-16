# Registry

Full featured, offline Registry hive parser for .NET (`netstandard2.0`, NuGet package `Registry`): keys, values, value slack, deleted keys and values, transaction logs, and the underlying records with their raw bytes. It is what RECmd (and rla), Registry Explorer, ShellBags Explorer (and SBECmd), AmcacheParser, AppCompatCacheParser, the RECmd plugins and others use.

## Usage

```csharp
var hive = new RegistryHive(@"C:\Temp\SYSTEM");

//a dirty hive (its sequence numbers differ): replay its transaction logs first
if (hive.Header.PrimarySequenceNumber != hive.Header.SecondarySequenceNumber)
{
    hive.ProcessTransactionLogs(new List<string> { @"C:\Temp\SYSTEM.LOG1", @"C:\Temp\SYSTEM.LOG2" }, true);
}

hive.RecoverDeleted = true; //also find deleted keys and values (DeletedRegistryKeys, UnassociatedRegistryValues)
hive.ParseHive();

var key = hive.GetKey(@"ControlSet001\Services\Tcpip\Parameters"); //null when it isn't there; case doesn't matter
foreach (var value in key.Values)
{
    Console.WriteLine($"{value.ValueName} ({value.ValueType}): {value.ValueData}");
}
```

`RegistryHiveOnDemand` reads only what a `GetKey` call needs (no deleted records, no transaction logs): much faster for a few lookups in a big hive. Only the values and subkeys of the key returned are filled in.

```csharp
var hive = new RegistryHiveOnDemand(@"C:\Temp\SOFTWARE");
var key = hive.GetKey(@"Microsoft\Windows NT\CurrentVersion");
```

### Transaction logs

`ProcessTransactionLogs` applies a dirty hive's logs as Windows does (see [Maxim Suhanov's Windows registry file format specification](https://github.com/msuhanov/regf), CC BY 4.0):

- New format logs (Windows 8.1 and later, `.LOG1`/`.LOG2`): log entries from the sequence number in the log's base block on, each one more than the last, until one is out of sequence or invalid (hashes, size). Both logs are used, the one with the earlier entries first. The hive grows when an entry says the hive bins data is bigger.
- Old format logs (`.LOG`, and `.LOG1`/`.LOG2` before Windows 8.1): the dirty pages of the newer log.
- A hive whose base block is invalid gets it back from the log.

The recovered hive is byte for byte what [yarp](https://github.com/msuhanov/yarp) recovers, for every dirty hive tested (a local corpus, the DFIR Artifact Museum and yarp's test hives).

### Damaged hives

Damaged or truncated hives are parsed as far as they can be: a missing cell or list, a key referenced twice, a subkey list pointing back at a parent, or a cell or hive bin with an impossible size are logged and skipped. `HardParsingErrors` and `SoftParsingErrors` count the records that could not be read (in use and free).

## Building and testing

The SDK is pinned in `global.json` (.NET 10). `dotnet test` runs the tests in `Registry.Test`, including the ones that use a local corpus of hives (`Corpus/Registry` next to the repository, or `REGISTRY_CORPUS`), which are skipped when it isn't there.

### Example data ###

Find below examples of the kinds of data that will be exposed. Of course, you don't have to deal with any of this if you just want the normal key, subkey and values. The output below is what ToString() generates for each object. All offsets are resolved and the entire hive is accessible via traditional object oriented methods using collections, linq, etc.

**Security Cell Record**

Size: 0xC8<br />
Signature: sk<br />
IsFree: False

FLink: 0x2F88C68<br />
BLink: 0x21D1078

ReferenceCount: 1

Security descriptor length: 0xB0

Security descriptor: Revision: 0x1<br />
Control: SeDaclPresent, SeSaclPresent, SeDaclAutoInherited, SeSaclAutoInherited, SeDaclProtected, SeSelfRelative

Owner offset: 0x94<br />
Owner SID: S-1-5-32-544<br />
Owner SID Type: BuiltinAdministrators

Group offset: 0xA4<br />
Group SID: S-1-5-18<br />
Group SID Type: LocalSystem

DaclrOffset: 0x1C<br />
DACL: ACL Size: 0x2<br />
ACL Type: Discretionary<br />
ACE Records Count: 5

------------ Ace record #0 ------------<br />
ACE Size: 0x18<br />
ACE Type: AccessAllowedAceType<br />
ACE Flags: ContainerInheritAce<br />
Mask: QueryValue, EnumerateSubkeys, Notify, ReadControl<br />
SID: S-1-5-32-545<br />
SID Type: BuiltinUsers<br />
SID Type Description: S-1-5-32-545: A built-in group. After the initial installation of the operating system, the only member is the Authenticated Users group. When a computer joins a domain, the Domain Users group is added to the Users group on the computer.

------------ Ace record #1 ------------<br />
ACE Size: 0x18<br />
ACE Type: AccessAllowedAceType<br />
ACE Flags: ContainerInheritAce<br />
Mask: FullControl<br />
SID: S-1-5-32-544<br />
SID Type: BuiltinAdministrators<br />
SID Type Description: S-1-5-32-544: A built-in group. After the initial installation of the operating system, the only member of the group is the Administrator account. When a computer joins a domain, the Domain Administrators group is added to the Administrators group. When a server becomes a domain controller, the Enterprise Administrators group also is added to the Administrators group.

------------ Ace record #2 ------------<br />
ACE Size: 0x14<br />
ACE Type: AccessAllowedAceType<br />
ACE Flags: ContainerInheritAce<br />
Mask: FullControl<br />
SID: S-1-5-18<br />
SID Type: LocalSystem<br />
SID Type Description: S-1-5-18: An account that is used by the operating system.

------------ Ace record #3 ------------<br />
ACE Size: 0x14<br />
ACE Type: AccessAllowedAceType<br />
ACE Flags: ContainerInheritAce<br />
Mask: FullControl<br />
SID: S-1-3-0<br />
SID Type: CreatorOwner<br />
SID Type Description: S-1-3-0: A placeholder in an inheritable access control entry (ACE). When the ACE is inherited, the system replaces this SID with the SID for the object's creator.

------------ Ace record #4 ------------<br />
ACE Size: 0x18<br />
ACE Type: AccessAllowedAceType<br />
ACE Flags: ContainerInheritAce<br />
Mask: QueryValue, EnumerateSubkeys, Notify, ReadControl<br />
SID: S-1-15-2-1<br />
SID Type: AllAppPackages<br />
SID Type Description: S-1-15-2-1: All applications running in an app package context.

SaclOffset: 0x14<br />
SACL: ACL Size: 0x2<br />
ACL Type: Security<br />
ACE Records Count: 0


**NK Cell Record**

Size: 0x90<br />
Signature: nk<br />
Flags: HiveEntryRootKey, NoDelete, CompressedName

Last Write Timestamp: 11/26/2014 4:42:54 PM -07:00

IsFree: False

Debug: 0x0

MaximumClassLength: 0x0<br />
ClassCellIndex: 0x0<br />
ClassLength: 0x0<br />

MaximumValueDataLength: 0x0<br />
MaximumValueDataLength: 0x0<br />
MaximumValueNameLength: 0x0

NameLength: 0x39<br />
MaximumNameLength: 0x2C<br />
Name: CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}<br />
Padding: 00-39-00-31-00-45-00

ParentCellIndex: 0x340<br />
SecurityCellIndex: 0xB0

SubkeyCountsStable: 0x1F<br />

SubkeyListsStableCellIndex: 0x2EE0750

SubkeyCountsVolatile: 0x1

UserFlags: 0x0<br />
VirtualControlFlags: 0x0<br />
WorkVar: 0x330038

ValueListCellIndex: 0x0


**Value Key Cell Record**

Size: 0x28<br />
Signature: vk<br />
Data Type: RegSz<br />

IsFree: False

DataLength: 0x4A<br />
OffsetToData: 0x69648E8

NameLength: 0xE<br />
NamePresentFlag: 0x1

ValueName: ReleaseVersion<br />
ValueData: 13.251.9001.1001-140704a-173665E-ATI<br />
ValueDataSlack: 96-06

**LH/LF List records**

Size: 0x10
Signature: lh

IsFree: False

NumberOfEntries: 1

------------ Offset/hash record #0 ------------<br />
Offset: 0x2EE6398, Hash: 4145906403<br />

------------ End of offsets ------------


**RI List record**

Size: 0x18
Signature: ri

IsFree: False

NumberOfEntries: 4

------------ Offset/hash record #0 ------------<br />
Offset: 0xC8F020<br />
------------ Offset/hash record #1 ------------<br />
Offset: 0xCA7020<br />
------------ Offset/hash record #2 ------------<br />
Offset: 0x30C3020<br />
------------ Offset/hash record #3 ------------<br />
Offset: 0x6B53020<br />

------------ End of offsets ------------



Open Source Development funding and support provided by the following contributors: [SANS Institute](http://sans.org/) and [SANS DFIR](http://dfir.sans.org/).
