using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ardayasa.Tests.Support;

namespace Ardayasa.Tests;

/// <summary>
/// Phase 2: availability (admin-managed, psychologist read-only), the
/// psychologist↔service mapping, public slot generation, and the patient
/// booking flow with the intake gate and DB-guarded slot holds.
/// </summary>
public class SchedulingTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private record TokenResponseBody(UserBody User, string AccessToken, int ExpiresInSeconds);

    private record UserBody(Guid Id, string FullName, string Email, string? WhatsAppNumber, string[] Roles);

    private record PsychologistBody(Guid Id, Guid UserId, string DisplayName, string Email);

    private record ProfileBody(Guid Id, string DisplayName, string? Slug);

    private record CategoryBody(Guid Id, string Name);

    private record ServiceBody(Guid Id, string Name);

    private record RuleBody(Guid Id, string DayOfWeek, string StartTime, string EndTime);

    private record ExceptionBody(Guid Id, string Date, string Kind, string? StartTime, string? EndTime);

    private record AvailabilityBody(RuleBody[] Rules, ExceptionBody[] Exceptions);

    private record MapRowBody(Guid ServiceId, string Name, bool Enabled);

    private record SlotBody(DateTime StartUtc, DateTime EndUtc, Guid[] PsychologistIds);

    private record ServicePsychologistBody(Guid PsychologistId, string DisplayName, string? Slug);

    private record DaySlotsBody(string Date, SlotBody[] Slots);

    private record BookingBody(
        Guid Id, Guid PsychologistId, string PsychologistName, string ServiceName, string Mode,
        DateTime StartUtc, DateTime EndUtc, int DurationMinutes, decimal PriceIdr, string Status,
        string? ZoomLink, DateTime? PaymentDueAtUtc);

    private record StaffBookingBody(Guid Id, string Status, string? ZoomLink, string PatientName);

    private record PagedBookingsBody(StaffBookingBody[] Items, int TotalCount);

    private record ErrorsBody(ErrorBody[] Errors);

    private record ErrorBody(string Code, string? Description);

    [Fact]
    public async Task Availability_AdminManaged_PsychologistReadOnly()
    {
        var client = factory.CreateApiClient();
        var adminToken = await LoginAdminAsync(client);
        var (psychId, psychToken) = await InvitePsychologistAsync(client, adminToken, "sched.psy.a@test.local", "Psikolog Jadwal A");

        // Admin replaces the weekly rules; invalid and overlapping windows are rejected.
        var put = await SendAsync(client, HttpMethod.Put, $"/api/admin/psychologists/{psychId}/availability", adminToken, new
        {
            rules = new object[]
            {
                new { dayOfWeek = "Monday", startTime = "09:00:00", endTime = "12:00:00" },
                new { dayOfWeek = "Monday", startTime = "13:00:00", endTime = "17:00:00" },
            },
        });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        var saved = await put.Content.ReadFromJsonAsync<AvailabilityBody>();
        Assert.Equal(2, saved!.Rules.Length);

        Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(client, HttpMethod.Put,
            $"/api/admin/psychologists/{psychId}/availability", adminToken,
            new { rules = new object[] { new { dayOfWeek = "Monday", startTime = "10:00:00", endTime = "09:00:00" } } })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(client, HttpMethod.Put,
            $"/api/admin/psychologists/{psychId}/availability", adminToken,
            new
            {
                rules = new object[]
                {
                    new { dayOfWeek = "Monday", startTime = "09:00:00", endTime = "12:00:00" },
                    new { dayOfWeek = "Monday", startTime = "11:00:00", endTime = "13:00:00" },
                },
            })).StatusCode);

        // Admin adds a dated exception and can remove it again.
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)).ToString("yyyy-MM-dd");
        var addException = await SendAsync(client, HttpMethod.Post,
            $"/api/admin/psychologists/{psychId}/availability/exceptions", adminToken,
            new { date, kind = "Block", startTime = (string?)null, endTime = (string?)null });
        Assert.Equal(HttpStatusCode.OK, addException.StatusCode);
        var exception = await addException.Content.ReadFromJsonAsync<ExceptionBody>();

        // Extra without times is invalid.
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(client, HttpMethod.Post,
            $"/api/admin/psychologists/{psychId}/availability/exceptions", adminToken,
            new { date, kind = "Extra", startTime = (string?)null, endTime = (string?)null })).StatusCode);

        // The psychologist sees their own jadwal praktik read-only...
        var ownView = await SendAsync(client, HttpMethod.Get, "/api/psychologist/availability", psychToken);
        Assert.Equal(HttpStatusCode.OK, ownView.StatusCode);
        var own = await ownView.Content.ReadFromJsonAsync<AvailabilityBody>();
        Assert.Equal(2, own!.Rules.Length);
        Assert.Single(own.Exceptions);

        // ...and has NO mutation surface: no write routes exist under the psychologist
        // prefix (405 = route exists but only for GET), and the admin routes are 403.
        Assert.Equal(HttpStatusCode.MethodNotAllowed,
            (await SendAsync(client, HttpMethod.Put, "/api/psychologist/availability", psychToken, new { rules = Array.Empty<object>() })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await SendAsync(client, HttpMethod.Put, $"/api/admin/psychologists/{psychId}/availability", psychToken, new { rules = Array.Empty<object>() })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await SendAsync(client, HttpMethod.Post, $"/api/admin/psychologists/{psychId}/availability/exceptions", psychToken,
                new { date, kind = "Block" })).StatusCode);

        var remove = await SendAsync(client, HttpMethod.Delete,
            $"/api/admin/psychologists/{psychId}/availability/exceptions/{exception!.Id}", adminToken);
        Assert.Equal(HttpStatusCode.NoContent, remove.StatusCode);

        // Anonymous gets 401 on both surfaces.
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/psychologist/availability")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/admin/psychologists/{psychId}/availability")).StatusCode);
    }

    [Fact]
    public async Task BookingFlow_IntakeGate_SlotHold_And_ZoomLinkVisibility()
    {
        var client = factory.CreateApiClient();
        var adminToken = await LoginAdminAsync(client);

        var (psychId, psychToken) = await InvitePsychologistAsync(client, adminToken, "sched.psy.b@test.local", "Psikolog Jadwal B");
        await SetProfileAndGetSlugAsync(client, adminToken, psychId, "Psikolog Jadwal B");
        var serviceId = await CreateBookableServiceAsync(client, adminToken, "Konseling Tes Online");

        // Enable the service for this psychologist (admin mapping) and open every weekday.
        Assert.Equal(HttpStatusCode.NoContent, (await SendAsync(client, HttpMethod.Put,
            $"/api/admin/psychologists/{psychId}/services", adminToken, new { serviceIds = new[] { serviceId } })).StatusCode);
        var allDays = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" }
            .Select(d => new { dayOfWeek = d, startTime = "09:00:00", endTime = "12:00:00" })
            .ToArray();
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(client, HttpMethod.Put,
            $"/api/admin/psychologists/{psychId}/availability", adminToken, new { rules = allDays })).StatusCode);

        // The admin mapping editor lists the service as enabled.
        var map = await SendAsync(client, HttpMethod.Get, $"/api/admin/psychologists/{psychId}/services", adminToken);
        Assert.Equal(HttpStatusCode.OK, map.StatusCode);
        Assert.Contains((await map.Content.ReadFromJsonAsync<MapRowBody[]>())!, r => r.ServiceId == serviceId && r.Enabled);

        // Public wizard data needs no login: catalog → psychologists for the service → slots.
        var catalog = await client.GetAsync("/api/booking/services");
        Assert.Equal(HttpStatusCode.OK, catalog.StatusCode);
        Assert.Contains((await catalog.Content.ReadFromJsonAsync<ServiceBody[]>())!, s => s.Id == serviceId);

        var offering = await client.GetAsync($"/api/booking/services/{serviceId}/psychologists");
        Assert.Equal(HttpStatusCode.OK, offering.StatusCode);
        Assert.Contains((await offering.Content.ReadFromJsonAsync<ServicePsychologistBody[]>())!,
            p => p.PsychologistId == psychId);

        var slotsResponse = await client.GetAsync($"/api/booking/slots?serviceId={serviceId}&psychologistId={psychId}");
        Assert.Equal(HttpStatusCode.OK, slotsResponse.StatusCode);
        var days = await slotsResponse.Content.ReadFromJsonAsync<DaySlotsBody[]>();
        Assert.NotEmpty(days!);
        // 09:00–12:00 with 60-minute sessions and 0 buffer = 3 slots per full day.
        var fullDay = days!.First(d => d.Slots.Length == 3);
        var slot = fullDay.Slots[0];
        Assert.Equal([psychId], slot.PsychologistIds);

        // A patient with an incomplete intake is gated out of booking.
        var patientToken = await RegisterPatientAsync(client, "sched.patient1@test.local");
        var blocked = await SendAsync(client, HttpMethod.Post, "/api/bookings", patientToken,
            BookingPayload(psychId, serviceId, slot.StartUtc));
        Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
        Assert.Equal("booking.intake_incomplete", (await blocked.Content.ReadFromJsonAsync<ErrorsBody>())!.Errors[0].Code);

        // Completing the Data Pribadi form opens the gate; booking creates a PendingPayment hold.
        await CompleteIntakeAsync(client, patientToken);
        var create = await SendAsync(client, HttpMethod.Post, "/api/bookings", patientToken,
            BookingPayload(psychId, serviceId, slot.StartUtc));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var booking = await create.Content.ReadFromJsonAsync<BookingBody>();
        Assert.Equal("PendingPayment", booking!.Status);
        Assert.Equal(230_000m, booking.PriceIdr);
        Assert.NotNull(booking.PaymentDueAtUtc);
        Assert.Null(booking.ZoomLink);

        // The held slot disappears from public slot generation — both in the
        // per-psychologist view and in the "tanpa preferensi" aggregate (this
        // psychologist is the only one offering the service).
        var slotsAfter = await client.GetAsync($"/api/booking/slots?serviceId={serviceId}&psychologistId={psychId}");
        var daysAfter = await slotsAfter.Content.ReadFromJsonAsync<DaySlotsBody[]>();
        Assert.DoesNotContain(daysAfter!.SelectMany(d => d.Slots), s => s.StartUtc == slot.StartUtc);

        var aggregate = await client.GetAsync($"/api/booking/slots?serviceId={serviceId}");
        Assert.Equal(HttpStatusCode.OK, aggregate.StatusCode);
        var aggregateDays = await aggregate.Content.ReadFromJsonAsync<DaySlotsBody[]>();
        Assert.DoesNotContain(aggregateDays!.SelectMany(d => d.Slots), s => s.StartUtc == slot.StartUtc);
        Assert.All(aggregateDays!.SelectMany(d => d.Slots), s => Assert.Equal([psychId], s.PsychologistIds));

        // ...and a second patient trying the same slot is rejected.
        var patient2Token = await RegisterPatientAsync(client, "sched.patient2@test.local");
        await CompleteIntakeAsync(client, patient2Token);
        var conflict = await SendAsync(client, HttpMethod.Post, "/api/bookings", patient2Token,
            BookingPayload(psychId, serviceId, slot.StartUtc));
        Assert.Equal(HttpStatusCode.BadRequest, conflict.StatusCode);
        Assert.Equal("booking.slot_unavailable", (await conflict.Content.ReadFromJsonAsync<ErrorsBody>())!.Errors[0].Code);

        // Unaligned times are never bookable.
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(client, HttpMethod.Post, "/api/bookings", patientToken,
            BookingPayload(psychId, serviceId, slot.StartUtc.AddMinutes(7)))).StatusCode);

        // The psychologist sees the session and attaches the meeting link to their own booking.
        var psychList = await SendAsync(client, HttpMethod.Get, "/api/psychologist/bookings", psychToken);
        Assert.Equal(HttpStatusCode.OK, psychList.StatusCode);
        Assert.Contains((await psychList.Content.ReadFromJsonAsync<StaffBookingBody[]>())!, b => b.Id == booking.Id);

        Assert.Equal(HttpStatusCode.OK, (await SendAsync(client, HttpMethod.Put,
            $"/api/psychologist/bookings/{booking.Id}/zoom-link", psychToken, new { zoomLink = "https://zoom.us/j/sched-test" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(client, HttpMethod.Put,
            $"/api/psychologist/bookings/{booking.Id}/zoom-link", psychToken, new { zoomLink = "http://insecure.example" })).StatusCode);

        // Another psychologist cannot even learn the booking exists.
        var (_, otherPsychToken) = await InvitePsychologistAsync(client, adminToken, "sched.psy.c@test.local", "Psikolog Jadwal C");
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(client, HttpMethod.Put,
            $"/api/psychologist/bookings/{booking.Id}/zoom-link", otherPsychToken, new { zoomLink = "https://zoom.us/j/x" })).StatusCode);

        // The patient does NOT see the link while the booking is unconfirmed.
        var own = await SendAsync(client, HttpMethod.Get, $"/api/me/bookings/{booking.Id}", patientToken);
        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        Assert.Null((await own.Content.ReadFromJsonAsync<BookingBody>())!.ZoomLink);

        // A patient cannot read someone else's booking (404, existence not leaked).
        Assert.Equal(HttpStatusCode.NotFound,
            (await SendAsync(client, HttpMethod.Get, $"/api/me/bookings/{booking.Id}", patient2Token)).StatusCode);

        // Admin sees the booking in the verification-bound list and can set the link too.
        var adminList = await SendAsync(client, HttpMethod.Get, "/api/admin/bookings?status=PendingPayment", adminToken);
        Assert.Equal(HttpStatusCode.OK, adminList.StatusCode);
        Assert.Contains((await adminList.Content.ReadFromJsonAsync<PagedBookingsBody>())!.Items, b => b.Id == booking.Id);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(client, HttpMethod.Put,
            $"/api/admin/bookings/{booking.Id}/zoom-link", adminToken, new { zoomLink = "https://zoom.us/j/admin-set" })).StatusCode);

        // Role gates: a patient cannot use staff booking surfaces; a psychologist cannot use admin ones.
        Assert.Equal(HttpStatusCode.Forbidden,
            (await SendAsync(client, HttpMethod.Get, "/api/psychologist/bookings", patientToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await SendAsync(client, HttpMethod.Get, "/api/admin/bookings", psychToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await SendAsync(client, HttpMethod.Post, "/api/bookings", psychToken, BookingPayload(psychId, serviceId, slot.StartUtc))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/me/bookings")).StatusCode);
    }

    [Fact]
    public async Task Settings_AdminOnly_BufferAffectsSlotGeneration()
    {
        var client = factory.CreateApiClient();
        var adminToken = await LoginAdminAsync(client);

        var (psychId, psychToken) = await InvitePsychologistAsync(client, adminToken, "sched.psy.d@test.local", "Psikolog Jadwal D");
        await SetProfileAndGetSlugAsync(client, adminToken, psychId, "Psikolog Jadwal D");
        var serviceId = await CreateBookableServiceAsync(client, adminToken, "Konseling Tes Buffer");
        await SendAsync(client, HttpMethod.Put, $"/api/admin/psychologists/{psychId}/services", adminToken,
            new { serviceIds = new[] { serviceId } });
        var allDays = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" }
            .Select(d => new { dayOfWeek = d, startTime = "09:00:00", endTime = "12:00:00" })
            .ToArray();
        await SendAsync(client, HttpMethod.Put, $"/api/admin/psychologists/{psychId}/availability", adminToken, new { rules = allDays });

        // Default buffer 0: 3 slots in a 3-hour window.
        var days = await (await client.GetAsync($"/api/booking/slots?serviceId={serviceId}&psychologistId={psychId}"))
            .Content.ReadFromJsonAsync<DaySlotsBody[]>();
        Assert.Equal(3, days!.First(d => d.Slots.Length >= 2).Slots.Length);

        // Only admin may change settings.
        Assert.Equal(HttpStatusCode.Forbidden,
            (await SendAsync(client, HttpMethod.Put, "/api/admin/settings", psychToken, new { slotBufferMinutes = 30 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await SendAsync(client, HttpMethod.Put, "/api/admin/settings", adminToken, new { slotBufferMinutes = -1 })).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await SendAsync(client, HttpMethod.Put, "/api/admin/settings", adminToken, new { slotBufferMinutes = 30 })).StatusCode);

        // Buffer 30 min: 09:00 and 10:30 fit; 12:00 would end 13:00 > 12:00 → 2 slots.
        var daysBuffered = await (await client.GetAsync($"/api/booking/slots?serviceId={serviceId}&psychologistId={psychId}"))
            .Content.ReadFromJsonAsync<DaySlotsBody[]>();
        Assert.Equal(2, daysBuffered!.First(d => d.Slots.Length >= 2).Slots.Length);

        // Reset for other tests sharing the factory.
        await SendAsync(client, HttpMethod.Put, "/api/admin/settings", adminToken, new { slotBufferMinutes = 0 });
    }

    // --- helpers ---

    private static object BookingPayload(Guid psychologistId, Guid serviceId, DateTime startUtc) => new
    {
        psychologistId,
        serviceId,
        mode = "Online",
        startUtc = startUtc.ToString("O"),
    };

    private async Task<string> LoginAdminAsync(HttpClient client)
        => await LoginAsync(client, TestWebApplicationFactory.AdminEmail, TestWebApplicationFactory.AdminPassword);

    private async Task<string> RegisterPatientAsync(HttpClient client, string email)
    {
        const string password = "Sandi12345!";
        await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Pasien Jadwal",
            email,
            whatsAppNumber = "+6281234567890",
            password,
        });
        await client.PostAsJsonAsync("/api/auth/verify-email", new { email, token = factory.Emails.ExtractToken(email) });
        return await LoginAsync(client, email, password);
    }

    private static async Task CompleteIntakeAsync(HttpClient client, string patientToken)
    {
        var response = await SendAsync(client, HttpMethod.Put, "/api/me/patient-profile", patientToken, new
        {
            fullName = "Pasien Jadwal Lengkap",
            birthPlace = "Bogor",
            birthDate = "1995-05-05",
            gender = "Female",
            domicileAddress = "Bukit Cimanggu City, Bogor",
            maritalStatus = "Single",
            lastEducation = "Bachelor",
            occupation = "Karyawan swasta",
            hasAccessedPsychologyServices = false,
            hasPriorDiagnosis = false,
            priorDiagnosis = (string?)null,
            consultationConcerns = "Kecemasan di tempat kerja.",
            counselingExpectations = "Bisa mengelola stres dengan lebih baik.",
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<(Guid PsychologistId, string Token)> InvitePsychologistAsync(
        HttpClient client, string adminToken, string email, string fullName)
    {
        var invite = await SendAsync(client, HttpMethod.Post, "/api/admin/psychologists", adminToken,
            new { fullName, email, title = "M.Psi., Psikolog" });
        Assert.Equal(HttpStatusCode.Created, invite.StatusCode);
        var created = await invite.Content.ReadFromJsonAsync<PsychologistBody>();

        const string password = "SandiPsikolog1!";
        var accept = await client.PostAsJsonAsync("/api/auth/accept-invitation", new
        {
            email,
            token = factory.Emails.ExtractToken(email),
            password,
        });
        Assert.Equal(HttpStatusCode.NoContent, accept.StatusCode);
        return (created!.Id, await LoginAsync(client, email, password));
    }

    /// <summary>The slug (needed by public booking URLs) is generated on the first admin profile save.</summary>
    private static async Task<string> SetProfileAndGetSlugAsync(HttpClient client, string adminToken, Guid psychologistId, string displayName)
    {
        var response = await SendAsync(client, HttpMethod.Put, $"/api/admin/psychologists/{psychologistId}/profile", adminToken, new
        {
            displayName,
            title = "M.Psi., Psikolog",
            specialization = "Psikolog Klinis Dewasa",
            education = new[] { "Magister Psikologi Profesi" },
            expertise = new[] { "Kecemasan" },
            bio = "Profil uji.",
            scheduleLines = Array.Empty<string>(),
            displayOrder = 1,
            isActive = true,
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<ProfileBody>();
        Assert.NotNull(profile!.Slug);
        return profile.Slug!;
    }

    private static async Task<Guid> CreateBookableServiceAsync(HttpClient client, string adminToken, string name)
    {
        var category = await SendAsync(client, HttpMethod.Post, "/api/admin/services/categories", adminToken,
            new { name = $"Kategori {name}", description = (string?)null, sortOrder = 1 });
        Assert.Equal(HttpStatusCode.OK, category.StatusCode);
        var categoryBody = await category.Content.ReadFromJsonAsync<CategoryBody>();

        var service = await SendAsync(client, HttpMethod.Post, "/api/admin/services", adminToken, new
        {
            categoryId = categoryBody!.Id,
            name,
            description = (string?)null,
            durationMinutes = 60,
            offlinePrice = 330_000,
            onlinePrice = 230_000,
            sessionCount = 1,
            notes = (string?)null,
            sortOrder = 1,
            isActive = true,
        });
        Assert.Equal(HttpStatusCode.OK, service.StatusCode);
        return (await service.Content.ReadFromJsonAsync<ServiceBody>())!.Id;
    }

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client, HttpMethod method, string url, string accessToken, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await client.SendAsync(request);
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<TokenResponseBody>();
        return body!.AccessToken;
    }
}
