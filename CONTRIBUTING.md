<!--
SPDX-FileCopyrightText: 2024 Friedrich von Never <friedrich@fornever.me>

SPDX-License-Identifier: MIT
-->

Contributor Guide
=================

Prerequisites
-------------
Currently, we only support building Omea on Windows.

You'll need [Visual Studio 2022][visual-studio] (a Community edition should suffice) with the .NET development workload.

Build
-----
Enter the **Developer Command Prompt for VS 2022** and run the following shell commands (starting from the repository root):

```console
$ cd Src
$ dotnet tool restore
$ msbuild /t:Build /property:Platform="Mixed Platforms" Omea.sln
```

This will build Omea and generate the artifacts in the `Bin` directory.

License Automation
------------------
If the CI asks you to update the file licenses, follow one of these:
1. Update the headers manually (look at the existing files), something like this:
   ```csharp
   // SPDX-FileCopyrightText: %year% %your name% <%your contact info, e.g. email%>
   //
   // SPDX-License-Identifier: GPL-2.0-only
   ```
   (accommodate to the file's comment style if required).
2. Alternately, use [REUSE][reuse] tool:
   ```console
   $ reuse annotate --license MIT --copyright '%your name% <%your contact info, e.g. email%>' %file names to annotate%
   ```

(Feel free to attribute the changes to "Omea authors <https://github.com/ForNeVeR/omea>" instead of your name in a multi-author file, or if you don't want your name to be mentioned in the project's source: this doesn't mean you'll lose the copyright.)

[reuse]: https://reuse.software/
[visual-studio]: https://visualstudio.microsoft.com/vs/
