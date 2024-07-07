// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;

namespace JetBrains.JetListViewLibrary
{
	/// <summary>
	/// Implements a mapping between item objects and JetListViewNodes.
	/// </summary>
	internal class JetListViewNodeMap
	{
        private HashMap _nodeMap = new HashMap();

        internal void Add( object item, JetListViewNode node )
        {
            Guard.NullArgument( node, "node" );
            object oldItem = _nodeMap [item];
            if ( oldItem == null )
            {
                _nodeMap [item] = node;
            }
            else if ( oldItem is JetListViewNode )
            {
                _nodeMap [item] = new JetListViewNode[] { oldItem as JetListViewNode, node };
            }
            else
            {
                JetListViewNode[] oldNodes = (JetListViewNode[]) oldItem;
                JetListViewNode[] nodes = new JetListViewNode[ oldNodes.Length+1 ];
                Array.Copy( oldNodes, nodes, oldNodes.Length );
                nodes [oldNodes.Length] = node;
                _nodeMap [item] = nodes;
            }
        }

        internal bool Contains( object item )
        {
            return _nodeMap.Contains( item );
        }

        internal JetListViewNode NodeFromItem( object item )
        {
            object nodeItem = _nodeMap [item];
            if ( nodeItem == null )
                return null;

            if ( nodeItem is JetListViewNode )
                return (JetListViewNode) nodeItem;

            JetListViewNode[] nodes = (JetListViewNode[]) nodeItem;
            return nodes [0];
        }

        internal JetListViewNode NodeFromItem( object item, JetListViewNode parentNode )
        {
            object nodeItem = _nodeMap [item];
            if ( nodeItem == null )
                return null;

            if ( nodeItem is JetListViewNode )
                return (JetListViewNode) nodeItem;

            JetListViewNode[] nodes = (JetListViewNode[]) nodeItem;
            for( int i=0; i<nodes.Length; i++ )
            {
                if ( nodes [i].Parent == parentNode )
                    return nodes [i];
            }
            return nodes [0];
        }

        public JetListViewNode[] NodesFromItem( object item )
        {
            object nodeItem = _nodeMap [item];
            if ( nodeItem == null )
                return new JetListViewNode[] {};

            if ( nodeItem is JetListViewNode )
                return new JetListViewNode[] { (JetListViewNode) nodeItem };

            return (JetListViewNode[]) nodeItem;
        }

        internal void Remove( object item, JetListViewNode parentNode )
        {
            object nodeItem = _nodeMap [item];
            if ( nodeItem != null )
            {
                if ( nodeItem is JetListViewNode )
                {
                    _nodeMap.Remove( item );
                }
                else
                {
                    JetListViewNode[] nodes = (JetListViewNode[]) nodeItem;
                    ArrayList newNodes = ArrayListPool.Alloc();
                    try
                    {
                        for( int src=0; src<nodes.Length; src++ )
                        {
                            if ( nodes [src].Parent != parentNode )
                            {
                                newNodes.Add( nodes [src] );
                            }
                        }
                        if ( newNodes.Count == 1 )
                        {
                            _nodeMap [item] = newNodes [0];
                        }
                        else
                        {
                            _nodeMap [item] = (JetListViewNode[]) newNodes.ToArray( typeof(JetListViewNode) );
                        }
                    }
                    finally
                    {
                        ArrayListPool.Dispose( newNodes );
                    }
                }
            }
        }

	    internal void Clear()
	    {
	        _nodeMap.Clear();
	    }
	}
}
