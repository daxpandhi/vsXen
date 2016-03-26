/*
The MIT License (MIT)

Copyright (c) 2015 Nukeation Studios.

Permission is hereby granted, free of charge, to any person obtaining a copy 
of this software and associated documentation files (the "Software"), to deal 
in the Software without restriction, including without limitation the rights 
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
copies of the Software, and to permit persons to whom the Software is 
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in 
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
IN THE SOFTWARE.

https://github.com/daxpandhi/vsXen

*/

using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.HtmlControls;

namespace vsXen.XenCoding.Xaml
{
    public static class XamlElementFactory
    {
        private static readonly Regex _valueCounterRegex = new Regex(@"(\$+)", RegexOptions.Compiled);

        public static HtmlControl CloneElement(this HtmlControl element, int count)
        {
            if (element == null)
                return null;

            using (HtmlControl control = Create(element.TagName.Increment(count)))
            {
                foreach (string attr in element.Attributes.Keys.Cast<string>()) {
                    control.Attributes[attr] = element.Attributes[attr].Increment(count);

                    if (attr == "class")
                    {
                        control.Attributes[attr] = control.Attributes[attr].Increment(count);
                    }
                }

                if (!string.IsNullOrEmpty(element.ID))
                {
                    control.ID = element.ID.Increment(count);
                }

               if (element.Controls.Count == 1)
                {
                    LiteralControl literal = element.Controls[0] as LiteralControl;

                    if (literal != null)
                        control.Controls.Add(new LiteralControl(literal.Text.Increment(count)));
                }

                return control;
            }
        }

        public static string Increment(this string text, int count)
        {
            MatchCollection matches = _valueCounterRegex.Matches(text);

            checked
            {
                text = matches.Cast<Match>().Aggregate(text, (current, match) 
                    => current.Replace(match.Value, (count + 1).ToString(CultureInfo.CurrentCulture).PadLeft(match.Value.Length, '0')));
            }

            return text;
        }

        public static HtmlControl Create(string tagName)
        {
            return tagName == null ? null : new BlockXAMLControl(tagName);

            //HtmlControl control = TryCreateSepcialControl(tagName, type, isClone);

            //if (control != null)
            //    return control;

            //if (tagName.StartsWith("lorem", StringComparison.Ordinal) ||
            //    (type == typeof(LoremControl) && isClone))
            //{
            //    return new LoremControl(isClone ? "lorem0" : tagName);
            //}
        }
    }
}
