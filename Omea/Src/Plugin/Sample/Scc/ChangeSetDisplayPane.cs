/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.SamplePlugins.SccPlugin
{
	/// <summary>
	/// The display pane for displaying ChangeSet resources.
	/// </summary>
	public class ChangeSetDisplayPane: System.Windows.Forms.UserControl, IDisplayPane
	{
        private System.Windows.Forms.RichTextBox _edtDescription;
        private System.Windows.Forms.Splitter splitter1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ListBox _changedFilesList;
        private System.Windows.Forms.Splitter splitter2;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.LinkLabel _lnkFileName;
        private System.Windows.Forms.RichTextBox _edtDiff;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
	    private IResource _changeSet;
	    private IResourceList _changeSetList;
	    private IResourceList _selectedChangeList;
        private Dictionary<string, string> _linkTextMap;
	    private DateTime _colorizeStartTime;
	    
		public ChangeSetDisplayPane()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitializeComponent call

		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Component Designer generated code
		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this._edtDescription = new System.Windows.Forms.RichTextBox();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this._edtDiff = new System.Windows.Forms.RichTextBox();
            this._lnkFileName = new System.Windows.Forms.LinkLabel();
            this.splitter2 = new System.Windows.Forms.Splitter();
            this._changedFilesList = new System.Windows.Forms.ListBox();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // _edtDescription
            // 
            this._edtDescription.BackColor = System.Drawing.SystemColors.Control;
            this._edtDescription.Dock = System.Windows.Forms.DockStyle.Top;
            this._edtDescription.Location = new System.Drawing.Point(0, 0);
            this._edtDescription.Name = "_edtDescription";
            this._edtDescription.ReadOnly = true;
            this._edtDescription.Size = new System.Drawing.Size(676, 48);
            this._edtDescription.TabIndex = 0;
            this._edtDescription.Text = "";
            this._edtDescription.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this._edtDescription_LinkClicked);
            // 
            // splitter1
            // 
            this.splitter1.Dock = System.Windows.Forms.DockStyle.Top;
            this.splitter1.Location = new System.Drawing.Point(0, 48);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(676, 3);
            this.splitter1.TabIndex = 1;
            this.splitter1.TabStop = false;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.panel2);
            this.panel1.Controls.Add(this.splitter2);
            this.panel1.Controls.Add(this._changedFilesList);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 51);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(676, 249);
            this.panel1.TabIndex = 2;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this._edtDiff);
            this.panel2.Controls.Add(this._lnkFileName);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(183, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(493, 249);
            this.panel2.TabIndex = 2;
            // 
            // _edtDiff
            // 
            this._edtDiff.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._edtDiff.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._edtDiff.Location = new System.Drawing.Point(4, 24);
            this._edtDiff.Multiline = true;
            this._edtDiff.Name = "_edtDiff";
            this._edtDiff.ReadOnly = true;
            this._edtDiff.Size = new System.Drawing.Size(488, 220);
            this._edtDiff.TabIndex = 1;
            this._edtDiff.Text = "";
            this._edtDiff.WordWrap = false;
            // 
            // _lnkFileName
            // 
            this._lnkFileName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._lnkFileName.Location = new System.Drawing.Point(4, 4);
            this._lnkFileName.Name = "_lnkFileName";
            this._lnkFileName.Size = new System.Drawing.Size(484, 16);
            this._lnkFileName.TabIndex = 0;
            this._lnkFileName.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this._lnkFileName_LinkClicked);
            // 
            // splitter2
            // 
            this.splitter2.Location = new System.Drawing.Point(180, 0);
            this.splitter2.Name = "splitter2";
            this.splitter2.Size = new System.Drawing.Size(3, 249);
            this.splitter2.TabIndex = 1;
            this.splitter2.TabStop = false;
            // 
            // _changedFilesList
            // 
            this._changedFilesList.Dock = System.Windows.Forms.DockStyle.Left;
            this._changedFilesList.IntegralHeight = false;
            this._changedFilesList.Location = new System.Drawing.Point(0, 0);
            this._changedFilesList.Name = "_changedFilesList";
            this._changedFilesList.Size = new System.Drawing.Size(180, 249);
            this._changedFilesList.TabIndex = 0;
            this._changedFilesList.SelectedIndexChanged += new System.EventHandler(this._changedFilesList_SelectedIndexChanged);
            // 
            // ChangeSetDisplayPane
            // 
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.splitter1);
            this.Controls.Add(this._edtDescription);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.Name = "ChangeSetDisplayPane";
            this.Size = new System.Drawing.Size(676, 300);
            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }
		#endregion

	    public Control GetControl()
	    {
	        return this;
	    }

	    public void DisplayResource( IResource resource )
	    {
	        _changeSet = resource;
	        _changeSetList = resource.ToResourceListLive();
	        _changeSetList.ResourceChanged += HandleChangesetChanged;
            _edtDescription.Text = resource.GetPropText( Core.Props.LongBody );
            HighlightDescriptionLinks();

            IResource repository = _changeSet.GetProp( Props.ChangeSetRepository );
            RepositoryType repType = SccPlugin.GetRepositoryType( repository );
            repType.OnChangesetSelected( repository, _changeSet );
	        
            _changedFilesList.BeginUpdate();
            try
            {
                _changedFilesList.Items.Clear();
                foreach( FileChange fileChange in resource.GetLinksOfType( FileChange.ResourceType, 
                    Props.Change ) )
                {
                    if ( Settings.HideUnchangedFiles )
                    {
                        if ( !fileChange.Binary && 
                            fileChange.ChangeType == "edit" &&
                            String.IsNullOrEmpty(fileChange.Diff) )
                        {
                            continue;
                        }
                    }

                    _changedFilesList.Items.Add( fileChange );
                }
            }
            finally
            {
                _changedFilesList.EndUpdate();
            }

            if ( _changedFilesList.Items.Count == 0 )
            {
                ClearSelectedChange();
                _lnkFileName.Links.Clear();
            }
            else
            {
                FileChange fileChange = (FileChange) _changedFilesList.Items [0];
                _changedFilesList.SelectedItem = fileChange;
                if ( repType.BuildLinkToFile( repository, fileChange ) == null )
                {
                    _lnkFileName.Links.Clear();
                }
            }
        }

	    private void HighlightDescriptionLinks()
	    {
            _linkTextMap = new Dictionary<string, string>();
            foreach( LinkRegex res in Core.ResourceStore.GetAllResources( LinkRegex.ResourceType ) )
            {
                string regexMatch = res.RegexMatch;
                string regexReplace = res.RegexReplace;
                if ( String.IsNullOrEmpty(regexMatch) || String.IsNullOrEmpty(regexReplace))
                {
                    continue;
                }
                Regex rxMatch = new Regex( regexMatch );
                foreach( Match m in rxMatch.Matches( _edtDescription.Text ) )
                {
                    _edtDescription.Select( m.Index, m.Length );
                    CHARFORMAT2 fmt = new CHARFORMAT2();
                    fmt.cbSize = Marshal.SizeOf( fmt );
                    fmt.dwMask = CFM.EFFECTS;
                    fmt.dwEffects = (uint) CFM.LINK;

                    SendMessage( _edtDescription.Handle, EditMessage.SETCHARFORMAT, SCF.SELECTION, ref fmt );

                    _linkTextMap [m.Value] = rxMatch.Replace( m.Value, regexReplace );
                }
            }
            _edtDescription.Select( 0, 0 );
	    }

	    public void HighlightWords( WordPtr[] words )
	    {
	    }

	    public void EndDisplayResource( IResource resource )
	    {
	        _changeSetList.ResourceChanged -= new ResourcePropIndexEventHandler( HandleChangesetChanged );
	        _changeSetList.Dispose();
	        SetFileChangeWatch( null );
	    }

	    private void SetFileChangeWatch( IResource fileChange )
	    {
	        if ( _selectedChangeList != null )
	        {
	            _selectedChangeList.ResourceChanged -= new ResourcePropIndexEventHandler( HandleFileChangeChanged );
	            _selectedChangeList.Dispose();
	            _selectedChangeList = null;
	        }
	        if ( fileChange != null )
	        {
	            _selectedChangeList = fileChange.ToResourceListLive();
	            _selectedChangeList.ResourceChanged += new ResourcePropIndexEventHandler( HandleFileChangeChanged );
	        }
	    }

	    public void DisposePane()
	    {
	        Dispose();
	    }

	    public string GetSelectedText( ref TextFormat format )
	    {
	        return null;
	    }

	    public string GetSelectedPlainText()
	    {
	        return null;
	    }

	    public bool CanExecuteCommand( string command )
	    {
	        return false;
	    }

	    public void ExecuteCommand( string command )
	    {
	    }

	    private void _changedFilesList_SelectedIndexChanged( object sender, EventArgs e )
	    {
	        IResource selChange = null;
	        FileChange fileChange = (FileChange) _changedFilesList.SelectedItem;
	        if ( fileChange != null )
	        {
	            selChange = fileChange.Resource;
	        }
	        SetFileChangeWatch( selChange );
	        UpdateSelectedChange();
	    }

	    private void UpdateSelectedChange()
	    {
	        FileChange fileChange = (FileChange) _changedFilesList.SelectedItem;
	        if ( fileChange != null )
	        {
	            IResource repository = _changeSet.GetProp( Props.ChangeSetRepository );
	            RepositoryType repType = SccPlugin.GetRepositoryType( repository );
	            string diffText = repType.OnFileChangeSelected( repository, fileChange );
	            _lnkFileName.Text = repType.BuildFileName( repository, fileChange );
	            if ( _lnkFileName.Links.Count == 1 )
	            {
	                _lnkFileName.Links [0].LinkData = fileChange;
	            }
                
	            _edtDiff.Clear();
	            string changeType = fileChange.ChangeType;
	            if ( changeType == "add" )
	            {
	                _edtDiff.Text = "New file";
	            }
	            else if ( changeType == "delete" )
	            {
	                _edtDiff.Text = "File deleted";
	            }
	            else if ( fileChange.Binary )
	            {
	                _edtDiff.Text = "Binary file";
	            }
	            else
	            {
	                string diff = fileChange.Diff;
	                if ( String.IsNullOrEmpty(diff) )
	                {
	                    _edtDiff.Text = diffText;
	                }
	                else
	                {
	                    _edtDiff.Text = FilterWhitespaceOnlyDiffs( diff );
	                    ColorizeDiff();
	                }
	            }
	        }
	        else
	        {
	            ClearSelectedChange();
	        }
	    }

	    /// <summary>
	    /// Processes the file diff in unified diff format. If a sequence of removed and added lines
	    /// has differences only in whitespace, marks these lines as unchanged.
	    /// </summary>
	    /// <param name="diff">The input string in unified diff format.</param>
	    /// <returns>The filtered string in unified diff format.</returns>
	    private static string FilterWhitespaceOnlyDiffs( string diff )
	    {
	        string[] diffLines = diff.Split( '\n' );
	        ArrayList resultLines = new ArrayList();
	        ArrayList removedLines = new ArrayList();
	        ArrayList addedLines = new ArrayList();
	        int i = 0;
	        while( i < diffLines.Length )
	        {
	            if ( !diffLines [i].StartsWith( "-" ) )
	            {
	                resultLines.Add( diffLines [i++] );
	                continue;
	            }
	            while ( i < diffLines.Length && diffLines [i].StartsWith( "-" ) )
	            {
	                removedLines.Add( diffLines [i++] );
	            }
	            while ( i < diffLines.Length && diffLines [i].StartsWith( "+" ) )
	            {
	                addedLines.Add( diffLines [i++] );
	            }
	            
	            bool whitespaceOnlyDiff = false;
	            if ( removedLines.Count == addedLines.Count )
	            {
	                whitespaceOnlyDiff = true;
	                for( int j=0; j<removedLines.Count; j++ )
	                {
	                    string removedLine = ((string) removedLines [j]).Replace( " ", "" ).Replace( "\t", "" );
	                    string addedLine = ((string) addedLines [j]).Replace( " ", "" ).Replace( "\t", "" );
	                    if ( removedLine.Substring( 1 ) != addedLine.Substring( 1 ) )
	                    {
	                        whitespaceOnlyDiff = false;
	                        break;
	                    }
	                }
	            }
	            if ( !whitespaceOnlyDiff )
	            {
	                resultLines.AddRange( removedLines );
	                resultLines.AddRange( addedLines );
	            }
	            else
	            {
	                foreach( string line in addedLines )
	                {
	                    resultLines.Add( " " + line.Substring( 1 ) );
	                }
	            }
	            removedLines.Clear();
	            addedLines.Clear();
	        }
	        return String.Join( "\n", (string[]) resultLines.ToArray( typeof(string) ) );
	    }

	    private void ColorizeDiff()
	    {
	        _colorizeStartTime = DateTime.Now;
	        ColorizeDiffLines( "+", Color.Green );
	        ColorizeDiffLines( "-", Color.Blue );
	        ColorizeDiffLines( "@", Color.DarkGray );
	    }

	    private void ColorizeDiffLines( string prefix, Color color )
	    {
	        string text = _edtDiff.Text;
	        int startIndex = 0;
	        int lineCount = 0;
	        while( true )
	        {
	            if ( (lineCount % 50 == 0) && (DateTime.Now - _colorizeStartTime).TotalMilliseconds > 1000)
	            {
	                return;
	            }
	            lineCount++;
	            startIndex = text.IndexOf( "\n" + prefix, startIndex );
	            if ( startIndex < 0 )
	            {
	                break;
	            }
	            int lineEnd = text.IndexOf( "\n", startIndex + 2 );
	            if ( lineEnd < 0 )
	            {
	                lineEnd = text.Length;
	            }
	            _edtDiff.Select( startIndex+1, lineEnd-startIndex-1 );
	            _edtDiff.SelectionColor = color;
	            startIndex = lineEnd;
	        }
	    }

        private void HandleChangesetChanged( object sender, ResourcePropIndexEventArgs e )
        {
            // redisplay resource only if interesting changes occur
            if ( !e.ChangeSet.IsPropertyChanged( Props.Change ) && !e.ChangeSet.IsPropertyChanged( Core.PropIds.LongBody ) )
            {
                return;
            }
            if ( !Core.UserInterfaceAP.IsOwnerThread )
            {
                Core.UIManager.QueueUIJob(() => HandleChangesetChanged(sender, e));
                return;
            }
            if ( e.Resource == _changeSet )
            {
                EndDisplayResource( e.Resource );
                if ( !e.Resource.IsDeleting )
                {
                    DisplayResource( e.Resource );
                }
            }
        }

        private void HandleFileChangeChanged( object sender, ResourcePropIndexEventArgs e )
	    {
	        if ( !Core.UserInterfaceAP.IsOwnerThread )
	        {
	            Core.UIManager.QueueUIJob( () => HandleFileChangeChanged(sender, e));
	            return;
	        }
	        FileChange fileChange = (FileChange) _changedFilesList.SelectedItem;
	        if ( fileChange != null && fileChange.Resource == e.Resource )
	        {
	            UpdateSelectedChange();
	        }
	    }

	    private void ClearSelectedChange()
	    {
	        _edtDiff.Text = "";
	        _lnkFileName.Text = "";
	    }

	    private void _lnkFileName_LinkClicked( object sender, LinkLabelLinkClickedEventArgs e )
	    {
	        IResource repository = _changeSet.GetProp( Props.ChangeSetRepository );
	        RepositoryType repType = SccPlugin.GetRepositoryType( repository );
	        string url = repType.BuildLinkToFile( repository, (FileChange) e.Link.LinkData );
	        if ( url != null )
	        {
	            Core.UIManager.OpenInNewBrowserWindow( url );
	        }
	    }

	    private void _edtDescription_LinkClicked( object sender, LinkClickedEventArgs e )
	    {
            if ( _linkTextMap.ContainsKey( e.LinkText ) )
            {
                Core.UIManager.OpenInNewBrowserWindow( _linkTextMap [e.LinkText] );
            }
            else
            {
                Core.UIManager.OpenInNewBrowserWindow( e.LinkText );
            }
	    }

        private enum CFM : uint
	    {
	        BOLD			= 0x00000001,
	        ITALIC			= 0x00000002,
	        UNDERLINE		= 0x00000004,
	        STRIKEOUT		= 0x00000008,
	        PROTECTED		= 0x00000010,
	        LINK			= 0x00000020,		// Exchange hyperlink extension
	        SIZE			= 0x80000000,
	        COLOR			= 0x40000000,
	        FACE			= 0x20000000,
	        OFFSET			= 0x10000000,
	        CHARSET			= 0x08000000,

	        // CHARFORMAT effects
	        //#define CFE_BOLD		0x0001
	        //#define CFE_ITALIC	0x0002
	        //#define CFE_UNDERLINE	0x0004
	        //#define CFE_STRIKEOUT	0x0008
	        //#define CFE_PROTECTED	0x0010
	        //#define CFE_LINK		0x0020
	        //#define CFE_AUTOCOLOR	0x40000000	// NOTE: this corresponds to
	        // CFM_COLOR, which controls it
	        // Masks and effects defined for CHARFORMAT2 -- an (*) indicates
	        // that the data is stored by RichEdit 2.0/3.0, but not displayed

	        SMALLCAPS		= 0x0040,			// (*)	
	        ALLCAPS			= 0x0080,			// Displayed by 3.0	
	        HIDDEN			= 0x0100,			// Hidden by 3.0
	        OUTLINE			= 0x0200,			// (*)	
	        SHADOW			= 0x0400,			// (*)	
	        EMBOSS			= 0x0800,			// (*)	
	        IMPRINT			= 0x1000,			// (*)	
	        DISABLED		= 0x2000,
	        REVISED			= 0x4000,
	        //
	        BACKCOLOR		= 0x04000000,
	        LCID			= 0x02000000,
	        UNDERLINETYPE	= 0x00800000,		// Many displayed by 3.0
	        WEIGHT			= 0x00400000,	
	        SPACING			= 0x00200000,  		// Displayed by 3.0	
	        KERNING			= 0x00100000,  		// (*)	
	        STYLE			= 0x00080000,  		// (*)	
	        ANIMATION		= 0x00040000,  		// (*)	
	        REVAUTHOR		= 0x00008000,

	        CFE_SUBSCRIPT		= 0x00010000,	// Superscript and subscript are
	        CFE_SUPERSCRIPT		= 0x00020000,	//  mutually exclusive			

	        SUBSCRIPT		= CFE_SUBSCRIPT | CFE_SUPERSCRIPT,
	        SUPERSCRIPT		= SUBSCRIPT,
	        //
	        //	CHARFORMAT "ALL" masks
	        EFFECTS  = (BOLD | ITALIC | UNDERLINE | COLOR | STRIKEOUT | /* CFE_*/ PROTECTED | LINK),
	        ALL      = (EFFECTS | SIZE | FACE | OFFSET | CHARSET),
	        EFFECTS2 = (EFFECTS | DISABLED | SMALLCAPS | ALLCAPS | HIDDEN  | OUTLINE | SHADOW | EMBOSS | IMPRINT | DISABLED | REVISED | SUBSCRIPT | SUPERSCRIPT | BACKCOLOR),
	        ALL2     = (ALL | EFFECTS2 | BACKCOLOR | LCID | UNDERLINETYPE | WEIGHT | REVAUTHOR | SPACING | KERNING | STYLE | ANIMATION),
	    }

	    [StructLayout(LayoutKind.Sequential, Pack=8, CharSet=CharSet.Auto)]
	    private struct CHARFORMAT2
	    {
	        private const int LF_FACESIZE = 32;       // Max size of a font name

	        public int cbSize;
	        public CFM dwMask;
	        public UInt32 dwEffects;
	        public UInt32 yHeight;
	        public UInt32 yOffset;
	        public int crTextColor;
	        public Byte   bCharSet;
	        public Byte   bPitchAndFamily;
	        [MarshalAs(UnmanagedType.ByValTStr, SizeConst=LF_FACESIZE)]
	        public String szFaceName;
	        public UInt16 wWeight;
	        public UInt16 sSpacing;
	        public int crBackColor;
	        public UInt32 lcid;
	        public UInt32 dwReserved;
	        public UInt16 sStyle;
	        public UInt16 wKerning;
	        public Byte bUnderlineType;
	        public Byte bAnimation;
	        public Byte bRevAuthor;
	        public Byte bReserved1;
	    }

	    [DllImport("user32.dll", CharSet=CharSet.Auto)]
	    private static extern int SendMessage(IntPtr hWnd, EditMessage msg, SCF wParam, ref CHARFORMAT2 fmt);

	    private enum EditMessage : int
	    {
	        FIRST				= 0x400,
	        GETLIMITTEXT		= FIRST + 37,
	        POSFROMCHAR			= FIRST + 38,
	        CHARFROMPOS			= FIRST + 39,
	        SCROLLCARET			= FIRST + 49,
	        CANPASTE			= FIRST + 50,
	        DISPLAYBAND			= FIRST + 51,
	        EXGETSEL			= FIRST + 52,
	        EXLIMITTEXT			= FIRST + 53,
	        EXLINEFROMCHAR		= FIRST + 54,
	        EXSETSEL			= FIRST + 55,
	        FINDTEXT			= FIRST + 56,
	        FORMATRANGE			= FIRST + 57,
	        GETCHARFORMAT		= FIRST + 58,
	        GETEVENTMASK		= FIRST + 59,
	        GETOLEINTERFACE		= FIRST + 60,
	        GETPARAFORMAT		= FIRST + 61,
	        GETSELTEXT			= FIRST + 62,
	        HIDESELECTION		= FIRST + 63,
	        PASTESPECIAL		= FIRST + 64,
	        REQUESTRESIZE		= FIRST + 65,
	        SELECTIONTYPE		= FIRST + 66,
	        SETBKGNDCOLOR		= FIRST + 67,
	        SETCHARFORMAT		= FIRST + 68,
	        SETEVENTMASK		= FIRST + 69,
	        SETOLECALLBACK		= FIRST + 70,
	        SETPARAFORMAT		= FIRST + 71,
	        SETTARGETDEVICE		= FIRST + 72,
	        STREAMIN			= FIRST + 73,
	        STREAMOUT			= FIRST + 74,
	        GETTEXTRANGE		= FIRST + 75,
	        FINDWORDBREAK		= FIRST + 76,
	        SETOPTIONS			= FIRST + 77,
	        GETOPTIONS			= FIRST + 78,
	        FINDTEXTEX			= FIRST + 79,
	        GETWORDBREAKPROCEX	= FIRST + 80,
	        SETWORDBREAKPROCEX	= FIRST + 81,
	        // RichEdit 2.0 messages
	        SETUNDOLIMIT		= FIRST + 82,
	        REDO				= FIRST + 84,
	        CANREDO				= FIRST + 85,
	        GETUNDONAME			= FIRST + 86,
	        GETREDONAME			= FIRST + 87,
	        STOPGROUPTYPING		= FIRST + 88,
	        SETTEXTMODE			= FIRST + 89,
	        GETTEXTMODE			= FIRST + 90,
	        SETTEXTEX           = FIRST + 97,
	    }

	    private enum SCF : int
	    {
	        SELECTION		= 0x0001,
	        WORD			= 0x0002,
	        DEFAULT			= 0x0000,	// Set default charformat or paraformat
	        ALL				= 0x0004,	// Not valid with SCF_SELECTION or SCF_WORD
	        USEUIRULES		= 0x0008,	// Modifier for SCF_SELECTION; says that
	        //  format came from a toolbar, etc., and
	        //  hence UI formatting rules should be
	        //  used instead of literal formatting
	        ASSOCIATEFONT	= 0x0010,	// Associate fontname with bCharSet (one
	        //  possible for each of Western, ME, FE,
	        //  Thai)
	        NOKBUPDATE		= 0x0020,	// Do not update KB layput for this change
	        //  even if autokeyboard is on
	        ASSOCIATEFONT2	= 0x0040,	// Associate plane-2 (surrogate) font
	    }
	}

    internal partial class FileChange
    {
        public override string ToString()
        {
            return String.Format("{0}#{1} ({2})", Name, Revision, ChangeType);
        }

    }

}

