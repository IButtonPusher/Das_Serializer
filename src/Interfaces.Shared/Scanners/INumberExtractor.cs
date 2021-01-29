using System;
using System.Threading.Tasks;

// ReSharper disable UnusedMemberInSuper.Global

// ReSharper disable UnusedMember.Global
namespace Das.Serializer
{
    public interface INumberExtractor
    {
        Boolean AreEqual(Double left,
                         Double right);

        /// <summary>
        ///     Tries to turn the whole string into the number using 0.00 formatting
        /// </summary>
        /// <returns>0 if it's a completely invalid input</returns>
        Double GetCurrency(String fromString);

        String GetCurrencyText(String fromString);

        /// <summary>
        ///     tries to infer in which format the text is (e.g 1.23 | 1,234.56 etc)
        /// </summary>
        Double GetDouble(String fromString);

        Int32 GetInt32(String fromString);

        Double GetNumericalDifference(Double left,
                                      Double right);

        /// <summary>
        ///     Tries to find some currency but if nothing found returns false
        /// </summary>
        Boolean TryGetCurrency(String fromString,
                               out Double currency);
    }
}
