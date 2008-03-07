/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Tasks
{
    internal class CreateTaskRuleAction : IRuleAction
    {
        public void Exec( IResource resource, IActionParameterStore actionStore )
        {
            if ( resource != null )
            {
                IResource task = NewTaskAction.CreateTask( resource.ToResourceList(), null );
                task.EndUpdate();
            }
        }
    }

    internal class AttachToTasksRuleAction : IRuleAction
    {
        public void Exec( IResource resource, IActionParameterStore actionStore )
        {
            if ( resource != null )
            {
                foreach( IResource task in actionStore.ParametersAsResList() )
                {
                    resource.AddLink( TasksPlugin._linkTarget, task );
                }
            }
        }
    }
}