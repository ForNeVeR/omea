////////////////////////////////////////////////////////////////////////////////////
//  File:   Header.cs
//  Author: Sergei Pavlovsky
//
//  Copyright (c) 2004 by Sergei Pavlovsky (sergei_vp@hotmail.com, sergei_vp@ukr.net)
//
//	This file is provided "as is" with no expressed or implied warranty.
//	The author accepts no liability if it causes any damage whatsoever.
// 
//  This code is free and may be used in any way you desire. If the source code in 
//  this file is used in any commercial application then a simple email would be 
//	nice.
//
//  Revisions: 
//    06/24/2004 - Bugfix. Control was flickering on resizing.
//
////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.ComponentModel.Design;
using System.Drawing.Design;
using System.Security.Permissions;

namespace SP.Windows
{
#region Common

	/// <summary>
	/// ErrMsg class
	/// </summary>
	internal abstract class ErrMsg
	{
		public static string NegVal()
		{
			return "Value cannot be negative.";
		}

		public static string NullVal()
		{
			return "Value cannot be null.";
		}

		public static string InvVal(string sValue)
		{
			return string.Format("Value of \"{0}\" is invalid.", sValue);
		}

		public static string IndexOutOfRange()
		{
			return "Index is out of range.";
		}

		public static string SectionIsAlreadyAttached(string sText)
		{
			return "Section \"" + sText + "\" is already added to the collection.";
		}

		public static string SectionDoesNotExist(string sText)
		{
			return "Section \"" + sText + "\" does not exist in the collection";
		}	

		public static string FailedToInsertItem()
		{
			return "Failed to insert item.";
		}	

		public static string FailedToRemoveItem()
		{
			return "Failed to remove item.";
		}			

		public static string FailedToChangeItem()
		{
			return "Failed to change item.";
		}			
	}

#endregion Common


#region Header Section

	/// <summary>
	/// Types
	/// </summary>

	[Serializable]
	public enum HeaderSectionSortMarks : int
	{
		Non	 = 0,
		Up	 = NativeHeader.HDF_SORTUP,
		Down = NativeHeader.HDF_SORTDOWN
	}

	/// <summary>
	/// HeaderSection class.
	/// </summary>
	[
		Description("HeaderSection component"),
		DefaultProperty("Text"),
		ToolboxItem(false),
		DesignTimeVisible(false),
		SecurityPermission(SecurityAction.LinkDemand, 
						   Flags=SecurityPermissionFlag.UnmanagedCode)
	]
	public class HeaderSection : Component, ICloneable
	{
		/// <summary>
		/// Data fields
		/// </summary>
		
		// Owner collection
		private HeaderSectionCollection collection = null;

		[
			Description("Collection which section is kept in."),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
			Browsable(false)
		]
		internal HeaderSectionCollection Collection
		{
			get { return this.collection; }
			set { this.collection = value; }
		}

		// Owner header control
		[
			Description("Owner header control."),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
			Browsable(false)
		]
		public Header Header
		{
			get { return collection != null ? collection.Header : null; }
		}

		// Index
		[
			Description("Index of the section."),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
			Browsable(false)
		]
		public int Index
		{
			get { return collection != null ? collection.IndexOf(this) : -1; }
		}

		// Width
		private int cxWidth = 100;

		internal void _SetWidth(int cx)
		{			
			if ( cx < 0 )
				throw new ArgumentOutOfRangeException("cx", cx, ErrMsg.NegVal());

			this.cxWidth = cx;
		}   

		[
			Category("Data"),
			Description("Specifies section width.")
		]                      
		public int Width
		{
			get { return this.cxWidth; }

			set
			{ 
				if ( value != this.cxWidth )
				{ 
					_SetWidth(value);

					// Notify owner header control
					Header owner = this.Header;
					if ( owner != null )
					{
						owner._OnSectionWidthChanged(this);
					}
				}
			}
		}

		// TODO Support owner drawing HDF_OWNERDRAW

		// Format
		private int fFormat = NativeHeader.HDF_LEFT;

		internal void _SetFormat(int fFormat)
		{
			this.fFormat = fFormat;
		}

		[
			Description("Raw window styles."),
			Browsable(false)
		]
		internal int Format
		{
			get 
			{ 
				if ( this._GetActualRightToLeft() == RightToLeft.Yes )
					return this.fFormat|NativeHeader.HDF_RTLREADING;
				else
					return this.fFormat; 
			}
		}

		// Text
		private string text = null;

		internal void _SetText(string text)
		{
			this.text = text; 
          
			if ( this.text != null )
				this.fFormat |= NativeHeader.HDF_STRING;
			else
				this.fFormat &= (~NativeHeader.HDF_STRING);
		}

		[
			Category("Data"),
			Description("Text to be displayed."),
			DefaultValue("Section")
		]
		public string Text
		{
			get { return this.text; }

			set
			{ 
				if ( value != this.text )
				{ 
					_SetText(value);

					// Notify owner header control
					Header owner = this.Header;
					if ( owner != null )
					{
						owner._OnSectionTextChanged(this);
					}
				}
			}
		} 

		// ImageIndex
		private int iImage = -1;

		internal void _SetImageIndex(int index)
		{
			this.iImage = index; 
          
			if ( this.iImage >= 0 )
				this.fFormat |= NativeHeader.HDF_IMAGE;
			else
			{
				if ( this.iImage != -1 )
					throw new ArgumentException(ErrMsg.InvVal(index.ToString()), "value");

				this.fFormat &= (~NativeHeader.HDF_IMAGE);
			}
		}

		[
			Category("Data"),
			Description("Index of image associated with section."),
			TypeConverter(typeof(ImageIndexConverter)),
		//      Editor(typeof(ImageIndexEditor), typeof(UITypeEditor)),
			Localizable(true),
			DefaultValue(-1)
		]
		public int ImageIndex
		{
			get { return this.iImage; }

			set
			{ 
				if ( value != this.iImage )
				{
					_SetImageIndex(value);

					// Notify owner header control
					Header owner = this.Header;
					if ( owner != null )
					{
						owner._OnSectionImageIndexChanged(this);
					}
				}
			}
		}

		// Bitmap
		private Bitmap bitmap = null;
		private IntPtr hBitmap = IntPtr.Zero;

		internal IntPtr _GetHBitmap()
		{
			if ( this.hBitmap == IntPtr.Zero && this.bitmap != null )
			{
				this.hBitmap = this.bitmap.GetHbitmap();
			}

			return this.hBitmap;
		}

		internal void _SetBitmap(Bitmap bitmap)
		{
			if ( this.hBitmap != IntPtr.Zero )
			{
				NativeWindowCommon.DeleteObject(this.hBitmap);
				this.hBitmap = IntPtr.Zero;
			}

			this.bitmap = bitmap; 
          
			if ( this.bitmap != null )
				this.fFormat |= NativeHeader.HDF_BITMAP;
			else
				this.fFormat &= (~NativeHeader.HDF_BITMAP);
		}


		[
			Category("Data"),
			Description("Bitmap to be drawn on the section."),
		]
		public Bitmap Bitmap
		{
			get { return this.bitmap; }
			set
			{ 
				if ( value != this.bitmap )
				{
					_SetBitmap(value);

					// Notify owner header control
					Header owner = this.Header;
					if ( owner != null )
					{
						owner._OnSectionBitmapChanged(this);
					}
				}
			}
		}

		// RightToLeft
		private RightToLeft enRightToLeft = RightToLeft.No;

		internal RightToLeft _GetActualRightToLeft()
		{
			Header owner = this.Header;

			return ( this.enRightToLeft == RightToLeft.Inherit && owner != null ) 
						? owner.RightToLeft
						: this.enRightToLeft;
		}

		internal void _SetRightToLeft(RightToLeft enRightToLeft)
		{
			this.enRightToLeft = enRightToLeft;
		}

		[
			Category("Appearance"),
			Description("Right to left layout."),
		]
		public RightToLeft RightToLeft
		{
			get { return enRightToLeft; }
			set 
			{
				if ( this.enRightToLeft != value )
				{
					_SetRightToLeft(value);

					// Notify owner header control
					Header owner = this.Header;
					if ( owner != null )
					{
						owner._OnSectionRightToLeftChanged(this);
					}
				}
			}
		}

		// Content align
		internal HorizontalAlignment _GetContentAlign()
		{
			switch ( fFormat & NativeHeader.HDF_JUSTIFYMASK )
			{
			case NativeHeader.HDF_LEFT:
				return HorizontalAlignment.Left;

			case NativeHeader.HDF_RIGHT:
				return HorizontalAlignment.Right;

			case NativeHeader.HDF_CENTER:
				return HorizontalAlignment.Center;
			}

			return HorizontalAlignment.Left;
		}

		internal void _SetContentAlign(HorizontalAlignment enValue)
		{
			int nFlag;

			switch ( enValue )
			{
			case HorizontalAlignment.Left:
				nFlag = NativeHeader.HDF_LEFT;
				break;

			case HorizontalAlignment.Right:
				nFlag = NativeHeader.HDF_RIGHT;
				break;

			case HorizontalAlignment.Center:
				nFlag = NativeHeader.HDF_CENTER;
				break;

			default:
				throw new NotSupportedException(ErrMsg.InvVal(enValue.ToString()), null);
			}

			this.fFormat &= (~NativeHeader.HDF_JUSTIFYMASK);
			this.fFormat |= nFlag;
		}

		[
			Category("Appearance"),
			Description("Specifies content alignment."),
		]
		public HorizontalAlignment ContentAlign
		{
			get { return _GetContentAlign(); }

			set 
			{
				if ( value != _GetContentAlign() )
				{
					_SetContentAlign(value);

					// Notify owner header control
					Header owner = this.Header;
					if ( owner != null )
					{
						owner._OnSectionContentAlignChanged(this);
					}
				}
			}
		}

		// Image align
		internal LeftRightAlignment _GetImageAlign()
		{
			if ( (this.fFormat & NativeHeader.HDF_BITMAP_ON_RIGHT) != 0 )
				return LeftRightAlignment.Right;
			else
				return LeftRightAlignment.Left;
		}

		internal void _SetImageAlign(LeftRightAlignment enValue)
		{
			int nFlag;
			const int fMask = NativeHeader.HDF_BITMAP_ON_RIGHT;

			switch ( enValue )
			{
			case LeftRightAlignment.Left:
				nFlag = 0;
				break;

			case LeftRightAlignment.Right:
				nFlag = NativeHeader.HDF_BITMAP_ON_RIGHT;
				break;
    
			default:
				throw new NotSupportedException(ErrMsg.InvVal(enValue.ToString()), null);
			}

			this.fFormat &= (~fMask);
			this.fFormat |= nFlag;
		}

		[
			Category("Appearance"),
			Description("Specifies image placement."),
		]
		public LeftRightAlignment ImageAlign
		{
			get { return _GetImageAlign(); }

			set 
			{
				if ( value != _GetImageAlign() )
				{
					_SetImageAlign(value);

					// Notify owner header control
					Header owner = this.Header;
					if ( owner != null )
					{
						owner._OnSectionImageAlignChanged(this);
					}
				}
			}
		}

		// Sort mark
		internal HeaderSectionSortMarks _GetSortMark()
		{
			const int fSortMask = NativeHeader.HDF_SORTUP|NativeHeader.HDF_SORTDOWN;

			return (HeaderSectionSortMarks)(this.fFormat & fSortMask);
		}

		internal void _SetSortMark(HeaderSectionSortMarks enValue)
		{
			const int fSortMask = NativeHeader.HDF_SORTUP|NativeHeader.HDF_SORTDOWN;
			int nFlag;

			switch ( enValue )
			{
			case HeaderSectionSortMarks.Non:
				nFlag = 0;
				break;

			case HeaderSectionSortMarks.Up:
				nFlag = NativeHeader.HDF_SORTUP;
				break;

			case HeaderSectionSortMarks.Down:
				nFlag = NativeHeader.HDF_SORTDOWN;
				break;
        
			default:
				throw new NotSupportedException(ErrMsg.InvVal(enValue.ToString()), null);
			}

			this.fFormat &= (~fSortMask);
			this.fFormat |= nFlag;
		}

		[
			Category("Appearance"),
			Description("Defines sort mark to be shown on the section."),
		]
		public HeaderSectionSortMarks SortMark
		{
			get { return _GetSortMark(); }

			set 
			{
				if ( value != _GetSortMark() )
				{
					_SetSortMark(value);

					// Notify owner header control
					Header owner = this.Header;
					if ( owner != null )
					{
						owner._OnSectionSortMarkChanged(this);
					}
				}
			}
		}

		// Tag
		internal void _SetTag(object tag)
		{
			this.tag = tag;
		}

		private object tag = null;

		[
			Browsable(false)
		]
		public object Tag
		{
			get { return this.tag; }
			set 
			{ 
				if ( this.tag != value )
				{
					this.tag = value; 
				}
			}
		}

		/// <summary>
		/// Construction & finalization
		/// </summary>
		public HeaderSection()
		{
		}

		public HeaderSection(string text, int cxWidth)
		{
			_SetText(text);
			_SetWidth(cxWidth);
		}

		public HeaderSection(string text, int cxWidth, int iImage)
		{
			_SetText(text);
			_SetWidth(cxWidth);
			_SetImageIndex(iImage);
		}

		public HeaderSection(string text, int cxWidth, object tag)
		{
			_SetText(text);
			_SetWidth(cxWidth);
			_SetTag(tag);
		}

		public HeaderSection(string text, int cxWidth, int iImage, object tag)
		{
			_SetText(text);
			_SetWidth(cxWidth);
			_SetImageIndex(iImage);
			_SetTag(tag);
		}

		public HeaderSection(string text, int cxWidth, Bitmap bitmap)
		{
			_SetText(text);
			_SetWidth(cxWidth);
			_SetBitmap(bitmap);
		}

		public HeaderSection(string text, int cxWidth, int iImage, Bitmap bitmap)
		{
			_SetText(text);
			_SetWidth(cxWidth);
			_SetImageIndex(iImage);
			_SetBitmap(bitmap);
		}

		public HeaderSection(string text, int cxWidth, int iImage, Bitmap bitmap, 
							 HorizontalAlignment enContentAlign)
		{
			_SetText(text);
			_SetWidth(cxWidth);
			_SetImageIndex(iImage);
			_SetBitmap(bitmap);
			_SetContentAlign(enContentAlign);
		}

		public HeaderSection(string text, int cxWidth, int iImage, Bitmap bitmap, 
							 HorizontalAlignment enContentAlign, 
							 LeftRightAlignment enImageAlign)
		{
			_SetText(text);
			_SetWidth(cxWidth);
			_SetImageIndex(iImage);
			_SetBitmap(bitmap);
			_SetContentAlign(enContentAlign);
			_SetImageAlign(enImageAlign);
		}

		public HeaderSection(string text, int cxWidth, int iImage, Bitmap bitmap, 
							 HorizontalAlignment enContentAlign, 
							 LeftRightAlignment enImageAlign, object tag)
		{
			_SetText(text);
			_SetWidth(cxWidth);
			_SetImageIndex(iImage);
			_SetBitmap(bitmap);
			_SetContentAlign(enContentAlign);
			_SetImageAlign(enImageAlign);
			_SetTag(tag);
		}

		public HeaderSection(string text, int cxWidth, int iImage, Bitmap bitmap, 
							 RightToLeft enRightToLeft,	HorizontalAlignment enContentAlign, 
							 LeftRightAlignment enImageAlign, 
							 HeaderSectionSortMarks enSortMark, object tag)
		{
			_SetText(text);
			_SetWidth(cxWidth);
			_SetImageIndex(iImage);
			_SetBitmap(bitmap);
			_SetRightToLeft(enRightToLeft);
			_SetContentAlign(enContentAlign);
			_SetImageAlign(enImageAlign);
			_SetSortMark(enSortMark);
			_SetTag(tag);
		}

		protected HeaderSection(int cxWidth, string text, int iImage, Bitmap bitmap, 
								RightToLeft enRightToLeft, int fFormat, object tag)
		{
			_SetText(text);
			_SetWidth(cxWidth);
			_SetImageIndex(iImage);
			_SetBitmap(bitmap);
			_SetRightToLeft(enRightToLeft);
			_SetFormat(fFormat);
			_SetTag(tag);
		}

		~HeaderSection()
		{
			Dispose(false);
		}

		/// <summary>
		/// Overrides
		/// </summary>
		public override string ToString()
		{
			return "HeaderSection: {" + this.text + "}"; 
		}

		protected override void Dispose(bool bDisposing)
		{
			if ( this.hBitmap != IntPtr.Zero )
			{
				NativeWindowCommon.DeleteObject(this.hBitmap);
				this.hBitmap = IntPtr.Zero;
			}

			if ( bDisposing && this.collection != null )
			{
				this.collection.Remove(this);
			}

			base.Dispose(bDisposing);
		}

		/// <summary>
		/// ICloneable implementation
		/// </summary>
		public virtual object Clone()
		{
			return new HeaderSection(this.cxWidth, this.text, this.iImage, this.bitmap, 
									 this.enRightToLeft, this.fFormat, this.tag);
		}

		/// <summary>
		/// Operations
		/// </summary>
		internal void ComposeNativeData(int iOrder, out NativeHeader.HDITEM item)
		{
			item = new NativeHeader.HDITEM();
      
			// Width
			item.mask = NativeHeader.HDI_WIDTH;
			item.cxy = this.cxWidth;

			// Text
			if ( this.text != null )
			{
				item.mask |= NativeHeader.HDI_TEXT;
				item.lpszText = this.text;
				item.cchTextMax = 0;
			}

			// ImageIndex
			if ( this.iImage >= 0 )
			{
				item.mask |= NativeHeader.HDI_IMAGE;
				item.iImage = this.iImage;
			}

			// Bitmap
			if ( this.bitmap != null && this.bitmap.GetHbitmap() != IntPtr.Zero )
			{
				item.mask |= NativeHeader.HDI_BITMAP;
				item.hbm = this._GetHBitmap();
			}

			// Format
			item.mask |= NativeHeader.HDI_FORMAT;
			item.fmt = this.Format;

			// Order
			if ( iOrder >= 0 )
			{
				item.mask |= NativeHeader.HDI_ORDER;
				item.iOrder = iOrder;
			}

			//      item.lParam;
			//      item.type;
			//      item.pvFilter;
		}    

	} 

#endregion // HeaderSection


#region Header Sections' Collection

	/// <summary>
	/// HeaderSectionCollection class.
	/// </summary>

	//  [Serializable]
	public class HeaderSectionCollection : IList
	{
		/// <summary>
		/// Data fields
		/// </summary>
		private Header owner = null;
		public Header Header
		{
			get { return this.owner; }
		}

		private ArrayList alSectionsByOrder = null;
		private ArrayList alSectionsByRawIndex = null;

		public int Count 
		{ 
			get { return this.alSectionsByOrder.Count; }
		}

		public HeaderSection this[int index] 
		{
			get { return (HeaderSection)this.alSectionsByOrder[index]; }
			set
			{
				if ( index < 0 || index >= this.alSectionsByOrder.Count )
					throw new ArgumentOutOfRangeException("index", index, 
														  ErrMsg.IndexOutOfRange());
      
				_SetSection(index, (HeaderSection)value);
			}
		}

		/// <summary>
		/// Construction
		/// </summary>
		internal HeaderSectionCollection(Header owner)
		{
			this.owner = owner;
			this.alSectionsByOrder = new ArrayList();
			this.alSectionsByRawIndex  = new ArrayList();
		}

		/// <summary>
		/// Helpers
		/// </summary>
		private void BindSection(HeaderSection item)
		{
			if ( item == null )
			{
				throw new ArgumentNullException("item", ErrMsg.NullVal());
			}

			if ( item.Collection != null )
			{
				throw new ArgumentException(ErrMsg.SectionIsAlreadyAttached(item.Text), "item");
			}

			item.Collection = this;
		}

		private void UnbindSection(HeaderSection item)
		{
			if ( item == null )
			{
				throw new ArgumentNullException("item", ErrMsg.NullVal());
			}

			if ( item.Collection != this )
			{
				throw new ArgumentException(ErrMsg.SectionDoesNotExist(item.Text), "item");
			}

			item.Collection = null;
		}

		/// <summary>
		/// Operations
		/// </summary>
		internal int _FindSectionRawIndex(HeaderSection item)
		{
			return this.alSectionsByRawIndex.IndexOf(item);
		}

		internal HeaderSection _GetSectionByRawIndex(int iSection)
		{
			return (HeaderSection)this.alSectionsByRawIndex[iSection];
		}

		internal void _Move(int iFrom, int iTo)
		{
			Debug.Assert( iFrom >= 0 || iFrom < this.alSectionsByOrder.Count );
			Debug.Assert( iTo >= 0 || iTo < this.alSectionsByOrder.Count );
			Debug.Assert( this.alSectionsByOrder.Count == this.alSectionsByRawIndex.Count );

			HeaderSection item = (HeaderSection)this.alSectionsByOrder[iFrom];
			this.alSectionsByOrder.RemoveAt(iFrom);
			this.alSectionsByOrder.Insert(iTo, item);
		}

		internal void _SetSection(int index, HeaderSection item)
		{
			Debug.Assert( index >= 0 || index < this.alSectionsByOrder.Count );
			Debug.Assert( this.alSectionsByOrder.Count == this.alSectionsByRawIndex.Count );			

			// Bind item to the collection
			BindSection(item);

			HeaderSection itemOld = (HeaderSection)this.alSectionsByOrder[index];
			int iSection = this.alSectionsByRawIndex.IndexOf(itemOld);

			try
			{        
				this.alSectionsByOrder[index] = item;
				this.alSectionsByRawIndex[iSection] = item;

				UnbindSection(itemOld);

				// Notify owner
				if ( this.owner != null )  
					this.owner._OnSectionChanged(iSection, item);
			}
			catch
			{
				if ( itemOld.Collection == null )
					BindSection(itemOld);

				this.alSectionsByOrder[index] = itemOld;
				this.alSectionsByRawIndex[iSection] = itemOld;

				UnbindSection(item);
				throw;
			}
		}

		public void Insert(int index, HeaderSection item)
		{ 
			Debug.Assert( this.alSectionsByOrder.Count == this.alSectionsByRawIndex.Count );

			if ( index < 0 || index > this.alSectionsByOrder.Count )
				throw new ArgumentOutOfRangeException("index", index, ErrMsg.IndexOutOfRange());

			// Bind item to the collection
			BindSection(item);

			try
			{
				this.alSectionsByOrder.Insert(index, item);
				this.alSectionsByRawIndex.Insert(index, item);

				try
				{
					// Notify owner
					if ( this.owner != null )  
						this.owner._OnSectionInserted(index, item);
				}
				catch
				{
					this.alSectionsByOrder.Remove(item);
					this.alSectionsByRawIndex.Remove(item);
					throw;
				}
			}
			catch
			{
				if ( this.alSectionsByOrder.Count > this.alSectionsByRawIndex.Count )
					this.alSectionsByOrder.RemoveAt(index);

				UnbindSection(item);
				throw;
			}
		}

		public int Add(HeaderSection item)
		{
			int index = this.alSectionsByOrder.Count;

			Insert(index, item);

			return index;
		}

		public void RemoveAt(int index)
		{
			Debug.Assert( this.alSectionsByOrder.Count == this.alSectionsByRawIndex.Count );

			if ( index < 0 || index >= this.alSectionsByOrder.Count )
				throw new ArgumentOutOfRangeException("index", index, ErrMsg.IndexOutOfRange());

			HeaderSection item = (HeaderSection)this.alSectionsByOrder[index];
			int iSectionRemoved = this.alSectionsByRawIndex.IndexOf(item);
			Debug.Assert( iSectionRemoved >= 0 );

			UnbindSection(item);

			this.alSectionsByOrder.RemoveAt(index);
			this.alSectionsByRawIndex.RemoveAt(iSectionRemoved);

			if ( this.owner != null )  
				this.owner._OnSectionRemoved(iSectionRemoved, item);
		}

		public virtual void Remove(HeaderSection item)
		{      
			int index = this.alSectionsByOrder.IndexOf(item);

			if ( index != -1 )
				RemoveAt(index);
		}

		public void Move(int iFrom, int iTo)
		{
			if ( iFrom < 0 || iFrom >= this.alSectionsByOrder.Count )
				throw new ArgumentOutOfRangeException("iFrom", iFrom, ErrMsg.IndexOutOfRange());

			if ( iTo < 0 || iTo >= this.alSectionsByOrder.Count )
				throw new ArgumentOutOfRangeException("iTo", iTo, ErrMsg.IndexOutOfRange());

			_Move(iFrom, iTo);
		}

		public void Clear()
		{
			Debug.Assert( this.alSectionsByOrder.Count == this.alSectionsByRawIndex.Count );

			foreach( HeaderSection item in this.alSectionsByOrder )
			{
				UnbindSection(item);
			}

			this.alSectionsByOrder.Clear();
			this.alSectionsByRawIndex.Clear();

			if ( this.owner != null )  
				this.owner._OnAllSectionsRemoved();
		}

    internal void Clear(bool bDisposeItems)
    {
      Debug.Assert( this.alSectionsByOrder.Count == this.alSectionsByRawIndex.Count );

      foreach( HeaderSection item in this.alSectionsByOrder )
      {
        UnbindSection(item);

        if ( bDisposeItems )
          item.Dispose();
      }

      this.alSectionsByOrder.Clear();
      this.alSectionsByRawIndex.Clear();

      if ( this.owner != null )  
        this.owner._OnAllSectionsRemoved();
    }


		public int IndexOf(HeaderSection item)
		{
			return this.alSectionsByOrder.IndexOf(item);
		}

		public bool Contains(HeaderSection item)
		{
			return this.alSectionsByOrder.IndexOf(item) != -1; 
		}

		public void CopyTo(Array aDest, int index)
		{
			this.alSectionsByOrder.CopyTo(aDest, index);
		}

		/// <summary>
		/// Implementation: IEnumerable
		/// </summary>
		IEnumerator IEnumerable.GetEnumerator()
		{ 
			return this.alSectionsByOrder.GetEnumerator();
		}

		/// <summary>
		/// Implementation: ICollection
		/// </summary>
		bool ICollection.IsSynchronized 
		{ 
			get { return true; }
		}

		object ICollection.SyncRoot 
		{
			get { return this; }
		}


		/// <summary>
		/// Implementation: IList
		/// </summary>
		bool IList.IsFixedSize 
		{
			get { return false; }
		}

		bool IList.IsReadOnly 
		{
			get { return false; }
		}

		object IList.this[int index] 
		{
			get { return this.alSectionsByOrder[index]; } 
			set 
			{ 
				_SetSection(index, (HeaderSection)value);
			} 
		}

		void IList.Insert(int index, object value)
		{
			Insert(index, (HeaderSection)value);
		}

		int IList.Add(object value)
		{     
			return Add((HeaderSection)value);
		}

		void IList.Remove(object value)
		{
			Remove((HeaderSection)value);
		}

		bool IList.Contains(object value)
		{
			return this.alSectionsByOrder.Contains(value); 
		}

		int IList.IndexOf(object value)
		{
			return this.alSectionsByOrder.IndexOf(value);
		}

	} // HeaderSectionCollection class


#endregion // HeaderSectionCollection


#region Header Event Arguments' classes

	/// <summary>
	/// HeaderSectionEventArgs class
	/// </summary>
	[Serializable]
	public class HeaderSectionEventArgs : EventArgs
	{
		// Fields
		private HeaderSection item = null;
		public HeaderSection Item
		{
			get { return this.item; }
		}

		// Fields
		private MouseButtons enButton = MouseButtons.None;
		public MouseButtons Button
		{
			get { return this.enButton; }
		}

		// Construction
		public HeaderSectionEventArgs(HeaderSection item)
		{
			this.item = item;
		}

		public HeaderSectionEventArgs(HeaderSection item, MouseButtons enButton)
		{
			this.item = item;
			this.enButton = enButton;
		}

	} // HeaderSectionEventArgs
  
	public delegate void HeaderSectionEventHandler(
							object sender, HeaderSectionEventArgs ea);


	/// <summary>
	/// HeaderSectionConformableEventArgs class
	/// </summary>
	[Serializable]
	public class HeaderSectionConformableEventArgs : HeaderSectionEventArgs
	{
		// Fields
		private bool bAccepted = true;
		public bool Accepted
		{
			get { return this.bAccepted; }
			set { this.bAccepted = value; }
		}

		// Construction
		public HeaderSectionConformableEventArgs(HeaderSection item)
			: base(item)
		{
		}

		public HeaderSectionConformableEventArgs(HeaderSection item, MouseButtons enButton)
			: base(item, enButton)
		{
		}

	} // HeaderSectionConformableEventArgs

	public delegate void HeaderSectionConformableEventHandler(
							object sender, HeaderSectionConformableEventArgs ea);


	/// <summary>
	/// HeaderSectionWidthEventArgs class
	/// </summary>
	[Serializable]
	public class HeaderSectionWidthEventArgs : HeaderSectionEventArgs
	{
		// Fields
		private int cxWidth = 0;
		public int Width
		{
			get { return this.cxWidth; }
		}

		// Construction
		public HeaderSectionWidthEventArgs(HeaderSection item)
			: base(item)
		{
		}

		public HeaderSectionWidthEventArgs(HeaderSection item, MouseButtons enButton)
			: base(item, enButton)
		{
		}

		public HeaderSectionWidthEventArgs(HeaderSection item, MouseButtons enButton, 
										   int cxWidth)
			: base(item, enButton)
		{
			this.cxWidth = cxWidth;
		}

	} // HeaderWidthItemEventArgs

	public delegate void HeaderSectionWidthEventHandler(
							object sender, HeaderSectionWidthEventArgs ea);


	/// <summary>
	/// HeaderSectionWidthConformableEventArgs class
	/// </summary>
	[Serializable]
	public class HeaderSectionWidthConformableEventArgs : HeaderSectionWidthEventArgs
	{
		// Fields
		private bool bAccepted = true;
		public bool Accepted
		{
			get { return this.bAccepted; }
			set { this.bAccepted = value; }
		}

		// Construction
		public HeaderSectionWidthConformableEventArgs(HeaderSection item)
			: base(item)
		{
		}

		public HeaderSectionWidthConformableEventArgs(HeaderSection item, MouseButtons enButton)
			: base(item, enButton)
		{
		}

		public HeaderSectionWidthConformableEventArgs(HeaderSection item, MouseButtons enButton, 
													  int cxWidth)
			: base(item, enButton, cxWidth)
		{
		}

	} // HeaderSectionWidthConformableEventArgs

	public delegate void HeaderSectionWidthConformableEventHandler(
							object sender, HeaderSectionWidthConformableEventArgs ea);


	/// <summary>
	/// HeaderSectionOrderEventArgs class
	/// </summary>
	[Serializable]
	public class HeaderSectionOrderEventArgs : HeaderSectionEventArgs
	{
		// Fields
		private int iOrder = -1;
		public int Order
		{
			get { return this.iOrder; }
		}

		// Construction
		public HeaderSectionOrderEventArgs(HeaderSection item)
			: base(item)
		{
		}

		public HeaderSectionOrderEventArgs(HeaderSection item, MouseButtons enButton)
			: base(item, enButton)
		{
		}

		public HeaderSectionOrderEventArgs(HeaderSection item, MouseButtons enButton, 
										   int iOrder)
			: base(item, enButton)
		{
			this.iOrder = iOrder;
		}

	} // HeaderSectionOrderEventArgs

	public delegate void HeaderSectionOrderEventHandler(
							object sender, HeaderSectionOrderEventArgs ea);


	/// <summary>
	/// HeaderSectionOrderConformableEventArgs class
	/// </summary>
	[Serializable]
	public class HeaderSectionOrderConformableEventArgs : HeaderSectionOrderEventArgs
	{
		// Fields
		private bool bAccepted = true;
		public bool Accepted
		{
			get { return this.bAccepted; }
			set { this.bAccepted = value; }
		}

		// Construction
		public HeaderSectionOrderConformableEventArgs(HeaderSection item)
			: base(item)
		{
		}

		public HeaderSectionOrderConformableEventArgs(HeaderSection item, MouseButtons enButton)
			: base(item, enButton)
		{
		}

		public HeaderSectionOrderConformableEventArgs(HeaderSection item, MouseButtons enButton, 
													  int iOrder)
			: base(item, enButton, iOrder)
		{
		}

	} // HeaderSectionOrderConformableEventArgs

	public delegate void HeaderSectionOrderConformableEventHandler(
							object sender, HeaderSectionOrderConformableEventArgs ea);

    public class HeaderCustomDrawEventArgs: HeaderSectionEventArgs
    {
        private readonly IntPtr _hdc;
        private readonly Rectangle _rc;

        public HeaderCustomDrawEventArgs( HeaderSection section, IntPtr hdc, Rectangle rc )
            : base( section )
        {
            _hdc = hdc;
            _rc = rc;
        }

        public IntPtr Hdc
        {
            get { return _hdc; }
        }

        public Rectangle Bounds
        {
            get { return _rc; }
        }
    }

    public delegate void HeaderCustomDrawEventHandler( object sender, HeaderCustomDrawEventArgs ea );

#endregion // HeaderEventArgs


#region Header control

	/// <summary>
	/// Header class.
	/// </summary>
	[
		Description("SP Header Control"),
		DefaultProperty("Sections"),
		DefaultEvent("AfterSectionTrack"),
		Designer(typeof(HeaderDesigner)),
		SecurityPermission(SecurityAction.LinkDemand, 
						   Flags=SecurityPermissionFlag.UnmanagedCode)
	]
	public class Header : Control
	{
		/// <summary>
		/// Types
		/// </summary>
		
		[Flags]
		public enum HitTestArea : int
		{
			NoWhere			= NativeHeader.HHT_NOWHERE,
			OnHeader		= NativeHeader.HHT_ONHEADER,
			OnDivider		= NativeHeader.HHT_ONDIVIDER,
			OnDividerOpen	= NativeHeader.HHT_ONDIVOPEN,
			OnFilter		= NativeHeader.HHT_ONFILTER,
			OnFilterButton	= NativeHeader.HHT_ONFILTERBUTTON,
			Above			= NativeHeader.HHT_ABOVE,
			Below			= NativeHeader.HHT_BELOW,
			ToLeft			= NativeHeader.HHT_TOLEFT,
			ToRight			= NativeHeader.HHT_TORIGHT
		}

		public struct HitTestInfo
		{
			public HitTestArea    fArea;
			public HeaderSection  section;
		}

		/// <summary>
		/// Data Fields
		/// </summary>
				
		private int fStyle = NativeHeader.WS_CHILD;

		// Clickable
		[
			Category("Behavior"),
			Description("Determines if control will generate events " +
						"when user clicks on its column titles." ),
			DefaultValue(false)
		]
		public bool Clickable
		{
			get { return (this.fStyle & NativeHeader.HDS_BUTTONS) != 0; }
			set 
			{ 
				bool bOldValue = (this.fStyle & NativeHeader.HDS_BUTTONS) != 0;

				if ( value != bOldValue )
				{
					if ( value )
						this.fStyle |= NativeHeader.HDS_BUTTONS;
					else
						this.fStyle &= (~NativeHeader.HDS_BUTTONS);

					if ( this.IsHandleCreated )
					{
						UpdateWndStyle();
					}
				}
			}
		}

		// HotTrack
		[
			Category("Behavior"),
			Description("Enables or disables hot tracking." ),
			DefaultValue(false)
		]
		public bool HotTrack 
		{
			get { return (this.fStyle & NativeHeader.HDS_HOTTRACK) != 0; }
			set 
			{ 
				bool bOldValue = (this.fStyle & NativeHeader.HDS_HOTTRACK) != 0;

				if ( value != bOldValue )
				{
					if ( value )
						this.fStyle |= NativeHeader.HDS_HOTTRACK;
					else
						this.fStyle &= (~NativeHeader.HDS_HOTTRACK);

					if ( this.IsHandleCreated )
					{
						UpdateWndStyle();
					}
				}
			}
		}

		// Flat
		[
			Category("Appearance"),
			Description("Causes the header control to be drawn flat when " + 
						"Microsoft® Windows® XP is running in classic mode." ),
			DefaultValue(false)
		]
		public bool Flat 
		{
			get { return (this.fStyle & NativeHeader.HDS_FLAT) != 0; }
			set 
			{ 
				bool bOldValue = (this.fStyle & NativeHeader.HDS_FLAT) != 0;

				if ( value != bOldValue )
				{
					if ( value )
						this.fStyle |= NativeHeader.HDS_FLAT;
					else
						this.fStyle &= (~NativeHeader.HDS_FLAT);

					if ( this.IsHandleCreated )
					{
						UpdateWndStyle();
					}
				}
			}
		}

		// AllowDragSections
		[
			Category("Behavior"),
			Description("Determines if user will be able to drag header column " + 
						"on another position." ),
			DefaultValue(false)
		]
		public bool AllowDragSections
		{
			get { return (this.fStyle & NativeHeader.HDS_DRAGDROP) != 0; }
			set 
			{ 
				bool bOldValue = (this.fStyle & NativeHeader.HDS_DRAGDROP) != 0;

				if ( value != bOldValue )
				{
					if ( value )
						this.fStyle |= NativeHeader.HDS_DRAGDROP;
					else
						this.fStyle &= (~NativeHeader.HDS_DRAGDROP);

					if ( this.IsHandleCreated )
					{
						UpdateWndStyle();
					}
				}
			}
		}

		// FullDragSections
		[
			Category("Behavior"),
			Description("Causes the header control to display column contents " + 
						"even while the user resizes a column." ),
			DefaultValue(false)
		]
		public bool FullDragSections
		{
			get { return (this.fStyle & NativeHeader.HDS_FULLDRAG) != 0; }
			set 
			{ 
				bool bOldValue = (this.fStyle & NativeHeader.HDS_FULLDRAG) != 0;

				if ( value != bOldValue )
				{
					if ( value )
						this.fStyle |= NativeHeader.HDS_FULLDRAG;
					else
						this.fStyle &= (~NativeHeader.HDS_FULLDRAG);

					if ( this.IsHandleCreated )
					{
						UpdateWndStyle();
					}
				}
			}
		}

		// Sections
		private HeaderSectionCollection colSections = null;

		[
			Category("Data"),
			Description("Sections of the header." ),
			MergableProperty(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Content),
			Localizable(true)
		]
		public HeaderSectionCollection Sections
		{
			get { return this.colSections; }
		}

		private ImageList imageList = null;
		[
			Category("Data"),
			Description("Images for header's sections." ),
			DefaultValue(null)
		]
		public ImageList ImageList
		{
			get { return this.imageList; }
			set 
			{ 
				if ( this.imageList != value )
				{
					EventHandler ehRecreateHandle = 
						new EventHandler(this.OnImageListRecreateHandle);

					EventHandler ehDetachImageList = 
						new EventHandler(this.OnDetachImageList);

					if ( this.imageList != null )
					{
						this.imageList.RecreateHandle -= ehRecreateHandle;
						this.imageList.Disposed -= ehDetachImageList;
					}

					this.imageList = value; 

					if ( this.imageList != null )
					{
						this.imageList.RecreateHandle += ehRecreateHandle;
						this.imageList.Disposed += ehDetachImageList;
					}         

					if ( IsHandleCreated )
					{
						HandleRef hrThis = new HandleRef(this, this.Handle);

						UpdateWndImageList(ref hrThis, this.imageList);            
					}
				}
			}
		}

		private int cxBitmapMargin = SystemInformation.Border3DSize.Width;
		[
			Category("Appearance"),
			Description("Width of the margin that surrounds a bitmap " +
						"within an existing header control." ),
			DefaultValue(2)
		]
		public int BitmapMargin
		{
			get { return this.cxBitmapMargin; }
			set 
			{ 
				if ( this.cxBitmapMargin != value )
				{
					if ( value < 0 )
						throw new ArgumentOutOfRangeException("value", value, ErrMsg.NegVal());

					this.cxBitmapMargin = value; 

					if ( IsHandleCreated )
					{
						HandleRef hrThis = new HandleRef(this, this.Handle);

						UpdateWndBitmapMargin(ref hrThis, this.cxBitmapMargin);
					}
				}
			}
		}

		/// <summary>
		/// Construction & finalization
		/// </summary>
		
		public Header()
			: base()
		{
			this.SetStyle(ControlStyles.UserPaint, false);
			//this.SetStyle(ControlStyles.UserMouse, false); // ???
			this.SetStyle(ControlStyles.StandardClick, false);
			this.SetStyle(ControlStyles.StandardDoubleClick, false);
			this.SetStyle(ControlStyles.Opaque, true);
			this.SetStyle(ControlStyles.ResizeRedraw, this.DesignMode);
			this.SetStyle(ControlStyles.Selectable, false); 
			//this.SetStyle(ControlStyles.AllPaintingInWmPaint, false); // ???

			this.colSections = new HeaderSectionCollection(this);
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
    
		protected override void Dispose(bool disposing)
		{
			if ( disposing )
			{
				if ( this.imageList != null )
				{
					this.imageList.RecreateHandle -= 
						new EventHandler(this.OnImageListRecreateHandle);

					this.imageList.Disposed -= 
						new EventHandler(this.OnDetachImageList);

					this.imageList = null;
				}

				this.colSections.Clear(true);
			}

			base.Dispose( disposing );
		}

		/// <summary>
		/// Helpers
		/// </summary>
    
		private int ExtractIndexFromNMHEADER(ref NativeHeader.NMHEADER nmh)
		{
			return nmh.iItem;
		}

		private MouseButtons ExtractMouseButtonFromNMHEADER(ref NativeHeader.NMHEADER nmh)
		{
			switch ( nmh.iButton )
			{
			case 0:
				return MouseButtons.Left;

			case 1:
				return MouseButtons.Right;

			case 2:
				return MouseButtons.Middle;         
			}

			return MouseButtons.None;
		}

		private void ExtractSectionDataFromNMHEADER(ref NativeHeader.NMHEADER nmh,
													ref NativeHeader.HDITEM2 item)
		{
			if ( nmh.pitem != IntPtr.Zero )
			{
				item = (NativeHeader.HDITEM2)Marshal.PtrToStructure(
												nmh.pitem, typeof(NativeHeader.HDITEM2));
			}
		}

		private static void UpdateWndImageList(ref HandleRef hrThis, ImageList imageList)
		{
			Debug.Assert( hrThis.Handle != IntPtr.Zero );

			IntPtr hIL = (imageList != null) ? imageList.Handle : IntPtr.Zero;

			NativeHeader.SetImageList(hrThis.Handle, new HandleRef(imageList, hIL).Handle);        
		}

		private static void UpdateWndBitmapMargin(ref HandleRef hrThis, int cxMargin)
		{
			Debug.Assert( hrThis.Handle != IntPtr.Zero );

			NativeHeader.SetBitmapMargin(hrThis.Handle, cxMargin);
		}   

		private static void UpdateWndStyle(ref HandleRef hrThis, int fNewStyle)
		{
			Debug.Assert( hrThis.Handle != IntPtr.Zero );

			const int fOptions = NativeHeader.SWP_NOSIZE|
								 NativeHeader.SWP_NOMOVE|
								 NativeHeader.SWP_NOZORDER|
								 NativeHeader.SWP_NOACTIVATE|
								 NativeHeader.SWP_FRAMECHANGED;

			int fStyle = NativeHeader.GetWindowLong(hrThis.Handle, NativeHeader.GWL_STYLE);
			fStyle &= ~(NativeHeader.HDS_BUTTONS|
						NativeHeader.HDS_HOTTRACK|
						NativeHeader.HDS_FLAT|
						NativeHeader.HDS_DRAGDROP|
						NativeHeader.HDS_FULLDRAG);

			fStyle |= fNewStyle;

			NativeHeader.SetWindowLong(hrThis.Handle, NativeHeader.GWL_STYLE, fStyle);

			NativeHeader.SetWindowPos(hrThis.Handle, IntPtr.Zero, 0, 0, 0, 0, fOptions);
		}   

		private void UpdateWndStyle()
		{
			HandleRef hrThis = new HandleRef(this, this.Handle);

			UpdateWndStyle(ref hrThis, this.fStyle);
		}   

		/// <summary>
		/// Internal notifications
		/// </summary>

		protected override void OnHandleCreated(EventArgs ea)
		{
			HandleRef hrThis = new HandleRef(this, this.Handle);

			// Set Window Style
			UpdateWndStyle(ref hrThis, this.fStyle);

			// Set Bitmap Margin
			UpdateWndBitmapMargin(ref hrThis, this.cxBitmapMargin);

			// Set ImageList
			UpdateWndImageList(ref hrThis, this.imageList);

			// Add items
			for ( int i = 0; i < this.colSections.Count; i++ )
			{
				HeaderSection item = colSections[i];

				NativeHeader.HDITEM hdi;
				item.ComposeNativeData(i, out hdi);

				int nResult = NativeHeader.InsertItem(this.Handle, i, ref hdi);
				Debug.Assert( nResult >= 0 );
				if ( nResult < 0 )
					throw new InvalidOperationException(ErrMsg.FailedToInsertItem(), 
														new Win32Exception());
			}

			base.OnHandleCreated(ea);
		}

		protected override void OnHandleDestroyed(EventArgs ea)
		{
			// Collect item parameters from native window

			base.OnHandleDestroyed(ea); 
		}

		protected override void OnEnabledChanged(EventArgs ea)
		{
			base.OnEnabledChanged(ea);
		}

		protected override void OnFontChanged(EventArgs ea)
		{
			base.OnFontChanged(ea);
		}

		internal void _OnSectionInserted(int index, HeaderSection item)
		{
			if ( this.IsHandleCreated )
			{
				NativeHeader.HDITEM hdi;
				hdi.lpszText = null;

				item.ComposeNativeData(index, out hdi);        

				int iResult = NativeHeader.InsertItem(new HandleRef(this, this.Handle).Handle, 
													  index, ref hdi);
				Debug.Assert( iResult == index );
				if ( iResult < 0 )
					throw new InvalidOperationException(ErrMsg.FailedToInsertItem(), 
														new Win32Exception());
			}
		}

		internal void _OnSectionRemoved(int iRawIndex, HeaderSection item)
		{
			if ( this.IsHandleCreated )
			{
				HandleRef hrThis = new HandleRef(this, this.Handle);

				bool bResult = NativeHeader.DeleteItem(hrThis.Handle, iRawIndex);
				Debug.Assert( bResult );
				if ( !bResult )
				{
					throw new InvalidOperationException(ErrMsg.FailedToRemoveItem(), 
														new Win32Exception());
				}
			}
		}

		internal void _OnAllSectionsRemoved()
		{
			if ( this.IsHandleCreated )
			{
				BeginUpdate();

				try
				{
					HandleRef hrThis = new HandleRef(this, this.Handle);

					while ( NativeHeader.GetItemCount(this.Handle) != 0  )
					{
						bool bResult = NativeHeader.DeleteItem(hrThis.Handle, 0);
						Debug.Assert( bResult );
						if ( !bResult )
						{
							throw new InvalidOperationException(ErrMsg.FailedToRemoveItem(), 
																new Win32Exception());
						}

					}
				}
				finally
				{
					EndUpdate();
				}
			}
		}

		internal void _OnSectionChanged(int iRawIndex, HeaderSection item)
		{
			if ( this.IsHandleCreated )
			{
				NativeHeader.HDITEM hdi;
				item.ComposeNativeData(-1, out hdi);

				HandleRef hrThis = new HandleRef(this, this.Handle);

				bool bResult = NativeHeader.SetItem(hrThis.Handle, iRawIndex, ref hdi);
				Debug.Assert( bResult );
				if ( !bResult )
				{
					throw new InvalidOperationException(ErrMsg.FailedToChangeItem(), 
														new Win32Exception());
				}
			}
		}

		internal void _OnSectionWidthChanged(HeaderSection item)
		{
			if ( this.IsHandleCreated )
			{
				int iSection = this.colSections._FindSectionRawIndex(item);
				Debug.Assert( iSection >= 0 );

				NativeHeader.HDITEM hdi = new NativeHeader.HDITEM();
				hdi.mask = NativeHeader.HDI_WIDTH;
				hdi.cxy = item.Width;

				HandleRef hrThis = new HandleRef(this, this.Handle);

				bool bResult = NativeHeader.SetItem(hrThis.Handle, iSection, ref hdi);
				Debug.Assert( bResult );
				if ( !bResult )
				{
					throw new InvalidOperationException(ErrMsg.FailedToChangeItem(), 
														new Win32Exception());
				}
			}
		}

		internal void _OnSectionTextChanged(HeaderSection item)
		{
			if ( this.IsHandleCreated )
			{
				int iSection = this.colSections._FindSectionRawIndex(item);
				Debug.Assert( iSection >= 0 );

				NativeHeader.HDITEM hdi = new NativeHeader.HDITEM();
				hdi.mask = NativeHeader.HDI_FORMAT|NativeHeader.HDI_TEXT;
				hdi.fmt = item.Format;
				hdi.lpszText = item.Text;

				HandleRef hrThis = new HandleRef(this, this.Handle);

				bool bResult = NativeHeader.SetItem(hrThis.Handle, iSection, ref hdi);
				Debug.Assert( bResult );
				if ( !bResult )
				{
					throw new InvalidOperationException(ErrMsg.FailedToChangeItem(), 
														new Win32Exception());
				}
			}
		}

		internal void _OnSectionImageIndexChanged(HeaderSection item)
		{
			if ( this.IsHandleCreated )
			{
				int iSection = this.colSections._FindSectionRawIndex(item);
				Debug.Assert( iSection >= 0 );

				NativeHeader.HDITEM hdi = new NativeHeader.HDITEM();
				hdi.mask = NativeHeader.HDI_FORMAT|NativeHeader.HDI_IMAGE;
				hdi.fmt = item.Format;
				hdi.iImage = item.ImageIndex;

				HandleRef hrThis = new HandleRef(this, this.Handle);

				bool bResult = NativeHeader.SetItem(hrThis.Handle, iSection, ref hdi);
				Debug.Assert( bResult );
				if ( !bResult )
				{
					throw new InvalidOperationException(ErrMsg.FailedToChangeItem(), 
														new Win32Exception());
				}
			}
		}

		internal void _OnSectionBitmapChanged(HeaderSection item)
		{
			if ( this.IsHandleCreated )
			{
				int iSection = this.colSections._FindSectionRawIndex(item);
				Debug.Assert( iSection >= 0 );

				NativeHeader.HDITEM hdi = new NativeHeader.HDITEM();
				hdi.mask = NativeHeader.HDI_FORMAT|NativeHeader.HDI_BITMAP;
				hdi.fmt = item.Format;
				hdi.hbm = item._GetHBitmap();

				HandleRef hrThis = new HandleRef(this, this.Handle);

				bool bResult = NativeHeader.SetItem(hrThis.Handle, iSection, ref hdi);
				Debug.Assert( bResult );
				if ( !bResult )
				{
					throw new InvalidOperationException(ErrMsg.FailedToChangeItem(), 
														new Win32Exception());
				}
			}
		}

		internal void _OnSectionRightToLeftChanged(HeaderSection item)
		{
			if ( this.IsHandleCreated )
			{
				int iSection = this.colSections._FindSectionRawIndex(item);
				Debug.Assert( iSection >= 0 );

				NativeHeader.HDITEM hdi = new NativeHeader.HDITEM();
				hdi.mask = NativeHeader.HDI_FORMAT;
				hdi.fmt = item.Format;

				HandleRef hrThis = new HandleRef(this, this.Handle);

				bool bResult = NativeHeader.SetItem(hrThis.Handle, iSection, ref hdi);
				Debug.Assert( bResult );
				if ( !bResult )
				{
					throw new InvalidOperationException(ErrMsg.FailedToChangeItem(), 
														new Win32Exception());
				}
			}
		}

		internal void _OnSectionContentAlignChanged(HeaderSection item)
		{
			if ( this.IsHandleCreated )
			{
				int iSection = this.colSections._FindSectionRawIndex(item);
				Debug.Assert( iSection >= 0 );

				NativeHeader.HDITEM hdi = new NativeHeader.HDITEM();
				hdi.mask = NativeHeader.HDI_FORMAT;
				hdi.fmt = item.Format;

				HandleRef hrThis = new HandleRef(this, this.Handle);

				bool bResult = NativeHeader.SetItem(hrThis.Handle, iSection, ref hdi);
				Debug.Assert( bResult );
				if ( !bResult )
				{
					throw new InvalidOperationException(ErrMsg.FailedToChangeItem(), 
														new Win32Exception());
				}
			}
		}

		internal void _OnSectionImageAlignChanged(HeaderSection item)
		{
			if ( this.IsHandleCreated )
			{
				int iSection = this.colSections._FindSectionRawIndex(item);
				Debug.Assert( iSection >= 0 );

				NativeHeader.HDITEM hdi = new NativeHeader.HDITEM();
				hdi.mask = NativeHeader.HDI_FORMAT;
				hdi.fmt = item.Format;

				HandleRef hrThis = new HandleRef(this, this.Handle);

				bool bResult = NativeHeader.SetItem(hrThis.Handle, iSection, ref hdi);
				Debug.Assert( bResult );
				if ( !bResult )
				{
					throw new InvalidOperationException(ErrMsg.FailedToChangeItem(), 
														new Win32Exception());
				}
			}
		}

		internal void _OnSectionSortMarkChanged(HeaderSection item)
		{
			if ( this.IsHandleCreated )
			{
				int iSection = this.colSections._FindSectionRawIndex(item);
				Debug.Assert( iSection >= 0 );

				NativeHeader.HDITEM hdi = new NativeHeader.HDITEM();
				hdi.mask = NativeHeader.HDI_FORMAT;
				hdi.fmt = item.Format;

				HandleRef hrThis = new HandleRef(this, this.Handle);

				bool bResult = NativeHeader.SetItem(hrThis.Handle, iSection, ref hdi);
				Debug.Assert( bResult );
				if ( !bResult )
				{
					throw new InvalidOperationException(ErrMsg.FailedToChangeItem(), 
														new Win32Exception());
				}
			}
		}

		private void OnImageListRecreateHandle(object sender, EventArgs ea)
		{
			if ( IsHandleCreated )
			{
				HandleRef hrThis = new HandleRef(this, this.Handle);

				UpdateWndImageList(ref hrThis, this.imageList);
			}
		}

		private void OnDetachImageList(object sender, EventArgs ea)
		{
			if ( sender == this.imageList )
			{
				this.imageList = null;

				if ( IsHandleCreated )
				{
					HandleRef hrThis = new HandleRef(this, this.Handle);

					UpdateWndImageList(ref hrThis, this.imageList);
				}
			}
		}

		/// <summary>
		/// Events
		/// </summary>

		[
			Description("Occurs when user clicks on the section.")
		]
		public event HeaderSectionEventHandler SectionClick;
		protected virtual void OnSectionClick(HeaderSectionEventArgs ea)
		{
			if ( this.SectionClick != null )
				this.SectionClick(this, ea);
		}

		[
			Description("Occurs when user performs double clicks on the section.")
		]
		public event HeaderSectionEventHandler SectionDblClick;
		protected virtual void OnSectionDblClick(HeaderSectionEventArgs ea)
		{
			if ( this.SectionDblClick != null )
				this.SectionDblClick(this, ea);
		}

		[
			Description("Occurs when user performs double click on section's divider.")
		]
		public event HeaderSectionEventHandler DividerDblClick;
		protected virtual void OnDividerDblClick(HeaderSectionEventArgs ea)
		{
			if ( this.DividerDblClick != null )
				this.DividerDblClick(this, ea);
		}

		[
			Description("Occurs when user is about to start resizing of the section.")
		]
		public event HeaderSectionWidthConformableEventHandler BeforeSectionTrack;
		protected void OnBeforeSectionTrack(HeaderSectionWidthConformableEventArgs ea)
		{
			if ( this.BeforeSectionTrack != null )
			{
				Delegate[] aHandlers = this.BeforeSectionTrack.GetInvocationList();
        
				foreach( HeaderSectionWidthConformableEventHandler handler in aHandlers )
				{
					try
					{
						handler(this, ea);
					}
					catch ( Exception )
					{
						ea.Accepted = false;
					}
       
					if ( !ea.Accepted )
						break;
				}
			}
		}

		[
			Description("Occurs when user is resizing the section.")
		]
		public event HeaderSectionWidthConformableEventHandler SectionTracking;
		protected void OnSectionTracking(HeaderSectionWidthConformableEventArgs ea)
		{
			if ( this.SectionTracking != null )
			{
				Delegate[] aHandlers = this.SectionTracking.GetInvocationList();
        
				foreach( HeaderSectionWidthConformableEventHandler handler in aHandlers )
				{
					try
					{
						handler(this, ea);
					}
					catch ( Exception )
					{
						ea.Accepted = false;
					}
       
					if ( !ea.Accepted )
						break;
				}
			}
		}

		[
			Description("Occurs when user has section resized.")
		]
		public event HeaderSectionWidthEventHandler AfterSectionTrack;
		protected virtual void OnAfterSectionTrack(HeaderSectionWidthEventArgs ea)
		{
			if ( this.AfterSectionTrack != null )
				this.AfterSectionTrack(this, ea);
		}

		[
			Description("Occurs when user is about to start dragging of the " + 
						      "section to another position.")
		]
		public event HeaderSectionOrderConformableEventHandler BeforeSectionDrag;
		protected void OnBeforeSectionDrag(HeaderSectionOrderConformableEventArgs ea)
		{
			if ( this.BeforeSectionDrag != null )
			{
				Delegate[] aHandlers = this.BeforeSectionDrag.GetInvocationList();
        
				foreach( HeaderSectionOrderConformableEventHandler handler in aHandlers )
				{
					try
					{
						handler(this, ea);
					}
					catch ( Exception )
					{
						ea.Accepted = false;
					}
       
					if ( !ea.Accepted )
						break;
				}
			}
		}

		[
			Description("Occurs when user has drugged the section to another position")
		]
		public event HeaderSectionOrderConformableEventHandler AfterSectionDrag;
		protected virtual void OnAfterSectionDrag(HeaderSectionOrderConformableEventArgs ea)
		{
			if ( this.AfterSectionDrag != null )
			{
				Delegate[] aHandlers = this.AfterSectionDrag.GetInvocationList();
        
				foreach( HeaderSectionOrderConformableEventHandler handler in aHandlers )
				{
					try
					{
						handler(this, ea);
					}
					catch ( Exception )
					{
						ea.Accepted = false;
					}
       
					if ( !ea.Accepted )
						break;
				}
			}
		}

        public event HeaderCustomDrawEventHandler CustomDrawSection;
        protected virtual void OnCustomDrawSection(HeaderCustomDrawEventArgs ea)
        {
            if ( this.CustomDrawSection != null )
            {
                this.CustomDrawSection( this, ea );
            }
        }

        /// <summary>
		/// Operations
		/// </summary>
		protected override Size DefaultSize
		{
			get { return new Size(168, 24); }
		}

		protected override void CreateHandle()
		{
			if ( !this.RecreatingHandle )
			{
				InitCommonControlsHelper.Init(InitCommonControlsHelper.Classes.Header);
			}

			base.CreateHandle();
		}

		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams createParams = base.CreateParams;

				createParams.ClassName = NativeHeader.WC_HEADER;
				createParams.Style &= ~(NativeHeader.HDS_BUTTONS|
										NativeHeader.HDS_HOTTRACK|
										NativeHeader.HDS_FLAT|
										NativeHeader.HDS_DRAGDROP|
										NativeHeader.HDS_FULLDRAG);

				createParams.Style |= this.fStyle;
         
				return createParams;
			}
		}

		protected override void WndProc(ref Message msg)
		{            
			switch ( msg.Msg )
			{
			// Handle notifications
			case (NativeHeader.WM_NOTIFY + NativeHeader.OCM__BASE):
				{
					NativeWindowCommon.NMHDR nmhdr = 
						(NativeWindowCommon.NMHDR)msg.GetLParam(typeof(NativeWindowCommon.NMHDR));

					if ( nmhdr.code == NativeHeader.HDN_ITEMCHANGING )
					{
						NativeHeader.NMHEADER nmh = 
							(NativeHeader.NMHEADER)msg.GetLParam(typeof(NativeHeader.NMHEADER));

						int iSection = ExtractIndexFromNMHEADER(ref nmh);
						HeaderSection item = this.colSections._GetSectionByRawIndex(iSection);
						MouseButtons enButton = ExtractMouseButtonFromNMHEADER(ref nmh);
						int cxWidth = 0;

						NativeHeader.HDITEM2 hdi = new NativeHeader.HDITEM2();
						hdi.mask = 0;

						ExtractSectionDataFromNMHEADER(ref nmh, ref hdi);
						if ( (hdi.mask & NativeHeader.HDI_WIDTH) != 0 && this.FullDragSections )
						{
							cxWidth = hdi.cxy;

							HeaderSectionWidthConformableEventArgs ea = 
								new HeaderSectionWidthConformableEventArgs(item, enButton, cxWidth);

							OnSectionTracking(ea);

							msg.Result = ea.Accepted ? (IntPtr)0 : (IntPtr)1;
							return;
						}
					}
					else if ( nmhdr.code == NativeHeader.HDN_ITEMCHANGED )
					{
						NativeHeader.NMHEADER nmh = 
							(NativeHeader.NMHEADER)msg.GetLParam(typeof(NativeHeader.NMHEADER));

						int iSection = ExtractIndexFromNMHEADER(ref nmh);
						HeaderSection item = this.colSections._GetSectionByRawIndex(iSection);
						MouseButtons enButton = ExtractMouseButtonFromNMHEADER(ref nmh);
						int cxWidth = 0;

						NativeHeader.HDITEM2 hdi = new NativeHeader.HDITEM2();
						hdi.mask = 0;

						ExtractSectionDataFromNMHEADER(ref nmh, ref hdi);
						if ( (hdi.mask & NativeHeader.HDI_WIDTH) != 0 && this.FullDragSections )
						{
							cxWidth = hdi.cxy;

							HeaderSectionWidthEventArgs ea = 
								new HeaderSectionWidthEventArgs(item, enButton, cxWidth);

							item._SetWidth(cxWidth);

							OnAfterSectionTrack(ea);
						}				
					}
					else if ( nmhdr.code == NativeHeader.HDN_ITEMCLICK )
					{
						NativeHeader.NMHEADER nmh = 
							(NativeHeader.NMHEADER)msg.GetLParam(typeof(NativeHeader.NMHEADER));

						int iSection = ExtractIndexFromNMHEADER(ref nmh);
						MouseButtons enButton = ExtractMouseButtonFromNMHEADER(ref nmh);
						HeaderSection item = this.colSections._GetSectionByRawIndex(iSection);

						HeaderSectionEventArgs ea = new HeaderSectionEventArgs(item, enButton);

						OnSectionClick(ea);
					}
					else if ( nmhdr.code == NativeHeader.HDN_ITEMDBLCLICK )
					{
						NativeHeader.NMHEADER nmh = 
							(NativeHeader.NMHEADER)msg.GetLParam(typeof(NativeHeader.NMHEADER));

						int iSection = ExtractIndexFromNMHEADER(ref nmh);
						MouseButtons enButton = ExtractMouseButtonFromNMHEADER(ref nmh);
						HeaderSection item = this.colSections._GetSectionByRawIndex(iSection);

						HeaderSectionEventArgs ea = new HeaderSectionEventArgs(item, enButton);

						OnSectionDblClick(ea);
					}
					else if ( nmhdr.code == NativeHeader.HDN_DIVIDERDBLCLICK )
					{
						NativeHeader.NMHEADER nmh = 
							(NativeHeader.NMHEADER)msg.GetLParam(typeof(NativeHeader.NMHEADER));

						int iSection = ExtractIndexFromNMHEADER(ref nmh);
						MouseButtons enButton = ExtractMouseButtonFromNMHEADER(ref nmh);
						HeaderSection item = this.colSections._GetSectionByRawIndex(iSection);

						HeaderSectionEventArgs ea = new HeaderSectionEventArgs(item, enButton);

						OnDividerDblClick(ea);
					}
					else if ( nmhdr.code == NativeHeader.HDN_BEGINTRACK )
					{
						NativeHeader.NMHEADER nmh = 
							(NativeHeader.NMHEADER)msg.GetLParam(typeof(NativeHeader.NMHEADER));

						int iSection = ExtractIndexFromNMHEADER(ref nmh);
						HeaderSection item = this.colSections._GetSectionByRawIndex(iSection);
						MouseButtons enButton = ExtractMouseButtonFromNMHEADER(ref nmh);
						int cxWidth = 0;

						NativeHeader.HDITEM2 hdi = new NativeHeader.HDITEM2();
						hdi.mask = 0;

						ExtractSectionDataFromNMHEADER(ref nmh, ref hdi);
						if ( (hdi.mask & NativeHeader.HDI_WIDTH) != 0 )
						{
							cxWidth = hdi.cxy;
						}

						HeaderSectionWidthConformableEventArgs ea = 
							new HeaderSectionWidthConformableEventArgs(item, enButton, cxWidth);

						OnBeforeSectionTrack(ea);

						msg.Result = ea.Accepted ? (IntPtr)0 : (IntPtr)1;
						return;
					}
					else if ( nmhdr.code == NativeHeader.HDN_TRACK )
					{
						NativeHeader.NMHEADER nmh = 
							(NativeHeader.NMHEADER)msg.GetLParam(typeof(NativeHeader.NMHEADER));

						int iSection = ExtractIndexFromNMHEADER(ref nmh);
						HeaderSection item = this.colSections._GetSectionByRawIndex(iSection);
						MouseButtons enButton = ExtractMouseButtonFromNMHEADER(ref nmh);
						int cxWidth = 0;

						NativeHeader.HDITEM2 hdi = new NativeHeader.HDITEM2();
						hdi.mask = 0;

						ExtractSectionDataFromNMHEADER(ref nmh, ref hdi);
						if ( (hdi.mask & NativeHeader.HDI_WIDTH) != 0 )
						{
							cxWidth = hdi.cxy;
						}

						HeaderSectionWidthConformableEventArgs ea = 
							new HeaderSectionWidthConformableEventArgs(item, enButton, cxWidth);

						OnSectionTracking(ea);

						msg.Result = ea.Accepted ? (IntPtr)0 : (IntPtr)1;
						return;
					}
					else if ( nmhdr.code == NativeHeader.HDN_ENDTRACK )
					{
						NativeHeader.NMHEADER nmh = 
							(NativeHeader.NMHEADER)msg.GetLParam(typeof(NativeHeader.NMHEADER));

						int iSection = ExtractIndexFromNMHEADER(ref nmh);
						HeaderSection item = this.colSections._GetSectionByRawIndex(iSection);
						MouseButtons enButton = ExtractMouseButtonFromNMHEADER(ref nmh);
						int cxWidth = 0;

						NativeHeader.HDITEM2 hdi = new NativeHeader.HDITEM2();
						hdi.mask = 0;

						ExtractSectionDataFromNMHEADER(ref nmh, ref hdi);
						if ( (hdi.mask & NativeHeader.HDI_WIDTH) != 0 )
						{
							cxWidth = hdi.cxy;
						}

						HeaderSectionWidthEventArgs ea = 
							new HeaderSectionWidthEventArgs(item, enButton, cxWidth);

						item._SetWidth(cxWidth);

						OnAfterSectionTrack(ea);
					}
					else if ( nmhdr.code == NativeHeader.HDN_BEGINDRAG )
					{
						NativeHeader.NMHEADER nmh = 
							(NativeHeader.NMHEADER)msg.GetLParam(typeof(NativeHeader.NMHEADER));

						int iSection = ExtractIndexFromNMHEADER(ref nmh);
                        if ( iSection < 0 )
                        {
                            return;
                        }
						HeaderSection item = this.colSections._GetSectionByRawIndex(iSection);
						MouseButtons enButton = MouseButtons.Left; // Microsoft bugfix
						// MouseButtons enButton = ExtractMouseButtonFromNMHEADER(ref nmh);

						int iOrder = this.colSections.IndexOf(item);

						HeaderSectionOrderConformableEventArgs ea = 
							new HeaderSectionOrderConformableEventArgs(item, enButton, iOrder);

						OnBeforeSectionDrag(ea);

						msg.Result = ea.Accepted ? (IntPtr)0 : (IntPtr)1;
						return;
					}
					else if ( nmhdr.code == NativeHeader.HDN_ENDDRAG )
					{
						NativeHeader.NMHEADER nmh = 
							(NativeHeader.NMHEADER)msg.GetLParam(typeof(NativeHeader.NMHEADER));

						int iSection = ExtractIndexFromNMHEADER(ref nmh);
						HeaderSection item = this.colSections._GetSectionByRawIndex(iSection);
						MouseButtons enButton = ExtractMouseButtonFromNMHEADER(ref nmh);

						NativeHeader.HDITEM2 hdi = new NativeHeader.HDITEM2();
						hdi.mask = 0;

						ExtractSectionDataFromNMHEADER(ref nmh, ref hdi);
						Debug.Assert( (hdi.mask & NativeHeader.HDI_ORDER) != 0 );
						int iNewOrder = hdi.iOrder;

					    bool accepted = HandleEndDrag( item, enButton, iNewOrder );

					    msg.Result = accepted ? (IntPtr)0 : (IntPtr)1;
						return;
					}
                    else if ( nmhdr.code == NativeHeader.NM_CUSTOMDRAW )
                    {
                        NativeHeader.NMCUSTOMDRAW customDraw = (NativeHeader.NMCUSTOMDRAW)msg.GetLParam(typeof(NativeHeader.NMCUSTOMDRAW));
                        switch( customDraw.dwDrawStage )
                        {   
                            case NativeHeader.CDDS_PREPAINT:
                                msg.Result = (IntPtr) NativeHeader.CDRF_NOTIFYITEMDRAW;
                                return;

                            case NativeHeader.CDDS_ITEMPREPAINT:
                                msg.Result = (IntPtr) NativeHeader.CDRF_NOTIFYPOSTPAINT;
                                return;

                            case NativeHeader.CDDS_ITEMPOSTPAINT:
                                HeaderSection section = this.colSections._GetSectionByRawIndex(customDraw.dwItemSpec);
                                Rectangle rc = Rectangle.FromLTRB( customDraw.rcLeft, customDraw.rcTop, 
                                    customDraw.rcRight, customDraw.rcBottom );
                                HeaderCustomDrawEventArgs ea = new HeaderCustomDrawEventArgs( section, customDraw.hdc, rc );
                                OnCustomDrawSection( ea );
                                msg.Result = (IntPtr) NativeHeader.CDRF_DODEFAULT;
                                return;
                        }
                    }
					          
					//		  else if ( nmhdr.code == NativeHeader.HDN_GETDISPINFO )
					//		  {
					//		  }
					//		  else if ( nmhdr.code == NativeHeader.HDN_FILTERCHANGE )
					//		  {
					//		  }
					//		  else if ( nmhdr.code == NativeHeader.HDN_FILTERBTNCLICK )
					//		  {
					//		  }
				}
				break;

			case NativeHeader.WM_SETCURSOR:
				DefWndProc(ref msg);
				return;
			}

			base.WndProc(ref msg);
		}

	    public bool HandleEndDrag( HeaderSection item, MouseButtons enButton, int iNewOrder )
	    {
	        HeaderSectionOrderConformableEventArgs ea = 
	            new HeaderSectionOrderConformableEventArgs(item, enButton, iNewOrder);
    
	        OnAfterSectionDrag(ea);
    
	        // Update orders
	        if ( ea.Accepted )
	        {
	            int iOldOrder = this.colSections.IndexOf(item);
                if ( iOldOrder >= 0 )
                {
                    this.colSections._Move(iOldOrder, iNewOrder);
                }
	        }
	        return ea.Accepted;
	    }

	    /// <summary>
		/// Operations
		/// </summary>

		public void BeginUpdate()
		{
			if ( this.IsHandleCreated )
			{
				HandleRef hrThis = new HandleRef(this, this.Handle);

				NativeWindowCommon.SendMessage(hrThis.Handle, NativeWindowCommon.WM_SETREDRAW, 
											   0, 0);
			}
		}

		public void EndUpdate()
		{
			if ( this.IsHandleCreated )
			{
				HandleRef hrThis = new HandleRef(this, this.Handle);

				NativeWindowCommon.SendMessage(hrThis.Handle, NativeWindowCommon.WM_SETREDRAW, 
											   1, 0);
			}
		}

		public Rectangle GetSectionRect(HeaderSection item)
		{
			int iSection = this.colSections._FindSectionRawIndex(item);
			Debug.Assert( iSection >= 0 );

			HandleRef hrThis = new HandleRef(this, this.Handle);

			NativeHeader.RECT rc;
			bool bResult = NativeHeader.GetItemRect(hrThis.Handle, iSection, out rc);
			Debug.Assert( bResult );
			if ( !bResult )
				throw new Win32Exception();

			return Rectangle.FromLTRB(rc.left, rc.top, rc.right, rc.bottom);
		}

		public void CalculateLayout(Rectangle rectArea, out Rectangle rectPosition)
		{    
			NativeHeader.HDLAYOUT hdl = new NativeHeader.HDLAYOUT();
			hdl.prc = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(NativeHeader.RECT)));
			hdl.pwpos = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(NativeHeader.WINDOWPOS)));

			try
			{
				HandleRef hrThis = new HandleRef(this, this.Handle);

				NativeHeader.RECT rc = new NativeHeader.RECT();     
				rc.left = rectArea.Left;
				rc.top = rectArea.Top;
				rc.right = rectArea.Right;
				rc.bottom = rectArea.Bottom;   

				Marshal.StructureToPtr(rc, hdl.prc, false);

				bool bResult = NativeHeader.Layout(hrThis.Handle, ref hdl);
				Debug.Assert( bResult );
				if ( !bResult )
					throw new Win32Exception();

				NativeHeader.WINDOWPOS wp = 
					(NativeHeader.WINDOWPOS)Marshal.PtrToStructure(hdl.pwpos, 
														typeof(NativeHeader.WINDOWPOS));

				rectPosition = new Rectangle(wp.x, wp.y, wp.cx, wp.cy);
			}
			finally
			{
				if ( hdl.prc != IntPtr.Zero )
					Marshal.FreeHGlobal(hdl.prc);

				if ( hdl.pwpos != IntPtr.Zero )
					Marshal.FreeHGlobal(hdl.pwpos);
			}
		}

		public int SetHotDivider(int x, int y)
		{
			HandleRef hrThis = new HandleRef(this, this.Handle);

			return NativeHeader.SetHotDivider(hrThis.Handle, true, (y << 16) | x);
		}

		public int SetHotDivider(int iDevider)
		{
			HandleRef hrThis = new HandleRef(this, this.Handle);

			return NativeHeader.SetHotDivider(hrThis.Handle, false, iDevider);
		}

		public HitTestInfo HitTest(int x, int y)
		{
			return HitTest(new Point(x, y));
		}

		public HitTestInfo HitTest(Point point)
		{
			HandleRef hrThis = new HandleRef(this, this.Handle);

			Point pointClient = PointToClient(point);

			NativeHeader.HDHITTESTINFO htiRaw = new NativeHeader.HDHITTESTINFO();
			htiRaw.pt.x = pointClient.X;
			htiRaw.pt.y = pointClient.Y;
			htiRaw.iItem = -1;
			htiRaw.flags = 0;
    
			NativeHeader.HitTest(hrThis.Handle, ref htiRaw);
     
			HitTestInfo hti = new HitTestInfo();
			hti.fArea = (HitTestArea)htiRaw.flags;

			if ( htiRaw.iItem >= 0 )
			{
				hti.section = this.colSections._GetSectionByRawIndex(htiRaw.iItem);
			}

			return hti;
		}

	}

#endregion // Header control
}
