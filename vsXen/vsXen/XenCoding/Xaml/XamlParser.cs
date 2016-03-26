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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using Xen = vsXen.vsXenPackage;

namespace vsXen.XenCoding.Xaml
{
    public static class XamlParser
    {
        private static char[] _attr = { '#', '.', '[', '{' };
        private static char[] _elem = { '>', '+', '^' };
        private static char[] _strips = { '>', '+', '^', '$', '#', '.', '[', '{', '*', '-' };
        private static Regex _shortcuts = new Regex(@"^([\w]+):([\w]+)$", RegexOptions.Compiled);
        private static Regex _emptyingComponent = new Regex("{([^}]+)}", RegexOptions.Compiled);
        private static Regex _collapseMultipleLinefeeds = new Regex("([" + Environment.NewLine + "]+)", RegexOptions.Compiled);
        private static Dictionary<string, string> _shorts = new Dictionary<string, string>();

        static XamlParser()
        {
            // SHORTCUTS > Containers
            _shorts.Add("bd", "Border");
            _shorts.Add("dock", "DockPanel");
            _shorts.Add("rel", "RelativePanel");
            _shorts.Add("scroll", "ScrollViewer");
            _shorts.Add("stack", "StackPanel");
            _shorts.Add("uniform", "UniformPanel");
            _shorts.Add("wrap", "WrapPanel");
            _shorts.Add("grp", "GroupBox");

            // SHORTCUTS > Controls
            _shorts.Add("btn", "Button");
            _shorts.Add("check", "CheckBox");
            _shorts.Add("ci", "ComboBoxItem");
            _shorts.Add("combo", "ComboBox");
            _shorts.Add("cp", "ContentPresenter");
            _shorts.Add("date", "DatePicker");
            _shorts.Add("li", "ListBoxItem");
            _shorts.Add("list", "ListBox");
            _shorts.Add("mi", "MenuItem");
            _shorts.Add("radio", "RadioButton");
            _shorts.Add("rect", "Rectangle");
            _shorts.Add("rtb", "RichTextBox");
            _shorts.Add("rtf", "RichTextBox");
            _shorts.Add("split", "GridSplitter");
            _shorts.Add("tab", "TabItem");
            _shorts.Add("tabs", "TabControl");
            _shorts.Add("tb", "TextBlock");
            _shorts.Add("ti", "TreeViewItem");
            _shorts.Add("txt", "TextBox");

            // Others
            _shorts.Add("ct", "ControlTemplate");
            _shorts.Add("set", "Setter");
            _shorts.Add("setter", "Setter");
            _shorts.Add("style", "Style");
            _shorts.Add("trig", "Trigger");
            _shorts.Add("trigger", "Trigger");

            // ALL CONTROLS
            _shorts.Add("border", "Border");
            _shorts.Add("button", "Button");
            _shorts.Add("calendar", "Calendar");
            _shorts.Add("canvas", "Canvas");
            _shorts.Add("checkbox", "CheckBox");
            _shorts.Add("combobox", "ComboBox");
            _shorts.Add("comboboxitem", "ComboBoxItem");
            _shorts.Add("contentcontrol", "ContentControl");
            _shorts.Add("datagrid", "DataGrid");
            _shorts.Add("datepicker", "DatePicker");
            _shorts.Add("dockpanel", "DockPanel");
            _shorts.Add("documentviewer", "DocumentViewer");
            _shorts.Add("ellipse", "Ellipse");
            _shorts.Add("expander", "Expander");
            _shorts.Add("frame", "Frame");
            _shorts.Add("grid", "Grid");
            _shorts.Add("gridsplitter", "GridSplitter");
            _shorts.Add("groupbox", "GroupBox");
            _shorts.Add("image", "Image");
            _shorts.Add("label", "Label");
            _shorts.Add("listbox", "ListBox");
            _shorts.Add("listboxitem", "ListBoxItem");
            _shorts.Add("listview", "ListView");
            _shorts.Add("mediaelement", "MediaElement");
            _shorts.Add("menu", "Menu");
            _shorts.Add("menuitem", "MenuItem");
            _shorts.Add("passwordbox", "PasswordBox");
            _shorts.Add("progressbar", "ProgressBar");
            _shorts.Add("radiobutton", "RadioButton");
            _shorts.Add("rectangle", "Rectangle");
            _shorts.Add("relativepanel", "RelativePanel");
            _shorts.Add("richtextbox", "RichTextBox");
            _shorts.Add("scrollbar", "ScrollBar");
            _shorts.Add("scrollviewer", "ScrollViewer");
            _shorts.Add("separator", "Separator");
            _shorts.Add("slider", "Slider");
            _shorts.Add("stackpanel", "StackPanel");
            _shorts.Add("statusbar", "StatusBar");
            _shorts.Add("tabcontrol", "TabControl");
            _shorts.Add("textblock", "TextBlock");
            _shorts.Add("textbox", "TextBox");
            _shorts.Add("toggle", "ToggleButton");
            _shorts.Add("toolbar", "ToolBar");
            _shorts.Add("toolbarpanel", "ToolBarPanel");
            _shorts.Add("toolbartray", "ToolBarTray");
            _shorts.Add("treeview", "TreeView");
            _shorts.Add("treeviewitem", "TreeViewItem");
            _shorts.Add("viewbox", "Viewbox");
            _shorts.Add("webbrowser", "WebBrowser");
            _shorts.Add("windowsformshost", "WindowsFormsHost");
            _shorts.Add("wrappanel", "WrapPanel");

            try
            {
                foreach (var Short in Xen.Options.Core.Shorts)
                {
                    try
                    {
                        _shorts.Add(Short.Key, Short.Value);
                    }
                    catch (ArgumentException)
                    {
                        // do nothing
                    }
                }

            }
            catch (Exception)
            {

            }
        }

        public static bool IsValid(string zenSyntax)
        {
            if (string.IsNullOrEmpty(zenSyntax) || zenSyntax.StartsWith("asp:", StringComparison.OrdinalIgnoreCase))
                return false;

            int indexSpace = zenSyntax.IndexOf(' ');

            if (indexSpace > -1 && (indexSpace < zenSyntax.IndexOfAny(new[] { '[', '{', '"', '.' }) || indexSpace > zenSyntax.LastIndexOfAny(new[] { ']', '}', '"' })))
                return false;

            if ((zenSyntax.Contains("{") || zenSyntax.Contains("}")) && (zenSyntax.Count(c => c == '{') != zenSyntax.Count(c => c == '}')))
                return false;

            if (zenSyntax.Contains("<") || zenSyntax.Contains("|") || zenSyntax.Contains("@"))
                return false;

            if (!zenSyntax.StartsWith("place", StringComparison.CurrentCultureIgnoreCase))
            {
                char last = zenSyntax.Last();

                if (!char.IsLetterOrDigit(last) && last != ']' && last != '}' && last != '+' && !char.IsWhiteSpace(last))
                    return false;
            }

            return zenSyntax.Count(z => z == ']') == zenSyntax.Count(z => z == '[');
        }

        public static string Parse(string zenSyntax)
        {
            string result = ParseGroup(zenSyntax);

            // Redudant loop to ensure we catch any tag matches
            for (int i = 0; i < _shorts.Count; i++)
            {
                result = result.Replace($"<{_shorts.ElementAt(i).Key}>", $"<{_shorts.ElementAt(i).Value}>");
                result = result.Replace($"</{_shorts.ElementAt(i).Key}>", $"</{_shorts.ElementAt(i).Value}>");
                result = result.Replace($"<{_shorts.ElementAt(i).Key} ", $"<{_shorts.ElementAt(i).Value} ");
            }

            return result;
        }

        public static string ParseGroup(string zenSyntax)
        {
            if (!IsValid(zenSyntax))
                return string.Empty;

            Control root = null;

            try
            {
                root = new Control();
                List<string> parts = GetSubParts(zenSyntax, _elem);

                //AdjustImplicitTagNames(parts);
                foreach (var s in _shorts)
                {
                    for (int index = 0; index < parts.Count; index++)
                    {
                        string p = parts[index];

                        p = _strips.Aggregate(p, (current1, strip) => StripToFirst(current1, strip.ToString()));
                        string pLow = p.ToLower(); //.Replace(">", string.Empty);

                        if (pLow == s.Key)
                        {
                            int length = parts[index].StartsWith(">") ? p.Length + 1 : p.Length;

                            //length = parts[index].StartsWith("^+") ? p.Length + 2 : length;
                            string firstPart = parts[index].Substring(0, length).Replace(p, s.Value);
                            string secondPart = parts[index].Substring(length);
                            parts[index] = firstPart + secondPart;
                        }
                    }
                }

                // TODO: Add option to omit
                //if (!IsValidHtmlElements(parts))
                //    return string.Empty;

                IEnumerable<Control> current = new[] { root };

                BuildControlTree(CloneStack(parts), current.First(), -1);

                return RenderControl(root);
            }
            finally
            {
                root?.Dispose();
            }
        }

        private static string StripToFirst(string p, string x)
        {
            string returnValue = p;
            if (p.Contains(x) && p.Length > 3)
            {
                returnValue = p.Split(new[] { x }, StringSplitOptions.RemoveEmptyEntries)[0];
                if (returnValue.Length < 3)
                    returnValue = p.Split(new[] { x }, StringSplitOptions.RemoveEmptyEntries)[1];
            }
            return returnValue;
        }

        [SuppressMessage("ReSharper", "RedundantAssignment")]
        private static void BuildControlTree(Stack<string> parts, Control control, int nestedCounter)
        {
            if (!parts.Any())
                return;

            string name;
            var part = parts.Pop();
            int count = GetCountAndName(part, out name, _attr);
            var htmlControl = GenerateElement(part, name);
            bool foundMaintainLevelSemaphore = false;
            bool foundLevelUpSemaphore = false;

            for (int i = 0; i < count; ++i)
            {
                var clone = htmlControl.CloneElement(count > 1 || nestedCounter == -1 ? i : nestedCounter);
                clone.SkinID = Guid.NewGuid().ToString();
                control.Controls.Add(clone);

                if (parts.Any() && parts.Peek()[0] == '+')
                {
                    foundMaintainLevelSemaphore = true;
                }
                else if (parts.Any() && parts.Peek()[0] == '^')
                {
                    foundLevelUpSemaphore = true;
                }
                else // must be ">" meaning; going down!
                {
                    if (count > 1)
                        nestedCounter = i;

                    BuildControlTree(CloneStack(parts), FindControlBySkinId(control, clone.SkinID), nestedCounter);

                    nestedCounter = -1;
                }
            }

            if (foundMaintainLevelSemaphore)
            {
                BuildControlTree(CloneStack(parts), control, nestedCounter);
            }
            else if (foundLevelUpSemaphore)
            {
                Control parent = control;

                for (int j = 0; j < parts.Peek().Count(a => a == '^'); j++)
                {
                    if (parent.Parent == null)
                        break;

                    parent = parent.Parent;
                }

                BuildControlTree(CloneStack(parts), parent, nestedCounter);
            }

            nestedCounter = -1;
        }

        private static Stack<T> CloneStack<T>(IEnumerable<T> collection)
        {
            return new Stack<T>(collection.Reverse());
        }

        private static Control FindControlBySkinId(Control root, string id)
        {
            return root.SkinID == id ? root : (from Control c in root.Controls select FindControlBySkinId(c, id)).FirstOrDefault(t => t != null);
        }

        private static HtmlControl GenerateElement(string part, string name)
        {
            HtmlControl element = !_shortcuts.IsMatch(name) ? CreateElementWithAttributes(part, name) : ShortcutHelper.Parse(part);
            return element;
        }

        private static HtmlControl CreateElementWithAttributes(string part, string name)
        {
            using (HtmlControl element = XamlElementFactory.Create(name))
            {
                List<string> subParts = GetSubParts(part, _attr);

                foreach (string subPart in subParts)
                {
                    // Class
                    switch (subPart[0])
                    {
                        case '.':
                            AddClass(element, subPart);
                            break;

                        case '#':
                            AddId(element, subPart);
                            break;

                        case '[':
                            AddAttributes(element, subPart);
                            break;

                        case '{':
                            AddInnerText(element, subPart);
                            break;
                    }
                }

                switch (element.TagName.ToLower())
                {

                    case "setter":
                    case "trigger":
                        element.Attributes["Property"] = "";
                        element.Attributes["Value"] = "";
                        break;
                }


                return element;
            }
        }

        private static void AddInnerText(Control element, string subPart)
        {
            string clean = subPart.Substring(1, subPart.IndexOf('}') - 1);

            LiteralControl lit = new LiteralControl(clean);
            element.Controls.Add(lit);
        }

        private static void AddAttributes(HtmlControl element, string attribute)
        {
            int start = attribute.IndexOf('[');
            int end = attribute.IndexOf(']');

            if (start > -1 && end > start)
            {
                string content = attribute.Substring(start + 1, end - start - 1);
                List<string> parts = content.Trim().Split(' ').ToList();

                for (int i = parts.Count - 1; i > 0; i--)
                {
                    string part = parts[i];
                    int singleCount = part.Count(c => c == '\'');
                    int doubleCount = part.Count(c => c == '"');

                    if (((singleCount > 1 || doubleCount > 1) && !part.Contains("=")) ||
                        ((doubleCount == 1) && part.EndsWith("\"", StringComparison.Ordinal)) ||
                        ((singleCount == 1) && part.EndsWith("'", StringComparison.Ordinal)))
                    {
                        parts[i - 1] += " " + part;
                        parts.RemoveAt(i);
                    }
                }

                foreach (string[] sides in parts.Select(part => part.Split('=')))
                {
                    if (sides.Length == 1)
                    {
                        element.Attributes[sides[0]] = string.Empty;
                    }
                    else
                    {
                        sides[1] = sides[1].Trim();
                        char firstChar = sides[1][0];
                        char lastChar = sides[1][sides[1].Length - 1];
                        if ((firstChar == '\'' || firstChar == '"') && firstChar == lastChar)
                        {
                            element.Attributes[sides[0]] = sides[1].Substring(1, sides[1].Length - 2);
                        }
                        else
                        {
                            element.Attributes[sides[0]] = sides[1];
                        }
                    }
                }
            }
        }

        private static void AddId(HtmlControl element, string part)
        {
            int index = part.IndexOf('*');
            string clean = part;

            if (index > 0)
            {
                clean = clean.Substring(0, index);
            }
            element.Attributes["x:Name"] = clean.TrimStart(_attr);
        }

        private static void AddClass(HtmlControl element, string className)
        {
            string clean = className.TrimStart(_attr);
            int index = clean.IndexOf('*');

            if (index > 0)
            {
                clean = clean.Substring(0, index);
            }

            element.Attributes["Style"] = $"{{DynamicResource {clean}}}"; //element.Attributes["class"];


        }

        private static int GetCountAndName(string part, out string cleanPart, char[] symbols)
        {
            int index = part.IndexOf('*');
            int count = 1;

            if (index > -1 && part.Length > index + 1 && !char.IsNumber(part[index + 1]))
                index = -1;

            if (index > -1 && int.TryParse(part.Substring(index + 1), out count))
            {
                string[] subParts = part.Split(symbols, StringSplitOptions.RemoveEmptyEntries);
                cleanPart = subParts[0];

                if (index < cleanPart.Length)
                {
                    cleanPart = cleanPart.Substring(0, index);
                }

                cleanPart = cleanPart.Trim(_attr).Trim(_elem);
            }
            else
            {
                string[] subParts = part.Split(_attr, StringSplitOptions.RemoveEmptyEntries);
                cleanPart = subParts[0].TrimStart(_attr).Trim(_elem);
            }

            cleanPart = _emptyingComponent.Replace(cleanPart, string.Empty);

            return count;
        }

        private static List<string> GetSubParts(string zenSyntax, char[] symbols)
        {
            List<string> parts = GetParts(zenSyntax, symbols);

            for (int i = parts.Count - 1; i > 0; i--)
            {
                string part = parts[i];

                if ((part.Contains("]") && !part.Contains("[")) || (part.Contains("}") && !part.Contains("{")))
                {
                    parts[i - 1] += part;
                    parts.RemoveAt(i);
                }
            }

            return parts;
        }

        private static List<string> GetParts(string zenSyntax, char[] symbols)
        {
            List<string> parts = new List<string>();
            int index = 0;

            for (int i = 0; i < zenSyntax.Length; i++)
            {
                char c = zenSyntax[i];

                if (i > 0 && symbols.Contains(c))
                {
                    parts.Add(zenSyntax.Substring(index, i - index));
                    index = i;
                }
            }

            IEnumerable<string> final = new[] { zenSyntax.Substring(index) };

            parts.AddRange(final);

            // Adjust for multiple ^ characters
            for (int i = parts.Count - 2; i >= 0; i--)
            {
                if (parts[i] == "^")
                {
                    parts[i] = '^' + parts[i + 1];
                    parts.RemoveAt(i + 1);
                }
            }

            return parts;
        }

        public static string RenderControl(Control control)
        {
            if (control == null)
                return null;

            using (StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            using (XhtmlTextWriter htmlTextWriter = new XhtmlTextWriter(stringWriter))
            {
                control.RenderControl(htmlTextWriter);

                return _collapseMultipleLinefeeds.Replace(HttpUtility.HtmlDecode(stringWriter.ToString())
                                         .Trim(Environment.NewLine.ToArray()), Environment.NewLine); // Replace multiple linefeeds with single.
            }
        }
    }
}