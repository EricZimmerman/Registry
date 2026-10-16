# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

A `netstandard2.0` NuGet library (package id `Registry`) that parses offline Registry hives: keys, values, value slack, deleted records, transaction logs. Its consumers are RECmd (and rla), Registry Explorer, ShellBags Explorer/SBECmd (ShellBags), AmcacheParser (Amcache), AppCompatCacheParser (AppCompatCache), RegistryPlugins, Srum and USBDevices, so keep the public API compatible. Input may be corrupt or hostile.

## Commands

The SDK is pinned in `global.json` (.NET 10). The library uses `LangVersion 10`. Tests target `net10.0` and use NUnit 5 + FluentAssertions 7 (pinned to `[7.2.2,8.0.0)`, since 8.x requires a commercial license).

```bash
dotnet build -c Release                                     # builds Registry.slnx (ExampleApp is net462)
dotnet test -c Release                                      # tests using Corpus/Registry are skipped when it isn't there
dotnet test --filter "FullyQualifiedName~LogReplayTests"    # one fixture
REGISTRY_FUZZ_ROUNDS=10000 REGISTRY_FUZZ_SEED=1 dotnet test --filter "Name~RandomDamageIsHandled"
dotnet pack -c Release                                      # only the library is packable
```

## Parsing

- `RegistryBase` reads the whole file into `FileBytes` and the base block (`Header`). `RegistryHive.ParseHive` walks the hive bins (`HBinRecord.Process`), keeps nk/vk/sk/lk cells in `CellRecords` and lists (lf, lh, li, ri, db) in `ListRecords`, then builds the tree from the root (`GetSubKeysAndValues`) and, with `RecoverDeleted`, the deleted keys and values (`BuildDeletedRegistryKeys`).
- Records read their fields from the hive's bytes in place (`NkCellRecord`/`VkCellRecord` `U16`/`U32`/... helpers). `RawBytes` is still a copy per call; don't use it in hot paths. A field past the end of a damaged record is 0. Names, key paths (`RegistryKey.KeyPath`, rebuilt only when the parent or its path changes), value data (`DataBlockRaw`) and list offsets are worked out once.
- `new ArraySegment<byte>(...).ToArray()` is LINQ's `ToArray` on netstandard2.0 (it boxes and copies a byte at a time): use `Helpers.Slice`. Use `Helpers.AbsSize` for cell sizes (`Math.Abs(int.MinValue)` throws). Guard `Log.Verbose`/`Log.Debug` calls with interpolated strings in per-cell code with `Log.IsEnabled`.
- Only cells whose records need their bytes are copied while walking a bin (lists, sk, lk, and data cells when recovering deleted records); nk and vk records read themselves from the hive.
- `_keyPathKeyMap` ignores case (`OrdinalIgnoreCase`, as Windows compares names by upper case) and is keyed by the keys' own paths. `ExpandKeyPath` still returns lower case paths, as it always has.
- Names with the compressed flag are one byte per character, which Windows widens: Latin-1 (`Helpers.Latin1`), not Windows-1252. An allocated value without the ASCII flag has a UTF-16 name (PR #37).
- Resident value data (data length with the high bit set) is as long as the data length says, 0 to 4 bytes. SAM's RID values have 0 bytes (the RID is the value type, `DataTypeRaw`); device property INT16 values (type 0xFFFF0005, masked to 5 = big endian DWORD) have 2.
- Damaged hives: a truncated file is read as far as it goes; a cell or hbin with an impossible size ends that hbin (or the bins); a missing cell or list, an unexpected list type, and a key referenced twice (including a list that points back at an ancestor, which used to recurse until the stack overflowed) are logged and skipped.
- Deleted records: `HBinRecord.ExtractRecordsFromSlack` finds nk/vk signatures in free cells and in the slack of lists and data. A remnant whose size goes past its cell (issue #20: its cell was merged with the one before and split again for a list) is read from the hive, when it is a whole record (name included). Remnants inside a cell whose name is gone are kept as "(Unable to determine name)".

## Transaction logs

`LogReplay` (used by `RegistryHive.ProcessTransactionLogs` and `TransactionLog.UpdateHiveBytes`) follows Maxim Suhanov's Windows registry file format specification (https://github.com/msuhanov/regf, CC BY 4.0, which the README credits):
- New format (`HvLE` entries, file type 6): from the sequence number in the log's base block on, each entry one more than the last; the first invalid one (signature, size a multiple of 512, hive bins data size a multiple of 4096, dirty pages count, Marvin32 hashes, sequence number) ends the log. Logs reuse their space, so older entries after the current ones are normal and must not be applied (the LogExtend Amcache was unparsable because they were). The hive grows to the entries' hive bins data size (issue #24); pages overwrite in place.
- Both logs: the one with the earlier entries first, then the other when it starts at the next sequence number. A primary file with an invalid base block uses only the log with the latest entries, and its base block comes from that log. A log older than the hive (primary sequence number below the hive's secondary) is not eligible.
- Old format (`DIRT` dirty vector, file type 1 or 2): applied when not older than the primary (last written time; the first hbin's timestamp when the base block is invalid); of two, the newer one.
- The result gets the last sequence number (both fields), the last entry's hive bins data size and flags (bit 0), file type 0 and a new checksum (0 is written as 1, 0xFFFFFFFF as 0xFFFFFFFE).
- yarp (msuhanov/yarp, GPL-3.0) was used only as a black box: its recovered hives are byte for byte the same for every dirty hive in the corpus, the DFIR Artifact Museum and its own test hives. Never copy its code. Where yarp refuses a single LOG1 or LOG2 (it wants both), that log is applied on its own, as yarp's `recover_new` does with it.

## Tests

`Registry.Test` (net10.0); `Hives/` and `Binary/` are copied to the output.
- `TestHives.Path("NAME")` finds a hive in `Hives` (case-insensitively; names were written with Windows separators). `TestHives.Corpus(@"ExpandoTestHives\...")` is a hive in `Corpus/Registry` next to the repository (or `REGISTRY_CORPUS`); the test is ignored when it isn't there. Never copy corpus files into the repo.
- `LogReplayTests` (with `LogBuilder`, which builds primaries and new and old format logs with real hashes): sequence rules, stale entries, growth, two logs, invalid base blocks, old format, and the LogExtend Amcache from the corpus.
- `RobustnessTests`: truncation, cycles, keys listed twice, Latin-1 names, invalid sizes, random damage (`REGISTRY_FUZZ_ROUNDS`, `REGISTRY_FUZZ_SEED`), and issue #20's deleted key in the Lone Wolf SYSTEM hive (corpus). Add regression tests here.
- The older fixtures (`TestRegistryHive`, `TestVKCellRecord`, ...) were converted from NFluent; the `ExpandoTest*` ones use the corpus.
- Validation against yarp over 381 hives (corpus, Museum, yarp's tests, the repo's): every key (path, last written time) and value (name, size, data) is the same, apart from value types (Registry masks device property types to their low 12 bits and maps unknown ones to `RegUnknown`, keeping `DataTypeRaw`).

## Versioning and release

- MinVer sets the version from the git tag (bare tags like `2026.10.0`); there is no `<Version>` in the csproj.
- To release: push to `master`, then `git tag <ver> && git push origin <ver> && gh release create <ver> --verify-tag --generate-notes`. Then bump the consumers listed above.
- `.github/workflows/publish.yml` (identical to GuidMapping's) packs, validates and tests on PRs and pushes to `master`. When a GitHub Release is published, it pushes to NuGet.org using trusted publishing (`NuGet/login` with the `NUGET_USER` repo variable, `EricRZimmerman`).
- Dependabot (`.github/dependabot.yml`) opens monthly PRs for GitHub Actions and NuGet updates.
