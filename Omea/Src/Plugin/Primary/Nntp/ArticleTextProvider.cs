/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.Nntp
{
    class ArticleTextProvider : IResourceTextProvider
    {
        #region IResourceTextProvider Members

        bool IResourceTextProvider.ProcessResourceText( IResource res, IResourceTextConsumer consumer )
        {
            if (res != null)
            {
                int id = res.Id;
//                if (res.Type == NntpPlugin._newsArticle || res.Type == NntpPlugin._newsLocalArticle)
                if( NntpPlugin.IsNntpType( res.Type ) )
                    {
                    string text = res.GetPropText( Core.Props.LongBody );
                    if( text.Trim().Length > 0 )
                    {
                        consumer.AddDocumentFragment( id, text );
                    }
                    else
                    {
                        HtmlIndexer.IndexHtml( res, res.GetPropText( NntpPlugin._propHtmlContent ), consumer, DocumentSection.BodySection );
                    }
                    consumer.RestartOffsetCounting();
                    consumer.AddDocumentHeading( id, res.GetPropText( Core.Props.Subject ) );

                    IResource author = res.GetLinkProp( Core.ContactManager.Props.LinkFrom );
                    IResource account = res.GetLinkProp( Core.ContactManager.Props.LinkEmailAcctFrom );
                    if( author != null )
                    {
                        //  Construct [From] section out of contact name and its account
                        string fromText = author.DisplayName;
                        if (account != null)
                            fromText += " " + account.DisplayName;
                        consumer.AddDocumentFragment( id, fromText + " ", DocumentSection.SourceSection );
                    }
                    IResourceList groups = res.GetLinksOfType( NntpPlugin._newsGroup, NntpPlugin._propTo );
                    foreach( IResource group in groups )
                    {
                        consumer.AddDocumentFragment( id, group.GetPropText( Core.Props.Name ) + " ", DocumentSection.SourceSection );
                    }
                }
                else
                {
                    IResource article = res.GetLinkProp( NntpPlugin._propAttachment );
                    if( article != null && NntpPlugin.IsNntpType( article.Type ) )
                    {
                        consumer.AddDocumentHeading(id, res.GetPropText(Core.Props.Name));
                    }
                }
            }
            return true;
        }
        #endregion
    }
}
