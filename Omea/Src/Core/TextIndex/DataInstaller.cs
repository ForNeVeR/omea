// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Build.InstallationData;
using JetBrains.Omea.Base.Install;

/// This causes the Data files to be installed on the target system.

[assembly : InstallFile("TextIndexData", TargetRootXml.InstallDir, "Data", SourceRootXml.ProductHomeDir, "Lib/Data", "*", "{636b9847-09bb-476d-b146-75abff7b87ed}")]
