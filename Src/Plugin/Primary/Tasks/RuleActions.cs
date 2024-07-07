// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

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
