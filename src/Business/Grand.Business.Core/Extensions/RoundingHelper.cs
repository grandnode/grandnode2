using Grand.Domain.Directory;

namespace Grand.Business.Core.Extensions
{
    public static class RoundingHelper
    {

        /// <summary>
        /// Round price or order total for the currency
        /// </summary>
        /// <param name="value">Value to round</param>
        /// <param name="currency">Currency</param>
        /// <returns>Rounded value</returns>
        public static double RoundPrice(double value, Currency currency)
        {
            return currency != null ? value.Round(currency.NumberDecimal, currency.RoundingTypeId, currency.MidpointRoundId) : value;
        }

        /// <summary>
        /// Round
        /// </summary>
        /// <param name="value">Value to round</param>
        /// <param name="doubles">Doubles</param>
        /// <param name="roundingType">The rounding type</param>
        /// <param name="midpointRounding">Mid point rounding</param>
        /// <returns>Rounded value</returns>
        public static double Round(this double value, int doubles, RoundingType roundingType, MidpointRounding midpointRounding)
        {

            //default round (Rounding001)
            var rez = Math.Round(value, doubles, midpointRounding);
            var fractionPart = (rez - Math.Truncate(rez)) * 10;

            //cash rounding not needed
            if (fractionPart == 0)
                return rez;

            //Cash rounding (details: https://en.wikipedia.org/wiki/Cash_rounding)
            switch (roundingType)
            {
                //rounding with 0.05 or 5 intervals
                case RoundingType.Rounding005Up:
                case RoundingType.Rounding005Down:
                    fractionPart = (fractionPart - Math.Truncate(fractionPart)) * 10;
                    if (fractionPart is 5 or 0)
                        break;
                    if (roundingType == RoundingType.Rounding005Down)
                        fractionPart = fractionPart > 5 ? 5 - fractionPart : fractionPart * -1;
                    else
                        fractionPart = fractionPart > 5 ? 10 - fractionPart : 5 - fractionPart;

                    rez += fractionPart / 100;
                    break;
                //rounding with 0.10 intervals
                case RoundingType.Rounding01Up:
                case RoundingType.Rounding01Down:
                    fractionPart = (fractionPart - Math.Truncate(fractionPart)) * 10;
                    if (fractionPart == 0)
                        break;

                    if (roundingType == RoundingType.Rounding01Down && fractionPart == 5)
                        fractionPart = -5;
                    else
                        fractionPart = fractionPart < 5 ? fractionPart * -1 : 10 - fractionPart;

                    rez += fractionPart / 100;
                    break;
                //rounding with 0.50 intervals
                case RoundingType.Rounding05:
                    fractionPart *= 10;
                    fractionPart = fractionPart < 25 ? fractionPart * -1 : fractionPart < 50 || fractionPart < 75 ? 50 - fractionPart : 100 - fractionPart;

                    rez += fractionPart / 100;
                    break;
                //rounding with 1.00 intervals
                case RoundingType.Rounding1:
                case RoundingType.Rounding1Up:
                    fractionPart *= 10;

                    if (roundingType == RoundingType.Rounding1Up && fractionPart > 0)
                        rez = Math.Truncate(rez) + 1;
                    else
                        rez = fractionPart < 50 ? Math.Truncate(rez) : Math.Truncate(rez) + 1;

                    break;
                case RoundingType.Rounding001:
                default:
                    break;
            }

            return rez;
        }
    }
}
