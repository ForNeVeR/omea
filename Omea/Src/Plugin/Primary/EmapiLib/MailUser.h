// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

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
