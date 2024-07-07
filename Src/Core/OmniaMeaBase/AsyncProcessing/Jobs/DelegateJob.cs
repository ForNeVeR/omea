// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Reflection;

using System35;

using JetBrains.Annotations;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.AsyncProcessing
{
    public class DelegateJob: AbstractNamedJob
    {
        private Delegate _method;
        private object[] _args;
        private object _retVal;
        private int _hashCode;
        private static object[] _emptyArgs = new object[] {};

    	[NotNull]
    	private string _name = "";

    	public DelegateJob( Delegate method, object[] args )
        {
            _method = method;
            _args = args ?? _emptyArgs;
    		_hashCode = ComputeHashCode();
        }

        public DelegateJob( string name, Delegate method, object[] args )
        {
            _name = name;
            _method = method;
            _args = args ?? _emptyArgs;
        	_hashCode = ComputeHashCode();
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public override bool Equals(object obj)
        {
            DelegateJob job = obj as DelegateJob;
            if( job == null )
            {
                return false;
            }
            bool result = _args.Length == job._args.Length && _method.Equals( job._method );
            if( result )
            {
                for( int i = 0; i < _args.Length; ++i )
                {
                    result = Equals( _args[ i ], job._args[ i ] );
                    if ( !result )
                    {
                        break;
                    }
                }
            }
            return result;
        }

        protected override void Execute()
        {
            try
            {
                _retVal = _method.DynamicInvoke( _args );
            }
            catch( TargetParameterCountException )
            {
                throw new TargetParameterCountException( string.Format("Parameter count mismatch when invoking method “{0}::{1}”.", _method.Method.DeclaringType.FullName, _method.Method.Name) );
            }
        }

    	[NotNull]
    	public override string Name
    	{
    		get
    		{
    			return _name;
    		}
    	}

    	[Obsolete("Avoid changing the job name. Currently, it's done in the text index only.")]
    	public void Rename([NotNull] string name)
    	{
    		if(name == null)
    			throw new ArgumentNullException("name");
    		_name = name;
    	}

    	public object ReturnValue
        {
            get { return _retVal; }
        }

        public Delegate Method
        {
            get { return _method; }
        }

        private int ComputeHashCode()
        {
            int result =  _method.GetHashCode();
            object target = _method.Target;
            if( target != null )
            {
                result += target.GetHashCode();
            }
            foreach( object obj in _args )
            {
                if ( obj != null )
                {
                    result += obj.GetHashCode();
                }
            }
            return result;
        }
    }
}
