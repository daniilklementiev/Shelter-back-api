namespace Shelter.Services.KdfService;

public interface IKdfService
{
    String GenerateSalt();
    String GetDerivedKey(String password, String salt);
}