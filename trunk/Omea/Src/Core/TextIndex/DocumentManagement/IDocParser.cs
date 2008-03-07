/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>


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
