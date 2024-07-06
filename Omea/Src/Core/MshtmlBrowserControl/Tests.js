// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// JetBrains Omea Mshtml Browser Component
//
// Some test cases for the classes in this assembly.
//
// This file belongs to the managed part and implements the MshtmlBrowserControl class.
// This file contains the implementation of the Windows Forms Web browser control and concentrates all the component logic. Other entites, such as unmanaged part and the AbstractWebBrowser proxy-inheritor, should not carry out any meaningful processing.
// The MshtmlBrowserControl class consumes MSHTML/WebBrowser events delegated by the unmanaged part, responds to the IWebBrower interface members invocation and fires the IWebBrowser events.
//
// © JetBrains Inc, 2004—2005
// Written by (H) Serge Baltic
//
@if(@DEBUG)
import NUnit.Framework;
import JetBrains.Omea.GUIControls.MshtmlBrowser;
import System.Text.RegularExpressions;

package JetBrains.Omea.GUIControls.MshtmlBrowser.Tests
{
	public NUnit.Framework.TestFixtureAttribute class Hilite
	{
		var _words : ArrayList;	// List of the WordPtr to submit to highlighter
		var	_hashColoring : Hashtable;	// Imitates the hiliter coloring logic
		var	_currentColor : int;	// Imitates the hiliter coloring logic
		var	_contentIn : StringBuilder;	// Builds the input content
		var	_contentOut : StringBuilder;	// Builds the expected output content
		var	_offset : int;	// Current offset in the input content
		var	_rand : Random;

		public SetUpAttribute function Setup() : void
		{
			_words = new ArrayList();
			_hashColoring = new Hashtable();
			_contentIn = new StringBuilder();
			_contentOut = new StringBuilder();
			_offset = 0;
			_currentColor = 0;
			_rand = new Random(0x100);

			CreateColors(3);
		}

		public TearDownAttribute function Shutdown() : void
		{
			_words = null;
			_hashColoring = null;
			_contentIn = null;
			_contentOut = null;
		}

		// Create some <num> human-readable colors and assign to the browser object
		public function CreateColors( num : int ) : void
		{
			var	colors : MshtmlBrowserControl.BiColor[] = new MshtmlBrowserControl.BiColor[ num ];

			for(var a : int = 0; a < num; a++)
				colors[a] = new MshtmlBrowserControl.BiColor(String.Format("Hilite{0}F", a), String.Format("Hilite{0}B", a));

			MshtmlBrowserControl.HiliteColors = colors;
		}

		// Adds some text/html to the input content, not intended for matching
		public function AddText( text : System.String ) : void
		{
			_contentIn.Append(text);
			_contentOut.Append(text);
			_offset += text.Length;
		}

		// Adds a search hit to the content and the expected form to the output content.
		// match — search result, as if received from the text index
		// original — for checking the highlighting in common color
		// escaped — search result, as it occurs in the text (may have entities)
		public function AddMatch( match : System.String, original : System.String, escaped : System.String ) : void
		{
			if(original == null)
				original = match;
			if(escaped == null)
				escaped = match;

			var	word : WordPtr;
			word.StartOffset = _offset;
			word.Text = match;
			word.Original = original;
			_words.Add(word);

			// Guess the color
			var color : MshtmlBrowserControl.BiColor;
			if( _hashColoring.ContainsKey( word.Original ) )	// Source already known and has a color assigned
				color = _hashColoring[ word.Original ];
			else	// Assign a new color
			{
				color = MshtmlBrowserControl.HiliteColors[ _currentColor++ ];
				_currentColor = _currentColor < MshtmlBrowserControl.HiliteColors.Length ? _currentColor : 0;	// Wrap around
				_hashColoring[ word.Original ] = color;
			}

			_contentIn.Append(escaped);

			_contentOut.Append(String.Format("<span style=\"color: {0}; background-color: {1};\" title=\"Search result for {2}\">", color.ForeColor, color.BackColor, word.Original));
			_contentOut.Append(escaped);
			_contentOut.Append( "</span>" );

			_offset += escaped.Length;
		}

		// Invokes hilite and checks the result
		// Forces the catchup (shifts offsets a little bit) if specified
		public function DoCheck( bForceCatchup ) : void
		{
			var	word : WordPtr;
			var	wordsPtr : WordPtr[] = new WordPtr[_words.Count];
			for( var a : int = 0; a < _words.Count; a++ )
			{
				word = _words[a];
				if( (bForceCatchup) && (NoDupeChars(word.Text)) )	// Don't shift words that have duplicate characters — they're not guaranteed to succeed in finding
				{
					word.StartOffset -= _rand.Next(word.Text.Length);	// Shift at no more than the word length
					word.StartOffset = word.StartOffset > 0 ? word.StartOffset : 0;	// Don't allow to be negative
				}
				wordsPtr[a] = word;
			}

			var	result : System.String = MshtmlBrowserControl.HiliteMain(_contentIn.ToString(), wordsPtr);

			Assert.IsNotNull(result);

			// Strip out the span IDs which are random and cannot be checked by equality
			var regex : Regex = new Regex(" id\=\"SearchHit\-[a-f0-9]+\-[a-f0-9]+\-[a-f0-9]+\-[a-f0-9]+\-[a-f0-9]+\"", RegexOptions.None);
			result = regex.Replace(result, "");

			Assert.AreEqual(_contentOut.ToString(), result);
			Trace.WriteLine("[OMEA.MSHTML] Test produced: " + result);
		}

		// Checks if all the chars in the string are unique
		public static function NoDupeChars( s : System.String ) : boolean
		{
			var	hash : Hashtable = new Hashtable();
			for( var ch : char in s.GetEnumerator() )
			{
				if(hash.ContainsKey(ch))
					return false;
				hash[ch] = true;
			}
			return true;
		}

		public TestAttribute function None() : void
		{
			AddText("Hello, World!");
			DoCheck(false);
		}

		public TestAttribute function One() : void
		{
			AddText("So ");
			AddMatch("be", null, null);
			AddText(" it.");
			DoCheck(false);
			DoCheck(true);
		}

		public TestAttribute function PlainMatches() : void
		{
			AddText("Rain, oh ");
			AddMatch("Gautama", null, null);
			AddText(", is the ");
			AddMatch("fire", null, null);
			AddText(", the year its ");
			AddMatch("fuel", null, null);
			AddText(", the clouds its ");
			AddMatch("smoke", null, null);
			AddText(", the lightning its ");
			AddMatch("flame", null, null);
			AddText(", cinders, sparks.");
			DoCheck(false);
			DoCheck(true);
		}

		public TestAttribute function SimpleCatchup() : void
		{
			var	word : WordPtr;
			word.Text = "runaway";
			word.Original = "runaway";
			word.StartOffset = 0;
			var	words : WordPtr[] = new WordPtr[1];
			words[0] = word;

			var result = MshtmlBrowserControl.HiliteMain("The runaway train.", words);

			Assert.IsNotNull(result);
		}

		public TestAttribute function SimpleFallback() : void
		{
			var	word : WordPtr;
			word.Text = "runaway";
			word.Original = "runaway";
			word.StartOffset = 8;
			var	words : WordPtr[] = new WordPtr[1];
			words[0] = word;

			var result = MshtmlBrowserControl.HiliteMain("runaway train", words);

			Assert.IsTrue(result == null);
		}

		public TestAttribute function EntityMatches() : void
		{
			AddText("Rain, oh ");
			AddMatch("Gautama", null, "G&#x61;ut&#97;ma");
			AddText(", is the ");
			AddMatch("fire", null, "f&#x69;re");
			AddText(", the year its ");
			AddMatch("fuel", null, "&#102;ue&#x6C;");
			AddText(", the clouds its ");
			AddMatch("smoke", null, "sm&#111;ke");
			AddText(", the lightning its ");
			AddMatch("flame", null, "f&#x6c;ame");
			AddText(", cinders, sparks.");
			DoCheck(false);
			DoCheck(true);
		}

		public TestAttribute function SameColor() : void
		{
			AddText("We ");
			AddMatch("go", "go", null);
			AddText(", ");
			AddMatch("went", "go", null);
			AddText(", ");
			AddMatch("gone", "go", "g&#111;ne");
			AddText(", ");
			AddMatch("going", "go", "g&#111;ing");
			AddText(" ");
			AddMatch("west", "west", "west");
			AddText("!");

			DoCheck(false);
			DoCheck(true);
		}
	}
}
@end
