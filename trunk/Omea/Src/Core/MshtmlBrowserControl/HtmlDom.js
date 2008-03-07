/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

// JetBrains Omea Mshtml Browser Component
//
// Implements some fragments of the HTML DOM so that it could be used from the managed applications.
//
// This file belongs to the managed part and implements the MshtmlBrowserNest class.
// This file inherits the AbstractWebBrowser class and transparently delegates all the calls to the WebBrowserControl class that implements the IWebBrowser interface. The WebBrowserControl cannot be inherited from AbstractWebBrowser directly because it already extends the AxHost class. This class must not contain any meaningful processing but should transparently relay the calls instead.
//
// © JetBrains Inc, 2004
// Written by (H) Serge Baltic
//
import System;
import System.Collections;
import System.ComponentModel;
import System.Drawing;
import System.Data;
import System.Windows.Forms;
import JetBrains.Omea.OpenAPI;
import System.Diagnostics;
import JetBrains.DataStructures;

package JetBrains.Omea.GUIControls.MshtmlBrowser
{
	/// Class that represents a managed interface for the HTML document object.
	public class MshtmlDocument extends HtmlDomObject implements IHtmlDomDocument
	{
		var _instance;
		internal function MshtmlDocument(instance)
		{
			super(instance);
			_instance = instance;
		}
		
		// Creates a new wrapper for the object, or returns null if it is null.
		public static function Attach(instance) : IHtmlDomDocument
		{
			return (instance != null) && (instance != System.DBNull) ? (new JetBrains.Omea.GUIControls.MshtmlBrowser.MshtmlDocument(instance)) : null;
		}
		
		public function GetElementById(id : System.String) : IHtmlDomElement
		{
			return MshtmlElement.Attach(_instance.getElementById(id));
		}
		
		public function get Body() : IHtmlDomElement
		{
			return MshtmlElement.Attach(_instance.body);
		}
		
		public function CreateElement(tagName : System.String) : IHtmlDomElement
		{
			return MshtmlElement.Attach(_instance.createElement(tagName));
		}
	}
	
	/// Class that represents a managed interface for any HTML element.
	public class MshtmlElement extends HtmlDomObject implements IHtmlDomElement
	{
		var _instance;
		public function MshtmlElement(instance)
		{
			super(instance);
			_instance = instance;
		}		
		
		// Creates a new wrapper for the object, or returns null if it is null.
		public static function Attach(instance) : IHtmlDomElement
		{
			return (instance != null) && (instance != System.DBNull) ? (new JetBrains.Omea.GUIControls.MshtmlBrowser.MshtmlElement(instance)) : null;
		}
		
		public function get Id() : System.String
		{
			var sRet : System.String = _instance.id != null ? _instance.id : null;	// Don't return the "undefined" string for null
			return sRet;
		}
		public function set Id(value : System.String)
		{
			_instance.id = value;
		}		
		
		public function get Name() : System.String
		{
			var text = _instance.name;
			return text != null ? text : "";
		}
		public function set Name(value : System.String)
		{
			_instance.name = value;
		}

		public function get InnerHtml() : System.String
		{
			var text = _instance.innerHTML;
			return text != null ? text : "";
		}		
		public function set InnerHtml(value : System.String)
		{
			_instance.innerHTML = value;
		}
		
		public function get OuterHtml() : System.String
		{
			var text = _instance.outerHTML;
			return text != null ? text : "";
		}
		
		public function get InnerText() : System.String
		{
			var text = _instance.innerText;
			return text != null ? text : "";
		}		
		public function set InnerText(value : System.String)
		{
			_instance.innerText = value;
		}
		
		public function get ClassName() : System.String
		{
			var text = _instance.className;
			return text != null ? text : "";			
		}
		public function set ClassName(value : System.String)
		{
			_instance.className = value;
		}
		
		public function get TagName() : System.String
		{
			var text = _instance.tagName;
			return text != null ? text : "";			
		}
		public function set TagName(value : System.String)
		{
			_instance.tagName = value;
		}
		
		public function ScrollIntoView(bAlignToTop : boolean)
		{
			_instance.scrollIntoView(bAlignToTop);
		}
		
		public function get OffsetTop() : int
		{
			return _instance.offsetTop;
		}

		public function get OffsetLeft() : int
		{
			return _instance.offsetLeft;
		}

		public function get OffsetHeight() : int
		{
			return _instance.offsetHeight;
		}

		public function get OffsetWidth() : int
		{
			return _instance.offsetWidth;
		}

		public function get ScrollLeft() : int
		{
			return _instance.scrollLeft;
		}
		public function set ScrollLeft(value : int)
		{
			_instance.scrollLeft = value;
		}

		public function get ScrollTop() : int
		{
			return _instance.scrollTop;
		}
		public function set ScrollTop(value : int)
		{
			_instance.scrollTop = value;
		}

		public function get ScrollWidth() : int
		{
			return _instance.scrollWidth;
		}

		public function get ScrollHeight() : int
		{
			return _instance.scrollHeight;
		}
		
		public function get ClientLeft() : int
		{
			return _instance.clientLeft;
		}

		public function get ClientTop() : int
		{
			return _instance.clientTop;
		}

		public function get ClientWidth() : int
		{
			return _instance.clientWidth;
		}

		public function get ClientHeight() : int
		{
			return _instance.clientHeight;
		}
		
		public function DoScroll(action : ScrollAction) : void
		{
			// Convert to string
			var	sAction : System.String = "";
			switch(action)
			{
			case ScrollAction.Left:
				sAction = "left";
				break;
			case ScrollAction.Right:
				sAction = "right";
				break;
			case ScrollAction.Up:
				sAction = "up";
				break;
			case ScrollAction.Down:
				sAction = "down";
				break;
			case ScrollAction.PageLeft:
				sAction = "pageLeft";
				break;
			case ScrollAction.PageRight:
				sAction = "pageRight";
				break;
			case ScrollAction.PageUp:
				sAction = "pageUp";
				break;
			case ScrollAction.PageDown:
				sAction = "pageDown";
				break;
			default:
				throw new NotImplementedException(String.Format("Unknown scrolling action {0}.", action));
			}
			
			// Apply
			_instance.doScroll(sAction);
		}
		
		public function get OffsetParent() : IHtmlDomElement
		{
			var ret : System.Object = _instance.offsetParent;
			return ret != null ? new MshtmlElement(ret) : null;			
		}

		public function get ParentElement() : IHtmlDomElement
		{
			var ret : System.Object = _instance.parentElement;
			return ret != null ? new MshtmlElement(ret) : null;			
		}
		
		public function RemoveNode(deep : boolean) : void
		{
			_instance.removeNode(deep);
		}
		
		public function GetAttribute(name : System.String, flags : GetAttributeFlags) : System.Object
		{
			var value : System.Object = _instance.getAttribute(name, flags);
			if(value == DBNull.Value)
			{
				var	value2 : System.String;
				value2 = null;
				return value2;
			}
			return value;	// Convert DBNull to real Null
		}

		public function GetAttribute(name : System.String) : System.Object
		{
			var value : System.Object = _instance.getAttribute(name, 0);
			if(value == DBNull.Value)
			{
				var	value2 : System.String;
				value2 = null;
				return value2;
			}
			return value;	// Convert DBNull to real Null
		}

		public function SetAttribute(name : System.String, value : System.Object, caseSensitive : boolean) : void
		{
			_instance.setAttribute(name, value, caseSensitive ? 1 : 0);
		}		
		
		public function remove_Click( handler : HtmlEventHandler ) : void
		{	throw new NotImplementedException("Cannot unsubscribe from HTML events.");	}	// Cannot unsubscribe currently
		public function add_Click( handler : HtmlEventHandler ) : void
		{	new HtmlEventHandlerProxy(_instance, "onclick", handler);	}
		
		public function remove_DoubleClick( handler : HtmlEventHandler ) : void
		{	throw new NotImplementedException("Cannot unsubscribe from HTML events.");	}	// Cannot unsubscribe currently
		public function add_DoubleClick( handler : HtmlEventHandler ) : void
		{	new HtmlEventHandlerProxy(_instance, "ondblclick", handler);	}
		
		public function remove_MouseEnter( handler : HtmlEventHandler ) : void
		{	throw new NotImplementedException("Cannot unsubscribe from HTML events.");	}	// Cannot unsubscribe currently
		public function add_MouseEnter( handler : HtmlEventHandler ) : void
		{	new HtmlEventHandlerProxy(_instance, "onmouseenter", handler);	}
		
		public function remove_MouseLeave( handler : HtmlEventHandler ) : void
		{	throw new NotImplementedException("Cannot unsubscribe from HTML events.");	}	// Cannot unsubscribe currently
		public function add_MouseLeave( handler : HtmlEventHandler ) : void
		{	new HtmlEventHandlerProxy(_instance, "onmouseleave", handler);	}
		
		public function remove_MouseDown( handler : HtmlEventHandler ) : void
		{	throw new NotImplementedException("Cannot unsubscribe from HTML events.");	}	// Cannot unsubscribe currently
		public function add_MouseDown( handler : HtmlEventHandler ) : void
		{	new HtmlEventHandlerProxy(_instance, "onmousedown", handler);	}

		public function remove_MouseWheel( handler : HtmlEventHandler ) : void
		{	throw new NotImplementedException("Cannot unsubscribe from HTML events.");	}	// Cannot unsubscribe currently
		public function add_MouseWheel( handler : HtmlEventHandler ) : void
		{	new HtmlEventHandlerProxy(_instance, "onmousewheel", handler);	}

		public function remove_MouseMove( handler : HtmlEventHandler ) : void
		{	throw new NotImplementedException("Cannot unsubscribe from HTML events.");	}	// Cannot unsubscribe currently
		public function add_MouseMove( handler : HtmlEventHandler ) : void
		{	new HtmlEventHandlerProxy(_instance, "onmousemove", handler);	}

		public function remove_MouseOut( handler : HtmlEventHandler ) : void
		{	throw new NotImplementedException("Cannot unsubscribe from HTML events.");	}	// Cannot unsubscribe currently
		public function add_MouseOut( handler : HtmlEventHandler ) : void
		{	new HtmlEventHandlerProxy(_instance, "onmouseout", handler);	}

		public function remove_MouseOver( handler : HtmlEventHandler ) : void
		{	throw new NotImplementedException("Cannot unsubscribe from HTML events.");	}	// Cannot unsubscribe currently
		public function add_MouseOver( handler : HtmlEventHandler ) : void
		{	new HtmlEventHandlerProxy(_instance, "onmouseover", handler);	}

		public function remove_MouseUp( handler : HtmlEventHandler ) : void
		{	throw new NotImplementedException("Cannot unsubscribe from HTML events.");	}	// Cannot unsubscribe currently
		public function add_MouseUp( handler : HtmlEventHandler ) : void
		{	new HtmlEventHandlerProxy(_instance, "onmouseup", handler);	}

		public function remove_ContextMenu( handler : HtmlEventHandler ) : void
		{	throw new NotImplementedException("Cannot unsubscribe from HTML events.");	}	// Cannot unsubscribe currently
		public function add_ContextMenu( handler : HtmlEventHandler ) : void
		{	new HtmlEventHandlerProxy(_instance, "oncontextmenu", handler);	}
		
		public function remove_Drag( handler : HtmlEventHandler ) : void
		{	throw new NotImplementedException("Cannot unsubscribe from HTML events.");	}	// Cannot unsubscribe currently
		public function add_Drag( handler : HtmlEventHandler ) : void
		{	new HtmlEventHandlerProxy(_instance, "ondrag", handler);	}

		public function remove_Drop( handler : HtmlEventHandler ) : void
		{	throw new NotImplementedException("Cannot unsubscribe from HTML events.");	}	// Cannot unsubscribe currently
		public function add_Drop( handler : HtmlEventHandler ) : void
		{	new HtmlEventHandlerProxy(_instance, "ondrop", handler);	}

		public function remove_DragEnter( handler : HtmlEventHandler ) : void
		{	throw new NotImplementedException("Cannot unsubscribe from HTML events.");	}	// Cannot unsubscribe currently
		public function add_DragEnter( handler : HtmlEventHandler ) : void
		{	new HtmlEventHandlerProxy(_instance, "ondragenter", handler);	}

		public function remove_DragLeave( handler : HtmlEventHandler ) : void
		{	throw new NotImplementedException("Cannot unsubscribe from HTML events.");	}	// Cannot unsubscribe currently
		public function add_DragLeave( handler : HtmlEventHandler ) : void
		{	new HtmlEventHandlerProxy(_instance, "ondragleave", handler);	}

		public function remove_DragOver( handler : HtmlEventHandler ) : void
		{	throw new NotImplementedException("Cannot unsubscribe from HTML events.");	}	// Cannot unsubscribe currently
		public function add_DragOver( handler : HtmlEventHandler ) : void
		{	new HtmlEventHandlerProxy(_instance, "ondragover", handler);	}

		public function remove_DragStart( handler : HtmlEventHandler ) : void
		{	throw new NotImplementedException("Cannot unsubscribe from HTML events.");	}	// Cannot unsubscribe currently
		public function add_DragStart( handler : HtmlEventHandler ) : void
		{	new HtmlEventHandlerProxy(_instance, "ondragstart", handler);	}

		public function remove_DragEnd( handler : HtmlEventHandler ) : void
		{	throw new NotImplementedException("Cannot unsubscribe from HTML events.");	}	// Cannot unsubscribe currently
		public function add_DragEnd( handler : HtmlEventHandler ) : void
		{	new HtmlEventHandlerProxy(_instance, "ondragend", handler);	}

		public function remove_Paste( handler : HtmlEventHandler ) : void
		{	throw new NotImplementedException("Cannot unsubscribe from HTML events.");	}	// Cannot unsubscribe currently
		public function add_Paste( handler : HtmlEventHandler ) : void
		{	new HtmlEventHandlerProxy(_instance, "onpaste", handler);	}

		public function remove_BeforePaste( handler : HtmlEventHandler ) : void
		{	throw new NotImplementedException("Cannot unsubscribe from HTML events.");	}	// Cannot unsubscribe currently
		public function add_BeforePaste( handler : HtmlEventHandler ) : void
		{	new HtmlEventHandlerProxy(_instance, "onbeforepaste", handler);	}

		public function remove_BeforeCopy( handler : HtmlEventHandler ) : void
		{	throw new NotImplementedException("Cannot unsubscribe from HTML events.");	}	// Cannot unsubscribe currently
		public function add_BeforeCopy( handler : HtmlEventHandler ) : void
		{	new HtmlEventHandlerProxy(_instance, "onbeforecopy", handler);	}

		public function remove_Copy( handler : HtmlEventHandler ) : void
		{	throw new NotImplementedException("Cannot unsubscribe from HTML events.");	}	// Cannot unsubscribe currently
		public function add_Copy( handler : HtmlEventHandler ) : void
		{	new HtmlEventHandlerProxy(_instance, "oncopy", handler);	}

		public function remove_BeforeCut( handler : HtmlEventHandler ) : void
		{	throw new NotImplementedException("Cannot unsubscribe from HTML events.");	}	// Cannot unsubscribe currently
		public function add_BeforeCut( handler : HtmlEventHandler ) : void
		{	new HtmlEventHandlerProxy(_instance, "onbeforecut", handler);	}

		public function remove_Cut( handler : HtmlEventHandler ) : void
		{	throw new NotImplementedException("Cannot unsubscribe from HTML events.");	}	// Cannot unsubscribe currently
		public function add_Cut( handler : HtmlEventHandler ) : void
		{	new HtmlEventHandlerProxy(_instance, "oncut", handler);	}

		public function remove_PropertyChange( handler : HtmlEventHandler ) : void
		{	throw new NotImplementedException("Cannot unsubscribe from HTML events.");	}	// Cannot unsubscribe currently
		public function add_PropertyChange( handler : HtmlEventHandler ) : void
		{	new HtmlEventHandlerProxy(_instance, "onpropertychange", handler);	}

		public function remove_Resize( handler : HtmlEventHandler ) : void
		{	throw new NotImplementedException("Cannot unsubscribe from HTML events.");	}	// Cannot unsubscribe currently
		public function add_Resize( handler : HtmlEventHandler ) : void
		{	new HtmlEventHandlerProxy(_instance, "onresize", handler);	}
		
		public function get ChildNodes() : IEnumerable/*<IHtmlDomElement>*/
		{
			return new MshtmlElementsEnumerable(_instance.childNodes);
		}
		
		public function InsertBefore(newChild : IHtmlDomElement, refChild : IHtmlDomElement) : IHtmlDomElement
		{
			if(refChild != null)
				return Attach(_instance.insertBefore(newChild.Instance, refChild.Instance));
			else
				return Attach(_instance.insertBefore(newChild.Instance));
		}

		public function AppendChild(newChild : IHtmlDomElement) : IHtmlDomElement
		{
			return Attach(_instance.appendChild(newChild.Instance));
		}
		
		public function GetProperty2(name : System.String) : Object
		{
			var instance = _instance;
			return eval("instance." + name, "unsafe");
		}
		
		public function SetProperty2(name : System.String, value : Object) : void
		{
			var instance = _instance;
			var	val = value;
			eval("instance." + name + " = val;", "unsafe");
		}
		
		public function InvokeMethod2(name : System.String) : Object
		{
			var instance = _instance;
			return eval("instance." + name + "()");
		}
		
		public function GetElementsByTagName(tagname : System.String) : IEnumerable/*<IHtmlDomElement>*/
		{
			return new MshtmlElementsEnumerable(_instance.getElementsByTagName(tagname));
		}		
	}
	
	/// Helps with handling the events: listens for the events and relays them to the delegate specified.
	/// Makes it unnecessary to hold the whole element object in memory.
	public class HtmlEventHandlerProxy
	{
		/// Instance to which we attach the event handler (it also keeps the proxy object alive).
		var	_instance;
		
		/// A delegate that should be called when shit happens.
		var _handler : HtmlEventHandler;
		
		/// Attaches to the events.
		/// instance — HTML object to attach to.
		/// name — name of the event, including the "on-" prefix.
		/// handler — HtmlEventHandler delegate to sink the event.
		public function HtmlEventHandlerProxy(instance, name, handler : HtmlEventHandler)
		{
			_instance = instance;
			_handler = handler;
			_instance.attachEvent(name, this.HtmlEventCallback);
		}
		public function HtmlEventCallback()
		{
			try
			{
				var	sender : IHtmlDomElement = MshtmlElement.Attach(_instance);
				var	args : HtmlEventArgs = new MshtmlEventArgs(_instance.document.parentWindow.event);
				_handler(sender, args);
			}
			catch(ex : Exception)	// Don't conceal exceptions by passing them back to the browser that will merely ignore them
			{
				Core.ReportException( ex, ExceptionReportFlags.AttachLog );
			}
		}
	}
	
	/// The event arguments class that implements the missing properties of the base class.
	public class MshtmlEventArgs extends HtmlEventArgs
	{
		public function MshtmlEventArgs(instance)
		{
			super(instance);
		}
		
		public function get FromElement() : IHtmlDomElement
		{
			var ret = Instance.fromElement;
			return ret != null ? new MshtmlElement(ret) : null;
		}
		
		public function get SrcElement() : IHtmlDomElement
		{			
			var ret = Instance.srcElement;
			return ret != null ? new MshtmlElement(ret) : null;
		}
		
		public function get ToElement() : IHtmlDomElement
		{
			var ret = Instance.toElement;
			return ret != null ? new MshtmlElement(ret) : null;
		}
	}
	
	/// An IEnumerator-based class that provides for enumerating the collections of HTML elements.
	public class MshtmlElementsEnumerator implements IEnumerator/*<IHtmlDomElement>*/
	{
		/// The collection being enumerated.
		var _collection : System.Object;
		
		/// Current position in the collection.
		var _position : int = -1;	// Not started		
		
		internal function MshtmlElementsEnumerator(collection)
		{
			_collection = collection;			
		}
		
		public function MoveNext() : boolean
		{
			if(_collection == null)
				return false;	// No collection — no elements
			
			// Advance
			_position++;
			
			// True means "there is a valid current element"
			return _position < _collection.length;
		}
		
		public function get Current() : System.Object
		{
			// Validate
			if((_collection == null) || (_position < 0) || (_position >= _collection.length))
				throw new Exception("The enumerator is not positioned over a valid collection item.");
				
			// Return the current element
			return MshtmlElement.Attach(_collection.item(_position));
		}
		
		public function Reset() : void
		{
			_position = -1;
		}
	}
	
	/// Implements an IEnumerable interface for the collection of HTML elements, provides an instance of MshtmlElementsEnumerator, as needed.
	public class MshtmlElementsEnumerable implements IEnumerable/*<IHtmlDomElement>*/
	{
		// The collection to be enumerated.
		var _collection : System.Object;
		
		internal function MshtmlElementsEnumerable(collection)
		{
			_collection = collection;
		}
		
		public function GetEnumerator() : IEnumerator/*<IHtmlDomElement>*/
		{
			return new MshtmlElementsEnumerator(_collection);
		}
	}
}

/*
		/// Wires up the event specified and initiates listening for this event if it's the first time we start listening for it.
		public function WireEvent(name : System.String, map : HashSet, handler : HtmlEventHandler)
		{
			if(map.Contains(handler))
				return;	// Already subscribed for the event
			if(map.Count == 0)	// Not listening to the event yet
				_instance.attachEvent(name, OnClick);
			map.Add(handler);
		}
		
		/// Unwires the event and stops listening to it if the last listener is being removed.
		public function UnwireEvent(name : System.String, map : HashSet, handler : HtmlEventHandler)
		{
			if(!map.Contains(handler))
				throw new ArgumentException("Trying to unsubscribe from an event we're not subscribed to.");	// Not subscribed to the event
			map.Remove(handler);
			if(map.Count == 0)	// The last handler has gone, detach from the event
				_instance.detachEvent(name, OnClick);
		}
		
		/// Fires the specified event.
		public function FireEvent(map : HashSet)
		{
			if(map.Count == 0)
				throw new InvalidOperationException("An event has been unsubscribed from, but we're still listening to it.");				
				
			var	args : HtmlEventArgs = new MshtmlEventArgs(_instance.document.parentWindow.event);
			for(var handler : HtmlEventHandler in map.GetEnumerator())
				handler(this, args);		
		}
			
		
		/// Click event		
		protected var _evtClick : JetBrains.DataStructures.HashSet = new HashSet();
		public function remove_Click( handler : HtmlEventHandler ) : void
		{	UnwireEvent("onclick", _evtClick, handler);	}
		public function add_Click( handler : HtmlEventHandler ) : void
		{	WireEvent("onclick", _evtClick, handler);	}
		public function OnClick( args : HtmlEventArgs ) : void
		{	FireEvent(_evtClick);	}
*/