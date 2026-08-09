// Thai shaping algorithm adapted from SaladLab/Unity3D.ThaiFontAdjuster.
// Copyright (c) 2016 SaladLab. Distributed under the MIT License.

using System.Text;

namespace LomThaiText
{
    internal static class ThaiFontAdjuster
    {
        internal static bool HasThai(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            for (var i = 0; i < value.Length; i++)
            {
                var character = value[i];
                if ((character >= '\u0E00' && character <= '\u0E7F') ||
                    (character >= '\uF700' && character <= '\uF71A'))
                    return true;
            }
            return false;
        }

        internal static bool HasCjk(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            for (var i = 0; i < value.Length; i++)
            {
                int scalar = value[i];
                if (scalar >= 0xD800 && scalar <= 0xDBFF && i + 1 < value.Length)
                {
                    int low = value[i + 1];
                    if (low >= 0xDC00 && low <= 0xDFFF)
                    {
                        scalar = 0x10000 + ((scalar - 0xD800) << 10) + (low - 0xDC00);
                        i++;
                    }
                }

                if (IsCjkScalar(scalar)) return true;
            }
            return false;
        }

        private static bool IsCjkScalar(int scalar)
        {
            return (scalar >= 0x3400 && scalar <= 0x9FFF) ||
                   (scalar >= 0xF900 && scalar <= 0xFAFF) ||
                   (scalar >= 0x20000 && scalar <= 0x2A6DF) ||
                   (scalar >= 0x2A700 && scalar <= 0x2B73F) ||
                   (scalar >= 0x2B740 && scalar <= 0x2B81F) ||
                   (scalar >= 0x2B820 && scalar <= 0x2CEAF) ||
                   (scalar >= 0x2CEB0 && scalar <= 0x2EBEF) ||
                   (scalar >= 0x2EBF0 && scalar <= 0x2EE5F) ||
                   (scalar >= 0x2F800 && scalar <= 0x2FA1F) ||
                   (scalar >= 0x30000 && scalar <= 0x3134F) ||
                   (scalar >= 0x31350 && scalar <= 0x323AF);
        }

        internal static string Adjust(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            var result = new StringBuilder(value.Length);
            for (var i = 0; i < value.Length; i++)
            {
                var character = value[i];
                if (IsTop(character) && i > 0)
                {
                    var baseCharacter = value[i - 1];
                    if (IsLower(baseCharacter) && i > 1) baseCharacter = value[i - 2];
                    if (IsBase(baseCharacter))
                    {
                        var nikhahit = i < value.Length - 1 &&
                            (value[i + 1] == '\u0E33' || value[i + 1] == '\u0E4D');
                        if (IsAscender(baseCharacter))
                        {
                            if (nikhahit)
                            {
                                character = (char)(character + ('\uF713' - '\u0E48'));
                                result.Append('\uF711').Append(character);
                                if (value[i + 1] == '\u0E33') result.Append('\u0E32');
                                i++;
                                continue;
                            }
                            character = (char)(character + ('\uF705' - '\u0E48'));
                        }
                        else if (!nikhahit)
                        {
                            character = (char)(character + ('\uF70A' - '\u0E48'));
                        }
                    }
                    // Unity legacy Text can discard a second combining mark even when
                    // no ascender shift is needed. The C90 top-left glyphs in the
                    // adjusted font are exact metric/outline duplicates of the normal
                    // top marks, but their PUA codepoints force Unity to emit them.
                    if (i > 1 && IsUpper(value[i - 1]))
                        character = (char)(character + ('\uF713' - '\u0E48'));
                }
                else if (IsUpper(character) && i > 0 && IsAscender(value[i - 1]))
                {
                    switch (character)
                    {
                        case '\u0E31': character = '\uF710'; break;
                        case '\u0E34': character = '\uF701'; break;
                        case '\u0E35': character = '\uF702'; break;
                        case '\u0E36': character = '\uF703'; break;
                        case '\u0E37': character = '\uF704'; break;
                        case '\u0E4D': character = '\uF711'; break;
                        case '\u0E47': character = '\uF712'; break;
                    }
                }
                else if (IsLower(character) && i > 0 &&
                         (value[i - 1] == '\u0E0E' || value[i - 1] == '\u0E0F'))
                {
                    character = (char)(character + ('\uF718' - '\u0E38'));
                }
                else if (character == '\u0E0D' && i < value.Length - 1 && IsLower(value[i + 1]))
                    character = '\uF70F';
                else if (character == '\u0E10' && i < value.Length - 1 && IsLower(value[i + 1]))
                    character = '\uF700';
                result.Append(character);
            }
            return result.ToString();
        }

        private static bool IsBase(char character)
        {
            return (character >= '\u0E01' && character <= '\u0E2F') ||
                   character == '\u0E30' || character == '\u0E40' || character == '\u0E41';
        }

        private static bool IsAscender(char character)
        {
            return character == '\u0E1B' || character == '\u0E1D' ||
                   character == '\u0E1F' || character == '\u0E2C';
        }

        private static bool IsTop(char character)
        {
            return character >= '\u0E48' && character <= '\u0E4C';
        }

        private static bool IsLower(char character)
        {
            return character >= '\u0E38' && character <= '\u0E3A';
        }

        private static bool IsUpper(char character)
        {
            return character == '\u0E31' || character == '\u0E34' ||
                   character == '\u0E35' || character == '\u0E36' ||
                   character == '\u0E37' || character == '\u0E47' ||
                   character == '\u0E4D';
        }
    }
}
