/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Reflection;
using System.Threading;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.AsyncProcessing
{
    public interface ICancelable
    {
        void OnCancel();
    }

    public class WaitForSingleObjectJob : AbstractJob
    {
        public WaitForSingleObjectJob( WaitHandle handle )
        {
            _handle = handle;
        }
        protected override void Execute()
        {
            if( _handle != null )
            {
                InvokeAfterWait( NextMethod, _handle );
                _handle = null;
            }
        }
        private WaitHandle _handle;
    }

    public class DelegateJob: SimpleJob
    {
        private Delegate _method;
        private object[] _args;
        private object _retVal;
        private int _hashCode;
        private static object[] _emptyArgs = new object[] {};
        
        public DelegateJob( Delegate method, object[] args )
        {
            _method = method;
            _args = args;
            if ( _args == null )
            {
                _args = _emptyArgs;
            }
            _hashCode = ComputeHashCode();
        }

        public DelegateJob( string name, Delegate method, object[] args )
        {
            Name = name;
            _method = method;
            _args = args;
            if ( _args == null )
            {
                _args = _emptyArgs;
            }
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
                    result = Object.Equals( _args[ i ], job._args[ i ] );
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
                throw new TargetParameterCountException( "Parameter count mismatch when invoking method " +
                    _method.Method.DeclaringType + "." + _method.Method.Name );
            }
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

    internal class DelegateJobFilter
    {
        private Delegate _method;

        internal DelegateJobFilter( Delegate method )
        {
            _method = method;
        }

        public bool DoFilter( AbstractJob job )
        {
            return job is DelegateJob && ((DelegateJob) job).Method.Equals( _method );
        }
    }
}