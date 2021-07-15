# Binskim

BinSkim is a checker that examines Portable Executable (PE) files and their associated Program Database File Formats (PDB) to identify various security problems. These include:

- **Use of Outdated Compiler Tool Sets** - Binaries should be compiled against the most recent compiler tool sets wherever possible to maximize the use of current compiler-level and OS-provided security mitigations.
- **Insecure Compilation Settings** - Binaries should be compiled with the most secure settings possible to enable OS-provided security mitigations, maximize compiler errors and actionable warnings reporting, among other things.
- **Signing issues** - Signed binaries should be signed with cryptographically-strong algorithms.

## Source and Drop Location

BinSkim is an open-source project on **[GitHub](https://github.com/Microsoft/binskim)**. The latest version is available as a **[NuGet](https://www.nuget.org/packages/Microsoft.CodeAnalysis.BinSkim/)** package. **[Learn more](https://www.nuget.org/)** about NuGet.

## Running BinSkim

### Quick Start

The primary function of BinSkim is to analyze Windows PEs, such as Dynamic Link Libraries (dll) and Executable Files (exe). To analyze a file, pass one or more arguments that resolve one or more PEs.

```csharp
// Analyze a single binary named MyProjectFile.dll found in c:\temp
binskim.exe analyze c:\temp\MyProjectFile.dll

// Analyze all files with the .dll or .exe extension starting in the
// current working directory and recursing through all child directories
binskim analyze *.exe *.dll --recurse

// Analyze all files with the .dll extension starting in the current
// current directory and write results to a SARIF log file
binskim analyze *.dll --output MyLog.sarif 
```

### Help Reference

| Command Type | Description |
| ------------ | ----------- |
| **General** | General BinSkim help message. Displays all built-in commands (e.g. help, analyze and capture) for which more detailed help can be requested  `binskim.exe --help` |
| **Detailed** | <p>Specific commands. Structure looks like this: `binskim.exe help [command]`</p><ul><li>`binskim.exe help analyze`</li><li>`binskim.exe help exportRules`</li><li>`binskim.exe help exportConfig`</li><li>`binskim.exe help dump`</li><li>`binskim.exe help version`</li></ul> |

### Analyze Command
The **`analyze`** command supports the following additional arguments:

| Argument (short form, long form) | Meaning |
| -------------------------------- | ------- |
| **`--trace`** | Execution traces, expressed as a semicolon-delimited list, that should be emitted to the console and log file (if appropriate). Valid values: PdbLoad. |
| **`--sympath`** | Symbols path value (e.g. `SRV http://msdl.microsoft.com/download/symbols or Cache d:\symbols;Srv http://symweb`) |
| **`--local-symbol-directories`** | A set of semicolon-delimited local directory paths that will be examined when attempting to locate PDBs. |
| **`-o, --output`** | File path used to write and output analysis using [SARIF](https://github.com/Microsoft/sarif-sdk) |
| **`-r, --recurse`** | Recurse into subdirectories when evaluating file specifier arguments |
| **`-c, --config`** | (Default: ‘default’) Path to policy file to be used to configure analysis. Passing value of 'default' (or omitting the argument) invokes built-in settings |
| **`-q, --quiet`** | Do not log results to the console |
| **`-s, --statistics`** | Generate timing and other statistics for analysis session |
| **`-h, --hashes`** | Output hashes of analysis targets when emitting SARIF reports |
| **`-e, --environment`** | <p>Log machine environment details of run to output file.</p><p>**WARNING:** This option records potentially sensitive information (such as all environment variable values) to the log file.</p> |
| **`-p, --plugin`** | Path to plugin that will be invoked against all targets in the analysis set. |
| **`--rich-return-code`** | Output a more detailed exit code consisting of a series of flags about execution, rather than outputting '0' for success/'1' for failure (see codes below) |
| **`--level`** | Filter output of scan results to one or more failure levels. Valid values: Error, Warning and Note. |
| **`--kind`** | Filter output one or more result kinds. Valid values: Fail (for literal scan results), Pass, Review, Open, NotApplicable and Informational. |
| **`--baseline`** | A Sarif file to be used as baseline. |
| **`-v, --sarif-output-version`** | (Default: Current) The SARIF version of the output log file. Valid values are OneZeroZero and Current |

In addition to the named arguments above, BinSkim accepts one or more specifiers to a file, directory, or filter pattern that resolves to one or more binaries to analyze. Arguments can include wild cards, relative paths (in which case the file or directory path is resolved relative to the current working directory), and environment variables.

All these arguments can be applied one or more times on the command-line. For analysis to occur, at least one specifier must be passed that resolves to one or more files.

#### --sympath

The `--sympath` argument provides a path to a symbol server. The syntax for this argument is identical to the symbol path provided to Windows debuggers, as documented **[here](https://msdn.microsoft.com/en-us/library/windows/hardware/ff558829(v=vs.85).aspx)**. The symbol path can also be used to specify a directory location to cache any downloaded symbols. Note that BinSkim will clear the _NT_SYMBOL_PATH environment variable at runtime to prevent unexpected network activity or slowdowns related to PDB acquisition during analysis. Use `--sympath` to provide symbol server information instead. *IMPORTANT*: be sure to specific a `Cache*` component to your symbol path if at all possible. This will greatly improve performance, as BinSkim will download the PDB locally and scan from there, rather than crawling the PDB across the network.

**NOTE:** BinSkim requires PDBs to complete a significant subset of its analysis (see list below) which should be located along a target .dll or .exe. BinSkim explicitly clears any symbol path configured in the environment via `%_NT_SYMBOL_PATH%` 

When BinSkim cannot properly load a PDB, because it is missing, corrupted, etc., the tool will emit an instance of `ERR97`. This message will report the problem including information on the specific `HRESULT` (and its meaning) error code returned by the PDB loading API. Here's an example: `error ERR997.ExceptionLoadingPdb : BA2013 : 'symsrv.dll' was not evaluated for check 'InitializeStackProtection' because its PDB could not be loaded -- (E_PDB_NOT_FOUND (File not found))`. See the documentation for `ERR97` in the [Rules and Errors Troubleshooting Guide](https://github.com/microsoft/binskim/blob/master/docs/RulesAndErrorsTroubleshootingGuide.md) for more information.

The following table lists all BinSkim rules by ID and Name, detailing specific PDB information examined during analysis. Generally, each of these checks also inspects each object module language in order to restrict analysis to Microsoft C/C++ compilers.

| ID | Name | Data Examined |
| -- | ---- | ------------- |
| **BA2002** | `DoNotIncorporateVulnerableDependencies` | Source files for all linked object modules |
| **BA2006** | `BuildWithSecureTools` | Compiler version of all linked object modules |
| **BA2007** | `EnableCriticalCompilerWarnings` | Compiler warning level and explicitly disabled warnings for all linked object modules |
| **BA2011** | `EnableStackProtection` | `IDiaSymbol::get_hasSecurityChecks` for all linked object modules |
| **BA2013** | `InitializeStackProtection` | Scans PDB for /GS feature function name |
| **BA2014** | `DoNotDisableStackProtectionForFunctions` | `IDiaSymbol::get_isSafeBuffers` value for all binary functions |
| **BA2024** | `EnableSpectreMitigations` | Compiler version of all linked object modules |
  
#### --local-symbol-directories

The `--local-symbol-directories` argument configures a set of semicolon-delimited local directory paths that will be examined when attempting to locate PDBs. Provide this argument when your build system redirects PDB production to an alternate location (rather than emitting them alongside their matching binary).

#### -o, --output

The `-o` or `--output` argument specifies a file path to which BinSkim’s SARIF-formatted results will be written. The Microsoft SARIF SDK ships with a Microsoft Visual Studio Add-In that can be compiled and used to load SARIF log files into the Microsoft Visual Studio IDE.

#### -r, --recurse

The `-r` or `--recurse` argument will recurse into child directories for each file specifier passed on the command-line. If the argument does not appear on the command-line, each file specifier will be resolved against the provided directory, if there is one, or the current working directory, if there is not.

#### -c, --config

The `-c` or `--config` argument can be used to pass settings, rendered as XML, that can be used to reconfigure analysis.  Accepts a single argument that specifies the path of the configuration file. See the `exportConfig` command for information on generating a preliminary configuration file that can be modified and passed back into BinSkim to reconfigure analysis.

#### -q, --quiet

The `-q` or `--quiet` argument suppresses BinSkim console output. BinSkim will raise an error when the -q is specified without providing a log file location to persist result via the `-o` argument.

#### -s, --statistics

The `-s` or `--statistics` argument configures BinSkim to record and report on various statistics related to the analysis, including total time-of-execution, the number of valid analysis targets the command-line run resolved to, and the number of invalid targets (i.e., non-portable executable files) specified for analysis.

#### -h, --hashes

The `-h` or `--hashes` argument configures BinSkim to emit MD5, SHA1 and SHA256 hashes of analysis targets to the SARIF output log specified via the `-o` argument. BinSkim will raise an exception in cases when -h is specified but no SARIF results file is configured via the `--output` argument. File hashes are emitted to log files to assist in results caching as well as scenarios where it is helpful to verify for auditing, compliance or other purposes that a specific version of a file was analyzed.

#### -e, --environment

The `-e` or `--environment` argument configures BinSkim to emit machine environment details to the SARIF log file specified via the -o argument. This information includes the user account, machine name, working directory and complete set of environment variables and definitions that were present during the analysis run. This information may be useful in some tool troubleshooting scenarios. WARNING: the information emitted by the -e argument may represent unwanted information disclosure.

#### --rich-return-code

The `--rich-return-code` argument configures BinSkim to exit with a detailed exit code consisting of a series of flags about possible exit states, rather than exiting with a simple `0` for success/`1` for failure.  Note that multiple errors may occur in a single run--for instance, if one or more errors fired (`0x80000000`) one or more warnings fired (`0x4000000`), and we encountered an exception in then Analyze() function for a skimmer (`0x8`), the exit code would be `0xC0000008`.

Non-fatal warnings correspond to behaviors that should be expected during normal successful operation of the tool--for instance, the tool can execute successfully and still find errors.

| Name | Value | Explanation/Guidance |
| -- | ---- | ------------- |
| **InvalidCommandLineOption** | `0x1` | Invalid command line options were passed to BinSkim. Please check your command line options.  |
| **ExceptionInSkimmerInitialize** | `0x2` | A Skimmer/Rule was unable to initialize. That rule will be disabled during this run. Please report this to the BinSkim team. |
| **ExceptionRaisedInSkimmerCanAnalyze** | `0x4` | A Skimmer/Rule encountered an exception when attempting to determine if it applied to a target file. That rule will be disabled for the remainder of the run. Please report this to the BinSkim team. |
| **ExceptionInSkimmerAnalyze** | `0x8` | An exception was raised when a skimmer attempted to analyze a file. That rule will be disabled for the remainder of the run. Please report this to the BinSkim team. |
| **ExceptionCreatingLogFile** | `0x10` | BinSkim was unable to write to the log file you specified on the command line. The file may already exist, or you may not have permission to write to the folder you specified. |
| **ExceptionLoadingPdb** | `0x20` | BinSkim encountered an exception loading a Pdb. This can occur if a PDB is missing, or if it's malformed. Ensure that valid .PDB files are present for each PE binary you wish to scan--BinSkim cannot evaluate some of its rules if they are missing. |
| **ExceptionInEngine** | `0x40` | The BinSkim engine encountered an unexpected exception and execution could not continue. Please report this to the BinSkim team. |
| **ExceptionLoadingTargetFile** | `0x80` | BinSkim failed to load/parse one of the input files. Ensure your input files are valid binaries that BinSkim can parse, and reach out to the BinSkim team if they are.  |
| **ExceptionLoadingAnalysisPlugin** | `0x100` | (**Not Currently Used**) |
| **NoRulesLoaded** | `0x200` | No rules were loaded when BinSkim was started. This likely indicates you are missing the library containing the rules, or we encountered exceptions while trying to instantiate all of the rules. (BinSkim.Rules.dll). Please check your BinSkim installation, and if you're certain it's correct, reach out to the BinSkim team. |
| **NoValidAnalysisTargets** | `0x400` | No targets provided at the command line were valid for analysis by BinSkim.  Check that the path you provided to BinSkim exists and contains valid binaries that BinSkim can analyze. |
| **RuleMissingRequiredConfiguration** | `0x800` | Configuration for a rule is missing/incorrect.  Check the configuration file you provided. |
| **TargetParseError** | `0x1000` | (**Not Currently Used**) |
| **MissingFile** | `0x2000` | A file provided on the command line was not present--for instance, the configuration file specified is missing.  Check that any configuration or plugin files you provided are present. |
| **ExceptionAccessingFile** | `0x4000` |  BinSkim was unable to load a file provided on the command line, but it exists--for instance, it could not read the configuration file you specified.  Check the permissions to any configuration or plugin files you provided. |
| **ExceptionInstantiatingSkimmers** | `0x8000` | BinSkim encountered an unexpected error instantiating the rules, and could not recover.  Please report this to the BinSkim team. |
| **RuleCannotRunOnPlatform** | `0x08000000` | A rule could not execute on the current platform/operating system.  Some rules require Windows specific APIs, so if you are executing on non-Windows platforms this is expected. (Non-Fatal) (**Not Yet Used**) |
| **RuleNotApplicableToTarget** | `0x10000000` | A rule did not apply to a particular target. (Non-Fatal) |
| **TargetNotValidToAnalyze** | `0x20000000` | A target passed to BinSkim was not valid to analyze (for instance, if you pass a .txt file to BinSkim). (Non-Fatal) |
| **OneOrMoreWarningsFired** | `0x40000000` | The tool's results included one or more Warnings. (Non-Fatal) |
| **OneOrMoreErrorsFired** | `0x80000000` | The tool's results included one or more Errors. (Non-Fatal) |

This leads to these masks being helpful when determining what to do with a rich exit code:

| Name | Value | Explanation/Guidance |
| -- | ---- | ------------- |
| **NonFatalExitCode** | 0xF8000000 | These are the currently explicitly reserved non-fatal exit codes--they will occur during normal execution of the tool.  They may be helpful for checking if the tool found any issues or similar during its execution. |
| **FatalExitCode** | 0x0000FFFF | These are all the explicitly reserved fatal exit codes--they indicate something unexpected went wrong during execution, or that a target that we expected to be able to analyze could not be fully analyzed (for example, the .PDB file was missing, or the file was incorrectly formatted).  This may be helpful for checking during any tool run. |

Note--In the future we may add add new fatal or non-fatal exit codes to this command.  They will be documented here and in the release documentation.

#### -p, --plugin

The `-p` or `--plugin` argument is used to provide a path to a BinSkim plugin that will be loaded and invoked at analysis time, in addition to the built-in checks. This argument can be specified multiple times on the command-line.

## BinSkim Release History

The latest version is always available on **[NuGet](https://www.nuget.org/packages/Microsoft.CodeAnalysis.BinSkim/)**. History is available on **[NuGet Release History](../src/ReleaseHistory.md)**.
