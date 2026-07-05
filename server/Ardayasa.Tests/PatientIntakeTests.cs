using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ardayasa.Tests.Support;

namespace Ardayasa.Tests;

/// <summary>
/// Phase 1.5: patient intake form + admin-managed psychologist assignments.
/// The core guarantee under test: intake answers are readable only by the
/// patient themselves and psychologists with an assignment row — not by other
/// patients, not by unassigned psychologists, and not by admin.
/// </summary>
public class PatientIntakeTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private record TokenResponseBody(UserBody User, string AccessToken, int ExpiresInSeconds);

    private record UserBody(Guid Id, string FullName, string Email, string? WhatsAppNumber, string[] Roles);

    private record ProfileBody(
        string FullName, string? BirthPlace, string? BirthDate, string? Gender,
        string? DomicileAddress, string? MaritalStatus, string? LastEducation, string? Occupation,
        bool? HasAccessedPsychologyServices, bool? HasPriorDiagnosis, string? PriorDiagnosis,
        string? ConsultationConcerns, string? CounselingExpectations, bool IsComplete);

    private record PatientListItemBody(
        Guid UserId, string FullName, string Email, string? WhatsAppNumber,
        bool ProfileCompleted, AssignmentSummaryBody[] Assignments);

    private record AssignmentSummaryBody(Guid PsychologistId, string DisplayName);

    private record PagedBody(PatientListItemBody[] Items, int TotalCount, int Page, int PageSize);

    private record PsychologistBody(Guid Id, Guid UserId, string DisplayName, string Email);

    private record PsychPatientListItemBody(Guid PatientUserId, string FullName, bool ProfileCompleted);

    private record PsychPatientDetailBody(Guid PatientUserId, string AccountName, string Email, ProfileBody? Profile);

    private static readonly object CompleteIntake = new
    {
        fullName = "Pasien Intake Lengkap",
        birthPlace = "Bogor",
        birthDate = "1995-04-12",
        gender = "Female",
        domicileAddress = "Jl. Melati No. 1, Bogor",
        maritalStatus = "Married",
        lastEducation = "Bachelor",
        occupation = "Guru",
        hasAccessedPsychologyServices = true,
        hasPriorDiagnosis = true,
        priorDiagnosis = "Gangguan kecemasan umum (2023)",
        consultationConcerns = "Sulit tidur dan cemas berlebihan sejak awal tahun.",
        counselingExpectations = "Bisa mengelola kecemasan dan tidur lebih baik.",
    };

    [Fact]
    public async Task PatientProfile_UpsertRoundTrip_AndCompleteness()
    {
        var client = factory.CreateApiClient();
        var patientToken = await RegisterPatientAsync(client, "intake1@test.local");

        // Nothing saved yet → 404
        var empty = await SendAsync(client, HttpMethod.Get, "/api/me/patient-profile", patientToken);
        Assert.Equal(HttpStatusCode.NotFound, empty.StatusCode);

        // Partial save is allowed and reported incomplete
        var partial = await SendAsync(client, HttpMethod.Put, "/api/me/patient-profile", patientToken,
            new { fullName = "Pasien Intake", occupation = "Guru" });
        Assert.Equal(HttpStatusCode.OK, partial.StatusCode);
        var partialBody = await partial.Content.ReadFromJsonAsync<ProfileBody>();
        Assert.NotNull(partialBody);
        Assert.False(partialBody.IsComplete);

        // Full save round-trips and is complete
        var full = await SendAsync(client, HttpMethod.Put, "/api/me/patient-profile", patientToken, CompleteIntake);
        Assert.Equal(HttpStatusCode.OK, full.StatusCode);
        var fullBody = await full.Content.ReadFromJsonAsync<ProfileBody>();
        Assert.NotNull(fullBody);
        Assert.True(fullBody.IsComplete);
        Assert.Equal("Gangguan kecemasan umum (2023)", fullBody.PriorDiagnosis);
        Assert.Equal("Female", fullBody.Gender);

        // Changing "prior diagnosis" to no clears the stored description
        var cleared = await SendAsync(client, HttpMethod.Put, "/api/me/patient-profile", patientToken,
            CompleteIntakeWith(hasPriorDiagnosis: false, priorDiagnosis: null));
        Assert.Equal(HttpStatusCode.OK, cleared.StatusCode);
        var clearedBody = await cleared.Content.ReadFromJsonAsync<ProfileBody>();
        Assert.NotNull(clearedBody);
        Assert.Null(clearedBody.PriorDiagnosis);
        Assert.True(clearedBody.IsComplete);

        // "Yes" without a description is rejected
        var invalid = await SendAsync(client, HttpMethod.Put, "/api/me/patient-profile", patientToken,
            CompleteIntakeWith(hasPriorDiagnosis: true, priorDiagnosis: null));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task IntakeAccess_FollowsAssignments()
    {
        var client = factory.CreateApiClient();
        var adminToken = await LoginAsync(client, TestWebApplicationFactory.AdminEmail, TestWebApplicationFactory.AdminPassword);

        // Patient with a completed intake
        var patientToken = await RegisterPatientAsync(client, "intake2@test.local");
        var patientId = await GetUserIdAsync(client, patientToken);
        await SendAsync(client, HttpMethod.Put, "/api/me/patient-profile", patientToken, CompleteIntake);

        // Two psychologists
        var (psychAId, psychAToken) = await InvitePsychologistAsync(client, adminToken, "psikolog.a@test.local", "Psikolog A");
        var (psychBId, psychBToken) = await InvitePsychologistAsync(client, adminToken, "psikolog.b@test.local", "Psikolog B");

        // Unassigned psychologist: empty list, detail 404
        var listBefore = await SendAsync(client, HttpMethod.Get, "/api/psychologist/patients", psychAToken);
        Assert.Equal(HttpStatusCode.OK, listBefore.StatusCode);
        Assert.Empty((await listBefore.Content.ReadFromJsonAsync<PsychPatientListItemBody[]>())!);
        var detailBefore = await SendAsync(client, HttpMethod.Get, $"/api/psychologist/patients/{patientId}", psychAToken);
        Assert.Equal(HttpStatusCode.NotFound, detailBefore.StatusCode);

        // Admin assigns psychologist A; duplicate assign → 409
        var assign = await SendAsync(client, HttpMethod.Post, $"/api/admin/patients/{patientId}/assignments", adminToken,
            new { psychologistId = psychAId });
        Assert.Equal(HttpStatusCode.NoContent, assign.StatusCode);
        var duplicate = await SendAsync(client, HttpMethod.Post, $"/api/admin/patients/{patientId}/assignments", adminToken,
            new { psychologistId = psychAId });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        // Assigned psychologist A sees the patient and the full intake
        var listAfter = await SendAsync(client, HttpMethod.Get, "/api/psychologist/patients", psychAToken);
        var listItems = await listAfter.Content.ReadFromJsonAsync<PsychPatientListItemBody[]>();
        Assert.NotNull(listItems);
        var row = Assert.Single(listItems);
        Assert.Equal(patientId, row.PatientUserId);
        Assert.True(row.ProfileCompleted);

        var detail = await SendAsync(client, HttpMethod.Get, $"/api/psychologist/patients/{patientId}", psychAToken);
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        var detailBody = await detail.Content.ReadFromJsonAsync<PsychPatientDetailBody>();
        Assert.NotNull(detailBody?.Profile);
        Assert.Equal("Gangguan kecemasan umum (2023)", detailBody.Profile.PriorDiagnosis);

        // Psychologist B (not assigned) still gets 404
        var detailB = await SendAsync(client, HttpMethod.Get, $"/api/psychologist/patients/{patientId}", psychBToken);
        Assert.Equal(HttpStatusCode.NotFound, detailB.StatusCode);

        // Patient sees psychologist A in "Psikolog Saya"
        var mine = await SendAsync(client, HttpMethod.Get, "/api/me/psychologists", patientToken);
        Assert.Equal(HttpStatusCode.OK, mine.StatusCode);
        var mineBody = await mine.Content.ReadFromJsonAsync<AssignmentSummaryBody[]>();
        Assert.NotNull(mineBody);
        Assert.Equal(psychAId, Assert.Single(mineBody).PsychologistId);

        // Unassign → psychologist A loses access
        var unassign = await SendAsync(client, HttpMethod.Delete, $"/api/admin/patients/{patientId}/assignments/{psychAId}", adminToken);
        Assert.Equal(HttpStatusCode.NoContent, unassign.StatusCode);
        var detailGone = await SendAsync(client, HttpMethod.Get, $"/api/psychologist/patients/{patientId}", psychAToken);
        Assert.Equal(HttpStatusCode.NotFound, detailGone.StatusCode);
    }

    [Fact]
    public async Task IntakeEndpoints_EnforceRoles()
    {
        var client = factory.CreateApiClient();
        var adminToken = await LoginAsync(client, TestWebApplicationFactory.AdminEmail, TestWebApplicationFactory.AdminPassword);
        var patientToken = await RegisterPatientAsync(client, "intake3@test.local");
        var patientId = await GetUserIdAsync(client, patientToken);
        await SendAsync(client, HttpMethod.Put, "/api/me/patient-profile", patientToken, CompleteIntake);

        // Anonymous: everything 401
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/me/patient-profile")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/patients")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/psychologist/patients")).StatusCode);

        // Admin has NO route to intake answers: the psychologist detail endpoint is
        // forbidden for admins, and the admin list only carries a completeness flag.
        var adminDetail = await SendAsync(client, HttpMethod.Get, $"/api/psychologist/patients/{patientId}", adminToken);
        Assert.Equal(HttpStatusCode.Forbidden, adminDetail.StatusCode);

        var adminList = await SendAsync(client, HttpMethod.Get, "/api/admin/patients?search=intake3", adminToken);
        Assert.Equal(HttpStatusCode.OK, adminList.StatusCode);
        var raw = await adminList.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Gangguan kecemasan", raw);
        var paged = System.Text.Json.JsonSerializer.Deserialize<PagedBody>(raw,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Assert.NotNull(paged);
        var item = Assert.Single(paged.Items);
        Assert.Equal(patientId, item.UserId);
        Assert.True(item.ProfileCompleted);

        // A patient cannot use admin or psychologist endpoints
        Assert.Equal(HttpStatusCode.Forbidden,
            (await SendAsync(client, HttpMethod.Get, "/api/admin/patients", patientToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await SendAsync(client, HttpMethod.Get, $"/api/psychologist/patients/{patientId}", patientToken)).StatusCode);
    }

    private static object CompleteIntakeWith(bool hasPriorDiagnosis, string? priorDiagnosis) => new
    {
        fullName = "Pasien Intake Lengkap",
        birthPlace = "Bogor",
        birthDate = "1995-04-12",
        gender = "Female",
        domicileAddress = "Jl. Melati No. 1, Bogor",
        maritalStatus = "Married",
        lastEducation = "Bachelor",
        occupation = "Guru",
        hasAccessedPsychologyServices = true,
        hasPriorDiagnosis,
        priorDiagnosis,
        consultationConcerns = "Sulit tidur dan cemas berlebihan sejak awal tahun.",
        counselingExpectations = "Bisa mengelola kecemasan dan tidur lebih baik.",
    };

    private async Task<string> RegisterPatientAsync(HttpClient client, string email)
    {
        const string password = "Sandi12345!";
        await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Pasien Intake",
            email,
            whatsAppNumber = "+6281234567890",
            password,
        });
        await client.PostAsJsonAsync("/api/auth/verify-email", new { email, token = factory.Emails.ExtractToken(email) });
        return await LoginAsync(client, email, password);
    }

    private async Task<(Guid PsychologistId, string Token)> InvitePsychologistAsync(
        HttpClient client, string adminToken, string email, string fullName)
    {
        var invite = await SendAsync(client, HttpMethod.Post, "/api/admin/psychologists", adminToken,
            new { fullName, email, title = "M.Psi., Psikolog" });
        Assert.Equal(HttpStatusCode.Created, invite.StatusCode);
        var created = await invite.Content.ReadFromJsonAsync<PsychologistBody>();
        Assert.NotNull(created);

        const string password = "SandiPsikolog1!";
        var accept = await client.PostAsJsonAsync("/api/auth/accept-invitation", new
        {
            email,
            token = factory.Emails.ExtractToken(email),
            password,
        });
        Assert.Equal(HttpStatusCode.NoContent, accept.StatusCode);
        return (created.Id, await LoginAsync(client, email, password));
    }

    private static async Task<Guid> GetUserIdAsync(HttpClient client, string accessToken)
    {
        var response = await SendAsync(client, HttpMethod.Get, "/api/auth/me", accessToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<UserBody>();
        return body!.Id;
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
