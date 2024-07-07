// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Drawing;

using NUnit.Framework;

namespace JetBrains.UI.Tests.RichText
{
	/// <summary>
	/// Tests the <see cref="JetBrains.UI.RichText.RichText"/> class
	/// </summary>
	[TestFixture]
	public class RichTextTest
	{
    private JetBrains.UI.RichText.RichTextParameters myParameters = new JetBrains.UI.RichText.RichTextParameters(new Font("Arial", 9));

	  [Test]
    public void TestConstructorAndProperties()
    {
      JetBrains.UI.RichText.RichText text = new JetBrains.UI.RichText.RichText("Sample string", myParameters);

      Assertion.AssertEquals("Sample string", text.Text);
      Assertion.AssertEquals("Sample string".Length, text.Length);
      Assertion.AssertEquals("Sample string", text.ToString());
    }

    [Test]
    public void TestAppend()
    {
      JetBrains.UI.RichText.RichText text = new JetBrains.UI.RichText.RichText("Sample string", myParameters);

      text.Append(" with an addition");

      Assertion.AssertEquals("Sample string with an addition", text.Text);
      Assertion.AssertEquals("Sample string with an addition".Length, text.Length);
    }

    [Test]
    public void TestSetStyleTextInvariance()
    {
      JetBrains.UI.RichText.RichText text = new JetBrains.UI.RichText.RichText("Sample string", myParameters);

      text.SetStyle(new JetBrains.UI.RichText.TextStyle(FontStyle.Bold, Color.White, Color.Red), 2, 7);

      Assertion.AssertEquals("Sample string", text.Text);
      Assertion.AssertEquals("Sample string".Length, text.Length);
      Assertion.AssertEquals("Sa|mple st|ring", text.ToString());
    }
	}
}
