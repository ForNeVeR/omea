// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using JetBrains.Omea.Base;
using JetBrains.Omea.Contacts;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.HTML;
using JetBrains.Omea.OpenAPI;

namespace GUIControls
{
	/// <summary>
	/// Summary description for ExportListForm.
	/// </summary>
	public class ExportListForm : DialogBase
	{
        private GroupBox boxRange;
        private RadioButton radioSelected;
        private RadioButton radioOwnerResource;
        private RadioButton radioTyped;
        private GroupBox boxExportFormat;
        private RadioButton radioText;
        private RadioButton radioHtml;
        private RadioButton radioTabs;
        private RadioButton radioString;
        private CheckBox    checkHeader;
        private TextBox textDelimiter;
        private Button btnFile;
        private Button btnOK;
        private Button btnCancel;
        private TextBox textFile;
        private GroupBox boxDelimiters;
        private RadioButton radioXml;

        private readonly ColumnDescriptor[] displayedColumns;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public ExportListForm( ColumnDescriptor[] columns )
		{
			InitializeComponent();

            //  Set appropriate names and tags for radio button texts.
            displayedColumns = columns;
            InitializeRangeSettings();
            InitializeFormatSettings();

            RestoreSettings();
        }

        private void  InitializeRangeSettings()
        {
            IResourceList selected = Core.ResourceBrowser.SelectedResources;
            radioSelected.Checked = (selected.Count > 0);
            if( !radioSelected.Checked )
            {
                radioSelected.Enabled = false;
                radioOwnerResource.Checked = true;
            }

            if( Core.ResourceBrowser.OwnerResource != null )
                radioOwnerResource.Text = "Resources in \"" + Core.ResourceBrowser.OwnerResource.DisplayName + "\"";
            else
            {
                radioOwnerResource.Enabled = false;
                radioOwnerResource.Text = "Resources in the parent folder";

                //  Move selection to the previous or next control depending
                //  on what is enabled.
                if( radioSelected.Enabled )
                    radioSelected.Checked = true;
                else
                    radioTyped.Checked = true;
            }

            string[]  tabTypes = Core.TabManager.CurrentTab.GetResourceTypes();
            if( tabTypes != null && tabTypes.Length > 0 )
            {
                IResource resType = Core.ResourceStore.FindUniqueResource( "ResourceType", "Name", tabTypes[ 0 ] );
                radioTyped.Text = "All resources of type " + resType.DisplayName;
            }
            else
            {
                radioTyped.Text = "All resources of all types";
                radioTyped.Enabled = false;

                //  Move selection to the previous or next control depending
                //  on what is enabled.
                if( radioOwnerResource.Enabled )
                    radioSelected.Checked = true;
            }
        }

        private void  InitializeFormatSettings()
        {
            textDelimiter.Text = Core.SettingStore.ReadString( "Omea", "ExportListStringDelimiter", string.Empty );
            checkHeader.Checked = Core.SettingStore.ReadBool( "Omea", "IncludeHeaders", false );

            string format = Core.SettingStore.ReadString( "Omea", "ExportListFormat", "xml" );
            switch( format )
            {
                case "text" : radioText.Checked = true; break;
                case "xml" : radioXml.Checked = true; break;
                case "html" : radioHtml.Checked = true; break;
                default: radioXml.Checked = true; break;
            }
            radioTabs.Checked = true;
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

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.boxRange = new System.Windows.Forms.GroupBox();
            this.radioTyped = new System.Windows.Forms.RadioButton();
            this.radioOwnerResource = new System.Windows.Forms.RadioButton();
            this.radioSelected = new System.Windows.Forms.RadioButton();
            this.boxExportFormat = new System.Windows.Forms.GroupBox();
            this.radioXml = new System.Windows.Forms.RadioButton();
            this.boxDelimiters = new System.Windows.Forms.GroupBox();
            this.radioTabs = new System.Windows.Forms.RadioButton();
            this.radioString = new System.Windows.Forms.RadioButton();
            this.textDelimiter = new System.Windows.Forms.TextBox();
            this.radioHtml = new System.Windows.Forms.RadioButton();
            this.radioText = new System.Windows.Forms.RadioButton();
            this.checkHeader = new CheckBox();
            this.btnFile = new System.Windows.Forms.Button();
            this.textFile = new System.Windows.Forms.TextBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.boxRange.SuspendLayout();
            this.boxExportFormat.SuspendLayout();
            this.boxDelimiters.SuspendLayout();
            this.SuspendLayout();
            //
            // boxRange
            //
            this.boxRange.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.boxRange.Controls.Add(this.radioTyped);
            this.boxRange.Controls.Add(this.radioOwnerResource);
            this.boxRange.Controls.Add(this.radioSelected);
            this.boxRange.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.boxRange.Location = new System.Drawing.Point(8, 8);
            this.boxRange.Name = "boxRange";
            this.boxRange.Size = new System.Drawing.Size(224, 96);
            this.boxRange.TabIndex = 0;
            this.boxRange.TabStop = false;
            this.boxRange.Text = "Select range of resources";
            //
            // radioTyped
            //
            this.radioTyped.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.radioTyped.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radioTyped.Location = new System.Drawing.Point(8, 68);
            this.radioTyped.Name = "radioTyped";
            this.radioTyped.Size = new System.Drawing.Size(212, 20);
            this.radioTyped.TabIndex = 2;
            this.radioTyped.Text = "&All resource of type";
            //
            // radioOwnerResource
            //
            this.radioOwnerResource.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.radioOwnerResource.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radioOwnerResource.Location = new System.Drawing.Point(8, 42);
            this.radioOwnerResource.Name = "radioOwnerResource";
            this.radioOwnerResource.Size = new System.Drawing.Size(212, 20);
            this.radioOwnerResource.TabIndex = 1;
            this.radioOwnerResource.Text = "&Resources in ";
            //
            // radioSelected
            //
            this.radioSelected.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radioSelected.Location = new System.Drawing.Point(8, 16);
            this.radioSelected.Name = "radioSelected";
            this.radioSelected.Size = new System.Drawing.Size(132, 20);
            this.radioSelected.TabIndex = 0;
            this.radioSelected.Text = "S&elected resources";
            //
            // boxExportFormat
            //
            this.boxExportFormat.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.boxExportFormat.Controls.Add(this.radioXml);
            this.boxExportFormat.Controls.Add(this.boxDelimiters);
            this.boxExportFormat.Controls.Add(this.radioHtml);
            this.boxExportFormat.Controls.Add(this.radioText);
            this.boxExportFormat.Controls.Add(this.checkHeader);
            this.boxExportFormat.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.boxExportFormat.Location = new System.Drawing.Point(8, 112);
            this.boxExportFormat.Name = "boxExportFormat";
            this.boxExportFormat.Size = new System.Drawing.Size(224, 180);
            this.boxExportFormat.TabIndex = 1;
            this.boxExportFormat.TabStop = false;
            this.boxExportFormat.Text = "Export format";
            //
            // radioXml
            //
            this.radioXml.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radioXml.Location = new System.Drawing.Point(12, 20);
            this.radioXml.Name = "radioXml";
            this.radioXml.Size = new System.Drawing.Size(56, 24);
            this.radioXml.TabIndex = 7;
            this.radioXml.Text = "&XML";
            this.radioXml.CheckedChanged += new System.EventHandler(this.Format_CheckedChanged);
            //
            // boxDelimiters
            //
            this.boxDelimiters.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.boxDelimiters.Controls.Add(this.radioTabs);
            this.boxDelimiters.Controls.Add(this.radioString);
            this.boxDelimiters.Controls.Add(this.textDelimiter);
            this.boxDelimiters.Location = new System.Drawing.Point(20, 77);
            this.boxDelimiters.Name = "boxDelimiters";
            this.boxDelimiters.Size = new System.Drawing.Size(196, 44);
            this.boxDelimiters.TabIndex = 6;
            this.boxDelimiters.TabStop = false;
            this.boxDelimiters.Text = "Delimit fileds with";
            this.boxDelimiters.FlatStyle = System.Windows.Forms.FlatStyle.System;
            //
            // radioTabs
            //
            this.radioTabs.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radioTabs.Location = new System.Drawing.Point(8, 16);
            this.radioTabs.Name = "radioTabs";
            this.radioTabs.Size = new System.Drawing.Size(52, 20);
            this.radioTabs.TabIndex = 4;
            this.radioTabs.Text = "T&abs";
            this.radioTabs.CheckedChanged += new System.EventHandler(this.Delimiter_CheckedChanged);
            //
            // radioString
            //
            this.radioString.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radioString.Location = new System.Drawing.Point(64, 16);
            this.radioString.Name = "radioString";
            this.radioString.Size = new System.Drawing.Size(64, 20);
            this.radioString.TabIndex = 5;
            this.radioString.Text = "St&ring:";
            this.radioString.CheckedChanged += new System.EventHandler(this.Delimiter_CheckedChanged);
            //
            // textDelimiter
            //
            this.textDelimiter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.textDelimiter.Location = new System.Drawing.Point(128, 16);
            this.textDelimiter.Name = "textDelimiter";
            this.textDelimiter.Size = new System.Drawing.Size(60, 21);
            this.textDelimiter.TabIndex = 6;
            this.textDelimiter.Text = "";
            this.textDelimiter.TextChanged += new System.EventHandler(this.textDelimiter_TextChanged);
            //
            // radioHtml
            //
            this.radioHtml.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radioHtml.Location = new System.Drawing.Point(12, 128);
            this.radioHtml.Name = "radioHtml";
            this.radioHtml.Size = new System.Drawing.Size(60, 20);
            this.radioHtml.TabIndex = 8;
            this.radioHtml.Text = "&Html";
            this.radioHtml.CheckedChanged += new System.EventHandler(this.Format_CheckedChanged);
            //
            // radioText
            //
            this.radioText.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radioText.Location = new System.Drawing.Point(12, 52);
            this.radioText.Name = "radioText";
            this.radioText.Size = new System.Drawing.Size(60, 20);
            this.radioText.TabIndex = 3;
            this.radioText.Text = "&Text";
            this.radioText.CheckedChanged += new System.EventHandler(this.Format_CheckedChanged);
            //
            //
            //
            this.checkHeader.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.checkHeader.Location = new System.Drawing.Point(20, 152);
            this.checkHeader.Size = new System.Drawing.Size(90, 20);
            this.checkHeader.Text = "&Include header";
            this.checkHeader.Name = "checkHeader";
            this.checkHeader.TabIndex = 8;
            //
            // btnFile
            //
            this.btnFile.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnFile.Location = new System.Drawing.Point(8, 300);
            this.btnFile.Name = "btnFile";
            this.btnFile.TabIndex = 9;
            this.btnFile.Text = "&Save to...";
            this.btnFile.Click += new System.EventHandler(this.btnFile_Click);
            //
            // textFile
            //
            this.textFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.textFile.Location = new System.Drawing.Point(88, 300);
            this.textFile.Name = "textFile";
            this.textFile.Size = new System.Drawing.Size(144, 21);
            this.textFile.TabIndex = 10;
            this.textFile.Text = "";
            this.textFile.TextChanged += new System.EventHandler(this.textFile_TextChanged);
            //
            // btnOK
            //
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.Location = new System.Drawing.Point(240, 16);
            this.btnOK.Name = "btnOK";
            this.btnOK.TabIndex = 11;
            this.btnOK.Text = "OK";
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            //
            // btnCancel
            //
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(240, 48);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.TabIndex = 12;
            this.btnCancel.Text = "Cancel";
            //
            // ExportListForm
            //
            this.AcceptButton = this.btnOK;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(324, 335);
            this.ControlBox = false;
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.textFile);
            this.Controls.Add(this.btnFile);
            this.Controls.Add(this.boxExportFormat);
            this.Controls.Add(this.boxRange);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            int height = 369;
            if( Core.ScaleFactor.Height > 1.01 )
                height = (int)(height * (Core.ScaleFactor.Height + 0.1));
            this.MinimumSize = new System.Drawing.Size(332, height);
            this.MaximumSize = new System.Drawing.Size(600, height);
            this.Name = "ExportListForm";
            this.Text = "Export Resources List";
            this.boxRange.ResumeLayout(false);
            this.boxExportFormat.ResumeLayout(false);
            this.boxDelimiters.ResumeLayout(false);
            this.ResumeLayout(false);

        }
		#endregion

        private void btnOK_Click(object sender, EventArgs e)
        {
            string title;
            IResourceList list = GetResources( out title );

            try
            {
                if( radioText.Checked )
                {
                    StreamWriter sw = new StreamWriter( textFile.Text );
                    Core.SettingStore.WriteString( "Omea", "ExportListFormat", "text" );
                    WriteTextFile( sw, list );
                    sw.Close();
                }
                else
                    if( radioXml.Checked )
                    {
                        Core.SettingStore.WriteString( "Omea", "ExportListFormat", "xml" );
                        WriteXmlFile( textFile.Text, list );
                    }
                    else //  Html
                    {
                        StreamWriter sw = new StreamWriter( textFile.Text );
                        Core.SettingStore.WriteString( "Omea", "ExportListFormat", "html" );
                        WriteHtmlFile( sw, list, title );
                        sw.Close();
                    }
            }
            catch (IOException ex)
            {
                ReportSaveError( ex );
                return;
            }
            catch (UnauthorizedAccessException ex)
            {
                ReportSaveError(ex);
                return;
            }
            if( !String.IsNullOrEmpty( textDelimiter.Text ) )
                Core.SettingStore.WriteString( "Omea", "ExportListStringDelimiter", textDelimiter.Text );

            Core.SettingStore.WriteBool( "Omea", "IncludeHeaders", checkHeader.Checked );

            DialogResult = DialogResult.OK;
        }

	    private void ReportSaveError( Exception ex )
	    {
	        MessageBox.Show( this,
	                         "Error saving export file: " + ex.Message +
	                         ". Please enter a valid file name to save to.",
	                         "Error Saving Resources" );
	    }

	    private void btnFile_Click(object sender, EventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            if( radioText.Checked )
            {
                dlg.Filter = "Text files|*.txt|All files|*.*";
            }
            else
            if( radioXml.Checked )
            {
                dlg.Filter = "XML files|*.xml|All files|*.*";
            }
            else //  Html
            {
                dlg.Filter = "Html files|*.html|All files|*.*";
            }

            if( dlg.ShowDialog() == DialogResult.OK )
            {
                textFile.Text = dlg.FileName;
            }
        }

        private void Format_CheckedChanged(object sender, EventArgs e)
        {
            boxDelimiters.Enabled = radioText.Checked;
            checkHeader.Enabled = radioHtml.Checked;
        }

        private void Delimiter_CheckedChanged(object sender, EventArgs e)
        {
            textDelimiter.Enabled = radioString.Checked;
            CheckControls();
        }

        private void textDelimiter_TextChanged(object sender, EventArgs e)
        {
            CheckControls();
        }

        private void textFile_TextChanged(object sender, EventArgs e)
        {
            CheckControls();
        }

        private void  CheckControls()
        {
            bool enabled = !textDelimiter.Enabled || !String.IsNullOrEmpty( textDelimiter.Text );
            enabled = enabled && !String.IsNullOrEmpty( textFile.Text );
            enabled = enabled && (radioSelected.Enabled || radioOwnerResource.Enabled || radioTyped.Enabled);
            btnOK.Enabled = enabled;
       }

        #region Resources
        private IResourceList  GetResources( out string title )
        {
            if( radioSelected.Checked )
            {
                title = "Selected Resources";
                return Core.ResourceBrowser.SelectedResources;
            }
            else
            if( radioOwnerResource.Checked )
            {
                title = Core.ResourceBrowser.OwnerResource.DisplayName;
                return Core.ResourceBrowser.VisibleResources;
            }
            else
            {
                string[] types = Core.TabManager.CurrentTab.GetResourceTypes();
                title = "All resources of type \"" + types[ 0 ] + '\"';
                return Core.ResourceStore.GetAllResources( types );
            }
        }
        #endregion Resources

        #region Writing
        #region Text format
        private void  WriteTextFile( StreamWriter sw, IResourceList list )
        {
            string delim = radioTabs.Checked ? "\t" : textDelimiter.Text;
            foreach( IResource res in list )
            {
                WriteResource( res, sw, delim );
            }
        }

        private void  WriteResource( IResource res, StreamWriter sw, string delimiter )
        {
            foreach( ColumnDescriptor descr in displayedColumns )
            {
                if( (descr.Flags & ColumnDescriptorFlags.FixedSize) == 0 )
                {
                    string[] props = descr.PropNames;
                    string   prop = (props[ 0 ][ 0 ] != '-') ? props[ 0 ] : props[ 0 ].Substring( 1 );
                    string   text = string.Empty;

                    if( prop == "DisplayName")
                        text = res.DisplayName;
                    else
                    {
                        IPropType propType = Core.ResourceStore.PropTypes[ prop ];
                        if( propType.DataType != PropDataType.Link )
                            text = res.GetPropText( propType.Id );
                        else
                        {
                            IResourceList linked = res.GetLinksOfType( null, propType.Name );
                            if( linked.Count > 0 )
                                text = linked[ 0 ].DisplayName;
                        }
                    }
                    sw.Write( text + delimiter );
                }
            }
            sw.WriteLine();
        }
        #endregion Text format

        #region XML format
        private void  WriteXmlFile( string fileName, IResourceList list )
        {
            XmlTextWriter writer = new XmlTextWriter( fileName, new UTF8Encoding( false ) );
            writer.Formatting = Formatting.Indented;
            try
            {
                writer.WriteStartDocument();
                writer.WriteStartElement( "Omea-XML-Export" );
                writer.WriteAttributeString( "version", "1.0" );
                writer.WriteStartElement( "head" );
                writer.WriteElementString( "title", "Omea Resources" );
                writer.WriteEndElement();
                writer.WriteStartElement( "resources" );
                foreach( IResource res in list )
                {
                    WriteResource2Entry( res, writer );
                }
                writer.WriteEndElement();

                writer.WriteEndDocument();
            }
            finally
            {
                writer.Close();
            }

        }

        private void  WriteResource2Entry( IResource res, XmlTextWriter writer )
        {
            writer.WriteStartElement( "resource" );
            writer.WriteAttributeString( "type", res.Type );
            writer.WriteAttributeString( "id", res.Id.ToString() );

            IPropertyCollection props = res.Properties;
            foreach( IResourceProperty prop in props )
            {
                IPropType type = Core.ResourceStore.PropTypes[ prop.Name ];
                if( !type.HasFlag( PropTypeFlags.Internal ) )
                {
                    WriteResourceProperty2Entry( res, prop, writer );
                }
            }
            writer.WriteEndElement();
        }

        private static void WriteResourceProperty2Entry( IResource res, IResourceProperty prop, XmlTextWriter writer )
        {
            int    propId = Core.ResourceStore.PropTypes[ prop.Name ].Id;
            string name = prop.Name;
            ContactManager cMgr = (ContactManager)Core.ContactManager;

            if( prop.DataType != PropDataType.Link )
            {
                string val = HtmlTools.SafeHtmlDecode( prop.Value.ToString() );
                writer.WriteStartElement( "prop" );
                writer.WriteAttributeString( "name", name );
                writer.WriteAttributeString( "value", val );
                writer.WriteEndElement();
            }
            else
            if( !cMgr.IsMajorLink( propId ) && !cMgr.IsNameLink( propId ) && ( propId != Core.Props.Reply ))
            {
                IResourceList linked = res.GetLinksOfType( null, propId );
                string val = HtmlTools.SafeHtmlDecode( linked[ 0 ].DisplayName );
                if( linked.Count == 1 )
                {
                    writer.WriteStartElement( "prop" );
                    writer.WriteAttributeString( "name", name );
                    writer.WriteAttributeString( "value", val );
                    writer.WriteEndElement();
                }
                else
                {
                    writer.WriteStartElement( name + "-set" );
                    foreach( IResource linkRes in linked )
                    {
                        val = HtmlTools.SafeHtmlDecode( linkRes.DisplayName );
                        writer.WriteStartElement( "prop" );
                        writer.WriteAttributeString( "name", name );
                        writer.WriteAttributeString( "value", val );
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }
            }
        }
        #endregion XML format

        #region Html format
        private void  WriteHtmlFile( StreamWriter sw, IResourceList list, string title )
        {
            WriteHtmlHeader( sw, title );
            sw.WriteLine( "<table>");
            if( checkHeader.Checked )
                WriteTableHeader( sw );

            foreach( IResource res in list )
            {
                WriteResource2Table( res, sw );
            }
            sw.WriteLine( "</table>");
            sw.WriteLine( "</body></html>");
        }

        private static void  WriteHtmlHeader( StreamWriter sw, string title )
        {
            sw.WriteLine( "<html><head><title>" + title + "</title>" );
            sw.WriteLine( "<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\">" );
            sw.WriteLine( "<body>");
        }

        private void  WriteTableHeader( StreamWriter sw )
        {
            sw.WriteLine( "<thead><tr>");
            foreach( ColumnDescriptor descr in displayedColumns )
            {
                if( (descr.Flags & ColumnDescriptorFlags.FixedSize) == 0 )
                {
                    string[] props = descr.PropNames;
                    string   prop = (props[ 0 ][ 0 ] != '-') ? props[ 0 ] : props[ 0 ].Substring( 1 );
                    string   text;
                    sw.Write( "<th>" );

                    if( prop == "DisplayName" )
                        text = prop;
                    else
                    {
                        IPropType propType = Core.ResourceStore.PropTypes[ prop ];
                        text = !String.IsNullOrEmpty( propType.DisplayName ) ? propType.DisplayName : propType.Name;
                    }
                    sw.Write( HtmlTools.SafeHtmlDecode( text ) );
                    sw.WriteLine( "</th>" );
                }
            }
            sw.WriteLine( "</tr></thead>");
        }

        private void  WriteResource2Table( IResource res, StreamWriter sw )
        {
            sw.WriteLine( "<tr>");
            foreach( ColumnDescriptor descr in displayedColumns )
            {
                if( (descr.Flags & ColumnDescriptorFlags.FixedSize) == 0 )
                {
                    string[] props = descr.PropNames;
                    string   prop = (props[ 0 ][ 0 ] != '-') ? props[ 0 ] : props[ 0 ].Substring( 1 );
                    string   text = string.Empty;

                    sw.Write( "<td>" );
                    if( prop == "DisplayName")
                        text = res.DisplayName;
                    else
                    {
                        IPropType propType = Core.ResourceStore.PropTypes[ prop ];
                        if( propType.DataType != PropDataType.Link )
                            text = res.GetPropText( propType.Id );
                        else
                        {
                            IResourceList linked = res.GetLinksOfType( null, propType.Name );
                            if( linked.Count > 0 )
                                text = linked[ 0 ].DisplayName;
                        }
                    }
                    sw.Write( HtmlTools.SafeHtmlDecode( text ) );
                    sw.Write( "</td>" );
                }
            }
            sw.WriteLine( "</tr>");
        }
        #endregion Html format
        #endregion Writing
    }
}
