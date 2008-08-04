using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.SamplePlugins.FriendFeed
{
    public partial class ShareDialog : Form
    {
        public ShareDialog()
        {
            InitializeComponent();
        }

        public void ShowResource( IResource resource )
        {
            _edtURL.Text = resource.GetStringProp( "Link" );
            _edtTitle.Text = resource.GetStringProp( Core.Props.Subject );
        }

        public string Title
        {
            get { return _edtTitle.Text; }
        }

        public string Comment
        {
            get { return _edtComment.Text; }
        }
    }
}