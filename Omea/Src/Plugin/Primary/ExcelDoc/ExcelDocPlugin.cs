// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

using JetBrains.Build.InstallationData;
using JetBrains.Omea.Base;
using JetBrains.Omea.Base.Install;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.FileTypes;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

/// Take along the cmdline tool.
[assembly:InstallFile("ExcelDocXlHtml", TargetRootXml.InstallDir, "", SourceRootXml.ProductHomeDir, "Lib/Distribution", "xlhtml-w32.exe", "e0b9195f-496d-4612-9d22-8b2338bfdb81")]

namespace JetBrains.Omea.ExcelDocPlugin
{
	[PluginDescriptionAttribute("Excel Documents", "JetBrains Inc.", "Microsoft Excel files viewer and plain text extractor, for search capabilities", PluginDescriptionFormat.PlainText, "Icons/ExcelPluginIcon.png")]
	public class ExcelDocPlugin: IPlugin, IResourceDisplayer, IResourceTextProvider
	{
		private readonly Tracer _tracer = new Tracer( "ExcelDocPlugin" );
		private const string _converterName = "xlhtml-w32.exe";
		private const string _MSExcelFile = "MSExcelFile";
		private static string _tempDir;

		#region IPlugin Members

		public void Register()
		{
			try
			{
				RegisterTypes();
			}
			catch( Exception exception )
			{
				_tracer.TraceException( exception );
			}

			Core.PluginLoader.RegisterResourceTextProvider( _MSExcelFile, this );
			Core.PluginLoader.RegisterResourceDisplayer( _MSExcelFile, this );

			// the temp dir will be cleaned up automatically
			_tempDir = Core.FileResourceManager.GetUniqueTempDirectory();
		}

		public void Startup()
		{
		}

		public void Shutdown()
		{
			Core.FileResourceManager.DeregisterFileResourceType( _MSExcelFile );
		}

		#endregion

		public static string TempDir
		{
			get { return _tempDir; }
		}

		#region IResourceDisplayer members

		IDisplayPane IResourceDisplayer.CreateDisplayPane( string resourceType )
		{
			return new ExcelDisplayPane();
		}

		#endregion

		bool IResourceTextProvider.ProcessResourceText( IResource res, IResourceTextConsumer consumer )
		{
			//  Forbid extraction of the excel document body in the context
			//  of "search context extraction" because it is too slow and
			//  can cause indefinite memory consumption.
			if( consumer.Purpose == TextRequestPurpose.ContextExtraction )
				return false;

			try
			{
				string fileName = Core.FileResourceManager.GetSourceFile( res );
				if ( fileName != null )
				{
					if( ! File.Exists( fileName ) )
					{
						return false;
					}
					Trace.WriteLine( "Indexing Excel document file " + fileName );
					RunConverter( fileName, consumer, res  );
					Core.FileResourceManager.CleanupSourceFile( res , fileName );

					//  If we managed to successfully retrieve the text from the
					//  doc file (e.g. using newer version of the convertor) -
					//  clean the [possibly] assigned error sign.
					new ResourceProxy( res ).DeleteProp( Core.Props.LastError );
				}
			}
			catch ( Exception e )
			{
				//  If convertion process failed (whatever the reason is) -
				//  remember the error text and show it in the DisplayPane
				//  instead of the actual content.
				new ResourceProxy( res ).SetProp( Core.Props.LastError, e.Message );
				Trace.WriteLine( "LastError for id=" + res.Id + " - " + e.Message );
			}
			return true;
		}

		internal static void RunConverter( string fileName, IResourceTextConsumer consumer, IResource res )
		{
			Process process = CreateConverterProcess( fileName, true );

			try
			{
				if(!process.Start())
					throw new Exception();
			}
			catch
			{
				return;
			}
			try
			{
				Encoding utf8 = new UTF8Encoding( false, false );
				StreamReader outputReader = new StreamReader( process.StandardOutput.BaseStream, utf8 );

				string content = Utils.StreamReaderReadToEnd( outputReader );

				consumer.RestartOffsetCounting();	// Just in case ;)
				HtmlIndexer.IndexHtml( res, content, consumer, DocumentSection.BodySection );
			}
			finally
			{
				string stderr = Utils.StreamReaderReadToEnd( process.StandardError );
				process.WaitForExit();
				if ( process.ExitCode != 0 )
				{
					throw new Exception( _converterName + " (Excel-to-HTML) has performed an invalid operation while converting \"" + fileName + "\". " + stderr );
				}
			}
			return;
		}

		internal static Process CreateConverterProcess( string fileName, bool background )
		{
			if ( !File.Exists( fileName ) )
			{
				throw new Exception( "Could not find the file to convert: " + fileName );
			}

			string tmpPath = "-tmp" + Utils.QuotedString( FileResourceManager.GetTrashDirectory() );

			Process process = new Process();
			process.StartInfo.FileName = _converterName;
			process.StartInfo.Arguments = tmpPath + " -nd -te ";
			if( background )
			{
				process.StartInfo.Arguments += "-l ";
			}
			process.StartInfo.Arguments += Utils.QuotedString( fileName );
			process.StartInfo.WorkingDirectory = Application.StartupPath;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			return process;
		}

		#region implementation details

		private void RegisterTypes()
		{
			string exts = Core.SettingStore.ReadString( "FilePlugin", "MSExcelExts" );
			exts = ( exts.Length == 0 ) ? ".xls" : exts + ",.xls";
			string[] extsArray = exts.Split( ',' );
			for( int i = 0; i < extsArray.Length; ++i )
			{
				extsArray[ i ] = extsArray[ i ].Trim();
			}
			Core.FileResourceManager.RegisterFileResourceType(
				_MSExcelFile, "Excel Document", "Name", 0, this, extsArray );
		}

		#endregion

	}
}
