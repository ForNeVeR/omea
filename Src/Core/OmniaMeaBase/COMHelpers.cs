// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Runtime.InteropServices;
using JetBrains.Omea.Diagnostics;

namespace JetBrains.Omea.COM
{
    public class COM_Error
    {
        public static bool Compare(  COMException exception, uint const_error_code )
        {
            return (uint)exception.ErrorCode == const_error_code;
        }
        public static bool IsRPC_ServerIsUnavailable( COMException exception )
        {
            return Compare( exception, 0x800706BA );
        }
        public static bool RemoteProcCallFailed( COMException exception )
        {
            return Compare( exception, 0x800706BE );
        }
        public static bool CouldNotOpenMacroStorage( COMException exception )
        {
            return Compare( exception, 0x800A175D );
        }
    }

    public class _com_object
    {
        public static void Release( object com_Object )
        {
            if ( com_Object == null ) return;
            try
            {
                int count = Marshal.ReleaseComObject( com_Object );
                while ( true )
                {
                    int old_count = count;
                    count = Marshal.ReleaseComObject( com_Object );
                    if ( count <= 0 || count == old_count ) break;
                }
            }
            catch ( Exception exception )
            {
                Tracer._Trace("Exception when trying to release COM object" );
                Tracer._TraceException( exception );
            }
        }
    }

    public class COM_Object
    {
        private object _com_Object;
        public COM_Object( object com_Object )
        {
            _com_Object = com_Object;
        }
        public void Release( )
        {
            Release( _com_Object );
        }
        public object COM_Pointer
        {
            get { return _com_Object; }
        }
        public static void Release( object comObject )
        {
            _com_object.Release( comObject );
        }
        public static void ReleaseIfNotNull( Object_Ref_Counting objRefCounting )
        {
            if ( objRefCounting != null )
            {
                objRefCounting.Release();
            }
        }
    }

    public class Object_Ref_Counting
    {
        private int _count = 0;
        //private System.Diagnostics.StackTrace _callStack = new System.Diagnostics.StackTrace();
        //private ArrayList _callStacks = new ArrayList();
        //private ArrayList _callStacksRelease = new ArrayList();
        //private string _strCallStack;

        public Object_Ref_Counting()
        {
            //_strCallStack = _callStack.ToString();
            AddRef();
        }
        public Object_Ref_Counting AddRef()
        {
            //_callStacks.Add( new System.Diagnostics.StackTrace().ToString() );
            System.Threading.Interlocked.Increment( ref _count );
            return this;
        }
        public void Release()
        {
            //_callStacksRelease.Add( new System.Diagnostics.StackTrace().ToString() );
            if ( System.Threading.Interlocked.Decrement( ref _count ) == 0 )
            {
                ReleaseObject();
            }
        }
        protected virtual void ReleaseObject()
        {
        }
        ~Object_Ref_Counting()
        {
            if ( _count != 0 )
            {
/*
                Tracer._Trace( "COM_WRAPPER_____________________________________________" );
                Tracer._Trace( "COM_WRAPPER error in releasing Count " + _count.ToString() );
                Tracer._Trace( "COM_WRAPPER call stack : " + _strCallStack );

                foreach ( string callStack in  _callStacks )
                {
                    Tracer._Trace( "COM_WRAPPER_____________________________________________" );
                    Tracer._Trace( "COM_WRAPPER addref call stack : " + callStack );
                }
                foreach ( string callStack in  _callStacksRelease )
                {
                    Tracer._Trace( "COM_WRAPPER_____________________________________________" );
                    Tracer._Trace( "COM_WRAPPER release call stack : " + callStack );
                }

                Tracer._Trace( "COM_WRAPPER_____________________________________________" );
                */
                ReleaseObject();

            }
            else
            {
                //Tracer._Trace( "COM_WRAPPER released OK" );
            }
        }
    }

    public class COM_Object_Ref_Counting : Object_Ref_Counting
    {
        private object _com_Object;

        public COM_Object_Ref_Counting( object com_Object )
        {
            _com_Object = com_Object;
        }
        protected override void ReleaseObject()
        {
            _com_object.Release( _com_Object );
        }
        public object COM_Pointer
        {
            get { return _com_Object; }
        }
    }

}
