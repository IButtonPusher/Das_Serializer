using System;
using System.Collections.Generic;
using System.Text;

// ReSharper disable UnusedMember.Global

namespace Serializer
{
    public interface ITextParser : INumberExtractor
    {
        String FindJsonValue(String input, String toFind);

        /// <summary>
        /// Returns all values for the given key
        /// </summary>
        IEnumerable<String> EnumerateJsonValues(String input, String key);

        Double FindJsonDouble(String input, String toFind);

        String FindTextWithin(String input, String leftBounds, String rightBounds);

        String LeftOf(String input, String rightBounds);

        /// <summary>
        /// The text after the first delimeter and before the second, between second and third, etc
        /// </summary>
        /// <returns>delimiters.Length - 1 items</returns>
        Boolean TryFindTextWithin(String input, String[] delimiters, out String[] found);

        /// <summary>
        /// The text before and after the first delimeter, before and after the second, etc
        /// </summary>
        /// <returns>delimiters.Length - 1 items</returns>
        Boolean TryFindTextSurrounding(String input, String[] delimiters, out String[] found,
            Int32 findRequired = -1);

        IEnumerable<String> FindTextWithin(String input, String[] delimiters);

        IEnumerable<string> FindTextsWithin(string input, string leftOf,
            string rightOf);

        IEnumerable<String> FindTextWithin(String input, String leftBounds, String middleBounds,
            String rightBounds);

        String FindTextWithin(String input,
            String leftBounds, String rightBounds, Int32 startIndex);

        /// <summary>
        /// From the end of afterFound till the end of the text. If afterFound is not found,
        /// returns inText
        /// </summary>
        /// <returns></returns>
        String After(String inText, String afterFound);


        String RemoveAll(String input, String[] toRemove);

        /// <summary>
        /// Ordinal not case sensitive
        /// </summary>
        /// <param name="checkingString"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Boolean EndsWithOrdinal(String checkingString, String value);

        /// <summary>
        /// Ordinal not case sensitive
        /// </summary>
        /// <param name="checkingString"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Boolean ContainsOrdinal(String checkingString, String value);

        Boolean TrySkip(String inText, String whenStartsWith, out String remaining);

        IEnumerator<String> FindJsonValues(String input, String toFind1, String toFind2);

        IEnumerator<String> FindJsonValues(String input, String toFind1, String toFind2,
            String toFind3);

        IEnumerator<String> FindJsonValues(String input, String toFind1, String toFind2,
            String toFind3, String toFind4);

        IEnumerator<String> FindJsonValues(String input, String toFind1, String toFind2,
            String toFind3, String toFind4, String toFind5);

        IEnumerator<String> FindJsonValues(String input, String toFind1, String toFind2,
            String toFind3, String toFind4, String toFind5, String toFind6);

        IEnumerator<String> FindJsonValues(String input, String toFind1, String toFind2,
            String toFind3, String toFind4, String toFind5, String toFind6, String toFind7);


        Boolean ContainsAll(String str, String[] list);

        string FindValue(string json, string item, ref Int32 startIndex);

        String AsUSString(Decimal amount, Int32 decimalPlaces);

        String AsUSString(Double amount, Int32 decimalPlaces);

        String AsUSString(Decimal amount);

        String AsUSString(Double amount);

        /// <summary>
        /// Comma delimited
        /// </summary>
        /// <returns>true if any items were in the amountList</returns>
        Boolean AppendAsUsCommaString(StringBuilder sb, IEnumerable<Decimal> amountList,
            Int32 multiple, Int32 decimalPlaces);

        /// <summary>
        /// returns index of searchFor + its length using ordinal rules
        /// </summary>
        Int32 IndexOfEnd(String searchIn, String searchFor);

        Int32 IndexOf(String searchIn, String searchFor);

        Boolean TryIndexOf(String searchIn, String searchFor, out Int32 index);

        Int32 LastIndexOf(String searchIn, String searchFor);

        Int32 LastIndexOf(String searchIn, String searchFor, Int32 startIndex);

        Int32 IndexOf(String searchIn, String searchFor, Int32 startIndex);

        String[] GetLines(String str);
    }
}