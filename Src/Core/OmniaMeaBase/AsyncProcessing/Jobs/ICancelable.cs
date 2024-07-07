// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

namespace JetBrains.Omea.AsyncProcessing
{
    public interface ICancelable
    {
        void OnCancel();
    }
}
