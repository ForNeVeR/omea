/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

// CDialogAnchor — Implements anchoring of UI controls on the WinAPI dialogs.
// 
// This helps with implementing the resizeable dialogs 
// that maintain the relative layout of the conrols when resizing, 
// according to the anchoring information provided.
//
// © JetBrains Inc, 2005
// Written by (H) Serge Baltic
//
#pragma once

/// Anchoring flags that define the side of the control that keeps its distance to the window edge constant when the window resizes.
enum Anchor {anchorNone = 0, anchorLeft = 1, anchorTop = 2, anchorRight = 4, anchorBottom = 8 };

/// A class that describes each anchored control in the DialogAnchor.
class CAnchoredControl
{
// Construction
public:
	/// Initializes the control and assigns the initial values to its anchoring info.
	/// sizeClient is the size of dialog's client rectangle that is needed for gathering the initial anchoring information.
	CAnchoredControl(HWND hwnd, HWND hwndDialog, Anchor anchor, CSize sizeClient);

	// Copy
	CAnchoredControl(const CAnchoredControl &other);
	CAnchoredControl &operator=(const CAnchoredControl &other);

	~CAnchoredControl();

// Data
protected:
	/// Handle of the control being anchored.
	HWND	m_hWnd;

	/// Distances from the specific edges of the controls to the corresponding dialog client rect edges. Some of these distances should be maintained by anchoring.
	CRect	m_rcDistances;

	/// Defines which sides are anchored.
	Anchor	m_anchor;

	/// Initial dimensions of the control. Help calculating the anchorings that do not change sizes along a specific axis.
	CSize	m_sizeDimensions;

// Operations
public:
	/// Performs layouting of the control.
	void PerformLayout(HWND hwndDialog, CSize sizeClient) const;
};

/// A class that manages layouting of the anchored controls within a dialog.
class CDialogAnchor
{
public:
	CDialogAnchor(void);
	~CDialogAnchor(void);

// Declarations
public:

// Operations
public:
	/// Attaches to the given dialog. Stores its HWND for further operations and remembers the dimensions for anchoring the controls when the size changes.
	/// All the controls must be added before the dialog's size changes for the anchoring to work properly.
	void Attach(HWND hwndDialog);

	/// Adds a control to the anchoring list.
	void AddControl(HWND hwndControl, Anchor anchor);

	/// Using the information about the initial window size and initial controls' locations, updates the layout so that the anchoring is favored.
	/// Call this function whenever the attached dialog's size changes.
	void PerformLayout() const;

// Data
protected:

	/// HWND of the dialog window that is being layouted. See Attach().
	HWND	m_hwndDialog;

	/// Initial size of the dialog at the time the anchorings are registered.
	CSize	m_sizeDialog;

	/// Initial client size of the dialog at the time the anchorings are registered.
	CSize	m_sizeClient;

	/// The anchored-control data, that stores the controls' handles and anchoring information for them.
	std::vector<CAnchoredControl>	m_controls;
};

