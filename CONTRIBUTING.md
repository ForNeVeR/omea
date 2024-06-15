Building Omea
=============

Environment Setup
-----------------
Omea's sources were released in 2008, and thus use very old technologies and are pretty tricky to build.

This document will guide you through the supported workflow that's verified to work. It doesn't mean that it's impossible to build Omea using more modern tools or different approach (e.g. a different VM provider); it's just what's been tested at the current moment.

### Prerequisites
This guide has been tested on Windows 11 with Hyper-V enabled.

You'll need to download the following tools:
- [Windows XP Mode][downloads.windows-xp-mode],
- [Visual Studio 2008 Pro][downloads.visual-studio-2008] (thanks to [the answer of Dr. Koutheir Attouchi on Stack Overflow][downloads.visual-studio-2008.source] for the link).

Also, you'll need to build [Folder to ISO][tools.folder-to-iso] from sources.

### Installation
1. Follow [this guide from Stack Overflow][guide.windows-xp] to get a Windows XP installation in Hyper-V.
2. Start the VM, go through the initial installation steps (don't change the locale, leave everything with defaults; feel free to set up a correct time zone though).
3. After the OS installation, the VM will restart. On the initial startup, deny any attempts to load drivers from the internet; it won't help anyway.
4. Shut down the OS. Make a VM checkpoint in case you'll need to reset (it's pretty fragile).
5. Start the OS. Set up the screen resolution to 1024×768 (otherwise, the VS installer may break).
6. Insert Visual Studio installation image, install it with the default workload.
7. Shut down the OS. Make another checkpoint just in case.
8. Update the `Path` variable:
   1. Press **Start**.
   2. Choose **My Computer**.
   3. Right click any free space in the **My Computer** window, choose **Properties** to get to the **System Properties** window.
   4. Go to the **Advanced** tab and click the **Environment Variables** button.
   5. Choose the `Path` variable in the **System variables** group, press **Edit**.
   6. Add `;C:\Program Files\Microsoft SDKs\Windows\v6.0A\bin` to the end of the variable text.

      (This directory contains `xsd.exe` tool that's used during the build process.)

Obtaining the Sources
---------------------
It is quite tricky to deliver any files to the virtual machine, and so far I've decided to rely on an ISO file.
1. Start **Folder to ISO**, choose the Omea source folder and an output file location, press **Create ISO**.
2. Insert the resulting ISO into the VM, copy sources somewhere (e.g. `C:\Sources\Omea`).

Building the Solution
---------------------
1. Open `C:\Sources\Omea\Src\Omea.sln` using Visual Studio (double-click in Explorer).
2. Build the solution (`F6` key by default).
3. Grab the binaries in the `C:\Sources\Omea\Bin` dir.

   In particular, `Omea.exe` is expected to start and work in this environment.

I wasn't successful in building any other solutions from the source dump; e.g. `Omea\Setup\Installer.sln` is impossible to build.

[downloads.visual-studio-2008]: http://download.microsoft.com/download/8/1/d/81d3f35e-fa03-485b-953b-ff952e402520/VS2008ProEdition90dayTrialENUX1435622.iso
[downloads.visual-studio-2008.source]: https://stackoverflow.com/a/26742363/2684760
[downloads.windows-xp-mode]: https://archive.org/details/windows-xp-mode_20200907
[guide.windows-xp]: https://superuser.com/a/1230653/286768
[tools.folder-to-iso]: https://github.com/bastisk/Folder-To-Iso
