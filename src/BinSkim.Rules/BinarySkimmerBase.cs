﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Resources;
using Microsoft.CodeAnalysis.IL.Sdk;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CodeAnalysis.Sarif.Driver;

namespace Microsoft.CodeAnalysis.IL.Rules
{
    public abstract class BinarySkimmerBase : SkimmerBase<BinaryAnalyzerContext>, IBinarySkimmer
    {
        private static Uri s_helpUri = new Uri("https://github.com/microsoft/binskim");

        public override Uri HelpUri { get { return s_helpUri; } }

        // TODO: author good help text
        // https://github.com/Microsoft/binskim/issues/192
        public override Message Help {  get { return this.FullDescription; } }

        public BinarySkimmerBase()
        {
            // Set Binscope friendly name for backwards compatibility, if one exists.
            string altId = BinScopeCompatibility.GetBinScopeRuleReadableName(this.Id);
            if (!String.IsNullOrEmpty(altId))
            {
                this.SetProperty<string>(BinScopeCompatibility.EquivalentBinScopeRulePropertyName, altId);
            }
        }

        // BA2012.DoNotModifyStackProtectionCookie is the only rule
        // that potentially fires warnings (and its default is error).
        public override ResultLevel DefaultLevel => ResultLevel.Error;

        protected override ResourceManager ResourceManager
        {
            get
            {
                return RuleResources.ResourceManager;
            }
        }
    }
}
