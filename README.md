# Registry #


Full featured, offline registry parser in C#

## The goals of this project are:  ##

1. full parsing of all known registry structures
2. Make registry value slack space accessible
3. Deleted key support
4. ???



**Testing metrics**

Total hives processed: 106<br />
total hbins processed: 120,494<br />
Total records processed OK: 5,552,415<br />
total records with errors: 647<br />

Success rate: 5552415 / 5553062 = 99.98834877046213 %<br />
Fail rate: 647 / 5553062 = 0.0116512295378658 %<br />

(many of the errors are vk records that are free and do not contain all necessary data)

### Example data ###

Find below examples of the kinds of data that will be exposed. Of course, you dont have to deal with any of this if you just want the normal key, subkey and values. The output below is what ToString() generates for each object. All offsets are resolved and the entire hive is accessible via traditional object oriented methods using collections, linq, etc.

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



