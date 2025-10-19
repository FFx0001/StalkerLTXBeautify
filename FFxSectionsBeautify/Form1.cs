using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace FFxSectionsBeautify
{
    public partial class main_form : Form
    {
        public main_form()
        {
            InitializeComponent();
        }

        private string CleanSpaces(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            StringBuilder result = new StringBuilder();
            bool lastWasSpace = false;

            foreach (char c in input)
            {
                if (c == ' ' || c == '\t')
                {
                    if (!lastWasSpace)
                    {
                        result.Append(' ');
                        lastWasSpace = true;
                    }
                }
                else
                {
                    result.Append(c);
                    lastWasSpace = false;
                }
            }

            return result.ToString().Trim();
        }

        private void processing()
        {
            if (rtb_input == null || rtb_input.Lines == null) return;

            List<string> cleanedLines = new List<string>();
            foreach (string line in rtb_input.Lines)
            {
                if (line == null) continue;

                string trimmed = line.Trim();
                if (trimmed.Length > 0)
                {
                    string singleSpaced = CleanSpaces(trimmed);
                    cleanedLines.Add(singleSpaced);
                }
                else
                {
                    cleanedLines.Add("");
                }
            }

            List<string> finalLines = CleanEmptyLines(cleanedLines);
            List<Section> sections = ParseSections(finalLines);

            if (sections.Count > 0)
            {
                AddCommentsToFirstSection(sections, finalLines);
                AddCommentsToOtherSections(sections);
                SetPaddings(sections);
                BuildOutput(sections);
            }
        }

        private List<string> CleanEmptyLines(List<string> lines)
        {
            List<string> result = new List<string>();
            bool lastWasEmpty = false;

            foreach (string line in lines)
            {
                if (line == null) continue;

                if (line.Length == 0)
                {
                    if (!lastWasEmpty)
                    {
                        result.Add("");
                        lastWasEmpty = true;
                    }
                }
                else
                {
                    result.Add(line);
                    lastWasEmpty = false;
                }
            }

            return result;
        }

        private class Section
        {
            public int Padding = 0;
            public int AdditionalLinesPadding = 5;
            public bool isHudSection = false;
            public List<string> Comments = new List<string>();
            public string FullSectionFirstLine = "";
            public List<string> Lines = new List<string>();

            public string Build(int offset)
            {
                StringBuilder sb = new StringBuilder();

                if (Comments != null)
                {
                    foreach (string comment in Comments)
                    {
                        if (comment != null)
                        {
                            sb.Append(new string(' ', Padding));
                            sb.AppendLine(comment);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(FullSectionFirstLine))
                {
                    sb.Append(new string(' ', Padding));
                    sb.AppendLine(FullSectionFirstLine);
                }

                if (Lines != null)
                {
                    foreach (string line in Lines)
                    {
                        if (line != null)
                        {
                            string formattedLine = FormatLineWithOffset(line, Padding + AdditionalLinesPadding, offset);
                            sb.AppendLine(formattedLine);
                        }
                    }
                }

                return sb.ToString();
            }

            private string FormatLineWithOffset(string line, int basePadding, int offset)
            {
                if (string.IsNullOrEmpty(line))
                    return new string(' ', basePadding);

                int equalsIndex = line.IndexOf('=');
                if (equalsIndex <= 0)
                    return new string(' ', basePadding) + line;

                string key = line.Substring(0, equalsIndex).Trim();
                string value = line.Substring(equalsIndex).Trim();

                int totalKeyLength = basePadding + key.Length;
                int spacesNeeded = Math.Max(1, offset - totalKeyLength);

                return new string(' ', basePadding) + key + new string(' ', spacesNeeded) + value;
            }
        }

        private List<Section> ParseSections(List<string> lines)
        {
            List<Section> sections = new List<Section>();
            if (lines == null) return sections;

            Section currentSection = null;
            bool lastWasEmpty = false;

            foreach (string line in lines)
            {
                if (line == null) continue;

                if (line.Length == 0)
                {
                    if (currentSection != null && !lastWasEmpty)
                    {
                        currentSection.Lines.Add("");
                        lastWasEmpty = true;
                    }
                }
                else
                {
                    lastWasEmpty = false;

                    if (line.StartsWith("[") && line.IndexOf(']') > 0)
                    {
                        currentSection = new Section();
                        currentSection.FullSectionFirstLine = line;
                        currentSection.isHudSection = line.IndexOf("hud", StringComparison.OrdinalIgnoreCase) >= 0;
                        sections.Add(currentSection);
                    }
                    else if (currentSection != null)
                    {
                        currentSection.Lines.Add(line);
                    }
                }
            }

            return sections;
        }

        private void AddCommentsToFirstSection(List<Section> sections, List<string> allLines)
        {
            if (sections == null || sections.Count == 0 || allLines == null) return;

            List<string> firstComments = new List<string>();
            bool foundFirstSection = false;

            for (int i = 0; i < allLines.Count && !foundFirstSection; i++)
            {
                string line = allLines[i];
                if (string.IsNullOrEmpty(line)) continue;

                if (line.StartsWith("[") && line.IndexOf(']') > 0)
                {
                    foundFirstSection = true;
                }
                else if (line.StartsWith(";"))
                {
                    firstComments.Add(line);
                }
            }

            if (firstComments.Count > 0)
            {
                sections[0].Comments.AddRange(firstComments);
            }
        }

        private void AddCommentsToOtherSections(List<Section> sections)
        {
            if (sections == null) return;

            for (int i = 1; i < sections.Count; i++)
            {
                if (sections[i] == null || sections[i].isHudSection) continue;

                Section prevSection = sections[i - 1];
                if (prevSection == null || prevSection.Lines == null) continue;

                List<string> trailingComments = new List<string>();

                for (int j = prevSection.Lines.Count - 1; j >= 0; j--)
                {
                    string line = prevSection.Lines[j];
                    if (string.IsNullOrEmpty(line)) continue;

                    if (line.StartsWith(";"))
                    {
                        trailingComments.Insert(0, line);
                    }
                    else
                    {
                        break;
                    }
                }

                if (trailingComments.Count > 0)
                {
                    RemoveTrailingComments(prevSection, trailingComments.Count);
                    sections[i].Comments.AddRange(trailingComments);
                }
            }
        }

        private void RemoveTrailingComments(Section section, int count)
        {
            if (section == null || section.Lines == null) return;

            int removed = 0;
            for (int i = section.Lines.Count - 1; i >= 0 && removed < count; i--)
            {
                string line = section.Lines[i];
                if (!string.IsNullOrEmpty(line) && line.StartsWith(";"))
                {
                    section.Lines.RemoveAt(i);
                    removed++;
                }
            }
        }

        private void SetPaddings(List<Section> sections)
        {
            if (sections == null) return;

            foreach (Section section in sections)
            {
                if (section != null)
                {
                    section.Padding = section.isHudSection ? 5 : 0;
                }
            }
        }

        private void BuildOutput(List<Section> sections)
        {
            if (sections == null || rtb_output == null) return;

            StringBuilder result = new StringBuilder();
            int offset = (int)nupd_offset.Value;

            foreach (Section section in sections)
            {
                if (section == null) continue;

                if (result.Length > 0)
                {
                    result.AppendLine();
                }

                result.Append(section.Build(offset));
            }

            rtb_output.Text = result.ToString();
        }

        private void button1_Click_2(object sender, EventArgs e)
        {
            if (rtb_output != null)
            {
                rtb_output.Clear();
            }

            if (rtb_input != null && !string.IsNullOrEmpty(rtb_input.Text))
            {
                processing();
            }
        }
    }
}