// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma unmanaged
#ifdef EMAPI_MANAGED
#pragma managed
#endif

template< typename T >
RCPtr<T>::RCPtr( T* realPtr = NULL ) : pointee( realPtr )
{
    init();
}
template< typename T >
RCPtr<T>::RCPtr( const RCPtr<T>& rhs ) : pointee( rhs.pointee )
{
    init();
}

template< typename T >
RCPtr<T>::~RCPtr()
{
    try
    {
        if ( pointee != 0 )
        {
            pointee->removeRef();
        }
    }
    catch(...)//trap all exceptions in destructor
    {}
}
template< typename T >
RCPtr<T>& RCPtr<T>::operator=( const RCPtr<T>& rhs )
{
    if ( pointee != rhs.pointee )
    {
        T* oldPointee = pointee;
        pointee = rhs.pointee;
        init();
        if ( oldPointee != 0 )
        {
            oldPointee->removeRef();
        }
    }
    return *this;
}
template< typename T >
T* RCPtr<T>::operator->() const
{
    return pointee;
}
template< typename T >
T* RCPtr<T>::get() const
{
    return pointee;
}
template< typename T >
T& RCPtr<T>::operator*() const
{
    return *pointee;
}
template< typename T >
bool RCPtr<T>::IsNull() const
{
    return ( pointee == 0 );
}
template< typename T >
void RCPtr<T>::release()
{
    if ( pointee != 0 )
    {
        if ( pointee->removeRef() == 0 )
        {
            pointee = 0;
        }
    }
}
template< typename T >
int RCPtr<T>::GetRefCount() const
{
    if ( pointee != 0 )
    {
        return pointee->GetRefCount();
    }
    return 0;
}
template< typename T >
void RCPtr<T>::init()
{
    if ( pointee == 0 ) return;
    pointee->addRef();
}
template< typename T >
RCPtr<T>* RCPtr<T>::CloneOnHeap() const
{
    return new RCPtr<T>( *this );
}
