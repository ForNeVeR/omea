/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Tasks
{
    internal class PriorityColumn : ImageListColumn
    {
        internal PriorityColumn() : base( TasksPlugin._propPriority )
        {
            AddIconValue( TasksPlugin.LoadIconFromAssembly( "PriorityHigh.ico" ), 1 );
            AddIconValue( TasksPlugin.LoadIconFromAssembly( "PriorityLow.ico" ), 2 );
            SetHeaderIcon( TasksPlugin.LoadIconFromAssembly( "PriorityHeader.ico" ) );
        }
        public override void MouseClicked( IResource res, Point pt )
        {
            switch( res.GetIntProp( TasksPlugin._propPriority ) )
            {
                case 0: new ResourceProxy( res ).SetProp( TasksPlugin._propPriority, 1 ); break;
                case 1: new ResourceProxy( res ).SetProp( TasksPlugin._propPriority, 2 ); break;
                case 2: new ResourceProxy( res ).SetProp( TasksPlugin._propPriority, 0 ); break;
            }
        }
    }

    internal class StatusColumn : ImageListColumn
    {
        internal StatusColumn() : base( TasksPlugin._propStatus )
        {
            AddIconValue( TasksPlugin.LoadIconFromAssembly( "in_progress.ico" ), 1 );
            AddIconValue( TasksPlugin.LoadIconFromAssembly( "completed.ico" ), 2 );
            AddIconValue( TasksPlugin.LoadIconFromAssembly( "waiting.ico" ), 3 );
            AddIconValue( TasksPlugin.LoadIconFromAssembly( "deferred.ico" ), 4 );
            SetHeaderIcon( TasksPlugin.LoadIconFromAssembly( "StatusHeader.ico" ) );
        }
        public override void MouseClicked( IResource res, Point pt )
        {
            if( res.GetLinksTo( null, TasksPlugin._linkSuperTask ).Count == 0 )
            {
                int newStatus = 0;
                ResourceProxy proxy = new ResourceProxy( res );
                proxy.BeginUpdate();

                switch( res.GetIntProp( TasksPlugin._propStatus ) )
                {
                    case 0: newStatus = 1; break;
                    case 1: newStatus = 2; break;
                    case 2: newStatus = 0; break;
                    case 3: newStatus = 1; break;
                    case 4: newStatus = 1; break;
                }

                proxy.SetProp( TasksPlugin._propStatus, newStatus );
                if( newStatus == 2 )
                    proxy.SetProp( TasksPlugin._propCompletedDate, DateTime.Now );
                else
                    proxy.DeleteProp( TasksPlugin._propCompletedDate );

                proxy.EndUpdate();
            }
        }
    }
}
