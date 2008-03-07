/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using JetBrains.Build.InstallationData;
using JetBrains.Omea.Base.Install;

/// Installs supplementary tools.

[assembly : InstallFile("DeleteIndexCmd", TargetRootXml.InstallDir, "", SourceRootXml.ProductHomeDir, "Lib/Distribution", "DeleteIndex.bat", "2818f926-dcc2-4107-b5aa-d8407b688705")]
[assembly : InstallFile("DeleteTextIndexCmd", TargetRootXml.InstallDir, "", SourceRootXml.ProductHomeDir, "Lib/Distribution", "DeleteTextIndex.bat", "fa5e9f33-fac9-49bd-b9cb-659a989573b8")]
[assembly : InstallFile("OmeaConnectorHtmlInstall", TargetRootXml.InstallDir, "", SourceRootXml.ProductHomeDir, "Lib/Distribution", "InstallOmeaConnector.html", "f1067d18-3b28-4095-81e5-ab3614ee9608")]

/// Installs additional license agreements.

[assembly : InstallFile("LicenseSharpZipLib", TargetRootXml.InstallDir, "", SourceRootXml.ProductHomeDir, "Lib/Distribution", "ICSharpCode.SharpZipLib.License.txt", "cfe9c6bd-ea6b-4f8a-ba20-de0f2ca26f89")]
[assembly : InstallFile("LicenseThirdParty", TargetRootXml.InstallDir, "", SourceRootXml.ProductHomeDir, "Lib/Distribution", "Third-Party Software.txt", "b7a0a349-f98f-4a25-a24a-2d8d70c9ad02")]