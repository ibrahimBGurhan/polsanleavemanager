using PolsanLeave.Calendar.Internal.Domain;

namespace PolsanLeave.Calendar.Internal.Data;

/// <summary>
/// Seed data: official public holidays for 2026 and 2027. Religious holiday dates per the Diyanet
/// calendar. Arife days (half day) are deliberately absent — they are working days (spec 0001, R-4).
/// </summary>
internal static class TurkishPublicHolidays
{
    public static readonly CalendarYear[] Years = [new(2026), new(2027)];

    public static readonly PublicHoliday[] All =
    [
        new(new DateOnly(2026, 1, 1), "Yılbaşı"),
        new(new DateOnly(2026, 3, 20), "Ramazan Bayramı 1. gün"),
        new(new DateOnly(2026, 3, 21), "Ramazan Bayramı 2. gün"),
        new(new DateOnly(2026, 3, 22), "Ramazan Bayramı 3. gün"),
        new(new DateOnly(2026, 4, 23), "Ulusal Egemenlik ve Çocuk Bayramı"),
        new(new DateOnly(2026, 5, 1), "Emek ve Dayanışma Günü"),
        new(new DateOnly(2026, 5, 19), "Atatürk'ü Anma, Gençlik ve Spor Bayramı"),
        new(new DateOnly(2026, 5, 27), "Kurban Bayramı 1. gün"),
        new(new DateOnly(2026, 5, 28), "Kurban Bayramı 2. gün"),
        new(new DateOnly(2026, 5, 29), "Kurban Bayramı 3. gün"),
        new(new DateOnly(2026, 5, 30), "Kurban Bayramı 4. gün"),
        new(new DateOnly(2026, 7, 15), "Demokrasi ve Millî Birlik Günü"),
        new(new DateOnly(2026, 8, 30), "Zafer Bayramı"),
        new(new DateOnly(2026, 10, 29), "Cumhuriyet Bayramı"),

        new(new DateOnly(2027, 1, 1), "Yılbaşı"),
        new(new DateOnly(2027, 3, 9), "Ramazan Bayramı 1. gün"),
        new(new DateOnly(2027, 3, 10), "Ramazan Bayramı 2. gün"),
        new(new DateOnly(2027, 3, 11), "Ramazan Bayramı 3. gün"),
        new(new DateOnly(2027, 4, 23), "Ulusal Egemenlik ve Çocuk Bayramı"),
        new(new DateOnly(2027, 5, 1), "Emek ve Dayanışma Günü"),
        new(new DateOnly(2027, 5, 16), "Kurban Bayramı 1. gün"),
        new(new DateOnly(2027, 5, 17), "Kurban Bayramı 2. gün"),
        new(new DateOnly(2027, 5, 18), "Kurban Bayramı 3. gün"),
        new(new DateOnly(2027, 5, 19), "Kurban Bayramı 4. gün; Atatürk'ü Anma, Gençlik ve Spor Bayramı"),
        new(new DateOnly(2027, 7, 15), "Demokrasi ve Millî Birlik Günü"),
        new(new DateOnly(2027, 8, 30), "Zafer Bayramı"),
        new(new DateOnly(2027, 10, 29), "Cumhuriyet Bayramı"),
    ];
}
