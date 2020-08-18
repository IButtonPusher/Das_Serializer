using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable UnusedMember.Global

namespace Das.Serializer
{
    public interface ITextParser : INumberExtractor
    {
        /// <summary>
        ///     From the end of afterFound till the end of the text. If afterFound is not found,
        ///     returns inText
        /// </summary>
        /// <returns></returns>
        String After(String inText, String afterFound);

        /// <summary>
        ///     Comma delimited
        /// </summary>
        /// <returns>true if any items were in the amountList</returns>
        Boolean AppendAsUsCommaString(StringBuilder sb, IEnumerable<Double> amountList,
            Int32 multiple, Int32 decimalPlaces);

        String AsUSString(Double amount, Int32 decimalPlaces);

        String AsUSString(Decimal amount, Int32 decimalPlaces);

        String AsUSString(Double amount);

        String AsUSString(Decimal amount);

        String BuildString<T1, T2, T3, T4>(T1 item1, T2 item2, T3 item3, T4 item4);

        String BuildString<T1, T2, T3, T4, T5>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5);

        String BuildString<T1, T2, T3, T4, T5, T6>(T1 item1, T2 item2,
            T3 item3, T4 item4, T5 item5, T6 item6);


        Boolean ContainsAll(String str, String[] list);

        /// <summary>
        ///     Ordinal not case sensitive
        /// </summary>
        /// <param name="checkingString"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Boolean ContainsOrdinal(String checkingString, String value);

        /// <summary>
        ///     Ordinal not case sensitive
        /// </summary>
        /// <param name="checkingString"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Boolean EndsWithOrdinal(String checkingString, String value);

        /// <summary>
        ///     Returns all values for the given key
        /// </summary>
        IEnumerable<String> EnumerateJsonValues(String input, String key);

        Double FindJsonDouble(String input, String toFind);

        String? FindJsonValue(String input, String toFind);

        String? FindJsonValue(String json, String item, ref Int32 startIndex);

        IEnumerator<String?> FindJsonValues(String input, String toFind1, String toFind2);

        IEnumerator<String?> FindJsonValues(String input, String toFind1, String toFind2,
                                            String toFind3);

        IEnumerator<String?> FindJsonValues(String input, String toFind1, String toFind2,
                                            String toFind3, String toFind4);

        IEnumerator<String?> FindJsonValues(String input, String toFind1, String toFind2,
                                            String toFind3, String toFind4, String toFind5);

        IEnumerator<String?> FindJsonValues(String input, String toFind1, String toFind2,
                                            String toFind3, String toFind4, String toFind5, String toFind6);

        IEnumerator<String?> FindJsonValues(String input, String toFind1, String toFind2,
                                            String toFind3, String toFind4, String toFind5, String toFind6, String toFind7);

        IEnumerable<String> FindTextsWithin(String input, String leftOf,
            String rightOf);

        String? FindTextWithin(String input, String leftBounds, String rightBounds);

        IEnumerable<String> FindTextWithin(String input, String[] delimiters);

        IEnumerable<String> FindTextWithin(String input, String leftBounds, String middleBounds,
            String rightBounds);

        String? FindTextWithin(String input,
                               String leftBounds, String rightBounds, Int32 startIndex);

        String[] GetLines(String str);

        /// <summary>
        /// Not safe for async code.  Use BorrowStringBuilder if there will be an await in the 'using' block
        /// </summary>
        ITextRemunerable GetThreadsStringBuilder(String initial);

        /// <summary>
        /// Not safe for async code.  Use BorrowStringBuilder if there will be an await in the 'using' block
        /// </summary>
        ITextRemunerable GetThreadsStringBuilder();

        ITextRemunerable BorrowStringBuilder(String initial);

        ITextRemunerable GetThreadsStringBuilder(Int32 capacity);

        Int32 IndexOf(String searchIn, String searchFor);

        Int32 IndexOf(String searchIn, String searchFor, Int32 startIndex);

        /// <summary>
        ///     returns index of searchFor + its length using ordinal rules
        /// </summary>
        Int32 IndexOfEnd(String searchIn, String searchFor);

        Int32 LastIndexOf(String searchIn, String searchFor);

        Int32 LastIndexOf(String searchIn, String searchFor, Int32 startIndex);

        String LeftOf(String input, String rightBounds);


        String RemoveAll(String input, String[] toRemove);

        /// <summary>
        ///     The text before and after the first delimeter, before and after the second, etc
        /// </summary>
        /// <returns>delimiters.Length - 1 items</returns>
        Boolean TryFindTextSurrounding(String input, String[] delimiters, out String[] found,
            Int32 findRequired = -1);

        /// <summary>
        ///     The text after the first delimeter and before the second, between second and third, etc
        /// </summary>
        /// <returns>delimiters.Length - 1 items</returns>
        Boolean TryFindTextWithin(String input, String[] delimiters, out String[] found);

        Boolean TryIndexOf(String searchIn, String searchFor, out Int32 index);

        Boolean TrySkip(String inText, String whenStartsWith, out String? remaining);
    }
}