// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.MemoryWatchPlugin
{
    public class MemoryWatchPlugin : IPlugin
    {
        public void Register()
        {
        }

        public void Startup()
        {
        }

        public void Shutdown()
        {
        }
    }

    public class ShowMemoryWatchAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            MemoryWatch dlg = new MemoryWatch();
            dlg.Show();
        }
    }
}
