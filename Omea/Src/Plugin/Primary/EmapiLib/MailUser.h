/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

#pragma once

#include "mapiprop.h"

class MailUser : public MAPIProp
{
private:
    IMailUser* _lpMailUser;
public:
    MailUser( IMailUser* lpMailUser );
    virtual ~MailUser();
};
