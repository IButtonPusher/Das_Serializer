﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Das.Extensions;

// ReSharper disable UnusedMember.Global

namespace Das.Serializer
{
    public class CoreNumeric : INumberExtractor
    {
        public Double GetCurrency(String fromString)
        {
            var isAnyValid = false;
            return GetCurrencyImpl(fromString, ref isAnyValid);
        }

        public Boolean TryGetCurrency(String fromString,
                                      out Double currency)
        {
            var isAnyValid = false;
            currency = GetCurrencyImpl(fromString, ref isAnyValid);
            return isAnyValid;
        }


        public String GetCurrencyText(String str)
        {
            IEnumerable<Char> GetValidChars()
            {
                var isDotDetected = false;
                for (var i = 0; i < str.Length; i++)
                {
                    var ch = str[i];
                    if (Char.IsDigit(ch))
                        yield return ch;
                    else if (!isDotDetected && ch == DOT && i < str.Length - 1)
                    {
                        isDotDetected = true;
                        yield return ch;
                    }
                }
            }

            return String.Concat(GetValidChars());
        }

        public Double GetDouble(String fromString)
        {
            return Convert.ToDouble(GetCurrency(fromString));
        }

        public Int32 GetInt32(String fromString)
        {
            return Convert.ToInt32(GetCurrency(fromString));
        }

        public Double GetNumericalDifference(Double left,
                                             Double right)
        {
            if (right.IsZero())
                return 0;

            //right = baseline
            if (left > right)
                return (left / right - 1) * 100;

            return (0 - (1 - left / right)) * 100;
        }

        public Boolean AreEqual(Double left,
                                Double right)
        {
            return Math.Abs(left - right) < TOLERANCE;
        }

        public Int64 GetInt64(String fromString)
        {
            var str = String.Concat(fromString.Where(Char.IsNumber));
            if (Int64.TryParse(str, out var yes))
                return yes;
            return 0;
        }

        private static Double GetCurrencyImpl(String fromString,
                                              ref Boolean isAnyValid)
        {
            var len = fromString.Length;
            _multiple = 1;
            _currentGroup = _commaGroupLength = _dotGroupLength =
                _currentGroupLength = 0;
            _isNegation = false;

            _commaGroup = 0;
            _dotGroup = 0;
            _buildingResult = 0;


            for (var c = len - 1; c >= -1; c--)
            {
                Char current;
                if (c >= 0)
                {
                    current = fromString[c];
                    if (Char.IsNumber(current))
                    {
                        isAnyValid = true;
                        _currentGroupLength++;
                        _currentGroup += (current & 15) * _multiple;
                        _multiple *= 10;
                        continue;
                    }
                }
                else
                    current = EOL;

                switch (current)
                {
                    case NEG:
                        _isNegation = true;
                        goto eol;
                    case EOL:
                        eol:
                        if (_commaGroupLength > 0 && _dotGroupLength > 0 && _currentGroupLength > 0)
                            switch (_firstGroup)
                            {
                                case COMMA:
                                    current = DOT;
                                    break;
                                case DOT:
                                    current = COMMA;
                                    break;
                            }
                        else
                            current = _commaGroupLength.IsZero() ? COMMA : DOT;

                        break;
                    case DOT:
                    case COMMA:
                        break;
                    default:
                        if (isAnyValid)
                            c = 0;
                        continue;
                }

                switch (current)
                {
                    case COMMA:
                        _commaGroupLength += _currentGroupLength;
                        if (_dotGroupLength.IsZero())
                            _firstGroup = COMMA;

                        if (_commaGroup > 0)
                            for (var mult = 1000; mult < Int32.MaxValue; mult *= 1000)
                            {
                                if (_commaGroup >= mult)
                                    continue;

                                _commaGroup += _currentGroup * mult;
                                break;
                            }
                        else
                            _commaGroup = _currentGroup;

                        break;

                    case DOT:
                        _dotGroupLength += _currentGroupLength;

                        if (_commaGroupLength.IsZero())
                            _firstGroup = DOT;

                        if (_dotGroup > 0)
                            for (var mult = 1000; mult < Int32.MaxValue; mult *= 1000)
                            {
                                if (_dotGroup >= mult)
                                    continue;
                                _dotGroup += _currentGroup * mult;
                                break;
                            }
                        else
                            _dotGroup = _currentGroup;

                        break;
                }


                _currentGroup = _currentGroupLength = 0;
                _multiple = 1;
                if (_isNegation)
                    break;
            }

            _buildingResult = 0;

            switch (_firstGroup)
            {
                case COMMA when _commaGroupLength % 3 == 0:
                    _buildingResult += _commaGroup;

                    _buildingResult += _dotGroup * Math.Pow(10, _commaGroupLength);

                    break;

                case COMMA:
                {
                    if (_dotGroup > 0 || _currentGroup > 0 || fromString[0] == ZERO)
                        _buildingResult = _commaGroup
                                          / Math.Pow(10, _commaGroupLength);
                    else
                        _buildingResult = _commaGroup;

                    if (_dotGroupLength > 0)
                        _buildingResult += _dotGroup;

                    _buildingResult += _currentGroup *
                                       Math.Pow(10, _dotGroupLength);
                    break;
                }
                case DOT:
                {
                    if (_commaGroup > 0 || _commaGroupLength > 0 || _currentGroup > 0
                        || fromString[0] == ZERO)
                        _buildingResult = _dotGroup
                                          / Math.Pow(10, _dotGroupLength);
                    else
                        _buildingResult = _dotGroup;

                    if (_commaGroupLength > 0)
                        _buildingResult += _commaGroup;

                    _buildingResult += _currentGroup *
                                       Math.Pow(10, _commaGroupLength);
                    break;
                }
            }

            return _isNegation ? 0 - _buildingResult : _buildingResult;
        }

        private const Double TOLERANCE = 0.000001;

        private const Char DOT = '.';
        private const Char COMMA = ',';
        private const Char ZERO = '0';
        private const Char EOL = '\n';
        private const Char NEG = '-';

        [ThreadStatic]
        private static Double _buildingResult;

        [ThreadStatic]
        private static Int64 _currentGroup;

        [ThreadStatic]
        private static Int32 _currentGroupLength;

        [ThreadStatic]
        private static Int32 _dotGroupLength;

        [ThreadStatic]
        private static Int64 _dotGroup;

        [ThreadStatic]
        private static Int64 _commaGroup;

        [ThreadStatic]
        private static Int32 _commaGroupLength;

        [ThreadStatic]
        private static Int64 _multiple;

        [ThreadStatic]
        private static Char _firstGroup;

        [ThreadStatic]
        private static Boolean _isNegation;
    }
}
