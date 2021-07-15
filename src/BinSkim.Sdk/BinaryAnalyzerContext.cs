// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.CodeAnalysis.BinaryParsers;
using Microsoft.CodeAnalysis.Sarif;

namespace Microsoft.CodeAnalysis.IL.Sdk
{
    public class BinaryAnalyzerContext : IAnalysisContext
    {
        private Uri uri;
        private IBinary iBinary;

        public IBinary Binary
        {
            get
            {
                this.iBinary = this.iBinary
                    ?? BinaryTargetManager.GetBinaryFromFile(
                        this.uri,
                        this.SymbolPath,
                        this.LocalSymbolDirectories,
                        this.TracePdbLoads);

                return this.iBinary;
            }
            set => this.iBinary = value;
        }

        public Exception TargetLoadException
        {
            get => this.Binary != null ? this.Binary.LoadException : null;
            set => throw new InvalidOperationException();
        }

        public bool IsValidAnalysisTarget
        {
            get => this.Binary != null && this.Binary.Valid;
            set => throw new InvalidOperationException();
        }

        public string LocalSymbolDirectories { get; set; }

        public Uri TargetUri
        {
            get => this.uri;
            set
            {
                if (this.uri != null)
                {
                    throw new InvalidOperationException(SdkResources.IllegalContextReuse);
                }
                this.uri = value;
            }
        }

        public bool TracePdbLoads { get; set; }

        public string SymbolPath { get; set; }

        public IAnalysisLogger Logger { get; set; }

        public ReportingDescriptor Rule { get; set; }

        public PropertiesDictionary Policy { get; set; }

        public HashData Hashes { get; set; }

        public string MimeType
        {
            get => Microsoft.CodeAnalysis.Sarif.Writers.MimeType.Binary;
            set => throw new InvalidOperationException();
        }

        public RuntimeConditions RuntimeErrors { get; set; }
        public bool AnalysisComplete { get; set; }
        public DefaultTraces Traces { get; set; }

        public ICompilerDataLogger CompilerDataLogger { get; set; }

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed && disposing)
            {
                if (this.iBinary != null)
                {
                    this.iBinary.Dispose();
                    this.iBinary = null;
                }
                this.disposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            this.Dispose(true);
        }
    }
}
