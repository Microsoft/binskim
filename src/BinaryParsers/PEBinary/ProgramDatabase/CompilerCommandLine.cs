﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;

using Microsoft.CodeAnalysis.Sarif.Driver;

namespace Microsoft.CodeAnalysis.BinaryParsers.ProgramDatabase
{
    /// <summary>Possible status for commandline switches when searching the PDB (no default state is given).</summary>
    public enum SwitchState
    {
        SwitchNotFound = 0,
        SwitchEnabled = 1,
        SwitchDisabled = -1
    }

    /// <summary>Order of precendence for the provided switches to clarify what to do in the presence of multiple disagreeing copies of the switch.</summary>
    public enum OrderOfPrecedence
    {
        FirstWins = 0,
        LastWins = 1
    }

    /// <summary>Processes command lines stored by compilers in PDBs.</summary>
    internal struct CompilerCommandLine
    {
        // I can't defend this design where the "once-ness" and the level of the warning
        // are part of the same value; but it's what the native compiler does :(
        //
        // See email from the compiler team attached to twcsec-tfs01 bug# 17770
        //
        // (Causes many WTFs like:
        //     cl.exe /c /W1 /wd4265 /w14265 /wo4265 C4265.cpp
        // emits no warning, but
        //     cl.exe /c /W1 /wd4265 /w14265 C4265.cpp
        // does.)

        private enum WarningState
        {
            Level1 = 1,
            Level2 = 2,
            Level3 = 3,
            Level4 = 4,
            AsError,
            Once,
            Disabled
        }

        private static readonly char[] switchPrefix = new char[] { '-', '/' };

        /// <summary>
        /// The raw, unadulterated command line before processing.
        /// </summary>
        public readonly string Raw;

        /// <summary>
        /// The set of warnings explicitly disabled in this command line. Sorted.
        /// </summary>
        public readonly ImmutableArray<int> WarningsExplicitlyDisabled;

        /// <summary>
        /// The warning level (/W1, /W3, /Wall, etc.) in the range [0, 4] from this command line.
        /// </summary>
        public readonly int WarningLevel;

        /// <summary>
        /// Whether or not this command line treats warnings as errors.
        /// </summary>
        public readonly bool WarningsAsErrors;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompilerCommandLine"/> struct from a raw PDB-supplied command line.
        /// </summary>
        /// <param name="commandLine">The raw command line from the PDB.</param>
        public CompilerCommandLine(string commandLine)
        {
            //
            // https://msdn.microsoft.com/en-us/library/thxezb7y.aspx
            //

            this.Raw = commandLine ?? "";
            this.WarningLevel = 0;
            this.WarningsAsErrors = false;
            var explicitWarnings = new Dictionary<int, WarningState>();
            foreach (string argument in ArgumentSplitter.CommandLineToArgvW(commandLine))
            {
                if (!IsCommandLineOption(argument))
                {
                    continue;
                }

                switch (argument.Length)
                {
                    case 2:
                        // /w Disables all compiler warnings.
                        if (argument[1] == 'w')
                        {
                            this.WarningLevel = 0;
                        }
                        break;
                    case 3:
                        if (argument[1] == 'W')
                        {
                            char wChar = argument[2];
                            if (wChar == 'X')
                            {
                                // Treats all compiler warnings as errors.
                                this.WarningsAsErrors = true;
                            }
                            else if (wChar >= '0' && wChar <= '4')
                            {
                                // Specifies the level of warning to be generated by the compiler.
                                this.WarningLevel = wChar - '0';
                            }
                        }
                        break;
                    case 4:
                        if (argument.EndsWith("WX-"))
                        {
                            // (inverse of) Treats all compiler warnings as errors.
                            this.WarningsAsErrors = false;
                        }
                        break;
                    case 5:
                        if (argument.EndsWith("Wall"))
                        {
                            // Displays all /W4 warnings and any other warnings that are not included in /W4
                            this.WarningLevel = 4;
                        }
                        break;
                    case 7:
                        if (argument[1] != 'w')
                        {
                            break;
                        }

                        WarningState state;
                        char mode = argument[2];
                        if (mode == 'd')
                        {
                            // Disables the compiler warning that is specified
                            state = WarningState.Disabled;
                        }
                        else if (mode == 'e')
                        {
                            // Treats as an error the compiler warning that is specified
                            state = WarningState.AsError;
                        }
                        else if (mode == 'o')
                        {
                            // Reports the error only once for the compiler warning that is specified
                            state = WarningState.Once;
                        }
                        else if (mode >= '1' && mode <= '4')
                        {
                            // Specifies the level for a particular warning.
                            // e.g. /w14996 sets 4996 to level 1
                            state = (WarningState)(mode - '1' + (int)WarningState.Level1);
                        }
                        else
                        {
                            break;
                        }

                        int warningNumber;
                        if (!int.TryParse(argument.Remove(0, 3), 0, CultureInfo.InvariantCulture, out warningNumber))
                        {
                            break;
                        }

                        explicitWarnings[warningNumber] = state;
                        break;
                }
            }

            ImmutableArray<int>.Builder explicitlyDisabledBuilder = ImmutableArray.CreateBuilder<int>();
            foreach (KeyValuePair<int, WarningState> entry in explicitWarnings)
            {
                bool isEnabled;
                switch (entry.Value)
                {
                    case WarningState.AsError:
                    case WarningState.Once:
                        isEnabled = true;
                        break;
                    case WarningState.Disabled:
                        isEnabled = false;
                        break;
                    case WarningState.Level1:
                        isEnabled = this.WarningLevel >= 1;
                        break;
                    case WarningState.Level2:
                        isEnabled = this.WarningLevel >= 2;
                        break;
                    case WarningState.Level3:
                        isEnabled = this.WarningLevel >= 3;
                        break;
                    case WarningState.Level4:
                        isEnabled = this.WarningLevel >= 4;
                        break;
                    default:
                        isEnabled = true;
                        Debug.Fail("Unexpected WarningState");
                        break;
                }

                if (!isEnabled)
                {
                    explicitlyDisabledBuilder.Add(entry.Key);
                }
            }

            explicitlyDisabledBuilder.Sort();
            this.WarningsExplicitlyDisabled = explicitlyDisabledBuilder.ToImmutable();
        }

        private static bool IsCommandLineOption(string candidate)
        {
            if (candidate.Length < 2)
            {
                return false;
            }

            char c = candidate[0];
            return c == '/' || c == '-';
        }

        /// <summary>
        /// Determine if a switch is set,unset or not present on the command-line.
        /// </summary>
        /// <param name="switchNames">Array of switches that alias each other and all set the same compiler state.</param>
        /// <param name="overrideNames">Array of switches that invalidate the state of the switches in switchNames.</param>
        /// <param name="defaultState">The default state of the switch should no instance of the switch or its overrides be found.</param>
        /// <param name="precedence">The precedence rules for this set of switches.</param>
        public SwitchState GetSwitchState(string[] switchNames, string[] overrideNames, SwitchState defaultState, OrderOfPrecedence precedence)
        {
            // TODO-paddymcd-MSFT - This is an OK first pass.
            // Unfortunately composite switches get tricky and not all switches support the '-' semantics 
            // e.g. /O1- gets translated to /O1 /O-, the second of which is not supported.
            // Additionally, currently /d2guardspecload is translated into /guardspecload, which may be a bug for ENC
            SwitchState namedswitchesState = SwitchState.SwitchNotFound;

            if (switchNames != null && switchNames.Length > 0)
            {
                // array of strings for the switch name without the preceding switchPrefix to make comparison easier
                string[] switchArray = new string[switchNames.Length];
                string[] overridesArray = null;

                SwitchState namedoverridesState = SwitchState.SwitchNotFound;

                for (int index = 0; index < switchNames.Length; index++)
                {
                    // if present remove the slash or minus
                    switchArray[index] = switchNames[index].TrimStart(switchPrefix);
                }

                if (overrideNames != null && overrideNames.Length > 0)
                {
                    overridesArray = new string[overrideNames.Length];

                    for (int index = 0; index < overrideNames.Length; index++)
                    {
                        // if present remove the slash or minus
                        overridesArray[index] = overrideNames[index].TrimStart(switchPrefix);
                    }
                }

                foreach (string arg in ArgumentSplitter.CommandLineToArgvW(this.Raw))
                {
                    if (IsCommandLineOption(arg))
                    {
                        string realArg = arg.TrimStart(switchPrefix);

                        // Check if this matches one of the names switches
                        for (int index = 0; index < switchArray.Length; index++)
                        {
                            if (realArg.StartsWith(switchArray[index]))
                            {
                                // partial stem match - now check if this is a full match or a match with a "-" on the end
                                if (realArg.Equals(switchArray[index]))
                                {
                                    namedswitchesState = SwitchState.SwitchEnabled;
                                    // not necessary at this time, but here for completeness...
                                    namedoverridesState = SwitchState.SwitchDisabled;
                                }
                                else if (realArg[switchArray[index].Length] == '-')
                                {
                                    namedswitchesState = SwitchState.SwitchDisabled;
                                }
                                // Else we have a stem match - do nothing
                            }
                        }

                        // check if this matches one of the named overrides
                        if (overridesArray != null)
                        {
                            for (int index = 0; index < overridesArray.Length; index++)
                            {
                                if (realArg.StartsWith(overridesArray[index]))
                                {
                                    // partial stem match - now check if this is a full match or a match with a "-" on the end
                                    if (realArg.Equals(overridesArray[index]))
                                    {
                                        namedoverridesState = SwitchState.SwitchEnabled;
                                        namedswitchesState = SwitchState.SwitchDisabled;
                                    }
                                    else if (realArg[overridesArray[index].Length] == '-')
                                    {
                                        namedoverridesState = SwitchState.SwitchDisabled;
                                        // Unsetting an override has no impact upon the named switches
                                    }
                                    // Else we have a stem match - do nothing
                                }
                            }
                        }

                        if (namedswitchesState != SwitchState.SwitchNotFound &&
                            namedoverridesState != SwitchState.SwitchNotFound &&
                            precedence == OrderOfPrecedence.FirstWins)
                        {
                            // we found a switch that impacts the desired state and FirstWins is set
                            break;
                        }
                    }
                }

                if (namedswitchesState == SwitchState.SwitchNotFound)
                {
                    if (namedoverridesState == SwitchState.SwitchEnabled)
                    {
                        namedswitchesState = SwitchState.SwitchDisabled;
                    }
                    else
                    {
                        namedswitchesState = defaultState;
                    }
                }
            }

            return namedswitchesState;
        }

        /// <summary>
        /// Get the value of a compiler command-line options
        /// </summary>
        /// <param name="optionNames">Array of command line options to search for a value</param>
        /// <param name="precedence">The precedence ruls for this set of options</param>
        /// <param name="optionValue">string to recieve the value of the command line option</param>
        /// <returns>true if one of the options is found, false if none are found</returns>
        public bool GetOptionValue(string[] optionNames, OrderOfPrecedence precedence, ref string optionValue)
        {
            bool optionFound=false;

            if (optionNames != null && optionNames.Length > 0)
            {
                // array of strings for the options name without the preceding switchPrefix to make comparison easier
                string[] optionsArray = new string[optionNames.Length]; ;

                for (int index = 0; index < optionNames.Length; index++)
                {
                    // if present remove the slash or minus
                    optionsArray[index] = optionNames[index].TrimStart(switchPrefix);
                }

                foreach (string arg in ArgumentSplitter.CommandLineToArgvW(this.Raw))
                {
                    if (IsCommandLineOption(arg))
                    {
                        string realArg = arg.TrimStart(switchPrefix);

                        // Check if this matches one of the names switches
                        for (int index = 0; index < optionsArray.Length; index++)
                        {
                            if (realArg.StartsWith(optionsArray[index]))
                            {
                                optionFound = true;
                                optionValue = realArg.Substring(optionsArray[index].Length);
                            }
                        }

                        if (optionFound == true &&
                            precedence == OrderOfPrecedence.FirstWins)
                        {
                            // we found a switch that impacts the desired state and FirstWins is set
                            break;
                        }
                    }
                }
            }

            return optionFound;
        }
    }
}
