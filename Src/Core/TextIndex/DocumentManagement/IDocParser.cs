// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only


using   JetBrains.Omea.TextIndex;

/******************************************************************************
    Interface DocParser determines the primary logic for working with all types
    of documents.
******************************************************************************/

public interface IDocParser
{
    void    Init( string str );
    void    Next( string str );
    Word    getNextWord();
}
