/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.FiltersManagement
{
    public class FilterManagerStandards : IStandardConditions
    {
        public string   ResourceIsUnreadName            { get{ return "Resource is not read";  } }
        public string   ResourceIsUnreadNameDeep        { get{ return "notread";               } }
        public string   ResourceIsFlaggedName           { get{ return "Resource is flagged";   } }
        public string   ResourceIsFlaggedNameDeep       { get{ return "flagged";               } }
        public string   ResourceIsAnnotatedName         { get{ return "Resource is annotated"; } }
        public string   ResourceIsAnnotatedNameDeep     { get{ return "annotated";             } }
        public string   ResourceIsCategorizedName       { get{ return "Resource is categorized"; } }
        public string   ResourceIsCategorizedNameDeep   { get{ return "categorized";           } }
        public string   ResourceIsAClippingName         { get{ return "Resource is a clipping"; } }
        public string   ResourceIsAClippingNameDeep     { get{ return "clipping";              } }
        public string   ResourceIsDeletedName           { get{ return "Resource is deleted";   } }
        public string   ResourceIsDeletedNameDeep       { get{ return "deleted";               } }
        public string   ResourceHasEmptyContentName     { get{ return "Resource has empty text content"; } }
        public string   ResourceHasEmptyContentNameDeep { get{ return "textcontentempty";      } }

        public string   ResourceIsFlaggedWithFlagXName  { get{ return "Resource is flagged with %flag(s)%"; } }
        public string   ResourceIsFlaggedWithFlagXNameDeep  { get{ return "flaggedwith";    } }
        public string   SizeIsInTheIntervalXName        { get{ return "Size is %in the interval% (bytes)";  } }
        public string   SizeIsInTheIntervalXNameDeep    { get{ return "Size";               } }
        public string   ResourceBelongsToWorkspaceXName { get{ return "Resource belongs to the %specified% workspace"; } }
        public string   ResourceBelongsToWorkspaceXNameDeep { get{ return "inworkspace";    } }
        public string   MessageIsInThreadOfXName        { get{ return "Message is in the thread of a %head%"; } }
        public string   MessageIsInThreadOfXNameDeep    { get{ return "msginthreadof"; } }
        public string   FromContactXName                { get{ return "From %Contact(s)%";  } }
        public string   FromContactXNameDeep            { get{ return "from";               } }
        public string   ToContactXName                  { get{ return "Sent to %Contact(s)%"; } }
        public string   ToContactXNameDeep              { get{ return "to";                 } }
        public string   CCContactXName                  { get{ return "Copied (CC) to %Contact(s)%"; } }
        public string   CCContactXNameDeep              { get{ return "cc";                 } }
        public string   FromToCCContactXName            { get{ return "From, To or CC to %Contact(s)%"; } }
        public string   FromToCCContactXNameDeep        { get{ return "fromtocc";           } }
        public string   FromToContactXName              { get{ return "From or To %Contact(s)%"; } }
        public string   FromToContactXNameDeep          { get{ return "fromto";             } }

        public string   BodyMatchesSearchQueryXName     { get{ return "Matching %query% in the body"; } }
        public string   BodyMatchesSearchQueryXNameDeep { get{ return "querybody";          } }
        public string   SubjectMatchSearchQueryXName    { get{ return "Matching %query% in the subject/header"; } }
        public string   SubjectMatchSearchQueryXNameDeep{ get{ return "querysubject";       } }
        public string   SourceMatchSearchQueryXName     { get{ return "Matching %query% in the source/sender"; } }
        public string   SourceMatchSearchQueryXNameDeep { get{ return "querysource";        } }
        public string   SubjectIsTextXName              { get{ return "Subject is %text%";  } }
        public string   SubjectIsTextXNameDeep          { get{ return "Subjectis";          } }
        public string   SubjectContainsTextXName        { get{ return "Subject contains %text%"; } }
        public string   SubjectContainsTextXNameDeep    { get{ return "Subjectcontains";    } }
        public string   ResourceContainsTextXName       { get{ return "Resource contains %text%"; } }
        public string   ResourceContainsTextXNameDeep   { get{ return "Rescontainstext";    } }


        public string   InTheCategoryXName              { get{ return "With %the following% category(ies)"; } }
        public string   InTheCategoryXNameDeep          { get{ return "category";           } }
        public string   InTheCategoryAndSubcategoriesXName     { get{ return "With %the following% category(ies) and their subcategories"; } }
        public string   InTheCategoryAndSubcategoriesXNameDeep { get{ return "categoryandsubcategories";  } }
        public string   SenderIsInTheCategoryXName      { get{ return "Sender is in %category(ies)%"; } }
        public string   SenderIsInTheCategoryXNameDeep  { get{ return "Senderincategory";   } }
        public string   ReceivedInTheTimeSpanXName      { get{ return "Received within or dated by %time span%"; } }
        public string   ReceivedInTheTimeSpanXNameDeep  { get{ return "timespan";           } }
        public string   DeletedInTheTimeSpanXName       { get{ return "Deleted on or within %time span%";} }
        public string   DeletedInTheTimeSpanXNameDeep   { get{ return "delspan";            } }

        public string   ReceivedAheadOfTodayName        { get{ return "Resource date is in the future"; } }
        public string   ReceivedAheadOfTodayNameDeep    { get{ return "aheadtoday";         } }
        public string   MessageHasReplyName             { get{ return "Message has been replied to"; } }
        public string   MessageHasReplyDeep             { get{ return "msghasreply";         } }
        public string   MessageIsAReplyName             { get{ return "Message is a reply"; } }
        public string   MessageIsAReplyDeep             { get{ return "msgisareply";         } }
        
      
        public string   DeleteResourceActionName        { get{ return "Delete resource";       } }
        public string   DeleteResourceActionNameDeep    { get{ return "Delete";                } }
        public string   DeleteResourcePermActionName    { get{ return "Delete resource permanently"; } }
        public string   DeleteResourcePermActionNameDeep{ get{ return "Deletepermanently";     } }
        public string   MarkResourceAsReadActionName    { get{ return "Mark resource as read"; } }
        public string   MarkResourceAsReadActionNameDeep{ get{ return "read";                  } }
        public string   MarkResourceAsUnreadActionName  { get{ return "Mark resource as unread"; } }
        public string   MarkResourceAsUnreadActionNameDeep{ get{ return "unread";              } }
        public string   MarkResourceAsImportantActionName { get{ return "Mark resource as important"; } }
        public string   MarkResourceAsImportantActionNameDeep { get{ return "important";       } }
        public string   MarkMessageWithFlagActionName   { get{ return "Mark message with %flag%";   } }
        public string   MarkMessageWithFlagActionNameDeep{ get{ return "markflag";             } }
        public string   ShowDesktopAlertActionName      { get{ return "Show desktop alert";    } }
        public string   ShowDesktopAlertActionNameDeep  { get{ return "alert";                 } }
        public string   ShowAsPlainTextActionName       { get{ return "Show as plain text";    } }
        public string   ShowAsPlainTextActionNameDeep   { get{ return "plaintext";             } }

        public string   AssignCategoryActionName        { get{ return "Assign %specified% category"; } }
        public string   AssignCategoryActionNameDeep    { get{ return "assigncategory";              } }
        public string   AssignCategoryToAuthorActionName{ get{ return "Assign %category% to an author of a message"; } }
        public string   AssignCategoryToAuthorActionNameDeep{ get{ return "categorytoauthor";        } }
        public string   PlaySoundFromFileActionName     { get{ return "Play sound from %file%";      } }
        public string   PlaySoundFromFileActionNameDeep { get{ return "playsoundfile";               } }
        public string   DisplayMessageBoxActionName     { get{ return "Display message box with %text%";  } }
        public string   DisplayMessageBoxActionNameDeep { get{ return "displaymessage";              } }
        public string   RunApplicationActionName        { get{ return "Run application %file%";  } }
        public string   RunApplicationActionNameDeep    { get{ return "runapp";  } }

        public static string  DummyConditionName        { get{ return "DummyExpCondition"; } }

        //---------------------------------------------------------------------
        private static IResource GetTypedResource( String type, String name, String msgTypeName )
        {
            IResource res = Core.ResourceStore.FindUniqueResource(type, "DeepName", name);
            if (res == null) throw new ApplicationException("FilterManagerStandards -- Internal error: " + msgTypeName + " is not defined.");
            return res;
        }

        public IResource   ResourceIsUnread
        {
            get { return GetTypedResource( FilterManagerProps.ConditionResName, ResourceIsUnreadNameDeep, "condition" );  }
        }
        public IResource   ResourceIsFlagged
        {
            get { return GetTypedResource( FilterManagerProps.ConditionResName, ResourceIsFlaggedNameDeep, "condition" );  }
        }
        public IResource   ResourceIsAnnotated
        {
            get { return GetTypedResource( FilterManagerProps.ConditionResName, ResourceIsAnnotatedNameDeep, "condition" );  }
        }
        public IResource   ResourceIsCategorized
        {
            get { return GetTypedResource( FilterManagerProps.ConditionResName, ResourceIsCategorizedNameDeep, "condition" );  }
        }
        public IResource   ResourceIsAClipping
        {
            get { return GetTypedResource( FilterManagerProps.ConditionResName, ResourceIsAClippingNameDeep, "condition" );  }
        }
        public IResource   ResourceIsDeleted
        {
            get { return GetTypedResource( FilterManagerProps.ConditionResName, ResourceIsDeletedNameDeep, "condition" );  }
        }
        public IResource   ResourceHasEmptyContent
        {
            get { return GetTypedResource( FilterManagerProps.ConditionResName, ResourceHasEmptyContentNameDeep, "condition" );  }
        }

        public IResource   ResourceIsFlaggedWithFlagX
        {
            get { return GetTypedResource( FilterManagerProps.ConditionTemplateResName, ResourceIsFlaggedWithFlagXNameDeep, "condition template" );  }
        }
        public IResource   SizeIsInTheIntervalX
        {
            get { return GetTypedResource( FilterManagerProps.ConditionTemplateResName, SizeIsInTheIntervalXNameDeep, "condition template" );  }
        }
        public IResource   ResourceBelongsToWorkspaceX
        {
            get { return GetTypedResource( FilterManagerProps.ConditionTemplateResName, ResourceBelongsToWorkspaceXNameDeep, "condition template" );  }
        }
        public IResource   MessageIsInThreadOfX
        {
            get { return GetTypedResource( FilterManagerProps.ConditionTemplateResName, MessageIsInThreadOfXNameDeep, "condition template" );  }
        }

        public IResource   FromContactX
        {
            get { return GetTypedResource( FilterManagerProps.ConditionTemplateResName, FromContactXNameDeep, "condition template" );  }
        }
        public IResource   ToContactX
        {
            get { return GetTypedResource( FilterManagerProps.ConditionTemplateResName, ToContactXNameDeep, "condition template" );  }
        }
        public IResource   CCContactX
        {
            get { return GetTypedResource( FilterManagerProps.ConditionTemplateResName, CCContactXNameDeep, "condition template" );  }
        }
        public IResource   FromToCCContactX
        {
            get { return GetTypedResource( FilterManagerProps.ConditionTemplateResName, FromToCCContactXNameDeep, "condition template" );  }
        }
        public IResource   FromToContactX
        {
            get { return GetTypedResource( FilterManagerProps.ConditionTemplateResName, FromToContactXNameDeep, "condition template" );  }
        }

        public IResource   BodyMatchesSearchQueryX
        {
            get { return GetTypedResource( FilterManagerProps.ConditionTemplateResName, BodyMatchesSearchQueryXNameDeep, "condition template" );  }
        }
        public IResource   SubjectMatchSearchQueryX
        {
            get { return GetTypedResource( FilterManagerProps.ConditionTemplateResName, SubjectMatchSearchQueryXNameDeep, "condition template" );  }
        }
        public IResource   SourceMatchSearchQueryX
        {
            get { return GetTypedResource( FilterManagerProps.ConditionTemplateResName, SourceMatchSearchQueryXNameDeep, "condition template" );  }
        }
        public IResource   SubjectIsTextX
        {
            get { return GetTypedResource( FilterManagerProps.ConditionTemplateResName, SubjectIsTextXNameDeep, "condition template" );  }
        }
        public IResource   SubjectContainsTextX
        {
            get { return GetTypedResource( FilterManagerProps.ConditionTemplateResName, SubjectContainsTextXNameDeep, "condition template" );  }
        }
        public IResource   ResourceContainsTextX
        {
            get { return GetTypedResource( FilterManagerProps.ConditionTemplateResName, ResourceContainsTextXNameDeep, "condition template" );  }
        }

        public IResource   InTheCategoryX
        {
            get { return GetTypedResource( FilterManagerProps.ConditionTemplateResName, InTheCategoryXNameDeep, "condition template" );  }
        }
        public IResource   InTheCategoryAndSubcategoriesX
        {
            get { return GetTypedResource( FilterManagerProps.ConditionTemplateResName, InTheCategoryAndSubcategoriesXNameDeep, "condition template" );  }
        }
        public IResource   SenderIsInTheCategoryX
        {
            get { return GetTypedResource( FilterManagerProps.ConditionTemplateResName, SenderIsInTheCategoryXNameDeep, "condition template" );  }
        }
        public IResource   ReceivedInTheTimeSpanX
        {
            get { return GetTypedResource( FilterManagerProps.ConditionTemplateResName, ReceivedInTheTimeSpanXNameDeep, "condition template" );  }
        }
        public IResource DeletedInTheTimeSpanX
        {
            get { return GetTypedResource( FilterManagerProps.ConditionTemplateResName, DeletedInTheTimeSpanXNameDeep, "condition template" );  }
        }

        public IResource   ReceivedAheadOfToday
        {
            get { return GetTypedResource( FilterManagerProps.ConditionResName, ReceivedAheadOfTodayNameDeep, "condition" );  }
        }
        public IResource   MessageHasReply
        {
            get { return GetTypedResource( FilterManagerProps.ConditionResName, MessageHasReplyDeep, "condition" );  }
        }
        public IResource   MessageIsAReply
        {
            get { return GetTypedResource( FilterManagerProps.ConditionResName, MessageIsAReplyDeep, "condition" );  }
        }

        public IResource   DeleteResourceAction
        {
            get { return GetTypedResource( FilterManagerProps.RuleActionResName, DeleteResourceActionNameDeep, "action" );  }
        }
        public IResource   DeleteResourcePermAction
        {
            get { return GetTypedResource( FilterManagerProps.RuleActionResName, DeleteResourcePermActionNameDeep, "action" );  }
        }
        public IResource   MarkResourceAsReadAction
        {
            get { return GetTypedResource( FilterManagerProps.RuleActionResName, MarkResourceAsReadActionNameDeep, "action" );  }
        }
        public IResource   MarkResourceAsUnreadAction
        {
            get { return GetTypedResource( FilterManagerProps.RuleActionResName, MarkResourceAsUnreadActionNameDeep, "action" );  }
        }
        public IResource   MarkResourceAsImportantAction
        {
            get { return GetTypedResource( FilterManagerProps.RuleActionResName, MarkResourceAsImportantActionNameDeep, "action" );  }
        }
        public IResource   MarkMessageWithFlagAction
        {
            get { return GetTypedResource( FilterManagerProps.RuleActionResName, MarkMessageWithFlagActionNameDeep, "action" );  }
        }
        public IResource   ShowDesktopAlertAction
        {
            get { return GetTypedResource( FilterManagerProps.RuleActionResName, ShowDesktopAlertActionNameDeep, "action" );  }
        }
        public IResource   ShowAsPlainTextAction
        {
            get { return GetTypedResource( FilterManagerProps.RuleActionResName, ShowAsPlainTextActionNameDeep, "action" );  }
        }

        public IResource   AssignCategoryAction
        {
            get { return GetTypedResource( FilterManagerProps.RuleActionTemplateResName, AssignCategoryActionNameDeep, "action template" );  }
        }
        public IResource   AssignCategoryToAuthorAction
        {
            get { return GetTypedResource( FilterManagerProps.RuleActionTemplateResName, AssignCategoryToAuthorActionNameDeep, "action template" );  }
        }
        public IResource   PlaySoundFromFileAction
        {
            get { return GetTypedResource( FilterManagerProps.RuleActionTemplateResName, PlaySoundFromFileActionNameDeep, "action template" );  }
        }
        public IResource   DisplayMessageBoxAction
        {
            get { return GetTypedResource( FilterManagerProps.RuleActionTemplateResName, DisplayMessageBoxActionNameDeep, "action template" );  }
        }
        public IResource   RunApplicationAction
        {
            get { return GetTypedResource( FilterManagerProps.RuleActionTemplateResName, RunApplicationActionNameDeep, "action template" );  }
        }

        public static IResource  DummyCondition
        {
            get
            {
                IResource res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionResName, "Name", "DummyExpCondition" );
                if( res == null ) throw new ApplicationException( "FilterManagerStandards -- Internal error: action template is not defined." );
                return res;
            }
        }
    }
}