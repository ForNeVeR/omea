// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.AsyncProcessing;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Base
{
    public class Guard
    {
        public static bool IsResourceLive( IResource resource )
        {
            if ( resource.IsDeleting )
            {
                return false;
            }
            return true;
        }
        public static void NullArgument( object parameter, string parameterName )
        {
            if ( parameter == null )
            {
                throw new ArgumentNullException( parameterName );
            }
        }
        public static void NullMember( object member, string memberName )
        {
            if ( member == null )
            {
                string message = "Member '" + memberName + "' must not be null at this context";
                throw new NullReferenceException( message );
            }
        }
        public static void NullLocalVariable( object variable, string variableName )
        {
            if ( variable == null )
            {
                string message = "Local variable '" + variableName + "' must not be null at this context";
                throw new NullReferenceException( message );
            }
        }
        public static void EmptyStringArgument( string parameter, string parameterName )
        {
            if ( String.IsNullOrEmpty( parameter ) )
            {
                throw new ArgumentException( "'" + parameterName + "' parameter must be not null and not empty" );
            }
        }
        public static void QueryIndexingWithCheckId( IResource resource )
        {
            NullArgument( resource, "resource" );
            if ( resource.Id == -1 )
            {
                throw new InvalidResourceIdException( resource.Id, "Trying to index resource with id == -1" );
            }
            if ( Core.TextIndexManager != null )
            {
                Core.TextIndexManager.QueryIndexing( resource.Id );
            }
        }

        public static void ValidResourceType( string resType, string paramName )
        {
            NullArgument( resType, paramName );
            if ( !Core.ResourceStore.ResourceTypes.Exist( resType ) )
            {
                throw new ArgumentException( "The resource type '" + resType + "' does not exist", paramName );
            }
        }

        public static void OwnerThread( IAsyncProcessor asyncProcessor )
        {
            if ( !asyncProcessor.IsOwnerThread )
            {
                throw new InvalidOperationException( "The method must be called from the " + ((AsyncProcessor) asyncProcessor).ThreadName + " thread" );
            }
        }
    }
}
