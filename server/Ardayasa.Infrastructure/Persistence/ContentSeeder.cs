using Ardayasa.Application.Common;
using Ardayasa.Application.Common.Interfaces;
using Ardayasa.Domain;
using Ardayasa.Domain.Entities;
using Ardayasa.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ardayasa.Infrastructure.Persistence;

/// <summary>
/// Seeds Phase 1 marketing content on first run (each block only when its table is
/// empty, so admin edits are never overwritten). Sources: the clinic's Instagram
/// content (see screenshots_instagram/ and docs/DECISIONS.md 2026-07-04).
/// Testimonials and articles are generated examples, replaceable via the admin CMS.
/// </summary>
public static class ContentSeeder
{
    public static async Task SeedAsync(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        IFileStorage files,
        string seedAssetsPath,
        ILogger logger)
    {
        await SeedServicesAsync(db);
        await SeedFaqAsync(db);
        var psychologistIds = await SeedPsychologistsAsync(db, userManager, files, seedAssetsPath, logger);
        await SeedTestimonialsAsync(db, psychologistIds);
        await SeedArticlesAsync(db, userManager);
        await db.SaveChangesAsync();
        logger.LogInformation("Content seed completed");
    }

    // --- Service catalog (from the June 2026 pricelist) ---

    private static async Task SeedServicesAsync(AppDbContext db)
    {
        if (await db.ServiceCategories.AnyAsync())
        {
            return;
        }

        var sort = 0;

        var konseling = NewCategory("Konseling", "Sesi bersama psikolog untuk membicarakan permasalahan yang sedang dialami, memahami diri, dan mencari solusi terbaik.", 1);
        konseling.Services =
        [
            Svc(konseling, "Konseling Dewasa", 60, offline: 330_000, online: 230_000, sort: ++sort),
            Svc(konseling, "Konseling Pasangan", 90, offline: 550_000, online: null, sort: ++sort, notes: "Hanya tersedia offline"),
            Svc(konseling, "Konseling Anak — anak + orang tua", 90, offline: 410_000, online: null, sort: ++sort),
            Svc(konseling, "Konseling Anak — orang tua saja", 60, offline: 350_000, online: null, sort: ++sort),
            Svc(konseling, "Konseling Anak — online", 60, offline: null, online: 230_000, sort: ++sort, notes: "Anak / orang tua"),
            Svc(konseling, "Konseling Pendidikan", 60, offline: 330_000, online: 230_000, sort: ++sort),
        ];

        var bundling = NewCategory("Bundling Konseling", "Paket beberapa sesi konseling dengan harga lebih hemat.", 2);
        bundling.Services =
        [
            Svc(bundling, "Paket Konseling Dewasa — 3 sesi", 60, offline: 915_000, online: null, sessions: 3, sort: ++sort),
            Svc(bundling, "Paket Konseling Dewasa — 5 sesi", 60, offline: 1_435_000, online: null, sessions: 5, sort: ++sort),
            Svc(bundling, "Paket Konseling Dewasa — 12 sesi", 60, offline: 3_300_000, online: null, sessions: 12, sort: ++sort),
            Svc(bundling, "Paket Konseling Pasangan — 3 sesi", 90, offline: 1_500_000, online: null, sessions: 3, sort: ++sort, notes: "Hanya tersedia offline"),
            Svc(bundling, "Paket Konseling Anak — 3 sesi", 90, offline: 1_155_000, online: null, sessions: 3, sort: ++sort),
            Svc(bundling, "Paket Konseling Anak — 5 sesi", 60, offline: 1_835_000, online: null, sessions: 5, sort: ++sort),
        ];

        var asesmen = NewCategory("Asesmen", "Rangkaian tes untuk membantu memahami kondisi klien sebagai dasar perencanaan layanan selanjutnya.", 3);
        asesmen.Services =
        [
            Svc(asesmen, "Asesmen Inteligensi (IQ)", 120, offline: 530_000, online: null, sort: ++sort),
            Svc(asesmen, "Asesmen Minat Bakat", 120, offline: 530_000, online: null, sort: ++sort),
            Svc(asesmen, "Asesmen Kepribadian", 120, offline: 530_000, online: null, sort: ++sort),
            Svc(asesmen, "Asesmen Kesiapan Sekolah", 90, offline: 530_000, online: null, sort: ++sort),
            Svc(asesmen, "Asesmen Kesehatan Mental", 120, offline: 425_000, online: null, sort: ++sort),
            Svc(asesmen, "Skrining Kesehatan Mental", null, offline: 400_000, online: null, sort: ++sort),
            Svc(asesmen, "Cek Tumbuh Kembang", 90, offline: 480_000, online: null, sort: ++sort),
            Svc(asesmen, "Asesmen Perilaku", 90, offline: 480_000, online: null, sort: ++sort),
            Svc(asesmen, "Skrining Diskalkulia dan Disleksia", null, offline: 480_000, online: null, sort: ++sort, notes: "90–120 menit"),
        ];

        var konsultasi = NewCategory("Konsultasi Hasil", "Sesi pembahasan hasil asesmen atau skrining bersama psikolog.", 4);
        konsultasi.Services =
        [
            Svc(konsultasi, "Konsultasi Hasil Asesmen", 45, offline: 260_000, online: 230_000, sort: ++sort),
            Svc(konsultasi, "Konsultasi Hasil Skrining", 45, offline: 230_000, online: null, sort: ++sort),
        ];

        var psikoterapi = NewCategory(
            "Psikoterapi",
            "Fokus pada proses pemulihan: mengolah emosi, mengubah pola pikir atau perilaku yang tidak adaptif. Pendekatan: Cognitive Behavioral Therapy, Acceptance and Commitment Therapy, Dialectical Behavior Therapy, Motivational Interviewing, Eye Movement Desensitization and Reprocessing, Behavioral Modification.",
            5);
        psikoterapi.Services =
        [
            Svc(psikoterapi, "Psikoterapi", 90, offline: 500_000, online: 450_000, sort: ++sort),
            Svc(psikoterapi, "Paket Psikoterapi — 4 sesi", 90, offline: 1_800_000, online: 1_600_000, sessions: 4, sort: ++sort),
        ];

        db.ServiceCategories.AddRange(konseling, bundling, asesmen, konsultasi, psikoterapi);
    }

    private static ServiceCategory NewCategory(string name, string description, int sort)
        => new() { Id = Guid.NewGuid(), Name = name, Description = description, SortOrder = sort };

    private static Service Svc(
        ServiceCategory category, string name, int? duration,
        decimal? offline, decimal? online, int sort, int sessions = 1, string? notes = null)
        => new()
        {
            Id = Guid.NewGuid(),
            CategoryId = category.Id,
            Name = name,
            DurationMinutes = duration,
            OfflinePrice = offline,
            OnlinePrice = online,
            SessionCount = sessions,
            Notes = notes,
            SortOrder = sort,
        };

    // --- FAQ (copy from the clinic's Instagram FAQ series) ---

    private static async Task SeedFaqAsync(AppDbContext db)
    {
        if (await db.FaqItems.AnyAsync())
        {
            return;
        }

        (string Q, string A)[] items =
        [
            ("Apa saja yang dilakukan saat konseling?",
             "<p>Saat konseling, psikolog akan <strong>mendalami permasalahan klien</strong> hingga bagaimana masa kecilnya yang mungkin berpengaruh pada kehidupannya saat ini. Kemudian, psikolog akan mendampingi klien untuk <strong>menemukan solusi terbaik</strong> dan mendorong klien untuk <strong>mencapai perkembangan diri</strong> yang optimal.</p><p>Psikolog juga akan <strong>membekali</strong> klien dengan berbagai <strong>keterampilan psikologis</strong> yang dapat membantu dalam menghadapi kesulitan yang dialami.</p>"),
            ("Bedanya psikoterapi, konseling, sama asesmen apa ya?",
             "<p><strong>Konseling</strong> adalah sesi bersama psikolog untuk <strong>membicarakan</strong> permasalahan yang sedang dialami, memahami diri, dan mencari solusi terbaik. Biasanya menjadi <strong>langkah awal</strong> sebelum asesmen atau psikoterapi.</p><p>Kalau <strong>asesmen</strong> itu <strong>rangkaian tes untuk membantu memahami kondisi klien</strong>, seperti inteligensi, kepribadian, minat bakat, atau kesehatan mental, sebagai dasar perencanaan layanan selanjutnya.</p><p>Sedangkan <strong>psikoterapi</strong> fokusnya pada <strong>proses pemulihan</strong>: mengolah emosi, mengubah pola pikir atau perilaku yang tidak adaptif, dan melatih keterampilan agar lebih siap menghadapi stres ke depan.</p>"),
            ("Bagaimana menentukan apakah aku butuh psikoterapi atau tidak?",
             "<p>Psikolog akan <strong>mendalami kasus klien terlebih dahulu</strong> melalui sesi konseling, observasi, dan asesmen untuk menentukan apakah psikoterapi <strong>dibutuhkan atau tidak</strong>.</p>"),
            ("Apakah aku bisa percayakan ceritaku pada psikolog?",
             "<p>Tentu! Kami para psikolog sudah melakukan <strong>sumpah profesi</strong> untuk <strong>menjunjung tinggi kode etik psikologi</strong> yang salah satunya terkait <strong>menjaga kerahasiaan</strong> data klien.</p>"),
            ("Apa yang harus aku siapkan sebelum sesi konseling?",
             "<ul><li><strong>Siapkan cerita atau keluhan</strong> yang ingin kamu konsultasikan.</li><li>Ingat atau catat <strong>gejala</strong> yang kamu rasakan.</li><li>Tulis <strong>pertanyaan</strong> yang ingin ditanyakan pada psikolog untuk menghindari lupa saat konseling nanti.</li></ul>"),
            ("Apakah konseling bisa dilakukan secara online?",
             "<p><strong>Bisa.</strong> Layanan online tetap mengikuti <strong>standar profesional dan kerahasiaan yang sama</strong>, sehingga kamu bisa menyesuaikan dengan kebutuhan dan kenyamananmu!</p>"),
            ("Bagaimana cara mendaftar sesi-nya?",
             "<ul><li>Kamu bisa <strong>hubungi admin melalui WhatsApp</strong> di <strong>0851-2130-5115</strong>.</li><li>Admin akan memberikan <strong>pilihan psikolog</strong> beserta ketersediaan jadwal dan expertise-nya.</li><li>Kamu akan diminta mengisi <strong>form pendaftaran</strong> dan melakukan <strong>pembayaran</strong>.</li><li>Kamu bisa <strong>menghadiri sesi tepat waktu</strong> sesuai jadwal.</li></ul>"),
            ("Lokasinya di mana?",
             "<p>Saat ini kantor kami berada di <strong>Bogor</strong>: <strong>Bukit Cimanggu City Blok N3/4</strong>.</p><p>Kalau kamu saat ini ada di area Depok, Jakarta, Tangerang, Bekasi, dan sekitarnya, layanan tersedia secara online. Jadi tetap bisa konsultasi tanpa harus datang langsung :)</p>"),
            ("Kalau aku butuh sesi tatap muka, tapi aku di luar Bogor atau tidak bisa datang ke kantor di Bogor, gimana?",
             "<p>Tetap bisa! Jika memang urgent atau kamu lebih nyaman bertemu langsung, sesi tatap muka dapat <strong>kami fasilitasi di co-working space terdekat</strong>.</p><p>Kamu bisa koordinasi terlebih dahulu bersama admin ya!</p>"),
            ("Promo berlaku sampai kapan?",
             "<p>Promo berlaku <strong>sesuai tanggal yang tertera</strong> di poster kami. Setelah melakukan pembayaran <strong>selama periode promo</strong>, layanan dapat digunakan <strong>maksimal H+7</strong> setelah pembayaran ya!</p>"),
            ("Apakah harga promo tetap berlaku jika harus reschedule melewati masa penggunaan?",
             "<p>Harga promo <strong>berlaku selama masa penggunaan</strong> yang ditentukan. Apabila reschedule sesi <strong>melewati batas</strong> tersebut, klien dapat <strong>tetap melanjutkan layanan</strong> dengan melakukan <strong>penyesuaian ke harga normal</strong>.</p>"),
        ];

        db.FaqItems.AddRange(items.Select((x, i) => new FaqItem
        {
            Id = Guid.NewGuid(),
            Question = x.Q,
            AnswerHtml = x.A,
            SortOrder = i + 1,
            IsPublished = true,
        }));
    }

    // --- Psychologists (profiles from Instagram; placeholder accounts, no password set —
    //     they claim access via the password-reset/invitation flow) ---

    private static async Task<Dictionary<string, Guid>> SeedPsychologistsAsync(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        IFileStorage files,
        string seedAssetsPath,
        ILogger logger)
    {
        var ids = new Dictionary<string, Guid>();
        if (await db.Psychologists.AnyAsync())
        {
            return ids;
        }

        var profiles = new[]
        {
            new SeedPsychologist(
                "Fahira Dumbi", "M.Psi., Psikolog", "Psikolog Klinis Dewasa",
                ["Sarjana Psikologi, Universitas Indonesia", "Magister Psikologi Profesi, Universitas Indonesia"],
                ["Kecemasan, trauma, kedukaan", "Relasi interpersonal (teman, keluarga, pasangan)", "Gangguan mood dan emosi (depresi, anger issues, stress management)", "Pengembangan diri (self-esteem, well-being, lifestyle)"],
                "Fahira menemani klien dewasa yang sedang menghadapi kecemasan, trauma, dan kedukaan. Ia percaya setiap orang berhak mendapatkan ruang aman untuk didengar tanpa dihakimi, dan berfokus membekali klien dengan keterampilan psikologis yang bisa dipakai sehari-hari.",
                ["Senin 09.00–13.00 WIB", "Rabu 09.00–13.00 WIB", "Jumat 09.00–13.00 WIB", "Ahad 09.00–11.00 WIB"]),
            new SeedPsychologist(
                "Anna Nadia", "M.Psi., Psikolog", "Psikolog Klinis Dewasa",
                ["Sarjana Psikologi, Universitas Gadjah Mada", "Magister Psikologi Profesi, Universitas Indonesia"],
                ["Kedukaan", "Stress dan kecemasan", "Gangguan kepribadian", "Kesulitan penyesuaian diri", "Gangguan mood (depresi, bipolar, dll.)", "Pengembangan diri (self-esteem, pekerjaan)", "Relasi interpersonal (pasangan, keluarga, dan pertemanan)"],
                "Anna berpengalaman mendampingi klien dewasa dengan tantangan suasana hati, penyesuaian diri, dan relasi. Pendekatannya hangat dan kolaboratif: klien dan psikolog bekerja sama menemukan langkah yang paling realistis untuk dijalani.",
                ["Senin, Rabu, Jumat, Sabtu 13.00–16.00 WIB", "Selasa 09.00–12.00 WIB", "Ahad 11.00–16.00 WIB"]),
            new SeedPsychologist(
                "Anisa Zahra", "M.Psi., Psikolog", "Psikolog Klinis Dewasa",
                ["Sarjana Psikologi, Universitas Gadjah Mada", "Magister Psikologi Profesi, Universitas Gadjah Mada"],
                ["Pranikah", "Self-harm", "Suicide issue", "Stres dan burnout", "Pengelolaan emosi dan diri", "Karier dan pengembangan diri", "Masalah/gangguan kepribadian", "Depresi dan gangguan mood lainnya", "Permasalahan relasi (romantis, keluarga, pertemanan)"],
                "Anisa mendampingi klien yang bergulat dengan emosi yang terasa berat — termasuk self-harm, burnout, dan persoalan relasi. Ia berfokus menciptakan ruang yang tenang dan tidak menghakimi agar proses pemulihan berjalan dengan aman.",
                ["Selasa & Kamis 14.00–18.00 WIB", "Sabtu 09.00–12.00 WIB", "Ahad 16.00–17.00 WIB"]),
            new SeedPsychologist(
                "Rania Fakhirah", "M.Psi., Psikolog", "Psikolog Klinis Anak dan Remaja",
                ["Sarjana Psikologi, Universitas Padjadjaran", "Magister Psikologi Profesi, Universitas Indonesia"],
                ["Masalah pengasuhan", "Masalah dan gangguan akademik", "Masalah perilaku anak dan remaja", "Masalah regulasi emosi anak dan remaja", "Masalah perkembangan anak (gangguan bahasa, Autism Spectrum Disorder, ADHD)"],
                "Rania berfokus pada anak dan remaja: mulai dari pengasuhan, perilaku, regulasi emosi, hingga tumbuh kembang. Ia bekerja bersama orang tua sebagai satu tim, karena perubahan pada anak selalu dimulai dari lingkungan yang mendukung.",
                ["Rabu 09.00–13.00 WIB & 19.00–20.00 WIB (online)", "Kamis 09.00–11.00 WIB (online) & 14.00–18.00 WIB", "Jumat 09.00–13.00 WIB & 19.00–20.00 WIB (online)", "Sabtu dengan perjanjian"]),
            new SeedPsychologist(
                "Aulia Z. Nisa", "M.Psi., Psikolog", "Psikolog Pendidikan",
                ["Sarjana Psikologi, Universitas Brawijaya", "Magister Psikologi Profesi, Universitas Indonesia"],
                ["Kesiapan sekolah", "Minat bakat", "Motivasi belajar", "Kesulitan akademik", "Regulasi diri", "Perkembangan anak"],
                "Aulia membantu anak dan keluarga menavigasi dunia pendidikan: kesiapan sekolah, minat bakat, motivasi belajar, hingga kesulitan akademik. Ia percaya setiap anak punya cara belajarnya sendiri yang layak dipahami dan didukung.",
                ["Sabtu 09.00–17.00 WIB & 19.00–20.00 WIB (online)", "Ahad 14.00–17.00 WIB"]),
        };

        var order = 0;
        foreach (var profile in profiles)
        {
            var slug = SlugHelper.Generate(profile.Name);
            var email = $"{slug.Replace("-", ".")}@ardayasa.local";

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = $"{profile.Name}, {profile.Title}",
                EmailConfirmed = true,
                CreatedAtUtc = DateTime.UtcNow,
            };

            // No password: the account is claimed later via the invitation/reset flow.
            var created = await userManager.CreateAsync(user);
            if (!created.Succeeded)
            {
                logger.LogWarning("Skipping psychologist seed for {Name}: {Errors}", profile.Name,
                    string.Join("; ", created.Errors.Select(e => e.Description)));
                continue;
            }

            await userManager.AddToRoleAsync(user, Roles.Psychologist);

            string? photoKey = null;
            var photoPath = Path.Combine(seedAssetsPath, "psychologists", $"{slug}.jpg");
            if (File.Exists(photoPath))
            {
                await using var stream = File.OpenRead(photoPath);
                photoKey = await files.SaveAsync(stream, $"{slug}.jpg");
            }
            else
            {
                logger.LogWarning("Seed photo not found for {Name} at {Path}", profile.Name, photoPath);
            }

            var psychologist = new Psychologist
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                DisplayName = profile.Name,
                Title = profile.Title,
                Slug = slug,
                Specialization = profile.Specialization,
                Education = [.. profile.Education],
                Expertise = [.. profile.Expertise],
                Bio = profile.Bio,
                PhotoKey = photoKey,
                ScheduleLines = [.. profile.Schedule],
                DisplayOrder = ++order,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
            };

            db.Psychologists.Add(psychologist);
            ids[slug] = psychologist.Id;
        }

        return ids;
    }

    private sealed record SeedPsychologist(
        string Name, string Title, string Specialization,
        string[] Education, string[] Expertise, string Bio, string[] Schedule);

    // --- Testimonials (generated examples — fictional, replaceable via admin CMS) ---

    private static async Task SeedTestimonialsAsync(AppDbContext db, Dictionary<string, Guid> psychologists)
    {
        if (await db.Testimonials.AnyAsync())
        {
            return;
        }

        Guid? For(string slug) => psychologists.TryGetValue(slug, out var id) ? id : null;

        (string Name, string Role, string Content, string? Slug)[] items =
        [
            ("Alya D.", "Klien Konseling Individu",
             "Sesi konseling di Ardayasa benar-benar membantu saya memahami diri sendiri dan merasa lebih tenang setiap hari.", "fahira-dumbi"),
            ("Rizky & Dinda", "Klien Konseling Pasangan",
             "Terima kasih Ardayasa, hubungan kami jadi lebih sehat dan komunikasi kami jauh lebih baik sekarang.", "anna-nadia"),
            ("Fajar R.", "Klien Konseling Karier",
             "Bimbingan kariernya sangat membuka wawasan saya dan membantu saya menemukan tujuan yang jelas.", "anisa-zahra"),
            ("Sari W.", "Orang Tua Klien",
             "Anak saya jadi lebih terbuka dan bisa mengelola emosinya. Kami sebagai orang tua juga dibimbing cara mendampinginya di rumah.", "rania-fakhirah"),
            ("Ratna H.", "Klien Asesmen Anak",
             "Hasil asesmennya dijelaskan dengan sangat jelas dan sabar, jadi kami tahu langkah pendampingan belajar yang tepat untuk anak kami.", "aulia-z-nisa"),
            ("Andi P.", "Klien Psikoterapi",
             "Prosesnya bertahap dan tidak pernah terasa dipaksakan. Sekarang saya punya cara yang lebih sehat untuk menghadapi stres.", null),
        ];

        db.Testimonials.AddRange(items.Select((x, i) => new Testimonial
        {
            Id = Guid.NewGuid(),
            AuthorName = x.Name,
            RoleLabel = x.Role,
            Content = x.Content,
            Rating = 5,
            PsychologistId = x.Slug is null ? null : For(x.Slug),
            IsPublished = true,
            SortOrder = i + 1,
            CreatedAtUtc = DateTime.UtcNow,
        }));
    }

    // --- Sample articles (generated examples — replaceable via admin CMS) ---

    private static async Task SeedArticlesAsync(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        if (await db.Articles.AnyAsync())
        {
            return;
        }

        var admins = await userManager.GetUsersInRoleAsync(Roles.Admin);
        var author = admins.FirstOrDefault();
        if (author is null)
        {
            return;
        }

        var kesehatanMental = new ArticleCategory { Id = Guid.NewGuid(), Name = "Kesehatan Mental", Slug = "kesehatan-mental", SortOrder = 1 };
        var relasi = new ArticleCategory { Id = Guid.NewGuid(), Name = "Relasi", Slug = "relasi", SortOrder = 2 };
        var pengembanganDiri = new ArticleCategory { Id = Guid.NewGuid(), Name = "Pengembangan Diri", Slug = "pengembangan-diri", SortOrder = 3 };
        db.ArticleCategories.AddRange(kesehatanMental, relasi, pengembanganDiri);

        var now = DateTime.UtcNow;
        (string Title, string Excerpt, string Html, ArticleCategory Category, int DaysAgo)[] articles =
        [
            ("Mengenal Kecemasan: Kapan Perlu Bantuan Profesional?",
             "Cemas itu wajar — tetapi ada tanda-tanda ketika kecemasan mulai mengganggu keseharian dan sebaiknya dibicarakan dengan psikolog.",
             "<p>Rasa cemas adalah respons alami tubuh terhadap situasi yang dianggap mengancam atau tidak pasti. Menjelang ujian, wawancara kerja, atau keputusan besar, hampir semua orang merasakannya. Dalam kadar yang wajar, kecemasan justru membantu kita bersiap.</p><h2>Kapan kecemasan menjadi masalah?</h2><p>Kecemasan perlu perhatian lebih ketika ia mulai <strong>mengganggu fungsi sehari-hari</strong>. Beberapa tandanya:</p><ul><li>Khawatir berlebihan yang sulit dikendalikan hampir setiap hari</li><li>Sulit tidur atau sering terbangun karena pikiran berputar</li><li>Gejala fisik seperti jantung berdebar, sesak, atau mual tanpa sebab medis</li><li>Mulai menghindari aktivitas, tempat, atau orang tertentu</li><li>Sulit berkonsentrasi di sekolah atau pekerjaan</li></ul><h2>Apa yang bisa dilakukan?</h2><p>Langkah pertama yang sederhana: <strong>ceritakan pada orang yang kamu percaya</strong>, jaga pola tidur dan aktivitas fisik, serta kurangi kafein. Teknik pernapasan lambat juga terbukti membantu meredakan gejala akut.</p><p>Jika kecemasan sudah berlangsung berminggu-minggu dan terasa mengganggu, berkonsultasi dengan psikolog bukan tanda kelemahan — justru bentuk keberanian merawat diri. Psikolog akan membantu memahami pemicunya dan membekalimu keterampilan untuk mengelolanya.</p><p><em>Kamu tidak harus menghadapi semuanya sendirian.</em></p>",
             kesehatanMental, 21),
            ("Komunikasi Sehat dalam Hubungan: 5 Kebiasaan Kecil yang Berdampak Besar",
             "Kualitas hubungan sering ditentukan bukan oleh momen besar, melainkan kebiasaan komunikasi kecil yang dilakukan setiap hari.",
             "<p>Banyak pasangan datang ke ruang konseling bukan karena masalah besar, melainkan karena tumpukan kesalahpahaman kecil yang tidak pernah selesai. Kabar baiknya: komunikasi adalah keterampilan yang bisa dilatih.</p><h2>1. Dengarkan untuk memahami, bukan untuk membalas</h2><p>Saat pasangan bercerita, tahan keinginan untuk langsung memberi solusi atau pembelaan. Coba ulangi inti ceritanya dengan bahasamu sendiri — ini membuat pasangan merasa benar-benar didengar.</p><h2>2. Gunakan \"aku\" alih-alih \"kamu\"</h2><p>\"Aku merasa kesepian kalau kita jarang mengobrol\" terdengar sangat berbeda dari \"Kamu tidak pernah punya waktu untukku\". Kalimat \"aku\" mengundang diskusi; kalimat \"kamu\" mengundang pertahanan diri.</p><h2>3. Jangan menunda percakapan penting</h2><p>Menghindari konflik terasa aman, tetapi persoalan yang dipendam biasanya kembali dengan bunga. Sepakati waktu yang tenang untuk membicarakannya.</p><h2>4. Kenali tanda banjir emosi</h2><p>Ketika detak jantung naik dan kepala terasa panas, percakapan tidak akan produktif. Tidak apa-apa meminta jeda — asal disertai komitmen untuk kembali melanjutkan.</p><h2>5. Rayakan hal-hal kecil</h2><p>Ucapan terima kasih dan apresiasi kecil setiap hari adalah tabungan emosi yang membuat hubungan lebih tahan terhadap konflik.</p><p>Jika komunikasi terasa terus buntu, konseling pasangan dapat menjadi ruang netral untuk saling memahami kembali.</p>",
             relasi, 14),
            ("Burnout di Tempat Kerja: Tanda-tanda dan Cara Mengatasinya",
             "Lelah biasa hilang dengan istirahat. Burnout tidak. Kenali bedanya dan langkah-langkah pemulihannya.",
             "<p>Burnout adalah kelelahan fisik, emosional, dan mental akibat stres kerja yang berkepanjangan. Ia berkembang perlahan, sehingga sering baru disadari ketika sudah berat.</p><h2>Tanda-tanda burnout</h2><ul><li><strong>Kelelahan kronis</strong> — tidur cukup tetapi tetap merasa habis energi</li><li><strong>Sinisme</strong> — pekerjaan yang dulu bermakna kini terasa hampa atau menjengkelkan</li><li><strong>Menurunnya performa</strong> — sulit fokus, sering menunda, merasa tidak kompeten</li><li>Gejala fisik: sakit kepala, gangguan pencernaan, mudah sakit</li></ul><h2>Apa bedanya dengan lelah biasa?</h2><p>Lelah biasa pulih dengan istirahat akhir pekan. Burnout menetap: liburan hanya menunda, karena sumber stresnya tidak berubah.</p><h2>Langkah pemulihan</h2><p>Mulai dari yang bisa dikendalikan: <strong>batasi jam kerja</strong> yang melebar, latih mengatakan tidak, dan jadwalkan pemulihan aktif — olahraga ringan, hobi, waktu bersama orang terdekat. Bicarakan beban kerja dengan atasan bila memungkinkan.</p><p>Bila gejala menetap lebih dari beberapa minggu atau mulai memengaruhi kesehatan dan relasi, pertimbangkan berkonsultasi dengan psikolog. Konseling membantu memetakan sumber stres dan menyusun strategi pemulihan yang realistis.</p>",
             pengembanganDiri, 7),
        ];

        db.Articles.AddRange(articles.Select(a => new Article
        {
            Id = Guid.NewGuid(),
            Title = a.Title,
            Slug = SlugHelper.Generate(a.Title),
            Excerpt = a.Excerpt,
            ContentHtml = a.Html,
            CategoryId = a.Category.Id,
            Status = ArticleStatus.Published,
            PublishedAtUtc = now.AddDays(-a.DaysAgo),
            AuthorUserId = author.Id,
            CreatedAtUtc = now.AddDays(-a.DaysAgo),
            UpdatedAtUtc = now.AddDays(-a.DaysAgo),
        }));
    }
}
