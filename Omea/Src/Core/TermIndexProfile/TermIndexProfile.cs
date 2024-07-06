// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only


using   System;
using   System.IO;
using   System.Diagnostics;
using   JetBrains.Omea.TextIndex;
using   JetBrains.Omea.Containers;

public class TermIndexProfile
{
	public TermIndexProfile(){}

    public  static  void    Main( string[] Arguments )
    {
        TermIndexAccessor   TermIndex = new TermIndexAccessor( Arguments[ 0 ] );
        StreamWriter        writer = new StreamWriter( "lexdump.dat", false, System.Text.Encoding.Default );
        int                 TotalDocEntries = 0, TotalInstances = 0;
        Console.WriteLine( "loading..." );
        TermIndex.Load();
        Console.WriteLine( "dumping..." );
        foreach( KeyPair pair in TermIndex.Keys )
        {
            TermIndexRecord record = TermIndex.GetRecordByHandle( pair._offset );
            int     InstancesCount = 0;
            TotalDocEntries += record.DocsNumber;
            for( int j = 0; j < record.DocsNumber; j++ )
            {
                InstancesCount += record.GetEntryAt( j ).Count;
                TotalInstances += record.GetEntryAt( j ).Count;
            }
/*
            if( Arguments.Length == 1 || Arguments[ 1 ] == "bydoc" )
                writer.WriteLine( "{0,6}  {1,8}  {2}", record.DocsNumber, InstancesCount, record.Term );
            else
                writer.WriteLine( "{1,8}  {0,6}  {2}", record.DocsNumber, InstancesCount, record.Term );
*/
        }
        writer.WriteLine( "--- 1 Terms number: " + TermIndex.TermsNumber );
        writer.WriteLine( "--- 2 Words number: " + TotalInstances );
        writer.WriteLine( "--- 3 Entries number: " + TotalDocEntries );
        TermIndex.Close();
        writer.Close();
        Console.WriteLine( "sorting..." );

        Process process = new Process();
        process.StartInfo.FileName = "sort.exe";
        process.StartInfo.Arguments = " /R /L C lexdump.dat /O lexdump.srt";
        process.StartInfo.WorkingDirectory = ".";
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.UseShellExecute = false;
        process.Start();
        process.WaitForExit();
        Console.WriteLine( "done..." );
    }
}
