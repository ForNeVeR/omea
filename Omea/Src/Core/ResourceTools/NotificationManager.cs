// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.FiltersManagement;
using JetBrains.Omea.OpenAPI;
using JetBrains.DataStructures;
using System.Collections;

namespace JetBrains.Omea.ResourceTools
{
    public class NotificationManager: INotificationManager
	{
        internal class NotifyMeCondition
        {
            public IResource ConditionTemplate;
            public int LinkPropID;

            public NotifyMeCondition( IResource conditionTemplate, int linkPropID )
            {
                ConditionTemplate = conditionTemplate;
                LinkPropID = linkPropID;
            }
        }

        private readonly HashMap _notifyMeConditions = new HashMap();
        private readonly HashMap _ruleResourceTypes = new HashMap();

	    /// <summary>
	    /// Registers a resource type for which the "Notify Me" feature can be used.
	    /// </summary>
	    /// <param name="resType">Type of the resources for which "Notify Me" is invoked.</param>
	    /// <param name="ruleResType">
	    ///   Type of the resources processed by "Notify Me" rules for the resource type.
	    /// </param>
	    public void RegisterNotifyMeResourceType( string resType, string ruleResType )
	    {
            _ruleResourceTypes [resType] = ruleResType;
	    }

	    public void RegisterNotifyMeCondition( string resType, IResource conditionTemplate, int linkPropID )
	    {
            #region Preconditions
            if ( resType == null )
                throw new ArgumentNullException( "resType", "NotificationManager -- Registered resource type is null." );

            if ( conditionTemplate == null )
                throw new ArgumentNullException( "conditionTemplate", "NotificationManager -- Registered condition template is null." );

            if( conditionTemplate.Type != FilterManagerProps.ConditionTemplateResName )
                throw new ArgumentException( "NotificationManager -- Condition template has inproper type [" + conditionTemplate.Type + "]" );
            #endregion Preconditions

            ArrayList conditions = (ArrayList) _notifyMeConditions [resType];
            if ( conditions == null )
            {
                conditions = new ArrayList();
                _notifyMeConditions [resType] = conditions;
            }
            conditions.Add( new NotifyMeCondition( conditionTemplate, linkPropID ) );
	    }

	    public IResource[] GetNotifyMeConditions( string resType )
	    {
            ArrayList conditions = (ArrayList) _notifyMeConditions [resType];
            if ( conditions == null )
            {
                return new IResource[] {};
            }

            IResource[] result = new IResource[ conditions.Count ];
            for( int i=0; i<conditions.Count; i++ )
            {
                result [i] = ((NotifyMeCondition) conditions [i]).ConditionTemplate;
            }
            return result;
	    }

        public int GetConditionLinkType( string resType, IResource conditionTemplate )
        {
            ArrayList conditions = (ArrayList) _notifyMeConditions [resType];
            if ( conditions == null )
            {
                throw new ArgumentException( "No conditions found for resource type", "resType" );
            }

            foreach( NotifyMeCondition condition in conditions )
            {
                if ( condition.ConditionTemplate == conditionTemplate )
                {
                    return condition.LinkPropID;
                }
            }

            throw new ArgumentException( "Condition " + conditionTemplate.DisplayName +
                " is not registered for resource type " + resType, "conditionTemplate" );
        }

	    /// <summary>
	    /// Returns the resource type for which the rules are created, given the resource type
	    /// for which the dialog is invoked.
	    /// </summary>
	    /// <param name="resType">The resource type for which the dialog is invoked.</param>
	    /// <returns>The resource type for which the rules are created.</returns>
	    public string GetRuleResourceType( string resType )
	    {
            if ( _ruleResourceTypes.Contains( resType ) )
            {
                return (string) _ruleResourceTypes [resType];
            }
            return resType;
	    }
	}
}
