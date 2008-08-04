/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Diagnostics;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.FiltersManagement
{
	public class FilterConvertors
	{
        private static readonly FilterRegistry fMgr = Core.FilterRegistry as FilterRegistry;

        public static IResource InstantiateTemplate( IResource template, object param, string[] resTypes )
        {
            return InstantiateTemplate( template, param, null, resTypes );
        }
        public static IResource InstantiateTemplate( IResource template, object param,
                                                     string representation, string[] resTypes )
        {
            #region Preconditions
            if( template.Type != FilterManagerProps.ConditionTemplateResName )
                throw new InvalidOperationException( "Input parameter must be of ConditionTemplate resource type" );
            #endregion Preconditions

            string      propName = template.GetStringProp( "ApplicableToProp" );
            ConditionOp op  = (ConditionOp)template.GetProp( "ConditionOp" );

            //-----------------------------------------------------------------
            IResource   condition;
            if( op == ConditionOp.Eq && ResourceTypeHelper.IsDateProperty( propName ))
            {
                condition = TimeSpan2Condition( (string)param, propName, resTypes );
            }
            else
            if( op == ConditionOp.QueryMatch )
            {
                string  sectionName = null;
                if( template.HasProp( "SectionOrder" ))
                    sectionName = DocSectionHelper.FullNameByOrder( (uint)template.GetIntProp( "SectionOrder" ));
                
                condition = fMgr.CreateQueryConditionAux( resTypes, (string)param, sectionName );
            }
            else
            if( op == ConditionOp.Eq || op == ConditionOp.Has )
            {
                condition = fMgr.CreateStandardConditionAux( resTypes, propName, op, (string)param );
            }
            else
            if( op == ConditionOp.In )
            {
                condition = fMgr.CreateStandardConditionAux( resTypes, propName, op, (IResourceList)param );
            }
            else
            if( op == ConditionOp.InRange )
            {
                condition = IntRange2Condition( (string)param, propName);
            }
            else
                throw new InvalidOperationException( "Not all Operations are supported now" );

            FilterRegistry.ReferCondition2Template( condition, template );

            //-----------------------------------------------------------------
            //  Do not forget to set additional parameters from the template
            //  and representation, e.g. if the template has "custom" style,
            //  propagate it to the aux condition.
            //-----------------------------------------------------------------
            ResourceProxy proxy = new ResourceProxy( condition );
            proxy.BeginUpdate();

            if( template.GetStringProp( "ConditionType" ) == "custom" )
                proxy.SetProp( "ConditionType", "custom" );

            if( !String.IsNullOrEmpty( representation ) )
                proxy.SetProp( "SurfaceConditionVal", representation );
            proxy.EndUpdate();

            return( condition );
        }

	    public static IResource Template2Action( IResource template, object param, string representation )
        {
            if( template.Type != FilterManagerProps.RuleActionTemplateResName )
                throw new ArgumentException( "FilterRegistry -- Invalid type of parameter - RuleActionTemplate is expected" );

            IResource   action;
            ConditionOp op = (ConditionOp)template.GetProp( "ConditionOp" );

            if( op == ConditionOp.In )
            {
                if( param is IResourceList )
                    action = FilterRegistry.RegisterRuleActionProxy( template, (IResourceList)param );
                else
                if( param is string )
                    action = FilterRegistry.RegisterRuleActionProxy( template, (string)param );
                else
                    throw new ArgumentException( "Illegal parameter type for the operation - string or Resource List expected" );
            }
            else
            if( op == ConditionOp.Eq )
            {
                if( param is string )
                    action = FilterRegistry.RegisterRuleActionProxy( template, (string)param );
                else
                    throw new ArgumentException( "Illegal parameter type for the operation - string expected" );
            }
            else
                throw new InvalidOperationException( "Not all Operations are supported now" );

            if( !String.IsNullOrEmpty( representation ) )
                new ResourceProxy( action ).SetProp( "SurfaceConditionVal", representation );
            return( action );
        }

        /// <summary>
        /// Converts string representation of date, absolute date range or date
        /// range relative to fixed anchors (today, this week, etc) to proper
        /// filter condition.
        /// </summary>
        /// <returns>
        /// Standard condition if time span description is fixed anchor,
        /// newly created auxiliary condition otherwise.
        /// </returns>
        private static IResource TimeSpan2Condition( string timeDesc, string propName, string[] resTypes )
        {
            Debug.Assert( timeDesc != null );

            IResource   condition;
            if( timeDesc.ToLower() == "tomorrow" )
                condition = fMgr.CreateStandardConditionAux( resTypes, propName, ConditionOp.InRange, "Tomorrow", "+1" );
            else
            if( timeDesc.ToLower() == "today" )
                condition = fMgr.CreateStandardConditionAux( resTypes, propName, ConditionOp.InRange, "Today", "+1" );
            else
            if( timeDesc.ToLower() == "yesterday" )
                condition = fMgr.CreateStandardConditionAux( resTypes, propName, ConditionOp.InRange, "Yesterday", "+1" );
            else
            if( timeDesc.ToLower() == "this week" )
                condition = fMgr.CreateStandardConditionAux( resTypes, propName, ConditionOp.InRange, "WeekStart", "+7" );
            else
            if( timeDesc.ToLower() == "last week" )
                condition = fMgr.CreateStandardConditionAux( resTypes, propName, ConditionOp.InRange, "WeekStart", "-7" );
            else
            if( timeDesc.ToLower() == "next week" )
                condition = fMgr.CreateStandardConditionAux( resTypes, propName, ConditionOp.InRange, "NextWeekStart", "+7" );
            else
            if( timeDesc.ToLower() == "this month" )
                condition = fMgr.CreateStandardConditionAux( resTypes, propName, ConditionOp.InRange, "MonthStart", "+30" );
            else
            if( timeDesc.ToLower() == "last month" )
                condition = fMgr.CreateStandardConditionAux( resTypes, propName, ConditionOp.InRange, "MonthStart", "-30" );
            else
            if( timeDesc.ToLower().StartsWith( "last " ) )
            {
                string[]    fields = timeDesc.ToLower().Split( ' ' );
                string      interval = "-" + fields[ 1 ];
                if( fields[ 2 ] != "days" )
                    interval += fields[ 2 ][ 0 ]; // append first letter of the unit

                condition = fMgr.CreateStandardConditionAux( resTypes, propName, ConditionOp.InRange, "Tomorrow", interval );
            }
            else
            {
                string  beforePart = null, afterPart = null;
                int     delimiter = timeDesc.IndexOf( " and " );
                if( timeDesc.ToLower().StartsWith( "before " ))
                {
                    beforePart = timeDesc.Substring( 7 );
                    if( delimiter != -1 )
                    {
                        beforePart = beforePart.Substring( 0, delimiter - 7 );
                        timeDesc = timeDesc.Substring( delimiter + 5 );
                    }
                }

                if( timeDesc.ToLower().StartsWith( "after " ))
                    afterPart = timeDesc.Substring( 6 );

                if( beforePart != null && afterPart != null )
                    condition = fMgr.CreateStandardConditionAux( resTypes, propName, ConditionOp.InRange, beforePart, afterPart );
                else
                if( beforePart != null )
                    condition = fMgr.CreateStandardConditionAux( resTypes, propName, ConditionOp.Lt, beforePart );
                else
                if( afterPart != null )
                    condition = fMgr.CreateStandardConditionAux( resTypes, propName, ConditionOp.Gt, afterPart );
                else
                    throw new ArgumentException( "FilterRegistry -- Illegal format of Date interval for instantiation." );
            }
            return condition;
        }

        /// <summary>
        /// Converts string representation of the integer interval into the
        /// proper filter condition.
        /// </summary>
        private static IResource IntRange2Condition( string interval, string propName )
        {
            Debug.Assert( interval != null );
            Debug.Assert( propName != null );

            int     partsDelimiter = interval.IndexOf( " and " );
            string  largerPart = "", smallerPart = "";
            IResource condition;

            //-------------------------------------------------------------
            if( interval.StartsWith( "larger than " ))
            {
                Trace.WriteLine( "IntIntervalForm -- Editing Larger Than" );
                largerPart = interval.Substring( 12 );
                if( partsDelimiter != -1 )
                {
                    largerPart = largerPart.Substring( 0, partsDelimiter - 12 );
                    interval = interval.Substring( partsDelimiter + 5 );
                }
            }
            if( interval.StartsWith( "smaller than " ))
                smallerPart = interval.Substring( 13 );

             Trace.WriteLine( "IntIntervalForm -- Converting" );
            //-------------------------------------------------------------
            if( largerPart != "" && smallerPart != "" )
                condition = fMgr.CreateStandardConditionAux( null, propName, ConditionOp.InRange, largerPart, smallerPart );
            else
            if( largerPart != "" )
                condition = fMgr.CreateStandardConditionAux( null, propName, ConditionOp.Gt, largerPart );
            else
            if( smallerPart != "" )
                condition = fMgr.CreateStandardConditionAux( null, propName, ConditionOp.Lt, smallerPart );
            else
                throw( new InvalidOperationException( "Can not recognize interval format" ) );
            return condition;
        }
	}
}
