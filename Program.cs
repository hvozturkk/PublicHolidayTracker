using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PublicHolidayTracker
{
    // Ödevde verilen sınıf
    class Holiday
    {
        public string date { get; set; }        // "2025-01-01" gibi
        public string localName { get; set; }   // Yerel isim (örn: Yılbaşı)
        public string name { get; set; }        // İngilizce isim
        public string countryCode { get; set; } // Örn: "TR"
        public bool @fixed { get; set; }        // 'fixed' C# için anahtar kelime, başına @ koyduk
        public bool global { get; set; }        // Global tatil mi?
    }

    class Program
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly Dictionary<int, List<Holiday>> _holidaysByYear = new Dictionary<int, List<Holiday>>();
        private static readonly int[] _years = new[] { 2023, 2024, 2025 };

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8; // Türkçe karakterler için

            Console.WriteLine("Resmi tatiller yükleniyor, lütfen bekleyin...\n");
            await LoadHolidaysAsync();

            await ShowMenuAsync();
        }

        /// <summary>
        /// 3 yılın (2023–2025) tatillerini baştan çekip hafızaya alır.
        /// </summary>
        private static async Task LoadHolidaysAsync()
        {
            foreach (var year in _years)
            {
                var url = $"https://date.nager.at/api/v3/PublicHolidays/{year}/TR";

                try
                {
                    string json = await _httpClient.GetStringAsync(url);

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var holidays = JsonSerializer.Deserialize<List<Holiday>>(json, options) ?? new List<Holiday>();
                    _holidaysByYear[year] = holidays;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{year} yılı için veri alınırken hata oluştu: {ex.Message}");
                    _holidaysByYear[year] = new List<Holiday>();
                }
            }
        }

        /// <summary>
        /// Ana menü döngüsü
        /// </summary>
        private static async Task ShowMenuAsync()
        {
            while (true)
            {
                Console.WriteLine("===== PublicHolidayTracker =====");
                Console.WriteLine("1. Tatil listesini göster (yıl seçmeli)");
                Console.WriteLine("2. Tarihe göre tatil ara (gg-aa formatı)");
                Console.WriteLine("3. İsme göre tatil ara");
                Console.WriteLine("4. Tüm tatilleri 3 yıl boyunca göster (2023–2025)");
                Console.WriteLine("5. Çıkış");
                Console.Write("Seçiminiz: ");

                var choice = Console.ReadLine();
                Console.WriteLine();

                switch (choice)
                {
                    case "1":
                        ShowHolidaysByYear();
                        break;
                    case "2":
                        SearchByDate();
                        break;
                    case "3":
                        SearchByName();
                        break;
                    case "4":
                        ShowAllHolidays();
                        break;
                    case "5":
                        Console.WriteLine("Programdan çıkılıyor...");
                        return;
                    default:
                        Console.WriteLine("Geçersiz seçim, lütfen 1-5 arasında bir değer girin.\n");
                        break;
                }

                Console.WriteLine("Devam etmek için bir tuşa basın...");
                Console.ReadKey();
                Console.Clear();
            }
        }

        /// <summary>
        /// Kullanıcıdan yıl alır ve o yıla ait resmi tatilleri listeler.
        /// </summary>
        private static void ShowHolidaysByYear()
        {
            Console.Write("Yıl giriniz (2023 / 2024 / 2025): ");
            var yearInput = Console.ReadLine();

            if (!int.TryParse(yearInput, out int year) || !_holidaysByYear.ContainsKey(year))
            {
                Console.WriteLine("Geçersiz yıl girdiniz.\n");
                return;
            }

            var holidays = _holidaysByYear[year];

            if (holidays.Count == 0)
            {
                Console.WriteLine($"{year} yılı için tatil bulunamadı.\n");
                return;
            }

            Console.WriteLine($"\n{year} yılı resmi tatilleri:\n");
            foreach (var h in holidays.OrderBy(h => h.date))
            {
                Console.WriteLine($"{h.date} - {h.localName} ({h.name}) - Global: {h.global}");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// gg-aa formatında gün-ay alır, tüm yıllar içinde o gün/aya denk gelen tatilleri listeler.
        /// </summary>
        private static void SearchByDate()
        {
            Console.Write("Tarih giriniz (gg-aa formatında, örn: 01-01): ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input) || !input.Contains("-"))
            {
                Console.WriteLine("Geçersiz format. Örnek: 23-04\n");
                return;
            }

            var parts = input.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 ||
                !int.TryParse(parts[0], out int day) ||
                !int.TryParse(parts[1], out int month))
            {
                Console.WriteLine("Geçersiz tarih. Örnek: 23-04\n");
                return;
            }

            var results = new List<(int year, Holiday holiday)>();

            foreach (var kvp in _holidaysByYear)
            {
                int year = kvp.Key;
                foreach (var h in kvp.Value)
                {
                    if (DateTime.TryParse(h.date, out DateTime date))
                    {
                        if (date.Day == day && date.Month == month)
                        {
                            results.Add((year, h));
                        }
                    }
                }
            }

            if (results.Count == 0)
            {
                Console.WriteLine("Bu tarihe denk gelen resmi tatil bulunamadı.\n");
                return;
            }

            Console.WriteLine("\nBulunan tatiller:\n");
            foreach (var item in results.OrderBy(r => r.year))
            {
                Console.WriteLine($"{item.year} - {item.holiday.date} - {item.holiday.localName} ({item.holiday.name})");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Tatil ismine göre (yerel veya İngilizce) arama yapar.
        /// </summary>
        private static void SearchByName()
        {
            Console.Write("Tatil ismi giriniz (yerel isim veya İngilizce isim): ");
            var keyword = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(keyword))
            {
                Console.WriteLine("Boş arama yapılamaz.\n");
                return;
            }

            keyword = keyword.Trim().ToLower();

            var results = new List<(int year, Holiday holiday)>();

            foreach (var kvp in _holidaysByYear)
            {
                int year = kvp.Key;
                foreach (var h in kvp.Value)
                {
                    var local = h.localName?.ToLower() ?? string.Empty;
                    var name = h.name?.ToLower() ?? string.Empty;

                    if (local.Contains(keyword) || name.Contains(keyword))
                    {
                        results.Add((year, h));
                    }
                }
            }

            if (results.Count == 0)
            {
                Console.WriteLine("Bu isimle eşleşen resmi tatil bulunamadı.\n");
                return;
            }

            Console.WriteLine("\nBulunan tatiller:\n");
            foreach (var item in results.OrderBy(r => r.year).ThenBy(r => r.holiday.date))
            {
                Console.WriteLine($"{item.year} - {item.holiday.date} - {item.holiday.localName} ({item.holiday.name})");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// 2023–2025 arasındaki tüm tatilleri listeler.
        /// </summary>
        private static void ShowAllHolidays()
        {
            Console.WriteLine("2023–2025 arasındaki tüm resmi tatiller:\n");

            foreach (var year in _years)
            {
                Console.WriteLine($"--- {year} ---");

                if (!_holidaysByYear.TryGetValue(year, out var holidays) || holidays.Count == 0)
                {
                    Console.WriteLine("Tatil bulunamadı.\n");
                    continue;
                }

                foreach (var h in holidays.OrderBy(h => h.date))
                {
                    Console.WriteLine($"{h.date} - {h.localName} ({h.name}) - Global: {h.global}");
                }
                Console.WriteLine();
            }
        }
    }
}
