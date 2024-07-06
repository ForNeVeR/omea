// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only


namespace JetBrains.Omea.OpenAPI
{
    /// <summary>
    /// Interface allows to get access to the set of standard conditions and
    /// condition templates by its resource and name.
    /// </summary>
    /// <since>444</since>
    public interface IStandardConditions
    {
        IResource   ResourceIsUnread                    { get; }
        IResource   ResourceIsFlagged                   { get; }
        IResource   ResourceIsAnnotated                 { get; }
        IResource   ResourceIsCategorized               { get; }
        IResource   ResourceIsAClipping                 { get; }
        IResource   ResourceIsDeleted                   { get; }
        IResource   ResourceHasEmptyContent             { get; }

        IResource   ResourceIsFlaggedWithFlagX          { get; }
        IResource   SizeIsInTheIntervalX                { get; }
        IResource   ResourceBelongsToWorkspaceX         { get; }
        IResource   MessageIsInThreadOfX                { get; }

        IResource   FromContactX                        { get; }
        IResource   ToContactX                          { get; }
        IResource   CCContactX                          { get; }
        IResource   FromToCCContactX                    { get; }
        IResource   FromToContactX                      { get; }

        IResource   BodyMatchesSearchQueryX             { get; }
        IResource   SubjectMatchSearchQueryX            { get; }
        IResource   SourceMatchSearchQueryX             { get; }
        IResource   SubjectIsTextX                      { get; }
        IResource   SubjectContainsTextX                { get; }
        IResource   ResourceContainsTextX               { get; }

        IResource   InTheCategoryX                      { get; }
        IResource   InTheCategoryAndSubcategoriesX      { get; }
        IResource   SenderIsInTheCategoryX              { get; }
        IResource   ReceivedInTheTimeSpanX              { get; }
        IResource   DeletedInTheTimeSpanX               { get; }
        IResource   ReceivedAheadOfToday                { get; }
        IResource   MessageHasReply                     { get; }
        IResource   MessageIsAReply                     { get; }

        IResource   DeleteResourceAction                { get; }
        IResource   DeleteResourcePermAction            { get; }
        IResource   MarkResourceAsReadAction            { get; }
        IResource   MarkResourceAsUnreadAction          { get; }
        IResource   MarkResourceAsImportantAction       { get; }
        IResource   MarkMessageWithFlagAction           { get; }
        IResource   ShowDesktopAlertAction              { get; }
        IResource   ShowAsPlainTextAction               { get; }
        IResource   AssignCategoryAction                { get; }
        IResource   AssignCategoryToAuthorAction        { get; }
        IResource   PlaySoundFromFileAction             { get; }
        IResource   DisplayMessageBoxAction             { get; }
        IResource   RunApplicationAction                { get; }

        string      ResourceIsUnreadName                { get; }
        string      ResourceIsUnreadNameDeep            { get; }
        string      ResourceIsFlaggedName               { get; }
        string      ResourceIsFlaggedNameDeep           { get; }
        string      ResourceIsAnnotatedName             { get; }
        string      ResourceIsAnnotatedNameDeep         { get; }
        string      ResourceIsCategorizedName           { get; }
        string      ResourceIsCategorizedNameDeep       { get; }
        string      ResourceIsAClippingName             { get; }
        string      ResourceIsAClippingNameDeep         { get; }
        string      ResourceIsDeletedName               { get; }
        string      ResourceIsDeletedNameDeep           { get; }
        string      ResourceHasEmptyContentName         { get; }
        string      ResourceHasEmptyContentNameDeep     { get; }

        string      ResourceIsFlaggedWithFlagXName      { get; }
        string      ResourceIsFlaggedWithFlagXNameDeep  { get; }
        string      SizeIsInTheIntervalXName            { get; }
        string      SizeIsInTheIntervalXNameDeep        { get; }
        string      ResourceBelongsToWorkspaceXName     { get; }
        string      ResourceBelongsToWorkspaceXNameDeep { get; }
        string      MessageIsInThreadOfXName            { get; }
        string      MessageIsInThreadOfXNameDeep        { get; }

        string      FromContactXName                    { get; }
        string      FromContactXNameDeep                { get; }
        string      ToContactXName                      { get; }
        string      ToContactXNameDeep                  { get; }
        string      CCContactXName                      { get; }
        string      CCContactXNameDeep                  { get; }
        string      FromToCCContactXName                { get; }
        string      FromToCCContactXNameDeep            { get; }
        string      FromToContactXName                  { get; }
        string      FromToContactXNameDeep              { get; }

        string      BodyMatchesSearchQueryXName         { get; }
        string      BodyMatchesSearchQueryXNameDeep     { get; }
        string      SubjectMatchSearchQueryXName        { get; }
        string      SubjectMatchSearchQueryXNameDeep    { get; }
        string      SourceMatchSearchQueryXName         { get; }
        string      SourceMatchSearchQueryXNameDeep     { get; }
        string      SubjectIsTextXName                  { get; }
        string      SubjectIsTextXNameDeep              { get; }
        string      SubjectContainsTextXName            { get; }
        string      SubjectContainsTextXNameDeep        { get; }
        string      ResourceContainsTextXName           { get; }
        string      ResourceContainsTextXNameDeep       { get; }

        string      InTheCategoryXName                  { get; }
        string      InTheCategoryAndSubcategoriesXName  { get; }
        string      InTheCategoryAndSubcategoriesXNameDeep { get; }
        string      InTheCategoryXNameDeep              { get; }
        string      SenderIsInTheCategoryXName          { get; }
        string      SenderIsInTheCategoryXNameDeep      { get; }
        string      ReceivedInTheTimeSpanXName          { get; }
        string      ReceivedInTheTimeSpanXNameDeep      { get; }
        string      DeletedInTheTimeSpanXName           { get; }
        string      DeletedInTheTimeSpanXNameDeep       { get; }
        string      ReceivedAheadOfTodayName            { get; }
        string      ReceivedAheadOfTodayNameDeep        { get; }
        string      MessageHasReplyName                 { get; }
        string      MessageHasReplyDeep                 { get; }
        string      MessageIsAReplyName                 { get; }
        string      MessageIsAReplyDeep                 { get; }

        string      DeleteResourceActionName            { get; }
        string      DeleteResourceActionNameDeep        { get; }
        string      DeleteResourcePermActionName        { get; }
        string      DeleteResourcePermActionNameDeep    { get; }
        string      MarkResourceAsReadActionName        { get; }
        string      MarkResourceAsReadActionNameDeep    { get; }
        string      MarkResourceAsUnreadActionName      { get; }
        string      MarkResourceAsUnreadActionNameDeep  { get; }
        string      MarkResourceAsImportantActionName   { get; }
        string      MarkResourceAsImportantActionNameDeep{ get; }
        string      MarkMessageWithFlagActionName       { get; }
        string      MarkMessageWithFlagActionNameDeep   { get; }
        string      ShowDesktopAlertActionName          { get; }
        string      ShowDesktopAlertActionNameDeep      { get; }
        string      ShowAsPlainTextActionName           { get; }
        string      ShowAsPlainTextActionNameDeep       { get; }
        string      AssignCategoryActionName            { get; }
        string      AssignCategoryActionNameDeep        { get; }
        string      AssignCategoryToAuthorActionName    { get; }
        string      AssignCategoryToAuthorActionNameDeep{ get; }
        string      PlaySoundFromFileActionName         { get; }
        string      PlaySoundFromFileActionNameDeep     { get; }
        string      DisplayMessageBoxActionName         { get; }
        string      DisplayMessageBoxActionNameDeep     { get; }
        string      RunApplicationActionName            { get; }
        string      RunApplicationActionNameDeep        { get; }
    }
}
