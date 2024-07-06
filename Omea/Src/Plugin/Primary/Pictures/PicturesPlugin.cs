// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Pictures
{
    [PluginDescriptionAttribute("Pictures", "JetBrains Inc.", "Graphical files viewer in Omea.", PluginDescriptionFormat.PlainText, "Icons/PicturesPluginIcon.png")]
	public class PicturesPlugin : IPlugin, IResourceDisplayer
    {
        #region IPlugin Members

        public void Register()
        {
            _preview = new PicturePreview();
            RegisterTypes();
            Core.PluginLoader.RegisterResourceDisplayer( "Picture", this );
        }

        public void Startup()
        {
        }

        public void Shutdown()
        {
            _preview.Dispose();
            Core.FileResourceManager.DeregisterFileResourceType( "Picture" );
        }

        #endregion

        #region IResourceDisplayer Members

        public IDisplayPane CreateDisplayPane( string resourceType )
        {
            return _preview;
        }

        #endregion

        #region implementation details

        private void RegisterTypes()
        {
            string exts = Core.SettingStore.ReadString( "FilePlugin", "PicExts" );
            exts = ( exts.Length == 0 ) ? ".bmp,.jpg,.jpeg,.gif,.ico,.png" : exts + ",.bmp,.jpg,.jpeg,.gif,.ico,.png";
            string[] extsArray = exts.Split( ',' );
            for( int i = 0; i < extsArray.Length; ++i )
            {
                extsArray[ i ] = extsArray[ i ].Trim();
            }
            Core.FileResourceManager.RegisterFileResourceType( "Picture", "Picture", "Name",
                ResourceTypeFlags.Normal, this, extsArray );
            foreach( string ext in extsArray )
            {
                Core.FileResourceManager.SetContentType( "Picture", "image/" + ext.Substring( 1 ) );
            }
        }

        internal static PicturePreview  _preview;

        #endregion
    }

    public class ToggleZoomAction : IAction
    {
        public void Execute( IActionContext context )
        {
            PicturesPlugin._preview.ToggleShrinkToFit();
            Core.ResourceBrowser.RedisplaySelectedResource();
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            IResourceList selected = context.SelectedResources;
            presentation.Visible = selected.Count > 0 && selected[ 0 ].Type == "Picture";
            if( presentation.Visible )
            {
				presentation.Enabled = true;
				presentation.Text = PicturesPlugin._preview.ShrinkToFit ? "Shrink to Fit (ON)" : "Shrink to Fit (OFF)";
            }
        }
    }
}
