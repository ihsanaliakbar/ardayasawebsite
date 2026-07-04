namespace Ardayasa.Domain;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Psychologist = "Psychologist";
    public const string Patient = "Patient";

    public static readonly string[] All = [Admin, Psychologist, Patient];
}
