using System;
using UnityEngine;

/// <summary>
/// Tzolk'in date calculator.
/// Returns both the display string ("4 Ajaw") and the raw indices
/// needed for calendar ring rotation.
///
/// MATH:
///   Reference: Dec 21, 2012 = 4 Ajaw (end of 13th b'ak'tun)
///   Day sign cycles every 20 days, number cycles every 13 days.
///   Both advance by 1 each day independently.
/// </summary>
public static class BdayGetter
{
    public struct TzolkinResult
    {
        public int number;        // 1-13
        public int signIndex;     // 0-19
        public string signName;   // e.g. "Ajaw"
        public string fullName;   // e.g. "4 Ajaw"
    }

    static readonly string[] daySigns = new string[]
    {
        "Imix", "Ik'", "Ak'bal", "K'an", "Chicchan",
        "Cimi", "Manik'", "Lamat", "Muluk", "Ok",
        "Chuwen", "Eb'", "Ben", "Ix", "Men",
        "Kib'", "Kab'an", "Etz'nab'", "Kawak", "Ajaw"
    };

    // Reference: Dec 21, 2012 = 4 Ajaw
    static readonly DateTime referenceDate = new DateTime(2012, 12, 21);
    const int referenceNumber = 4;
    const int referenceSignIndex = 19; // Ajaw

    /// <summary>
    /// Calculate the Tzolk'in date for a given Gregorian date.
    /// </summary>
    public static TzolkinResult GetTzolkinDate(int year, int month, int day)
    {
        DateTime inputDate = new DateTime(year, month, day);
        int daysBetween = (int)(inputDate - referenceDate).TotalDays;

        // Number cycles 1-13
        int number = ((referenceNumber - 1 + daysBetween) % 13 + 13) % 13 + 1;

        // Sign cycles 0-19
        int signIndex = ((referenceSignIndex + daysBetween) % 20 + 20) % 20;

        string signName = daySigns[signIndex];

        return new TzolkinResult
        {
            number = number,
            signIndex = signIndex,
            signName = signName,
            fullName = $"{number} {signName}"
        };
    }

    public static int DaySignCount => 20;
    public static int NumberCount => 13;
}