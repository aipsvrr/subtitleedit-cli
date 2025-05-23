﻿using System.Text.RegularExpressions;

namespace seconv.libse.Forms.FixCommonErrors
{
    public class FixAloneLowercaseIToUppercaseI : IFixCommonError
    {
        public static class Language
        {
            public static string FixLowercaseIToUppercaseI { get; set; } = "Fix alone lowercase 'i' to 'I' (English)";
        }

        public void Fix(Subtitle subtitle, IFixCallbacks callbacks)
        {
            string fixAction = Language.FixLowercaseIToUppercaseI;
            int iFixes = 0;
            for (int i = 0; i < subtitle.Paragraphs.Count; i++)
            {
                Paragraph p = subtitle.Paragraphs[i];
                string oldText = p.Text;
                string s = p.Text;
                if (s.Contains('i'))
                {
                    s = FixAloneLowercaseIToUppercaseLine(RegexUtils.LittleIRegex, oldText, s, 'i');
                    if (s != oldText && callbacks.AllowFix(p, fixAction))
                    {
                        p.Text = s;
                        iFixes++;
                        callbacks.AddFixToListView(p, fixAction, oldText, p.Text);
                    }
                }
            }
            callbacks.UpdateFixStatus(iFixes, Language.FixLowercaseIToUppercaseI);
        }

        public static string FixAloneLowercaseIToUppercaseLine(Regex re, string oldText, string input, char target)
        {
            //html tags
            var s = input.Replace(">" + target + "</", ">I</")
                         .Replace(">" + target + " ", ">I ")
                         .Replace(">" + target + "\u200B" + Environment.NewLine, ">I" + Environment.NewLine) // Zero Width Space
                         .Replace(">" + target + "\uFEFF" + Environment.NewLine, ">I" + Environment.NewLine); // Zero Width No-Break Space

            s = s.Replace(" i-i ", " I-I ");
            s = s.Replace(" i-i-i ", " I-I-I ");

            // reg-ex
            var match = re.Match(s);
            var assaDrawStart = s.IndexOf("{\\p1", StringComparison.Ordinal);
            var assaDrawEnd = s.IndexOf("{\\p0}", StringComparison.Ordinal);
            while (match.Success)
            {
                if (s[match.Index] == target && !s.Substring(match.Index).StartsWith("i.e.", StringComparison.Ordinal) &&
                    !s.Substring(match.Index).StartsWith("i-", StringComparison.Ordinal) &&
                    !(assaDrawStart >= 0 && match.Index > assaDrawStart && match.Index < assaDrawEnd))
                {
                    var prev = '\0';
                    var next = '\0';
                    if (match.Index > 0)
                    {
                        prev = s[match.Index - 1];
                    }

                    if (match.Index + 1 < s.Length)
                    {
                        next = s[match.Index + 1];
                    }

                    var wholePrev = string.Empty;
                    if (match.Index > 1)
                    {
                        wholePrev = s.Substring(0, match.Index - 1);
                    }

                    if (prev != '>' && next != '>' && next != '}' && !wholePrev.TrimEnd().EndsWith("...", StringComparison.Ordinal))
                    {
                        var fix = prev != '.' && prev != '\'';

                        if (prev == ' ' && next == '.')
                        {
                            fix = false;
                        }

                        if (prev == '-' && match.Index > 2)
                        {
                            fix = false;
                        }

                        if (fix && next == '-' && match.Index < s.Length - 5 && s[match.Index + 2] == 'l' && !(Environment.NewLine + @" <>!.?:;,").Contains(s[match.Index + 3]))
                        {
                            fix = false;
                        }

                        if (fix)
                        {
                            string temp = s.Substring(0, match.Index) + "I";
                            if (match.Index + 1 < oldText.Length)
                            {
                                temp += s.Substring(match.Index + 1);
                            }

                            s = temp;
                        }
                    }
                }
                match = match.NextMatch();
            }

            return s;
        }
    }
}
