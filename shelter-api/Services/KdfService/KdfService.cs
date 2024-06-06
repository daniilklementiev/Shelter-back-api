using System.Security.Cryptography;

namespace Shelter.Services.KdfService;

public class KdfService : IKdfService
{
    private readonly IHashService _hashService;

    public KdfService(IHashService hashService)
    {
        _hashService = hashService;
    }

    public String GenerateSalt()
    {
        byte[] salt = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }
        return Convert.ToBase64String(salt);
    }

    public String GetDerivedKey(String password, String salt)
    {
        return _hashService.Hash(password + salt);
    }
}