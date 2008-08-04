/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

using JetBrains.Build.InstallationData;
using JetBrains.Omea.Base;
using JetBrains.Omea.Base.Install;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.RTF;

/// Take along the cmdline tools.
[assembly:InstallFile("WordDocWvWare", TargetRootXml.InstallDir, "", SourceRootXml.ProductHomeDir, "Lib/Distribution", "wvWare.exe", "3e65c012-e321-4976-b7ed-1f6295d97d09")]
[assembly:InstallFile("WordDocWvWareZlib", TargetRootXml.InstallDir, "", SourceRootXml.ProductHomeDir, "Lib/Distribution", "zlib1.dll", "3a471f12-57df-453b-9ff2-e3a445dbff5d")]
[assembly:InstallFile("WordDocWvWareText", TargetRootXml.InstallDir, "", SourceRootXml.ProductHomeDir, "Lib/Distribution", "wvText.xml", "c5b5cc89-621c-438e-a21d-df1c2f5900c4")]
[assembly:InstallFile("WordDocWvWareHtml", TargetRootXml.InstallDir, "", SourceRootXml.ProductHomeDir, "Lib/Distribution", "wvHtml.xml", "195ea38a-bf9f-40a7-93f9-e2d397254cdc")]

namespace JetBrains.Omea.WordDocPlugin
{
	[PluginDescriptionAttribute("Word Documents", "JetBrains Inc.", "Microsoft Word files viewer and plain text extractor, for search capabilities", PluginDescriptionFormat.PlainText, "Icons/WordDocPluginIcon.png")]
	public class WordDocPlugin: IPlugin, IResourceDisplayer, IResourceTextProvider
	{
		private readonly Tracer _tracer = new Tracer( "WordDocPlugin" );
		private static string cMSWordFile = "MSWordFile";
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

			Core.PluginLoader.RegisterResourceTextProvider( cMSWordFile, this );
			Core.PluginLoader.RegisterResourceDisplayer( cMSWordFile, this );

			// the temp dir will be cleaned up automatically
			_tempDir = Core.FileResourceManager.GetUniqueTempDirectory();
		}

		public void Startup()
		{
		}

		public void Shutdown()
		{
			Core.FileResourceManager.DeregisterFileResourceType( cMSWordFile );
		}

		#endregion

		public static string TempDir
		{
			get { return _tempDir; }
		}

		#region IResourceDisplayer members

		IDisplayPane IResourceDisplayer.CreateDisplayPane( string resourceType )
		{
			return new WordDisplayPane();
		}

		#endregion

		bool IResourceTextProvider.ProcessResourceText( IResource res, IResourceTextConsumer consumer )
		{
			//  Forbid extraction of the word document body in the context
			//  of "search context extraction" because it is too slow and
			//  can cause indefinite memory consumption.
			if( consumer.Purpose == TextRequestPurpose.ContextExtraction )
				return false;

			string fileName = Core.FileResourceManager.GetSourceFile( res );
			if ( !string.IsNullOrEmpty(fileName) )
			{
				bool isRtf;
				try
				{
					isRtf = IsRtfFile( fileName );
				}
				catch( IOException )  // see #4335
				{
					return false;
				}
				catch( UnauthorizedAccessException )  // see #6126
				{
					return false;
				}
                
				try
				{
					if ( isRtf )
					{
						Trace.WriteLine( "Indexing rich text file " + fileName );
						StreamReader reader = null;
						try
						{
							FileStream fileStream;
							try
							{
								fileStream = new FileStream( fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite );
							}
							catch( IOException ex )  // OM-7843
							{
								Trace.WriteLine( "Error opening RTF file for indexing: " + ex.Message );
								return false;
							}
							reader = new StreamReader( fileStream, Encoding.Default );
							RTFParser parser = new RTFParser();
							string content;
							try
							{
								content = parser.Parse( reader );
							}
							catch( Exception ex )
							{
								throw new Exception( "Error parsing RTF file " + fileName, ex );
							}
							consumer.AddDocumentFragment( res.Id, content );
						}
						finally
						{
							if( reader != null )
								reader.Close();                            
						}
					}
					else
					{
						Trace.WriteLine( "Indexing Word document file " + fileName );
						string text = RunWvWare( fileName, "wvText.xml", true );
						consumer.AddDocumentFragment( res.Id, text );
					}

					Core.FileResourceManager.CleanupSourceFile( res, fileName );

					//  If we managed to successfully retrieve the text from the
					//  doc file (e.g. using newer version of the convertor) - 
					//  clean the [possibly] assigned error sign.
					new ResourceProxy( res ).DeleteProp( Core.Props.LastError );
				}
				catch ( Exception e )
				{
					//  If convertion process failed (whatever the reason is) -
					//  remember the error text and show it in the DisplayPane
					//  instead of the actual content.
					new ResourceProxy( res ).SetProp( Core.Props.LastError, e.Message );
					Trace.WriteLine( "LastError for id=" + res.Id + " - " + e.Message );
				}
			}
			return true;
		}

		internal static bool IsRtfFile( string fileName )
		{
			FileStream f;
			try
			{
				f = File.Open( fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite );
				byte[] signature = new byte [5];
				f.Read( signature, 0, 5 );
				f.Close();
				return Encoding.ASCII.GetString( signature ) == @"{\rtf";
			}
			catch( ArgumentException )
			{
			}
			return false;
		}

		internal static string RunWvWare( string fileName, string configFileName, bool background )
		{
			Process process = CreateWvWareProcess( fileName, configFileName, background );

			try
			{
				if(!process.Start())
					throw new Exception();
			}
			catch
			{
				return "";	
			}
			process.Start();
			string result = Utils.StreamReaderReadToEnd( process.StandardOutput );
			string stderr = Utils.StreamReaderReadToEnd( process.StandardError );
			process.WaitForExit();
			if ( process.ExitCode == -2 )  // password-protected document, password not supplied
			{
				return "";
			}
			if ( process.ExitCode == -5 )  // not a Word document
			{
				return "";
			}
			if ( process.ExitCode == -7 )  // No space left or permission denied
			{
				return "";
			}
			if ( process.ExitCode != 0 )
			{
				if ( process.ExitCode == -3 )
				{
					throw new Exception( "wvWare.exe crashed when converting " + fileName + ": " + stderr );
				}
				else
				{
					throw new Exception( "wvWare conversion of " + fileName + " failed: " + stderr );
				}
			}
			return result;
		}

		internal static Process CreateWvWareProcess( string fileName, string configFileName, bool background )
		{
			if ( !File.Exists( fileName ) )
			{
				throw new Exception( "Could not find the file to convert: " + fileName );
			}
            
			string configPath = Path.Combine( Application.StartupPath, configFileName );
			if ( !File.Exists( configPath ) )
			{
				throw new Exception( "Failed to find wvWare configuration file " + configPath );
			}
            
			Process process = new Process();
			process.StartInfo.FileName = "wvWare.exe";
			process.StartInfo.Arguments = "-x " + configFileName + " -c utf-8 -d " + Utils.QuotedString( TempDir ) + " ";
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
			string exts = Core.SettingStore.ReadString( "FilePlugin", "MSWordExts" );
			exts = ( exts.Length == 0 ) ? ".doc,.rtf" : exts + ",.doc,.rtf";
			string[] extsArray = exts.Split( ',' );
			for( int i = 0; i < extsArray.Length; ++i )
			{
				extsArray[ i ] = extsArray[ i ].Trim();
			}
			Core.FileResourceManager.RegisterFileResourceType(
				cMSWordFile, "Word Document", "Name", 0, this, extsArray );
		}

		#endregion

	}
}
