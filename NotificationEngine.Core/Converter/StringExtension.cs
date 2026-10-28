using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Primitives;


namespace NotificationEngine.Core.Converter
{
    public static class StringExtension
    {
        private static string GeneralLongTimePattern => CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern +
                                                        " " + CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern;

        public static bool IsDateTime(this string data, string dateFormat)
        {
            // ReSharper disable once RedundantAssignment
            DateTime dateVal = default;
            return DateTime.TryParseExact(data, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None,
                out dateVal);
        }

        public static int ToInt32(this string value)
        {
            int number = 0;
            int.TryParse(value, out number);
            return number;
        }

        public static int ToInt32(this StringValues value)
        {
            int number;
            int.TryParse(value, out number);
            return number;
        }

        public static long ToInt64(this string value)
        {
            long number;
            long.TryParse(value, out number);
            return number;
        }

        public static short ToInt16(this string value)
        {
            short number;
            short.TryParse(value, out number);
            return number;
        }

        public static decimal ToDecimal(this string value)
        {
            decimal number;
            decimal.TryParse(value, out number);
            return number;
        }

        public static bool ToBoolean(this string value)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value)) throw new ArgumentException("value");
            var val = value.ToLower().Trim();
            switch (val)
            {
                case "false":
                    return false;
                case "f":
                    return false;
                case "true":
                    return true;
                case "t":
                    return true;
                case "yes":
                    return true;
                case "no":
                    return false;
                case "y":
                    return true;
                case "n":
                    return false;
                default:
                    throw new ArgumentException("Invalid boolean");
            }
        }

        public static DateTime? ToDateTime(this StringValues val)
        {
            return ToDateTime(val.ToString());
        }

        public static bool ToBoolan(this StringValues val)
        {
            try
            {
                return Convert.ToBoolean(val.ToString().Split(",")[0]);
            }
            catch
            {
                return false;
            }
        }

        public static DateTime? ToDateTime(this string text)
        {
            if (DateTime.TryParseExact(text, GeneralLongTimePattern, CultureInfo.CurrentCulture,
                DateTimeStyles.None, out var result))
                return result;
            return null;
        }

        public static IEnumerable<T> SplitTo<T>(this string str, params char[] separator) where T : IConvertible
        {
            return str.Split(separator, StringSplitOptions.None).Select(s => (T) Convert.ChangeType(s, typeof(T)));
        }

        public static IEnumerable<T> SplitTo<T>(this string str, StringSplitOptions options, params char[] separator)
            where T : IConvertible
        {
            return str.Split(separator, options).Select(s => (T) Convert.ChangeType(s, typeof(T)));
        }

        public static IEnumerable<T> SplitTo<T>(this string str, string separator)
            where T : IConvertible
        {
            return str.Split(separator, StringSplitOptions.None).Select(s => (T) Convert.ChangeType(s, typeof(T)));
        }

        public static T ToEnum<T>(this string value, T defaultValue = default) where T : struct
        {
            if (!typeof(T).IsEnum) throw new ArgumentException("Type T Must of type System.Enum");

            T result;
            var isParsed = Enum.TryParse(value, true, out result);
            return isParsed ? result : defaultValue;
        }

        public static string? Format(this string value, object arg0)
        {
            return string.Format(value, arg0);
        }

        public static string? Format(this string value, params object[] args)
        {
            return string.Format(value, args);
        }

        public static string? GetEmptyStringIfNull(this string? val)
        {
            return val != null ? val.Trim() : "";
        }

        public static string? GetNullIfEmptyString(this string? myValue)
        {
            if (myValue == null || myValue.Length <= 0) return null;
            myValue = myValue.Trim();
            if (myValue.Length > 0) return myValue;
            return null;
        }

        public static bool IsInteger(this string val)
        {
            // Variable to collect the Return value of the TryParse method.

            // Define variable to collect out parameter of the TryParse method. If the conversion fails, the out parameter is zero.
            int retNum;

            // The TryParse method converts a string in a specified style and culture-specific format to its double-precision floating point number equivalent.
            // The TryParse method does not generate an exception if the conversion fails. If the conversion passes, True is returned. If it does not, False is returned.
            var isNum = int.TryParse(val, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out retNum);
            return isNum;
        }

        public static string? Capitalize(this string? s)
        {
            if (s == null || s.Length <= 0) return s;
            return s.Substring(0, 1).ToUpper() + s.Substring(1).ToLower();
        }

        public static string? FirstCharacter(this string? val)
        {
            return !string.IsNullOrEmpty(val)
                ? val.Length >= 1
                    ? val.Substring(0, 1)
                    : val
                : null;
        }

        public static string? LastCharacter(this string? val)
        {
            return !string.IsNullOrEmpty(val)
                ? val.Length >= 1
                    ? val.Substring(val.Length - 1, 1)
                    : val
                : null;
        }

        public static bool EndsWithIgnoreCase(this string? val, string? suffix)
        {
            if (val == null) throw new ArgumentNullException("val", "val parameter is null");
            if (suffix == null) throw new ArgumentNullException("suffix", "suffix parameter is null");
            if (val.Length < suffix.Length) return false;
            return val.EndsWith(suffix, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool StartsWithIgnoreCase(this string val, string prefix)
        {
            if (val == null) throw new ArgumentNullException("val", "val parameter is null");
            if (prefix == null) throw new ArgumentNullException("prefix", "prefix parameter is null");
            if (val.Length < prefix.Length) return false;
            return val.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase);
        }

        public static string? Replace(this string s, params char[] chars)
        {
            return chars.Aggregate(s, (current, c) => current.Replace(c.ToString(CultureInfo.InvariantCulture), ""));
        }

        public static string? RemoveChars(this string s, params char[] chars)
        {
            var sb = new StringBuilder(s.Length);
            foreach (var c in s.Where(c => !chars.Contains(c))) sb.Append(c);
            return sb.ToString();
        }

        public static bool IsEmailAddress(this string email)
        {
            var pattern =
                "^[a-zA-Z][\\w\\.-]*[a-zA-Z0-9]@[a-zA-Z0-9][\\w\\.-]*[a-zA-Z0-9]\\.[a-zA-Z][a-zA-Z\\.]*[a-zA-Z]$";
            return Regex.Match(email, pattern).Success;
        }

        public static bool IsNumeric(this string val)
        {
            // Variable to collect the Return value of the TryParse method.

            // Define variable to collect out parameter of the TryParse method. If the conversion fails, the out parameter is zero.
            double retNum;

            // The TryParse method converts a string in a specified style and culture-specific format to its double-precision floating point number equivalent.
            // The TryParse method does not generate an exception if the conversion fails. If the conversion passes, True is returned. If it does not, False is returned.
            var isNum = double.TryParse(val, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out retNum);
            return isNum;
        }

        public static string? Truncate(this string s, int maxLength)
        {
            if (string.IsNullOrEmpty(s) || maxLength <= 0) return string.Empty;
            if (s.Length > maxLength) return s.Substring(0, maxLength) + "...";
            return s;
        }

        public static string? GetDefaultIfEmpty(this string myValue, string defaultValue)
        {
            if (!string.IsNullOrEmpty(myValue))
            {
                myValue = myValue.Trim();
                return myValue.Length > 0 ? myValue : defaultValue;
            }

            return defaultValue;
        }

        public static byte[] ToBytes(this string val)
        {
            var bytes = new byte[val.Length * sizeof(char)];
            Buffer.BlockCopy(val.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static string? Reverse(this string val)
        {
            var chars = new char[val.Length];
            for (int i = val.Length - 1, j = 0; i >= 0; --i, ++j) chars[j] = val[i];
            val = new string(chars);
            return val;
        }

        public static bool IsAlpha(this string val)
        {
            if (string.IsNullOrEmpty(val)) return false;
            return val.Trim().Replace(" ", "").All(char.IsLetter);
        }

        public static bool IsAlphaNumeric(this string val)
        {
            if (string.IsNullOrEmpty(val)) return false;
            return val.Trim().Replace(" ", "").All(char.IsLetterOrDigit);
        }

        public static string? CreateHashSha512(string val)
        {
            if (string.IsNullOrEmpty(val)) throw new ArgumentException("val");
            var sb = new StringBuilder();
            using (var hash = SHA512.Create())
            {
                var data = hash.ComputeHash(val.ToBytes());
                foreach (var b in data) sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }

        public static string? CreateHashSha256(string val)
        {
            if (string.IsNullOrEmpty(val)) throw new ArgumentException("val");
            var sb = new StringBuilder();
            using (var hash = SHA256.Create())
            {
                var data = hash.ComputeHash(val.ToBytes());
                foreach (var b in data) sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }

        public static IDictionary<string, string>? QueryStringToDictionary(this string? queryString)
        {
            if (string.IsNullOrWhiteSpace(queryString)) return null;
            if (!queryString.Contains("?")) return null;
            var query = queryString.Replace("?", "");
            if (!query.Contains("=")) return null;
            return query.Split('&').Select(p => p.Split('=')).ToDictionary(
                key => key[0].ToLower().Trim(), value => value[1]);
        }

        public static string? ReverseSlash(this string val, int direction)
        {
            switch (direction)
            {
                case 0:
                    return val.Replace(@"/", @"\");
                case 1:
                    return val.Replace(@"\", @"/");
                default:
                    return val;
            }
        }

        public static string? ReplaceLineFeeds(this string val)
        {
            return Regex.Replace(val, @"^[\r\n]+|\.|[\r\n]+$", "");
        }

        public static bool IsValidIPv4(this string val)
        {
            if (string.IsNullOrEmpty(val)) return false;
            return Regex.Match(val,
                    @"(?:^|\s)([a-z]{3,6}(?=://))?(://)?((?:25[0-5]|2[0-4]\d|[01]?\d\d?)\.(?:25[0-5]|2[0-4]\d|[01]?\d\d?)\.(?:25[0-5]|2[0-4]\d|[01]?\d\d?)\.(?:25[0-5]|2[0-4]\d|[01]?\d\d?))(?::(\d{2,5}))?(?:\s|$)")
                .Success;
        }

        public static int GetByteSize(this string val, Encoding encoding)
        {
            if (val == null) throw new ArgumentNullException("val");
            if (encoding == null) throw new ArgumentNullException("encoding");
            return encoding.GetByteCount(val);
        }

        public static string? Left(this string? val, int length)
        {
            if (string.IsNullOrEmpty(val)) throw new ArgumentNullException("val");
            if (length < 0 || length > val.Length)
                throw new ArgumentOutOfRangeException("length",
                    "length cannot be higher than total string length or less than 0");
            return val.Substring(0, length);
        }

        public static string? Right(this string? val, int length)
        {
            if (string.IsNullOrEmpty(val)) throw new ArgumentNullException("val");
            if (length < 0 || length > val.Length)
                throw new ArgumentOutOfRangeException("length",
                    "length cannot be higher than total string length or less than 0");
            return val.Substring(val.Length - length);
        }

        public static IEnumerable<string> ToTextElements(this string val)
        {
            if (val == null) throw new ArgumentNullException("val");
            var elementEnumerator = StringInfo.GetTextElementEnumerator(val);
            while (elementEnumerator.MoveNext())
            {
                var textElement = elementEnumerator.GetTextElement();
                yield return textElement;
            }
        }

        public static bool DoesNotStartWith(this string val, string prefix)
        {
            return val == null || prefix == null ||
                   !val.StartsWith(prefix, StringComparison.InvariantCulture);
        }

        public static bool DoesNotEndWith(this string val, string suffix)
        {
            return val == null || suffix == null ||
                   !val.EndsWith(suffix, StringComparison.InvariantCulture);
        }

        public static bool IsNull(this string val)
        {
            return val == null;
        }

        public static bool IsNullOrEmpty(this string val)
        {
            return string.IsNullOrEmpty(val);
        }

        public static bool IsMinLength(this string val, int minCharLength)
        {
            return val != null && val.Length >= minCharLength;
        }

        public static bool IsMaxLength(this string val, int maxCharLength)
        {
            return val != null && val.Length <= maxCharLength;
        }

        public static bool IsLength(this string val, int minCharLength, int maxCharLength)
        {
            return val != null && val.Length >= minCharLength && val.Length <= minCharLength;
        }

        public static int? GetLength(string val)
        {
            return val == null ? (int?) null : val.Length;
        }

        public static string? MaskEmail(this string mail)
        {
            if (string.IsNullOrEmpty(mail))
                return mail;
            string pattern = @"(?<=[\w]{1})[\w-\._\+%]*(?=[\w]{1}@)";
            return Regex.Replace(mail, pattern, m => new string('*', m.Length));
        }

        public static string ToSnakeCase(this string name)
        {
            return Regex.Replace(name, "([a-z])([A-Z])", "$1_$2").ToLower(CultureInfo.InvariantCulture);
        }
    }
}
