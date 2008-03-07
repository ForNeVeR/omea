/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using JetBrains.Build.InstallationData;
using JetBrains.Omea.Base.Install;

/// This causes the Data files to be installed on the target system.

[assembly : InstallFile("TextIndexData", TargetRootXml.InstallDir, "Data", SourceRootXml.ProductHomeDir, "Lib/Data", "*", "{636b9847-09bb-476d-b146-75abff7b87ed}")]