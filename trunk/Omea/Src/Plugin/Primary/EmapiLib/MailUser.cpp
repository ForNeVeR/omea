/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

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
