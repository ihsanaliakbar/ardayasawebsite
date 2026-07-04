namespace Ardayasa.Infrastructure.Email;

/// <summary>
/// Indonesian (id-ID) email copy, centralized so it can move to per-locale resource
/// files when an English version is added. This is the backend counterpart of the
/// frontend's ngx-translate files — no user-facing string may live outside this class.
/// </summary>
public static class EmailTemplates
{
    private const string Footer =
        "<p style=\"color:#888;font-size:12px\">Email ini dikirim otomatis oleh Ardayasa Wellbeing and Growth Center. " +
        "Jika Anda tidak merasa melakukan permintaan ini, abaikan email ini.</p>";

    public static (string Subject, string HtmlBody) EmailVerification(string fullName, string verifyUrl) =>
        ("Verifikasi email Anda — Ardayasa",
         $"""
          <p>Halo {fullName},</p>
          <p>Terima kasih telah mendaftar di Ardayasa Wellbeing and Growth Center.
          Silakan verifikasi alamat email Anda dengan menekan tombol di bawah ini:</p>
          <p><a href="{verifyUrl}" style="background:#4a6b5d;color:#fff;padding:10px 18px;border-radius:6px;text-decoration:none">Verifikasi Email</a></p>
          <p>Atau salin tautan berikut ke peramban Anda:<br>{verifyUrl}</p>
          {Footer}
          """);

    public static (string Subject, string HtmlBody) PasswordReset(string fullName, string resetUrl) =>
        ("Atur ulang kata sandi — Ardayasa",
         $"""
          <p>Halo {fullName},</p>
          <p>Kami menerima permintaan untuk mengatur ulang kata sandi akun Anda.
          Tekan tombol di bawah ini untuk membuat kata sandi baru (berlaku 24 jam):</p>
          <p><a href="{resetUrl}" style="background:#4a6b5d;color:#fff;padding:10px 18px;border-radius:6px;text-decoration:none">Atur Ulang Kata Sandi</a></p>
          <p>Atau salin tautan berikut ke peramban Anda:<br>{resetUrl}</p>
          {Footer}
          """);

    public static (string Subject, string HtmlBody) PsychologistInvitation(string fullName, string inviteUrl) =>
        ("Undangan bergabung — Ardayasa Wellbeing and Growth Center",
         $"""
          <p>Halo {fullName},</p>
          <p>Anda diundang untuk bergabung sebagai psikolog di platform
          Ardayasa Wellbeing and Growth Center. Silakan atur kata sandi Anda
          melalui tautan berikut (berlaku 24 jam):</p>
          <p><a href="{inviteUrl}" style="background:#4a6b5d;color:#fff;padding:10px 18px;border-radius:6px;text-decoration:none">Terima Undangan</a></p>
          <p>Atau salin tautan berikut ke peramban Anda:<br>{inviteUrl}</p>
          {Footer}
          """);
}
