// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma unmanaged

#include "mailuser.h"
#include "RCPtrDef.h"

template RCPtr<MailUser>;

MailUser::MailUser( IMailUser* lpMailUser ) : MAPIProp( lpMailUser )
{
    _lpMailUser = lpMailUser;
}

MailUser::~MailUser()
{
    _lpMailUser = NULL;
}
