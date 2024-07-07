// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only


// EMAPILib.h

#pragma once

#include "helpers.h"

#using <mscorlib.dll>
using namespace System;
using namespace System::Text;
using namespace System::Collections;

class MAPISession;

namespace EMAPILib
{
	public ref class EMAPISession
	{
        private:
            MAPISession* _pMAPISession; //MAPI Session Pointer
            static IQuoting^ _quoter;
            static ILibManager^ _libManager;
        public:
            bool CanClose();
            void CheckDependencies();
            static void DeleteMessage( const ESPropValueSPtr& entryID );
            static void MoveMessage( const ESPropValueSPtr& entryID, const ESPropValueSPtr& folderID );
            static void CopyMessage( const ESPropValueSPtr& entryID, const ESPropValueSPtr& folderID );
            void AddRecipients( System::Object^ mapiObject, ArrayList^ recipients );
            static int RegisterForm();
            static void UnregisterForm( int formID );
            static void BeginReadProp( int prop_id );
            static void EndReadProp();
            EMAPISession( int fake );
            bool Initialize( bool pickLogonProfile, ILibManager^ libManager );
            void SetQuoter( IQuoting^ quoter );
            static IQuoting^ GetQuoter();

            EMAPILib::IEMessage^ LoadFromMSG( String^ path );
            void Uninitialize();
			bool CompareEntryIDs( String^ entryID1, String ^entryID2 );
            ~EMAPISession();
            EMAPILib::IEAddrBook^ OpenAddrBook();
            EMAPILib::IEMsgStores^ GetMsgStores();
            EMAPILib::IEMsgStore^ OpenMsgStore( String^ entryID );

			// heap diagnostics
			static int ObjectsCount();
			static int HeapSize();
	};
}

